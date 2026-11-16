"""Independent physical dimensions/volume checks and native linked-size verification."""
import argparse
from collections import Counter
from hashlib import sha256
import json
import math
from pathlib import Path
from zipfile import ZipFile
from check_placements import triangles
from check_back import volume


def verify(path,expected,known_volume=None):
    faces=triangles(path);points=set(p for f in faces for p in f);edges=Counter()
    assert all(math.isfinite(v) for p in points for v in p)
    size=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    assert all(abs(a-b)<.001 for a,b in zip(size,expected)),(path.name,size,expected)
    assert abs(min(p[2] for p in points))<.0001
    for a,b,c in faces:
        u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
        assert sum(t*t for t in (u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]))>1e-18
        for p,q in ((a,b),(b,c),(c,a)):edges[(p,q)]+=1
    assert all(n==1 and edges.get((b,a))==1 for (a,b),n in edges.items())
    measured=volume(faces);assert measured>0
    if known_volume is not None:assert abs(measured-known_volume)<.1,(path.name,measured,known_volume)
    return {'file':str(path),'result':'PASS','size_mm':size,'volume_mm3':measured,'triangles':len(faces)}


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--inputs',default='artifacts/tests-1.13.0');parser.add_argument('--output',default='artifacts/dimension-files-1.13.0.json');parser.add_argument('--native',action='store_true');args=parser.parse_args();root=Path(args.inputs);results=[]
    if args.native:
        with ZipFile(root/'Original.lithopro') as z:before=json.loads(z.read('project.json'));source=z.read('source.bin')
        with ZipFile(root/'Groessen-Test.lithopro') as z:saved=json.loads(z.read('project.json'));assert source==z.read('source.bin')
        assert saved['Version']==12 and saved['Name']=='Groessen-Test' and saved['Model']['LockDimensions']
        assert saved['Model']['Width']==150 and abs(saved['Model']['Height']-110)<1e-8
        before['Name']='Groessen-Test';before['Model']['Width']=150;before['Model']['Height']=110
        assert before==saved,'Unexpected image/model setting changed'
        result=verify(root/'Groessen-Test.3mf',[150,4,110]);result.update({'format':12,'linked_width_height':[150,110],'other_settings_unchanged':True,'source_bytes_unchanged':True,'source_sha256':sha256(source).hexdigest()});results.append(result)
    else:
        for name,w,h in (('Original',40,30),('Width',80,60),('Height',60,45),('Unlinked',30,45)):
            for ext in ('stl','3mf'):results.append(verify(root/f'Size-{name}.{ext}',[w,h,3],w*h*3))
    Path(args.output).write_text(json.dumps(results,indent=2),encoding='utf-8');print(f'PASS: {len(results)} independent sizing files; physical axes, unchanged thickness, known volume, closed edges and Z=0.')


if __name__=='__main__':main()
