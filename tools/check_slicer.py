"""Optional independent Bambu Studio import/export check for generated test models."""
import argparse
import json
import math
from pathlib import Path
import struct
import subprocess
import xml.etree.ElementTree as ET
from zipfile import ZipFile


def geometry(path):
    vertices = []
    triangles = 0
    if path.suffix.lower() == '.stl':
        data = path.read_bytes()
        triangles = struct.unpack_from('<I', data, 80)[0]
        if len(data) != 84 + triangles * 50:
            raise ValueError('Ungültige binäre STL-Länge')
        for offset in range(84, len(data), 50):
            vertices.extend(struct.unpack_from('<3f', data, offset + 12 + i * 12) for i in range(3))
    else:
        with ZipFile(path) as archive:
            for name in archive.namelist():
                if not name.lower().endswith('.model'):
                    continue
                root = ET.fromstring(archive.read(name))
                for element in root.iter():
                    tag = element.tag.rsplit('}', 1)[-1]
                    if tag == 'vertex':
                        vertices.append(tuple(float(element.attrib[axis]) for axis in 'xyz'))
                    elif tag == 'triangle':
                        triangles += 1
    if not vertices or not triangles or not all(math.isfinite(v) for p in vertices for v in p):
        raise ValueError('Keine endliche Modellgeometrie gefunden')
    size = [max(p[i] for p in vertices) - min(p[i] for p in vertices) for i in range(3)]
    return {'triangles': triangles, 'size_mm': size}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--bambu', default=r'C:\Program Files\Bambu Studio\bambu-studio.exe')
    parser.add_argument('--inputs', default='artifacts/tests-1.1.0')
    parser.add_argument('--output', default='artifacts/slicer-1.1.0')
    args = parser.parse_args()
    destination = Path(args.output).resolve()
    destination.mkdir(parents=True, exist_ok=True)
    inputs = Path(args.inputs).resolve()
    results = []
    for name in ['Base-Cylinder-6.stl', 'Stand-Flat.stl', 'Opening-Sphere-30-4.3mf']:
        source = inputs / name
        working = destination / source.stem
        working.mkdir(exist_ok=True)
        exported = working / 'bambu-roundtrip.3mf'
        if exported.exists():
            exported.unlink()
        run = subprocess.run([args.bambu, '--export-3mf', str(exported), str(source)],
                             cwd=working, capture_output=True, text=True, errors='replace', timeout=90)
        (working / 'console.txt').write_text(run.stdout + run.stderr, encoding='utf-8')
        report = working / 'result.json'
        status = json.loads(report.read_text(encoding='utf-8-sig')) if report.exists() else {}
        if run.returncode != 0 or status.get('return_code', 0) != 0 or not exported.exists():
            raise RuntimeError(f'Bambu-Konvertierung fehlgeschlagen: {source.name}, Exit {run.returncode}, {status}')
        before, after = geometry(source), geometry(exported)
        if before['triangles'] != after['triangles'] or any(abs(a-b) > .02 for a,b in zip(before['size_mm'],after['size_mm'])):
            raise RuntimeError(f'Geometrie verändert: {source.name}: {before} -> {after}')
        results.append({'file': source.name, 'result': 'PASS', 'input': before, 'bambu_export': after})
        print(f'PASS Bambu-Roundtrip: {source.name} - {before["triangles"]} Dreiecke', flush=True)
    (destination / 'verification.json').write_text(json.dumps(results, indent=2, ensure_ascii=False), encoding='utf-8')


if __name__ == '__main__':
    main()
