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

        public void Normalize()
        {
            Version = 1;
            GraphicsPreset = Mathf.Clamp(GraphicsPreset, 0, 3);
            MasterVolume = float.IsNaN(MasterVolume) || float.IsInfinity(MasterVolume) ? 1f : Mathf.Clamp01(MasterVolume);
            if ((int)Difficulty < 0 || (int)Difficulty > 3) Difficulty = FighterDifficulty.Regular;
        }
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
        static readonly float[] RenderScales = { .7f, .85f, 1f, 1.2f };
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
        }

        public static void BeginSession(bool automated = false)
        {
            allowPersistence = !automated;
            current = automated ? new GameSettingsData() : Load();
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
            int preset = data.GraphicsPreset;
            QualitySettings.shadowDistance = ShadowDistances[preset];
            QualitySettings.antiAliasing = preset < 2 ? 2 : 4;
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
                runtimePipeline.shadowDistance = ShadowDistances[preset];
                runtimePipeline.renderScale = RenderScales[preset];
                runtimePipeline.msaaSampleCount = preset < 2 ? 2 : 4;
            }
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
