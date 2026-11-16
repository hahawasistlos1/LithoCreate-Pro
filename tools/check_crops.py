"""Independent PNG pixel-crop checks and native project-selection verification."""
import argparse
from hashlib import sha256
import json
import math
from pathlib import Path
import struct
from zipfile import ZipFile
import zlib


def png(data):
    assert data[:8]==b'\x89PNG\r\n\x1a\n';offset=8;packed=bytearray();header=None
    while offset<len(data):
        length=struct.unpack_from('>I',data,offset)[0];kind=data[offset+4:offset+8];chunk=data[offset+8:offset+8+length]
        assert zlib.crc32(kind+chunk)&0xffffffff==struct.unpack_from('>I',data,offset+8+length)[0]
        if kind==b'IHDR':header=struct.unpack('>IIBBBBB',chunk)
        if kind==b'IDAT':packed.extend(chunk)
        offset+=12+length
    w,h,depth,color,compression,filtering,interlace=header
    assert depth==8 and color in (0,2,4,6) and compression==filtering==interlace==0
    channels={0:1,2:3,4:2,6:4}[color];stride=w*channels;raw=zlib.decompress(packed);assert len(raw)==h*(stride+1)
    rows=[];previous=bytearray(stride)
    for j in range(h):
        kind=raw[j*(stride+1)];row=bytearray(raw[j*(stride+1)+1:(j+1)*(stride+1)])
        for i in range(stride):
            a=row[i-channels] if i>=channels else 0;b=previous[i];c=previous[i-channels] if i>=channels else 0
            p=a+b-c;pa,pb,pc=abs(p-a),abs(p-b),abs(p-c);paeth=a if pa<=pb and pa<=pc else b if pb<=pc else c
            assert kind in (0,1,2,3,4);row[i]=(row[i]+(0,a,b,(a+b)//2,paeth)[kind])&255
        pixels=[]
        for i in range(w):
            pixel=row[i*channels:(i+1)*channels]
            pixels.append(tuple(pixel[:3])+(pixel[3] if color==6 else 255,) if color in (2,6) else (pixel[0],pixel[0],pixel[0],pixel[1] if color==4 else 255))
        rows.append(pixels);previous=row
    return w,h,rows


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--inputs',default='artifacts/tests-1.12.0');parser.add_argument('--output',default='artifacts/crop-files-1.12.0.json');parser.add_argument('--native',action='store_true');args=parser.parse_args();root=Path(args.inputs);results=[]
    if args.native:
        with ZipFile(root/'Original.lithopro') as z:before=json.loads(z.read('project.json'));source=z.read('source.bin')
        with ZipFile(root/'Ausschnitt-Test.lithopro') as z:saved=json.loads(z.read('project.json'));assert source==z.read('source.bin')
        assert saved['Version']==11 and saved['Name']=='Ausschnitt-Test';e=saved['Edits'];w,h,_=png(source)
        assert e['CropAspectWidth']==16 and e['CropAspectHeight']==9 and e['CropAspectLock']
        assert e['CropWidth']>0 and e['CropHeight']>0 and 0<=e['CropX']<=1-e['CropWidth']+1e-10 and 0<=e['CropY']<=1-e['CropHeight']+1e-10
        assert abs(e['CropWidth']*w/(e['CropHeight']*h)-16/9)<1e-8
        assert e['CropWidth']<.99 and e['CropX']>.001,'Expected an actual off-center mouse selection'
        allowed=('CropX','CropY','CropWidth','CropHeight','CropAspectWidth','CropAspectHeight','CropAspectLock')
        before['Name']=saved['Name']
        for field in allowed:before['Edits'][field]=e[field]
        assert before==saved,'Unexpected project setting changed'
        results.append({'result':'PASS','format':11,'source_size_px':[w,h],'region':{k:e[k] for k in allowed},'selected_aspect':16/9,'source_bytes_unchanged':True,'other_settings_unchanged':True,'source_sha256':sha256(source).hexdigest()})
    else:
        sw,sh,source=png((root/'CropSource.png').read_bytes());assert (sw,sh)==(120,100)
        for name,a,b in (('4x5',4,5),('16x9',16,9),('5x2',5,2)):
            ratio=a/b;cropw=min(sw,sh*ratio);croph=min(sh,sw/ratio);x=int((sw-cropw)/2);y=int((sh-croph)/2)
            w,h,pixels=png((root/f'Crop-{name}.png').read_bytes())
            assert abs(w-cropw)<1.000001 and abs(h-croph)<1.000001
            errors=[abs(pixels[j][i][c]-source[j+y][i+x][c]) for j in range(h) for i in range(w) for c in range(3)]
            assert max(errors)<=1,(name,max(errors));assert all(p[3]==255 for row in pixels for p in row)
            results.append({'file':f'Crop-{name}.png','result':'PASS','size_px':[w,h],'source_origin_px':[x,y],'pixels_compared':w*h,'max_channel_error':max(errors),'aspect':ratio})
    Path(args.output).write_text(json.dumps(results,indent=2),encoding='utf-8');print(f'PASS: {len(results)} independent crop checks; source coordinates, aspect, pixel dimensions and unchanged data.')


if __name__=='__main__':main()
