"""Independent standard-library checks of the 24 placement fixture STL/3MF files."""
import argparse
import json
import math
from pathlib import Path
import struct
import xml.etree.ElementTree as ET
from zipfile import ZipFile


def triangles(path):
    if path.suffix == '.stl':
        data = path.read_bytes()
        count = struct.unpack_from('<I', data, 80)[0]
        if len(data) != 84 + 50 * count:
            raise ValueError('Invalid binary STL length')
        return [tuple(struct.unpack_from('<3f', data, start + 12 + 12 * i) for i in range(3))
                for start in range(84, len(data), 50)]
    with ZipFile(path) as archive:
        root = ET.fromstring(archive.read('3D/3dmodel.model'))
    if root.attrib.get('unit') != 'millimeter':
        raise ValueError('3MF unit is not millimeter')
    ns = {'m': 'http://schemas.microsoft.com/3dmanufacturing/core/2015/02'}
    meshes = root.findall('m:resources/m:object/m:mesh', ns)
    if len(meshes) != 1:
        raise ValueError('Fixture must contain exactly one mesh')
    vertices = [tuple(float(v.attrib[c]) for c in 'xyz') for v in meshes[0].findall('m:vertices/m:vertex', ns)]
    return [tuple(vertices[int(t.attrib[c])] for c in ('v1', 'v2', 'v3'))
            for t in meshes[0].findall('m:triangles/m:triangle', ns)]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs', default='artifacts/tests-1.4.0')
    parser.add_argument('--output', default='artifacts/placement-files-1.4.0.json')
    args = parser.parse_args()
    expected = {'Upright': [40, 3, 30], 'UpsideDown': [40, 3, 30],
                'FaceUp': [40, 30, 3], 'FaceDown': [40, 30, 3],
                'RightEdgeDown': [30, 3, 40], 'LeftEdgeDown': [30, 3, 40]}
    results = []
    for orientation, dimensions in expected.items():
        for turn in (False, True):
            target = [dimensions[1], dimensions[0], dimensions[2]] if turn else dimensions
            for extension in ('stl', '3mf'):
                path = Path(args.inputs) / f'Placement-{orientation}-{turn}.{extension}'
                faces = triangles(path)
                points = [p for face in faces for p in face]
                if not points or not all(math.isfinite(v) for p in points for v in p):
                    raise ValueError(f'Invalid coordinates: {path.name}')
                size = [max(p[i] for p in points) - min(p[i] for p in points) for i in range(3)]
                if any(abs(a - b) > .001 for a, b in zip(size, target)) or abs(min(p[2] for p in points)) > .0001:
                    raise ValueError(f'Incorrect dimensions or Z=0: {path.name}, {size}')
                volume = sum(a[0] * (b[1] * c[2] - b[2] * c[1]) +
                             a[1] * (b[2] * c[0] - b[0] * c[2]) +
                             a[2] * (b[0] * c[1] - b[1] * c[0]) for a, b, c in faces) / 6
                if abs(volume - 3600) > .01:
                    raise ValueError(f'Incorrect signed volume: {path.name}, {volume}')
                results.append({'file': path.name, 'result': 'PASS', 'size_mm': size, 'volume_mm3': volume})
    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(results, indent=2), encoding='utf-8')
    print(f'PASS: {len(results)} independent STL/3MF placement checks; dimensions, positive volume, Z=0.')


if __name__ == '__main__':
    main()
