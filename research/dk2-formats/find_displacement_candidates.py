"""Find x86 displacement byte candidates; these are NOT proven references.

Print candidates with nearest containing Flame mapping function. Branch offsets,
constants, unrelated objects and interior instruction bytes are false positives.
Each candidate needs disassembly/decompilation before being used as evidence.
"""
from pathlib import Path
import argparse,hashlib,json,re,struct

ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def find(fields):
    b=EXE.read_bytes();pe=struct.unpack_from('<I',b,60)[0]
    count=struct.unpack_from('<H',b,pe+6)[0];optional=struct.unpack_from('<H',b,pe+20)[0]
    base=struct.unpack_from('<I',b,pe+52)[0]
    for i in range(count):
        p=pe+24+optional+i*40
        if b[p:p+8].rstrip(b'\0')==b'.text':
            _,va,size,offset=struct.unpack_from('<4I',b,p+8)
            code=b[offset:offset+size];origin=base+va;break
    else:raise ValueError('No .text section')
    mapping=(ROOT/'sources/DKII_EXE_v170.sgmap').read_text(encoding='utf8')
    functions=[(int(a,16),name,int(size)) for a,name,size in re.findall(r'global: va=([0-9A-F]+),name=([^,\n]+),size=(\d+)',mapping)]
    result={}
    for field in fields:
        rows=[];p=0
        while True:
            p=code.find(struct.pack('<I',field),p)
            if p<0:break
            address=origin+p;containing=next(((a,n,s) for a,n,s in functions if a<=address<a+s),None)
            rows.append(dict(address=hex(address),function=hex(containing[0]) if containing else None,
                name=containing[1] if containing else None,nearby_hex=code[max(0,p-5):p+10].hex(' ')))
            p+=1
        result[hex(field)]=rows
    return dict(program_sha256=hashlib.sha256(b).hexdigest(),warning=__doc__,candidates=result)

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('offsets',nargs='+',type=lambda s:int(s,0));parser.add_argument('--output',type=Path)
    args=parser.parse_args();result=find(args.offsets)
    if args.output:args.output.write_text(json.dumps(result,indent=2),encoding='utf8')
    for field,rows in result['candidates'].items():
        print(field,len(rows),'candidates')
        for row in rows:print(row)

if __name__=='__main__':main()
