"""Independent connected-body, free bore and tab-volume checks for exported hanging tabs."""
import argparse
from collections import Counter,defaultdict
from hashlib import sha256
import json
import math
from pathlib import Path
from zipfile import ZipFile
from check_placements import triangles
from check_mounting import crossings
from check_back import volume


def check(faces,w,h,tw,th,d,inset,stand=0,depth=4):
    points=set(p for f in faces for p in f);edges=Counter();adj=defaultdict(set)
    assert all(math.isfinite(v) for p in points for v in p)
    bounds=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    assert all(abs(a-b)<.001 for a,b in zip(bounds,[w,h+stand+th,depth])),bounds
    assert abs(min(p[1] for p in points))<.0001 and abs(min(p[2] for p in points))<.0001
    for a,b,c in faces:
        u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
        assert sum(t*t for t in (u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]))>1e-18
        for p,q in ((a,b),(b,c),(c,a)):edges[(p,q)]+=1;adj[p].add(q);adj[q].add(p)
    assert all(n==1 and edges.get((b,a))==1 for (a,b),n in edges.items())
    seen=set();todo=[next(iter(points))]
    while todo:
        p=todo.pop()
        if p in seen:continue
        seen.add(p);todo.extend(adj[p]-seen)
    assert seen==points,'Disconnected body or tabs'
    assert len(points)-len(edges)//2+len(faces)==-2,'Expected genus two'
    base=h+stand;back=(depth/2 if stand else 0);hole_area=0;bores=[]
    for x in (-w/2+inset+tw/2,w/2-inset-tw/2):
        y=base+th/2
        rim=sorted((p for p in points if abs(p[2]-back)<.0001 and abs(math.hypot(p[0]-x,p[1]-y)-d/2)<.1),key=lambda p:math.atan2(p[1]-y,p[0]-x))
        assert len(rim)>=32
        free=math.inf
        for a,b in zip(rim,rim[1:]+rim[:1]):
            ax,ay=a[0]-x,a[1]-y;dx,dy=b[0]-a[0],b[1]-a[1];t=max(0,min(1,-(ax*dx+ay*dy)/(dx*dx+dy*dy)))
            free=min(free,math.hypot(ax+t*dx,ay+t*dy))
        assert d-.0001<=2*free<d+.01,(d,2*free)
        assert min(tw,th)/2-max(math.hypot(p[0]-x,p[1]-y) for p in rim)>=1.1999
        assert crossings(faces,x+.019,y+.027)==0,'Blocked bore'
        assert crossings(faces,x+d/2+.5,y+.031)==2,'Missing material next to bore'
        hole_area+=abs(sum((a[0]-x)*(b[1]-y)-(b[0]-x)*(a[1]-y) for a,b in zip(rim,rim[1:]+rim[:1])))/2
        bores.append({'center_xy':[x,y],'free_diameter_mm':2*free})
    # Closing the attachment in its own y=0 plane adds zero signed volume.
    tabs=[tuple((p[0],p[1]-base,p[2]) for p in f) for f in faces if min(p[1] for p in f)>=base-.0001 and max(p[1] for p in f)>base+.0001]
    measured=volume(tabs);expected=(2*tw*th-hole_area)*4
    assert abs(measured-expected)<.03,(measured,expected)
    assert volume(faces)>0
    return {'result':'PASS','size_mm':bounds,'one_connected_body':True,'euler':-2,'bores':bores,'tab_volume_mm3':measured,'volume_mm3':volume(faces)}


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--inputs',default='artifacts/tests-1.15.0');parser.add_argument('--output',default='artifacts/tab-files-1.15.0.json');parser.add_argument('--native',action='store_true');args=parser.parse_args();root=Path(args.inputs);results=[]
    if args.native:
        with ZipFile(root/'Original.lithopro') as z:before=json.loads(z.read('project.json'));source=z.read('source.bin')
        with ZipFile(root/'Laschen-Test.lithopro') as z:after=json.loads(z.read('project.json'));assert source==z.read('source.bin')
        assert after['Version']==13 and after['Model']['HangingTabsEnabled']
        before['Name']='Laschen-Test';before['Model']['TabHoleDiameter']=5;assert before==after,'Unexpected project change'
        original=triangles(root/'Laschen-Test.3mf')
        planar=[tuple((p[0],p[2],-p[1]) for p in f) for f in original]
        result=check(planar,100,80,12,14,5,8);result.update({'file':'Laschen-Test.3mf','source_unchanged':True,'other_settings_unchanged':True,'source_sha256':sha256(source).hexdigest()});results.append(result)
    else:
        for name in ('Flat','Frame','Stand','Edge'):
            for ext in ('stl','3mf'):
                path=root/f'Tabs-{name}.{ext}';edge=name=='Edge';stand=name=='Stand'
                result=check(triangles(path),40 if edge else 80,30 if edge else 60,6 if edge else 12,20 if edge else 14,3 if edge else 4,0 if edge else 8,5 if stand else 0,25 if stand else 4);result['file']=path.name;results.append(result)
    Path(args.output).write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8');print(f'PASS: {len(results)} tab files; one connected body, two free bores, dimensions, minimum material, tab volume and closed edges.')


if __name__=='__main__':main()
