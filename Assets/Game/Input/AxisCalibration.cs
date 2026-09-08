using System;
using UnityEngine;

namespace PacificCombat
{
    [Serializable]
    public sealed class AxisCalibration
    {
        public float Minimum = -1, Maximum = 1, Center, Deadzone = .04f, Response = 1;
        public bool Invert;
        public void Normalize()
        {
            if (!Finite(Minimum) || !Finite(Maximum) || Maximum - Minimum < .01f) { Minimum = -1; Maximum = 1; }
            if (!Finite(Center)) Center = (Minimum + Maximum) * .5f;
            Center = Mathf.Clamp(Center, Minimum + .001f, Maximum - .001f);
            Deadzone = Finite(Deadzone) ? Mathf.Clamp(Deadzone, 0, .4f) : .04f;
            Response = Finite(Response) ? Mathf.Clamp(Response, .5f, 3f) : 1f;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public float Signed(float raw)
        {
            if (!Finite(raw)) return 0;
            float normalized = raw >= Center ? (raw - Center) / Mathf.Max(.001f, Maximum - Center) : (raw - Center) / Mathf.Max(.001f, Center - Minimum);
            float magnitude = Mathf.Clamp01((Mathf.Abs(normalized) - Deadzone) / Mathf.Max(.01f, 1 - Deadzone));
            return Mathf.Pow(magnitude, Response) * Mathf.Sign(normalized) * (Invert ? -1 : 1);
        }
        public float Unsigned(float raw)
        {
            if (!Finite(raw)) return 0;
            float value = Mathf.InverseLerp(Minimum, Maximum, raw);
            return Invert ? 1 - value : value;
        }
    }

    [Serializable]
    public sealed class InputCalibrationData
    {
        public AxisCalibration Pitch = new AxisCalibration(), Roll = new AxisCalibration(), Yaw = new AxisCalibration(), Throttle = new AxisCalibration();
        public AxisCalibration Axis(int index) => index == 0 ? Pitch : index == 1 ? Roll : index == 2 ? Yaw : Throttle;
        public void Normalize()
        {
            if (Pitch == null) Pitch = new AxisCalibration(); if (Roll == null) Roll = new AxisCalibration();
            if (Yaw == null) Yaw = new AxisCalibration(); if (Throttle == null) Throttle = new AxisCalibration();
            Pitch.Normalize(); Roll.Normalize(); Yaw.Normalize(); Throttle.Normalize();
        }
    }

    public static class InputCalibrationStore
    {
        public const string Key = "PacificCombat.AxisCalibration.v1";
        public static InputCalibrationData Load(string key = Key)
        {
            var data = new InputCalibrationData();
            try { if (PlayerPrefs.HasKey(key)) JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(key), data); }
            catch (Exception) { data = new InputCalibrationData(); }
            data.Normalize(); return data;
        }
        public static bool Save(InputCalibrationData data, string key = Key)
        {
            try { data.Normalize(); PlayerPrefs.SetString(key, JsonUtility.ToJson(data)); PlayerPrefs.Save(); return true; }
            catch (Exception) { return false; }
        }
    }
}
