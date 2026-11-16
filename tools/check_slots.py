"""Independent capsule-opening checks of exported rear covers (Python standard library)."""
import argparse
from collections import Counter
from hashlib import sha256
import json
import math
from pathlib import Path
from zipfile import ZipFile
from check_placements import triangles
from check_mounting import crossings
from check_back import volume


def verify(path,holes):
    faces=triangles(path);points=set(p for f in faces for p in f)
    assert all(math.isfinite(v) for p in points for v in p)
    size=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    assert all(abs(a-b)<.001 for a,b in zip(size,[100,85,5])),size
    assert abs(min(p[2] for p in points))<.0001
    edges=Counter()
    for a,b,c in faces:
        u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
        assert sum(t*t for t in (u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]))>1e-18
        for p,q in ((a,b),(b,c),(c,a)):edges[(p,q)]+=1
    assert all(n==1 and edges.get((b,a))==1 for (a,b),n in edges.items())
    removed=0;checks=0
    for hole in holes:
        x,y,r,a,horizontal=hole['X'],hole['Y'],hole['Diameter']/2,hole.get('SlotExtension',0)/2,hole.get('Horizontal',False)
        removed+=math.pi*r*r+4*r*a
        assert crossings(faces,x,y)==0
        for end in (-a,0,a):
            cx=x+(end if horizontal else 0);cy=y+(0 if horizontal else end)
            for j in range(24):
                angle=j*math.tau/24;dx,dy=math.cos(angle),math.sin(angle)
                assert crossings(faces,cx+(r-.002)*dx,cy+(r-.002)*dy)==0,(path.name,'obstructed capsule',hole,j)
                checks+=1
        # Outward endcaps and both straight flanks retain the nominal material boundary.
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            cx=x+(a*dx if horizontal else 0);cy=y+(0 if horizontal else a*dy)
            assert crossings(faces,cx+(r+.04)*dx,cy+(r+.04)*dy)==2,(path.name,'missing material',hole)
        wall=set()
        for f in faces:
            if min(p[2] for p in f)<.0001 and abs(max(p[2] for p in f)-2)<.0001:
                for p in f:
                    dx,dy=p[0]-x,p[1]-y
                    distance=math.hypot(max(abs(dx)-a,0),dy) if horizontal else math.hypot(dx,max(abs(dy)-a,0))
                    if abs(distance-r)<.03:wall.add(p)
        assert wall
        actual=[max(p[i] for p in wall)-min(p[i] for p in wall) for i in (0,1)]
        target=[2*r+(2*a if horizontal else 0),2*r+(0 if horizontal else 2*a)]
        assert all(abs(p-q)<.03 for p,q in zip(actual,target)),(actual,target)
    expected=(8500-removed)*2+(93.5*78.5-90.3*75.3)*3
    measured=volume(faces);assert abs(measured-expected)<3,(measured,expected)
    return {'file':str(path),'result':'PASS','triangles':len(faces),'size_mm':size,'volume_mm3':measured,'analytic_volume_mm3':expected,'inside_clearance_rays':checks,'holes':holes}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs',default='artifacts/tests-1.10.0')
    parser.add_argument('--output',default='artifacts/slot-files-1.10.0.json')
    parser.add_argument('--native',action='store_true')
    args=parser.parse_args();results=[]
    if args.native:
        root=Path(args.inputs)
        with ZipFile(root/'Original.lithopro') as z:
            before=json.loads(z.read('project.json'));source=z.read('source.bin')
        with ZipFile(root/'Langloch-Test.lithopro') as z:
            saved=json.loads(z.read('project.json'));assert source==z.read('source.bin')
        assert saved['Version']==9 and saved['Name']=='Langloch-Test'
        before['Name']='Langloch-Test';first=before['Model']['BackMountingHoles'][0];first['SlotExtension']=10;first['Horizontal']=True
        assert before==saved,'Unexpected project change'
        holes=list(saved['Model']['BackMountingHoles'])+[{'X':0,'Y':42.5,'Diameter':8}]
        result=verify(root/'Langloch-Test – Rückwand.3mf',holes)
        result.update({'format':9,'other_settings_unchanged':True,'source_bytes_unchanged':True,'source_sha256':sha256(source).hexdigest()});results.append(result)
    else:
        for mode in ('Vertical','Horizontal','Mixed'):
            holes=[{'X':x,'Y':y,'Diameter':4,'SlotExtension':8,'Horizontal':mode=='Horizontal' or mode=='Mixed' and i%2==1} for i,(x,y) in enumerate(((-25,63.75),(25,63.75),(-25,21.25),(25,21.25)))]
            holes.append({'X':0,'Y':42.5,'Diameter':8})
            for extension in ('stl','3mf'):results.append(verify(Path(args.inputs)/f'Slots-{mode}.{extension}',holes))
    Path(args.output).write_text(json.dumps(results,indent=2),encoding='utf-8')
    print(f'PASS: {len(results)} independent slot files; free capsule rays, material, width/length, volume, closed edges and Z=0.')


if __name__=='__main__':main()
