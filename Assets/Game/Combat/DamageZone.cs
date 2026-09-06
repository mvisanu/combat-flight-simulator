using UnityEngine;

namespace PacificCombat
{
    public enum DamageZoneType
    {
        Engine, Cockpit, Pilot, FuelTank, LeftWing, RightWing, LeftAileron,
        RightAileron, Elevator, Rudder, HorizontalStabilizer, VerticalStabilizer, Fuselage
    }

    [DisallowMultipleComponent]
    public sealed class DamageZone : MonoBehaviour
    {
        public AircraftDamage Owner;
        public DamageZoneType Type = DamageZoneType.Fuselage;
        void Awake() { if (!Owner) Owner = GetComponentInParent<AircraftDamage>(); }
    }
}
