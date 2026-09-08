# Work status

Implemented items are checked only where there is saved build/test evidence. External hardware and historical fidelity checks remain open rather than being represented as completed.

## CRITICAL
- [x] Integrated Windows build passes core and expanded acceptance (`Logs/todo-final-build.log`).
- [x] Correct real aircraft collision hulls so all four types can land on their wheels. Real factory landing/braking, hard touchdown and fatal wall-impact tests pass (`Logs/landing-playmode-acceptance.txt`).

## HIGH
- [ ] Validate sustained performance on Ryzen 5 5600 / i5-12400 and RTX 3060 / RX 6700 XT class hardware. **External hardware required:** the available PC has an RTX 5080 Laptop GPU. A slower render preset does not substitute for this test.
- [x] Broaden AI validation to damaged aircraft and varied offset/banked spawns. All four aircraft survive the recoverable damaged fixture, reject unsafe aerobatics and pass the original five-minute circuit engagement.
- [ ] Validate HOTAS, pedals and controllers on physical devices. **Physical devices required:** software Input System events, analog centering/inversion/range checks and saved calibration pass; actual travel, device-driver mappings and reconnect behavior must be tested with the user's hardware.

## MEDIUM
- [x] Refine Mustang/Zero dimensions and cockpit fittings from available references; add generated wear textures and four live engine instruments. Mustang reference: original T.O. 1F-51D-1 general arrangement and cockpit pages. Editable Blender source is saved.
- [ ] Complete blueprint-level A6M2 model/cockpit verification. **Reference gap:** a complete manufacturer drawing set has not been obtained. Current cockpit layouts and unit choices remain game approximations; see `ArtSource/References/README.md`.
- [x] Implement phased Immelmann, Split-S, rolling scissors and barrel-roll defense. All 16 aircraft/maneuver combinations complete through aerodynamic controls, with energy/altitude/damage abort tests (`Logs/ai-expanded-acceptance.txt`).
- [x] Add landing/ground suspension, steering and brakes; pooled physical detached structure; engine temperature/oil pressure/overheating and progressive fuel-fed fire. Ground, thermal, debris lifetime/reuse and collision tests pass.
- [x] Validate incipient stall/spin entry and recovery against FAA principles and compare four speed envelopes with published museum reference values (`Logs/advanced-flight-acceptance.txt`).
- [ ] Validate exact aircraft-specific stall speeds and developed-spin envelopes. **Reference gap:** current speed comparisons use broad conditions and some nearby aircraft variants. Matching weight/altitude/power test data are still needed; this is not a certified flight model.

## LOW
- [x] Add Intercept, Free flight and Landing practice alongside Sweep, plus Clear, Scattered, Overcast and Squall weather. Four runtime scenario/weather reloads and runway landing objective pass (`Builds/Windows/WorldSmoke/results.txt`).
- [x] Complete graphics/audio/gameplay settings listed in `prompt.md` section 72: display, VSync/frame cap, render scale, texture/shadow/cloud/terrain/MSAA/effects quality, separate audio buses, damage/aerodynamic assists.
- [x] Persist expanded preferences and analog device calibration; verify exact settings round-trip, invalid-input handling and actual virtual gamepad events without changing user preferences.
- [x] Cache HUD telemetry and binding labels, suppress unused debug formatting, add allocation markers and an opt-in sustained rendering harness. No zero-allocation claim; benchmark evidence belongs in `DEVELOPMENT_LOG.md`.

## POLISH
- [x] Author a seamless Mustang engine loop from a CC0 field recording, with reproducible processing and source attribution (`ArtSource/Audio/README.md`).
- [ ] Obtain and audition authentic, redistributable engine recordings for the Zero, Bf 109 and P-38. Their synthesized fallbacks remain; the Mustang sample is not mislabeled as their engines.
- [x] Add live oil/engine temperature, manifold pressure and oil-pressure instruments.
- [x] Replace cloud billboards with bounded volumetric ray marching, configurable sample quality and optical-depth AI visibility. Cloud visibility stays independent of the graphics setting. Reef/surf geometry remains a documented visual approximation, not a fluid simulation.
