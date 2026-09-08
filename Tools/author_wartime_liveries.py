"""Representative WWII schemes and projected national insignia (Blender context).
References/provenance: ArtSource/References/WWII-Liveries.md.
"""
from mathutils.bvhtree import BVHTree
from mathutils import Matrix
from mathutils.geometry import tessellate_polygon

zero_grey=mat('A6M2 warm grey green paint',(.58,.60,.48),.08,.59)
zero_dark=mat('Reference Zero dark green paint',(.035,.145,.095),.12,.51)
olive=mat('USAAF olive drab paint',(.19,.22,.105),.10,.59)
neutral_grey=mat('USAAF neutral grey paint',(.36,.39,.39),.08,.63)
rlm_green=mat('Bf109 grey green upper paint',(.28,.31,.26),.08,.56)
rlm_dark=mat('Bf109 grey violet camouflage paint',(.21,.23,.25),.08,.59)
rlm_light=mat('Bf109 pale blue grey paint',(.62,.67,.69),.06,.58)

def wartime_livery(name,obs):
    global objects
    objects=obs
    # Replace placeholder placement with period upper-left/lower-right US markings.
    old=('National roundel','Insignia bars','Blue star','US star','Lightning insignia','Balkenkreuz','Fuselage paint panel')
    for o in list(obs):
        if o.name.startswith(old): obs.remove(o); bpy.data.objects.remove(o,do_unlink=True)
    if name=='A6MZero':
        for o in list(obs):
            if o.data.materials[0]==green:
                o.data.materials[0]=zero_dark
                if o.type=='MESH': split_paint(o,lambda p:p.y<-.09,zero_grey,obs)
            if o.name.startswith(('Fuselage','Panel joint')):
                # Dark radial cowling is an A6M recognition feature.
                if o.type=='MESH' and o.name.startswith('Fuselage'):
                    # Split face regions into separate objects so the JSON material bridge stays simple.
                    split_paint(o,lambda p:p.z>2.25,black,obs)
    elif name=='P51D':
        for o in list(obs):
            if o.name.startswith('Anti glare'): o.data.materials[0]=olive
            if o.name.startswith('Spinner'): o.data.materials[0]=yellow
            if o.name.startswith('Fuselage'):
                split_paint(o,lambda p:p.z>1.5,yellow,obs)
                split_paint(o,lambda p:p.z<1.45 and p.y>.25,olive,obs)
            if o.name.startswith('Fin'): o.data.materials[0]=olive
    elif name=='P38Lightning':
        for o in list(obs):
            if o.name.startswith('P38 spinner'): o.data.materials[0]=red
            if o.name.startswith('P38 anti glare'): o.data.materials[0]=olive
            if o.name.startswith(('P38 central','P38 engine')):
                split_paint(o,lambda p:p.y>.38 and p.z>1.0,olive,obs)
    elif name=='Bf109':
        for o in list(obs):
            if o.name.startswith('Fuselage'):
                split_paint(o,lambda p:p.y<-.20 and p.z>1.5,yellow,obs)
            if 'Bf109' in o.data.materials[0].name:
                o.data.materials[0]=rlm_green
                if o.type=='MESH': split_paint(o,lambda p:p.y<.25,rlm_light,obs)
            if o.name.startswith('Fuselage'):
                split_paint(o,lambda p:p.y<-.20 and p.z>1.5,yellow,obs)
    deps=bpy.context.evaluated_depsgraph_get(); vertices=[]; faces=[]
    for o in obs:
        if not o.name.startswith(('Wing_','Fuselage','Fin','P38 central','P38 boom','P38 engine','P38 twin fin')): continue
        ev=o.evaluated_get(deps); me=ev.to_mesh(); offset=len(vertices)
        for pt in me.vertices:
            p=ev.matrix_world@pt.co; vertices.append(Vector((p.x,p.z,-p.y)))
        faces.extend([tuple(offset+i for i in poly.vertices) for poly in me.polygons]); ev.to_mesh_clear()
    tree=BVHTree.FromPolygons(vertices,faces)
    def patch(label,center,right,up,points,material,offset=.018):
        normal=Vector(right).cross(Vector(up)).normalized(); coords=[]; triangles=[]
        # Conform the entire decal, not only its perimeter: a flat chord through a
        # curved fuselage hid roundel centers and stars inside the skin.
        source=[Vector((u,w,0)) for u,w in points]
        def project(q):
            u,w=q.x,q.y
            p=Vector(center)+Vector(right)*u+Vector(up)*w
            hit=tree.ray_cast(p+normal*10,-normal,20)[0]
            return tuple((hit if hit is not None else p)+normal*offset)
        def triangle(a,b,c,level):
            if level:
                ab=(a+b)*.5;bc=(b+c)*.5;ca=(c+a)*.5
                for t in [(a,ab,ca),(ab,b,bc),(ca,bc,c),(ab,bc,ca)]:triangle(*t,level-1)
            else:
                projected=[Vector(project(q)) for q in (a,b,c)]
                if (projected[1]-projected[0]).cross(projected[2]-projected[0]).length_squared<1e-12:return
                start=len(coords);coords.extend([tuple(q) for q in projected]);triangles.append((start,start+1,start+2))
        for tri in tessellate_polygon([source]):triangle(*[source[q] if isinstance(q,int) else q for q in tri],2)
        return mesh(label,coords,triangles,material,False)
    def rectangle(label,c,r,u,w,h,m,offset=.018):
        patch(label,c,r,u,[(-w/2,-h/2),(w/2,-h/2),(w/2,h/2),(-w/2,h/2)],m,offset)
    def disc(label,c,r,u,size,m,offset=.022):
        patch(label,c,r,u,[(math.cos(k*math.pi/16)*size,math.sin(k*math.pi/16)*size) for k in range(32)],m,offset)
    def stars(c,r,u,size):
        rectangle('US WWII blue bar border',c,r,u,size*3.05,size*.76,navy)
        rectangle('US WWII white bars',c,r,u,size*2.82,size*.53,ivory,.035)
        disc('US WWII blue field',c,r,u,size,navy,.050)
        pts=[(math.sin(k*math.pi/5)*(size*.90 if k%2==0 else size*.36), math.cos(k*math.pi/5)*(size*.90 if k%2==0 else size*.36)) for k in range(10)]
        # Clockwise star outline; reverse it for the same normal as the blue field.
        patch('US WWII white star',c,r,u,list(reversed(pts)),ivory,.075)
    def cross(c,r,u,size):
        for w,h in [(size*2,size*.6),(size*.6,size*2)]: rectangle('German cross white border',c,r,u,w,h,ivory)
        for w,h in [(size*1.76,size*.31),(size*.31,size*1.76)]: rectangle('German Balkenkreuz',c,r,u,w,h,black,.026)
    def lettering(label,body,center,right,up,size,material):
        normal=Vector(right).cross(Vector(up)).normalized()
        data=bpy.data.curves.new(label,'FONT'); data.body=body;data.align_x='CENTER';data.align_y='CENTER';data.size=size;data.resolution_u=3
        o=bpy.data.objects.new(label,data);bpy.context.collection.objects.link(o);register(o,label,material)
        r=Vector(v(right));u=Vector(v(up));n=Vector(v(normal))
        o.matrix_world=Matrix(((r.x,u.x,n.x,0),(r.y,u.y,n.y,0),(r.z,u.z,n.z,0),(0,0,0,1)))
        o.location=v(center)
    if name=='P51D':
        # The user's reference shows the European Lou IV-style yellow nose and invasion bands.
        # Wrap fuselage bands onto the loft instead of floating rectangular overlays.
        for band in range(5):
            z0=-2.65+band*.28; z1=z0+.28;coords=[]
            for z in [z0,z1]:
                for k in range(48):
                    n=Vector((math.cos(k*math.pi/24),math.sin(k*math.pi/24),0));p=Vector((0,0,z))
                    hit=tree.ray_cast(p+n*10,-n,20)[0]; coords.append(tuple((hit if hit is not None else p)+n*.010))
            quads=[(k,(k+1)%48,(k+1)%48+48,k+48) for k in range(48)]
            mesh('Mustang invasion fuselage band',coords,quads,ivory if band%2==0 else black,True)
        for side in [-1,1]:
            for top in [-1,1]:
                for band in range(5):
                    rectangle('Mustang invasion wing band',(side*(1.40+band*.30),top*.10,.12),(1,0,0),(0,0,-top),.30,2.5,ivory if band%2==0 else black,.009)
            lettering('Mustang squadron forward code','E2',(side*.59,.06,-.55),(0,0,-side),(0,1,0),.40,black)
            lettering('Mustang squadron aft code','C',(side*.31,.05,-3.0),(0,0,-side),(0,1,0),.40,black)
            lettering('Mustang nose art','LOU IV',(side*.585,.1,1.45),(0,0,-side),(0,1,0),.21,red)
            lettering('Mustang tail serial','413410',(side*.065,1.16,-3.74),(0,0,-side),(0,1,0),.115,ivory)
    if name=='P38Lightning':
        for side in [-1,1]:
            lettering('Lightning tail serial','426981',(side*2.72,1.12,-5.43),(0,0,-side),(0,1,0),.19,black)
            lettering('Lightning nose number','981',(side*.36,-.02,2.9),(0,0,-side),(0,1,0),.22,black)
    if name=='Bf109':
        coords=[]
        for z in [-2.85,-2.42]:
            for k in range(48):
                n=Vector((math.cos(k*math.pi/24),math.sin(k*math.pi/24),0));p=Vector((0,0,z))
                hit=tree.ray_cast(p+n*10,-n,20)[0];coords.append(tuple((hit if hit is not None else p)+n*.014))
        mesh('Bf109 yellow rear band',coords,[(k,(k+1)%48,(k+1)%48+48,k+48) for k in range(48)],yellow,True)
        for side in [-1,1]:
            lettering('Bf109 outlined number','67',(side*.53,.12,-.85),(0,0,-side),(0,1,0),.52,ivory)
            lettering('Bf109 black number','67',(side*.537,.12,-.85),(0,0,-side),(0,1,0),.46,black)
    if name=='A6MZero':
        for side in [-1,1]: lettering('Zero reference tail code','53-104',(side*.066,1.26,-3.74),(0,0,-side),(0,1,0),.14,ivory)
    if name in ['P51D','P38Lightning']:
        span=3.9 if name=='P51D' else 5.6; z=.20 if name=='P51D' else -.85
        stars((-span,.10,z),(1,0,0),(0,0,-1),.52)
        stars((span,-.12,z),(1,0,0),(0,0,1),.52)
        for side in [-1,1]:
            stars((side*(.4 if name=='P51D' else 2.95),.02,-2 if name=='P51D' else -3.1),(0,0,-side),(0,1,0),.36 if name=='P51D' else .25)
    elif name=='A6MZero':
        for side in [-1,1]:
            for top in [-1,1]: disc('Japan wing Hinomaru',(side*3.9,top*.12,.20),(1,0,0),(0,0,-top),.57,red)
            disc('Japan fuselage Hinomaru',(side*.44,0,-1.8),(0,0,-side),(0,1,0),.40,red)
    else:
        for side in [-1,1]:
            for top in [-1,1]: cross((side*3.5,top*.12,.15),(1,0,0),(0,0,-top),.48)
            cross((side*.42,.03,-1.9),(0,0,-side),(0,1,0),.34)
            # Wartime tail marking, documented as historical aircraft identification.
            c=(side*.06,1.15,-3.52); r=(0,0,-side); u=(0,1,0)
            for border,matl,ofs in [(.037,ivory,.023),(.023,black,.029)]:
                for arm in range(4):
                    angle=math.pi/4+arm*math.pi/2
                    def rot(p): return (p[0]*math.cos(angle)-p[1]*math.sin(angle),p[0]*math.sin(angle)+p[1]*math.cos(angle))
                    patch('German WWII tail marking',c,r,u,[rot(p) for p in [(-border,-border),(.18+border,-border),(.18+border,.18+border),(.18-border,.18+border),(.18-border,border),(-border,border)]],matl,ofs)
        # Irregular upper-wing splinter fields, plus side mottling.
        for side in [-1,1]:
            patch('Bf109 upper camouflage',(side*2.2,.1,.25),(1,0,0),(0,0,-1),[(-1.4,-.3),(-.3,-.6),(.9,-.1),(.6,.6),(-.8,.7)],rlm_dark,.007)
        rng=random.Random(109)
        for side in [-1,1]:
            for k in range(16):
                z=-2.7+rng.random()*4.9; yy=.05+rng.random()*.39
                disc('Bf109 side mottle',(side*.5,yy,z),(0,0,-side),(0,1,0),.035+rng.random()*.05,rlm_dark,.009)
    return obs

def split_paint(o,predicate,material,obs):
    # Paint a subset of original faces without changing their position or winding.
    me=o.data; chosen=[]
    for poly in me.polygons:
        p=o.matrix_world@poly.center; game=Vector((p.x,p.z,-p.y))
        if predicate(game): chosen.append(poly.index)
    if not chosen: return
    keep=[tuple(p.vertices) for p in me.polygons if p.index not in chosen]
    selected=[tuple(me.polygons[i].vertices) for i in chosen]
    pts=[tuple(o.matrix_world@v.co) for v in me.vertices]
    new=bpy.data.meshes.new(o.name+' paint region');new.from_pydata(pts,[],selected);new.update()
    region=bpy.data.objects.new(o.name+' paint region',new);bpy.context.collection.objects.link(region);register(region,region.name,material)
    for p in new.polygons:p.use_smooth=True
    replacement=bpy.data.meshes.new(o.name+' painted shell');replacement.from_pydata([tuple(v.co) for v in me.vertices],[],keep);replacement.update();replacement.materials.append(o.data.materials[0]);o.data=replacement
    for p in replacement.polygons:p.use_smooth=True



