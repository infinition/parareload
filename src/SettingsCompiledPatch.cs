using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Setting;

namespace ParaReload
{
    /// <summary>
    /// L'autocompletion de la console (UICheatCommands.UpdateSuggestion) et HELP listent
    /// les methodes MAJUSCULES de ProcessCheatCommandEvent plus les alias de Cheats.setting.
    /// Nos commandes n'etant pas de vraies methodes, on les declare comme alias en memoire
    /// pour qu'elles apparaissent dans les deux listes. Le prefixe de dispatch les intercepte
    /// avant que le jeu ne tente de resoudre l'alias, donc la cible n'est jamais suivie.
    ///
    /// Les settings sont recompiles a chaque changement de mod, ce qui remplace l'objet Cheats :
    /// on se raccroche donc a la fin de la compilation pour reinjecter.
    /// </summary>
    public static class SettingsCompiledPatch
    {
        private static readonly (string Alias, ulong Guid)[] Commands =
        {
            ("RELOAD", 9700000000000000001uL),
            ("RELOADALL", 9700000000000000002uL),
            ("RELOADLIST", 9700000000000000003uL),
            ("AUTORELOAD", 9700000000000000004uL),
        };

        private static Cheats _lastPatched;

        public static void Postfix()
        {
            try
            {
                // Settings.Get<T> declenche une compilation si le dictionnaire est vide.
                // On ne veut surtout pas forcer ca depuis un postfix : on attend que le jeu
                // ait compile Cheats de lui-meme.
                if (!Settings.Exists(typeof(Cheats)))
                {
                    return;
                }

                var cheats = Settings.Get<Cheats>();
                if (cheats == null || ReferenceEquals(cheats, _lastPatched))
                {
                    return;
                }

                _lastPatched = cheats;
                cheats.Aliases = Merge(cheats.Aliases);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Injection des alias ParaReload : " + e.Message);
            }
        }

        private static CheatCommandAlias[] Merge(CheatCommandAlias[] existing)
        {
            var merged = new List<CheatCommandAlias>(existing ?? Array.Empty<CheatCommandAlias>());

            foreach (var command in Commands)
            {
                bool alreadyThere = merged.Any(a =>
                    a != null && string.Equals(a.Alias, command.Alias, StringComparison.OrdinalIgnoreCase));

                if (alreadyThere)
                {
                    continue;
                }

                merged.Add(new CheatCommandAlias
                {
                    GUID = command.Guid,
                    Alias = command.Alias,
                    Command = command.Alias,
                    Description = 0uL,
                });
            }

            return merged.ToArray();
        }
    }
}
