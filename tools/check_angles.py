"""Independent matrix-based checks of twelve tilted STL/3MF files."""
import argparse
from collections import Counter,defaultdict
import json
import math
from pathlib import Path
from check_placements import triangles
from check_back import volume


def multiply(a,b):
    return [[sum(a[i][k]*b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]


def rotation(x,y,z):
    x,y,z=map(math.radians,(x,y,z));cx,sx=math.cos(x),math.sin(x);cy,sy=math.cos(y),math.sin(y);cz,sz=math.cos(z),math.sin(z)
    return multiply([[cz,-sz,0],[sz,cz,0],[0,0,1]],multiply([[cy,0,sy],[0,1,0],[-sy,0,cy]],[[1,0,0],[0,cx,-sx],[0,sx,cx]]))


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs',default='artifacts/tests-1.9.0')
    parser.add_argument('--output',default='artifacts/angle-files-1.9.0.json')
    args=parser.parse_args();root=Path(args.inputs);results=[]
    for ext in ('stl','3mf'):
        base=set(p for face in triangles(root/f'Angles-Base.{ext}') for p in face)
        for name,angles,bed90 in [('Base',(0,0,0),False),('X',(30,0,0),False),('Y',(0,30,0),False),('Z',(0,0,45),False),('XYZ',(23,-17,38),False),('XYZ-Bed90',(23,-17,38),True)]:
            matrix=rotation(*angles);expected=[];pivot=[0,-1.5,15]
            for point in base:
                v=[point[i]-pivot[i] for i in range(3)]
                q=[sum(matrix[i][j]*v[j] for j in range(3))+pivot[i] for i in range(3)]
                expected.append([-q[1],q[0],q[2]] if bed90 else q)
            bottom=min(p[2] for p in expected);expected=[(p[0],p[1],p[2]-bottom) for p in expected]
            path=root/f'Angles-{name}.{ext}';faces=triangles(path);points=set(p for f in faces for p in f)
            assert len(points)==len(base)
            cells=defaultdict(list)
            for p in points: cells[tuple(math.floor(v/.001) for v in p)].append(p)
            for p in expected:
                key=tuple(math.floor(v/.001) for v in p)
                assert any(math.dist(p,q)<.0001 for dx in (-1,0,1) for dy in (-1,0,1) for dz in (-1,0,1) for q in cells.get((key[0]+dx,key[1]+dy,key[2]+dz),())),(name,p)
            edges=Counter()
            for a,b,c in faces:
                for i,j in ((a,b),(b,c),(c,a)): edges[(i,j)]+=1
            assert all(n==1 and edges.get((b,a))==1 for (a,b),n in edges.items())
            measured=volume(faces);assert abs(measured-3600)<.05,(name,measured)
            size=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
            assert abs(min(p[2] for p in points))<.0001
            results.append({'file':path.name,'result':'PASS','angles_xyz_deg':angles,'bed90':bed90,'size_mm':size,'volume_mm3':measured,'all_vertices_match_independent_matrix':True})
    Path(args.output).write_text(json.dumps(results,indent=2),encoding='utf-8')
    print('PASS: Twelve independent angle STL/3MF checks; matrix order, all vertices, pivot, bed rotation, volume, closed edges and Z=0.')


if __name__=='__main__': main()
