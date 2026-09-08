using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PacificCombat.Editor
{
    public static class InputAcceptance
    {
        [MenuItem("Pacific Combat/Run Input Acceptance")]
        public static void Run()
        {
            var originalKeyboard = Keyboard.current;
            Keyboard keyboard = null;
            var originalGamepad = Gamepad.current;
            Gamepad gamepad = null;
            GameObject fixture = null;
            InputActionAsset testActions = null;
            var originalSettings = InputSystem.settings;
            InputSettings testSettings = null;
            try
            {
                // Input System 1.8.1 defaults Update() to an Editor update outside
                // Play Mode; InputActionState deliberately ignores those events.
                // This package-provided test flag enables actual player updates.
                // Clone settings so neither the authored asset nor the user's mode
                // or feature flags are changed by this acceptance fixture.
                testSettings = UnityEngine.Object.Instantiate(originalSettings);
                testSettings.hideFlags = HideFlags.DontSave;
                testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
                testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
                InputSystem.settings = testSettings;
                keyboard = InputSystem.AddDevice<Keyboard>();
                fixture = new GameObject("Input acceptance fixture");
                fixture.AddComponent<AircraftController>();
                var input = fixture.AddComponent<AircraftInput>();
                input.Initialize();
                testActions = input.Actions;
                input.Actions.devices = new InputDevice[] { keyboard };
                // Test the shipped defaults without changing persisted user overrides.
                input.Actions.RemoveAllBindingOverrides();
                input.Actions.Enable();
                CheckAxis(input, keyboard, "Pitch", Key.S, 1);
                CheckAxis(input, keyboard, "Pitch", Key.W, -1);
                CheckAxis(input, keyboard, "Roll", Key.A, -1);
                CheckAxis(input, keyboard, "Roll", Key.D, 1);
                CheckAxis(input, keyboard, "Yaw", Key.Q, -1);
                CheckAxis(input, keyboard, "Yaw", Key.E, 1);
                CheckAxis(input, keyboard, "Throttle", Key.LeftShift, 1);
                CheckAxis(input, keyboard, "Throttle", Key.LeftCtrl, -1);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();
                Require(input.Actions.FindAction("Fire").IsPressed(), "Space fires guns");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                Require(!input.Actions.FindAction("Fire").IsPressed(), "Release stops firing");
                var fire = input.Actions.FindAction("Fire");
                fire.ApplyBindingOverride(0, "<Keyboard>/j");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space)); InputSystem.Update();
                Require(!fire.IsPressed(), "Override removes original key");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J)); InputSystem.Update();
                Require(fire.IsPressed(), "Override accepts replacement key");
                gamepad = InputSystem.AddDevice<Gamepad>();
                input.Actions.devices = new InputDevice[] { keyboard, gamepad };
                input.Calibration.Pitch = new AxisCalibration(); input.Calibration.Roll = new AxisCalibration();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                InputSystem.QueueStateEvent(gamepad,new GamepadState { leftStick = new Vector2(.65f,-.45f), rightTrigger = 1 });
                InputSystem.Update();
                Require(Mathf.Abs(input.ReadAxis(input.Actions.FindAction("Roll"),1)-input.Calibration.Roll.Signed(.65f))<.01f,
                    "Gamepad analog roll passes through calibration");
                Require(Mathf.Abs(input.ReadAxis(input.Actions.FindAction("Pitch"),0)-input.Calibration.Pitch.Signed(-.45f))<.01f,
                    "Gamepad analog pitch preserves signed input");
                Require(fire.IsPressed(),"Gamepad trigger fires");
                InputSystem.QueueStateEvent(gamepad,new GamepadState()); InputSystem.Update();
                Require(Mathf.Abs(input.ReadAxis(input.Actions.FindAction("Roll"),1))<.001f && !fire.IsPressed(),"Analog release returns to neutral and stops firing");
                Debug.Log("INPUT ACCEPTANCE PASSED: eight signed axes, trigger press/release and remapped trigger via actual Input System events; user bindings unchanged.");
            }
            finally
            {
                if (testActions) { testActions.Disable(); UnityEngine.Object.DestroyImmediate(testActions); }
                if (fixture) UnityEngine.Object.DestroyImmediate(fixture);
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                if (gamepad != null) InputSystem.RemoveDevice(gamepad);
                if (originalGamepad != null && originalGamepad.added) originalGamepad.MakeCurrent();
                if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
                // 1.8.1 skips feature-flag application when the original flag set
                // is null, so explicitly clear our flag before restoring it.
                if (testSettings) testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", false);
                InputSystem.settings = originalSettings;
                if (testSettings) UnityEngine.Object.DestroyImmediate(testSettings);
            }
        }
        static void CheckAxis(AircraftInput input, Keyboard keyboard, string action, Key key, float expected)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.Update();
            var inputAction = input.Actions.FindAction(action);
            Require(Mathf.Abs(inputAction.ReadValue<float>() - expected) < .001f, action + " / " + key + " value=" + inputAction.ReadValue<float>() + " enabled=" + inputAction.enabled + " controls=" + inputAction.controls.Count + " key=" + keyboard[key].ReadValue());
        }
        static void Require(bool value, string message) { if (!value) throw new Exception("INPUT ACCEPTANCE FAILED: " + message); }
    }
}
