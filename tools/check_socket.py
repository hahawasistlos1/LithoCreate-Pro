"""Independent STL/3MF checks for the socket and matching receiver fixtures."""
import argparse
import json
import math
from pathlib import Path
from check_placements import triangles


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs', default='artifacts/tests-1.5.0')
    parser.add_argument('--output', default='artifacts/socket-files-1.5.0.json')
    args = parser.parse_args()
    results = []
    for extension in ('stl', '3mf'):
        socket_path = Path(args.inputs) / f'Socket-Reference.{extension}'
        receiver_path = Path(args.inputs) / f'Socket-Receiver.{extension}'
        faces = triangles(socket_path)
        points = set(p for face in faces for p in face)
        size = [max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
        assert all(abs(a-b)<.001 for a,b in zip(size,[60,60,8])),size
        assert abs(min(p[2] for p in points))<.0001
        top = [p for p in points if abs(p[2]-8)<.0001]
        peg_radius = max(math.hypot(p[0],p[1]) for p in top)
        hole_radius = min(math.hypot(p[0],p[1]) for p in top)
        segments = len(top)//2
        assert abs(peg_radius-16.75)<.001
        assert abs(hole_radius*math.cos(math.pi/segments)-4)<.001
        volume = sum(a[0]*(b[1]*c[2]-b[2]*c[1])+a[1]*(b[2]*c[0]-b[0]*c[2])+a[2]*(b[0]*c[1]-b[1]*c[0]) for a,b,c in faces)/6
        factor = segments*math.sin(math.tau/segments)/2
        expected = factor*((30**2-hole_radius**2)*5+(16.75**2-hole_radius**2)*3)
        assert abs(volume-expected)<.1,(volume,expected)
        receiver_points = set(p for face in triangles(receiver_path) for p in face)
        bottom = [p for p in receiver_points if abs(p[2])<.0001]
        radius = min(math.hypot(p[0],p[1]) for p in bottom)
        inner = [p for p in bottom if abs(math.hypot(p[0],p[1])-radius)<.001]
        free_radius = radius*math.cos(math.pi/len(inner))
        clearance = free_radius-peg_radius
        assert abs(free_radius-17)<.001 and abs(clearance-.25)<.001
        results.append({'socket':socket_path.name,'receiver':receiver_path.name,'result':'PASS','size_mm':size,'volume_mm3':volume,'free_cable_diameter_mm':hole_radius*2*math.cos(math.pi/segments),'radial_clearance_mm':clearance})
    output = Path(args.output)
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(results,indent=2),encoding='utf-8')
    print('PASS: Four independent STL/3MF file checks; socket dimensions, volume, cable opening, receiver clearance.')


if __name__ == '__main__':
    main()
