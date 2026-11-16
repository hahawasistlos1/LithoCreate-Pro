"""Independent verification of configurable two-object 3MF print layouts."""
import argparse
from collections import Counter
from hashlib import sha256
import json
import math
from pathlib import Path
import xml.etree.ElementTree as ET
from zipfile import ZipFile

NS={'m':'http://schemas.microsoft.com/3dmanufacturing/core/2015/02'}


def verify(path,bed,margin,gap,row,native=False,positions=None):
    with ZipFile(path) as z:
        root=ET.fromstring(z.read('3D/3dmodel.model'))
        rels=ET.fromstring(z.read('_rels/.rels'));types=ET.fromstring(z.read('[Content_Types].xml'))
    assert root.attrib['unit']=='millimeter'
    assert any(r.attrib.get('Target')=='/3D/3dmodel.model' for r in rels)
    assert any(t.attrib.get('ContentType')=='application/vnd.ms-package.3dmanufacturing-3dmodel+xml' for t in types)
    objects=root.findall('m:resources/m:object',NS);items=root.findall('m:build/m:item',NS)
    assert len(objects)==len(items)==2
    resources={o.attrib['id']:o for o in objects};assert len(resources)==2
    parts=[]
    for index,item in enumerate(items):
        obj=resources[item.attrib['objectid']];assert obj.attrib['type']=='model' and len(obj.findall('m:mesh',NS))==1 and not obj.findall('m:components',NS)
        vertices=[tuple(float(v.attrib[c]) for c in 'xyz') for v in obj.findall('m:mesh/m:vertices/m:vertex',NS)]
        assert vertices and all(math.isfinite(c) for v in vertices for c in v)
        low=[min(v[i] for v in vertices) for i in range(3)];size=[max(v[i] for v in vertices) for i in range(3)];assert all(abs(v)<.0001 for v in low)
        expected=([48,40,16] if index==0 else [40,48,5]) if native else ([40,30,3] if index==0 else [20,10,2])
        assert all(abs(a-b)<.001 for a,b in zip(size,expected))
        edges=Counter();volume=0;count=0
        for t in obj.findall('m:mesh/m:triangles/m:triangle',NS):
            ids=tuple(int(t.attrib[c]) for c in ('v1','v2','v3'));assert all(0<=i<len(vertices) for i in ids)
            a,b,c=[vertices[i] for i in ids];u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
            assert sum((u[(i+1)%3]*v[(i+2)%3]-u[(i+2)%3]*v[(i+1)%3])**2 for i in range(3))>1e-16
            volume+=sum(a[i]*(b[(i+1)%3]*c[(i+2)%3]-b[(i+2)%3]*c[(i+1)%3]) for i in range(3))/6
            for i,j in ((ids[0],ids[1]),(ids[1],ids[2]),(ids[2],ids[0])):edges[(i,j)]+=1
            count+=1
        assert volume>0 and all(n==1 and edges.get((j,i))==1 for (i,j),n in edges.items())
        if not native:assert abs(volume-(3600 if index==0 else 400))<.02
        matrix=list(map(float,item.attrib['transform'].split()));assert len(matrix)==12 and matrix[:9]==[1,0,0,0,1,0,0,0,1] and matrix[11]==0 and all(math.isfinite(v) for v in matrix)
        p=matrix[9:11];assert all(v>=margin-.0001 for v in p) and p[0]+size[0]<=bed[0]-margin+.0001 and p[1]+size[1]<=bed[1]-margin+.0001 and size[2]<=bed[2]+.0001
        parts.append({'name':obj.attrib['name'],'position_mm':p,'size_mm':size,'volume_mm3':volume,'triangles':count})
    assert [p['name'] for p in parts]==(['Leuchtkasten','Rückwand'] if native else ['Lithophanie','Rückwand'])
    a,b=parts;sa,sb=a['size_mm'],b['size_mm'];w=sa[0]+sb[0]+gap if row else max(sa[0],sb[0]);d=max(sa[1],sb[1]) if row else sa[1]+sb[1]+gap
    x=(bed[0]-w)/2;y=(bed[1]-d)/2
    expected=([x,y+(d-sa[1])/2],[x+sa[0]+gap,y+(d-sb[1])/2]) if row else ([x+(w-sa[0])/2,y],[x+(w-sb[0])/2,y+sa[1]+gap])
    if positions is not None:expected=positions
    assert all(abs(v-t)<.001 for part,e in zip(parts,expected) for v,t in zip(part['position_mm'],e))
    pa,pb=a['position_mm'],b['position_mm']
    assert pa[0]+sa[0]+gap<=pb[0]+.001 or pb[0]+sb[0]+gap<=pa[0]+.001 or pa[1]+sa[1]+gap<=pb[1]+.001 or pb[1]+sb[1]+gap<=pa[1]+.001
    return {'file':path.name,'result':'PASS','margin_mm':margin,'gap_mm':gap,'arrangement':'row' if row else 'column','parts':parts}


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--inputs',default='artifacts/tests-1.16.0');parser.add_argument('--output',default='artifacts/layout-files-1.16.0.json');parser.add_argument('--native',action='store_true');args=parser.parse_args();root=Path(args.inputs);results=[]
    if args.native:
        with ZipFile(root/'Original.lithopro') as z:before=json.loads(z.read('project.json'));source=z.read('source.bin')
        with ZipFile(root/'Anordnungs-Test.lithopro') as z:after=json.loads(z.read('project.json'));assert source==z.read('source.bin')
        assert after['Version']==14 and after['PrintLayout']=={'Margin':12,'Gap':12.5,'Arrangement':2}
        before['Name']='Anordnungs-Test';before['PrintLayout']['Gap']=12.5;assert before==after,'Unexpected image/model/printer change'
        files=list(root.glob('*.3mf'));assert len(files)==1
        result=verify(files[0],[220,220,250],12,12.5,False,True);result.update({'source_unchanged':True,'other_project_settings_unchanged':True,'source_sha256':sha256(source).hexdigest()});results.append(result)
    else:
        for name in ('Row','Column','Auto','Tight'):
            tight=name=='Tight';results.append(verify(root/f'Layout-{name}.3mf',[61,30,50] if tight else [100,80,50],0 if tight else 12.5,1 if tight else 9.5,name in ('Row','Tight')))
    Path(args.output).write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8');print(f'PASS: {len(results)} configurable 3MF layouts; dimensions, volumes, exact positions, margins, gaps and closed edges.')


if __name__=='__main__':main()
