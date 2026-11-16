"""Verify the actual print-set files exported after undo and redo independently."""
import argparse
import json
from pathlib import Path
from check_layouts import verify


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs',default='artifacts/tests-1.19.0');parser.add_argument('--output',default='artifacts/history-files-1.19.0.json')
    args=parser.parse_args();root=Path(args.inputs);results=[]
    for name,positions in [('Undo',[[50,45],[120,100]]),('Redo',[[50,45],[130,100]])]:
        result=verify(root/f'History-{name}.3mf',[220,220,250],10,8,False,True,positions);result['arrangement']='manual';results.append(result)
    a,b=results
    assert all(pa['size_mm']==pb['size_mm'] and pa['triangles']==pb['triangles'] and pa['volume_mm3']==pb['volume_mm3'] for pa,pb in zip(a['parts'],b['parts']))
    Path(args.output).write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
    print('PASS: 2 undo/redo print sets; restored XY, unchanged geometry, closed edges, margins and gaps.')


if __name__=='__main__':main()
