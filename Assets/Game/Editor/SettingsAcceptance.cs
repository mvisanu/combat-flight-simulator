using System;
using UnityEditor;
using UnityEngine;

namespace PacificCombat.Editor
{
    public static class SettingsAcceptance
    {
        [MenuItem("Pacific Combat/Run Settings Acceptance")]
        public static void Run()
        {
            string key = "PacificCombat.Tests.Settings." + Guid.NewGuid().ToString("N");
            bool hadUserSettings = PlayerPrefs.HasKey(GameSettings.PreferenceKey);
            string userSettings = PlayerPrefs.GetString(GameSettings.PreferenceKey, "");
            int assertions = 0;
            try
            {
                var defaults = GameSettings.Load(key);
                Require(defaults.GraphicsPreset == 2 && defaults.MasterVolume == 1f && defaults.Difficulty == FighterDifficulty.Regular,
                    "Missing preferences use high graphics, full audio and regular AI.", ref assertions);
                Require(defaults.ShowHUD && !defaults.LeadIndicator && !defaults.UnlimitedAmmo,
                    "Training assists are off and the flight HUD is on by default.", ref assertions);
                var configured = new GameSettingsData
                {
                    GraphicsPreset = 0, MasterVolume = .37f, Difficulty = FighterDifficulty.Ace,
                    LeadIndicator = true, UnlimitedAmmo = true, ShowHUD = false
                };
                Require(GameSettings.Save(configured, key), "Preferences save successfully.", ref assertions);
                var restored = GameSettings.Load(key);
                Require(restored.GraphicsPreset == 0 && Mathf.Abs(restored.MasterVolume - .37f) < .0001f && restored.Difficulty == FighterDifficulty.Ace,
                    "Graphics, volume and difficulty round-trip through the preferences store.", ref assertions);
                Require(restored.LeadIndicator && restored.UnlimitedAmmo && !restored.ShowHUD,
                    "Training assists and HUD visibility round-trip through the preferences store.", ref assertions);
                configured.Mission = MissionPreset.LandingPractice; configured.Weather = WeatherPreset.Squall;
                configured.DetailSettingsVersion = 1; configured.CustomGraphics = true; configured.RenderScale = .8f;
                configured.ResolutionWidth = 1920; configured.ResolutionHeight = 1080; configured.WindowMode = 1;
                configured.EngineVolume = .22f; configured.WeaponVolume = .33f; configured.EffectsVolume = .44f; configured.WindVolume = .55f; configured.UIVolume = .66f;
                configured.CloudQuality = 3; configured.TerrainQuality = 1; configured.EffectsQuality = 0; configured.ShadowQuality = 1;
                configured.AntiAliasing = 3; configured.TextureQuality = 2; configured.FrameLimit = 90; configured.VSync = true;
                configured.AdvancedFlight = false; configured.SimplifiedDamage = true;
                Require(GameSettings.Save(configured,key),"Expanded settings save",ref assertions);
                restored = GameSettings.Load(key);
                Require(JsonUtility.ToJson(restored) == JsonUtility.ToJson(configured),"Every expanded setting round-trips exactly",ref assertions);
                var axis = new AxisCalibration { Minimum = -.8f, Maximum = .9f, Center = .1f, Deadzone = .05f, Response = 1.5f };
                axis.Normalize();
                Require(axis.Signed(.1f) == 0 && Mathf.Abs(axis.Signed(-.8f)+1)<.001f && Mathf.Abs(axis.Signed(.9f)-1)<.001f,
                    "Asymmetric hardware travel calibrates to centered full-range controls",ref assertions);
                axis.Invert = true;
                Require(axis.Signed(.9f) < -.99f && axis.Unsigned(-.8f) > .99f && axis.Signed(float.NaN) == 0,
                    "Inversion and invalid input remain safe",ref assertions);
                var calibration = new InputCalibrationData { Pitch = axis };
                Require(InputCalibrationStore.Save(calibration,key+".axis") && InputCalibrationStore.Load(key+".axis").Pitch.Invert,
                    "Hardware calibration persists",ref assertions);

                PlayerPrefs.SetString(key, "{\"GraphicsPreset\":99,\"MasterVolume\":-4,\"Difficulty\":99}");
                var invalid = GameSettings.Load(key);
                Require(invalid.GraphicsPreset == 3 && invalid.MasterVolume == 0 && invalid.Difficulty == FighterDifficulty.Regular,
                    "Out-of-range stored values are clamped or replaced with safe defaults.", ref assertions);
                Require(invalid.ShowHUD, "Partial preference documents preserve defaults for missing fields.", ref assertions);
                PlayerPrefs.SetString(key, "{\"Version\":99,\"ShowHUD\":false}");
                Require(GameSettings.Load(key).ShowHUD, "Unknown schema versions fall back to defaults.", ref assertions);
                PlayerPrefs.SetString(key, "this is not a settings document");
                Require(GameSettings.Load(key).GraphicsPreset == 2 && GameSettings.LastStorageError != null,
                    "Malformed preferences produce defaults and a diagnostic without throwing.", ref assertions);
                var nonfinite = new GameSettingsData { MasterVolume = float.NaN, GraphicsPreset = -99, Difficulty = (FighterDifficulty)(-1) };
                Require(GameSettings.Save(nonfinite, key), "Invalid in-memory settings are normalized before saving.", ref assertions);
                var normalized = GameSettings.Load(key);
                Require(normalized.MasterVolume == 1 && normalized.GraphicsPreset == 0 && normalized.Difficulty == FighterDifficulty.Regular,
                    "Non-finite volume and invalid options cannot escape into application settings.", ref assertions);
                string beforeFailure = PlayerPrefs.GetString(key);
                Require(!GameSettings.Save(null, key) && GameSettings.LastStorageError != null && PlayerPrefs.GetString(key) == beforeFailure,
                    "Failed saves return a diagnostic and retain the previous preference document.", ref assertions);
                Require(PlayerPrefs.HasKey(GameSettings.PreferenceKey) == hadUserSettings && PlayerPrefs.GetString(GameSettings.PreferenceKey, "") == userSettings,
                    "The acceptance suite never changes real user settings.", ref assertions);
                Debug.Log($"SETTINGS ACCEPTANCE PASSED: {assertions} assertions. Defaults, round-trip, clamping, schema fallback, malformed input and failed saves; unique temporary preference key only.");
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.DeleteKey(key+".axis");
                PlayerPrefs.Save();
            }
        }

        static void Require(bool condition, string message, ref int assertions)
        {
            if (!condition) throw new Exception("SETTINGS ACCEPTANCE FAILED: " + message);
            assertions++;
        }
    }
}
