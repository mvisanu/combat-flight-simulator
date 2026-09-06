using UnityEngine;

namespace PacificCombat
{
    [CreateAssetMenu(menuName = "Pacific Combat/Weapon configuration")]
    public sealed class WeaponData : ScriptableObject
    {
        public string WeaponName = "Six Browning .50 caliber guns";
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

        public static WeaponData CreateMustang() => CreateInstance<WeaponData>();
        public static WeaponData CreateZero()
        {
            var data = CreateInstance<WeaponData>();
            data.WeaponName = "Two 20 mm cannon and two 7.7 mm machine guns";
            data.GunCount = 4;
            data.Ammunition = 1320;
            data.RoundsPerMinutePerGun = 700;
            data.MixedCannon = true;
            data.MuzzleVelocity = 740;
            data.Damage = 7;
            return data;
        }
    }
}
