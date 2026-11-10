"""New Terrain getter-rooted all-PE search, including forwarded arguments.

Derived from the bounded phase72 engine. Tracks constant aliases and ESP/EBP
spills through up to three direct call levels. Virtual slot70 is a lead only;
every resulting unknown-field match needs independent Terrain identity proof.
Does not repeat the earlier displacement-only and adjusted-pointer inventories.
"""
from pathlib import Path
import json,struct
from build_effect_semantics import pe_read

ROOT=Path(__file__).resolve().parent

def main():
    source=(ROOT/'scan_keeper_pointer_arguments.py').read_text(encoding='utf8')
    source=repr(__doc__)+'\n'+source[source.index('from pathlib import Path'):]
    old="TARGETS={'KeeperSpells':[(0x3A,4,'unknownC8'),(0x3E,4,'controlRecharge'),(0x58,4,'controlManaCost'),(0x42,1,'controlShotId')]}"
    new="TARGETS={'Terrain':[(0x183,2,'unknown198'),(0x1B5,4,'unknown224'),(0x17B,4,'controlFlags'),(0x17F,2,'controlRaw188'),(0x181,2,'controlRaw196')]}"
    assert old in source
    source=source.replace(old,new)
    rows=[]
    for label,exe,vtable,expected in [
        ('DX',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',0x66E3EC,0x508DD0),
        ('old',ROOT/'source-binaries/DKII-local13.exe',0x65F31C,None),
    ]:
        data=exe.read_bytes();getter=struct.unpack('<I',pe_read(data,vtable+0x70,4))[0]
        if expected is not None:assert getter==expected
        body=source.replace("getters={0x50DD60:'KeeperSpells'};slots={0x110:'KeeperSpells'}",f"getters={{{getter}:'Terrain'}};slots={{0x70:'Terrain'}}")
        body=body.replace("data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()",f"data=Path({str(exe)!r}).read_bytes()")
        body=body.replace('phase72-DX-keeper-pointer-argument-leads.json',f'phase74-{label}-terrain-argument-leads.json')
        assert 'KeeperSpells' not in body and str(getter) in body
        (ROOT/f'scan_{label.lower()}_terrain_arguments.py').write_text(body,encoding='utf8')
        rows.append(dict(program=exe.name,exe=str(exe),world_vtable=vtable,slot=0x70,getter=getter))
    (ROOT/'phase74-terrain-getter-seeds.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
    print('Generated two new Terrain-only searches:',rows)

if __name__=='__main__':main()
