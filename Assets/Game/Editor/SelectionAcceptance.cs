using System;
using UnityEditor;
using UnityEngine;

namespace PacificCombat.Editor
{
    public static class SelectionAcceptance
    {
        [MenuItem("Pacific Combat/Run Aircraft Selection Acceptance")]
        public static void Run()
        {
            int assertions = 0;
            string key = "PacificCombat.Tests.Selection." + Guid.NewGuid().ToString("N");
            bool hadUserSettings = PlayerPrefs.HasKey(GameSettings.PreferenceKey);
            string originalUserSettings = PlayerPrefs.GetString(GameSettings.PreferenceKey, "");
            MissionDefinition template = null;
            try
            {
                var catalog = Resources.Load<AircraftCatalog>(AircraftCatalog.ResourcePath);
                Require(catalog && catalog.Entries.Length == 4, "The built resource catalog contains all four aircraft.", ref assertions);
                string[] expectedModels = { "Art/P51D", "Art/A6MZero", "Art/Bf109", "Art/P38Lightning" };
                for (int i = 0; i < 4; i++)
                {
                    var entry = catalog.Get((AircraftType)i);
                    Require(entry.Aircraft.Type == (AircraftType)i && entry.Weapons && !string.IsNullOrWhiteSpace(entry.Weapons.DisplayLabel),
                        "Each aircraft has explicitly matching flight data and a labeled weapon configuration.", ref assertions);
                    Require(entry.ModelResourcePath == expectedModels[i] && Resources.Load<GameObject>(entry.ModelResourcePath),
                        "Each catalog choice resolves to its authored model resource: " + entry.ModelResourcePath, ref assertions);
                }

                template = ScriptableObject.CreateInstance<MissionDefinition>();
                template.StartingAltitude = 3210f;
                template.ConfigureAircraft(catalog, AircraftType.P51D, AircraftType.A6MZero);
                for (int player = 0; player < 4; player++) for (int enemy = 0; enemy < 4; enemy++)
                {
                    var mission = UnityEngine.Object.Instantiate(template);
                    try
                    {
                        mission.ConfigureAircraft(catalog, (AircraftType)player, (AircraftType)enemy);
                        Require(mission.PlayerAircraft.Type == (AircraftType)player && mission.EnemyAircraft.Type == (AircraftType)enemy
                            && mission.PlayerWeapons == catalog.Get((AircraftType)player).Weapons && mission.EnemyWeapons == catalog.Get((AircraftType)enemy).Weapons,
                            "Both roles receive the correct airframe and armament for matchup " + player + "/" + enemy + ".", ref assertions);
                        Require(mission.StartingAltitude == 3210f && template.PlayerAircraft.Type == AircraftType.P51D && template.EnemyAircraft.Type == AircraftType.A6MZero,
                            "Selection preserves mission parameters and never modifies the authored mission template.", ref assertions);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(mission); }
                }

                var defaults = GameSettings.Load(key);
                Require(defaults.PlayerAircraft == AircraftType.P51D && defaults.EnemyAircraft == AircraftType.A6MZero,
                    "A new user retains the original Mustang-versus-Zero matchup.", ref assertions);
                var selected = new GameSettingsData { PlayerAircraft = AircraftType.P38Lightning, EnemyAircraft = AircraftType.Bf109 };
                Require(GameSettings.Save(selected, key), "Aircraft choices save successfully.", ref assertions);
                var restored = GameSettings.Load(key);
                var restarted = UnityEngine.Object.Instantiate(template);
                try
                {
                    restarted.ConfigureAircraft(catalog, restored.PlayerAircraft, restored.EnemyAircraft);
                    Require(restarted.PlayerAircraft.Type == AircraftType.P38Lightning && restarted.EnemyAircraft.Type == AircraftType.Bf109,
                        "A fresh mission after preference reload recreates both selected aircraft types.", ref assertions);
                }
                finally { UnityEngine.Object.DestroyImmediate(restarted); }
                PlayerPrefs.SetString(key, "{\"PlayerAircraft\":99,\"EnemyAircraft\":-1}");
                var invalid = GameSettings.Load(key);
                Require(invalid.PlayerAircraft == AircraftType.P51D && invalid.EnemyAircraft == AircraftType.A6MZero,
                    "Invalid persisted aircraft identifiers fall back safely.", ref assertions);
                PlayerPrefs.SetString(key, "{\"Version\":1,\"MasterVolume\":0.5}");
                var legacy = GameSettings.Load(key);
                Require(legacy.PlayerAircraft == AircraftType.P51D && legacy.EnemyAircraft == AircraftType.A6MZero && legacy.MasterVolume == .5f,
                    "Existing version-one preferences migrate without resetting unrelated settings.", ref assertions);

                string[] codes = { "p51", "zero", "bf109", "p38" };
                for (int i = 0; i < codes.Length; i++)
                    Require(AircraftCatalog.TryParseCode(codes[i], out var parsed) && parsed == (AircraftType)i,
                        "Documented command-line aircraft code resolves: " + codes[i], ref assertions);
                Require(!AircraftCatalog.TryParseCode("spitfire", out _) && !AircraftCatalog.TryParseCode("", out _),
                    "Unknown and empty aircraft codes are rejected.", ref assertions);
                AircraftCatalog.ApplyCommandLine(defaults, new[] { "--player=p38", "--ENEMY=BF109", "--player=zero", "--enemies=8" });
                Require(defaults.PlayerAircraft == AircraftType.A6MZero && defaults.EnemyAircraft == AircraftType.Bf109,
                    "Command-line selection is case-insensitive, role-specific and honors the last valid override.", ref assertions);
                Require(PlayerPrefs.HasKey(GameSettings.PreferenceKey) == hadUserSettings && PlayerPrefs.GetString(GameSettings.PreferenceKey, "") == originalUserSettings,
                    "Selection acceptance never modifies real user preferences.", ref assertions);
                Debug.Log($"SELECTION ACCEPTANCE PASSED: {assertions} assertions; all 16 matchup mappings, authored models, immutable mission templates, persisted mission recreation, legacy/invalid preferences and CLI overrides.");
            }
            finally
            {
                if (template) UnityEngine.Object.DestroyImmediate(template);
                PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
            }
        }

        static void Require(bool condition, string message, ref int assertions)
        {
            if (!condition) throw new Exception("SELECTION ACCEPTANCE FAILED: " + message);
            assertions++;
        }
    }
}
