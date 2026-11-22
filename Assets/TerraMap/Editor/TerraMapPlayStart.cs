using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TerraMap.EditorTools
{
    /// <summary>
    /// Play always starts the entry scene, whatever scene the editor has open. Unity can open the project on an
    /// Untitled scene (for example on first import, when -openfile is ignored); without this, Play shows an empty sky.
    /// </summary>
    [InitializeOnLoad]
    static class TerraMapPlayStart
    {
        const string EntryScene = "Assets/TerraMap/Scenes/TerraMap.unity";

        static TerraMapPlayStart() => EditorApplication.delayCall += Apply;

        static void Apply()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EntryScene);
            if (scene == null) return;
            EditorSceneManager.playModeStartScene = scene;
            // Swap an unsaved, unchanged Untitled scene for the entry scene so the Hierarchy matches what Play runs.
            var active = SceneManager.GetActiveScene();
            if (!EditorApplication.isPlayingOrWillChangePlaymode && SceneManager.sceneCount == 1
                && string.IsNullOrEmpty(active.path) && !active.isDirty)
                EditorSceneManager.OpenScene(EntryScene, OpenSceneMode.Single);
        }
    }
}
