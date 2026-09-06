using UnityEngine;

namespace PacificCombat
{
    [CreateAssetMenu(menuName = "Pacific Combat/Mission")]
    public sealed class MissionDefinition : ScriptableObject
    {
        public string MissionName = "Pacific Fighter Sweep";
        [TextArea] public string Description = "Intercept four Zero fighters. Keep your speed; avoid a prolonged turning fight.";
        public AircraftData PlayerAircraft;
        public AircraftData EnemyAircraft;
        public WeaponData PlayerWeapons;
        public WeaponData EnemyWeapons;
        public float StartingAltitude = 3048f;
        public float StartingSpeed = 111.76f;
        public float EnemyRange = 4000f;
        [Range(1, 16)] public int EnemyCount = 4;
    }
}
