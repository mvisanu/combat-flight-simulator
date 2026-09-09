"""Render the actual authored geometry, separately from generated reference art.

Run Blender --background ArtSource/PacificAirframes.blend --python this_file.
Does not save or alter the source blend. Runtime propellers are added for preview.
"""
import bpy, math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource'/'Previews'; OUT.mkdir(exist_ok=True)
scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.samples=16; scene.cycles.use_denoising=True
scene.render.resolution_x=1200; scene.render.resolution_y=800; scene.render.resolution_percentage=100
scene.world.color=(.35,.35,.35)
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.32,.39,.48,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.55
for o in bpy.data.objects:
    if o.name.startswith('Eroded volcanic'): o.hide_render=True
camera_data=bpy.data.cameras.new('Gallery camera'); camera=bpy.data.objects.new('Gallery camera',camera_data)
scene.collection.objects.link(camera); scene.camera=camera; camera_data.type='ORTHO'
def aim(obj,point): obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()
for name,location,power,size in [('Key',(-6,-7,12),2600,9),('Fill',(8,-2,7),1800,8),('Rim',(0,8,10),3000,7)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    light=bpy.data.objects.new(name,data);scene.collection.objects.link(light);light.location=location;aim(light,(0,0,0))
preview=[]
for name in ['P51D','A6MZero','Bf109','P38Lightning']:
    for other in ['P51D','A6MZero','Bf109','P38Lightning']: bpy.data.collections[other].hide_render=other!=name
    for o in preview: bpy.data.objects.remove(o,do_unlink=True)
    preview=[]
    engines=[(-2.65,4.12),(2.65,4.12)] if name=='P38Lightning' else [(0,4.02 if name=='A6MZero' else 3.995 if name=='Bf109' else 4.25)]
    for cx,z in engines:
        for k in range(4 if name=='P51D' else 3):
            angle=math.radians(25+k*(90 if name=='P51D' else 120))
            radii=[.18,.35,.65,1,1.3,1.52,1.64,1.67];chords=[.09,.14,.22,.24,.20,.14,.07,.004]
            points=[];faces=[]
            for j,(radius,chord) in enumerate(zip(radii,chords)):
                twist=math.radians(35+(8-35)*radius/1.67)
                for q in range(12):
                    theta=q*math.pi/6;xx=math.cos(theta)*chord*.5;zz=math.sin(theta)*chord*.08
                    x=xx*math.cos(twist)+zz*math.sin(twist);depth=-xx*math.sin(twist)+zz*math.cos(twist)
                    points.append((cx+x*math.cos(angle)-radius*math.sin(angle),-z-depth,.05+x*math.sin(angle)+radius*math.cos(angle)))
                    if j<7:
                        a=j*12+q;b=j*12+(q+1)%12;faces.extend([(a,a+12,b+12),(a,b+12,b)])
            faces.extend([tuple(range(12)),tuple(range(95,83,-1))])
            data=bpy.data.meshes.new('Preview blade');data.from_pydata(points,[],faces);data.update()
            blade=bpy.data.objects.new('Preview blade',data);scene.collection.objects.link(blade)
            data.materials.append(bpy.data.materials['Anti glare charcoal']);data.materials.append(bpy.data.materials['Recognition yellow'])
            for face in data.polygons:
                face.use_smooth=True;face.material_index=1 if min(face.vertices)>=60 else 0
            preview.append(blade)
    camera.location=(-10,-15,7);aim(camera,(0,.3,0));camera_data.ortho_scale=19 if name=='P38Lightning' else 14
    scene.render.filepath=str(OUT/(name+'-airframe.png'));bpy.ops.render.render(write_still=True)
    camera.location=(-4,-11,2.6);aim(camera,(0,-3.1,.1));camera_data.ortho_scale=9 if name=='P38Lightning' else 5.2
    scene.render.filepath=str(OUT/(name+'-nose.png'));bpy.ops.render.render(write_still=True)
print('AIRCRAFT GALLERY COMPLETE')
