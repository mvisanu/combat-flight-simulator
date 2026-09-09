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
            var catalog = GenerateAircraftAssets();
            var mission = Asset<MissionDefinition>("PacificFighterSweep", ScriptableObject.CreateInstance<MissionDefinition>);
            mission.ConfigureAircraft(catalog, AircraftType.P51D, AircraftType.A6MZero);
            EditorUtility.SetDirty(mission);
            // Serialized materials anchor the required variants without including every URP shader variant.
            string[] shaders = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Particles/Unlit", "PacificCombat/Ocean", "PacificCombat/Terrain", "PacificCombat/Shallows", "PacificCombat/Cloud", "PacificCombat/Atmosphere", "PacificCombat/VolumeCloud" };
            var materials = new Material[shaders.Length];
            for (int i = 0; i < shaders.Length; i++)
            {
                Shader shader = Shader.Find(shaders[i]);
                if (!shader) throw new System.Exception("Required shader not found: " + shaders[i]);
                string path = "Assets/Game/Materials/Included/Shader" + i + ".mat";
                materials[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!materials[i]) { materials[i] = new Material(shader) { enableInstancing = true }; AssetDatabase.CreateAsset(materials[i], path); }
                else materials[i].shader = shader;
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
            PlayerSettings.resizableWindow = true;
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
            pipeline.supportsCameraDepthTexture = true;
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

        public static AircraftCatalog GenerateAircraftAssets()
        {
            Directory.CreateDirectory("Assets/Game/ScriptableObjects");
            Directory.CreateDirectory("Assets/Game/Resources");
            var aircraft = new[]
            {
                Asset<AircraftData>("P51D_Data", AircraftData.CreateMustang),
                Asset<AircraftData>("A6MZero_Data", AircraftData.CreateZero),
                Asset<AircraftData>("Bf109_Data", AircraftData.CreateBf109),
                Asset<AircraftData>("P38Lightning_Data", AircraftData.CreateLightning)
            };
            var weapons = new[]
            {
                Asset<WeaponData>("BrowningM2", WeaponData.CreateMustang),
                Asset<WeaponData>("ZeroArmament", WeaponData.CreateZero),
                Asset<WeaponData>("Bf109Armament", WeaponData.CreateBf109),
                Asset<WeaponData>("LightningArmament", WeaponData.CreateLightning)
            };
            // The original serialized Zero asset predates independently tracked
            // cannon magazines and explicit muzzle locations. Migrate those fields.
            var zeroDefaults = WeaponData.CreateZero();
            weapons[1].MuzzlePositions = zeroDefaults.MuzzlePositions;
            weapons[1].CannonCount = zeroDefaults.CannonCount;
            weapons[1].CannonAmmunition = zeroDefaults.CannonAmmunition;
            weapons[1].CannonMuzzleVelocity = zeroDefaults.CannonMuzzleVelocity;
            weapons[1].CannonRoundsPerMinute = zeroDefaults.CannonRoundsPerMinute;
            weapons[1].CannonDamage = zeroDefaults.CannonDamage;
            UnityEngine.Object.DestroyImmediate(zeroDefaults);
            string[] names = { "P-51D", "A6M Zero", "Bf 109", "P-38" };
            string[] models = { "Art/P51D", "Art/A6MZero", "Art/Bf109", "Art/P38Lightning" };
            string[] armament = { ".50 CAL", "20 MM + 7.7 MM", "20 MM + 13 MM", "20 MM + .50 CAL" };
            string[] advice =
            {
                "Keep your speed. Strike, extend, then climb for another pass.",
                "Use low-speed agility. Turn inside faster opponents and watch your dive speed.",
                "Climb for an advantage. Make short cannon bursts and keep enough speed to escape.",
                "Concentrated nose guns reward accurate aim. Use speed and altitude to set up each pass."
            };
            const string path = "Assets/Game/Resources/AircraftCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<AircraftCatalog>(path);
            if (!catalog) { catalog = ScriptableObject.CreateInstance<AircraftCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
            catalog.Entries = new AircraftCatalogEntry[4];
            for (int i = 0; i < 4; i++)
            {
                // Explicitly migrate old Mustang/Zero assets whose new enum/label fields
                // would otherwise deserialize to the first enum/default weapon label.
                aircraft[i].Type = (AircraftType)i;
                weapons[i].DisplayLabel = armament[i];
                EditorUtility.SetDirty(aircraft[i]); EditorUtility.SetDirty(weapons[i]);
                catalog.Entries[i] = new AircraftCatalogEntry
                {
                    Type = (AircraftType)i, ShortName = names[i], ModelResourcePath = models[i],
                    FlyingAdvice = advice[i], Aircraft = aircraft[i], Weapons = weapons[i]
                };
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
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
            GenerateAircraftAssets();
            SelectionAcceptance.Run(); AircraftRosterAcceptance.Run(); RadarAcceptance.Run(); FlightAcceptance.Run(); CombatAcceptance.Run(); FuelAcceptance.Run(); SettingsAcceptance.Run(); InputAcceptance.Run(); AIAcceptance.Run(); ArtAcceptance.Run();
            WorldAcceptance.Run();
            if (Application.isBatchMode) { AIAcceptance.RunRoster(); AIExpandedAcceptance.Run(); AdvancedFlightAcceptance.Run(); }
            Debug.Log("ALL CORE ACCEPTANCE CHECKS PASSED");
        }
        [MenuItem("Pacific Combat/Build Windows player")]
        public static void VerifyAndBuild() { BlenderArtImporter.Import(); Validate(); Build(); }

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
