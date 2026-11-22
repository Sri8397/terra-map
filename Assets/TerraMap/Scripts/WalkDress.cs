using System.Collections.Generic;
using UnityEngine;

namespace TerraWalk
{
    /// <summary>
    /// Puts clothes and hair on a character. The Quaternius body, outfits and hair all share one 65-bone skeleton
    /// (same names, same rest pose), so a part's skinned meshes are re-pointed at the body's bones by name and the
    /// part's own skeleton is thrown away. One skeleton then carries everything and one animation moves it all.
    /// </summary>
    public static class WalkDress
    {
        /// <summary>Moves every skinned mesh of <paramref name="part"/> onto <paramref name="character"/>'s skeleton, then destroys the part.</summary>
        public static int Attach(GameObject character, GameObject part)
        {
            if (character == null || part == null) return 0;
            var bones = new Dictionary<string, Transform>();
            foreach (var t in character.GetComponentsInChildren<Transform>(true)) bones[t.name] = t;
            bones.TryGetValue("root", out var skeletonRoot);
            // Skinned meshes sit beside the skeleton root (under the same parent), like the body's own meshes.
            var parent = skeletonRoot != null ? skeletonRoot.parent : character.transform;

            var attached = 0;
            foreach (var mesh in part.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var source = mesh.bones;
                var mapped = new Transform[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    mapped[i] = source[i] != null && bones.TryGetValue(source[i].name, out var bone) ? bone : null;
                }
                mesh.bones = mapped;
                mesh.rootBone = mesh.rootBone != null && bones.TryGetValue(mesh.rootBone.name, out var rootBone) ? rootBone : skeletonRoot;
                mesh.transform.SetParent(parent, false);
                attached++;
            }
            // Rigid parts (hair exported "Origin at 0": modelled on the character standing at the origin) are pinned to
            // the head bone where they are, so they follow it. The character must be at the origin, unrotated and unscaled.
            if (bones.TryGetValue("Head", out var head))
            {
                foreach (var mesh in part.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mesh.transform.SetParent(head, true);
                    attached++;
                }
            }
            Object.Destroy(part);
            return attached;
        }
    }
}
