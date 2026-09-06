# Pacific Fighter Sweep

A Unity 6 Windows combat-flight prototype: fly a P-51D Mustang against four A6M Zero fighters over a procedural Pacific environment. Aircraft use shared Rigidbody aerodynamics; AI flies through virtual pilot controls and the same physics. Aircraft geometry and synthesized audio are original functional placeholders.

## Launch

Open this folder in **Unity 6000.0.79f1 (Unity 6 LTS)**. Allow the Package Manager to restore dependencies. Open `Assets/Game/Scenes/PacificFighterSweep.unity`, enter Play Mode and choose **Fly mission**. If the scene is missing, use **Pacific Combat → Generate mission assets and scene**.

Build through **Pacific Combat → Build Windows player**, or run `./Tools/Build.ps1` to validate and build from PowerShell. The executable is `Builds/Windows/PacificFighterSweep.exe`. The build uses URP, prefers DirectX 12 and includes DirectX 11 fallback. Pass `-force-d3d11` if needed.

## Controls

| Input | Action |
|---|---|
| S / W | Pitch up / down |
| A / D | Roll left / right |
| Q / E | Rudder left / right |
| Left Shift / Left Ctrl | Increase / decrease throttle |
| Space / left mouse | Fire guns |
| F / G / B | Toggle flaps / toggle gear / hold brakes |
| C / V | Cycle cameras / toggle cockpit |
| Right mouse + movement | Look around |
| M | Toggle mouse flight |
| Tab | Cycle enemy target |
| R / Escape | Restart / pause or resume |
| F3 | Developer telemetry |

Gamepad: left stick pitch/roll, right stick horizontal rudder, shoulder buttons throttle, right trigger guns, west face button flaps, D-pad down gear, north face button target, stick clicks camera/cockpit, Start pause. Joystick stick/twist/trigger/throttle bindings are included. Use **Controls & remapping** to bind recognized axes, pedals, buttons and keys; overrides persist in Unity PlayerPrefs. Physical device compatibility and calibration need testing with your hardware.

## Mission and training

Start at 10,000 feet and 250 mph, with enemies about 4 km ahead. Destroy all enemies to win; aircraft destruction or crashing causes defeat. Pause, restart and mission results are implemented. Training lead indication and unlimited ammunition are optional; ammunition is finite by default. Four AI difficulties affect reactions and aim, not flight performance. Graphics preset, master audio volume, difficulty, training assists and HUD visibility persist across launches. Developer telemetry remains temporary.

Fuel is finite and shown on the HUD. Throttle affects consumption; tank damage can cause leaks and fuel-fed fires. An empty tank stops engine thrust and combustion audio while the aircraft can still glide. Fuel-fed flames stop when the supply is exhausted, preserving existing damage. Missions restart with full tanks. Capacities use litres and engine consumption settings use litres/hour; these are tunable gameplay values.

The Mustang is faster and heavier. The Zero has lower wing loading and stronger low-speed pitch authority. Use speed and altitude rather than staying in a flat turning fight.

## Architecture

- `Assets/Game/Physics`: aircraft ScriptableObjects, finite fuel/consumption/leaks, engine spool/thrust, density, lift/drag, stall separation, airflow-sensitive torques, asymmetric wing damage and G measurement. Only spawning and floating-origin resets set aircraft positions directly.
- `Assets/Game/Input`: Unity Input System actions and persistent interactive rebinding.
- `Assets/Game/Combat`: six wing guns for the Mustang; two cannon and two MGs for the Zero. Fixed ballistic slots integrate travel, gravity and drag, raycast each segment, inherit aircraft velocity and converge near 300 m. Component damage degrades engines, wings and controls; fire/stress/collisions can destroy an aircraft.
- `Assets/Game/AI`: timed deterministic perception/tactics, target memory, velocity-aware ballistic lead, curved tactical interception, energy management, defensive turns, overshoot recovery, terrain avoidance and squad spacing. Attack priority rotates between surviving squad members. FixedUpdate commands shared flight controls; faster fleeing aircraft can legitimately escape.
- `Assets/Game/Settings`: versioned player preferences with safe defaults and isolated non-persistent automation settings. Graphics options modify a session pipeline clone, preserving authored URP assets.
- `Assets/Game/Missions`: mission data, spawning, objectives and restart/results state.
- `Assets/Game/Aircraft`, `Environment`, `Camera`, `UI`, `Effects`, `Audio`: procedural aircraft/environment, chase/cockpit/target/flyby cameras, reflector sight/HUD, pooled effects and synthesized audio.
- `Assets/Game/Editor`: asset/scene generation, Windows build and acceptance checks.

Aircraft, weapon and mission tuning assets live in `Assets/Game/ScriptableObjects`. Runtime creation supplies a playable scene without external art dependencies. The project does not contain imported historical aircraft models.

## Verification

Run **Pacific Combat → Run core acceptance checks** for editor tests covering flight, ballistics/damage, fuel, saved preferences and actual Input System keyboard events. AI checks include a five-minute maneuvering engagement with later firing passes and rotating squad roles. Batch entry point: `PacificCombat.Editor.ProjectBuilder.Validate`.

Run `./Tools/SmokeTest.ps1 -Enemies 4 -Duration 60 -GraphicsApi DirectX12` for a rendered acceptance run. The script checks the exit code, timeout and fresh report. Use `-Enemies 8 -GraphicsApi DirectX11 -Width 1280 -Height 720` for the larger scenario and fallback renderer.

The executable also accepts `--smoke-test`, `--enemies=8` and `--smoke-duration=60` directly. Tests exercise player/AI firing, fuel use/starvation, saved-settings isolation, pause, floating origin, victory, defeat and restart. Results, frame-time percentiles, memory samples and screenshots go beneath `Builds/Windows/SmokeTest4` or `SmokeTest8`. A mission ending early shortens the measured sample; the report lists observed and requested durations. Keep the window visible: Windows suppresses screen capture for hidden games. Tests use default assists and cannot overwrite saved settings. Automation does not run during normal play.

Test results and actual limitations are recorded in `DEVELOPMENT_LOG.md` and `TODO.md`. The 60+ FPS goal requires measured player performance; headless physics checks do not measure graphics performance.

## Performance strategy and limitations

Five primary aircraft Rigidbodies, simple compound colliders, bounded ballistic arrays and tracer/particle pools, staggered AI decisions, static coarse island meshes, instanced materials and aircraft LOD. Horizontal floating-origin shifts preserve altitude, projectile positions, particle trails and AI memory. There is no online model in gameplay.

This is the specification's first-playable milestone, not every future feature in its 120 sections. Full cockpit art/instruments, validated historical flight envelopes, full landing/ground handling, physical structural debris, production sound recordings, complete settings coverage and the extended maneuver/mode library remain follow-up work. See `TODO.md` for priorities. No multiplayer is implemented.
