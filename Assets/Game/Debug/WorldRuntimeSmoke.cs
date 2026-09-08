using System.Collections;
using System.IO;
using UnityEngine;

namespace PacificCombat
{
    public sealed class WorldRuntimeSmoke : MonoBehaviour
    {
        static int phase;
        MissionManager mission;
        string folder;
        bool failed;
        public static void ConfigureSettings()
        {
            GameSettings.Current.Mission = (MissionPreset)phase;
            GameSettings.Current.Weather = (WeatherPreset)phase;
            GameSettings.Current.PlayerAircraft = phase == 3 ? AircraftType.P38Lightning : AircraftType.P51D;
        }
        public void Initialize(MissionManager owner) { mission=owner; StartCoroutine(Run()); }
        void Check(bool value,string message)
        {
            File.AppendAllText(Path.Combine(folder,"results.txt"),(value?"PASS ":"FAIL ")+message+"\n"); failed |= !value;
        }
        IEnumerator Capture(string label)
        {
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder,label+".png"));
            yield return new WaitForSecondsRealtime(.25f);
        }
        IEnumerator Run()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../WorldSmoke")); Directory.CreateDirectory(folder);
            if(phase==0) File.WriteAllText(Path.Combine(folder,"results.txt"),"Scenario and weather runtime verification\n");
            yield return null;
            Check(mission.Definition.Scenario==(MissionPreset)phase && mission.Definition.Weather==(WeatherPreset)phase,"Selected scenario/weather restored after reload "+phase);
            Check(mission.Enemies.Length==(phase==0?4:phase==1?6:0),"Correct opposition count");
            Check(!GameSettings.PersistenceEnabled,"Automation does not alter user preferences");
            yield return Capture("briefing-"+phase);
            var hud=mission.GetComponent<FlightHUD>(); hud.SetSettingsVisible(true); hud.SetSettingsPage(phase);
            yield return Capture("settings-"+phase); hud.SetSettingsVisible(false);
            mission.BeginMission(); mission.Input.enabled=false;
            if(phase==3)
            {
                // Explicit test spawn above the runway; actual touchdown/stop uses forces.
                mission.Player.Body.position=new Vector3(0,12,-4300);
                mission.Player.Body.rotation=Quaternion.identity;
                mission.Player.Body.linearVelocity=Vector3.forward*30;
                mission.Player.Controls=new FlightControls{Gear=true,Brakes=true};
            }
            yield return new WaitForSecondsRealtime(phase==3?18:5);
            if(phase==2) Check(mission.State==MissionState.Flying && !mission.SelectedTarget,"Free flight remains active without targets");
            if(phase==3) Check(mission.State==MissionState.Victory && !mission.Player.IsDestroyed,$"Physical runway touchdown and braking completes landing objective: {mission.State} position={mission.Player.Body.position} speed={mission.Player.Body.linearVelocity.magnitude:F1} gear={mission.Player.GroundHandling.GearHealth:F2}");
            mission.Pause(); hud.enabled=false; yield return Capture("world-"+phase); hud.enabled=true;
            Check(PacificEnvironment.WindVelocity(mission.Player.Body.position).magnitude>(phase==3?1:-1),"Weather wind query finite");
            if(failed) { Application.Quit(1); yield break; }
            phase++;
            if(phase==4) { File.AppendAllText(Path.Combine(folder,"results.txt"),"WORLD SMOKE PASSED\n"); Application.Quit(0); }
            else mission.MainMenu();
        }
    }
}
