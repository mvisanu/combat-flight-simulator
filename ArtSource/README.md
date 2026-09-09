# Blender artwork

`PacificAirframes.blend` is the editable Blender 5.1 source for the P-51D, A6M Zero, Bf 109, P-38 Lightning and volcanic island. Separate FBX files provide interchange copies. These are newly authored, historically inspired game models, not licensed scans or blueprint-certified restorations. The new aircraft builders live in `Tools/author_extra_aircraft.py`, invoked by the main authoring script.

The mesh assets are authored locally with Blender. The original workflow uses the installed Blender MCP add-on's `execute_code` command over its local socket; `Tools/blender_mcp_client.py` provides that transport. The scripts also support a fresh background Blender process, which avoids changing an open editing session. AI-generated aircraft reference images are stored separately in `References/Generated`, with their prompts and provenance.

To reproduce the artwork, start a dedicated Blender session from the repository with `--python Tools/blender_mcp_start.py`, then run:

```powershell
python Tools/blender_mcp_client.py Tools/author_blender_art.py
```

The authoring script replaces the scene in that dedicated session. It writes the editable blend, FBX interchange files and exact Unity-coordinate mesh JSON. Do not run it in a session containing unsaved personal work. The starter locates the locally installed add-on under the Blender 5.1 user scripts directory; adjust that path for another installation.

In Unity, choose **Pacific Combat > Import Blender artwork**. The importer converts the JSON into mesh/material assets and prefabs beneath `Assets/Game/Resources/Art`. The normal build script imports before validation. Game builds load those prefabs and do not require Blender at runtime. JSON coordinates use X right, Y up and Z forward in metres; the proper rotation from Blender preserves triangle winding. Acceptance checks verify triangle/normal agreement.

Aircraft retain the existing invisible collision/damage volumes and animated flight controls. Fine details are grouped by material to limit draw calls. The lowest-distance-detail level uses the existing simple airframe. Cockpit dial faces and moving indicators are created in Unity so they report actual aircraft state. The Blender island is the central hero landform; additional islands use deterministic eroded meshes and shared terrain, reef and cloud shaders.

## Aircraft nose geometry and previews

`Tools/aircraft_nose_geometry.py` supplies shape-preserving interpolated sections,
tapered spinners with flat backplates, and recessed intake lips. The Mustang and
Bf 109 have continuous inline-engine nose contours; the Zero has a broad annular
radial cowl and a smaller spinner; the P-38J has paired chin intakes and a rounded
central gun pod. Propeller planes in `AircraftFactory.cs` match the authored hubs;
shared tapered airfoil blades replace the old rectangular bars. Anti-glare panels
are projected onto the cowl surface instead of sitting above it as thick plates.

To regenerate without touching an open Blender session:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.1/blender.exe' --background --python Tools/author_blender_art.py
& 'C:/Program Files/Blender Foundation/Blender 5.1/blender.exe' --background ArtSource/PacificAirframes.blend --python Tools/render_aircraft_gallery.py
```

`Previews` contains full-airframe and nose renders of all four actual meshes.
The render script adds preview-only propeller blades and studio lighting; it
does not save those additions into the source blend or runtime mesh exports.
