using UnityEngine;

namespace PacificCombat
{
    public enum MissionPreset { Sweep, Intercept, FreeFlight, LandingPractice }
    public enum WeatherPreset { Clear, Scattered, Overcast, Squall }

    public static class ScenarioCatalog
    {
        public static void Apply(MissionDefinition mission, MissionPreset preset)
        {
            mission.Scenario = preset;
            mission.StartingAltitude = 3048; mission.StartingSpeed = 111.76f;
            mission.EnemyCount = 4; mission.EnemyRange = 4000;
            switch (preset)
            {
                case MissionPreset.Intercept:
                    mission.MissionName = "Island Intercept";
                    mission.Description = "Intercept six fighters approaching from the east. Climb to meet them and defend the island airspace.";
                    mission.EnemyCount = 6; mission.EnemyRange = 6500; mission.StartingAltitude = 2200;
                    break;
                case MissionPreset.FreeFlight:
                    mission.MissionName = "Pacific Free Flight";
                    mission.Description = "Explore the islands without opponents. Practice handling, stalls and recovery; return to the menu whenever you are ready.";
                    mission.EnemyCount = 0;
                    break;
                case MissionPreset.LandingPractice:
                    mission.MissionName = "Island Landing Practice";
                    mission.Description = "Runway 36 is straight ahead. Lower gear and flaps, manage your descent, then brake to a stop on the runway.";
                    mission.EnemyCount = 0; mission.StartingAltitude = 220; mission.StartingSpeed = 75;
                    break;
                default:
                    mission.MissionName = "Pacific Fighter Sweep";
                    mission.Description = "Destroy the opposing fighters. Keep your energy and choose your attack passes carefully.";
                    break;
            }
        }
    }
}
