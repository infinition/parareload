using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ParaReload
{
    /// <summary>
    /// Rejoue a chaud la sequence que le jeu ne fait qu'au demarrage :
    /// decouverte des dossiers .mod, relecture de leur base d'assets depuis le disque,
    /// recompilation des .setting, reimport des sources modifiees, puis rafraichissement
    /// des vignettes et du catalogue.
    /// </summary>
    public static class ReloadService
    {
        public static bool IsRunning { get; private set; }

        /// <summary>Lance un reload si aucun n'est deja en cours.</summary>
        public static void Request(string modFilter, bool includeMainMod, ConsoleOut output)
        {
            if (IsRunning)
            {
                output.Error("Un reload est deja en cours.");
                return;
            }

            Plugin.Run(ReloadRoutine(modFilter, includeMainMod, output));
        }

        private static IEnumerator ReloadRoutine(string modFilter, bool includeMainMod, ConsoleOut output)
        {
            IsRunning = true;
            var stopwatch = Stopwatch.StartNew();

            // On garde la liste hors du try pour pouvoir la reutiliser apres les yield.
            var reloaded = new List<string>();
            var discovered = new List<string>();
            var failed = new List<string>();
            int assetsAfter = 0;

            var modManager = ModManager.Instance;
            var assetManager = AssetManager.Instance;

            if (modManager == null || assetManager == null)
            {
                output.Error("ModManager / AssetManager pas encore prets. Charge une partie d'abord.");
                IsRunning = false;
                yield break;
            }

            // 1. Nouveaux dossiers .mod apparus depuis le lancement du jeu.
            var freshGuids = new HashSet<ulong>();
            try
            {
                discovered.AddRange(DiscoverNewMods(modManager, assetManager, freshGuids));
            }
            catch (Exception e)
            {
                output.Error("Echec du scan des nouveaux mods : " + e.Message);
                Plugin.Log.LogError("DiscoverNewMods : " + e);
            }

            // Le jeu charge lui-meme les mods fraichement enregistres au prochain Update,
            // en respectant Enabled et le mod en cours d'edition. On lui laisse un frame.
            if (discovered.Count > 0)
            {
                yield return null;
                yield return null;
            }

            // 2. Decharger puis recharger la base d'assets de chaque mod cible.
            //    Les mods tout juste decouverts viennent d'etre lus, on ne les relit pas deux fois.
            var targets = CollectTargets(modManager, assetManager, modFilter, includeMainMod, freshGuids);

            if (targets.Count == 0 && discovered.Count == 0)
            {
                output.Error(modFilter == null
                    ? "Aucun mod utilisateur charge a recharger."
                    : "Aucun mod charge ne correspond a \"" + modFilter + "\".");
                IsRunning = false;
                yield break;
            }

            foreach (var mod in targets)
            {
                string name = mod.ModName ?? mod.FileNameNoExtension;

                try
                {
                    // LoadAssetPackage remet IsLoaded a vrai et rescanne le dossier depuis le disque,
                    // en invalidant le _Metacache par date d'ecriture.
                    assetManager.UnloadAssetPackage(mod.GUID);
                    assetManager.LoadAssetPackage(mod.GUID);
                    reloaded.Add(name);
                }
                catch (Exception e)
                {
                    failed.Add(name);
                    Plugin.Log.LogError("Reload de " + name + " : " + e);
                }

                // Un frame entre chaque mod : le reimport declenche des coroutines Unity
                // et on evite de bloquer le rendu sur un gros pack.
                yield return null;
            }

            // 3. Recompiler les .setting (Items, Translations, Surfaces...) et rafraichir les textes.
            try
            {
                Settings.Instance.IsSettingsCompilationDirty = true;
                Settings.Instance.IsTranslationDirty = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Marquage des settings : " + e);
            }

            // Settings.Update() et AssetManager.Update() tournent apres nous dans la frame.
            yield return null;
            yield return null;

            // 4. Attendre la fin du reimport des sources modifiees (FBX, PNG...).
            //    Garde-fou : on ne bloque pas la coroutine indefiniment si le reimport cale.
            float reimportDeadline = Time.realtimeSinceStartup + 300f;
            while (assetManager.ReimportNeeded || assetManager.IsReimporting)
            {
                if (Time.realtimeSinceStartup > reimportDeadline)
                {
                    failed.Add("reimport interrompu apres 300 s");
                    break;
                }

                yield return null;
            }

            // 5. Recoudre le monde : les objets deja poses pointent sur des Mesh et des
            //    textures que Unload a detruits, et les caches de materiaux sont perimes.
            VisualRefresher.Refresh();

            // Les systemes du jeu consomment les listes "dirty" sur plusieurs frames,
            // avec un budget de 20 ms. On leur laisse le temps avant de mesurer la suite.
            for (int frame = 0; frame < 5; frame++)
            {
                yield return null;
            }

            // 6. Vignettes manquantes puis catalogue.
            if (Plugin.RegenerateThumbnails.Value)
            {
                TryGenerateNewThumbnails();
            }

            if (Plugin.RefreshBuildCatalog.Value)
            {
                TryRefreshBuildCatalog();
            }

            foreach (var mod in targets)
            {
                assetsAfter += assetManager.GetAssetsInPackage(mod.GUID).count;
            }

            stopwatch.Stop();

            var summary = new List<string>();
            if (discovered.Count > 0)
            {
                summary.Add("Nouveaux mods : " + string.Join(", ", discovered.ToArray()));
            }
            if (reloaded.Count > 0)
            {
                summary.Add("Recharges : " + string.Join(", ", reloaded.ToArray()));
            }
            if (failed.Count > 0)
            {
                summary.Add("Echecs : " + string.Join(", ", failed.ToArray()));
            }
            summary.Add(assetsAfter + " assets, " + stopwatch.ElapsedMilliseconds + " ms");

            if (includeMainMod)
            {
                // Les visuels de Paras ne sont pas reconstruits : les rebatir demande de
                // decharger puis recharger chaque personnage, ce qui annule ses interactions
                // en cours. Mieux vaut le dire que le faire dans le dos du joueur.
                summary.Add("Main.mod recharge : si des Paras sont devenus invisibles, "
                    + "recharge ta sauvegarde.");
            }

            if (failed.Count > 0)
            {
                output.Error(string.Join(Environment.NewLine, summary.ToArray()));
            }
            else
            {
                output.Info(string.Join(Environment.NewLine, summary.ToArray()));
            }

            IsRunning = false;
        }

        /// <summary>
        /// Enregistre les dossiers *.mod presents sur le disque mais inconnus du jeu.
        /// LoadExistingMod marque la liste des mods comme sale ; c'est ModManager.Update
        /// qui fera le chargement effectif, en respectant Enabled et le mod edite.
        /// </summary>
        private static List<string> DiscoverNewMods(
            ModManager modManager, AssetManager assetManager, HashSet<ulong> freshGuids)
        {
            var added = new List<string>();
            string dataPath = modManager.DataPath;

            if (string.IsNullOrEmpty(dataPath) || !Directory.Exists(dataPath))
            {
                return added;
            }

            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ulong guid in modManager.Mods)
            {
                var asset = assetManager.GetAsset(guid);
                if (asset != null)
                {
                    known.Add(Normalize(asset.FilePath));
                }
            }

            // Meme enumeration que ModManager.LoadAllMods : tout lister puis filtrer sur le
            // suffixe, plutot qu'un motif "*.mod" dont la semantique Windows reserve des
            // surprises (noms courts 8.3, casse).
            var candidates = Directory.GetDirectories(dataPath, "*", SearchOption.TopDirectoryOnly)
                .Where(d => d.EndsWith(".mod", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Plugin.Log.LogInfo("Scan de " + dataPath + " : " + candidates.Length
                + " dossier(s) .mod, " + known.Count + " deja connu(s).");

            foreach (string dir in candidates)
            {
                if (known.Contains(Normalize(dir)))
                {
                    continue;
                }

                Plugin.Log.LogInfo("Dossier .mod inconnu : " + dir);
                var mod = modManager.LoadExistingMod(dir);

                if (mod == null)
                {
                    Plugin.Log.LogWarning("LoadExistingMod a renvoye null pour " + dir
                        + " (GUID deja pris, ou extension non reconnue).");
                    continue;
                }

                added.Add(string.IsNullOrEmpty(mod.ModName) ? Path.GetFileNameWithoutExtension(dir) : mod.ModName);
                freshGuids.Add(mod.GUID);
                Plugin.Log.LogInfo("Nouveau mod enregistre : " + dir + " (GUID " + mod.GUID + ").");
            }

            return added;
        }

        /// <summary>
        /// Les mods rechargeables : charges, non systeme, et hors Main.mod sauf demande explicite.
        /// Recharger un mod systeme ferait sauter les sauvegardes et les premades en memoire.
        /// </summary>
        private static List<AssetMod> CollectTargets(
            ModManager modManager, AssetManager assetManager, string modFilter, bool includeMainMod,
            HashSet<ulong> skip)
        {
            var targets = new List<AssetMod>();

            foreach (ulong guid in modManager.Mods.ToArray())
            {
                var mod = assetManager.GetAsset(guid) as AssetMod;
                if (mod == null || !mod.IsLoaded || skip.Contains(guid))
                {
                    continue;
                }

                bool isMain = guid == ModManager.MainModGUID;

                if (isMain && !includeMainMod)
                {
                    continue;
                }

                if (!isMain && (mod.IsSystemMod || modManager.IsBaseMod(guid)))
                {
                    continue;
                }

                if (modFilter != null && !Matches(mod, modFilter))
                {
                    continue;
                }

                targets.Add(mod);
            }

            return targets;
        }

        private static bool Matches(AssetMod mod, string filter)
        {
            return string.Equals(mod.ModName, filter, StringComparison.OrdinalIgnoreCase)
                || string.Equals(mod.FileNameNoExtension, filter, StringComparison.OrdinalIgnoreCase)
                || (mod.ModName != null && mod.ModName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void TryGenerateNewThumbnails()
        {
            try
            {
                Settings.Get<Setting.Items>().ActionGenerateNewThumbnails?.Invoke();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Generation des vignettes : " + e.Message);
            }
        }

        /// <summary>
        /// UIBuildModeItemList.RefreshList sort tout de suite si _isListDirty est faux ;
        /// on rearme le drapeau avant d'appeler, sinon le catalogue garde son ancienne liste.
        /// </summary>
        private static void TryRefreshBuildCatalog()
        {
            try
            {
                var catalog = UI.GetOrNull<UIBuildModeCatalog>(0);
                var list = catalog?.ItemList;
                if (list == null)
                {
                    return;
                }

                AccessTools.Field(typeof(UIBuildModeItemList), "_isListDirty")?.SetValue(list, true);
                list.RefreshList();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Rafraichissement du catalogue : " + e.Message);
            }
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>Liste lisible des mods, pour la commande RELOADLIST.</summary>
        public static string DescribeMods()
        {
            var modManager = ModManager.Instance;
            var assetManager = AssetManager.Instance;

            if (modManager == null || assetManager == null)
            {
                return "ModManager pas encore pret.";
            }

            var lines = new List<string>();
            foreach (ulong guid in modManager.Mods)
            {
                var mod = assetManager.GetAsset(guid) as AssetMod;
                if (mod == null)
                {
                    continue;
                }

                bool isMain = guid == ModManager.MainModGUID;
                string kind = isMain ? "principal" : (mod.IsSystemMod || modManager.IsBaseMod(guid) ? "systeme" : "utilisateur");
                string state = mod.IsLoaded ? "charge" : (mod.Enabled ? "active" : "desactive");
                int count = assetManager.GetAssetsInPackage(guid).count;

                lines.Add(string.Format("   {0,-28} {1,-12} {2,-10} {3,6} assets   {4}",
                    mod.ModName, kind, state, count, guid));
            }

            lines.Sort();
            return "MODS" + Environment.NewLine + string.Join(Environment.NewLine, lines.ToArray());
        }
    }
}
