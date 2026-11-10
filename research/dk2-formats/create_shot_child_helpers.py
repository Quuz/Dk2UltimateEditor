"""Generate phase174 helpers from frozen phase170/172 sources."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def main():
    refs={r['file']:r['sha256'] for schema in ['shot-instant-experience-semantics.json','jack-in-box-semantics.json'] for r in json.loads((ROOT/schema).read_text(encoding='utf-8'))['helpers']}
    def read(n):assert sha((ROOT/n).read_bytes())==refs[n];return (ROOT/n).read_text(encoding='utf-8')
    def write(n,s):
        file=ROOT/n
        if file.exists():assert file.read_text(encoding='utf-8')==s,n
        else:file.write_text(s,encoding='utf-8')
    s=read('crosscheck_shot_instant_experience.py').replace('import emulate_shot_instant_experience as native','import emulate_shot_child_chain as native')
    s=s.replace('phase172','phase174').replace('instant-xp','child-chain')
    start=s.index('CONTROLS=');end=s.index('\nRESULT,',start)
    s=s[:start]+"CONTROLS=['parent-first','wait-expired','full-selected-chain','trap-overrides-through-chain','invulnerable-chain']"+s[end:]
    write('crosscheck_shot_child_chain.py',s)
    s=read('prepare_shot_instant_primary.py').replace('import emulate_shot_instant_experience as native','import emulate_shot_child_chain as native')
    s=s.replace('phase172','phase174').replace('instant-xp','child-chain')
    s=s.replace("INHERITED=[","INHERITED=['phase172-instant-xp-primary.json',",1)
    write('prepare_shot_child_primary.py',s)
    s=read('validate_jack_storage.py').replace('import emulate_jack_in_box as native','import emulate_shot_child_chain as native')
    s=s.replace('phase170','phase174').replace('jack-storage','child-chain-storage')
    start=s.index('    specs=');end=s.index('\n    out=',start)
    s=s[:start]+'''    specs=[('Shots',0x104,load('area-damage-semantics.json')['corpus']['files'],
        [dict(HitThingShotId=153,DeathShotId=158,Data1=50,Data2=3),
         dict(HitThingShotId=0,DeathShotId=49,Data1=0,Data2=2),
         dict(HitThingShotId=0,DeathShotId=0,Data1=100,Data2=1)])]'''+s[end:]
    s=s.replace("records==dict(Shots=5551,Traps=1293) and total==328","records==dict(Shots=5551) and total==246")
    write('validate_shot_child_storage.py',s)
    queries=[dict(id=f'phase174-order-{a:06x}-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0) for a in [0x4ABD60,0x4AB180,0x50E2D0,0x50E1E0]]
    write('phase174-order-queries.json',json.dumps(queries,indent=2))
    print('Childchain independent CPU/primary/storage/order helpers generated.')

if __name__=='__main__':main()
