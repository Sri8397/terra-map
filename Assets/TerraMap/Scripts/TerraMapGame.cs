using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace TerraMap
{
    public sealed class TerraMapGame : MonoBehaviour
    {
        public TerraMapPlayer Player { get; private set; }
        public bool Paused { get; private set; }
        public const string CREDIT = "HELSINKI · 2 km × 2 km · OFFLINE · 3D model: City of Helsinki, CC BY 4.0";
        public string Status { get; private set; } = "Loading local Helsinki map…";
        public bool MapReady { get; private set; }

        IEnumerator Start()
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.antiAliasing = 8;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = 200f;
            QualitySettings.lodBias = 4f;
            QualitySettings.maximumLODLevel = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.Max(60, Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value));
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.65f, 0.74f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.53f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.24f, 0.22f);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = sun;

            Player = new GameObject("Player").AddComponent<TerraMapPlayer>();
            Player.Game = this;
            gameObject.AddComponent<TerraMapControls>().Game = this;
            // Helsinki 3D reality mesh 2017: sixteen 500 m blocks, already in east/up/north metres around the map centre.
            var map = new GameObject("Helsinki · 2 km × 2 km · Local").transform;
            var missing = 0;
            foreach (var km in new[] { "672496", "672497", "673496", "673497" })
            {
                foreach (var quarter in "abcd")
                {
                    var request = Resources.LoadAsync<GameObject>("Helsinki/Helsinki3D_2017_" + km + quarter);
                    yield return request;
                    if (!(request.asset is GameObject prefab)) { missing++; continue; }
                    var block = Instantiate(prefab, map, false);
                    block.name = prefab.name;
                    foreach (var mesh in block.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mesh.sharedMesh != null && mesh.GetComponent<Collider>() == null)
                            mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
                    }
                }
            }
            if (missing == 16)
            {
                Status = "Helsinki map missing in Resources/Helsinki. Run tools/download_map.sh (README.md → Map setup).";
                yield break;
            }
            if (missing > 0) Debug.LogWarning("terra-map: " + missing + " Helsinki blocks did not load.");
            Physics.SyncTransforms();
            MapReady = Player.ResetToStreet();
            Status = MapReady ? CREDIT : "No street collision at the spawn. Check the map in README.md.";
        }

        public void TogglePause()
        {
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : 1f;
            Player.ClearInput();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && Player != null && !Paused) TogglePause();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
