"""Independent checks of the six removable-back/receiver STL and 3MF fixtures."""
import argparse
import json
import math
from pathlib import Path
from check_placements import triangles


def volume(faces):
    return sum(a[0]*(b[1]*c[2]-b[2]*c[1])+a[1]*(b[2]*c[0]-b[0]*c[2])+a[2]*(b[0]*c[1]-b[1]*c[0]) for a,b,c in faces)/6


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs', default='artifacts/tests-1.6.0')
    parser.add_argument('--output', default='artifacts/back-files-1.6.0.json')
    args = parser.parse_args()
    results = []
    w, h, rim = 73.5, 53.5, 1.6
    solid_volume = 80*60*2 + (w*h-(w-2*rim)*(h-2*rim))*3
    for ext in ('stl', '3mf'):
        for name in ('Back-Closed', 'Back-Reference', 'Back-Receiver'):
            path = Path(args.inputs)/f'{name}.{ext}'
            faces = triangles(path)
            points = set(p for f in faces for p in f)
            assert all(math.isfinite(c) for p in points for c in p)
            size = [max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
            expected = [80, 60, 22] if name == 'Back-Receiver' else [80, 5, 60]
            assert all(abs(a-b)<.001 for a,b in zip(size,expected)), (path.name,size)
            assert abs(min(p[2] for p in points))<.0001
            measured = volume(faces)
            details = {}
            if name == 'Back-Receiver':
                # Receiver was laid flat before export; the open rear rim lies on Z=0.
                rear = [p for p in points if abs(p[2])<.0001]
                inner = [p for p in rear if abs(p[0])<39.999 and .001<p[1]<59.999]
                opening = [max(p[i] for p in inner)-min(p[i] for p in inner) for i in (0,1)]
                assert all(abs(a-b)<.001 for a,b in zip(opening,[74,54])),opening
                expected_volume = (80*60-74*54)*22+74*54*.8
                assert abs(measured-expected_volume)<.2,(measured,expected_volume)
                details['opening_mm'] = opening
            else:
                # Upright cover: original Z becomes -Y, original height becomes export Z.
                tip = [p for p in points if abs(p[1]+5)<.0001]
                outer = [max(p[i] for p in tip)-min(p[i] for p in tip) for i in (0,2)]
                assert all(abs(a-b)<.001 for a,b in zip(outer,[w,h])),outer
                details['clearance_per_side_mm'] = [(74-outer[0])/2,(54-outer[1])/2]
                if name == 'Back-Closed':
                    assert abs(measured-solid_volume)<.1,(measured,solid_volume)
                else:
                    hole = sorted((p for p in points if abs(p[1])<.0001 and math.hypot(p[0],p[2]-30)<5),key=lambda p:math.atan2(p[2]-30,p[0]))
                    assert len(hole)>=32
                    area = abs(sum(a[0]*(b[2]-30)-b[0]*(a[2]-30) for a,b in zip(hole,hole[1:]+hole[:1])))/2
                    free = math.inf
                    for a,b in zip(hole,hole[1:]+hole[:1]):
                        ax,ay=a[0],a[2]-30;bx,by=b[0],b[2]-30
                        dx,dy=bx-ax,by-ay
                        t=max(0,min(1,-(ax*dx+ay*dy)/(dx*dx+dy*dy)))
                        free=min(free,math.hypot(ax+t*dx,ay+t*dy))
                    assert 3.9999<=free<4.002,free
                    assert abs(measured-(solid_volume-area*2))<.1,(measured,solid_volume-area*2)
                    details['free_cable_diameter_mm']=2*free
            results.append({'file':path.name,'result':'PASS','size_mm':size,'volume_mm3':measured,**details})
    Path(args.output).write_text(json.dumps(results,indent=2),encoding='utf-8')
    print('PASS: Six independent STL/3MF checks; receiver, cover, rim clearance, volume, cable hole.')


if __name__ == '__main__':
    main()
