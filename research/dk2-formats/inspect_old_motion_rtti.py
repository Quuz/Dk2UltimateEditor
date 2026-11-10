from pathlib import Path
import struct,json
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    data=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();nt=struct.unpack_from('<I',data,60)[0];ns=struct.unpack_from('<H',data,nt+6)[0];opt=struct.unpack_from('<H',data,nt+20)[0];base=struct.unpack_from('<I',data,nt+52)[0];rows=[]
    for i in range(ns):
        at=nt+24+opt+40*i;vs,va,rs,fo=struct.unpack_from('<4I',data,at+8);name=data[at:at+8].rstrip(b'\0').decode();blob=data[fo:fo+rs]
        if name!='.rdata':continue
        for off in range(0,len(blob)-4,4):
            if struct.unpack_from('<I',blob,off)[0]!=0x4a8c00:continue
            address=base+va+off;print('method pointer',hex(address))
            for back in range(0,0x260,4):
                vtable=address-back
                try:
                    locator=struct.unpack('<I',pe_read(data,vtable-4,4))[0];sig,offset,cd,typ,hierarchy=struct.unpack('<5I',pe_read(data,locator,20));kind=pe_read(data,typ+8,96).split(b'\0')[0]
                    if sig==0 and kind.startswith(b'.?AV'):
                        r=dict(vtable=vtable,slot=back,locator=locator,type_descriptor=typ,type_name=kind.decode(),method_pointer_address=address);rows.append(r);print(r);break
                except (ValueError,AssertionError,struct.error):pass
    (ROOT/'phase100-old-motion-rtti.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
if __name__=='__main__':main()
