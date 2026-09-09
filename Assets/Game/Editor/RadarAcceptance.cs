using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PacificCombat.Editor
{
    public static class RadarAcceptance
    {
        public static void Run()
        {
            var player = Fixture("Radar player",0,Vector3.zero);
            var friend = Fixture("Radar friend",0,new Vector3(0,9000,1000));
            var foe = Fixture("Radar foe",1,new Vector3(0,0,1000));
            var dead = Fixture("Radar wreck",1,new Vector3(50,0,1000)); dead.IsDestroyed=true;
            var inactive = Fixture("Radar inactive",1,new Vector3(100,0,1000)); inactive.gameObject.SetActive(false);
            var outside = Fixture("Radar distant",1,new Vector3(0,0,20000));
            var sources = new[] { player,friend,foe,dead,inactive,outside };
            try
            {
                var radar = new RadarScope(); radar.Read(player,foe,sources);
                Require(radar.Count==2 && radar.Friends==1 && radar.Foes==1,"Teams determine friend/foe; self, wrecks, inactive and distant contacts excluded");
                Require(radar.Contacts[0].Selected && !radar.Contacts[0].Friendly,"Selected enemy retains hostile identity");
                Require(radar.Contacts[1].Friendly,"Friendly aircraft counted despite altitude separation");
                Require((radar.Contacts[0].IconPosition-radar.Contacts[1].IconPosition).magnitude>=33.9f,"Coincident contacts remain individually readable");
                Require(radar.Contacts[0].Position==radar.Contacts[1].Position,"Decluttering preserves true anchor positions");
                foreach (var delta in new[] {Vector3.forward*1000,Vector3.right*1000,Vector3.back*1000,Vector3.left*1000})
                {
                    Require(RadarScope.Project(delta,Vector3.forward,1000,out var point),"Exact range boundary included");
                    Require(Mathf.Abs(point.magnitude-RadarScope.Radius)<.01f,"Boundary maps to outer range ring");
                }
                RadarScope.Project(Vector3.right*1000,Vector3.right,2000,out var east);
                Require(Mathf.Abs(east.x)<.01f && east.y<0,"Eastward heading puts an eastern contact ahead");
                Require(!RadarScope.Project(new Vector3(float.NaN,0,0),Vector3.forward,1000,out _),"Nonfinite contacts excluded");
                friend.Team=1; radar.Read(player,friend,sources);
                Require(radar.Friends==0 && radar.Foes==2,"Team changes do not depend on aircraft model");
                foe.transform.position=new Vector3(0,0,5000); friend.IsDestroyed=true;
                radar.SetRange(0); radar.Read(player,foe,sources); Require(radar.Count==0,"Two-mile range and empty state");
                radar.SetRange(1); radar.Read(player,foe,sources); Require(radar.Foes==1,"Five-mile range includes the same contact");
                radar.SetRange(2); radar.Read(player,foe,sources); Require(radar.Foes==1,"Ten-mile range still excludes distant contact");
                player.transform.rotation=Quaternion.Euler(-90,0,0); radar.Read(player,foe,sources);
                Require(float.IsFinite(radar.Contacts[0].Position.x),"Vertical flight retains a finite last heading");
                Debug.Log("RADAR ACCEPTANCE PASSED: teams, live contacts, heading, horizontal range, crowding, target identity and empty state.");
            }
            finally { foreach (var aircraft in sources) Object.DestroyImmediate(aircraft.gameObject); }
        }
        static AircraftController Fixture(string name,int team,Vector3 position)
        {
            var obj = new GameObject(name); obj.transform.position=position;
            var aircraft=obj.AddComponent<AircraftController>(); aircraft.Team=team;
            aircraft.GetComponent<Rigidbody>().isKinematic=true;return aircraft;
        }
        static void Require(bool condition,string message)
        { if(!condition) throw new Exception("RADAR ACCEPTANCE FAILED: "+message); }
    }
}
