using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PacificCombat
{
    [RequireComponent(typeof(AircraftController))]
    public sealed class AircraftInput : MonoBehaviour
    {
        public bool MouseFlight;
        public bool ShowBindings;
        public bool CameraPressed => Pressed("Camera");
        public bool CockpitPressed => Pressed("Cockpit");
        public bool TargetPressed => Pressed("Target");
        public bool RestartPressed => Pressed("Restart");
        public bool PausePressed => Pressed("Pause");
        public string RebindingLabel { get; private set; }
        public InputActionAsset Actions { get; private set; }
        public event Action BindingsChanged;
        AircraftController aircraft;
        InputActionMap flight;
        InputAction pitch, roll, yaw, throttle, hardwareThrottle, fire, flaps, gear, brakes;
        InputActionRebindingExtensions.RebindingOperation rebind;
        Vector2 bindingScroll;
        const string PreferencesKey = "PacificCombat.InputBindings.v1";

        void Awake() => Initialize();

        public void Initialize()
        {
            if (Actions != null) return;
            aircraft = GetComponent<AircraftController>();
            Actions = ScriptableObject.CreateInstance<InputActionAsset>();
            flight = new InputActionMap("Flight");
            Actions.AddActionMap(flight);
            pitch = Axis("Pitch", "<Keyboard>/w", "<Keyboard>/s", "<Gamepad>/leftStick/y", "<Joystick>/stick/y");
            roll = Axis("Roll", "<Keyboard>/a", "<Keyboard>/d", "<Gamepad>/leftStick/x", "<Joystick>/stick/x");
            yaw = Axis("Yaw", "<Keyboard>/q", "<Keyboard>/e", "<Gamepad>/rightStick/x", "<Joystick>/twist");
            throttle = Axis("Throttle", "<Keyboard>/leftCtrl", "<Keyboard>/leftShift", null, null);
            throttle.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/leftShoulder").With("Positive", "<Gamepad>/rightShoulder");
            hardwareThrottle = flight.AddAction("HardwareThrottle", InputActionType.Value);
            hardwareThrottle.expectedControlType = "Axis";
            hardwareThrottle.AddBinding("<Joystick>/throttle");
            fire = Button("Fire", "<Keyboard>/space", "<Gamepad>/rightTrigger", "<Joystick>/trigger");
            fire.AddBinding("<Mouse>/leftButton");
            flaps = Button("Flaps", "<Keyboard>/f", "<Gamepad>/buttonWest");
            gear = Button("Gear", "<Keyboard>/g", "<Gamepad>/dpad/down");
            brakes = Button("Brakes", "<Keyboard>/b", "<Gamepad>/buttonEast");
            Button("Camera", "<Keyboard>/c", "<Gamepad>/rightStickPress");
            Button("Cockpit", "<Keyboard>/v", "<Gamepad>/leftStickPress");
            Button("Target", "<Keyboard>/tab", "<Gamepad>/buttonNorth");
            Button("Restart", "<Keyboard>/r");
            Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            Button("MouseFlight", "<Keyboard>/m");
            if (PlayerPrefs.HasKey(PreferencesKey))
            {
                try { Actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PreferencesKey)); }
                catch (Exception ex) { Debug.LogWarning("Ignoring incompatible input bindings: " + ex.Message); }
            }
        }

        InputAction Axis(string label, string negative, string positive, string gamepad, string joystick)
        {
            var action = flight.AddAction(label, InputActionType.Value);
            action.expectedControlType = "Axis";
            action.AddCompositeBinding("1DAxis").With("Negative", negative).With("Positive", positive);
            if (gamepad != null) action.AddBinding(gamepad).WithProcessor("axisDeadzone(min=0.08,max=0.95)");
            if (joystick != null) action.AddBinding(joystick).WithProcessor("axisDeadzone(min=0.04,max=0.98)");
            return action;
        }

        InputAction Button(string label, string keyboard, string gamepad = null, string joystick = null)
        {
            var action = flight.AddAction(label, InputActionType.Button, keyboard);
            if (gamepad != null) action.AddBinding(gamepad);
            if (joystick != null) action.AddBinding(joystick);
            return action;
        }

        bool Pressed(string action) => flight != null && flight.FindAction(action).WasPressedThisFrame();
        void OnEnable() { Actions?.Enable(); }
        void OnDisable() { rebind?.Cancel(); Actions?.Disable(); }
        void OnDestroy()
        {
            rebind?.Dispose();
            if (Actions != null) { if (Application.isPlaying) Destroy(Actions); else DestroyImmediate(Actions); }
        }

        void Update()
        {
            if (!aircraft.IsPlayer || aircraft.IsDestroyed || Time.timeScale == 0f || rebind != null) return;
            var controls = aircraft.Controls;
            controls.Pitch = pitch.ReadValue<float>();
            controls.Roll = roll.ReadValue<float>();
            controls.Yaw = yaw.ReadValue<float>();
            controls.Throttle = Mathf.Clamp01(controls.Throttle + throttle.ReadValue<float>() * Time.deltaTime * .3f);
            // Only consume hardware throttle after that axis is moved; attaching an idle joystick must not cut the engine.
            if (hardwareThrottle.activeControl != null)
                controls.Throttle = Mathf.Clamp01((hardwareThrottle.ReadValue<float>() + 1f) * .5f);
            if (Pressed("MouseFlight")) MouseFlight = !MouseFlight;
            if (MouseFlight && Mouse.current != null)
            {
                Vector2 position = Mouse.current.position.ReadValue();
                controls.Roll = Mathf.Clamp((position.x - Screen.width * .5f) / (Screen.width * .3f), -1f, 1f);
                controls.Pitch = Mathf.Clamp((position.y - Screen.height * .5f) / (Screen.height * .3f), -1f, 1f);
            }
            controls.Fire = fire.IsPressed();
            controls.Brakes = brakes.IsPressed();
            if (flaps.WasPressedThisFrame()) controls.Flaps = !controls.Flaps;
            if (gear.WasPressedThisFrame()) controls.Gear = !controls.Gear;
            aircraft.Controls = controls;
        }

        public void BeginRebind(string actionName, int bindingIndex)
        {
            var action = flight.FindAction(actionName);
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count || action.bindings[bindingIndex].isComposite) return;
            rebind?.Cancel();
            action.Disable();
            RebindingLabel = actionName + " — move an axis or press a key (Escape cancels)";
            rebind = action.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .OnCancel(operation => FinishRebind(action, operation, false))
                .OnComplete(operation => FinishRebind(action, operation, true));
            rebind.Start();
        }

        void FinishRebind(InputAction action, InputActionRebindingExtensions.RebindingOperation operation, bool save)
        {
            operation.Dispose();
            rebind = null;
            RebindingLabel = null;
            if (isActiveAndEnabled) action.Enable();
            if (save)
            {
                PlayerPrefs.SetString(PreferencesKey, Actions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();
                BindingsChanged?.Invoke();
            }
        }

        public void ResetBindings()
        {
            rebind?.Cancel();
            Actions.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PreferencesKey);
            BindingsChanged?.Invoke();
        }

        void OnGUI()
        {
            if (!ShowBindings || flight == null) return;
            GUILayout.BeginArea(new Rect(Screen.width * .5f - 280, 60, 560, Screen.height - 120), GUI.skin.box);
            GUILayout.Label("FLIGHT CONTROLS — select a binding to change it");
            if (RebindingLabel != null) GUILayout.Label(RebindingLabel);
            bindingScroll = GUILayout.BeginScrollView(bindingScroll);
            foreach (var action in flight.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    if (binding.isComposite) continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(action.name + (binding.isPartOfComposite ? " " + binding.name : ""), GUILayout.Width(175));
                    if (GUILayout.Button(action.GetBindingDisplayString(i))) BeginRebind(action.name, i);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore defaults")) ResetBindings();
            if (GUILayout.Button("Close")) { rebind?.Cancel(); ShowBindings = false; }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
