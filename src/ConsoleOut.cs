using System;

namespace ParaReload
{
    /// <summary>
    /// Ecrit dans la console de triche du jeu (CTRL+SHIFT+C) en reproduisant ce que
    /// <c>ProcessCheatCommandEvent.Print</c> fait, sans passer par ses champs statiques prives.
    /// Chaque appel est protege : une commande ne doit jamais faire tomber le jeu parce que
    /// l'UI n'est pas encore construite.
    /// </summary>
    public class ConsoleOut
    {
        private readonly int _commandId;
        private readonly int _playerIndex;

        public ConsoleOut(int commandId, int playerIndex)
        {
            _commandId = commandId;
            _playerIndex = playerIndex < 0 ? 0 : playerIndex;
        }

        public void Info(string text)
        {
            Write(text, CheatCommandTextStyle.Results);
            Plugin.Log.LogInfo(text);
        }

        public void Error(string text)
        {
            Write(text, CheatCommandTextStyle.Error);
            Plugin.Log.LogWarning(text);
        }

        private void Write(string text, CheatCommandTextStyle style)
        {
            try
            {
                UI.Get<UIDeveloperTools>(_playerIndex).UICheatCommands.AddText(_commandId, text, style);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Impossible d'ecrire dans la console : " + e.Message);
            }
        }
    }
}
