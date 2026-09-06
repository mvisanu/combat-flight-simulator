using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PacificCombat.Editor
{
    public static class ProjectBuilder
    {
        const string ScenePath = "Assets/Game/Scenes/PacificFighterSweep.unity";
        [MenuItem("Pacific Combat/Generate mission assets and scene")]
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/Game/ScriptableObjects");
            Directory.CreateDirectory("Assets/Game/Scenes");
            Directory.CreateDirectory("Assets/Game/Materials/Included");
            var mustang = Asset<AircraftData>("P51D_Data", AircraftData.CreateMustang);
            var zero = Asset<AircraftData>("A6MZero_Data", AircraftData.CreateZero);
            var mustangWeapons = Asset<WeaponData>("BrowningM2", WeaponData.CreateMustang);
            var zeroWeapons = Asset<WeaponData>("ZeroArmament", WeaponData.CreateZero);
            var mission = Asset<MissionDefinition>("PacificFighterSweep", ScriptableObject.CreateInstance<MissionDefinition>);
            mission.PlayerAircraft = mustang; mission.EnemyAircraft = zero;
            mission.PlayerWeapons = mustangWeapons; mission.EnemyWeapons = zeroWeapons;
            EditorUtility.SetDirty(mission);
            // Serialized materials anchor the required variants without including every URP shader variant.
            string[] shaders = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Particles/Unlit", "PacificCombat/Ocean" };
            var materials = new Material[shaders.Length];
            for (int i = 0; i < shaders.Length; i++)
            {
                Shader shader = Shader.Find(shaders[i]);
                if (!shader) throw new System.Exception("Required shader not found: " + shaders[i]);
                string path = "Assets/Game/Materials/Included/Shader" + i + ".mat";
                materials[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!materials[i]) { materials[i] = new Material(shader) { enableInstancing = true }; AssetDatabase.CreateAsset(materials[i], path); }
                if (i == 2)
                {
                    materials[i].SetFloat("_Surface", 1); materials[i].SetFloat("_ZWrite", 0);
                    materials[i].SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); materials[i].SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    materials[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); materials[i].renderQueue = 3000;
                }
                EditorUtility.SetDirty(materials[i]);
            }
            PlayerSettings.companyName = "Pacific Combat";
            PlayerSettings.productName = "Pacific Fighter Sweep";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            QualitySettings.vSyncCount = 0;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline = pipeline; }
            pipeline.shadowDistance = 1200; pipeline.msaaSampleCount = 4;
            EditorUtility.SetDirty(pipeline);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var manager = new GameObject("Pacific Fighter Sweep").AddComponent<MissionManager>();
            manager.Definition = mission;
            manager.IncludedShaderMaterials = materials;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("PROJECT GENERATION PASSED: URP assets, aircraft data, weapon data, mission and scene.");
        }
        static T Asset<T>(string name, System.Func<T> create) where T : ScriptableObject
        {
            string path = "Assets/Game/ScriptableObjects/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) { asset = create(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        [MenuItem("Pacific Combat/Run core acceptance checks")]
        public static void Validate()
        {
            FlightAcceptance.Run(); CombatAcceptance.Run(); FuelAcceptance.Run(); SettingsAcceptance.Run(); InputAcceptance.Run(); AIAcceptance.Run();
            Debug.Log("ALL CORE ACCEPTANCE CHECKS PASSED");
        }
        [MenuItem("Pacific Combat/Build Windows player")]
        public static void VerifyAndBuild() { Validate(); Build(); }

        public static void Build()
        {
            Generate();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/PacificFighterSweep.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Windows build failed: " + report.summary.result);
            Debug.Log($"WINDOWS BUILD PASSED: {report.summary.totalSize} bytes, {report.summary.totalTime.TotalSeconds:F1}s");
        }
    }
}
