using UnityEngine;

namespace PacificCombat
{
    [CreateAssetMenu(menuName = "Pacific Combat/Weapon configuration")]
    public sealed class WeaponData : ScriptableObject
    {
        public string WeaponName = "Six Browning .50 caliber guns";
        public string DisplayLabel = ".50 CAL";
        [Min(1)] public int GunCount = 6;
        [Min(1)] public int Ammunition = 1880;
        [Min(1)] public float RoundsPerMinutePerGun = 750;
        [Min(1)] public float MuzzleVelocity = 890;
        [Min(1)] public float Range = 1800;
        [Min(0)] public float Damage = 11;
        [Range(0, 2)] public float SpreadDegrees = .2f;
        [Min(20)] public float Convergence = 300;
        [Min(1)] public int TracerEvery = 4;
        public bool MixedCannon;
        [Min(0)] public int CannonCount = 2;
        [Min(0)] public int CannonAmmunition = 120;
        [Min(1)] public float CannonMuzzleVelocity = 600;
        [Min(1)] public float CannonRoundsPerMinute = 520;
        [Min(0)] public float CannonDamage = 30;
        public Vector3[] MuzzlePositions;

        public static WeaponData CreateMustang() => CreateInstance<WeaponData>();
        public static WeaponData CreateZero()
        {
            var data = CreateInstance<WeaponData>();
            data.WeaponName = "Two 20 mm cannon and two 7.7 mm machine guns";
            data.DisplayLabel = "20 MM + 7.7 MM";
            data.GunCount = 4;
            data.Ammunition = 1320;
            data.RoundsPerMinutePerGun = 700;
            data.MixedCannon = true;
            data.MuzzleVelocity = 740;
            data.Damage = 7;
            data.MuzzlePositions = new[] { new Vector3(-2.5f, -.05f, .8f), new Vector3(2.5f, -.05f, .8f), new Vector3(-.36f, .3f, 2.8f), new Vector3(.36f, .3f, 2.8f) };
            return data;
        }
        public static WeaponData CreateBf109()
        {
            var data = CreateInstance<WeaponData>();
            data.WeaponName = "Hub 20 mm cannon and two 13 mm machine guns"; data.DisplayLabel = "20 MM + 13 MM";
            data.GunCount = 3; data.Ammunition = 800; data.RoundsPerMinutePerGun = 900;
            data.MuzzleVelocity = 750; data.Damage = 10; data.MixedCannon = true;
            data.CannonCount = 1; data.CannonAmmunition = 200; data.CannonMuzzleVelocity = 780; data.CannonRoundsPerMinute = 650;
            data.MuzzlePositions = new[] { new Vector3(0, .05f, 4.15f), new Vector3(-.29f, .5f, 2.9f), new Vector3(.29f, .5f, 2.9f) };
            return data;
        }
        public static WeaponData CreateLightning()
        {
            var data = CreateInstance<WeaponData>();
            data.WeaponName = "Nose 20 mm cannon and four .50 caliber machine guns"; data.DisplayLabel = "20 MM + .50 CAL";
            data.GunCount = 5; data.Ammunition = 2300; data.MixedCannon = true;
            data.CannonCount = 1; data.CannonAmmunition = 150; data.CannonMuzzleVelocity = 850; data.CannonRoundsPerMinute = 600;
            data.MuzzlePositions = new[] { new Vector3(0, -.18f, 3.65f), new Vector3(-.2f, .05f, 3.5f), new Vector3(.2f, .05f, 3.5f), new Vector3(-.18f, .3f, 3.4f), new Vector3(.18f, .3f, 3.4f) };
            return data;
        }
        public static WeaponData ForAircraft(AircraftType type)
        {
            switch (type)
            {
                case AircraftType.A6MZero: return CreateZero();
                case AircraftType.Bf109: return CreateBf109();
                case AircraftType.P38Lightning: return CreateLightning();
                default: return CreateMustang();
            }
        }
    }
}
