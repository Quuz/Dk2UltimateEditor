"""Read Win32 dialog resources directly from the supplied Editor Pro PE."""
from pathlib import Path
import hashlib,json,struct

ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'

def extract(data):
    pe=struct.unpack_from('<I',data,60)[0]
    n,opt=struct.unpack_from('<H',data,pe+6)[0],struct.unpack_from('<H',data,pe+20)[0]
    rsrc_rva=struct.unpack_from('<I',data,pe+24+112)[0]
    sections=[]
    for i in range(n):
        p=pe+24+opt+40*i
        vs,rva,size,raw=struct.unpack_from('<4I',data,p+8)
        sections.append((rva,max(vs,size),raw))
    def raw_at(rva):
        for base,size,raw in sections:
            if base<=rva<base+size:return raw+rva-base
        raise ValueError(hex(rva))
    origin=raw_at(rsrc_rva)
    leaves=[]
    def walk(rel,path):
        p=origin+rel;n1,n2=struct.unpack_from('<2H',data,p+12)
        for i in range(n1+n2):
            key,target=struct.unpack_from('<2I',data,p+16+8*i)
            if key&0x80000000:
                q=origin+(key&0x7fffffff);length=struct.unpack_from('<H',data,q)[0]
                key=data[q+2:q+2+length*2].decode('utf-16-le')
            if target&0x80000000:walk(target&0x7fffffff,path+[key])
            else:
                rva,size=struct.unpack_from('<2I',data,origin+target)
                leaves.append((path+[key],data[raw_at(rva):raw_at(rva)+size]))
    walk(0,[])
    dialogs=[]
    for path,b in leaves:
        if path[0]!=5:continue
        pos=0
        def take(fmt):
            nonlocal pos
            v=struct.unpack_from(fmt,b,pos);pos+=struct.calcsize(fmt)
            return v[0] if len(v)==1 else v
        def string():
            nonlocal pos
            first=take('<H')
            if first==0:return ''
            if first==65535:return dict(ordinal=take('<H'))
            units=[first]
            while True:
                v=take('<H')
                if not v:break
                units.append(v)
            return struct.pack('<'+'H'*len(units),*units).decode('utf-16-le',errors='surrogatepass')
        extended=b[:4]==b'\x01\x00\xff\xff'
        if extended:
            version,signature,helpid,exstyle,style,count,x,y,cx,cy=take('<HHIIIHhhhh')
        else:style,exstyle,count,x,y,cx,cy=take('<IIHhhhh')
        menu,window_class,title=string(),string(),string()
        font=None
        if style&0x40:
            point=take('<H')
            if extended:weight,italic,charset=take('<HBB')
            font=dict(points=point,name=string())
        controls=[]
        for i in range(count):
            pos=(pos+3)&~3
            if extended:hid,ex,st,xx,yy,w,h,ident=take('<IIIhhhhI')
            else:st,ex,xx,yy,w,h,ident=take('<IIhhhhH')
            cls,label=string(),string();extra=take('<H')
            if extra:pos+=extra
            controls.append(dict(id=ident,title=label,window_class=cls,style=hex(st),
                template_visible=bool(st&0x10000000),template_disabled=bool(st&0x08000000),
                x=xx,y=yy,width=w,height=h))
        assert pos<=len(b),(path,pos,len(b))
        dialogs.append(dict(resource_path=path,title=title,extended=extended,font=font,
            resource_bytes=len(b),parsed_bytes=pos,trailing_hex=b[pos:].hex(),controls=controls))
    return dialogs

def main():
    data=EXE.read_bytes();dialogs=extract(data)
    output=dict(input=str(EXE),sha256=hashlib.sha256(data).hexdigest(),dialogs=dialogs)
    (ROOT/'editor-dialogs.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8')
    print('Editor dialog resources:',len(dialogs))
    print('Controls:',sum(len(d['controls']) for d in dialogs),'; template visibility/disabled flags recorded')

if __name__=='__main__':main()
