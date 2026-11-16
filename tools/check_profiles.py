"""Independent JSON checks of printer profiles and native project/library isolation."""
import argparse
from hashlib import sha256
import json
import math
from pathlib import Path
from zipfile import ZipFile

FIELDS={'Name','Process','BedX','BedY','BuildZ','Nozzle','Layer','Material'}


def catalog(path):
    items=json.loads(path.read_text(encoding='utf-8-sig'));assert isinstance(items,list) and len(items)<=100
    assert len({p['Name'].strip().casefold() for p in items})==len(items)
    for p in items:
        assert set(p)==FIELDS and p['Name'].strip() and len(p['Name'])<=256
        assert isinstance(p['Material'],str) and isinstance(p['Process'],str)
        assert all(math.isfinite(p[k]) and 0<p[k]<=2000 for k in ('BedX','BedY','BuildZ','Nozzle','Layer'))
    return items


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--inputs',default='artifacts/tests-1.17.0');parser.add_argument('--output',default='artifacts/profile-files-1.17.0.json');parser.add_argument('--native',action='store_true');args=parser.parse_args();root=Path(args.inputs)
    if args.native:
        items=catalog(root/'data/printers.json');assert len(items)==3
        expected={'Name':'Mein N95 · Fein','Process':'FDM','BedX':180,'BedY':220,'BuildZ':250,'Nozzle':.4,'Layer':.12,'Material':'PLA, Weiß'}
        assert items[-1]==expected and not any(p['Name']=='Mein N95 · PLA' for p in items)
        with ZipFile(root/'Original.lithopro') as z:before=json.loads(z.read('project.json'));source=z.read('source.bin')
        with ZipFile(root/'Profil-Test.lithopro') as z:after=json.loads(z.read('project.json'));assert source==z.read('source.bin')
        assert before['Version']==after['Version']==14 and after['Printer']==expected
        before['Name']='Profil-Test';before['Printer']['Name']=expected['Name'];before['Printer']['BedX']=180;assert before==after,'Unexpected photo/model/layout change'
        result={'result':'PASS','profile_count':3,'saved_profile':expected,'project_matches_profile':True,'source_unchanged':True,'other_settings_unchanged':True,'source_sha256':sha256(source).hexdigest()}
    else:
        items=catalog(root/'printer-profiles.json');assert len(items)==2 and items[0]['BedX']==180 and items[1]['Process']=='Resin' and items[1]['BedX']==143 and items[1]['BedY']==89 and items[1]['Layer']==.05
        assert catalog(root/'printer-legacy.json')==[]
        result={'result':'PASS','legacy_array_format':True,'profiles':items,'empty_library_preserved':True}
    Path(args.output).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print('PASS: printer JSON schema, stored dimensions, material/process and '+('native project/source isolation.' if args.native else 'empty library.'))


if __name__=='__main__':main()
