"""Save a useful initial viewport in the generated Blender source."""
import bpy
from mathutils import Quaternion, Vector
from pathlib import Path

for collection in list(bpy.data.collections):
    if len(collection.objects) == 0 and len(collection.children) == 0:
        bpy.data.collections.remove(collection)
    elif collection.name.startswith(('A6MZero','Bf109','P38Lightning')):
        collection.hide_viewport = True
for obj in bpy.data.objects:
    if obj.name.startswith('Eroded volcanic island'): obj.hide_set(True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.region_3d.view_distance = 16
            area.spaces.active.region_3d.view_location = Vector((0, 0, .2))
            area.spaces.active.region_3d.view_rotation = Quaternion((.82, .39, -.18, -.36)).normalized()
            area.spaces.active.shading.type = 'MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=str(Path(__file__).resolve().parents[1] / 'ArtSource/PacificAirframes.blend'))
print('Editable source opens on the Mustang; toggle aircraft collections to inspect the other types.')
