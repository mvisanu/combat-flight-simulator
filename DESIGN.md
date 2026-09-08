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
  ocean: "rgb(1.8% 12.5% 19%)"
  forest: "rgb(10% 19% 6.5%)"
  lagoon: "rgb(7% 43% 39%)"
  dial-ink: "#0f1414"
  dial-mark: "#e1e0c0"
  needle-ivory: "rgb(86% 88% 68%)"
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

The native Windows Unity game gives the 3D flight view priority. The user's WWII reflector sight and unobtrusive HUD remain binding. The requested Blender MCP upgrade adds authored, historically inspired metal and painted airframes, a modeled cockpit with working analog gauges, and natural Pacific relief, coasts, clouds and haze. These are original game models, not blueprint-certified historical reconstructions.

**Key Characteristics:**

- Open center with compact instruments at the screen edges.
- Amber targeting, warm ivory telemetry and matte charcoal controls.
- Authored airframes and physical instruments over volcanic relief, coral shelves and soft cumulus.

Source authority: `Assets/Game/UI/FlightHUD.cs`, `Assets/Game/Aircraft/CockpitInstruments.cs`, `Assets/Game/Camera/CameraController.cs`, `Assets/Game/Environment/PacificEnvironment.cs` and the shaders in `Assets/Game/Materials`. `ArtSource/README.md` describes editable Blender artwork and Unity imports; `Tools/author_blender_art.py` and `Tools/author_extra_aircraft.py` own authored material and mesh definitions. Current visual references are the refreshed `Builds/Windows/SmokeTest4` captures at 1600 x 900 and `Builds/Windows/SmokeTest8` captures at 1280 x 720, including flight, cockpit and terrain-detail views. Roster-extension references: `Builds/Windows/SmokeTest4-p38-bf109/{briefing,aircraft-detail,enemy-detail,cockpit}.png` at 1600 x 900 and `Builds/Windows/SmokeTest4-zero-p51/{briefing,cockpit,flight}.png` at 1280 x 720. Source documentation does not establish runtime performance.

## Colors

Primary sight amber marks the gunsight, target brackets and offscreen direction arrow. Training green distinguishes the optional lead indicator. Neutral instrument ivory carries labels and numbers over translucent panel charcoal. Matte blue-green buttons brighten on hover and keyboard focus; olive active fill and white type distinguish pressed or selected states. The ocean's base color is modified by lighting, Fresnel reflection and fog. Forest greens blend into basalt on steep/high terrain and warm sand near sea level; muted turquoise lagoons soften the coast. Aircraft combine brushed aluminum and alternate metal panels on the Mustang with weathered green paint and a warm grey underside on the Zero. The Bf 109 adds grey-green and darker painted panels with a pale underside and black crosses edged in ivory; the P-38 carries the aluminum material family across its pod and booms. Dark anti-glare surfaces and interior green frame the cockpit. Dial ink and pale printed marks support luminous ivory needles; orange reference marks and redline ticks remain selective.

## Typography

Use Unity's inherited IMGUI skin font; no custom font family is installed by the HUD. Frontmatter sizes are design-canvas units expressed as pixels for portability. Bold title and flight readout roles establish hierarchy; body, buttons and compact labels use regular weight. Keep instructions short enough for the fixed panels. Physical dial faces use compact built-in 5 x 7 stencil glyphs rendered into 256-pixel textures with mipmaps and trilinear filtering; they do not use the HUD font.

## Layout

`FlightHUD` scales a 1600 x 900 design canvas independently by screen width and height. At 1280 x 720, all HUD geometry and type scale to 80%; the main sight radius becomes 24 screen pixels. Preserve that matrix when drawing rotated sight segments so reticles do not escape the scale.

Mission status occupies the upper left, heading/camera/time the upper right, speed/altitude/engine the lower left, and weapons/target the lower right. Outer horizontal margins are 30 design units. Pause, results and settings share a centered 670 x 630 panel beginning at (465, 135), with 580-unit action rows. The expanded briefing uses a 670 x 750 panel at (465, 75) to accommodate mission and weather selection without shrinking aircraft choices. In cockpit mode, physical gauges replace the large lower-left telemetry: a compact 460 x 69 panel at (30, 805) retains throttle and G, with controls hints at y=847. Mission/navigation and weapon/target panels remain. The separate controls/remapping panel owns pixel-space layout and is not governed by this HUD canvas.

## Elevation & Depth

HUD surfaces use flat tonal layering with no decorative shadows. Menus dim the retained flight scene with a translucent scrim. World depth comes from perspective, lighting and atmospheric haze. The ocean uses randomized noise normals attenuated by screen-space footprint and camera distance to suppress moire; preserve its quiet distant surface. Terrain shading follows slope and height, reefs use transparent irregular shelves with patchy surf, and clouds use ellipsoid-bounded volume ray marching with 8/12/24/40 quality steps and optical-depth visibility checks. A sky gradient, warm sun and exponential-squared maritime fog connect the horizon. Runtime shader output is the visual authority; Blender material previews are not exact Unity appearance.

## Shapes

Panels and buttons have square corners. The sight uses a 48-segment circle, three ticks and a center dot; its radius is 30 design units and ring stroke is 1.5 units. Target brackets use a 12-unit radius, while the optional lead sight uses 7 units. Authored aircraft preserve the Mustang long nose and Zero radial nose while adding shaped airfoils, skin seams, access panels, exhausts, insignia and slender canopy frames. The Bf 109 uses a narrow fuselage and faceted framed canopy. The P-38 has a central pilot/gun pod, paired engine nacelles, twin tail booms and linking stabilizer, with guns concentrated in its nose. Transparent canopy shells keep the forward view open. The central island uses authored volcanic relief; surrounding islands use deterministic eroded meshes, irregular coastlines and fringing reefs. The simple airframe remains a distant LOD.

## Components

- **Flight instruments:** Outside the cockpit, compact edge panels reserve the largest type for speed and altitude. Fuel percentage shares the secondary engine readout; low, leaking and empty fuel states use explicit warning text below the sight. Keep the center available for tracking aircraft.
- **Menus:** Left-aligned matte buttons with consistent hover/focus treatment; action rows are 46 units tall. Briefing and results reuse the pause panel. Retain the prototype-art/audio disclosure.
- **Aircraft selection:** Briefing offers independent `YOUR AIRCRAFT` and `ENEMY AIRCRAFT` rows for P-51D Mustang, A6M Zero, Bf 109 G-6 and P-38J Lightning. Each row repeats the selected short name in its label and uses the established olive selected fill and white text. Four 140 x 42 choices sit on a 145-unit pitch beneath each label, at label y=340 and y=416 in the design canvas. Mission objectives precede `Fly mission`; compact controls/settings actions retain the menu styling.
- **Scenario selection:** Mission and weather each have four choices above the aircraft rows, at label y=188 and y=264. Sweep, Intercept, Free flight and Landing pair with Clear, Scattered, Overcast and Squall. Selected text repeats in the label; no color-only selection state.
- **Nationality identification:** Each aircraft choice carries a 28 x 15 design-unit raster flag, and the selected row label spells out United States, Japan or Germany. The period identifiers are the 48-star US flag, Japanese Hinomaru and German 1935–1945 flag. These identify the WWII roster; airframes carry service insignia rather than rectangular national flags. `ArtSource/References/WWII-Liveries.md` records the supplied paint references. The reference livery build passes mesh/material acceptance; paired runtime captures cover all four aircraft and the selector flags.
- **Settings:** Four tabs separate Gameplay, Graphics, Audio and Devices. Scrollable content stays above the fixed Back action. Arrow choices, continuous sliders and explicit ON/OFF rows share the existing type and palette. Graphics covers display, render scale, texture/shadow/cloud/terrain/effects quality and MSAA; audio has independent buses; Devices opens binding and analog calibration tools.  Difficulty and graphics choices show selection with active fill. Boolean rows explicitly prefix their labels with `ON` or `OFF`; color alone does not communicate state. Master audio uses the inherited IMGUI slider. Graphics, volume, difficulty, training assists and HUD visibility persist between sessions; developer telemetry is temporary.
- **Camera and cockpit:** Cockpit field of view is 80 degrees, with the camera anchored to `CockpitInstruments.EyePosition` at aircraft-local (0, 1.28, -0.12) metres; flyby is 50 degrees; chase/target vary from 62 to 73 degrees with airspeed. Above 700 RPM, solid propeller blades give way to a transparent blur disk with alpha 0.065, disabled depth writing and no cast/received shadows. The P-38 animates two counter-rotating nacelle propellers with matching blur disks, plus tricycle landing gear; its central nose remains clear of a propeller. All four player aircraft share the working cockpit instrument layout. Keep the forward sight and horizon visible through it.

- **Analog panel:** Eleven working gauges show indicated airspeed (MPH), attitude, altitude (1000 FT), heading, RPM (X100), fuel (percent), and climb (1000 FPM). Four smaller gauges sit above three larger engine/climb gauges. Needles and attitude follow live aircraft state with exponential smoothing; cockpit IAS includes density correction while the exterior HUD reports true airspeed. Instrument faces and needles are unlit for readability, with machined lit bezels.

## Do's and Don'ts

- Do preserve the WWII reflector sight and unobtrusive edge HUD.
- Do inspect flight, pause, settings and cockpit together at 1280 x 720 after visual changes.
- Do keep ocean detail filtered, button surfaces matte and toggle state explicit.
- Don't substitute opaque high-RPM propeller geometry that blocks the forward view.
- Do keep physical gauges readable and preserve the distinction between cockpit IAS and HUD true airspeed.
- Don't claim historical certification or treat screenshots as evidence of a performance target.

The additional lower cockpit row contains oil temperature, engine temperature, manifold pressure and oil pressure. Zero/Bf 109 airspeed and altitude faces use metric units; Mustang/P-38 retain imperial faces. Instrument layout remains a readable game approximation. Briefing and pause retain the disclosure ?Prototype aircraft | Recorded and synthesized audio?. HUD objective text is intentionally brief, with full instructions in the briefing.

Flight controls readability: 24px labels/buttons and 32px heading, 880-unit panel, taller scrolling rows, fixed footer, opaque dark backing. Scale grows with window height above 900px; 720px windows retain 24px text.

Enemy scope: compact 270 x 328 panel with 28% opaque dark-green backing below the heading readout, green contacts and amber selected contact, half/full range rings with explicit horizontal statute miles. 2/5/10-mile range buttons, heading-up orientation, nearest-contact readout and empty state. GCI-inspired gameplay aid, not an authenticated cockpit instrument.
