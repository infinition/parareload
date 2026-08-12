using System.Collections;
using UnityEngine;

namespace ParaReload
{
    /// <summary>
    /// Porteur de coroutines et boucle Update du plugin.
    ///
    /// Le GameObject du plugin BepInEx ne survit pas au chargement de scene de Paralives :
    /// il est detruit pendant le demarrage, ce qui tuait les coroutines et, tant que
    /// OnDestroy appelait UnpatchSelf, retirait aussi les patchs Harmony. On heberge donc
    /// notre propre objet, marque HideAndDontSave, qu'aucun changement de scene ne detruit.
    /// </summary>
    public class Host : MonoBehaviour
    {
        private static Host _instance;

        public static Host Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ParaReloadHost")
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                    };

                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Host>();
                    Plugin.Log.LogInfo("Hote ParaReload cree.");
                }

                return _instance;
            }
        }

        /// <summary>Force la creation de l'hote sans rien lui demander d'autre.</summary>
        public static void Ensure()
        {
            _ = Instance;
        }

        public static Coroutine Run(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }

        private void Update()
        {
            ModWatcher.Pump();
        }
    }
}
