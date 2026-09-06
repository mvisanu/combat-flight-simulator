# Remaining work

## CRITICAL
- No known blockers in the verified first-playable mission. Four- and eight-enemy smoke tests pass.

## HIGH
- Validate sustained performance on the requested midrange hardware; current measurements use an RTX 5080 Laptop GPU.
- Broaden AI validation to damaged aircraft and varied spawn/maneuver scenarios; the five-minute circuit engagement now verifies rotating roles and later attack passes.
- Validate hardware-specific HOTAS, pedal and controller bindings on physical devices.

## MEDIUM
- Replace procedural aircraft and cockpit with authored P-51D and A6M models.
- Expand reusable AI maneuvers (full Immelmann, Split-S, rolling scissors and barrel-roll defense).
- Full landing/ground handling, physical detached structural debris and richer engine/fire behavior. Fuel use, leaks, starvation and fuel-fed fire extinction are implemented.
- Validate stalls/spins and aircraft flight envelopes against reference data; current aerodynamics are a tuned simplified model.

## LOW
- Additional missions and weather presets.
- Full graphics and audio settings coverage from long-term specification.
- Existing graphics/audio/difficulty/training/HUD preferences and input bindings now persist; expand options alongside the full settings specification.
- Profile and reduce remaining IMGUI/telemetry garbage allocations; no claim of zero-allocation frames.

## POLISH
- Authored engine recordings, cockpit instruments, advanced cloud and shoreline effects.
- Cloud rendering currently uses geometric placeholders and does not occlude AI vision.
