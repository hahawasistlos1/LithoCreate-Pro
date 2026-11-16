"""Exercise the installed Bambu Studio against generated models, without printing or uploading."""
import argparse
import json
import math
import os
from pathlib import Path
import struct
import subprocess
import xml.etree.ElementTree as ET
from zipfile import ZipFile

NS={'m':'http://schemas.microsoft.com/3dmanufacturing/core/2015/02'}


def bounds(vertices):return [max(v[i] for v in vertices)-min(v[i] for v in vertices) for i in range(3)]


def stl(path):
    data=path.read_bytes();n=struct.unpack_from('<I',data,80)[0];assert len(data)==84+50*n
    points=[struct.unpack_from('<3f',data,84+50*t+12+12*v) for t in range(n) for v in range(3)]
    return n,bounds(points)


def verify(source,destination):
    count,size=stl(source)
    with ZipFile(destination) as z:
        assert z.testzip() is None
        models=[ET.fromstring(z.read(n)) for n in z.namelist() if n.endswith('.model')]
        root=ET.fromstring(z.read('3D/3dmodel.model'));assert root.attrib['unit']=='millimeter'
        meshes=[m for r in models for m in r.findall('.//m:mesh',NS)];assert len(meshes)==1
        vertices=[tuple(float(v.attrib[c]) for c in 'xyz') for v in meshes[0].findall('m:vertices/m:vertex',NS)]
        triangles=meshes[0].findall('m:triangles/m:triangle',NS);assert len(triangles)==count
        assert all(math.isfinite(c) for v in vertices for c in v)
        actual=bounds(vertices);assert all(abs(a-b)<.01 for a,b in zip(actual,size)),(source.name,actual,size)
    return {'file':source.name,'result':'PASS','triangles':count,'size_mm':actual,'bambu_3mf':destination.name}


def resolved(root,category,name,seen=None):
    seen=set() if seen is None else seen
    if name in seen or len(seen)>30:raise ValueError('Configuration inheritance cycle')
    seen=seen|{name};assert Path(name).name==name
    data=json.loads((root/category/(name+'.json')).read_text(encoding='utf-8-sig'));result={}
    if data.get('inherits'):result.update(resolved(root,category,data['inherits'],seen))
    for include in data.get('include',[]):result.update(resolved(root,category,include,seen))
    result.update(data);result.pop('inherits',None);result.pop('include',None);result['instantiation']='true';return result


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--exe',default='C:/Program Files/Bambu Studio/bambu-studio.exe');p.add_argument('--inputs',default='artifacts/tests-1.20.0');p.add_argument('--output',default='artifacts/bambu-1.20.0');p.add_argument('--slice',action='store_true');p.add_argument('--slice-only',action='store_true');args=p.parse_args()
    exe=Path(args.exe).resolve();source=Path(args.inputs).resolve();out=Path(args.output).resolve();out.mkdir(parents=True,exist_ok=True);assert exe.is_file()
    temp=out/'temp';temp.mkdir(exist_ok=True);environment=dict(os.environ,TEMP=str(temp),TMP=str(temp))
    def run(arguments,log):
        with (out/log).open('w',encoding='utf-8') as f:result=subprocess.run([str(exe),*map(str,arguments)],stdout=f,stderr=subprocess.STDOUT,timeout=120,check=False,env=environment if log=='slice.log' else None)
        assert result.returncode==0,(log,result.returncode)
    names=['Flat','Curved','Cylinder','Polygon','Sphere','Moon','Heart','Star','Oval','Lightbox','Frame','Nightlight','Disc','Pendant','Drop']
    results=[]
    for name in names:
        target=out/(name+'.3mf')
        if not args.slice_only:run(['--outputdir',out,'--export-3mf',target,source/(name+'.stl')],name+'.log')
        results.append(verify(source/(name+'.stl'),target));print('PASS Bambu import/export:',name,flush=True)
    (out/'verification-imports.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    if args.slice:
        root=exe.parent/'resources/profiles/BBL'
        settings=[]
        for category,name,filename in [('machine','Bambu Lab P1S 0.4 nozzle','machine.json'),('process','0.12mm Fine @BBL X1C','process.json'),('filament','Generic PETG @base','filament.json')]:
            data=resolved(root,category,name);path=out/filename;path.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');settings.append(path)
        destination=out/'P1S-PETG-Sliced.3mf'
        run(['--outputdir',out,'--load-settings',str(settings[0])+';'+str(settings[1]),'--load-filaments',settings[2],'--curr-bed-type','Textured PEI Plate','--arrange','1','--slice','0',source/'Flat.stl'],'slice.log')
        report=json.loads((out/'result.json').read_text());assert report['return_code']==0,report.get('error_string')
        gcode=list(out.glob('plate_*.gcode'));assert gcode,'Slicer did not generate actual toolpaths'
        text=gcode[0].read_text(encoding='utf-8');assert 'G1 ' in text and ('; filament used' in text or 'total filament' in text)
        (out/'toolpath-evidence.txt').write_text('\n'.join(line for line in text.splitlines() if ('estimated' in line or 'filament used' in line))[:4000],encoding='utf-8')
        gcode=[f.name for f in gcode]
        results.append({'result':'PASS','slicer':'Bambu Studio','profile':'P1S 0.4 / Generic PETG / 0.12 mm (test only)','actual_gcode':gcode,'print_sent':False})
    (out/'verification.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8');print('PASS:',len(names),'independent Bambu Studio imports/exports; actual slicing:',args.slice)


if __name__=='__main__':main()
