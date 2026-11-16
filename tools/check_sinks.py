"""Independent rear-cover countersink geometry and native-save verification."""
import argparse
from collections import Counter
from hashlib import sha256
import json
import math
from pathlib import Path
from zipfile import ZipFile
from check_placements import triangles
from check_back import volume


def hits(faces,x,y):
    result=[]
    for a,b,c in faces:
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-12:continue
        u=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den
        v=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den
        if u>=-1e-8 and v>=-1e-8 and u+v<=1+1e-8:
            z=u*a[2]+v*b[2]+(1-u-v)*c[2]
            if all(abs(z-other)>.0001 for other in result):result.append(z)
    return sorted(result)


def verify(path,holes):
    faces=triangles(path);points=set(p for f in faces for p in f);edges=Counter();slopes=[]
    assert all(math.isfinite(v) for p in points for v in p)
    size=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    assert all(abs(a-b)<.001 for a,b in zip(size,[100,85,5])) and abs(min(p[2] for p in points))<.0001
    for a,b,c in faces:
        for p,q in ((a,b),(b,c),(c,a)):edges[(p,q)]+=1
        u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
        normal=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]);length=math.sqrt(sum(t*t for t in normal))
        assert length>1e-9
        if abs(min(p[2] for p in (a,b,c)))<.0001 and .05<max(p[2] for p in (a,b,c))<1.9 and math.hypot(normal[0],normal[1])>.001:
            slopes.append(abs(abs(normal[2])/math.hypot(normal[0],normal[1])-1))
    assert all(n==1 and edges.get((b,a))==1 for (a,b),n in edges.items())
    assert slopes and max(slopes)<.01,(path.name,max(slopes))
    base_area=8500-math.pi*16;removed=0;profile_samples=0;max_profile_error=0
    for hole in holes:
        x,y,r=hole['X'],hole['Y'],hole['Diameter']/2
        extra=hole.get('SlotExtension',0);a=extra/2;depth=hole.get('SinkDepth',0);horizontal=hole.get('Horizontal',False)
        base_area-=math.pi*r*r+2*r*extra
        removed+=depth*depth*(math.pi*r+extra)+math.pi*depth**3/3
        assert not hits(faces,x,y)
        for j in range(24):
            angle=j*math.tau/24;dx,dy=math.cos(angle),math.sin(angle)
            # Support point of the mathematical capsule, independent of mesh construction.
            cx=x+(math.copysign(a,dx) if horizontal else 0);cy=y+(0 if horizontal else math.copysign(a,dy))
            assert not hits(faces,cx+(r-.002)*dx,cy+(r-.002)*dy),(path.name,'blocked bore',hole,j)
            outside=hits(faces,cx+(r+depth+.04)*dx,cy+(r+depth+.04)*dy)
            assert len(outside)==2 and abs(outside[0])<.0001 and abs(outside[1]-2)<.0001,(path.name,'missing surrounding material',hole,j,outside)
            if depth>0:
                for fraction in (.25,.5,.75):
                    measured=hits(faces,cx+(r+depth*fraction)*dx,cy+(r+depth*fraction)*dy)
                    expected=depth*(1-fraction)
                    assert len(measured)==2 and abs(measured[1]-2)<.0001,(path.name,hole,j,measured)
                    error=abs(measured[0]-expected);assert error<.04,(path.name,'incorrect bevel depth/side',hole,j,measured,expected)
                    max_profile_error=max(max_profile_error,error);profile_samples+=1
                for z,radius in ((0,r+depth),(depth,r),(2,r)):
                    loop=[]
                    for p in points:
                        px,py=p[0]-x,p[1]-y
                        distance=math.hypot(max(abs(px)-a,0),py) if horizontal else math.hypot(px,max(abs(py)-a,0))
                        if abs(p[2]-z)<.0001 and abs(distance-radius)<.04:loop.append(p)
                    assert loop
                    actual=[max(p[i] for p in loop)-min(p[i] for p in loop) for i in (0,1)]
                    target=[2*radius+(extra if horizontal else 0),2*radius+(0 if horizontal else extra)]
                    assert all(abs(p-q)<.01 for p,q in zip(actual,target)),(path.name,'mouth or bore size',z,actual,target)
    expected_volume=base_area*2+(93.5*78.5-90.3*75.3)*3-removed;measured_volume=volume(faces)
    assert abs(measured_volume-expected_volume)<3,(measured_volume,expected_volume)
    return {'file':str(path),'result':'PASS','triangles':len(faces),'size_mm':size,'volume_mm3':measured_volume,'analytic_volume_mm3':expected_volume,'depth_profile_samples':profile_samples,'max_profile_error_mm':max_profile_error,'max_bevel_slope_error':max(slopes),'holes':holes}


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--inputs',default='artifacts/tests-1.11.0');parser.add_argument('--output',default='artifacts/sink-files-1.11.0.json');parser.add_argument('--native',action='store_true');args=parser.parse_args();root=Path(args.inputs);results=[]
    if args.native:
        with ZipFile(root/'Original.lithopro') as z:before=json.loads(z.read('project.json'));source=z.read('source.bin')
        with ZipFile(root/'Senkung-Test.lithopro') as z:saved=json.loads(z.read('project.json'));assert source==z.read('source.bin')
        assert saved['Version']==10 and saved['Name']=='Senkung-Test';before['Name']='Senkung-Test';before['Model']['BackMountingHoles'][0]['SinkDepth']=1.4
        assert before==saved,'Unexpected project change'
        result=verify(root/'Senkung-Test – Rückwand.3mf',saved['Model']['BackMountingHoles']);result.update({'format':10,'source_bytes_unchanged':True,'other_settings_unchanged':True,'source_sha256':sha256(source).hexdigest()});results.append(result)
    else:
        for mode in ('Round','Slots','Mixed'):
            holes=[{'X':x,'Y':y,'Diameter':4,'SlotExtension':0 if mode=='Round' or mode=='Mixed' and i==0 else 8,'Horizontal':i%2==1,'SinkDepth':(1.2,.8,.4,0)[i] if mode=='Mixed' else 1} for i,(x,y) in enumerate(((-25,63.75),(25,63.75),(-25,21.25),(25,21.25)))]
            for ext in ('stl','3mf'):results.append(verify(root/f'Sinks-{mode}.{ext}',holes))
    Path(args.output).write_text(json.dumps(results,indent=2),encoding='utf-8');print(f'PASS: {len(results)} independent countersink files; bore and mouth sizes, depth profile, 45-degree faces, volume, closed edges and Z=0.')


if __name__=='__main__':main()
