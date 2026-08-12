using System;
using HarmonyLib;
using UnityEngine;

namespace ParaReload
{
    /// <summary>
    /// Recoud le monde apres un rechargement d'assets.
    ///
    /// <c>AssetData.Unload()</c> fait <c>Object.Destroy</c> sur les Mesh et les Texture2D.
    /// Les objets deja poses gardent alors un MeshFilter pointant sur un Mesh detruit
    /// (ils disparaissent) et des materiaux pointant sur des textures detruites. Les GUID,
    /// eux, sont stables puisqu'ils viennent des .meta : il suffit de reassigner.
    ///
    /// Deux caches doivent tomber en meme temps :
    /// le dictionnaire de materiaux batches, et surtout le singleton MaterialBuilder, qui
    /// capture l'objet Surfaces a sa premiere initialisation et ne le relit jamais. Sans ce
    /// second reset, une surface declaree par un mod ajoute a chaud reste invisible pour le
    /// constructeur de materiaux : le mesh apparait, pas sa texture.
    /// </summary>
    public static class VisualRefresher
    {
        public static void Refresh()
        {
            DropMaterialCaches();
            ReassignItemMeshes();
            MarkItemMaterialsDirty();
            MarkSegmentMaterialsDirty();
            MarkMoldingMaterialsDirty();
            MarkPlatformMaterialsDirty();
        }

        private static void DropMaterialCaches()
        {
            Guard("caches de materiaux", delegate
            {
                AccessTools.Field(typeof(MaterialBuilder), "_instance")?.SetValue(null, null);
                MaterialBuilder.ResetBatching();
            });
        }

        /// <summary>
        /// On passe par la surcharge SetMesh(Mesh, Mesh) et non SetMesh(ulong) : cette
        /// derniere sort immediatement quand le GUID demande est deja celui du composant,
        /// ce qui est precisement notre cas. Le GUID n'a pas change, c'est l'objet Mesh
        /// derriere lui qui a ete detruit.
        /// </summary>
        private static void ReassignItemMeshes()
        {
            Guard("meshes des objets", delegate
            {
                var references = UnityEngine.Object.FindObjectsOfType<ItemMeshReference>(true);
                int done = 0;

                foreach (var reference in references)
                {
                    if (reference == null || reference.MeshFilter == null)
                    {
                        continue;
                    }

                    Mesh rendering = reference.AssetMesh == 0uL
                        ? null
                        : AssetManager.Instance.GetMesh(reference.AssetMesh);

                    Mesh collider = reference.OverrideMeshForCollision
                        ? AssetManager.Instance.GetMesh(reference.MeshForCollision)
                        : rendering;

                    reference.SetMesh(rendering, collider);
                    done++;
                }

                Plugin.Log.LogInfo(done + " mesh(es) d'objets reassigne(s).");
            });
        }

        private static void MarkItemMaterialsDirty()
        {
            Guard("materiaux des objets", delegate
            {
                var manager = ItemManager.Instance;
                if (manager == null)
                {
                    return;
                }

                var references = UnityEngine.Object.FindObjectsOfType<ItemMeshReference>(true);
                foreach (var reference in references)
                {
                    if (reference != null)
                    {
                        manager.DirtyMaterials.Add(reference);
                    }
                }

                Plugin.Log.LogInfo(references.Length + " materiau(x) d'objets marque(s) sales.");
            });
        }

        /// <summary>
        /// Murs et sols : leur geometrie est procedurale, elle ne contient pas de Mesh
        /// d'asset. Seuls leurs materiaux, qui referencent des surfaces et donc des
        /// textures, ont besoin d'etre reconstruits.
        /// </summary>
        private static void MarkSegmentMaterialsDirty()
        {
            Guard("materiaux des murs", delegate
            {
                var manager = SegmentManager.Instance;
                if (manager != null)
                {
                    manager.DirtyMaterials.AddRange(manager.AllSegments);
                }
            });
        }

        private static void MarkMoldingMaterialsDirty()
        {
            Guard("materiaux des moulures", delegate
            {
                var manager = MoldingManager.Instance;
                if (manager != null)
                {
                    manager.DirtyMaterials.AddRange(manager.AllMoldings);
                }
            });
        }

        private static void MarkPlatformMaterialsDirty()
        {
            Guard("materiaux des plateformes", delegate
            {
                var manager = ZoneManager.Instance;
                if (manager == null)
                {
                    return;
                }

                var platforms = UnityEngine.Object.FindObjectsOfType<ZoneWallPlatformObject>(true);
                foreach (var platform in platforms)
                {
                    if (platform != null)
                    {
                        manager.DirtyPlatformMaterials.Add(platform);
                    }
                }
            });
        }

        /// <summary>
        /// Chaque etape est isolee : si le jeu change et qu'une seule casse, les autres
        /// doivent quand meme recoudre ce qu'elles peuvent.
        /// </summary>
        private static void Guard(string label, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Rafraichissement des " + label + " : " + e.Message);
            }
        }
    }
}
