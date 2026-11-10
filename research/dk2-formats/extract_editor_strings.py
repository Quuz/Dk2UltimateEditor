"""Extract native Win32 STRINGTABLE labels with IDs and PE resource provenance."""
from pathlib import Path
import hashlib,json,struct

ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'

def resource_leaves(data):
    pe=struct.unpack_from('<I',data,60)[0];n,opt=struct.unpack_from('<H',data,pe+6)[0],struct.unpack_from('<H',data,pe+20)[0]
    resource_rva=struct.unpack_from('<I',data,pe+24+112)[0];sections=[]
    for i in range(n):
        p=pe+24+opt+40*i;vs,rva,size,raw=struct.unpack_from('<4I',data,p+8);sections.append((rva,max(vs,size),raw))
    def raw_at(rva):
        for start,size,raw in sections:
            if start<=rva<start+size:return raw+rva-start
        raise ValueError(hex(rva))
    origin=raw_at(resource_rva);leaves=[]
    def walk(rel,path):
        p=origin+rel;a,b=struct.unpack_from('<HH',data,p+12)
        for i in range(a+b):
            key,target=struct.unpack_from('<II',data,p+16+8*i)
            if key&0x80000000:
                q=origin+(key&0x7fffffff);length=struct.unpack_from('<H',data,q)[0];key=data[q+2:q+2+2*length].decode('utf-16-le')
            if target&0x80000000:walk(target&0x7fffffff,path+[key])
            else:
                rva,size=struct.unpack_from('<II',data,origin+target);offset=raw_at(rva)
                leaves.append((path+[key],rva,offset,data[offset:offset+size]))
    walk(0,[]);return leaves

def extract(data):
    strings=[]
    for path,rva,offset,block in resource_leaves(data):
        if path[0]!=6:continue
        position=0
        for index in range(16):
            length=struct.unpack_from('<H',block,position)[0];position+=2
            if length:
                raw=block[position:position+2*length];assert len(raw)==2*length
                ident=(path[1]-1)*16+index
                strings.append(dict(id=ident,id_hex=hex(ident),text=raw.decode('utf-16-le'),resource_path=path,
                    text_file_offset=offset+position,text_rva=rva+position,utf16_bytes=2*length,
                    text_sha256=hashlib.sha256(raw).hexdigest(),block_sha256=hashlib.sha256(block).hexdigest()))
            position+=2*length
        assert position<=len(block)
    return strings

def main():
    b=EXE.read_bytes();strings=extract(b)
    result=dict(executable=str(EXE),executable_sha256=hashlib.sha256(b).hexdigest(),strings=strings,
        scope='Native resource strings only; a label is not a field binding or proof of gameplay.')
    (ROOT/'editor-strings.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Native editor strings:',len(strings))
    print([(s['id_hex'],s['text']) for s in strings if 0xF850<=s['id']<=0xF858])

if __name__=='__main__':main()
