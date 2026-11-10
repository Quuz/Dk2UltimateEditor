"""Generate new phase170 helpers from SHA-verified frozen phase169 sources."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
def main():
    prior=json.loads((ROOT/'shot-area-combination-semantics.json').read_text(encoding='utf-8'))
    refs={r['file']:r['sha256'] for r in prior['helpers']}
    def read(name):
        assert sha((ROOT/name).read_bytes())==refs[name]
        return (ROOT/name).read_text(encoding='utf-8')
    def write(name,text):
        f=ROOT/name
        if f.exists():assert f.read_text(encoding='utf-8')==text,name
        else:f.write_text(text,encoding='utf-8')
    s=read('crosscheck_shot_area_combinations.py')
    s=s.replace('import emulate_shot_area_combinations as native','import emulate_jack_in_box as native')
    s=s.replace('phase169','phase170').replace('area-combination','jack')
    s=s.replace('phase170-jacks-crosschecks.json','phase170-jack-crosschecks.json')
    start=s.index('CONTROLS=');end=s.index('\nRESULT,',start)
    s=s[:start]+"CONTROLS=['hostile-start','hostile-tick','hostile-capacity448','hostile-invulnerable','fired-source64-tag2','fired-source64-tag3','fired-trap-overrides-defaults','wind-real-AI']"+s[end:]
    write('crosscheck_jack_in_box.py',s)
    s=read('prepare_shot_area_combinations_primary.py')
    s=s.replace('import emulate_shot_area_combinations as native','import emulate_jack_in_box as native')
    s=s.replace('native.p.p.area.pe_read','native.p.p.area.pe_read')
    s=s.replace('phase169','phase170').replace('area-combinations','jack')
    s=s.replace("INHERITED=[","INHERITED=['phase169-area-combinations-primary.json','phase161-trap-creation-primary.json','phase144-wander-target-path-primary.json',",1)
    write('prepare_jack_primary.py',s)
    print('Phase170 independent CPU and primary helpers generated; prior hashes verified.')
if __name__=='__main__':main()
