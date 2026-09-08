using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace PacificCombat
{
    /// <summary>Opt-in player regression exercising the real briefing selection and scene reload paths.</summary>
    public sealed class SelectionRuntimeSmokeTest : MonoBehaviour
    {
        static SelectionRuntimeSmokeTest instance;
        static int phase;
        static AircraftType expectedPlayer = AircraftType.P51D, expectedEnemy = AircraftType.A6MZero;
        static readonly AircraftType[] PlayerSequence = { AircraftType.A6MZero, AircraftType.Bf109, AircraftType.P38Lightning, AircraftType.P51D };
        static readonly AircraftType[] EnemySequence = { AircraftType.Bf109, AircraftType.P38Lightning, AircraftType.P51D, AircraftType.A6MZero };
        MissionManager mission;
        AircraftController previousPlayer;
        string results;
        string originalPreferences;
        bool originallyHadPreferences, finished;
        float started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            instance = null; phase = 0;
            expectedPlayer = AircraftType.P51D;
            expectedEnemy = AircraftType.A6MZero;
        }

        public static void InitializeMission(MissionManager owner)
        {
            if (!instance)
            {
                var monitor = new GameObject("Aircraft selection regression monitor");
                instance = monitor.AddComponent<SelectionRuntimeSmokeTest>();
                DontDestroyOnLoad(monitor);
                instance.started = Time.realtimeSinceStartup;
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SelectionSmoke"));
                Directory.CreateDirectory(directory);
                instance.results = Path.Combine(directory, "results.txt");
                File.WriteAllText(instance.results, "Aircraft selection runtime regression — ten actual scene reloads\n");
                instance.originallyHadPreferences = PlayerPrefs.HasKey(GameSettings.PreferenceKey);
                instance.originalPreferences = PlayerPrefs.GetString(GameSettings.PreferenceKey, "");
                Application.logMessageReceived += instance.OnLog;
            }
            if (instance.finished) return;
            instance.mission = owner;
            owner.Input.enabled = false;
            instance.StartCoroutine(instance.RunPhase());
        }

        IEnumerator RunPhase()
        {
            // Allow Mission.Start and the loaded scene to finish initialization.
            yield return null;
            try { VerifyAndAdvance(); }
            catch (Exception ex) { Finish(false, ex.Message + "\n" + ex.StackTrace); }
        }

        void VerifyAndAdvance()
        {
            if (finished) return;
            Require(phase >= 0 && phase <= 10, "Reload count is bounded to ten.");
            Require(mission && mission.Player && mission.Enemies != null, "Mission created its aircraft.");
            Require(!GameSettings.PersistenceEnabled, "Selection regression cannot save user preferences.");
            Require(PlayerPrefs.HasKey(GameSettings.PreferenceKey) == originallyHadPreferences
                && PlayerPrefs.GetString(GameSettings.PreferenceKey, "") == originalPreferences, "Real user settings remain unchanged.");
            Require(mission.Player.Data.Type == expectedPlayer && mission.Definition.PlayerAircraft.Type == expectedPlayer,
                "Player aircraft matches expected " + expectedPlayer + ".");
            Require(mission.Definition.EnemyAircraft.Type == expectedEnemy, "Enemy definition matches expected " + expectedEnemy + ".");
            Require(mission.Weapons.Configuration == mission.Catalog.Get(expectedPlayer).Weapons, "Player uses selected armament.");
            for (int i = 0; i < mission.Enemies.Length; i++)
            {
                Require(mission.Enemies[i].Data.Type == expectedEnemy, "Enemy " + i + " has selected flight data.");
                Require(mission.Enemies[i].GetComponent<AircraftWeaponSystem>().Configuration == mission.Catalog.Get(expectedEnemy).Weapons,
                    "Enemy " + i + " uses selected armament.");
            }
            Require(GameSettings.Current.PlayerAircraft == expectedPlayer && GameSettings.Current.EnemyAircraft == expectedEnemy,
                "Session aircraft choices survive scene reload.");
            Require(mission.State == (phase == 9 ? MissionState.Flying : MissionState.Briefing), "Reload returns to the expected mission state.");
            Require(!ReferenceEquals(previousPlayer, mission.Player), "Reload creates a fresh player aircraft instance.");
            Require(mission.Weapons.ShotsFired == 0 && mission.Weapons.Ammo == mission.Weapons.Configuration.Ammunition,
                "Reload resets weapons to a fresh loadout.");
            File.AppendAllText(results, $"PASS phase {phase:00}: {expectedPlayer} versus {expectedEnemy}; state {mission.State}; ammo {mission.Weapons.Ammo}; preferences unchanged\n");
            previousPlayer = mission.Player;

            if (phase < 4)
            {
                expectedPlayer = PlayerSequence[phase];
                phase++;
                mission.SelectAircraft(expectedPlayer, false);
            }
            else if (phase < 8)
            {
                expectedEnemy = EnemySequence[phase - 4];
                phase++;
                mission.SelectAircraft(expectedEnemy, true);
            }
            else if (phase == 8)
            {
                phase++;
                mission.Restart();
            }
            else if (phase == 9)
            {
                phase++;
                mission.MainMenu();
            }
            else Finish(true, "All four player selections, all four enemy selections, Restart and Main Menu used their real scene-reload paths.");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Phase " + phase + ": " + message);
        }

        void Update()
        {
            if (!finished && Time.realtimeSinceStartup - started > 180f)
                Finish(false, "Selection reload regression exceeded its 180-second timeout at phase " + phase + ".");
        }

        void OnLog(string message, string trace, LogType type)
        {
            if (!finished && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                Finish(false, message + "\n" + trace);
        }

        void Finish(bool success, string detail)
        {
            if (finished) return;
            finished = true;
            string summary = (success ? "SELECTION SMOKE PASSED" : "SELECTION SMOKE FAILED") + ": " + detail;
            try { File.AppendAllText(results, summary + "\n"); }
            catch (Exception ex) { success = false; Debug.LogWarning("Could not write selection regression results: " + ex.Message); }
            if (success) Debug.Log(summary); else Debug.LogError(summary);
            Application.Quit(success ? 0 : 1);
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            if (instance == this) instance = null;
        }
    }
}
