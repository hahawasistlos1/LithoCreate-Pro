"""Independently verify preset output geometry and native preset/project isolation."""
import argparse
from hashlib import sha256
import json
from pathlib import Path
from zipfile import ZipFile
from check_dimensions import verify


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs',default='artifacts/tests-1.15.0')
    parser.add_argument('--output',default='artifacts/preset-files-1.15.0.json')
    parser.add_argument('--native',action='store_true')
    args=parser.parse_args();root=Path(args.inputs)
    if args.native:
        with ZipFile(root/'Original.lithopro') as z:before=json.loads(z.read('project.json'));source=z.read('source.bin')
        with ZipFile(root/'Vorlagen-Test.lithopro') as z:after=json.loads(z.read('project.json'));assert source==z.read('source.bin')
        catalog=json.loads((root/'data/model-presets.json').read_text(encoding='utf-8-sig'))
        assert catalog['Version']==1 and catalog['ModelVersion']==min(before['Version'],13) and len(catalog['Items'])==1
        item=catalog['Items'][0]
        assert set(item)=={'Id','Name','Model'} and item['Name']=='Leuchtturm · Wiederverwendbar'
        assert item['Model']==before['Model']
        before['Name']='Vorlagen-Test';assert before==after,'Preset changed image/printer/other project data'
        result={'result':'PASS','project_format':after['Version'],'preset_name':item['Name'],'photo_edits_printer_unchanged':True,'model_matches_preset':True,'source_sha256':sha256(source).hexdigest()}
    else:
        catalog=json.loads((root/'model-presets.json').read_text(encoding='utf-8-sig'))
        assert catalog['Version']==1 and catalog['ModelVersion']==13 and len(catalog['Items'])==16
        assert len({p['Id'] for p in catalog['Items']})==16
        assert {p['Model']['Shape'] for p in catalog['Items']}==set(range(15))
        assert all(set(p)=={'Id','Name','Model'} for p in catalog['Items'])
        result={'result':'PASS','catalog_shapes':15,'files':[verify(root/f'Preset-Applied.{ext}',[40,30,3],3600) for ext in ('stl','3mf')]}
    Path(args.output).write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print('PASS: native photo/project isolation and model preset.' if args.native else 'PASS: 15 preset shapes, isolated metadata, two closed STL/3MF meshes, dimensions and volume.')


if __name__=='__main__':main()
