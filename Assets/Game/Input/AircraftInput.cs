using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Collections.Generic;

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
        GUISkin controlsSkin;
        GUIStyle controlsHeading;
        const string PreferencesKey = "PacificCombat.InputBindings.v1";
        public InputCalibrationData Calibration { get; private set; }
        readonly AxisControl[] lastHardware = new AxisControl[4];
        readonly List<BindingRow> bindingRows = new List<BindingRow>();
        static readonly string[] AxisNames = { "Pitch", "Roll", "Yaw", "Throttle" };
        static readonly string[] SetupTabs = { "Bindings", "Device setup" };
        static readonly GUILayoutOption[] LabelWidth = { GUILayout.Width(290) };
        struct BindingRow { public string Action, Label, Display; public int Index; }
        bool deviceSetup, capturing;
        int setupAxis;
        float captureMin, captureMax, nextDeviceRead;
        string deviceSummary, axisReadout, calibrationStatus = "Bind the desired device first. Calibration affects analog axes only.";

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
            Calibration = GameSettings.PersistenceEnabled ? InputCalibrationStore.Load() : new InputCalibrationData();
            Calibration.Normalize();
            RefreshBindingLabels();
        }

        InputAction Axis(string label, string negative, string positive, string gamepad, string joystick)
        {
            var action = flight.AddAction(label, InputActionType.Value);
            action.expectedControlType = "Axis";
            action.AddCompositeBinding("1DAxis").With("Negative", negative).With("Positive", positive);
            if (gamepad != null) action.AddBinding(gamepad);
            if (joystick != null) action.AddBinding(joystick);
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
            if (controlsSkin) Destroy(controlsSkin);
            if (Actions != null) { if (Application.isPlaying) Destroy(Actions); else DestroyImmediate(Actions); }
        }

        void Update()
        {
            if (ShowBindings) UpdateDeviceSetup();
            if (!aircraft.IsPlayer || aircraft.IsDestroyed || Time.timeScale == 0f || rebind != null) return;
            var controls = aircraft.Controls;
            controls.Pitch = ReadAxis(pitch, 0);
            controls.Roll = ReadAxis(roll, 1);
            controls.Yaw = ReadAxis(yaw, 2);
            controls.Throttle = Mathf.Clamp01(controls.Throttle + throttle.ReadValue<float>() * Time.deltaTime * .3f);
            // Only consume hardware throttle after that axis is moved; attaching an idle joystick must not cut the engine.
            RememberHardware(hardwareThrottle, 3);
            if (lastHardware[3] != null && lastHardware[3].device.added)
                controls.Throttle = Calibration.Throttle.Unsigned(lastHardware[3].ReadUnprocessedValue());
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
                if (GameSettings.PersistenceEnabled)
                {
                    try { PlayerPrefs.SetString(PreferencesKey, Actions.SaveBindingOverridesAsJson()); PlayerPrefs.Save(); }
                    catch (Exception ex) { Debug.LogWarning("Bindings apply this session but could not be saved: " + ex.Message); }
                }
                RefreshBindingLabels();
                BindingsChanged?.Invoke();
            }
        }

        public void ResetBindings()
        {
            rebind?.Cancel();
            Actions.RemoveAllBindingOverrides();
            if (GameSettings.PersistenceEnabled) { PlayerPrefs.DeleteKey(PreferencesKey); PlayerPrefs.Save(); }
            RefreshBindingLabels();
            BindingsChanged?.Invoke();
        }

        void OnGUI()
        {
            if (!ShowBindings || flight == null) return;
            if (!controlsSkin)
            {
                controlsSkin = Instantiate(GUI.skin);
                controlsSkin.label.fontSize = 24;
                controlsSkin.label.wordWrap = true;
                controlsSkin.label.padding = new RectOffset(8, 8, 8, 8);
                controlsSkin.button.fontSize = 24;
                controlsSkin.button.wordWrap = true;
                controlsSkin.button.padding = new RectOffset(14, 14, 10, 10);
                controlsSkin.button.margin = new RectOffset(4, 4, 5, 5);
                controlsSkin.toggle.fontSize = 24;
                controlsSkin.toggle.padding = new RectOffset(28, 8, 8, 8);
                controlsSkin.box.padding = new RectOffset(20, 20, 16, 16);
                controlsSkin.verticalScrollbar.fixedWidth = 24;
                controlsSkin.verticalScrollbarThumb.fixedWidth = 24;
                controlsHeading = new GUIStyle(controlsSkin.label) { fontSize = 32, fontStyle = FontStyle.Bold };
            }
            var previousSkin = GUI.skin;
            var previousMatrix = GUI.matrix;
            float scale = Mathf.Min(Mathf.Max(1f, Screen.height / 900f), Screen.width / 920f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.skin = controlsSkin;
            float width = Screen.width / scale, height = Screen.height / scale;
            var panel = new Rect((width - 880) * .5f, 24, 880, height - 48);
            var previousColor = GUI.color;
            GUI.color = new Color(.055f, .08f, .10f, 1f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUILayout.BeginArea(panel, GUI.skin.box);
            GUILayout.Label("Flight controls", controlsHeading);
            GUILayout.Label("Select a binding to change it.");
            deviceSetup = GUILayout.Toolbar(deviceSetup ? 1 : 0, SetupTabs) == 1;
            if (RebindingLabel != null) GUILayout.Label(RebindingLabel);
            bindingScroll = GUILayout.BeginScrollView(bindingScroll);
            if (deviceSetup) DrawDeviceSetup();
            else foreach (var row in bindingRows)
            {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(row.Label, LabelWidth);
                    if (GUILayout.Button(row.Display)) BeginRebind(row.Action, row.Index);
                    GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore defaults")) ResetBindings();
            if (GUILayout.Button("Close")) { rebind?.Cancel(); SaveCalibration(); ShowBindings = false; }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.skin = previousSkin;
            GUI.matrix = previousMatrix;
        }

        public float ReadAxis(InputAction action, int index)
        {
            RememberHardware(action, index);
            if (action.activeControl != null && action.activeControl.device is Keyboard) return action.ReadValue<float>();
            var hardware = lastHardware[index];
            return hardware != null && hardware.device.added ? Calibration.Axis(index).Signed(hardware.ReadUnprocessedValue()) : action.ReadValue<float>();
        }

        void RememberHardware(InputAction action, int index)
        {
            if (action.activeControl is AxisControl axis && !(axis.device is Keyboard)) lastHardware[index] = axis;
            else if (lastHardware[index] != null && !lastHardware[index].device.added) lastHardware[index] = null;
        }

        void RefreshBindingLabels()
        {
            bindingRows.Clear();
            foreach (var action in flight.actions)
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    if (binding.isComposite) continue;
                    bindingRows.Add(new BindingRow { Action = action.name, Index = i, Label = action.name + (binding.isPartOfComposite ? " " + binding.name : ""), Display = action.GetBindingDisplayString(i) });
                }
        }

        InputAction SetupAction => setupAxis == 0 ? pitch : setupAxis == 1 ? roll : setupAxis == 2 ? yaw : hardwareThrottle;
        float SetupRaw()
        {
            RememberHardware(SetupAction, setupAxis);
            var axis = lastHardware[setupAxis];
            return axis != null && axis.device.added ? axis.ReadUnprocessedValue() : SetupAction.ReadValue<float>();
        }

        void UpdateDeviceSetup()
        {
            float raw = SetupRaw();
            if (capturing) { captureMin = Mathf.Min(captureMin, raw); captureMax = Mathf.Max(captureMax, raw); }
            if (Time.unscaledTime < nextDeviceRead) return;
            nextDeviceRead = Time.unscaledTime + .1f;
            var calibration = Calibration.Axis(setupAxis);
            var axis = lastHardware[setupAxis];
            string device = axis != null && axis.device.added ? axis.device.displayName : "No analog device moved on this binding";
            deviceSummary = "Connected input devices: " + InputSystem.devices.Count + "\n" + device;
            axisReadout = $"Raw {raw:0.000}   Output {(setupAxis == 3 ? calibration.Unsigned(raw) : calibration.Signed(raw)):0.000}\nRange {calibration.Minimum:0.000} to {calibration.Maximum:0.000}   Center {calibration.Center:0.000}";
        }

        void DrawDeviceSetup()
        {
            GUILayout.Label(deviceSummary);
            int selected = GUILayout.Toolbar(setupAxis, AxisNames);
            if (selected != setupAxis) { setupAxis = selected; capturing = false; nextDeviceRead = 0; }
            GUILayout.Label(axisReadout);
            GUILayout.Label(calibrationStatus);
            var profile = Calibration.Axis(setupAxis);
            profile.Invert = GUILayout.Toggle(profile.Invert, "Invert axis direction");
            GUILayout.Label("Deadzone  " + Mathf.RoundToInt(profile.Deadzone * 100) + "%");
            profile.Deadzone = GUILayout.HorizontalSlider(profile.Deadzone, 0, .3f);
            if (setupAxis != 3)
            {
                GUILayout.Label("Response curve  " + profile.Response.ToString("0.00"));
                profile.Response = GUILayout.HorizontalSlider(profile.Response, .5f, 2.5f);
            }
            if (!capturing && GUILayout.Button("Capture full axis travel"))
            {
                captureMin = captureMax = SetupRaw(); capturing = true;
                calibrationStatus = setupAxis == 3 ? "Move throttle fully from idle to maximum, then finish." : "Move fully both ways, release stick/pedals to center, then finish.";
            }
            if (capturing && GUILayout.Button(setupAxis == 3 ? "Finish throttle range" : "Use current center and finish"))
            {
                capturing = false;
                if (lastHardware[setupAxis] == null || captureMax - captureMin < .1f) calibrationStatus = "No usable analog travel captured. Check the axis binding and try again.";
                else
                {
                    profile.Minimum = captureMin; profile.Maximum = captureMax; profile.Center = setupAxis == 3 ? (captureMin + captureMax) * .5f : SetupRaw();
                    profile.Normalize(); SaveCalibration(); calibrationStatus = "Calibration captured. Check direction and full travel in the live readout.";
                }
            }
            if (GUILayout.Button("Reset this axis calibration"))
            {
                var defaults = new AxisCalibration();
                profile.Minimum = defaults.Minimum; profile.Maximum = defaults.Maximum; profile.Center = 0; profile.Deadzone = defaults.Deadzone; profile.Response = 1; profile.Invert = false;
                SaveCalibration();
            }
            GUILayout.Label("Physical device verification: check centered input, both limits,\ncorrect direction, trigger and throttle idle before flying.\nSynthetic acceptance tests do not certify your hardware.");
        }

        void SaveCalibration()
        {
            Calibration.Normalize();
            if (GameSettings.PersistenceEnabled && !InputCalibrationStore.Save(Calibration)) calibrationStatus = "Calibration applies this session but could not be saved.";
        }
    }
}
