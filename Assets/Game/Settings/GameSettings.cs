using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PacificCombat
{
    [Serializable]
    public sealed class GameSettingsData
    {
        public int Version = 1;
        public int GraphicsPreset = 2;
        public float MasterVolume = 1f;
        public FighterDifficulty Difficulty = FighterDifficulty.Regular;
        public bool LeadIndicator;
        public bool UnlimitedAmmo;
        public bool ShowHUD = true;
        public AircraftType PlayerAircraft = AircraftType.P51D;
        public AircraftType EnemyAircraft = AircraftType.A6MZero;
        public MissionPreset Mission = MissionPreset.Sweep;
        public WeatherPreset Weather = WeatherPreset.Scattered;
        public bool SimplifiedDamage;
        public bool AdvancedFlight = true;
        public float EngineVolume = .65f, WeaponVolume = .7f, EffectsVolume = .65f, WindVolume = .65f, UIVolume = .65f;
        public int ResolutionWidth, ResolutionHeight;
        public int WindowMode;
        public bool VSync;
        public int FrameLimit = 120;
        public int DetailSettingsVersion;
        public bool CustomGraphics;
        public float RenderScale = 1f;
        public int TextureQuality, ShadowQuality = 2, CloudQuality = 2, TerrainQuality = 2, EffectsQuality = 2, AntiAliasing = 2;

        public void ApplyPreset(int preset)
        {
            GraphicsPreset = Mathf.Clamp(preset, 0, 3);
            RenderScale = preset == 0 ? .7f : preset == 1 ? .85f : preset == 3 ? 1.2f : 1f;
            TextureQuality = preset == 0 ? 2 : preset == 1 ? 1 : 0;
            ShadowQuality = CloudQuality = TerrainQuality = EffectsQuality = GraphicsPreset;
            AntiAliasing = preset < 2 ? 1 : preset == 3 ? 3 : 2;
            CustomGraphics = false;
            DetailSettingsVersion = 1;
        }

        public void Normalize()
        {
            Version = 1;
            GraphicsPreset = Mathf.Clamp(GraphicsPreset, 0, 3);
            MasterVolume = float.IsNaN(MasterVolume) || float.IsInfinity(MasterVolume) ? 1f : Mathf.Clamp01(MasterVolume);
            if ((int)Difficulty < 0 || (int)Difficulty > 3) Difficulty = FighterDifficulty.Regular;
            if (!AircraftCatalog.IsValidType(PlayerAircraft)) PlayerAircraft = AircraftType.P51D;
            if (!AircraftCatalog.IsValidType(EnemyAircraft)) EnemyAircraft = AircraftType.A6MZero;
            if ((int)Mission < 0 || (int)Mission > 3) Mission = MissionPreset.Sweep;
            if ((int)Weather < 0 || (int)Weather > 3) Weather = WeatherPreset.Scattered;
            if (DetailSettingsVersion == 0) ApplyPreset(GraphicsPreset);
            DetailSettingsVersion = 1;
            EngineVolume = Volume(EngineVolume); WeaponVolume = Volume(WeaponVolume); EffectsVolume = Volume(EffectsVolume);
            WindVolume = Volume(WindVolume); UIVolume = Volume(UIVolume);
            if (ResolutionWidth < 640 || ResolutionWidth > 7680 || ResolutionHeight < 480 || ResolutionHeight > 4320) ResolutionWidth = ResolutionHeight = 0;
            WindowMode = Mathf.Clamp(WindowMode, 0, 2);
            if (FrameLimit != 0) FrameLimit = FrameLimit < 30 ? 120 : Mathf.Min(FrameLimit, 360);
            RenderScale = float.IsNaN(RenderScale) || float.IsInfinity(RenderScale) ? 1f : Mathf.Clamp(RenderScale, .5f, 1.5f);
            TextureQuality = Mathf.Clamp(TextureQuality, 0, 2);
            ShadowQuality = Mathf.Clamp(ShadowQuality, 0, 3); CloudQuality = Mathf.Clamp(CloudQuality, 0, 3);
            TerrainQuality = Mathf.Clamp(TerrainQuality, 0, 3); EffectsQuality = Mathf.Clamp(EffectsQuality, 0, 3); AntiAliasing = Mathf.Clamp(AntiAliasing, 0, 3);
        }
        static float Volume(float value) => float.IsNaN(value) || float.IsInfinity(value) ? .65f : Mathf.Clamp01(value);
    }

    /// <summary>Versioned player preferences. Smoke runs use an isolated, non-persistent session.</summary>
    public static class GameSettings
    {
        public const string PreferenceKey = "PacificCombat.Settings.v1";
        static GameSettingsData current;
        static bool allowPersistence = true;
        static UniversalRenderPipelineAsset runtimePipeline;
        static RenderPipelineAsset previousQualityPipeline;
        static readonly float[] ShadowDistances = { 0f, 500f, 1500f, 2500f };
        static readonly int[] Samples = { 1, 2, 4, 8 };
        static int appliedWidth = -1, appliedHeight = -1, appliedWindowMode = -1;
        public static event Action Changed;
        public static GameSettingsData Current => current ?? (current = Load());
        public static bool PersistenceEnabled => allowPersistence;
        public static string LastStorageError { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntime()
        {
            if (runtimePipeline)
            {
                QualitySettings.renderPipeline = previousQualityPipeline;
                UnityEngine.Object.Destroy(runtimePipeline);
            }
            runtimePipeline = null;
            previousQualityPipeline = null;
            current = null;
            allowPersistence = true;
            LastStorageError = null;
            appliedWidth = appliedHeight = appliedWindowMode = -1;
            Changed = null;
        }

        public static void BeginSession(bool automated = false)
        {
            // Scene reloads keep the current session, including aircraft choices and
            // unsaved preferences when storage is unavailable. A new app/domain loads afresh.
            if (current == null || allowPersistence != !automated)
                current = automated ? new GameSettingsData() : Load();
            allowPersistence = !automated;
            ApplyGlobal();
        }

        public static GameSettingsData Load(string key = PreferenceKey)
        {
            var data = new GameSettingsData();
            LastStorageError = null;
            try
            {
                if (!PlayerPrefs.HasKey(key)) return data;
                string json = PlayerPrefs.GetString(key, "");
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
                    throw new FormatException("Settings must be a JSON object.");
                // Overwrite initialized defaults so an older partial document retains defaults
                // for fields introduced later, especially ShowHUD and MasterVolume.
                JsonUtility.FromJsonOverwrite(json, data);
                if (data.Version != 1) return new GameSettingsData();
                data.Normalize();
                return data;
            }
            catch (Exception ex)
            {
                LastStorageError = ex.Message;
                return new GameSettingsData();
            }
        }

        public static bool Save(GameSettingsData data, string key = PreferenceKey)
        {
            LastStorageError = null;
            try
            {
                if (data == null) throw new ArgumentNullException(nameof(data));
                data.Normalize();
                PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception ex)
            {
                // Preferences can be read-only or full; the current session remains playable.
                LastStorageError = ex.Message;
                return false;
            }
        }

        public static bool SaveCurrent()
        {
            if (!allowPersistence) return true;
            bool saved = Save(Current);
            if (!saved) Debug.LogWarning("Could not save flight settings: " + LastStorageError);
            return saved;
        }

        public static void ApplyGlobal()
        {
            var data = Current;
            data.Normalize();
            AudioListener.volume = data.MasterVolume;
            ApplyAudio();
            QualitySettings.vSyncCount = data.VSync ? 1 : 0;
            Application.targetFrameRate = data.FrameLimit == 0 ? -1 : data.FrameLimit;
            QualitySettings.globalTextureMipmapLimit = data.TextureQuality;
            QualitySettings.shadows = data.ShadowQuality == 0 ? UnityEngine.ShadowQuality.Disable : data.ShadowQuality == 1 ? UnityEngine.ShadowQuality.HardOnly : UnityEngine.ShadowQuality.All;
            QualitySettings.shadowDistance = ShadowDistances[data.ShadowQuality];
            QualitySettings.antiAliasing = data.AntiAliasing == 0 ? 0 : Samples[data.AntiAliasing];
            QualitySettings.lodBias = data.TerrainQuality == 0 ? .6f : data.TerrainQuality == 1 ? .8f : data.TerrainQuality == 3 ? 1.5f : 1f;
            // Automated runs respect their wrapper's resolution and never switch display mode.
            if (Application.isPlaying && allowPersistence && (appliedWidth != data.ResolutionWidth || appliedHeight != data.ResolutionHeight || appliedWindowMode != data.WindowMode))
            {
                var mode = data.WindowMode == 1 ? FullScreenMode.FullScreenWindow : data.WindowMode == 2 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
                if (data.ResolutionWidth > 0) Screen.SetResolution(data.ResolutionWidth, data.ResolutionHeight, mode);
                else Screen.fullScreenMode = mode;
                appliedWidth = data.ResolutionWidth; appliedHeight = data.ResolutionHeight; appliedWindowMode = data.WindowMode;
            }
            PacificEnvironment.SetCloudQuality(data.CloudQuality);
            PacificEnvironment.SetTerrainQuality(data.TerrainQuality);
            PacificEnvironment.SetEffectsQuality(data.EffectsQuality);
            var source = QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline;
            if (source is UniversalRenderPipelineAsset pipeline)
            {
                if (!runtimePipeline || source != runtimePipeline)
                {
                    if (runtimePipeline) UnityEngine.Object.Destroy(runtimePipeline);
                    previousQualityPipeline = QualitySettings.renderPipeline;
                    runtimePipeline = UnityEngine.Object.Instantiate(pipeline);
                    runtimePipeline.name = "Session graphics settings";
                    runtimePipeline.hideFlags = HideFlags.DontSave;
                    QualitySettings.renderPipeline = runtimePipeline;
                }
                // Tune the session clone, never the project's authored URP asset.
                runtimePipeline.shadowDistance = ShadowDistances[data.ShadowQuality];
                runtimePipeline.renderScale = data.RenderScale;
                runtimePipeline.msaaSampleCount = Samples[data.AntiAliasing];
            }
            Changed?.Invoke();
        }

        public static void ApplyAudio()
        {
            AudioListener.volume = Current.MasterVolume;
            AircraftAudio.EngineVolume = Current.EngineVolume;
            AircraftAudio.WeaponVolume = Current.WeaponVolume;
            AircraftAudio.WindVolume = Current.WindVolume;
            AircraftAudio.EffectsVolume = Current.EffectsVolume;
        }

        public static void ApplyToMission(MissionManager mission)
        {
            if (!mission) return;
            var data = Current;
            data.Normalize();
            mission.LeadIndicator = data.LeadIndicator;
            mission.ShowHUD = data.ShowHUD;
            mission.Difficulty = data.Difficulty;
            if (mission.Weapons) mission.Weapons.UnlimitedAmmo = data.UnlimitedAmmo;
            if (mission.Damage) mission.Damage.SimplifiedDamage = data.SimplifiedDamage;
            if (mission.Player && mission.Player.Physics) mission.Player.Physics.AdvancedFlight = data.AdvancedFlight;
            if (mission.Enemies != null) mission.SetDifficulty(data.Difficulty);
            // Developer telemetry deliberately remains session-only (including its F3 shortcut).
        }

        public static void Commit(MissionManager mission)
        {
            ApplyGlobal();
            ApplyToMission(mission);
            SaveCurrent();
        }
    }
}
