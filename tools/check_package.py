"""Verify a portable release archive byte for byte, its examples and package hashes."""
import argparse
from hashlib import sha256
from io import BytesIO
import json
from pathlib import Path
from zipfile import ZipFile


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--version',default='1.21.2')
    parser.add_argument('--format',type=int,default=15)
    parser.add_argument('--examples',type=int)
    args=parser.parse_args();version=args.version
    if not all(p.isdigit() for p in version.split('.')) or len(version.split('.'))!=3:
        raise ValueError('Expected a numeric three-part version')
    has_shades=tuple(map(int,version.split('.'))) >= (1,21,0)
    has_spokes=tuple(map(int,version.split('.'))) >= (1,21,1)
    expected_examples=args.examples if args.examples is not None else 16 if has_shades else 12
    folder=Path('artifacts')/f'portable-{version}'
    archive=Path('artifacts')/f'LithoCreate-Pro-{version}-portabel.zip'
    installer=Path('artifacts')/f'LithoCreate-Pro-{version}-win-x64.msi'
    files={p.relative_to(folder).as_posix():p for p in folder.rglob('*') if p.is_file()}
    assert 'coreclr.dll' in files and 'LithoCreate Pro.exe' in files
    if tuple(map(int,version.split('.'))) >= (1,20,0):
        assert all(name in files for name in ['LithoCreate.PluginHost.exe','LithoCreate.PluginHost.dll','Extensions/LithoCreate.WaveForm.dll','Extensions/LithoCreate.DrilledPhoto.dll','Probedrucke/P1S-PETG-Probedrucke.zip'])
    if tuple(map(int,version.split('.'))) >= (1,20,1):
        assert 'Assets/LithoCreate.ico' in files
    assert not any(Path(name).name=='printers.json' for name in files),'Personal printer library must not be packaged'
    assert f'docs/NEU_IN_{version}.md' in files and f'docs/PRUEFPROTOKOLL_{version}.md' in files
    examples=[];slot_examples=[];sink_examples=[];crop_examples=[];size_examples=[];tab_examples=[];layout_examples=[];shade_examples={}
    with ZipFile(archive) as z:
        entries=[i.filename for i in z.infolist() if not i.is_dir()]
        assert len(entries)==len(set(entries)) and set(entries)==set(files)
        for name,path in files.items():
            data=z.read(name)
            assert data==path.read_bytes(),name
            if name.startswith('Beispiele/') and name.endswith('.lithopro'):
                with ZipFile(BytesIO(data)) as project:
                    settings=json.loads(project.read('project.json'))
                    assert settings['Version']==args.format
                    assert len(project.read('source.bin'))>0
                    if args.format>=14:
                        layout=settings['PrintLayout'];assert 0<=layout['Margin']<=50 and 1<=layout['Gap']<=50 and layout['Arrangement'] in ((0,1,2,3) if args.format>=15 else (0,1,2))
                        if {k:layout[k] for k in ('Margin','Gap','Arrangement')}!={'Margin':5,'Gap':5,'Arrangement':0}:layout_examples.append({'file':name,'options':layout})
                        if args.format>=15:assert all(0<=layout[k]<=2000 for k in ('BodyX','BodyY','AccessoryX','AccessoryY'))
                    model=settings['Model']
                    if model['Shape'] in (15,16,17,18):
                        shade_examples[model['Shape']]=(model['ShadeTopDiameter'],model['ShadeBottomDiameter'],model['ShadeHeight'],model['ShadeMount'])
                        if has_spokes:
                            assert model['ShadeMountStyle']==1
                    if model.get('LockDimensions',False):size_examples.append({'file':name,'width':model['Width'],'height':model['Height']})
                    if model.get('HangingTabsEnabled',False):tab_examples.append({'file':name,'model':model})
                    edits=settings['Edits']
                    if edits.get('CropAspectLock',False):crop_examples.append({'file':name,'edits':edits})
                    slots=[h for h in model.get('BackMountingHoles',[]) if h.get('SlotExtension',0)>0]
                    sinks=[h for h in model.get('BackMountingHoles',[]) if h.get('SinkDepth',0)>0]
                    if sinks:sink_examples.append({'file':name,'sinks':sinks})
                    elif slots:slot_examples.append({'file':name,'slots':slots})
                    for field in ('Angles','SocketAngles','BackAngles'):
                        assert all(-180<=model[field][axis]<=180 for axis in 'XYZ')
                    examples.append({'file':name,'format':settings['Version'],'angles':model['Angles']})
    assert len(examples)==expected_examples
    if has_shades:
        assert shade_examples=={15:(90,200,135,1),16:(185,185,185,2),17:(86,205,130,1),18:(115,190,145,0)}
    tilted=[e for e in examples if any(e['angles'].values())]
    assert len(tilted)==1 and tilted[0]['angles']=={'X':20,'Y':0,'Z':15}
    if args.format>=9:
        assert len(slot_examples)==1 and len(slot_examples[0]['slots'])==4
        assert all(h['SlotExtension']==8 and h['Diameter']==4 for h in slot_examples[0]['slots'])
        assert sum(h['Horizontal'] for h in slot_examples[0]['slots'])==2
    if args.format>=10:
        assert len(sink_examples)==1 and [h['SinkDepth'] for h in sink_examples[0]['sinks']]==[1.2,.8,.4]
        assert [h['SlotExtension'] for h in sink_examples[0]['sinks']]==[0,8,8]
    if args.format>=11:
        assert len(crop_examples)==1
        edits=crop_examples[0]['edits'];assert edits['CropAspectWidth']==4 and edits['CropAspectHeight']==5
        assert abs(edits['CropWidth']*600/(edits['CropHeight']*440)-.8)<1e-10
    if args.format>=12:
        assert len(size_examples)==1 and size_examples[0]['width']==120 and size_examples[0]['height']==88
    if args.format>=13:
        assert len(tab_examples)==1
        model=tab_examples[0]['model'];assert model['Width']==100 and model['Height']==80 and model['TabWidth']==12 and model['TabHeight']==14 and model['TabHoleDiameter']==4 and model['TabInset']==8
    if args.format>=14:
        if args.format>=15:
            assert len(layout_examples)==2
            assert next(e['options'] for e in layout_examples if e['options']['Arrangement']==2)=={'Margin':12,'Gap':9,'Arrangement':2,'BodyX':5,'BodyY':5,'AccessoryX':100,'AccessoryY':5}
            assert next(e['options'] for e in layout_examples if e['options']['Arrangement']==3)=={'Margin':10,'Gap':8,'Arrangement':3,'BodyX':20,'BodyY':25,'AccessoryX':120,'AccessoryY':100}
        else:assert len(layout_examples)==1 and layout_examples[0]['options']=={'Margin':12,'Gap':9,'Arrangement':2}
    hashes={p.name:sha256(p.read_bytes()).hexdigest().upper() for p in (archive,installer)}
    if has_shades:
        setup=Path('artifacts')/f'LithoCreate-Pro-{version}-Setup.exe'
        launcher=setup.read_bytes()
        assert launcher.startswith(b'MZ') and installer.read_bytes() in launcher
        hashes[setup.name]=sha256(launcher).hexdigest().upper()
    checksum=(Path('artifacts')/f'SHA256-{version}.txt').read_text(encoding='utf-8-sig')
    assert all(value in checksum for value in hashes.values())
    result={'result':'PASS','version':version,'portable_files_compared':len(files),'byte_exact_zip':True,'examples':sorted(examples,key=lambda e:e['file']),'slot_examples':slot_examples,'sink_examples':sink_examples,'crop_examples':crop_examples,'size_examples':size_examples,'tab_examples':tab_examples,'layout_examples':layout_examples,'sha256':hashes}
    (Path('artifacts')/f'package-verification-{version}.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'PASS: {len(files)} byte-exact ZIP files, {expected_examples} Format-{args.format} examples, one free-angle example, runtime and SHA-256 hashes.')


if __name__=='__main__': main()
