using UnityEngine;
using UnityEngine.InputSystem;

namespace PacificCombat
{
    public enum FlightCameraMode { Chase, Cockpit, Target, Flyby }
    public sealed class CameraController : MonoBehaviour
    {
        public FlightCameraMode Mode { get; private set; }
        public Camera Camera { get; private set; }
        MissionManager mission;
        float lookYaw, lookPitch;
        Vector3 flybyPosition;
        AircraftVisuals aircraftVisuals;
        public void Initialize(MissionManager owner) { mission = owner; Camera = GetComponent<Camera>(); aircraftVisuals = owner.Player.GetComponent<AircraftVisuals>(); Snap(); }
        public void Snap()
        {
            if (!mission || !mission.Player) return;
            var plane = mission.Player.transform;
            transform.position = plane.TransformPoint(new Vector3(0, 4.5f, -18));
            transform.rotation = Quaternion.LookRotation(plane.position + plane.forward * 30 - transform.position, plane.up);
            flybyPosition = plane.position + plane.forward * 300 + Vector3.right * 80;
        }
        public void Shift(Vector3 offset) { transform.position -= offset; flybyPosition -= offset; }
        public void SetMode(FlightCameraMode mode)
        {
            Mode = mode; lookYaw = lookPitch = 0;
            if (aircraftVisuals) aircraftVisuals.SetCockpitView(mode == FlightCameraMode.Cockpit);
            Snap();
        }
        void LateUpdate()
        {
            if (!mission || !mission.Player) return;
            if (mission.Input.CameraPressed) SetMode((FlightCameraMode)(((int)Mode + 1) % 4));
            if (mission.Input.CockpitPressed) SetMode(Mode == FlightCameraMode.Cockpit ? FlightCameraMode.Chase : FlightCameraMode.Cockpit);
            if (Mouse.current != null && Mouse.current.rightButton.isPressed && mission.State == MissionState.Flying)
            {
                var delta = Mouse.current.delta.ReadValue();
                lookYaw = Mathf.Clamp(lookYaw + delta.x * .12f, -160, 160);
                lookPitch = Mathf.Clamp(lookPitch - delta.y * .12f, -75, 75);
            }
            else { lookYaw = Mathf.Lerp(lookYaw, 0, Time.unscaledDeltaTime * 3); lookPitch = Mathf.Lerp(lookPitch, 0, Time.unscaledDeltaTime * 3); }
            var plane = mission.Player.transform;
            float follow = 1 - Mathf.Exp(-Time.unscaledDeltaTime * 12);
            Vector3 position;
            Quaternion rotation;
            if (Mode == FlightCameraMode.Cockpit)
            {
                position = plane.TransformPoint(CockpitInstruments.EyePosition);
                rotation = plane.rotation * Quaternion.Euler(10 + lookPitch, lookYaw, 0);
                Camera.fieldOfView = 80;
                Camera.nearClipPlane = .035f;
            }
            else if (Mode == FlightCameraMode.Flyby)
            {
                if (Vector3.Distance(flybyPosition, plane.position) > 1400) flybyPosition = plane.position + plane.forward * 350 + plane.right * 100;
                position = flybyPosition; rotation = Quaternion.LookRotation(plane.position - position, Vector3.up); Camera.fieldOfView = 50;
            }
            else
            {
                Quaternion orbit = plane.rotation * Quaternion.Euler(lookPitch, lookYaw, 0);
                position = plane.position + orbit * new Vector3(0, 4.5f, -18);
                Vector3 focus = Mode == FlightCameraMode.Target && mission.SelectedTarget ? mission.SelectedTarget.transform.position : plane.position + orbit * Vector3.forward * 45;
                rotation = Quaternion.LookRotation(focus - position, plane.up);
                Camera.fieldOfView = Mathf.Lerp(62, 73, mission.Player.Physics.Airspeed / 240);
            }
            transform.position = Mode == FlightCameraMode.Cockpit ? position : Vector3.Lerp(transform.position, position, follow);
            // A cockpit is rigidly attached to its airframe. Chase smoothing inside
            // the canopy causes the panel and windshield to swim during manoeuvres.
            transform.rotation = Mode == FlightCameraMode.Cockpit ? rotation : Quaternion.Slerp(transform.rotation, rotation, follow);
        }
    }
}
