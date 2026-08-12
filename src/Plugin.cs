using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ParaReload
{
    [BepInPlugin(Guid, "ParaReload", "1.0.0")]
    [BepInProcess("Paralives.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "infinition.paralives.parareload";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        public static ConfigEntry<bool> AutoReloadEnabled;
        public static ConfigEntry<float> AutoReloadDebounce;
        public static ConfigEntry<bool> RegenerateThumbnails;
        public static ConfigEntry<bool> RefreshBuildCatalog;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            AutoReloadEnabled = Config.Bind(
                "AutoReload", "Enabled", false,
                "Surveille les dossiers .mod et declenche un reload des qu'un fichier source change. "
                + "Se bascule aussi en jeu avec la commande AUTORELOAD.");

            AutoReloadDebounce = Config.Bind(
                "AutoReload", "DebounceSeconds", 1.5f,
                "Delai de calme apres le dernier changement de fichier avant de declencher le reload. "
                + "A monter si ton export (Blender, ParaForge) ecrit ses fichiers lentement.");

            RegenerateThumbnails = Config.Bind(
                "Reload", "RegenerateThumbnails", true,
                "Genere les vignettes de catalogue manquantes a la fin du reload.");

            RefreshBuildCatalog = Config.Bind(
                "Reload", "RefreshBuildCatalog", true,
                "Force le catalogue du mode Construction a se redessiner a la fin du reload.");

            _harmony = new Harmony(Guid);
            ApplyPatches();
            Host.Ensure();

            int patched = Harmony.GetAllPatchedMethods().Count();
            Log.LogInfo("ParaReload arme, " + patched + " methode(s) patchee(s). "
                + "Tape RELOAD dans la console CTRL+SHIFT+C.");
        }

        /// <summary>
        /// Applique les patchs en nommant explicitement les methodes cibles, et journalise
        /// le resultat. Un patch qui echoue en silence est le pire mode de panne possible :
        /// le plugin se dit arme et la commande repond "does not exist".
        /// </summary>
        private void ApplyPatches()
        {
            Patch(
                AccessTools.Method(typeof(ProcessCheatCommandEvent), "UpdateMessage",
                    new[] { typeof(MessageProcessCheatCommand) }),
                AccessTools.Method(typeof(CheatCommandPatch), nameof(CheatCommandPatch.Prefix)),
                prefix: true,
                label: "ProcessCheatCommandEvent.UpdateMessage");

            Patch(
                AccessTools.Method(typeof(global::Settings), "CompileSettingObjects"),
                AccessTools.Method(typeof(SettingsCompiledPatch), nameof(SettingsCompiledPatch.Postfix)),
                prefix: false,
                label: "Settings.CompileSettingObjects");
        }

        private void Patch(
            System.Reflection.MethodBase target, System.Reflection.MethodInfo patch, bool prefix, string label)
        {
            if (target == null)
            {
                Log.LogError("Cible introuvable : " + label + ". Le jeu a-t-il ete mis a jour ?");
                return;
            }

            try
            {
                var method = new HarmonyMethod(patch);
                _harmony.Patch(target, prefix ? method : null, prefix ? null : method);
                Log.LogInfo("Patch applique sur " + label + ".");
            }
            catch (Exception e)
            {
                Log.LogError("Patch de " + label + " impossible : " + e);
            }
        }

        private void Start()
        {
            if (AutoReloadEnabled.Value)
            {
                ModWatcher.Start();
            }
        }

        // Pas de OnDestroy : ce GameObject est detruit au chargement de scene par Paralives,
        // et un UnpatchSelf ici desarmait le plugin quelques secondes apres son demarrage.
        // Les patchs Harmony sont a l'echelle du processus, ils n'ont pas besoin d'un
        // MonoBehaviour vivant. Ce qui en a besoin vit dans Host.

        /// <summary>Lance une coroutine sur l'hote persistant.</summary>
        public static Coroutine Run(System.Collections.IEnumerator routine)
        {
            return Host.Run(routine);
        }
    }
}
