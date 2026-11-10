"""Generate phase172 helpers from verified frozen phase170/171 components."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def main():
    refs={r['file']:r['sha256'] for schema in ['shot-target-combination-semantics.json','jack-in-box-semantics.json'] for r in json.loads((ROOT/schema).read_text(encoding='utf-8'))['helpers']}
    def read(name):
        assert sha((ROOT/name).read_bytes())==refs[name],name
        return (ROOT/name).read_text(encoding='utf-8')
    def write(name,s):
        file=ROOT/name
        if file.exists():assert file.read_text(encoding='utf-8')==s,name
        else:file.write_text(s,encoding='utf-8')
    s=read('crosscheck_shot_target_combinations.py').replace('import emulate_shot_target_combinations as native','import emulate_shot_instant_experience as native')
    s=s.replace('phase171','phase172').replace('target-','instant-xp-')
    start=s.index('CONTROLS=');end=s.index('\nRESULT,',start)
    s=s[:start]+"CONTROLS=['xp-source-mask1-whole-tick','xp-collision-level1','xp-full-health','xp-unsigned-max','xp-tick2-abort','instant-source-area','instant-area-moved','instant-area-moved-no-target','instant-source-hit-zero','instant-hit-live-repeat']"+s[end:]
    s=s.replace('max_steps=1000000','max_steps=2000000')
    write('crosscheck_shot_instant_experience.py',s)
    s=read('prepare_shot_target_primary.py').replace('import emulate_shot_target_combinations as native','import emulate_shot_instant_experience as native')
    s=s.replace('phase171','phase172').replace('target-','instant-xp-')
    s=s.replace("INHERITED=[","INHERITED=['phase171-target-primary.json','phase131-area-primary.json',",1)
    s=s.replace('BLOCKS=[','BLOCKS=[(0x66D754,0x100),',1)
    write('prepare_shot_instant_primary.py',s)
    s=read('validate_jack_storage.py').replace('import emulate_jack_in_box as native','import emulate_shot_instant_experience as native')
    s=s.replace('phase170','phase172').replace('jack-storage','instant-xp-storage')
    start=s.index('    specs=');end=s.index('\n    out=',start)
    s=s[:start]+'''    specs=[('Shots',0x104,load('area-damage-semantics.json')['corpus']['files'],
        [dict(Data1=50,Data2=0,ShotProcessFlags=4,DamageType=7,ProcessType=7),
         dict(Data1=80,Data2=2,ShotProcessFlags=2,DamageType=2,ProcessType=34),
         dict(Data1=80,Data2=0,ShotProcessFlags=2,DamageType=7,ProcessType=34)]),
        ('Traps',0x82,load('trap-creation-activation-semantics.json')['storage_validation']['files'],
        [dict(TrapId=17,ShotTypeId=58,ShotData1=80,ShotData2=2),
         dict(TrapId=17,ShotTypeId=89,ShotData1=80,ShotData2=0)])]'''+s[end:]
    s=s.replace('total==328','total==410')
    write('validate_shot_instant_storage.py',s)
    queries=[dict(id='phase172-process-dispatch-xrefs',tool='get_xrefs_to',args=dict(program='DKII-DX.exe',address='0x4ad730',limit=100),print_limit=0)]
    for a in [0x4AD730,0x4AB180,0x4ABD60,0x4AE630,0x457880,0x48B120,0x4AFCB0,0x51DC40,0x441990,0x4363F0]:
        queries.append(dict(id=f'phase172-order-{a:06x}-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0))
    write('phase172-order-queries.json',json.dumps(queries,indent=2))
    print('Three helpers and exact canonical function order queries generated.')

if __name__=='__main__':main()
