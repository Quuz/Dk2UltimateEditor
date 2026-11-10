"""Phase176 discovery of original Chainfire mask0x10C/event8 and children."""
from pathlib import Path
import json,sys
import probe_shot_source_status as base
from emulate_resistance import post_context
from emulate_element_colors import region
ROOT=Path(__file__).resolve().parent
EXE=base.EXE
sha=base.sha
p=base.p
DEFS,RAWS,MID,ARENAS=base.DEFS,base.RAWS,base.MID,base.ARENAS

def config(c):return dict(IDs=[14,36,50,75,155],parent=50)|c

def build(c):
    cfg=config(c);args,checks,node,actors=base.build(cfg)
    if c.get('clear_bits'):
        mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
        ids=sorted(cfg['IDs'])
        for ID in c.get('mask_ids',[36]):
            off=239*ids.index(ID)+0xA0
            mask=int.from_bytes(mem[RAWS][off:off+4],'little')&~c['clear_bits']
            p.f.put(mem[RAWS],off,mask)
            if cfg['parent']==ID:p.f.put(mem[p.shot.RAW],0xA0,mask)
        args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
        checks[RAWS]=bytes(mem[RAWS]);checks[p.shot.RAW]=bytes(mem[p.shot.RAW])
    return args,checks,node,actors

def execute(c):
    args,checks,node,actors=build(c)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_shot_chainfire.py','probe_shot_source_status.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase176-chainfire-discovery-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=base.old.target.cpu.run(EXE,args,list(checks.items()),p.f.OBS,node,371);saved=dict(case=c,input=args,result=r)
        file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file,post_context(checks,saved['result']),node,actors

def describe(c):
    args,ch,s,file,post,node,actors=execute(c);r=s['result']
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],outcome=base.inspect(post,actors),
        events=r['process_events'],requests=r['hp_requests'],before=r['before_calls'][-5:],tail=r['trace_tail'][-12:])),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else {})
