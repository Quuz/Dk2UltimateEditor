"""Derive phase169 verification helpers without editing frozen phase168 files."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
def main():
    old=json.loads((ROOT/'shot-combination-semantics.json').read_text(encoding='utf-8'))
    refs={r['file']:r['sha256'] for r in old['helpers']}
    def read(name):
        assert sha((ROOT/name).read_bytes())==refs[name]
        return (ROOT/name).read_text(encoding='utf-8')
    def write(name,text):
        p=ROOT/name
        if p.exists():assert p.read_text(encoding='utf-8')==text,name
        else:p.write_text(text,encoding='utf-8')
    text=read('prepare_shot_combinations_primary.py')
    text=text.replace('import emulate_shot_combinations as native','import emulate_shot_area_combinations as native')
    text=text.replace('native.p.pe_read','native.p.p.area.pe_read').replace('phase168','phase169')
    text=text.replace('phase169-shot-combinations-emulations','phase169-area-combinations-emulations')
    text=text.replace('phase169-shot-combinations-primary','phase169-area-combinations-primary')
    text=text.replace("INHERITED=[","INHERITED=['phase168-shot-combinations-primary.json','phase131-area-primary.json',",1)
    before="    return dict(profile=dict(file=str(native.EXE),sha256=sha(blob)),functions=fresh,inherited=inherited,blocks=blocks,"
    extra="    for a,h in {0x4AB27D:'6a40',0x4AB286:'c7864901000001000000',0x4AB30B:'6a02',0x4AB314:'85c07508'}.items():\n        assert native.p.p.area.pe_read(blob,a,len(bytes.fromhex(h))).hex()==h\n"
    assert text.count(before)==1;text=text.replace(before,extra+before)
    text=text.replace("        collision_order=dict(","        area_tick_order=dict(start_call=0x4AB281,start_return_ignored=True,tick_call=0x4AB30F,tick_return_test=0x4AB314,normal_dispatch=0x4AB329),\n        collision_order=dict(")
    write('prepare_shot_area_combinations_primary.py',text)
    text=read('validate_shot_combinations.py')
    text=text.replace('import emulate_shot_combinations as native','import emulate_shot_area_combinations as native').replace('phase168','phase169')
    text=text.replace('phase169-shot-combinations-storage','phase169-area-combinations-storage')
    before="""    patches=[dict(Data1=80,Data2=5,ShotProcessFlags=4,ProcessType=5,DamageType=7),
             dict(Data1=80,Data2=5,ShotProcessFlags=4,ProcessType=10,DamageType=7),
             dict(Data1=0xFFFFFFFF,Data2=0x10005,ShotProcessFlags=4,ProcessType=38,DamageType=7)]"""
    after="""    patches=[dict(Data1=80,Data2=2,ShotProcessFlags=2,ProcessType=30,DamageType=1),
             dict(Data1=50,Data2=2,ShotProcessFlags=2,ProcessType=31,DamageType=2),
             dict(Data1=2,Data2=2,ShotProcessFlags=2,ProcessType=17,DamageType=2),
             dict(Data1=80,Data2=2,ShotProcessFlags=64,ProcessType=39,DamageType=2)]"""
    assert text.count(before)==1;text=text.replace(before,after)
    text=text.replace('selected_diagnostic_collision_scenes','selected_bounded_area_scenes').replace('diagnostic collision','bounded area')
    write('validate_shot_area_combinations.py',text)
    print('Two phase169 helpers generated; frozen phase168 hashes verified.')
if __name__=='__main__':main()
