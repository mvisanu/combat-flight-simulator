using UnityEngine;

namespace PacificCombat
{
    [DefaultExecutionOrder(100)]
    public sealed class FloatingOriginSystem : MonoBehaviour
    {
        public float RecenterDistance = 8000;
        public Vector3 TotalOffset { get; private set; }
        MissionManager mission;
        Transform environment;
        AircraftWeaponSystem[] weapons;
        AircraftEffects[] effects;
        StructuralDebris[] debris;
        public void Initialize(MissionManager owner, Transform world)
        {
            mission = owner; environment = world;
            weapons = new AircraftWeaponSystem[owner.Enemies.Length + 1];
            effects = new AircraftEffects[weapons.Length];
            debris = new StructuralDebris[weapons.Length];
            debris[0] = owner.Player.GetComponent<StructuralDebris>();
            weapons[0] = owner.Weapons; effects[0] = owner.Player.GetComponent<AircraftEffects>();
            for (int i = 0; i < owner.Enemies.Length; i++) { weapons[i + 1] = owner.Enemies[i].GetComponent<AircraftWeaponSystem>(); effects[i + 1] = owner.Enemies[i].GetComponent<AircraftEffects>(); }
            for (int i = 0; i < owner.Enemies.Length; i++) debris[i + 1] = owner.Enemies[i].GetComponent<StructuralDebris>();
        }
        void FixedUpdate()
        {
            if (!mission || !mission.Player) return;
            Vector3 offset = mission.Player.Body.position; offset.y = 0;
            if (offset.sqrMagnitude < RecenterDistance * RecenterDistance) return;
            TotalOffset += offset;
            environment.position -= offset;
            mission.Player.Body.position -= offset;
            for (int i = 0; i < mission.Enemies.Length; i++)
            {
                mission.Enemies[i].Body.position -= offset;
                mission.Enemies[i].GetComponent<FighterAIController>().ShiftOrigin(offset);
            }
            for (int i = 0; i < weapons.Length; i++) { weapons[i].ShiftProjectiles(offset); effects[i].ShiftParticles(offset); }
            for (int i = 0; i < debris.Length; i++) if (debris[i]) debris[i].ShiftOrigin(offset);
            mission.FlightCamera.Shift(offset);
            Physics.SyncTransforms();
        }
    }
}
