using System;
using UnityEngine;

namespace PacificCombat.Editor
{
    public static class WorldAcceptance
    {
        public static void Run()
        {
            var mission = ScriptableObject.CreateInstance<MissionDefinition>();
            var bank = new GameObject("Visibility fixture");
            try
            {
                foreach (MissionPreset preset in Enum.GetValues(typeof(MissionPreset)))
                {
                    ScenarioCatalog.Apply(mission,preset);
                    Require(!string.IsNullOrEmpty(mission.Description) && mission.StartingSpeed > 40,"Missing usable mission configuration");
                    Require(mission.EnemyCount == (preset == MissionPreset.Sweep ? 4 : preset == MissionPreset.Intercept ? 6 : 0),"Incorrect scenario opposition");
                }
                var cloud = bank.AddComponent<EnvironmentVisibility>();
                cloud.Radius = Vector3.one*.5f; bank.transform.localScale = new Vector3(1000,400,1000);
                Require(EnvironmentVisibility.IsObscured(new Vector3(-800,0,0),new Vector3(800,0,0)),"Dense cloud does not obscure track");
                Require(!EnvironmentVisibility.IsObscured(new Vector3(-800,300,0),new Vector3(800,300,0)),"Clear line above cloud incorrectly blocked");
                bank.transform.position = new Vector3(5000,0,-4000);
                Require(EnvironmentVisibility.IsObscured(new Vector3(4200,0,-4000),new Vector3(5800,0,-4000)),"Floating-origin cloud visibility drift");
                cloud.enabled = false;
                Require(!EnvironmentVisibility.IsObscured(new Vector3(4200,0,-4000),new Vector3(5800,0,-4000)),"Disabled cloud left stale visibility");
                var audio = Resources.Load<AudioClip>("Audio/MerlinCruise");
                Require(audio && audio.length > 5 && audio.channels == 1,"Recorded engine loop missing or invalid");
                Debug.Log("WORLD ACCEPTANCE PASSED: four scenario configurations, volumetric visibility and origin shift, recorded engine asset.");
            }
            finally { UnityEngine.Object.DestroyImmediate(mission); UnityEngine.Object.DestroyImmediate(bank); }
        }
        static void Require(bool condition,string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
