"""Author editable WWII airframes and volcanic terrain through Blender MCP.

All dimensions below use the game's metres: X right, Y up, Z forward.
The JSON bridge preserves exact coordinates/normals and imports into Unity prefabs.
FBX and .blend files are also retained as editable interchange/source artifacts.
"""
import bpy, math, json, random
from pathlib import Path
from mathutils import Vector

# The MCP transport supplies the source filename; works in any checkout directory.
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'ArtSource'
OUT.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials): bpy.data.materials.remove(material)
random.seed(1944)

def v(p): return (p[0], -p[2], p[1])
def mat(name, rgb, metal=0, rough=.5, alpha=1):
    m=bpy.data.materials.new(name); m.diffuse_color=(*rgb,alpha); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*rgb,alpha)
    p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
    p.inputs['Alpha'].default_value=alpha
    return m
aluminum=mat('Brushed aluminum',(.54,.59,.62),.8,.32)
panelmetal=mat('Alternate aluminum panels',(.43,.49,.52),.78,.38)
green=mat('IJN weathered green',(.055,.115,.081),.25,.55)
underside=mat('Warm grey underside',(.53,.56,.49),.2,.55)
black=mat('Anti glare charcoal',(.019,.026,.025),.1,.68)
rubber=mat('Rubber',(.012,.014,.013),0,.85)
interior=mat('Interior green',(.14,.19,.115),.15,.64)
leather=mat('Seat leather',(.17,.09,.04),0,.75)
ivory=mat('Insignia ivory',(.88,.87,.76),.05,.55)
navy=mat('Insignia navy',(.024,.057,.105),.1,.5)
red=mat('Hinomaru red',(.56,.025,.018),.1,.52)
yellow=mat('Recognition yellow',(.91,.55,.04),.1,.48)
glass=mat('Canopy glass',(.29,.48,.54),.05,.08,.095)
steel=mat('Exhaust steel',(.13,.12,.1),.65,.67)
seam=mat('Panel seams',(.105,.135,.14),.55,.58)
objects=[]
def register(o,name,m):
    o.name=name; o.data.materials.append(m); objects.append(o)
    return o
def mesh(name,points,faces,m,smooth=True):
    flip=name in ['Wing_L','Tailplane_L','National roundel','Blue star field']
    me=bpy.data.meshes.new(name); me.from_pydata([v(p) for p in points],[],[tuple(reversed(f)) if flip else f for f in faces]); me.update()
    o=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(o); register(o,name,m)
    for p in me.polygons: p.use_smooth=smooth
    return o
def box(name,p,s,m,bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=v(p)); o=bpy.context.object; o.scale=(s[0],s[2],s[1])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); register(o,name,m)
    if bevel:
        mod=o.modifiers.new('Machined edges','BEVEL'); mod.width=bevel; mod.segments=2
        o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return o
def tube(name,points,r,m):
    curve=bpy.data.curves.new(name,'CURVE'); curve.dimensions='3D'; curve.resolution_u=1
    curve.bevel_depth=r; curve.bevel_resolution=2
    spline=curve.splines.new('POLY'); spline.points.add(len(points)-1)
    for q,p in zip(spline.points,points): q.co=(*v(p),1)
    ob=bpy.data.objects.new(name,curve); bpy.context.collection.objects.link(ob); register(ob,name,m); return ob
def ellipse(name,p,s,m):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,location=v(p)); o=bpy.context.object; o.scale=(s[0],s[2],s[1]); register(o,name,m)
    for face in o.data.polygons: face.use_smooth=True
    return o

exec((ROOT / 'Tools/aircraft_nose_geometry.py').read_text(encoding='utf-8'), globals())

def airframe(zero):
    global objects
    objects=[]; skin=green if zero else aluminum
    # Cross sections: tail to spinner. Lofted shell leaves an actual open pilot well.
    stations=[(-4.55,.06,.09,.18),(-4.1,.17,.24,.16),(-3.5,.26,.33,.13),(-2.7,.35,.43,.1),(-1.8,.43,.52,.06),(-.95,.51,.60,.03),(-.3,.58,.63,.02),(.65,.61,.66,.02),(1.45,.60,.66,.01),(2.25,.57,.65,.03),(3.25,.52,.57,.05),(3.9,.43,.47,.05),(4.23,.3,.33,.05)]
    stations=stations[:9]+[(2.25,.57,.64,.03),(3.15,.51,.57,.05),
        (3.75,.465,.49,.05),(4.08,.435,.445,.05),(4.23,.43,.43,.05)]
    if zero:
        stations=[(z*.91,w*.96,h*.97,cy) for z,w,h,cy in stations[:9]]+[
            (1.85,.61,.67,.03),(2.35,.73,.75,.05),(2.65,.79,.79,.05),
            (3.3,.80,.80,.05),(3.76,.79,.79,.05)]
    # Reference span/length: T.O. 1F-51D-1 general arrangement and museum A6M2 dimensions.
    z,w,h,cy=stations[0]; stations[0]=(-4.44 if zero else -4.894,w,h,cy)
    panel_stations=list(stations)
    stations=smooth_stations(stations)
    pts=[]; faces=[]; n=48
    for z,w,h,cy in stations:
        for k in range(n):
            a=2*math.pi*k/n; pts.append((w*math.cos(a),cy+h*math.sin(a),z))
    for j in range(len(stations)-1):
        zmid=(stations[j][0]+stations[j+1][0])/2
        for k in range(n):
            amid=2*math.pi*(k+.5)/n
            if -1.1<zmid<1.1 and math.sin(amid)>.64: continue
            faces.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
    mesh('Fuselage',pts,faces,skin)
    if zero:
        radial_nose()
    else:
        spinner('Spinner',0,.05,4.23,.68,.43,yellow)
        intake('Mustang chin inlet',0,-.37,3.91,.22,.095,.38,aluminum)
    # Visible ring seams and rows of flush fasteners on engine and aft fuselage.
    for j in [1,3,4,8,9,10,11]:
        z,w,h,cy=panel_stations[j]
        tube('Panel joint',[(w*1.003*math.cos(k*2*math.pi/n),cy+h*1.003*math.sin(k*2*math.pi/n),z) for k in range(n+1)],.003,seam if not zero else black)
    for side in [-1,1]:
        for j in range(0 if zero else 20):
            z=1.55+j*.1
            ellipse('Cowling fastener',(side*.46,.37,z),(.011,.011,.011),steel)
        # Six exhaust stacks, with darker recessed openings.
        for i in range(0 if zero else 6):
            z=1.85+i*.22
            tube('Exhaust stack',[(side*.49,.2,z),(side*.68,.18,z-.09)],.052,steel)
        if not zero and side == 1:
            box('Anti glare cowling',(0,.64,2.4),( .63,.035,1.62),black,.04)
        span=6.0 if zero else 5.6388
        # Airfoil cross sections with taper, dihedral and rounded tip stations.
        wingpts=[]; wingfaces=[]; sections=[(.43,1.56,-1.22), (1.35,1.44,-1.10),(2.6,1.18,-.90),(4.1,.86,-.65),(span*.94,.58,-.40),(span,.26,-.18)]
        for x,front,back in sections:
            for k in range(32):
                a=2*math.pi*k/32; t=(1-math.cos(a))*.5
                z=front+(back-front)*t
                thick=.12*(1-x/span*.8)*math.sin(a)
                y=-.12+x*.025+thick+.035*math.sin(t*math.pi)
                wingpts.append((side*x,y,z))
        for j in range(len(sections)-1):
            for k in range(32): wingfaces.append((j*32+k,j*32+(k+1)%32,(j+1)*32+(k+1)%32,(j+1)*32+k))
        wingfaces.extend([tuple(range(31,-1,-1)),tuple(range((len(sections)-1)*32,len(sections)*32))])
        mesh('Wing_L' if side<0 else 'Wing_R',wingpts,wingfaces,skin)
        for x in [1.35,2.6,4.1,5.1]:
            y=-.12+x*.025+.055; front=1.65-x*.19; back=-1.25+x*.16
            tube('Wing skin panel',[(side*x,y,front),(side*x,y+.015,(front+back)*.5),(side*x,y,back)],.0035,seam if not zero else black)
        # Gear access covers and ammunition access hatch on upper surface.
        box('Gun access panel',(side*2.55,.024,.34),(.83,.012,.54),green if zero else panelmetal,.018)
        for k in range(3 if not zero else 1):
            tube('Gun barrel',[(side*(2.1+k*.34),-.07,1.17),(side*(2.1+k*.34),-.07,1.55)],.023,black)
        # Flat marking geometry on upper wings; no bulky cylinder primitives.
        cx=side*3.85; yy=.052; cz=.22; radius=.52
        disk=[(cx,yy,cz)]+[(cx+radius*math.cos(k*2*math.pi/64),yy,cz+radius*math.sin(k*2*math.pi/64)) for k in range(64)]
        mesh('National roundel',disk,[(0,k+1,(k+1)%64+1) for k in range(64)],red if zero else navy,False)
        if not zero:
            box('Insignia bars',(cx,yy+.003,cz),(1.72,.004,.35),ivory,0)
            # Blue disk covers the bars centrally; ivory five-point star on top.
            mesh('Blue star field',[(x,y+.009,z) for x,y,z in disk],[(0,k+1,(k+1)%64+1) for k in range(64)],navy,False)
            star=[(cx,yy+.014,cz)]+[(cx+(.46 if k%2==0 else .19)*math.sin(k*math.pi/5),yy+.014,cz+(.46 if k%2==0 else .19)*math.cos(k*math.pi/5)) for k in range(10)]
            mesh('US star',star,[(0,k+1,(k+1)%10+1) for k in range(10)],ivory,False)
        # Tapered tailplane, fixed forward portion only; moving elevator stays in game.
        tail=[(side*.14,.13,-3.13),(side*1.85,.17,-3.50),(side*2.02,.17,-3.92),(side*.14,.13,-3.95)]
        mesh('Tailplane_L' if side<0 else 'Tailplane_R',tail+[(x,y-.08,z) for x,y,z in tail],[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3)],skin)
    fin=[(-.06,.1,-3.05),(-.06,1.67,-3.67),(-.06,1.74,-3.96),(-.06,.13,-4.12),(.06,.1,-3.05),(.06,1.67,-3.67),(.06,1.74,-3.96),(.06,.13,-4.12)]
    mesh('Fin',fin,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3)],skin)
    if not zero:
        ellipse('Radiator housing',(0,-.55,-.75),(.44,.30,.94),aluminum)
        box('Radiator dark intake',(0,-.58,.02),(.64,.27,.045),black,.025)
        for i in range(7): box('Radiator grille',(i*.075-.225,-.58,.048),(.014,.23,.014),steel,.002)
    # Open canopy: thin transparent shell, realistic slender transverse arches.
    arches=[(-1.04,.37,.85),(-.67,.46,1.36),(.2,.47,1.47),(.73,.43,1.39),(1.1,.32,.98)]
    glasspts=[]; glassfaces=[]
    for z,w,top in arches:
        for k in range(25):
            a=math.pi*k/24; glasspts.append((w*math.cos(a),.55+(top-.55)*math.sin(a),z))
    for j in range(len(arches)-1):
        for k in range(24): glassfaces.append((j*25+k,j*25+k+1,(j+1)*25+k+1,(j+1)*25+k))
    mesh('CanopyGlass',glasspts,glassfaces,glass)
    for j,(z,w,top) in enumerate(arches):
        if j in [0,1,3,4] or zero:
            tube('CanopyFrame',[(w*math.cos(k*math.pi/32),.55+(top-.55)*math.sin(k*math.pi/32),z) for k in range(33)],.016,skin)
    for side in [-1,1]:
        tube('Canopy rail',[(side*w,.56,z) for z,w,top in arches],.024,skin)
        box('Cockpit sidewall',(side*.46,.45,-.03),(.06,.40,1.60),interior)
        box('Console',(side*.34,.40,-.15),(.19,.13,.7),black)
        for j in range(5): box('Console switch',(side*.34,.49,-.42+j*.11),(.015,.047,.015),steel,.002)
    box('Instrument panel',(0,.59,.87),(1.0,.76,.055),black,.08)
    # Reference-informed controls: throttle quadrant, radio box and oxygen regulator.
    for j in range(3):
        tube('Throttle quadrant lever',[(-.39+j*.05,.45,.12),(-.39+j*.05,.63,.03)],.012,steel)
        ellipse('Throttle quadrant grip',(-.39+j*.05,.65,.03),(.028,.025,.025),black if j==0 else red if j==1 else ivory)
    box('Radio control box',(.35,.5,-.48),(.17,.14,.22),black,.015)
    for j in range(3): ellipse('Radio knob',(.30+j*.05,.59,-.45),(.014,.017,.014),ivory)
    ellipse('Oxygen regulator',(.40,.52,.35),(.055,.06,.025),steel)
    if zero:
        for side in [-1,1]:
            box('Type 97 breech housing',(side*.26,.94,1.04),(.13,.15,.34),black,.015)
            tube('Zero canopy longitudinal frame',[(side*.24,1.31,-.65),(side*.28,1.39,.2),(side*.22,1.29,.73)],.012,green)
    box('Panel coaming',(0,.94,.88),(1.03,.09,.26),rubber,.045)
    box('Seat back',(0,.40,-.66),(.55,.62,.12),leather,.07)
    box('Seat pan',(0,.13,-.39),(.54,.12,.48),leather,.07)
    for side in [-1,1]: box('Harness',(side*.14,.43,-.583),(.075,.51,.022),ivory,.01)
    box('Cockpit floor',(0,.02,-.05),(.85,.05,1.5),interior)
    tube('Control stick',[(0,.07,-.12),(0,.40,-.06)],.021,black)
    ellipse('Stick grip',(0,.43,-.06),(.033,.07,.035),rubber)
    # Gunsight housing below eye level, transparent reflector screen.
    box('Reflector sight body',(0,1.025,.90),(.13,.12,.20),black,.015)
    box('Reflector glass',(0,1.16,.94),(.18,.15,.007),glass,.002)
    return objects

def export(name, obs):
    deps=bpy.context.evaluated_depsgraph_get(); nodes=[]; used={}
    # Consolidate tiny details by material to bound runtime draw calls.
    groups={}
    for o in obs:
        evaluated=o.evaluated_get(deps); me=evaluated.to_mesh(); me.calc_loop_triangles()
        used_vertices=sorted({i for tri in me.loop_triangles for i in tri.vertices})
        if not used_vertices: evaluated.to_mesh_clear(); continue
        remap={old:new for new,old in enumerate(used_vertices)}
        m=o.data.materials[0]; key=m.name
        group=groups.setdefault(key,{'name':key.replace(' ','_'),'material':key,'vertices':[],'normals':[],'uv':[],'triangles':[]})
        offset=len(group['vertices'])//3
        normal_matrix=evaluated.matrix_world.to_3x3().inverted().transposed()
        for index in used_vertices:
            vert=me.vertices[index]
            p=evaluated.matrix_world@vert.co; n=normal_matrix@vert.normal; n.normalize()
            group['vertices'].extend([round(p.x,6),round(p.z,6),round(-p.y,6)])
            group['normals'].extend([round(n.x,6),round(n.z,6),round(-n.y,6)])
            group['uv'].extend([round(p.x*.5,5),round(-p.y*.5,5)])
        # (x,z,-y) is a proper rotation: its determinant is +1, so preserve winding.
        for tri in me.loop_triangles: group['triangles'].extend([offset+remap[i] for i in tri.vertices])
        evaluated.to_mesh_clear()
        bs=m.node_tree.nodes.get('Principled BSDF')
        used[key]={'name':key,'color':list(m.diffuse_color),'metallic':bs.inputs['Metallic'].default_value,'smoothness':1-bs.inputs['Roughness'].default_value}
    payload={'name':name,'materials':list(used.values()),'nodes':list(groups.values())}
    (OUT/(name+'.json')).write_text(json.dumps(payload,separators=(',',':')))
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active=obs[0]
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','OTHER'},axis_forward='-Z',axis_up='Y',bake_anim=False)
    print(name+': '+str(sum(len(n['triangles'])//3 for n in groups.values()))+' triangles, '+str(len(groups))+' material batches')

exec((ROOT / 'Tools/author_wartime_liveries.py').read_text(encoding='utf-8'), globals())
for name,zero in [('P51D',False),('A6MZero',True)]:
    obs=wartime_livery(name,airframe(zero)); export(name,obs)
    collection=bpy.data.collections.new(name); bpy.context.scene.collection.children.link(collection)
    for o in obs:
        for c in list(o.users_collection): c.objects.unlink(o)
        collection.objects.link(o)
    collection.hide_render=zero

exec((ROOT / 'Tools/author_extra_aircraft.py').read_text(encoding='utf-8'), globals())

# A sculpted volcanic island in normalized coordinates, with radial erosion gullies.
objects=[]
from mathutils.noise import noise_vector, noise
pts=[]; faces=[]; size=144
for j in range(size+1):
    z=(j/size*2-1)*1.15
    for i in range(size+1):
        x=(i/size*2-1)*1.15; r=math.sqrt((x*.92)**2+(z*1.08)**2); a=math.atan2(z,x)
        edge=1+.08*math.sin(a*5)+.055*math.sin(a*9+1)
        radial=r/edge
        base=max(0,1-radial)**1.65*.39
        ridges=(.5+.5*math.sin(a*13+radial*12))*.035*max(0,1-radial)
        detail=noise(Vector((x*9,z*9,1.3)))*.029*max(0,1-radial)
        crater=math.exp(-((x+.12)**2+(z-.06)**2)/.008)*.085
        y=max(-.025,base+ridges+detail-crater-.007)
        pts.append((x,y,z))
for j in range(size):
    for i in range(size):
        p=j*(size+1)+i; faces.extend([(p,p+size+1,p+1),(p+1,p+size+1,p+size+2)])
terrain=mesh('Eroded volcanic island',pts,faces,green)
export('VolcanicIsland',[terrain]); terrain.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'PacificAirframes.blend'))
print('BLENDER AUTHORING COMPLETE')
