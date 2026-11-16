"""Independent checks of manually positioned two-object 3MF and project coordinates."""
import argparse
from hashlib import sha256
import json
from pathlib import Path
from zipfile import ZipFile
from check_layouts import verify


def project(path):
    with ZipFile(path) as z:return json.loads(z.read('project.json')),z.read('source.bin')


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs',default='artifacts/tests-1.18.0');parser.add_argument('--output',default='artifacts/manual-files-1.18.0.json');parser.add_argument('--native',action='store_true')
    args=parser.parse_args();root=Path(args.inputs);results=[]
    if args.native:
        before,source=project(root/'Original.lithopro');after,after_source=project(root/'Positions-Test.lithopro')
        assert source==after_source and after['Version']==15
        layout=after['PrintLayout'];assert layout['Arrangement']==3 and layout['Margin']==10 and layout['Gap']==8
        assert 35<=layout['BodyX']<=85 and 30<=layout['BodyY']<=75,'Body was not moved to the intended bed area'
        assert layout['AccessoryX']==120 and layout['AccessoryY']==100,'Accessory moved unexpectedly'
        before['Name']='Positions-Test';before['PrintLayout']=layout;assert before==after,'Unexpected image, geometry, printer or edit change'
        positions=[[layout['BodyX'],layout['BodyY']],[layout['AccessoryX'],layout['AccessoryY']]]
        exports=list(root.glob('*.3mf'));assert len(exports)==1
        result=verify(exports[0],[220,220,250],10,8,False,True,positions)
        result.update({'arrangement':'manual','source_unchanged':True,'other_project_settings_unchanged':True,'source_sha256':sha256(source).hexdigest()});results.append(result)
    else:
        for name,positions in [('Original',[[10,10],[65,50]]),('Moved',[[15,15],[70,60]]),('Edge',[[5,5],[75,65]]),('FromAuto',[[30,17.5],[40,52.5]])]:
            result=verify(root/f'Manual-{name}.3mf',[100,80,50],5,5,False,positions=positions);result['arrangement']='manual';results.append(result)
        data,source=project(root/'manual-project.lithopro');assert data['Version']==15 and source and data['PrintLayout']=={'Margin':5,'Gap':5,'Arrangement':3,'BodyX':10,'BodyY':10,'AccessoryX':65,'AccessoryY':50}
    Path(args.output).write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'PASS: {len(results)} manual 3MF layouts; project XY, exact positions, dimensions, volumes, closed edges, margins and gaps.')


if __name__=='__main__':main()
