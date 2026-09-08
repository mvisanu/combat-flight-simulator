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
        public AircraftCatalog Catalog;
        public MissionPreset Scenario;
        public WeatherPreset Weather = WeatherPreset.Scattered;
        public float StartingAltitude = 3048f;
        public float StartingSpeed = 111.76f;
        public float EnemyRange = 4000f;
        [Range(0, 16)] public int EnemyCount = 4;

        public void ConfigureAircraft(AircraftCatalog catalog, AircraftType player, AircraftType enemy)
        {
            if (!catalog) throw new System.ArgumentNullException(nameof(catalog));
            var playerEntry = catalog.Get(player);
            var enemyEntry = catalog.Get(enemy);
            Catalog = catalog;
            PlayerAircraft = playerEntry.Aircraft;
            PlayerWeapons = playerEntry.Weapons;
            EnemyAircraft = enemyEntry.Aircraft;
            EnemyWeapons = enemyEntry.Weapons;
        }
    }
}
