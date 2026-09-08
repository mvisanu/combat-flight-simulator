using System.Collections.Generic;
using UnityEngine;

namespace PacificCombat
{
    // Ellipsoid optical depth matches the cloud volumes, independent of graphics quality.
    [ExecuteAlways]
    public sealed class EnvironmentVisibility : MonoBehaviour
    {
        static readonly List<EnvironmentVisibility> banks = new List<EnvironmentVisibility>(64);
        public Vector3 Radius = new Vector3(700, 250, 550);
        public float Extinction = .009f;
        void OnEnable() { banks.Add(this); }
        void OnDisable() { banks.Remove(this); }
        public static bool IsObscured(Vector3 from, Vector3 to)
        {
            float depth = 0;
            for (int i = 0; i < banks.Count; i++)
            {
                var bank = banks[i];
                Vector3 a = bank.transform.InverseTransformPoint(from), b = bank.transform.InverseTransformPoint(to);
                float localLength = Vector3.Distance(a,b);
                depth += SegmentLength(a, b, bank.Radius) / Mathf.Max(.00001f,localLength) * Vector3.Distance(from,to) * bank.Extinction;
                if (depth > 1.5f) return true;
            }
            return false;
        }
        public static float SegmentLength(Vector3 a, Vector3 b, Vector3 radius)
        {
            Vector3 p = new Vector3(a.x / radius.x, a.y / radius.y, a.z / radius.z);
            Vector3 d = new Vector3((b.x-a.x)/radius.x, (b.y-a.y)/radius.y, (b.z-a.z)/radius.z);
            float aa = Vector3.Dot(d,d), bb = 2*Vector3.Dot(p,d), cc = Vector3.Dot(p,p)-1;
            if (aa < .000001f) return 0;
            float disc = bb*bb-4*aa*cc;
            if (disc <= 0) return 0;
            float root = Mathf.Sqrt(disc);
            float start = Mathf.Clamp01((-bb-root)/(2*aa)), end = Mathf.Clamp01((-bb+root)/(2*aa));
            return Mathf.Max(0,end-start)*Vector3.Distance(a,b);
        }
    }
}
