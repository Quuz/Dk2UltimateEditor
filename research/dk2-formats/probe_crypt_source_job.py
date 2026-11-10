"""Phase188: unchanged FrontEnd Firefly record with real HappyJobs28/24."""
from pathlib import Path
import json,struct
import probe_crypt_job_selection as base
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=base.p,base.EXE,base.sha,base.u
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/maps/FrontEnd3DLevelGlobals.kld'

def build(method,seed=None):
    c=dict(name='source-Firefly-'+method,method='preference')
    args,ch,ofs=base.build(c)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    raw=next(r for s in base.old.old.prior.fmt.parse(SOURCE.read_bytes()) if s.signature==170 for r in s.records if r[0xF4B]==32)
    assert raw[:32].split(b'\0')[0]==b'[Unused] Firefly'
    mem[p.target.RAW]=bytearray(raw);ch[p.target.RAW]=raw
    mem[base.old.ACTOR][0x36F]=32;ch[base.old.ACTOR]=bytes(mem[base.old.ACTOR])
    target=0x4780E0 if method=='happy' else 0x476FC0
    old=bytes(mem[base.ENTRY]);needle=b'\xb8'+struct.pack('<I',0x4782C0)
    assert old.count(needle)==1
    mem[base.ENTRY]=bytearray(old.replace(needle,b'\xb8'+struct.pack('<I',target)))
    if method=='AI':
        # State1 first reaches89 before its normal HappyJobs selector. Give
        # that original AI chain one additional update to activate275/80.
        calls=args['before_calls'];at=next(i for i,x in enumerate(calls) if x['address']==hex(base.old.CONTROL+512))
        calls.insert(at,dict(address='0x476fc0',registers=dict(ECX=hex(base.old.CONTEXT)),stack_args=[]))
    mem[0x6E415C]=bytearray(b'\x01');mem[0x6E4134]=bytearray(6)
    ch[0x6E415C]=bytes(mem[0x6E415C]);ch[0x6E4134]=bytes(mem[0x6E4134])
    if seed is not None:
        p.f.put(mem[p.f.WORLD],0x15A0,seed);ch[p.f.WORLD]=bytes(mem[p.f.WORLD])
    adapter=base.old.old.prior.readable
    mem[adapter.ADDRESS]=bytearray(adapter.adapter(adapter.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=adapter.ADDRESS])))
    args['allowed_external_IO_code']=[x for x in args['allowed_external_IO_code'] if x[0]!=adapter.ADDRESS]+[[adapter.ADDRESS,len(mem[adapter.ADDRESS])]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    return args,ch,ofs

def execute(method,seed=None):
    a,ch,ofs=build(method,seed);tag=sha((json.dumps(a,sort_keys=True)+sha(Path(__file__).read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase188-source-Firefly-{method}-{tag}.json'
    if file.exists():s=json.loads(file.read_text(encoding='utf-8'));assert s['input']==a
    else:
        r=base.old.old.prior.old.base.cpu.run(EXE,a,list(ch.items()),p.f.OBS,base.old.ACTOR,len(ch[base.old.ACTOR]))
        s=dict(input=a,result=r);file.write_text(json.dumps(s,indent=2),encoding='utf-8')
    return a,ch,s,file,ofs

def describe(method,seed):
        a,ch,s,f,ofs=execute(method,seed);r=s['result'];post=post_context(ch,r);rows=[]
        if r['success']:
            for i in range(6):
                b=post[base.old.LOG][i*base.old.STRIDE:(i+1)*base.old.STRIDE];at=lambda k:b[ofs[k][0]:ofs[k][0]+ofs[k][1]]
                actor,room,owner=at('actor'),at('room'),at('owner')
                rows.append(dict(index=i,state=u(actor,0x25E),action=u(actor,0x3A9),HP=u(actor,0x40),job=u(actor,0x24E+0x64),
                    registered=u(room,0x53,2),counters=list(owner[0x108A:0x109C])))
        v=dict(method=method,seed=seed,success=r['success'],error=r['emulation_error'],failures=r['protected_write_or_execution_failures'],snapshots=rows,
            tail=r['trace_tail'][-15:],calls={hex(pc):r['visited_instructions'].get(hex(pc),0) for pc in [0x4773D0,0x4780E0,0x478800,0x483AC0]},file=str(f.relative_to(ROOT)))
        print(json.dumps(dict(method=method,seed=seed,success=r['success'],error=r['emulation_error'],
            state_job_action=[(x['state'],x['job'],x['action']) for x in rows],file=str(f.relative_to(ROOT)))),flush=True)
        return v

def main():
    out=[]
    for seed in [0,2,17,42]:
        v=describe('happy',seed);out.append(v)
        if v['success'] and v['snapshots'][0]['job']==28:
            out.append(describe('AI',seed));break
    (ROOT/'phase188-source-Firefly-discovery.json').write_text(json.dumps(dict(source=str(SOURCE),sha256=sha(SOURCE.read_bytes()),replays=out),indent=2),encoding='utf-8')

if __name__=='__main__':main()
