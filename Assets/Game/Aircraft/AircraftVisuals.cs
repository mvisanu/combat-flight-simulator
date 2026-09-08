using UnityEngine;
namespace PacificCombat
{
    public sealed class AircraftVisuals : MonoBehaviour
    {
        public AircraftController Aircraft;
        public Transform Propeller;
        public Transform PropellerBlur;
        public Transform[] Propellers, PropellerBlurs, Rudders;
        public Transform[] Gear;
        public Transform[] Ailerons, Elevators, Flaps;
        public Transform Rudder;
        public Mesh[] GeneratedMeshes;
        public Transform DetailedAirframe;
        public Renderer LegacyCanopy;
        public CockpitInstruments Cockpit;
        public void SetCockpitView(bool visible)
        {
            if (Cockpit) Cockpit.SetVisible(visible);
            if (LegacyCanopy) LegacyCanopy.enabled = !DetailedAirframe && !visible;
        }
        void Update()
        {
            if (!Aircraft || !Aircraft.Engine) return;
            if (Propeller) Propeller.Rotate(0, 0, Aircraft.Engine.RPM * 6 * Time.deltaTime, Space.Self);
            bool fastPropeller = Aircraft.Engine.RPM > 700;
            if (Propeller && Propeller.gameObject.activeSelf == fastPropeller) Propeller.gameObject.SetActive(!fastPropeller);
            if (PropellerBlur && PropellerBlur.gameObject.activeSelf != fastPropeller) PropellerBlur.gameObject.SetActive(fastPropeller);
            if (Propellers != null) for (int i = 0; i < Propellers.Length; i++) if (Propellers[i])
            {
                Propellers[i].Rotate(0, 0, Aircraft.Engine.RPM * 6 * Time.deltaTime * (i == 0 ? 1 : -1), Space.Self);
                Propellers[i].gameObject.SetActive(!fastPropeller);
                if (PropellerBlurs != null && i < PropellerBlurs.Length && PropellerBlurs[i]) PropellerBlurs[i].gameObject.SetActive(fastPropeller);
            }
            if (Gear != null) for (int i = 0; i < Gear.Length; i++) if (Gear[i] && Gear[i].gameObject.activeSelf != Aircraft.Controls.Gear) Gear[i].gameObject.SetActive(Aircraft.Controls.Gear);
            float authority = Aircraft.ControlHealth;
            Animate(Ailerons, Aircraft.Controls.Roll * 20 * authority, true);
            Animate(Elevators, Aircraft.Controls.Pitch * 23 * authority, false);
            Animate(Flaps, Aircraft.Controls.Flaps ? -28 : 0, false);
            if (Rudder) Rudder.localRotation = Quaternion.Slerp(Rudder.localRotation, Quaternion.Euler(0, Aircraft.Controls.Yaw * 25 * authority, 0), 12 * Time.deltaTime);
            if (Rudders != null) for (int i = 0; i < Rudders.Length; i++) if (Rudders[i]) Rudders[i].localRotation = Quaternion.Slerp(Rudders[i].localRotation, Quaternion.Euler(0, Aircraft.Controls.Yaw * 25 * authority, 0), 12 * Time.deltaTime);
        }
        void Animate(Transform[] surfaces, float angle, bool opposite)
        {
            if (surfaces == null) return;
            for (int i = 0; i < surfaces.Length; i++) if (surfaces[i])
                surfaces[i].localRotation = Quaternion.Slerp(surfaces[i].localRotation, Quaternion.Euler(opposite && i == 0 ? -angle : angle, 0, 0), 12 * Time.deltaTime);
        }
        void OnDestroy()
        {
            if (GeneratedMeshes == null) return;
            for (int i = 0; i < GeneratedMeshes.Length; i++) if (GeneratedMeshes[i])
            {
                if (Application.isPlaying) Destroy(GeneratedMeshes[i]); else DestroyImmediate(GeneratedMeshes[i]);
            }
        }
    }
}
