"""Additional airframes, executed inside author_blender_art.py's Blender context."""
from mathutils import Matrix

camouflage=mat('Bf109 grey green',(.24,.28,.23),.25,.5)
camouflage_dark=mat('Bf109 mottled panels',(.13,.17,.15),.22,.56)
pale=mat('Bf109 pale underside',(.56,.62,.63),.18,.5)

def discard(obs, predicate):
    for o in list(obs):
        if predicate(o.name.split('.')[0]):
            obs.remove(o); bpy.data.objects.remove(o,do_unlink=True)

def cockpit_part(name):
    return name.startswith(('Canopy','Cockpit','Console','Instrument','Panel coaming','Seat','Harness','Control stick','Stick grip','Reflector'))

def build_bf109():
    global objects
    obs=airframe(False); objects=obs
    discard(obs,lambda n:n.startswith(('National','Insignia','Blue star','US star','Canopy','Radiator')))
    for o in obs:
        if not cockpit_part(o.name): o.matrix_world=Matrix.Diagonal((.88,.94,1,1))@o.matrix_world
        original=o.data.materials[0]
        if original==aluminum: o.data.materials[0]=camouflage
        elif original==panelmetal: o.data.materials[0]=camouflage_dark
        elif original==yellow: o.data.materials[0]=black
    # Faceted greenhouse canopy distinguishes the narrow German cockpit.
    arches=[(-1.02,.35,.85),(-.62,.40,1.39),(.65,.40,1.39),(1.06,.33,.95)]
    points=[]
    for z,w,top in arches: points.extend([(-w,.56,z),(-w*.78,top,z),(w*.78,top,z),(w,.56,z)])
    faces=[]
    for j in range(3):
        for k in range(3): faces.append((j*4+k,(j+1)*4+k,(j+1)*4+k+1,j*4+k+1))
    mesh('Bf109 canopy panes',points,faces,glass)
    for z,w,top in arches:
        tube('Bf109 canopy frame',[(-w,.56,z),(-w*.78,top,z),(w*.78,top,z),(w,.56,z)],.019,camouflage)
    for side in [-1,1]:
        tube('Bf109 canopy rail',[(side*w,.56,z) for z,w,top in arches],.022,camouflage)
        # Flat black cross with ivory border; no political emblem.
        x=side*3.5; y=.09; z=.15
        box('Balkenkreuz white span',(x,y,z),(1.13,.009,.35),ivory,0)
        box('Balkenkreuz white chord',(x,y+.001,z),(.35,.009,1.13),ivory,0)
        box('Balkenkreuz black span',(x,y+.009,z),(.97,.008,.18),black,0)
        box('Balkenkreuz black chord',(x,y+.010,z),(.18,.008,.97),black,0)
        box('Underwing radiator',(side*1.45,-.25,.02),(.64,.28,.92),camouflage_dark,.04)
        box('Radiator intake',(side*1.45,-.23,.50),(.48,.15,.025),black,.012)
        ellipse('Cowling gun blister',(side*.28,.61,1.6),(.14,.08,.31),camouflage)
        tube('Cowl machine gun',[(side*.19,.51,2.15),(side*.19,.51,3.5)],.022,black)
        for j in range(5):
            box('Fuselage paint panel',(side*.40,.1,-1.5-j*.28),(.015,.22,.20),camouflage_dark,.025)
    return obs

def loft(name,stations,cx,material,open_cockpit=False):
    points=[]; faces=[]; n=48
    for z,w,h,cy in stations:
        for k in range(n):
            a=2*math.pi*k/n; points.append((cx+w*math.cos(a),cy+h*math.sin(a),z))
    for j in range(len(stations)-1):
        zmid=(stations[j][0]+stations[j+1][0])/2
        for k in range(n):
            if open_cockpit and -1.1<zmid<1.1 and math.sin(2*math.pi*(k+.5)/n)>.64: continue
            faces.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
    faces.append(tuple(range(n-1,-1,-1))); faces.append(tuple(range((len(stations)-1)*n,len(stations)*n)))
    return mesh(name,points,faces,material)

def build_p38():
    global objects
    obs=airframe(False); objects=obs
    discard(obs,lambda n:not cockpit_part(n))
    pod=[(-2.3,.04,.08,.04),(-1.8,.30,.39,.02),(-1.0,.53,.58,.02),(-.3,.61,.65,0),(.65,.62,.65,0),(1.45,.57,.6,-.01),(2.2,.46,.47,-.02),(3.0,.32,.34,-.02),(3.65,.12,.16,-.02)]
    loft('P38 central pilot and gun pod',pod,0,aluminum,True)
    booms=[(-6.0,.05,.1,.08),(-5.5,.14,.22,.04),(-4.6,.21,.26,0),(-3.5,.27,.29,-.02),(-2.1,.37,.34,-.02),(-.7,.49,.48,-.02),(.8,.57,.61,0),(2.0,.56,.60,.02),(3.1,.49,.51,.02),(4.05,.32,.35,.02)]
    for side in [-1,1]:
        cx=side*2.65
        loft('P38 engine and tail boom',booms,cx,aluminum)
        ellipse('P38 spinner',(cx,.02,4.35),(.34,.34,.51),yellow)
        box('P38 anti glare engine',(cx,.595,1.65),(.54,.025,1.9),black,.035)
        for i in range(6):
            z=1.65+i*.23
            tube('P38 exhaust stack',[(cx+side*.5,.13,z),(cx+side*.68,.12,z-.08)],.052,steel)
        ellipse('Turbocharger inlet',(cx,.34,-1.58),(.24,.13,.50),steel)
        box('Boom cooling scoop',(cx,-.33,-1.15),(.59,.32,.93),panelmetal,.06)
        box('Boom radiator intake',(cx,-.35,-.66),(.43,.19,.03),black,.02)
        for z,w,h,cy in booms[1:-1:2]:
            tube('Boom skin seam',[(cx+w*1.003*math.cos(k*math.pi/24),cy+h*1.003*math.sin(k*math.pi/24),z) for k in range(49)],.003,seam)
        sections=[(.43,1.45,-1.8),(2.65,1.50,-1.78),(4.1,.80,-1.99),(6.35,-.03,-2.10),(7.60,-.62,-2.15),(7.9,-.91,-1.9)]
        wp=[]; wf=[]
        for x,front,back in sections:
            for k in range(32):
                a=k*math.pi/16; t=(1-math.cos(a))*.5
                wp.append((side*x,-.13+x*.014+.14*(1-x/9)*math.sin(a)+.035*math.sin(t*math.pi),front+(back-front)*t))
        for j in range(len(sections)-1):
            for k in range(32): wf.append((j*32+k,j*32+(k+1)%32,(j+1)*32+(k+1)%32,(j+1)*32+k))
        wf.extend([tuple(range(31,-1,-1)),tuple(range((len(sections)-1)*32,len(sections)*32))])
        mesh('Wing_L' if side<0 else 'Wing_R',wp,wf,aluminum)
        for x in [1.3,3.6,5.0,6.5]: tube('Lightning wing panel',[(side*x,.05,1.4-x*.3),(side*x,.065,-.6),(side*x,.02,-2.0)],.003,seam)
        # Twin fixed fins, with room behind for the animated rudders.
        fin=[(cx-.06,.02,-4.7),(cx-.06,1.62,-5.35),(cx-.06,1.78,-5.73),(cx-.06,.05,-5.8),(cx+.06,.02,-4.7),(cx+.06,1.62,-5.35),(cx+.06,1.78,-5.73),(cx+.06,.05,-5.8)]
        mesh('P38 twin fin',fin,[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3)],aluminum)
        # US star and bars on upper outer wings.
        x=side*5.65; y=.094; z=-.99; r=.56
        disk=[(x,y,z)]+[(x+r*math.cos(k*math.pi/32),y,z+r*math.sin(k*math.pi/32)) for k in range(64)]
        box('Lightning insignia bars',(x,y-.008,z),(1.86,.005,.36),ivory,0)
        mesh('National roundel',disk,[(0,k+1,(k+1)%64+1) for k in range(64)],navy,False)
        star=[(x,y+.007,z)]+[(x+(.49 if k%2==0 else .20)*math.sin(k*math.pi/5),y+.007,z+(.49 if k%2==0 else .20)*math.cos(k*math.pi/5)) for k in range(10)]
        mesh('US star',star,[(0,k+1,(k+1)%10+1) for k in range(10)],ivory,False)
    # Stabilizer links the two booms. The elevator is animated by Unity.
    box('P38 connecting stabilizer',(0,.07,-5.30),(5.3,.11,.85),aluminum,.035)
    for x in [-.26,-.10,.10,.26]: tube('P38 nose machine gun',[(x,.03,3.42),(x,.03,3.93)],.024,black)
    tube('P38 nose cannon',[(0,-.16,3.42),(0,-.16,4.08)],.038,black)
    return obs

for aircraft_name,builder in [('Bf109',build_bf109),('P38Lightning',build_p38)]:
    aircraft_objects=wartime_livery(aircraft_name,builder()); export(aircraft_name,aircraft_objects)
    collection=bpy.data.collections.new(aircraft_name); bpy.context.scene.collection.children.link(collection)
    for o in aircraft_objects:
        for c in list(o.users_collection): c.objects.unlink(o)
        collection.objects.link(o)
    collection.hide_render=True
