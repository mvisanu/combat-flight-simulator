using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PacificCombat
{
    /// <summary>Opt-in mixed-team radar fixture; never creates contacts in normal missions.</summary>
    public static class RadarRuntimeSmoke
    {
        public static IEnumerator Run(MissionManager mission,Func<string,IEnumerator> capture,Action<bool,string> require)
        {
            var hud=mission.GetComponent<FlightHUD>();
            float previousTimeScale=Time.timeScale;
            var originals=new Vector3[mission.Enemies.Length];
            for(int i=0;i<originals.Length;i++) originals[i]=mission.Enemies[i].transform.position;
            var fixtures=new AircraftController[3];
            int previousRange=hud.Radar.RangeIndex;
            try
            {
                Time.timeScale=0;
                for(int i=0;i<fixtures.Length;i++)
                {
                    var obj=new GameObject("Radar smoke fixture "+i);
                    obj.transform.position=mission.Player.transform.position+new Vector3(i<2?-1000:1000,0,1000+i*30);
                    fixtures[i]=obj.AddComponent<AircraftController>();
                    fixtures[i].Team=i<2?mission.Player.Team:mission.Player.Team+1;
                    fixtures[i].GetComponent<Rigidbody>().isKinematic=true;
                }
                for(int range=0;range<3;range++)
                {
                    hud.Radar.SetRange(range); yield return new WaitForSecondsRealtime(1.1f);
                    require(hud.Radar.Friends==2 && hud.Radar.Foes>=1,"Radar mixed teams at "+hud.Radar.Ranges[range]+" mi");
                    yield return capture("radar-"+hud.Radar.Ranges[range]+"mi.png");
                }
                foreach(var fixture in fixtures) fixture.gameObject.SetActive(false);
                foreach(var enemy in mission.Enemies) enemy.transform.position=mission.Player.transform.position+new Vector3(50000,0,50000);
                yield return new WaitForSecondsRealtime(.25f);
                require(hud.Radar.Count==0 && hud.Radar.Nearest=="No foes in range","Radar empty state clears stale contacts");
                yield return capture("radar-empty.png");
            }
            finally
            {
                foreach(var fixture in fixtures) if(fixture) Object.Destroy(fixture.gameObject);
                for(int i=0;i<originals.Length;i++) mission.Enemies[i].transform.position=originals[i];
                hud.Radar.SetRange(previousRange);Time.timeScale=previousTimeScale;
            }
        }
    }
}
