"""Independent verification of imported, plugin, workshop and physical calibration model files."""
import argparse
from collections import Counter
from io import BytesIO
import json
import math
from pathlib import Path
import struct
import xml.etree.ElementTree as ET
from zipfile import ZipFile

NS={'m':'http://schemas.microsoft.com/3dmanufacturing/core/2015/02'}


def read(data,extension):
    if extension=='.stl':
        count=struct.unpack_from('<I',data,80)[0];assert len(data)==84+50*count
        lookup={};vertices=[];triangles=[]
        for i in range(count):
            face=[]
            for j in range(3):
                p=struct.unpack_from('<3f',data,84+50*i+12+12*j)
                if p not in lookup:lookup[p]=len(vertices);vertices.append(p)
                face.append(lookup[p])
            triangles.append(face)
        return vertices,triangles
    with ZipFile(BytesIO(data)) as z:r=ET.fromstring(z.read('3D/3dmodel.model'))
    assert r.attrib['unit']=='millimeter'
    mesh=r.find('m:resources/m:object/m:mesh',NS);assert mesh is not None
    return ([tuple(float(v.attrib[c]) for c in 'xyz') for v in mesh.findall('m:vertices/m:vertex',NS)],[[int(t.attrib[c]) for c in ('v1','v2','v3')] for t in mesh.findall('m:triangles/m:triangle',NS)])


def verify(name,data):
    vertices,triangles=read(data,Path(name).suffix);edges=Counter();volume=0
    assert vertices and triangles and all(math.isfinite(c) for v in vertices for c in v)
    for ids in triangles:
        assert len(set(ids))==3 and all(0<=i<len(vertices) for i in ids)
        a,b,c=[vertices[i] for i in ids];u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
        cross=[u[(i+1)%3]*v[(i+2)%3]-u[(i+2)%3]*v[(i+1)%3] for i in range(3)];assert sum(c*c for c in cross)>1e-16
        volume+=sum(a[i]*(b[(i+1)%3]*c[(i+2)%3]-b[(i+2)%3]*c[(i+1)%3]) for i in range(3))/6
        for i,j in ((ids[0],ids[1]),(ids[1],ids[2]),(ids[2],ids[0])):edges[(i,j)]+=1
    assert volume>0 and all(n==1 and edges[(j,i)]==1 for (i,j),n in edges.items())
    size=[max(v[i] for v in vertices)-min(v[i] for v in vertices) for i in range(3)];assert abs(min(v[2] for v in vertices))<.001
    return {'file':name,'result':'PASS','size_mm':size,'volume_mm3':volume,'triangles':len(triangles)}


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--inputs',default='artifacts/tests-1.20.0');p.add_argument('--output',default='artifacts/workshop-files-1.20.0.json');args=p.parse_args();root=Path(args.inputs);results=[]
    for i in range(7):
        pair=[verify(f'Workshop-{i}'+ext,(root/(f'Workshop-{i}'+ext)).read_bytes()) for ext in ('.stl','.3mf')]
        assert all(abs(a-b)<.001 for a,b in zip(pair[0]['size_mm'],pair[1]['size_mm'])) and abs(pair[0]['volume_mm3']-pair[1]['volume_mm3'])<.01,(i,pair)
        if i in (0,1):
            width=8.5 if i==0 else 10.5;expected=(width*2+12*2+2*2**2)*50;assert abs(pair[0]['volume_mm3']-expected)<.05 and all(abs(a-b)<.001 for a,b in zip(pair[0]['size_mm'],[width+4,50,8]))
        if i==4:assert abs(pair[0]['volume_mm3']-100*80*1.2)<.1
        if i==5:assert abs(pair[0]['volume_mm3']-3*(40+30-3)*20)<.1
        results+=pair
    for file in ['Plate-Holes.stl','Photo-Holes-0.3mf','Photo-Holes-255.3mf','Import-Roundtrip.3mf','Plugin-Arc.3mf','Plugin-Drilled.3mf']:results.append(verify(file,(root/file).read_bytes()))
    with ZipFile(root/'P1S-PETG-Probedrucke.zip') as z:
        files=[n for n in z.namelist() if n.endswith('.stl')];assert len(files)==16
        profile=json.loads(z.read('Pruefwerte.json'))['Printer'];assert profile['BedX']==profile['BedY']==profile['BuildZ']==256 and profile['Material'].startswith('PETG')
        for file in files:
            result=verify(file,z.read(file))
            if file.startswith('Licht-'):
                thickness=float(file[6:-6]);assert all(abs(a-b)<.001 for a,b in zip(result['size_mm'],[15,35,thickness])) and abs(result['volume_mm3']-15*35*thickness)<.1
            results.append(result)
    Path(args.output).write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8');print(f'PASS: {len(results)} independent workshop/import/plugin/calibration files; closed edges, positive volumes, dimensions and Z=0.')


if __name__=='__main__':main()
