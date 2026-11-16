"""Verify the own-copy native 1.9.0 save/export session, without Core code."""
from collections import Counter
from hashlib import sha256
import json
import math
from pathlib import Path
from zipfile import ZipFile
from check_angles import rotation
from check_back import volume
from check_placements import triangles


def main():
    folder=Path('artifacts/native-1.9.0')
    with ZipFile(folder/'Original.lithopro') as old, ZipFile(folder/'Druckwinkel-Test.lithopro') as saved:
        original=json.loads(old.read('project.json')); current=json.loads(saved.read('project.json'))
        source=old.read('source.bin')
        assert source==saved.read('source.bin')
    assert current['Version']==8
    assert current['Name']=='Druckwinkel-Test'
    assert current['Model']['Angles']=={'X':20,'Y':-12.5,'Z':15}
    original['Name']=current['Name'];original['Model']['Angles']['Y']=-12.5
    assert original==current, 'Unexpected project setting changed'
    path=folder/'Druckwinkel-Test.3mf'; faces=triangles(path)
    assert len(faces)==381896
    points=set(p for f in faces for p in f)
    assert all(math.isfinite(v) for p in points for v in p)
    size=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    matrix=rotation(20,-12.5,15)
    expected=[sum(abs(matrix[i][j])*d for j,d in enumerate((100,85,4))) for i in range(3)]
    assert all(abs(a-b)<.001 for a,b in zip(size,expected)), (size,expected)
    assert abs(min(p[2] for p in points))<.0001
    edges=Counter()
    for a,b,c in faces:
        u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
        assert sum(w*w for w in (u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]))>1e-18
        for p,q in ((a,b),(b,c),(c,a)): edges[(p,q)]+=1
    assert all(n==1 and edges.get((b,a))==1 for (a,b),n in edges.items())
    measured=volume(faces);assert measured>0
    result={'result':'PASS','file':str(path),'triangles':len(faces),'size_mm':size,'expected_matrix_size_mm':expected,'volume_mm3':measured,'angles_xyz_deg':[20,-12.5,15],'project_format':8,'source_bytes_unchanged':True,'other_settings_unchanged':True,'source_sha256':sha256(source).hexdigest(),'export_sha256':sha256(path.read_bytes()).hexdigest()}
    (folder/'verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print('PASS: Native save and 3MF export; independent matrix bounds, closed edges, positive volume, Z=0, exact source and other settings.')


if __name__=='__main__': main()
