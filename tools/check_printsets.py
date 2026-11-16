"""Independent OPC/3MF checks of two named, separately placed print objects."""
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import xml.etree.ElementTree as ET
from zipfile import ZipFile


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs',default='artifacts/tests-1.8.0')
    parser.add_argument('--output',default='artifacts/printset-files-1.8.0.json')
    args=parser.parse_args()
    ns={'m':'http://schemas.microsoft.com/3dmanufacturing/core/2015/02'}
    results=[]
    for name,bed,expected_names in (
        ('Reference',(100,80,50),['Lithophanie','Rückwand']),
        ('Row',(80,50,10),['Lithophanie','Rückwand']),
        ('Lightbox',(220,220,250),['Leuchtkasten','Rückwand']),
        ('Sphere',(220,220,250),['Lithophanie','Stecksockel'])):
        path=Path(args.inputs)/f'PrintSet-{name}.3mf'
        with ZipFile(path) as archive:
            root=ET.fromstring(archive.read('3D/3dmodel.model'))
            rels=ET.fromstring(archive.read('_rels/.rels'))
            types=ET.fromstring(archive.read('[Content_Types].xml'))
        assert any(r.attrib.get('Target')=='/3D/3dmodel.model' and r.attrib.get('Type')=='http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel' for r in rels)
        assert any(t.attrib.get('Extension')=='model' and t.attrib.get('ContentType')=='application/vnd.ms-package.3dmanufacturing-3dmodel+xml' for t in types)
        assert root.attrib.get('unit')=='millimeter'
        objects=root.findall('m:resources/m:object',ns)
        items=root.findall('m:build/m:item',ns)
        assert len(objects)==len(items)==2
        resources={o.attrib['id']:o for o in objects}
        assert len(resources)==2 and len({i.attrib['objectid'] for i in items})==2
        parts=[]
        for index,item in enumerate(items):
            obj=resources[item.attrib['objectid']]
            assert obj.attrib.get('name')==expected_names[index] and obj.attrib.get('type')=='model'
            assert len(obj.findall('m:mesh',ns))==1 and not obj.findall('m:components',ns)
            matrix=list(map(float,item.attrib['transform'].split()))
            assert len(matrix)==12 and matrix[:9]==[1,0,0,0,1,0,0,0,1] and matrix[11]==0
            assert all(math.isfinite(v) for v in matrix)
            vertices=[tuple(float(v.attrib[c]) for c in 'xyz') for v in obj.findall('m:mesh/m:vertices/m:vertex',ns)]
            assert vertices and all(math.isfinite(c) for v in vertices for c in v)
            low=[min(v[i] for v in vertices) for i in range(3)]
            high=[max(v[i] for v in vertices) for i in range(3)]
            assert all(abs(v)<.0001 for v in low),low
            edges=Counter();volume=0;triangle_count=0
            for triangle in obj.findall('m:mesh/m:triangles/m:triangle',ns):
                ids=tuple(int(triangle.attrib[c]) for c in ('v1','v2','v3'))
                assert all(0<=i<len(vertices) for i in ids)
                a,b,c=[vertices[i] for i in ids]
                volume+=sum(a[i]*(b[(i+1)%3]*c[(i+2)%3]-b[(i+2)%3]*c[(i+1)%3]) for i in range(3))/6
                ab=[b[i]-a[i] for i in range(3)];ac=[c[i]-a[i] for i in range(3)]
                cross=[ab[(i+1)%3]*ac[(i+2)%3]-ab[(i+2)%3]*ac[(i+1)%3] for i in range(3)]
                assert sum(v*v for v in cross)>1e-16
                for i,j in ((ids[0],ids[1]),(ids[1],ids[2]),(ids[2],ids[0])): edges[(i,j)]+=1
                triangle_count+=1
            assert volume>0 and all(n==1 and edges.get((b,a))==1 for (a,b),n in edges.items())
            position=matrix[9:11]
            assert position[0]>=4.9999 and position[1]>=4.9999
            assert position[0]+high[0]<=bed[0]-4.9999 and position[1]+high[1]<=bed[1]-4.9999 and high[2]<=bed[2]+.0001
            parts.append({'name':obj.attrib['name'],'position_mm':position,'size_mm':high,'volume_mm3':volume,'triangles':triangle_count})
        a,b=parts
        assert a['position_mm'][0]+a['size_mm'][0]+4.9999<=b['position_mm'][0] or b['position_mm'][0]+b['size_mm'][0]+4.9999<=a['position_mm'][0] or a['position_mm'][1]+a['size_mm'][1]+4.9999<=b['position_mm'][1] or b['position_mm'][1]+b['size_mm'][1]+4.9999<=a['position_mm'][1]
        if name in ('Reference','Row'):
            assert all(abs(x-y)<.0001 for x,y in zip(a['size_mm'],[40,30,3])) and all(abs(x-y)<.0001 for x,y in zip(b['size_mm'],[20,10,2]))
            assert abs(a['volume_mm3']-3600)<.05 and abs(b['volume_mm3']-400)<.05
            expected=([30,17.5],[40,52.5]) if name=='Reference' else ([7.5,10],[52.5,20])
            assert a['position_mm']==expected[0] and b['position_mm']==expected[1]
        if name=='Lightbox':
            assert all(abs(x-y)<.001 for x,y in zip(a['size_mm'],[48,40,16])) and all(abs(x-y)<.001 for x,y in zip(b['size_mm'],[40,48,5]))
        if name=='Sphere':
            assert all(abs(x-y)<.001 for x,y in zip(b['size_mm'],[60,8,60]))
        results.append({'file':path.name,'result':'PASS','bed_mm':bed,'parts':parts,'gap_and_margin_mm':5})
    Path(args.output).write_text(json.dumps(results,indent=2,ensure_ascii=False),encoding='utf-8')
    print('PASS: Four independent two-object 3MF print sets; OPC, names, transforms, geometry, volume, spacing and bed limits.')


if __name__=='__main__':
    main()
