# Pacific Fighter Sweep

A Unity 6 Windows combat-flight prototype: choose a P-51D Mustang, A6M Zero, Bf 109 G-6 or P-38J Lightning and fly a custom dogfight over Pacific islands. Both sides use shared Rigidbody aerodynamics; AI flies through virtual pilot controls and the same physics. Blender-authored airframes and cockpit interiors replace the original primitive models; Mustang engine audio uses an authored CC0 field-recording loop; other engines retain synthesized fallbacks.

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

The briefing has **Your aircraft** and **Enemy aircraft** rows. Choose any of the four types for either side, then select **Fly mission**. Choices persist across launches and remain selected after restarting or returning to the main menu. The default matchup is Mustang versus Zero; custom matchups are sandbox scenarios, not claims of historical encounters.

Choose Sweep, Intercept, Free flight or Landing practice and a Clear, Scattered, Overcast or Squall weather preset. Landing practice approaches runway 36; extend gear and flaps, touch down gently and brake to a stop. Squalls introduce wind and gusts. Cloud volumes block AI sight independently of graphics quality.

Each type has its own mass, wing/engine tuning and gun configuration. The Bf 109 uses a hub cannon and two machine guns; the P-38 has concentrated nose armament, twin booms, counter-rotating propellers and tricycle gear. The P-38 currently uses combined engine power, damage and throttle control. Cockpit instruments share a functional layout across the roster.

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
- `Assets/Game/Aircraft`, `Environment`, `Camera`, `UI`, `Effects`, `Audio`: procedural aircraft/environment, chase/cockpit/target/flyby cameras, reflector sight/HUD, pooled effects and recorded/synthesized audio.
- `Assets/Game/Editor`: asset/scene generation, Windows build and acceptance checks.

Aircraft, weapon and mission tuning assets live in `Assets/Game/ScriptableObjects`. Blender-authored aircraft and island prefabs live in `Assets/Game/Resources/Art`, with editable `.blend`, FBX and reproducible source in `ArtSource` and `Tools/author_blender_art.py`. See [the artwork workflow](ArtSource/README.md). These are historically inspired original models, not blueprint-certified reproductions.

Cockpit view includes working indicated airspeed, attitude, altitude, heading, engine RPM, fuel, climb, oil temperature, engine temperature, manifold pressure and oil-pressure instruments. The airspeed gauge shows density-corrected IAS; the HUD retains true airspeed. The environment combines a Blender volcanic island with eroded procedural islands, slope-blended jungle/basalt/sand, shallow reefs, animated surf, soft cloud banks and an atmospheric sky.

## Verification

Run **Pacific Combat → Run core acceptance checks** for editor tests covering flight, ballistics/damage, fuel, saved preferences and actual Input System keyboard events. AI checks include a five-minute maneuvering engagement with later firing passes and rotating squad roles. Batch entry point: `PacificCombat.Editor.ProjectBuilder.Validate`.

Selection checks cover all 16 player/enemy combinations. Launch the player with `--selection-smoke` for ten real scene reloads covering every choice, Restart and Main Menu; results go to `Builds/Windows/SelectionSmoke/results.txt`. This test also isolates saved user preferences.

Run `./Tools/SmokeTest.ps1 -Player p38 -Enemy bf109 -Enemies 4 -Duration 35 -GraphicsApi DirectX12` for a rendered acceptance run. Aircraft codes are `p51`, `zero`, `bf109` and `p38`. The script checks the exit code, timeout and fresh report. Use `-Enemies 8 -GraphicsApi DirectX11 -Width 1280 -Height 720` for the larger scenario and fallback renderer.

The executable also accepts `--player=p38`, `--enemy=bf109`, `--smoke-test`, `--enemies=8` and `--smoke-duration=60` directly. Tests exercise player/AI firing, fuel use/starvation, saved-settings isolation, pause, floating origin, victory, defeat and restart. Script-run results, frame-time percentiles, memory samples and screenshots go beneath `Builds/Windows/SmokeTest4-p38-bf109` (with the count and chosen codes in each directory name). A mission ending early shortens the measured sample; the report lists observed and requested durations. Keep the window visible: Windows suppresses screen capture for hidden games. Tests use default assists and cannot overwrite saved settings. Automation does not run during normal play.

Test results and actual limitations are recorded in `DEVELOPMENT_LOG.md` and `TODO.md`. The 60+ FPS goal requires measured player performance; headless physics checks do not measure graphics performance.

## Performance strategy and limitations

Five primary aircraft Rigidbodies, simple compound colliders, bounded ballistic arrays and tracer/particle pools, staggered AI decisions, static island meshes, bounded cloud ray marching, instanced materials and aircraft LOD. Horizontal floating-origin shifts preserve altitude, projectile positions, particle trails and AI memory. There is no online model in gameplay.

The build now includes four aerobatic maneuver sequences, ground suspension/steering/braking, physical detached structure, thermal engine damage, expanded missions/weather, and all graphics/audio/gameplay options in section 72. Settings include analog-axis travel/center calibration, inversion, deadzone and response. Engine cooling and P-38 power are still aggregate game models. Exact blueprint cockpit fidelity, aircraft-specific developed-spin certification, remaining authentic engine recordings and tests on midrange PCs/physical flight controls remain open in `TODO.md`. No multiplayer is implemented.

For scenario/weather verification, launch with `--world-smoke`; results and captures go to `Builds/Windows/WorldSmoke`. For a five-minute rendering soak, pass `--smoke-test --soak-smoke --smoke-duration=300 --enemies=8 --smoke-label=soak-final`. The soak explicitly ignores combat damage and gives enemies unlimited ammunition to keep the workload alive, while exercising actual flight, AI, ballistics and hit effects. It is separate from ordinary damage/mission acceptance.

Air contacts: the upper-right radar shows active, living aircraft within a selectable 2/5/10 statute-mile horizontal radius (default 5), with half/full range rings. Cyan circles identify friends, red diamonds identify foes, and a white aircraft marks you at the center. Amber corner brackets identify the selected enemy without changing its red hostile symbol. Affiliation follows the aircraft's team, independently of its type or nationality. Nearby symbols spread apart with leader lines to their exact radar positions. The legend and friend/foe counts remain visible; current missions spawn the player and enemies, while same-team aircraft are supported when present. This heading-up omniscient gameplay aid does not simulate onboard radar, terrain masking, identification uncertainty or ground-station coverage.

Add `-Radar` to `Tools/SmokeTest.ps1` to test mixed-team contacts, all three ranges, crowded symbols and the empty state. This opt-in fixture adds temporary radar-only aircraft and saves `radar-2mi.png`, `radar-5mi.png`, `radar-10mi.png` and `radar-empty.png` in the smoke report folder; fixtures are removed before the regular flight test.
