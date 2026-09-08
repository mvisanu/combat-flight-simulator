using UnityEngine;

namespace PacificCombat
{
    public enum AircraftType { P51D, A6MZero, Bf109, P38Lightning }
    [CreateAssetMenu(menuName = "Pacific Combat/Aircraft Data")]
    public sealed class AircraftData : ScriptableObject
    {
        public string AircraftName = "P-51D Mustang";
        public AircraftType Type = AircraftType.P51D;
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
        [Header("Ground handling and engine temperatures")]
        [Min(1)] public float MaximumLandingSinkSpeed = 3.5f;
        public float CoolantLimitC = 125;
        public float CriticalEngineTemperatureC = 150;
        public float CoolingTimeSeconds = 70;

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
            data.Type = AircraftType.A6MZero;
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

        // Playable Bf 109 G-6 and P-38J archetypes; these are tunable simulation
        // configurations rather than a claim of certified historical performance.
        public static AircraftData CreateBf109()
        {
            var data = CreateInstance<AircraftData>();
            data.name = "Bf109G6_Data"; data.Type = AircraftType.Bf109; data.AircraftName = "Bf 109 G-6";
            data.Mass = 3200; data.WingArea = 16.05f; data.WingSpan = 9.9f;
            data.EnginePower = 1100000; data.MaximumThrust = 11200; data.MaxRPM = 2800;
            data.SuperchargerAltitude = 5700; data.MaximumRecommendedSpeed = 181;
            data.StallReferenceSpeed = 47; data.MaximumLiftCoefficient = 1.65f; data.TrimLiftCoefficient = .31f;
            data.LiftSlope = 5; data.StallAngle = 18; data.BaseDragCoefficient = .027f; data.InducedDragCoefficient = .047f;
            data.PitchAuthority = 1.8f; data.RollAuthority = 3.6f; data.YawAuthority = .9f;
            data.MaximumStructuralG = 8; data.MaximumSafeDiveSpeed = 220; data.HighSpeedControlLoss = .48f;
            data.Durability = .86f; data.FuelCapacity = 400; data.IdleFuelConsumption = 30; data.FullPowerFuelConsumption = 320;
            data.MaximumFuelLeakRate = 4;
            return data;
        }

        public static AircraftData CreateLightning()
        {
            var data = CreateInstance<AircraftData>();
            data.name = "P38J_Data"; data.Type = AircraftType.P38Lightning; data.AircraftName = "P-38J Lightning";
            data.Mass = 7800; data.WingArea = 30.4f; data.WingSpan = 15.8f;
            data.EnginePower = 2100000; data.MaximumThrust = 22800; data.MaxRPM = 3000;
            data.SuperchargerAltitude = 7600; data.MaximumRecommendedSpeed = 190;
            data.StallReferenceSpeed = 53; data.MaximumLiftCoefficient = 1.65f; data.TrimLiftCoefficient = .4f;
            data.LiftSlope = 5.05f; data.StallAngle = 17; data.BaseDragCoefficient = .026f; data.InducedDragCoefficient = .047f;
            data.PitchAuthority = 1.4f; data.RollAuthority = 2.9f; data.YawAuthority = .72f;
            data.MaximumStructuralG = 7.5f; data.MaximumSafeDiveSpeed = 225; data.HighSpeedControlLoss = .62f;
            data.Durability = 1.35f; data.FuelCapacity = 1500; data.IdleFuelConsumption = 80; data.FullPowerFuelConsumption = 680;
            data.MaximumFuelLeakRate = 8; data.FireFuelConsumption = 3;
            return data;
        }
    }
}
