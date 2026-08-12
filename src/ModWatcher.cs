using System;
using System.Collections.Generic;
using System.IO;

namespace ParaReload
{
    /// <summary>
    /// Surveille le dossier des mods et declenche un reload quand un fichier source change.
    ///
    /// Deux pieges evites ici :
    /// le jeu ecrit lui-meme des .meta, des .import et des _Metacache pendant le reload,
    /// ce qui bouclerait a l'infini ; et les mods systeme (sauvegardes, options, premades)
    /// sont reecrits en permanence par le jeu sans qu'aucun asset ne change.
    /// </summary>
    public static class ModWatcher
    {
        private static readonly string[] IgnoredExtensions =
        {
            ".meta", ".import", ".metacache", ".tmp", ".blend1", ".blend2",
        };

        private static readonly string[] IgnoredFolders =
        {
            "_Metacache", "_GeneratedThumbnails",
        };

        /// <summary>Dossiers .mod ecrits par le jeu lui-meme, jamais par le moddeur.</summary>
        private static readonly HashSet<string> IgnoredMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Local.mod", "MyOptions.mod", "MySavedGames.mod",
            "MyPremadeHouseholds.mod", "MyPremadeLot.mod", "MyPremadeOutfits.mod",
        };

        private static FileSystemWatcher _watcher;
        private static string _root;

        private static readonly object Gate = new object();
        private static DateTime? _pendingSince;
        private static string _lastChange;

        public static bool IsActive => _watcher != null;

        /// <summary>Demarre la surveillance. Renvoie le dossier surveille, ou null en cas d'echec.</summary>
        public static string Start()
        {
            Stop();

            var modManager = ModManager.Instance;
            string dataPath = modManager?.DataPath;

            if (string.IsNullOrEmpty(dataPath) || !Directory.Exists(dataPath))
            {
                Plugin.Log.LogWarning("AUTORELOAD : dossier des mods introuvable.");
                return null;
            }

            _root = dataPath;

            try
            {
                _watcher = new FileSystemWatcher(dataPath)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                                   | NotifyFilters.LastWrite | NotifyFilters.Size,
                    InternalBufferSize = 64 * 1024,
                };

                _watcher.Changed += OnChanged;
                _watcher.Created += OnChanged;
                _watcher.Deleted += OnChanged;
                _watcher.Renamed += OnRenamed;
                _watcher.Error += OnError;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("AUTORELOAD : " + e);
                Stop();
                return null;
            }

            Plugin.Log.LogInfo("AUTORELOAD actif sur " + dataPath);
            return dataPath;
        }

        public static void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnChanged;
                _watcher.Created -= OnChanged;
                _watcher.Deleted -= OnChanged;
                _watcher.Renamed -= OnRenamed;
                _watcher.Error -= OnError;
                _watcher.Dispose();
                _watcher = null;
            }

            lock (Gate)
            {
                _pendingSince = null;
                _lastChange = null;
            }
        }

        /// <summary>
        /// Appele chaque frame depuis le plugin. Les evenements arrivent sur un thread du pool,
        /// donc tout ce qui touche a Unity doit passer par ici.
        /// </summary>
        public static void Pump()
        {
            if (_watcher == null || ReloadService.IsRunning)
            {
                return;
            }

            string changed;

            lock (Gate)
            {
                if (_pendingSince == null)
                {
                    return;
                }

                double quiet = (DateTime.UtcNow - _pendingSince.Value).TotalSeconds;
                if (quiet < Plugin.AutoReloadDebounce.Value)
                {
                    return;
                }

                changed = _lastChange;
                _pendingSince = null;
                _lastChange = null;
            }

            var output = new ConsoleOut(0, 0);
            output.Info("AUTORELOAD : " + Path.GetFileName(changed) + " a change, rechargement.");
            ReloadService.Request(null, includeMainMod: false, output);
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Queue(e.FullPath);
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Queue(e.FullPath);
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            Plugin.Log.LogWarning("AUTORELOAD : la surveillance a decroche (" + e.GetException().Message + ").");
        }

        private static void Queue(string fullPath)
        {
            if (ShouldIgnore(fullPath))
            {
                return;
            }

            lock (Gate)
            {
                // On repousse la fenetre a chaque evenement : un export ecrit plusieurs
                // fichiers d'affilee, on ne recharge qu'une fois le calme revenu.
                _pendingSince = DateTime.UtcNow;
                _lastChange = fullPath;
            }
        }

        private static bool ShouldIgnore(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(_root))
            {
                return true;
            }

            string extension = Path.GetExtension(fullPath);
            foreach (string ignored in IgnoredExtensions)
            {
                if (string.Equals(extension, ignored, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (string.Equals(Path.GetFileName(fullPath), "AssetReimportInfo.txt", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string relative = Relative(fullPath);
            if (relative == null)
            {
                return true;
            }

            string[] segments = relative.Split('/');

            // Premier segment : le dossier .mod. On ignore ceux que le jeu ecrit tout seul.
            if (segments.Length == 0 || IgnoredMods.Contains(segments[0]))
            {
                return true;
            }

            foreach (string segment in segments)
            {
                foreach (string ignored in IgnoredFolders)
                {
                    if (string.Equals(segment, ignored, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string Relative(string fullPath)
        {
            string root = _root.Replace('\\', '/').TrimEnd('/');
            string path = fullPath.Replace('\\', '/');

            if (!path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return path.Substring(root.Length + 1);
        }
    }
}
