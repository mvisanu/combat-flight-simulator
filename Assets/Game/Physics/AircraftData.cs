using UnityEngine;

namespace PacificCombat
{
    [CreateAssetMenu(menuName = "Pacific Combat/Aircraft Data")]
    public sealed class AircraftData : ScriptableObject
    {
        public string AircraftName = "P-51D Mustang";
        public float Mass = 4200f;
        public float WingArea = 21.83f;
        public float WingSpan = 11.28f;
        public float EnginePower = 1119000f;
        public float MaximumThrust = 12500f;
        public float MaxRPM = 3000f;
        public float SuperchargerAltitude = 6000f;
        public float MaximumRecommendedSpeed = 195f;
        public float StallReferenceSpeed = 51f;
        public float MaximumLiftCoefficient = 1.5f;
        public float LiftSlope = 4.7f;
        public float TrimLiftCoefficient = .32f;
        public float StallAngle = 17f;
        public float BaseDragCoefficient = .023f;
        public float InducedDragCoefficient = .045f;
        public float PitchAuthority = 1.55f;
        public float RollAuthority = 4.1f;
        public float YawAuthority = .8f;
        public float MaximumStructuralG = 8.5f;
        public float MaximumSafeDiveSpeed = 235f;
        public float HighSpeedControlLoss = .3f;
        public float Durability = 1f;
        [Header("Fuel (litres; consumption in litres/hour)")]
        [Min(0)] public float FuelCapacity = 1020f;
        [Min(0)] public float IdleFuelConsumption = 45f;
        [Min(0)] public float FullPowerFuelConsumption = 380f;
        [Tooltip("Litres/second escaping from a fully ruptured tank.")]
        [Min(0)] public float MaximumFuelLeakRate = 6f;
        [Tooltip("Additional litres/second consumed by an active aircraft fire.")]
        [Min(0)] public float FireFuelConsumption = 2f;
        public float MaximumDiveSpeed => MaximumSafeDiveSpeed;

        public static AircraftData CreateMustang()
        {
            var data = CreateInstance<AircraftData>();
            data.name = "P51D_Data";
            return data;
        }

        public static AircraftData CreateZero()
        {
            var data = CreateInstance<AircraftData>();
            data.name = "A6MZero_Data";
            data.AircraftName = "A6M2 Zero";
            data.Mass = 2700f;
            data.WingArea = 22.44f;
            data.WingSpan = 12f;
            data.EnginePower = 708000f;
            data.MaximumThrust = 9000f;
            data.MaxRPM = 2550f;
            data.SuperchargerAltitude = 3800f;
            data.MaximumRecommendedSpeed = 150f;
            data.StallReferenceSpeed = 39f;
            data.MaximumLiftCoefficient = 1.7f;
            data.TrimLiftCoefficient = .205f;
            data.LiftSlope = 5.2f;
            data.StallAngle = 19f;
            data.BaseDragCoefficient = .031f;
            data.InducedDragCoefficient = .047f;
            data.PitchAuthority = 2.15f;
            data.RollAuthority = 3.6f;
            data.YawAuthority = 1.1f;
            data.MaximumStructuralG = 7f;
            data.MaximumSafeDiveSpeed = 185f;
            data.HighSpeedControlLoss = .75f;
            data.Durability = .63f;
            data.FuelCapacity = 520f;
            data.IdleFuelConsumption = 25f;
            data.FullPowerFuelConsumption = 260f;
            data.MaximumFuelLeakRate = 4f;
            return data;
        }
    }
}
