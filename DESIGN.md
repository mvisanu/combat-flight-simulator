---
name: Pacific Fighter Sweep
description: Restrained flight instruments over a Pacific combat view.
colors:
  sight-amber: "rgb(100% 72% 23%)"
  instrument-ivory: "rgb(95% 94% 82%)"
  panel-charcoal: "rgb(4.5% 7% 8% / 87%)"
  button-matte: "rgb(13% 19% 20%)"
  button-hover: "rgb(23% 31% 32%)"
  button-active: "rgb(32% 36% 24%)"
  state-white: "#ffffff"
  training-green: "rgb(50% 95% 65%)"
  ocean: "rgb(2.5% 16% 23%)"
typography:
  title:
    fontSize: "44px"
    fontWeight: 700
  readout:
    fontSize: "32px"
    fontWeight: 700
  body:
    fontSize: "22px"
  button:
    fontSize: "21px"
  label:
    fontSize: "16px"
rounded:
  square: "0px"
components:
  button:
    backgroundColor: "{colors.button-matte}"
    textColor: "{colors.instrument-ivory}"
    typography: "{typography.button}"
    rounded: "{rounded.square}"
    padding: "8px 10px 8px 20px"
    height: "46px"
  button-hover:
    backgroundColor: "{colors.button-hover}"
    textColor: "{colors.state-white}"
  button-active:
    backgroundColor: "{colors.button-active}"
    textColor: "{colors.state-white}"
  instrument-panel:
    backgroundColor: "{colors.panel-charcoal}"
    textColor: "{colors.instrument-ivory}"
    rounded: "{rounded.square}"
---

# Design System: Pacific Fighter Sweep

## Overview

**Creative North Star: "The Reflector Sight"**

The native Windows Unity game gives the 3D flight view priority. The user's WWII reflector sight and unobtrusive HUD are binding; clean placeholder 3D artwork is authorized for the first playable. Preserve Pacific blue water, atmospheric haze and readable aircraft silhouettes. This is the implemented prototype visual system, not a claim of finished production art.

**Key Characteristics:**

- Open center with compact instruments at the screen edges.
- Amber targeting, warm ivory telemetry and matte charcoal controls.
- Geometric aircraft, islands and cloud clusters with restrained surface detail.

Source authority: `Assets/Game/UI/FlightHUD.cs`, `Assets/Game/Materials/PacificOcean.shader`, `Assets/Game/Camera/CameraController.cs`, `Assets/Game/Aircraft/AircraftFactory.cs` and `AircraftVisuals.cs`. Visual reference: `Builds/Windows/SmokeTest8/{flight,pause,settings,cockpit}.png` at 1280 x 720. These captures establish appearance, not hardware performance.

## Colors

Primary sight amber marks the gunsight, target brackets and offscreen direction arrow. Training green distinguishes the optional lead indicator. Neutral instrument ivory carries labels and numbers over translucent panel charcoal. Matte blue-green buttons brighten on hover and keyboard focus; olive active fill and white type distinguish pressed or selected states. The ocean's base color is modified by lighting, Fresnel reflection and fog.

## Typography

Use Unity's inherited IMGUI skin font; no custom font family is installed by the HUD. Frontmatter sizes are design-canvas units expressed as pixels for portability. Bold title and flight readout roles establish hierarchy; body, buttons and compact labels use regular weight. Keep instructions short enough for the fixed panels.

## Layout

`FlightHUD` scales a 1600 x 900 design canvas independently by screen width and height. At 1280 x 720, all HUD geometry and type scale to 80%; the main sight radius becomes 24 screen pixels. Preserve that matrix when drawing rotated sight segments so reticles do not escape the scale.

Mission status occupies the upper left, heading/camera/time the upper right, speed/altitude/engine the lower left, and weapons/target the lower right. Outer horizontal margins are 30 design units. Pause, briefing, results and settings share a centered 670 x 630 panel beginning at (465, 135), with 580-unit action rows. The separate controls/remapping panel owns pixel-space layout and is not governed by this HUD canvas.

## Elevation & Depth

HUD surfaces use flat tonal layering with no decorative shadows. Menus dim the retained flight scene with a translucent scrim. World depth comes from perspective, lighting and atmospheric haze. The ocean filters waves with screen-space derivatives and attenuates wave strength with distance to suppress moire; retain the quiet distant surface rather than adding dense repeated lines.

## Shapes

Panels and buttons have square corners. The sight uses a 48-segment circle, three ticks and a center dot; its radius is 30 design units and ring stroke is 1.5 units. Target brackets use a 12-unit radius, while the optional lead sight uses 7 units. The Mustang has a silver long-nose silhouette; the Zero has a green radial-nose silhouette. Islands remain coarse meshes and clouds bounded geometric clusters.

## Components

- **Flight instruments:** Compact edge panels with the largest type reserved for speed and altitude. Fuel percentage shares the secondary engine readout; low, leaking and empty fuel states use explicit warning text below the sight. Keep the center available for tracking aircraft.
- **Menus:** Left-aligned matte buttons with consistent hover/focus treatment; action rows are 46 units tall. Briefing and results reuse the pause panel. Retain the prototype-art/audio disclosure.
- **Settings:** Difficulty and graphics choices show selection with active fill. Boolean rows explicitly prefix their labels with `ON` or `OFF`; color alone does not communicate state. Master audio uses the inherited IMGUI slider. Graphics, volume, difficulty, training assists and HUD visibility persist between sessions; developer telemetry is temporary.
- **Camera and cockpit:** Cockpit field of view is 78 degrees; flyby is 50 degrees; chase/target vary from 62 to 73 degrees with airspeed. Above 700 RPM, solid propeller blades give way to a transparent blur disk with alpha 0.065, disabled depth writing and no cast/received shadows. Keep the forward sight and horizon visible through it.

## Do's and Don'ts

- Do preserve the WWII reflector sight and unobtrusive edge HUD.
- Do inspect flight, pause, settings and cockpit together at 1280 x 720 after visual changes.
- Do keep ocean detail filtered, button surfaces matte and toggle state explicit.
- Don't substitute opaque high-RPM propeller geometry that blocks the forward view.
- Don't describe the prototype geometry as production aircraft art or treat screenshots as evidence of a performance target.
