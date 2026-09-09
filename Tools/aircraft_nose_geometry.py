"""Continuous airframe profiles, in game coordinates, used by Blender authoring."""

def smooth_stations(stations, steps=5):
    """Shape-preserving cubic interpolation: no bulges between sections."""
    slopes=[]
    for col in range(1,4):
        sec=[(b[col]-a[col])/(b[0]-a[0]) for a,b in zip(stations,stations[1:])]
        tangent=[sec[0]]
        for i in range(1,len(stations)-1):
            left,right=sec[i-1],sec[i]
            h0=stations[i][0]-stations[i-1][0]; h1=stations[i+1][0]-stations[i][0]
            tangent.append(0 if left*right<=0 else 3*(h0+h1)/((2*h1+h0)/left+(h1+2*h0)/right))
        tangent.append(sec[-1]); slopes.append(tangent)
    result=[]
    for i,(a,b) in enumerate(zip(stations,stations[1:])):
        dz=b[0]-a[0]
        for j in range(steps):
            t=j/steps; values=[a[0]+t*dz]
            for col in range(1,4):
                values.append((2*t**3-3*t*t+1)*a[col]+(t**3-2*t*t+t)*dz*slopes[col-1][i]
                              +(-2*t**3+3*t*t)*b[col]+(t**3-t*t)*dz*slopes[col-1][i+1])
            result.append(tuple(values))
    return result+[stations[-1]]

def profile_shell(name,stations,cx,material,close_back=True,close_front=True):
    points=[]; faces=[]; n=64
    for z,w,h,cy in stations:
        points.extend((cx+w*math.cos(k*2*math.pi/n),cy+h*math.sin(k*2*math.pi/n),z) for k in range(n))
    for j in range(len(stations)-1):
        for k in range(n): faces.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
    if close_back: faces.append(tuple(range(n-1,-1,-1)))
    if close_front: faces.append(tuple(range((len(stations)-1)*n,len(stations)*n)))
    return mesh(name,points,faces,material)

def spinner(name,cx,cy,base,length,radius,material):
    # Flat backplate and continuous ogive replace the overlapping ellipsoid.
    stations=[(base+length*t,max(.002,radius*math.cos(t*math.pi/2)),
               max(.002,radius*math.cos(t*math.pi/2)),cy) for t in [i/20 for i in range(21)]]
    return profile_shell(name,stations,cx,material)

def intake(name,cx,cy,z,width,height,depth,material):
    # Rolled lip and recessed throat have actual depth from oblique views.
    profile_shell(name+' lip',[(z-depth,width*.84,height*.85,cy),
        (z-.05,width,height,cy),(z,width*.97,height*.96,cy),
        (z+.005,width*.84,height*.78,cy),(z-.11,width*.82,height*.77,cy)],cx,material,False,False)
    profile_shell(name+' recessed throat',[(z-.12,width*.82,height*.77,cy),
        (z-.115,width*.82,height*.77,cy)],cx,black)

def radial_nose():
    # The Zero's cowl stays broad all the way to its annular opening.
    profile_shell('Radial cowling rolled lip',[
        (3.76,.79,.79,.05),(3.91,.785,.785,.05),(3.99,.75,.75,.05),
        (4.015,.70,.70,.05),(3.98,.645,.645,.05),(3.78,.635,.635,.05),
        (3.64,.635,.635,.05)],0,black,False,False)
    # Overlap the throat and backplate so an oblique game camera cannot see
    # scenery through a gap behind the engine (Blender also shades backfaces).
    profile_shell('Recessed radial engine face',[(3.64,.64,.64,.05),(3.65,.64,.64,.05)],0,black)
    for k in range(14):
        a=k*math.pi/7; x=.45*math.cos(a); y=.05+.45*math.sin(a)
        ellipse('Radial cylinder head',(x,y,3.74),(.105,.105,.09),steel)
        for j in range(3):
            r=.36+j*.09
            tube('Radial cooling fin',[(r*math.cos(a)-.055*math.sin(a),.05+r*math.sin(a)+.055*math.cos(a),3.80),
                 (r*math.cos(a)+.055*math.sin(a),.05+r*math.sin(a)-.055*math.cos(a),3.80)],.009,steel)
    spinner('Spinner',0,.05,3.99,.46,.245,aluminum)
