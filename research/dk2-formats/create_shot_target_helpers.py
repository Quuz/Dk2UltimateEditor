"""Derive phase171 helpers from SHA-verified frozen phase168 evidence."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def main():
    prior=json.loads((ROOT/'shot-combination-semantics.json').read_text(encoding='utf-8'))
    refs={r['file']:r['sha256'] for r in prior['helpers']}
    def read(name):
        assert sha((ROOT/name).read_bytes())==refs[name]
        return (ROOT/name).read_text(encoding='utf-8')
    def write(name,s):
        file=ROOT/name
        if file.exists():assert file.read_text(encoding='utf-8')==s,name
        else:file.write_text(s,encoding='utf-8')
    s=read('crosscheck_shot_combinations.py').replace('import emulate_shot_combinations as native','import emulate_shot_target_combinations as native')
    s=s.replace('phase168','phase171').replace('shot-combinations','target')
    start=s.index('CONTROLS=');end=s.index('\nRESULT,',start)
    s=s[:start]+"CONTROLS=['lightning-hit','lightning-immune-extra1','lightning-conductive','lightning-invulnerable445','lightning-no-AI-tables','deckem-area-collision','deckem-area-tick-abort','source-guided35']"+s[end:]
    write('crosscheck_shot_target_combinations.py',s)
    s=read('prepare_shot_combinations_primary.py').replace('import emulate_shot_combinations as native','import emulate_shot_target_combinations as native')
    s=s.replace('phase168','phase171').replace('shot-combinations','target')
    s=s.replace("INHERITED=[","INHERITED=['phase170-jack-primary.json','phase169-area-combinations-primary.json','phase168-shot-combinations-primary.json',",1)
    write('prepare_shot_target_primary.py',s)
    s=read('validate_shot_combinations.py').replace('import emulate_shot_combinations as native','import emulate_shot_target_combinations as native')
    s=s.replace('phase168','phase171').replace('shot-combinations','target')
    start=s.index('    patches=');end=s.index('\n    fields=',start)
    s=s[:start]+'''    patches=[dict(Data1=80,Data2=2,ShotProcessFlags=4,ProcessType=9,DamageType=7),
             dict(Data1=80,Data2=2,ShotProcessFlags=4,ProcessType=28,DamageType=7),
             dict(Data1=80,Data2=2,ShotProcessFlags=4,ProcessType=29,DamageType=7),
             dict(Data1=80,Data2=2,ShotProcessFlags=4,ProcessType=29,DamageType=1)]'''+s[end:]
    write('validate_shot_target_storage.py',s)
    print('Three helpers generated; frozen phase168 helper hashes preserved.')

if __name__=='__main__':main()
