"""Find initialized non-executable offset-table candidates, not field semantics.

Search DWORD/WORD values matching unresolved definition offsets in two exact
game EXEs. Adjacent numeric constants are not proof of a descriptor table.
Code references to candidate data addresses must be type-traced separately.
"""
from pathlib import Path
import json,hashlib,struct
ROOT=Path(__file__).resolve().parent
TARGETS={0x75C:['Creature.AbilityJump'],0x6EB:['Creature.PickUpPriority'],0x3A:['KeeperSpells.C8'],0x98:['Traps.204'],0x106:['Objects.34C'],0x183:['Terrain.198'],0x1B5:['Terrain.224']}

def sections(data):
    pe=struct.unpack_from('<I',data,60)[0];n=struct.unpack_from('<H',data,pe+6)[0];opt=struct.unpack_from('<H',data,pe+20)[0];base=struct.unpack_from('<I',data,pe+52)[0]
    for i in range(n):
        o=pe+24+opt+40*i;name=data[o:o+8].rstrip(b'\0').decode('ascii');vs,rva,rs,raw=struct.unpack_from('<4I',data,o+8);flags=struct.unpack_from('<I',data,o+36)[0]
        yield dict(name=name,address=base+rva,virtual_bytes=vs,raw_bytes=rs,raw_offset=raw,flags=flags),data[raw:raw+rs]

def main():
    out=[]
    for exe in [ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',ROOT/'source-binaries/DKII-local13.exe']:
        data=exe.read_bytes();rows=[];scanned=[]
        for sec,b in sections(data):
            if sec['flags']&0x20000000 or not sec['flags']&0x40000000 or sec['name'] in ['.rsrc','.reloc']:continue
            scanned.append(sec)
            for value,names in TARGETS.items():
                for width in [4,2]:
                    needle=value.to_bytes(width,'little');at=b.find(needle)
                    while at>=0:
                        # Keep unaligned candidates: DK2 structures are packed.
                        begin=max(0,at-24);end=min(len(b),at+28)
                        rows.append(dict(address=sec['address']+at,section=sec['name'],value=value,width=width,field_hints=names,context_address=sec['address']+begin,context_hex=b[begin:end].hex()))
                        at=b.find(needle,at+1)
        out.append(dict(exe=str(exe),sha256=hashlib.sha256(data).hexdigest(),sections=scanned,candidates=rows))
    (ROOT/'phase64-offset-table-candidates.json').write_text(json.dumps(dict(scope=__doc__,programs=out),indent=2),encoding='utf8')
    for s in out:
        print(Path(s['exe']).name,len(s['candidates']),'candidates')
        for r in s['candidates']:
            if r['value'] in [0x75C,0x6EB] and r['width']==4:print(hex(r['address']),r['field_hints'],r['context_hex'])
if __name__=='__main__':main()
