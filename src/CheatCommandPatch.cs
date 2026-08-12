using System;

namespace ParaReload
{
    /// <summary>
    /// Le jeu resout ses commandes par reflexion : <c>GetType().GetMethod(NOM).Invoke(...)</c>
    /// sur ProcessCheatCommandEvent. On ne peut pas ajouter de methode a un type existant,
    /// donc on intercepte le dispatch avant qu'il ne cherche la methode.
    /// Le patch est applique explicitement depuis Plugin.ApplyPatches, pas par attributs :
    /// on veut un message clair dans le log si la cible bouge.
    /// </summary>
    public static class CheatCommandPatch
    {
        public static bool Prefix(MessageProcessCheatCommand message)
        {
            if (message == null || string.IsNullOrEmpty(message.Command))
            {
                return true;
            }

            string raw = message.Command.Trim();
            int space = raw.IndexOf(' ');
            string name = (space == -1 ? raw : raw.Substring(0, space)).ToUpperInvariant();
            string args = space == -1 ? string.Empty : raw.Substring(space + 1).Trim();

            var output = new ConsoleOut(message.CommandID, message.PlayerIndex);

            try
            {
                switch (name)
                {
                    case "RELOAD":
                        ReloadService.Request(args.Length == 0 ? null : args, includeMainMod: false, output);
                        return false;

                    case "RELOADALL":
                        ReloadService.Request(null, includeMainMod: true, output);
                        return false;

                    case "RELOADLIST":
                        output.Info(ReloadService.DescribeMods());
                        return false;

                    case "AUTORELOAD":
                        ToggleAutoReload(args, output);
                        return false;

                    default:
                        return true;
                }
            }
            catch (Exception e)
            {
                output.Error(name + " a echoue : " + e.Message);
                Plugin.Log.LogError(e);
                return false;
            }
        }

        private static void ToggleAutoReload(string args, ConsoleOut output)
        {
            bool wanted;

            if (args.Length == 0)
            {
                wanted = !ModWatcher.IsActive;
            }
            else if (!TryParseOnOff(args, out wanted))
            {
                output.Error("Usage : AUTORELOAD [on|off]");
                return;
            }

            if (wanted)
            {
                string watched = ModWatcher.Start();
                output.Info(watched == null
                    ? "AUTORELOAD : impossible de demarrer la surveillance."
                    : "AUTORELOAD actif. Surveillance de " + watched);
            }
            else
            {
                ModWatcher.Stop();
                output.Info("AUTORELOAD inactif.");
            }

            Plugin.AutoReloadEnabled.Value = ModWatcher.IsActive;
        }

        private static bool TryParseOnOff(string value, out bool result)
        {
            switch (value.ToLowerInvariant())
            {
                case "on":
                case "1":
                case "true":
                case "oui":
                    result = true;
                    return true;

                case "off":
                case "0":
                case "false":
                case "non":
                    result = false;
                    return true;

                default:
                    result = false;
                    return false;
            }
        }
    }
}
