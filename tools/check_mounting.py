"""Read the four mounting fixtures independently; check bores, volume and closed edges."""
import argparse
from collections import Counter
import json
import math
from pathlib import Path
from check_placements import triangles
from check_back import volume


def crossings(faces, x, y):
    hits = []
    for a, b, c in faces:
        den = (b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-12:
            continue
        u=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den
        v=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den
        if u>=-1e-8 and v>=-1e-8 and u+v<=1+1e-8:
            z=u*a[2]+v*b[2]+(1-u-v)*c[2]
            if all(abs(z-other)>.0001 for other in hits):
                hits.append(z)
    return len(hits)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs',default='artifacts/tests-1.7.0')
    parser.add_argument('--output',default='artifacts/mounting-files-1.7.0.json')
    args=parser.parse_args()
    results=[]
    for name in ('Mounting-Reference','Mounting-NoCable'):
        for ext in ('stl','3mf'):
            path=Path(args.inputs)/f'{name}.{ext}'
            faces=triangles(path)
            points=set(p for face in faces for p in face)
            bounds=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
            assert all(abs(a-b)<.001 for a,b in zip(bounds,[100,85,5])),bounds
            assert abs(min(p[2] for p in points))<.0001
            edges=Counter()
            for a,b,c in faces:
                for u,v in ((a,b),(b,c),(c,a)):
                    edges[(u,v)]+=1
            assert all(count==1 and edges.get((b,a))==1 for (a,b),count in edges.items()),path.name
            holes=[(-25,63.75,4),(25,63.75,4),(-25,21.25,4),(25,21.25,4)]
            if name=='Mounting-Reference':
                holes.append((0,42.5,8))
            area_removed=0
            clear=[]
            for x,y,d in holes:
                rim=sorted((p for p in points if abs(p[2])<.0001 and abs(math.hypot(p[0]-x,p[1]-y)-d/2)<.1),key=lambda p:math.atan2(p[1]-y,p[0]-x))
                assert len(rim)>=32,(path.name,x,y,len(rim))
                area_removed+=abs(sum((a[0]-x)*(b[1]-y)-(b[0]-x)*(a[1]-y) for a,b in zip(rim,rim[1:]+rim[:1])))/2
                free=math.inf
                for a,b in zip(rim,rim[1:]+rim[:1]):
                    ax,ay=a[0]-x,a[1]-y;dx,dy=b[0]-a[0],b[1]-a[1]
                    t=max(0,min(1,-(ax*dx+ay*dy)/(dx*dx+dy*dy)))
                    free=min(free,math.hypot(ax+t*dx,ay+t*dy))
                assert d-.0001<=2*free<d+.003,(d,2*free)
                assert crossings(faces,x,y)==0,(path.name,x,y,'blocked bore')
                assert crossings(faces,x+d/2+1.5,y+.037)==2,(path.name,x,y,'missing material')
                clear.append({'x_mm':x,'y_mm':y,'free_diameter_mm':2*free})
            expected=(8500-area_removed)*2+(93.5*78.5-90.3*75.3)*3
            actual=volume(faces)
            assert abs(actual-expected)<.2,(actual,expected)
            results.append({'file':path.name,'result':'PASS','size_mm':bounds,'bores':clear,'volume_mm3':actual,'closed_oriented_edges':True})
    Path(args.output).write_text(json.dumps(results,indent=2),encoding='utf-8')
    print('PASS: Four independent mounting STL/3MF checks; exact positions, free bores, material, volume, closed edges.')


if __name__=='__main__':
    main()
