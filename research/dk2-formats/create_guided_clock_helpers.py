"""Generate phase173 cross/primary helpers from frozen verified phase171."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def main():
    refs={r['file']:r['sha256'] for r in json.loads((ROOT/'shot-target-combination-semantics.json').read_text(encoding='utf-8'))['helpers']}
    def read(n):assert sha((ROOT/n).read_bytes())==refs[n];return (ROOT/n).read_text(encoding='utf-8')
    def write(n,s):
        file=ROOT/n
        if file.exists():assert file.read_text(encoding='utf-8')==s,n
        else:file.write_text(s,encoding='utf-8')
    s=read('crosscheck_shot_target_combinations.py').replace('import emulate_shot_target_combinations as native','import emulate_guided_clock as native')
    s=s.replace('phase171','phase173').replace('target-','guided-clock-')
    start=s.index('CONTROLS=');end=s.index('\nRESULT,',start)
    s=s[:start]+"CONTROLS=['source-clock104','velocity-clock-100','velocity-clock-104','velocity-clock-4294967288','mask-miss']"+s[end:]
    write('crosscheck_guided_clock.py',s)
    s=read('prepare_shot_target_primary.py').replace('import emulate_shot_target_combinations as native','import emulate_guided_clock as native')
    s=s.replace('phase171','phase173').replace('target-','guided-clock-')
    s=s.replace("INHERITED=[","INHERITED=['phase172-instant-xp-primary.json','phase171-target-primary.json','phase131-area-primary.json',",1)
    s=s.replace('BLOCKS=[','BLOCKS=[(0x6C1488,0x2800),',1)
    write('prepare_guided_clock_primary.py',s)
    queries=[dict(id=f'phase173-order-{a:06x}-assembly',tool='disassemble_function',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0) for a in [0x4AF3A0,0x517490]]
    write('phase173-order-queries.json',json.dumps(queries,indent=2))
    print('Guided independent CPU/primary/order helpers generated.')

if __name__=='__main__':main()
