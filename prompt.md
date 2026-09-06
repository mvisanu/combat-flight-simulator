# WWII COMBAT FLIGHT SIMULATOR

## P-51D Mustang vs Mitsubishi A6M Zero

You are the lead Unity game developer, flight-physics engineer, gameplay programmer, AI programmer, technical artist, optimization engineer, and UI developer for this project.

Your job is to build a complete playable WWII combat flight simulator where the player pilots a **North American P-51D Mustang** against AI-controlled **Mitsubishi A6M Zero fighters**.

Do not only explain how to build the game.

Actually inspect the Unity project, create the required files, implement the systems, connect them, compile them, test them, and move the project toward a playable build.

The target experience should feel like a lightweight combination of:

* IL-2 Sturmovik
* War Thunder Simulator
* DCS-style aircraft handling

But the project must remain much lighter, easier to run, easier to maintain, and easier to develop.

The game should prioritize:

1. Smooth performance
2. Responsive controls
3. Believable aircraft physics
4. Strong 3D visuals
5. Good enemy AI
6. Fast loading
7. Modular C# code
8. Easy expansion

---

# 1. REQUIRED TECHNOLOGY STACK

Use this stack unless the existing project requires a compatible alternative.

## Engine

Unity 6 LTS or the latest stable Unity 6 version installed in the project.

Prefer:

Unity 6.3 LTS if installed and stable.

Do not upgrade the Unity project automatically unless necessary.

---

## Programming Language

C#

Do not introduce another gameplay programming language.

---

## Rendering

Use:

Universal Render Pipeline

URP

Do NOT use HDRP unless the existing project already depends heavily on it.

Do NOT use the legacy Built-In Render Pipeline for a new project.

The goal is:

High visual quality

*

Lower GPU/CPU cost

*

Faster iteration

*

Smaller project complexity

---

# 2. TARGET PLATFORM

Primary target:

Windows PC

Graphics API:

DirectX 12 when supported.

Fallback:

DirectX 11 if necessary.

Target frame rates:

Minimum:
60 FPS

Preferred:
90–120 FPS on capable hardware.

The simulation should remain playable on midrange gaming hardware.

Recommended target:

CPU:
Ryzen 5 5600 / Intel i5-12400 or equivalent

RAM:
16 GB

GPU:
RTX 3060 / RX 6700 XT class or equivalent

VRAM:
8 GB recommended

SSD recommended.

---

# 3. CORE DEVELOPMENT PHILOSOPHY

Build the game with:

GameObjects

*

Rigidbody physics

*

Custom aerodynamic calculations

*

C# AI

*

Burst/Jobs only where beneficial

Do NOT begin by converting the entire project to ECS/DOTS.

Do NOT create unnecessary architecture complexity.

Use normal Unity GameObjects for the first playable version.

Use optimization systems only where they provide measurable benefit.

---

# 4. PERFORMANCE RULES

Avoid unnecessary:

Instantiate

Destroy

FindObjectOfType

FindObjectsOfType

Resources.Load during gameplay

LINQ inside hot update loops

garbage allocations inside Update

garbage allocations inside FixedUpdate

full-scene searches

per-frame GetComponent calls

Use cached component references.

Use object pooling.

Use controlled AI update intervals.

Use LOD systems.

Use GPU instancing.

Use batching.

Use Burst and Jobs selectively.

---

# 5. CORE GAME

The player pilots:

## North American P-51D Mustang

Primary enemy:

## Mitsubishi A6M Zero

Initial scenario:

Player:
1 P-51D Mustang

Enemies:
4 A6M Zero fighters

Environment:

Pacific Ocean

Islands

Clouds

Atmospheric haze

Sunlight

Large open airspace

Starting altitude:

Approximately 10,000 feet.

Starting speed:

Approximately 250 mph.

Enemy distance:

Approximately 4 km.

Objective:

Destroy all enemy Zero fighters.

Victory:

All enemy aircraft destroyed.

Defeat:

Player crashes or aircraft becomes combat ineffective.

---

# 6. CORE PLAYER EXPERIENCE

The player should immediately feel the difference between the P-51 and Zero.

P-51:

Fast

Heavy

Excellent dive performance

Good energy retention

Strong high-speed control

Powerful guns

Poorer low-speed turning compared with Zero

Zero:

Light

Very maneuverable

Excellent low-speed turning

Lower maximum speed

Weaker high-speed handling

Poorer high-speed dive performance

Less durable

The player should learn naturally:

Do not try to continuously turn with a Zero.

Use:

Speed

Altitude

Dive attacks

Boom-and-zoom tactics

Energy management

---

# 7. PROJECT INSPECTION FIRST

Before writing new systems:

Inspect the entire project.

Identify:

Unity version

render pipeline

existing aircraft

existing prefabs

existing scripts

input systems

camera systems

UI

terrain

materials

textures

sounds

particles

existing scenes

installed packages

available Asset Store content

Do not replace existing working systems unnecessarily.

Reuse useful systems where possible.

Create a file:

DEVELOPMENT_LOG.md

Record:

Existing systems found

Assets found

Packages found

Systems reused

Systems replaced

Missing systems

---

# 8. PROJECT FOLDER STRUCTURE

Prefer a clear structure similar to:

Assets/

Game/

Aircraft/

P51/

Zero/

Common/

AI/

Combat/

Weapons/

Damage/

Physics/

Input/

Camera/

UI/

Missions/

Environment/

Audio/

Effects/

Materials/

ScriptableObjects/

Prefabs/

Scenes/

Debug/

Editor/

Do not reorganize third-party Asset Store folders unnecessarily.

---

# 9. PLAYER INPUT

Use:

Unity Input System

Support:

Keyboard

Mouse

Xbox controller

PlayStation controller

HOTAS

Joystick

Throttle

Rudder pedals

Keyboard defaults:

W = pitch down

S = pitch up

A = roll left

D = roll right

Q = rudder left

E = rudder right

Left Shift = increase throttle

Left Ctrl = decrease throttle

Space = fire guns

F = flaps

G = landing gear

B = brakes

C = camera change

V = cockpit/external camera

Tab = target selection

R = restart mission

Escape = pause

Allow bindings to be remapped.

---

# 10. FLIGHT PHYSICS

Do NOT implement arcade movement by directly rotating Transform.

Use Rigidbody physics.

Create a custom aerodynamic model.

Primary components:

AircraftController.cs

AircraftPhysics.cs

AircraftEngine.cs

AerodynamicSurface.cs

AircraftInput.cs

AircraftData.cs

---

# 11. AERODYNAMIC FORCES

Model:

Lift

Drag

Thrust

Gravity

Control forces

Angle of attack

Stall

Air density

Altitude

Velocity

Basic lift relationship:

Lift approximately follows:

0.5 × air density × velocity² × wing area × lift coefficient

Basic drag relationship:

Drag approximately follows:

0.5 × air density × velocity² × reference area × drag coefficient

Do not pursue mathematically perfect aerodynamics at the cost of stability or gameplay.

Prioritize:

Believable

Predictable

Stable

Tunable

---

# 12. AERODYNAMIC SURFACES

Create reusable aerodynamic surfaces.

Examples:

Left wing

Right wing

Horizontal stabilizer

Vertical stabilizer

Ailerons

Elevator

Rudder

Flaps

Each surface may define:

Area

Lift coefficient

Drag coefficient

Stall angle

Control authority

Surface orientation

Damage multiplier

---

# 13. ANGLE OF ATTACK

Calculate aircraft angle of attack.

Angle of attack should affect:

Lift

Drag

Control effectiveness

Stall behavior

HUD/debug information

At extreme AoA:

Lift should decrease.

Drag should increase.

Aircraft should buffet.

Control authority should decrease.

A wing may drop.

---

# 14. STALLS

Implement realistic but playable stalls.

The plane should not simply stop flying at one exact speed.

Stall depends on:

Airspeed

Angle of attack

Aircraft configuration

Control input

Damage

Possible stall behavior:

Nose drop

Wing drop

Reduced control authority

Spin tendency

Aircraft should be recoverable.

---

# 15. SPIN BEHAVIOR

Implement a simplified spin model.

Spin may occur when:

Aircraft is stalled

*

Large yaw imbalance exists

*

One wing stalls more severely than the other

Allow recovery through correct control input and gaining speed.

Do not automatically recover the player.

---

# 16. ENGINE

Create:

AircraftEngine.cs

For first version implement:

Throttle

Engine power

RPM

Propeller thrust

Altitude effect

Engine damage

Engine failure

Later architecture should allow:

Mixture

Propeller pitch

Manifold pressure

Fuel consumption

Coolant temperature

Oil temperature

Overheating

---

# 17. P-51 CHARACTERISTICS

Create a ScriptableObject:

P51D_Data

Tune approximately around historical behavior but prioritize gameplay balance.

Characteristics:

Higher speed

Higher mass

Strong dive acceleration

High energy retention

Good high-speed roll

Good high-altitude performance

Higher structural strength

Less effective low-speed turning compared with Zero

Armament:

6 × .50 caliber Browning machine guns

---

# 18. ZERO CHARACTERISTICS

Create:

A6MZero_Data

Characteristics:

Lower mass

Lower wing loading

Tight turning radius

Excellent low-speed maneuverability

Lower maximum speed

Lower dive limit

Lower structural durability

Reduced control authority at excessive speed

Armament configuration:

2 × 20 mm cannon

2 × machine guns

Use reusable weapon components so armament can be changed easily.

---

# 19. SCRIPTABLEOBJECT AIRCRAFT DATA

Create an AircraftData ScriptableObject.

Suggested properties:

Aircraft name

Mass

Wing area

Engine power

Maximum recommended speed

Stall reference speed

Maximum lift coefficient

Base drag coefficient

Induced drag coefficient

Pitch authority

Roll authority

Yaw authority

Maximum structural G

Maximum safe dive speed

Engine configuration

Weapon configuration

Fuel capacity

Damage multipliers

Use configuration assets instead of hardcoding aircraft values.

---

# 20. WEAPON SYSTEM

The P-51 uses six machine guns.

Prioritize performance.

Use:

Raycast / calculated ballistic hit system

*

Pooled tracer visuals

Do NOT create a Rigidbody GameObject for every bullet.

Architecture:

AircraftWeaponSystem.cs

MachineGun.cs

WeaponData.cs

TracerPool.cs

GunConvergence.cs

HitResult.cs

---

# 21. BULLET BALLISTICS

Even if hit detection is ray-based, model reasonable bullet travel.

Account for:

Muzzle velocity

Range

Spread

Convergence

Target velocity

Tracer travel time

Damage falloff if appropriate

Do not make machine guns behave like instantaneous laser beams visually.

---

# 22. GUN CONVERGENCE

P-51 wing guns should converge.

Configurable range:

Approximately 250–350 meters.

Expose convergence distance in Inspector.

Left-side guns should aim slightly right.

Right-side guns should aim slightly left.

They should intersect near the configured distance.

---

# 23. AMMUNITION

Track ammunition.

Display ammunition on HUD.

Support:

Rounds remaining

Rate of fire

Reload state if future aircraft use reloadable weapons

Out-of-ammo state

P-51 should not have unlimited ammunition by default.

Provide unlimited-ammo option for training mode.

---

# 24. TRACERS

Use pooled tracer visuals.

Do not spawn tracers for every single round.

Example:

Every 3rd–5th projectile may be visually represented as a tracer.

Make tracer ratio configurable.

---

# 25. DAMAGE MODEL

Do not use only:

Health = 100

Create component-based aircraft damage.

Damage zones:

Engine

Cockpit

Pilot

Fuel tank

Left wing

Right wing

Left aileron

Right aileron

Elevator

Rudder

Horizontal stabilizer

Vertical stabilizer

Fuselage

---

# 26. DAMAGE EFFECTS

Engine damage:

Reduced power

Smoke

Overheating

Failure

Wing damage:

Reduced lift

Higher drag

Asymmetric handling

Possible structural loss

Control surface damage:

Reduced authority

Fuel damage:

Fuel leak

Smoke

Possible fire

Pilot damage:

Loss of aircraft control

Structural damage:

Wing or tail separation

---

# 27. STRUCTURAL DAMAGE

Monitor:

G forces

Overspeed

Damage

Allow excessive stress to break components.

Do not make aircraft instantly explode every time health reaches zero.

Possible kills:

Engine fire

Pilot incapacitation

Wing loss

Tail loss

Fuel fire

Unrecoverable spin

Crash

Explosion

---

# 28. AI SYSTEM

Do not use an online AI model or LLM for real-time flight decisions.

Use fast deterministic C# AI.

Architecture:

FighterAIController.cs

FighterAIStateMachine.cs

FighterAIPerception.cs

FighterAITactics.cs

FighterAIAiming.cs

FighterAIFlightControl.cs

FighterAIManeuver.cs

SquadronController.cs

---

# 29. AI STATE MACHINE

Suggested states:

Patrol

Search

Intercept

Approach

Attack

Pursuit

Break

Evade

Reposition

Climb

Dive

RegainEnergy

DefensiveTurn

OvershootRecovery

Damaged

Disengage

Crash

Dead

Do not make every aircraft think every frame.

Use configurable AI decision intervals.

Example:

High-level decisions:

5–10 times per second

Physics control:

FixedUpdate

---

# 30. AI PERCEPTION

AI detects aircraft based on:

Distance

Field of view

Relative direction

Visibility state

Recent target memory

Do not make the AI magically omniscient before combat begins.

Once the fight is active, reasonable persistent tracking is acceptable.

---

# 31. AI TARGET INTERCEPT

Do not aim directly at target position.

Calculate approximate intercept using:

Target position

Target velocity

Shooter position

Projectile velocity

Estimate future target position.

Use that position for firing solution.

Add aiming error by difficulty.

---

# 32. AI MANEUVERS

Create reusable maneuvers:

Pure pursuit

Lead pursuit

Lag pursuit

Break turn

High yo-yo

Low yo-yo

Immelmann

Split-S

Scissors

Rolling scissors

Barrel roll defense

Dive escape

Climbing reposition

Energy extension

Overshoot defense

Not every maneuver needs perfect fighter-pilot mathematics.

They need believable behavior.

---

# 33. ZERO AI TACTICS

Zero AI should favor:

Low-speed turning

Inside turns

Horizontal maneuvering

Scissors

Overshoot forcing

Aggressive close-range fighting

Avoid:

Long high-speed dives

Chasing Mustang indefinitely in a straight line

Attempting to match Mustang high-speed energy indefinitely

---

# 34. P-51 AI TACTICS

If friendly or enemy P-51 AI is later added:

Favor:

Speed

Altitude advantage

Boom-and-zoom

Dive attack

Extension after attack

Climb back to altitude

Avoid prolonged low-speed horizontal fights.

---

# 35. AI ENERGY SYSTEM

Calculate approximate combat energy.

Consider:

Altitude

Velocity

Energy advantage

Energy disadvantage

Example decisions:

High energy advantage:

Attack

Dive

High yo-yo

Low energy:

Disengage

Dive for speed

Extend

Avoid excessive turning

AI behavior should emerge partly from energy state.

---

# 36. AI DIFFICULTY

Create difficulty presets.

Rookie:

Slow reactions

Poor aim

Limited tactics

Regular:

Balanced

Veteran:

Good energy management

Good aim

Advanced tactics

Ace:

Excellent positioning

Fast reactions

Strong tactical choices

Never give higher difficulties impossible aircraft performance.

Do not increase:

Engine thrust artificially

Turn rate artificially

Maximum speed artificially

AI must obey aircraft physics.

---

# 37. SQUADRON AI

For groups of fighters:

Create basic squad behavior.

Support:

Leader

Wingman

Attacker

Support role

Aircraft should avoid:

All four fighters occupying exactly the same path

All attacking from exactly the same angle

Constant collisions

Wingman should sometimes assist a teammate under attack.

---

# 38. CAMERA SYSTEM

Use:

Cinemachine if available.

Otherwise implement lightweight custom camera logic.

Camera modes:

Cockpit

Chase

Target view

Flyby

Free look

Primary:

Cockpit camera

Third-person chase camera

---

# 39. CHASE CAMERA

Camera should:

Smoothly follow aircraft

Respond to speed

Allow mouse look

Avoid excessive lag

Not cause motion sickness

Optional effects:

Small vibration during firing

Stall buffet

Engine damage vibration

High-speed airflow movement

Keep camera effects subtle.

---

# 40. COCKPIT CAMERA

Support:

Mouse look

Head rotation

Optional TrackIR-style architecture later

Optional VR architecture later

Do not implement VR in first milestone unless existing project already uses XR.

---

# 41. COCKPIT INSTRUMENTS

If cockpit model exists, animate:

Airspeed

Altitude

RPM

Heading

Vertical speed

Fuel

Throttle

Ammo where appropriate

If cockpit art is missing:

Create functional HUD values first.

Do not block the game waiting for cockpit artwork.

---

# 42. HUD

Create a lightweight HUD.

Display:

Airspeed

Altitude

Heading

Throttle

Ammo

Selected target

Target range

Mission objective

Enemy aircraft remaining

Optional:

G-force

AoA

Vertical speed

Engine health

Aircraft damage warning

---

# 43. GUNSIGHT

Create a WWII-style reflector gunsight.

Provide:

Center aiming reticle

Gun convergence reference

Optional training lead indicator

Allow lead indicator to be disabled for realism.

---

# 44. TARGETING

Tab cycles enemy aircraft.

Targeting system should provide:

Target reference

Distance

Relative direction

Screen indicator

Optional off-screen indicator

Do not add modern radar-style functionality unless configured as an accessibility aid.

---

# 45. ENVIRONMENT

Build a visually strong but performance-efficient Pacific environment.

Include:

Ocean

Several islands

Cloud formations

Sunlight

Atmospheric haze

Distance fog

Sky

Combat altitude variation

---

# 46. WORLD SIZE

Target a playable area roughly:

20–50 km across initially.

The game should support expansion beyond this.

Do not create enormous world geometry at full detail.

Use LOD.

---

# 47. FLOATING ORIGIN

Implement floating-origin support early.

The player aircraft should remain near Unity world origin.

When player travels far enough:

Shift environment/world objects.

Purpose:

Reduce floating-point precision errors.

Prevent:

Cockpit jitter

Aircraft shaking

Bullet inaccuracies

Physics instability

Camera jitter

Create something similar to:

FloatingOriginSystem.cs

Expose recenter distance.

---

# 48. TERRAIN

Use efficient terrain.

Prefer:

Unity Terrain

or

optimized mesh terrain

Use:

LOD

GPU instanced vegetation

distance culling

low-detail distant islands

Do not render tiny vegetation at aircraft cruising altitude unnecessarily.

---

# 49. OCEAN

Create a performant ocean shader/system.

Visual priorities:

Reflection

Sun glint

Normal-map waves

Color variation

Foam near shore if affordable

Distance blending

Do not use extremely expensive real-time fluid simulation.

---

# 50. CLOUDS

Clouds are extremely important visually.

Use a performant cloud solution appropriate to URP.

Clouds should provide:

Depth

Soft lighting

Sun response

Layered altitude

Distance fading

Optional cloud shadows

Avoid destroying frame rate.

Provide graphics quality options.

Low:

Simple clouds

Medium:

Layered clouds

High:

Higher quality volumetric effect if available

---

# 51. ATMOSPHERE

Aircraft combat scenes need strong atmospheric depth.

Use:

Distance haze

Fog

Sunlight scattering

Sky gradients

Horizon blending

Color grading

Cloud lighting

Far aircraft should naturally become harder to see.

---

# 52. LIGHTING

Use one primary directional sun.

Avoid unnecessary real-time lights.

Aircraft may use:

Reflection probes

Light probes

Screen-space effects

PBR materials

Do not create expensive lighting systems unnecessarily.

---

# 53. AIRCRAFT MATERIALS

Use physically based materials.

Aircraft should support:

Base color

Normal maps

Metallic

Roughness/smoothness

Ambient occlusion

Weathering

Oil stains

Panel variation

Paint wear

Keep textures optimized.

---

# 54. AIRCRAFT TEXTURE QUALITY

Near aircraft:

4K allowed.

Cockpit:

2K–4K depending on visibility.

Distant aircraft:

Use mipmaps and LOD appropriately.

Do not use 8K textures unless there is a strong demonstrated reason.

---

# 55. AIRCRAFT LOD

Create:

LOD0

Highest detail.

LOD1

Reduced polygons.

LOD2

Much lower detail.

LOD3

Very low detail.

Optional impostor/distant representation.

Aircraft may be several kilometers away.

Do not render cockpit/interior geometry for distant AI aircraft.

---

# 56. CONTROL SURFACE VISUALS

Animate:

Ailerons

Elevator

Rudder

Flaps

Landing gear

Propeller

Control surfaces must match physical control inputs.

---

# 57. PROPELLER

Support:

Slow visible propeller

Fast blurred propeller

Transition based on RPM.

Do not rely on expensive simulations.

---

# 58. LANDING GEAR

Implement:

Raise

Lower

Animation

Drag effect

Wheel collider or suitable ground handling

Optional unsafe-speed gear damage.

---

# 59. FLAPS

Implement:

Visual animation

Lift increase

Drag increase

Speed restrictions

Flap positions may be simplified initially.

---

# 60. AUDIO

Implement modular aircraft audio.

Sounds:

Engine idle

Engine cruise

High RPM

Wind

Gunfire

Bullet impacts

Structural stress

Stall buffet

Damage

Fire

Explosion

Water crash

Use AudioSource pooling if many simultaneous sounds occur.

---

# 61. ENGINE SOUND

Do not simply play one repeating engine clip.

Blend or adjust:

Pitch

Volume

Multiple layers

Based on:

RPM

Throttle

Aircraft speed

Cockpit/external camera

---

# 62. PARTICLES

Use pooled particles.

Effects:

Muzzle flash

Tracer

Impact spark

Smoke

Engine smoke

Fire

Debris

Explosion

Water impact

Avoid spawning excessive persistent particles.

---

# 63. COLLISIONS

Handle:

Terrain collision

Water collision

Aircraft collision

Projectile hit

Ground collision

High-speed impact should cause severe damage.

---

# 64. WATER IMPACT

Ocean crash should create:

Large splash

Spray

Optional debris

Aircraft destruction

Mission defeat.

Avoid expensive real-time water physics.

---

# 65. G FORCE

Calculate approximate G-force.

Display optional G meter.

Use G-force for:

Aircraft structural limits

Optional pilot blackout later

Do not implement complex human physiology initially.

---

# 66. MISSION SYSTEM

Create reusable mission definitions.

Use:

MissionDefinition ScriptableObject

Properties:

Mission name

Description

Player aircraft

Player spawn position

Player spawn altitude

Player heading

Player speed

Enemy groups

Friendly groups

Weather

Time

Objective

Victory condition

Defeat condition

---

# 67. INITIAL MISSION

Create:

PACIFIC FIGHTER SWEEP

Player:

1 × P-51D

Enemy:

4 × A6M Zero

Altitude:

10,000 feet

Player speed:

250 mph

Initial enemy range:

Approximately 4 km

Objective:

Destroy all enemy fighters.

Victory:

All enemy fighters destroyed.

Defeat:

Player destroyed or crashes.

---

# 68. GAME FLOW

Implement:

Main Menu

↓

Mission Selection

↓

Loading

↓

Mission

↓

Victory / Defeat

↓

Results

↓

Retry / Main Menu

Keep transitions simple and fast.

---

# 69. MAIN MENU

Initial menu:

Quick Mission

Settings

Controls

Quit

Campaign may be added later.

---

# 70. RESULTS

Show:

Kills

Shots fired

Hits

Accuracy

Mission time

Aircraft damage

Optional:

Maximum G

Highest speed

Score

---

# 71. PAUSE MENU

Escape opens:

Resume

Restart

Controls

Settings

Main Menu

Quit

---

# 72. SETTINGS

Graphics:

Resolution

Fullscreen

VSync

Frame limit

Render scale

Texture quality

Shadow quality

Cloud quality

Terrain quality

Anti-aliasing

Effects quality

Audio:

Master

Engine

Weapons

Effects

UI

Gameplay:

Difficulty

HUD

Lead indicator

Unlimited ammo

Simplified damage

Advanced flight

---

# 73. GRAPHICS PRESETS

Create:

LOW

MEDIUM

HIGH

ULTRA

Low should prioritize performance.

Ultra can increase:

Cloud quality

Shadow range

Terrain LOD

Texture quality

Render scale

Reflection quality

Particle density

Do not change aircraft physics based on graphics quality.

---

# 74. ANTI-ALIASING

Use an anti-aliasing method supported efficiently by URP.

Prefer stable visuals for:

Aircraft silhouettes

Wing edges

Distant targets

Do not enable an extremely expensive method by default.

---

# 75. POST PROCESSING

Use subtle:

Bloom

Color grading

Exposure

Vignette if appropriate

Motion blur optional

Do NOT overuse effects.

Aircraft visibility is important.

---

# 76. OPTIMIZATION

Use Unity Profiler regularly.

Check:

CPU

GPU

GC allocations

Physics

Rendering

Scripts

Memory

Do not optimize based purely on guesses.

Measure.

---

# 77. OBJECT POOLING

Pool:

Tracers

Impacts

Muzzle flashes

Smoke bursts

Explosions

Debris

Temporary markers

Do not Instantiate/Destroy continuously during combat.

---

# 78. AI OPTIMIZATION

Not all AI logic needs to run every frame.

Use:

FixedUpdate for flight control.

Timed updates for tactical decisions.

Distance-based update rates may be used.

Example:

Nearby combat aircraft:

10 tactical updates/second.

Far aircraft:

2–5 tactical updates/second.

Very distant aircraft:

Lower frequency.

---

# 79. PHYSICS OPTIMIZATION

Use:

Simple colliders

Limited Rigidbody count

Proper collision layers

Layer masks

Avoid unnecessary MeshColliders on moving aircraft.

Prefer compound primitive colliders where possible.

---

# 80. RENDER OPTIMIZATION

Use:

GPU instancing

LOD groups

Frustum culling

Occlusion where useful

GPU Resident Drawer if compatible

SRP Batcher

Terrain detail distance

Vegetation culling

Particle limits

Do not render cockpit interior for AI aircraft.

---

# 81. JOB SYSTEM / BURST

Use Burst/Jobs only when beneficial.

Good future candidates:

Large projectile calculations

Many AI sensor calculations

Large formation calculations

Terrain/object processing

Do not convert ordinary gameplay scripts unnecessarily.

First build clean C#.

Profile.

Then optimize bottlenecks.

---

# 82. DEBUG HUD

Create optional developer HUD.

Show:

FPS

Airspeed

Altitude

Velocity

AoA

Lift

Drag

Thrust

G-force

Engine RPM

Throttle

Aircraft state

Damage state

Target

AI state

AI maneuver

---

# 83. DEBUG VISUALIZATION

Optional gizmos:

Velocity vector

Lift vector

Drag vector

Thrust vector

Wing lift

AI target line

AI desired direction

AI intercept point

Gun convergence

Damage zones

Toggle debugging off in production.

---

# 84. CODE QUALITY

Production code must use:

Clear classes

Small methods

Meaningful names

Reusable components

Serialized tuning values

ScriptableObject configs

Cached references

Null checks

Comments where logic is non-obvious

Avoid:

God classes

Huge MonoBehaviours

Thousands of lines in one script

Copy/paste aircraft controllers

Magic numbers

---

# 85. SHARED AIRCRAFT ARCHITECTURE

P-51 and Zero should use the SAME core flight system.

Differences should come primarily from configuration.

For example:

AircraftController

AircraftPhysics

AircraftEngine

AerodynamicSurface

WeaponSystem

DamageModel

Then:

P51D_Data

Zero_Data

Do not build two unrelated flight systems.

---

# 86. REQUIRED CORE FILES

Expected systems may include:

AircraftController.cs

AircraftPhysics.cs

AircraftEngine.cs

AircraftInput.cs

AircraftData.cs

AerodynamicSurface.cs

AircraftDamage.cs

DamageZone.cs

AircraftWeaponSystem.cs

MachineGun.cs

WeaponData.cs

TracerPool.cs

TargetingSystem.cs

FighterAIController.cs

FighterAIStateMachine.cs

FighterAIAiming.cs

FighterAIPerception.cs

FighterAITactics.cs

SquadronController.cs

CameraController.cs

CockpitController.cs

FlightHUD.cs

MissionManager.cs

MissionDefinition.cs

CombatManager.cs

GameManager.cs

FloatingOriginSystem.cs

AircraftAudio.cs

AircraftEffects.cs

Do not create empty classes merely to satisfy this list.

Only create files when they contain real functionality.

---

# 87. FIRST PLAYABLE BUILD

Do not attempt every future feature before creating a playable game.

First playable version must include:

Flyable P-51

Believable flight physics

P-51 guns

4 Zero enemies

AI combat

Damage

HUD

Camera

Ocean

Basic islands

Clouds

Victory

Defeat

Restart

---

# 88. DEVELOPMENT PHASE 1

PROJECT INSPECTION

Inspect existing project.

Document:

Assets

Scripts

Packages

Scenes

Aircraft

Environment

UI

Audio

Decide what can be reused.

Compile project before major modifications.

---

# 89. DEVELOPMENT PHASE 2

PLAYER FLIGHT

Create or integrate P-51.

Implement:

Rigidbody

Mass

Center of mass

Lift

Drag

Thrust

Pitch

Roll

Yaw

Throttle

Stall

Camera

Acceptance:

Player can fly for several minutes without unstable physics.

---

# 90. DEVELOPMENT PHASE 3

WEAPONS

Implement:

6 guns

Convergence

Ammo

Raycast/ballistic hit logic

Tracer pool

Hit detection

Damage call

Acceptance:

Player can shoot and damage a target.

---

# 91. DEVELOPMENT PHASE 4

ZERO AIRCRAFT

Create Zero using shared aircraft framework.

Tune differences.

Acceptance:

Zero is visibly and physically more agile at low speed than Mustang.

P-51 should be faster.

---

# 92. DEVELOPMENT PHASE 5

AI

Implement first:

Detection

Intercept

Pursuit

Lead aiming

Fire

Break

Then:

Energy management

Advanced maneuvers

Defensive maneuvers

Acceptance:

Zero can autonomously engage the player.

---

# 93. DEVELOPMENT PHASE 6

DAMAGE

Implement:

Damage zones

Control degradation

Engine damage

Smoke

Fire

Structural damage

Crash state

Acceptance:

Aircraft damage changes behavior.

---

# 94. DEVELOPMENT PHASE 7

MISSION

Create:

Pacific Fighter Sweep

Acceptance:

Mission starts correctly.

Enemies engage.

Victory works.

Defeat works.

Restart works.

---

# 95. DEVELOPMENT PHASE 8

VISUALS

Improve:

Ocean

Islands

Clouds

Sky

Sun

Haze

Aircraft materials

Tracers

Smoke

Fire

Explosions

Maintain target frame rate.

---

# 96. DEVELOPMENT PHASE 9

UI AND AUDIO

Complete:

HUD

Menus

Pause

Results

Audio

Settings

---

# 97. DEVELOPMENT PHASE 10

PERFORMANCE

Profile:

1 vs 4

Then:

1 vs 8

Then:

4 vs 8 if friendly AI exists.

Record:

Average FPS

CPU frame time

GPU frame time

GC allocations

Memory

AI cost

Physics cost

Rendering cost

Fix major bottlenecks.

---

# 98. BASIC FLIGHT ACCEPTANCE TEST

Verify:

Throttle increases thrust.

Speed increases.

Aircraft produces lift.

Pitch works.

Roll works.

Yaw works.

Controls depend on airflow.

Low speed reduces control effectiveness.

Stalls occur.

Stalls are recoverable.

Aircraft does not rotate unrealistically.

Aircraft does not float indefinitely.

Gravity remains meaningful.

---

# 99. P-51 VS ZERO TEST

Verify:

P-51 has:

Higher maximum speed

Better energy retention

Better diving behavior

Higher durability

Zero has:

Tighter low-speed turn

Better low-speed maneuverability

Lower speed

Lower high-speed control effectiveness

A P-51 flat-turning continuously with a Zero should usually be disadvantaged.

A P-51 maintaining speed and altitude should gain tactical advantage.

---

# 100. WEAPON TEST

Verify:

Six P-51 guns fire.

Ammo decreases.

Tracer effects appear.

Convergence is correct.

Hits register.

Damage is applied to the correct aircraft.

Guns do not create excessive garbage allocations.

---

# 101. AI TEST

AI must:

Find player

Approach player

Attempt positional advantage

Lead target

Fire when aligned

Stop firing when badly aligned

Evade when attacked

Avoid obvious collisions

Avoid terrain

Recover from overshoot

Avoid endless turning loops where possible

Obey physics.

---

# 102. DAMAGE TEST

Verify:

Engine hits affect engine.

Wing hits affect lift/control.

Tail damage affects stability.

Aircraft may smoke.

Aircraft may burn.

Aircraft may lose structural parts.

Aircraft can crash without automatically exploding.

---

# 103. PERFORMANCE TEST

Target:

1 P-51

4 Zeroes

60+ FPS minimum on target hardware.

Then test:

8 enemy aircraft.

Look for:

GC spikes

AI spikes

Particle overload

Shadow cost

Cloud cost

Physics spikes

Too many draw calls

---

# 104. NO FAKE IMPLEMENTATIONS

Never mark something complete if it consists only of:

TODO

Pseudocode

Empty class

Debug text

Commented placeholder

A button that does nothing

If a system is incomplete:

Mark it clearly in TODO.md.

---

# 105. ERROR POLICY

After every meaningful coding stage:

Compile.

Check Unity Console.

Fix compile errors.

Fix important warnings.

Enter Play Mode.

Test feature.

Verify existing features still work.

Do not continue implementing new systems while the project has unresolved compile errors.

---

# 106. DEVELOPMENT LOG

Maintain:

DEVELOPMENT_LOG.md

For each major change:

Date

Feature

Files created

Files modified

What works

What was tested

Known bugs

Performance notes

Next task

---

# 107. TODO FILE

Maintain:

TODO.md

Categories:

CRITICAL

HIGH

MEDIUM

LOW

POLISH

Resolve critical gameplay blockers before cosmetic features.

---

# 108. README

Create/update README.md.

Include:

Project overview

Unity version

Controls

How to launch

Game modes

Scenes

Aircraft architecture

Physics architecture

AI architecture

Weapons

Damage

Mission system

Optimization strategy

Known limitations

---

# 109. FUTURE GAME MODES

Architect for future:

Quick Dogfight

Training

Campaign

Historical Missions

Free Flight

Survival

Bomber Escort

Interception

Carrier Defense

Multiplayer

Do not implement multiplayer yet unless existing networking infrastructure is already present.

---

# 110. FUTURE AIRCRAFT

Architecture should support adding:

P-47 Thunderbolt

P-38 Lightning

F4U Corsair

F6F Hellcat

Spitfire

Bf 109

Fw 190

Ki-43

Ki-84

Additional Zero variants

Adding aircraft should mainly require:

New model

New AircraftData

New weapons

Minor aircraft-specific logic only if necessary.

---

# 111. FUTURE WORLD SYSTEMS

Later possibilities:

Aircraft carrier

Battleships

Destroyers

Anti-aircraft fire

Ground vehicles

Airfields

Bombing

Rockets

Bombs

Do not allow these future features to delay the first dogfight.

---

# 112. VISUAL QUALITY GOAL

Create high-end visuals while remaining lighter than an Unreal Engine-style project.

Focus visual budget on what matters for a flight simulator:

Aircraft

Cockpit

Clouds

Atmosphere

Sky

Ocean

Sunlight

Terrain silhouettes

Smoke

Tracers

Explosions

Do not waste resources rendering excessive small ground detail the player cannot see from altitude.

---

# 113. GAMEPLAY QUALITY GOAL

The first combat mission should feel like this:

The player begins at altitude in a P-51 Mustang.

The Merlin engine is running.

Clouds and islands are visible below.

Four Zero fighters appear in the distance.

They detect the Mustang.

The formations close.

The player gains speed.

The Zeroes attempt to turn inside the P-51.

The player dives.

The gunsight crosses an enemy aircraft.

The player fires.

Six .50 caliber guns produce tracer fire.

Rounds strike the Zero.

Smoke appears.

The Zero breaks hard.

Another Zero attacks the Mustang.

The player uses speed and altitude to escape.

The dogfight continues based on:

Physics

Pilot decisions

Energy

Positioning

Damage

AI tactics

This is the core experience.

---

# 114. IMPORTANT AI CODING INSTRUCTION

Do not simply make enemies rotate toward the player.

Enemy aircraft must fly through the same aerodynamic system as the player.

AI should generate virtual pilot control inputs:

Pitch

Roll

Yaw

Throttle

Weapons

Flaps when appropriate

The AI should command the aircraft.

It should not teleport or directly force the aircraft orientation.

---

# 115. IMPORTANT PHYSICS INSTRUCTION

Do not directly set:

transform.rotation

for normal flight movement.

Do not directly set:

transform.position

for normal aircraft movement.

Use Rigidbody forces and torques.

Exceptions:

Initial spawning

Mission reset

Floating-origin world repositioning

Explicit debugging tools

---

# 116. IMPORTANT PERFORMANCE INSTRUCTION

Do not sacrifice 60 FPS for effects that provide little visual benefit.

Whenever choosing between:

A visually expensive implementation

and

A visually similar optimized implementation

prefer the optimized implementation.

Example:

Use raycast bullets + pooled tracer visuals rather than hundreds of projectile Rigidbodies.

Use aircraft LOD rather than full-detail models at 5 km.

Use simplified distant terrain.

Use lower AI decision rates for distant aircraft.

---

# 117. IMPORTANT CHATGPT DEVELOPMENT BEHAVIOR

Do not stop after producing a design document.

Perform actual development.

If tools allow project file modification:

Create the files.

Modify the scripts.

Create components.

Create ScriptableObjects.

Configure scenes.

Configure prefabs.

Compile.

Test.

Fix problems.

If something requires artwork that does not exist:

Use a clean placeholder.

Continue implementing functionality.

Do not repeatedly ask the user for confirmation for ordinary development decisions.

Choose sensible defaults.

Document those choices.

---

# 118. DO NOT DESTROY WORKING SYSTEMS

Before replacing an existing system:

Understand what it does.

Search for references.

Determine dependencies.

Prefer adapting working code.

Do not casually remove packages.

Do not delete assets without a documented reason.

---

# 119. SOURCE CONTROL

Before large modifications, preserve project stability.

If Git is available:

Make logically separated changes.

Avoid mixing unrelated modifications.

Do not commit generated build artifacts unnecessarily.

Maintain an appropriate Unity .gitignore.

---

# 120. FIRST TASK

Start now.

Perform these steps:

1. Inspect the Unity project.

2. Identify Unity version.

3. Identify rendering pipeline.

4. Identify existing aircraft models.

5. Identify P-51 assets.

6. Identify Zero assets.

7. Identify existing physics scripts.

8. Identify camera packages.

9. Identify Input System configuration.

10. Identify terrain/environment assets.

11. Identify audio assets.

12. Identify UI assets.

13. Identify existing scenes.

14. Compile the current project.

15. Record existing errors before making changes.

16. Create DEVELOPMENT_LOG.md.

17. Create TODO.md.

18. Determine the minimum changes required for the first playable P-51.

19. Implement the player flight system.

20. Compile and test it.

Then continue through the development phases automatically.

The immediate priority is:

FLYABLE P-51

Then:

P-51 GUNS

Then:

FLYABLE ZERO AI

Then:

P-51 VS 4 ZERO DOGFIGHT

Do not get distracted by menus, campaign systems, or cosmetic polish before the core dogfight works.
