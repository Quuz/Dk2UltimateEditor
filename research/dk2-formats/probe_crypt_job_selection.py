"""Phase188 discovery: raw preference -> original selector -> Job28 -> Crypt.

No initialCryptstate is supplied; declared wrapper invokes original job/AI
entry and records its return. Only research memory copies are edited.
"""
from pathlib import Path
import json,struct
import probe_crypt_movement as old
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p,EXE,sha,u=old.p,old.EXE,old.sha,old.u
RESULT,ENTRY=0x28000000,0x28001000

def cases():
    return [dict(name='direct-job28',method='job',job=28),
        dict(name='preference-job28',method='preference',raw_job=28),
        dict(name='preference-job0',method='preference',raw_job=0,continue_ai=False),
        dict(name='preference-zero-weight',method='preference',raw_job=28,weight=0),
        dict(name='full-AI-preference28',method='AI',raw_job=28,mood_mode=1),
        dict(name='owner-flag4-blocks',method='job',job=28,owner_flag4=True,continue_ai=False),
        dict(name='preference-hate28',method='preference',raw_job=28,raw_hate=28),
        dict(name='preference-high-job11c',method='preference',raw_job=0x11C)]

def build(c):
    follow=c.get('continue_ai',True)
    cfg=dict(species=5,owner=7,entry_state=274,register=False,activate=False,tick=follow,exit=follow,
        native_nav=True,room_area=True,null_renderer_cache=True,HP=900,
        registration='ai' if follow else None,register_dispatch=True,movement_steps=2 if follow else 0)
    args,ch,offsets=old.build(cfg)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    # The ordinary selector first considers its real lair. Warlock's loaded
    # definition771=27; World+188 is the original Object-definition lookup.
    # Add actual Objects27/21 (Warlock/Firefly lairs) to the sorted catalogue, using native
    # constructors/converters. A null catalogue used to cause a read at114;
    # those exploratory scenes are not accepted evidence.
    obj=old.old.prior
    source_objects={r[0x35A]:r for s in obj.fmt.parse(obj.SOURCE.read_bytes()) if s.signature==240 for r in s.records}
    ids=[9,11,21,27,142,143,144];assert all(len(source_objects[i])==894 for i in ids)
    mem[obj.RAWS]=bytearray(b''.join(source_objects[i] for i in ids))+bytearray([165])*64
    mem[obj.DEFS]=bytearray(285*len(ids))+bytearray([165])*64
    ch[obj.RAWS]=bytes(mem[obj.RAWS]);ch[obj.DEFS]=bytes(mem[obj.DEFS])
    for off in [0x15F8,0x15FC]:p.f.put(mem[p.f.WORLD],off,len(ids))
    ch[p.f.WORLD]=bytes(mem[p.f.WORLD])
    calls=args['before_calls'];where=next(i for i,x in enumerate(calls) if x['address']=='0x56d4f0')
    sample=next(x for x in calls if x['address']=='0x56d620');mid=int(sample['registers']['ECX'],0)
    calls[:]=[x for x in calls if x['address'] not in ['0x56d4f0','0x56d620','0x49dd60']]
    rebuilt=[]
    for i in range(len(ids)):
        rebuilt += [dict(address='0x56d4f0',registers=dict(ECX=hex(mid)),stack_args=[]),
            dict(address='0x56d620',registers=dict(ECX=hex(mid)),stack_args=[obj.RAWS+894*i]),
            dict(address='0x49dd60',registers=dict(ECX=hex(obj.DEFS+285*i)),stack_args=[mid,0])]
    calls[where:where]=rebuilt
    # Explicit already-initialized null-job singleton. Its native getter
    # otherwise registers a CRT shutdown callback, outside this scene's
    # startup/OS context. No game function or vtable is replaced.
    mem[0x6E4164]=bytearray(b'\x01');mem[0x6E4170]=bytearray(6)
    ch[0x6E4164]=bytes(mem[0x6E4164]);ch[0x6E4170]=bytes(mem[0x6E4170])
    # Nonzero initial clock lets the actual selector store its last-choice
    # timestamp; zero is also its "never selected" sentinel.
    session=u(mem[p.f.WORLD],4);p.f.put(mem[session],4,10);ch[session]=bytes(mem[session])
    raw=mem[p.target.RAW]
    if 'raw_job' in c:
        p.f.put(raw,0xE38,c['raw_job']);raw[0xE40]=c.get('weight',100)
        p.f.put(raw,0xE44,0)
        ch[p.target.RAW]=bytes(raw)
    if 'raw_hate' in c:p.f.put(raw,0xE74,c['raw_hate']);ch[p.target.RAW]=bytes(raw)
    if 'mood_mode' in c:p.f.put(mem[old.ACTOR],0x24E+0x3C,c['mood_mode']);ch[old.ACTOR]=bytes(mem[old.ACTOR])
    if c.get('owner_flag4'):
        owner=u(mem[old.ACTOR],0x2A);p.f.put(mem[owner],0x76,u(mem[owner],0x76)|4);ch[owner]=bytes(mem[owner])
    entry={'job':0x478800,'preference':0x4782C0,'AI':0x476FC0}[c['method']]
    code=bytearray()
    if c['method']=='job':code+=b'\x68'+struct.pack('<I',c['job'])
    code+=b'\xb9'+struct.pack('<I',old.CONTEXT)+b'\xb8'+struct.pack('<I',entry)+bytes.fromhex('ffd0a3')+struct.pack('<I',RESULT)+b'\xc3'
    mem[ENTRY]=code;mem[RESULT]=bytearray([165])*68;ch[RESULT]=bytes(mem[RESULT])
    calls=args['before_calls'];indices=[i for i,x in enumerate(calls) if x['address']=='0x476d30' and x['stack_args']==[274,0]]
    assert len(indices)==1
    calls[indices[0]]=dict(address=hex(ENTRY),registers=dict(ECX='0'),stack_args=[])
    args['allowed_external_IO_code'].append([ENTRY,len(code)]);args['observe_writes'].append([RESULT,68])
    adapter=old.old.prior.readable
    mem[adapter.ADDRESS]=bytearray(adapter.adapter(adapter.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=adapter.ADDRESS])))
    args['allowed_external_IO_code']=[x for x in args['allowed_external_IO_code'] if x[0]!=adapter.ADDRESS]+[[adapter.ADDRESS,len(mem[adapter.ADDRESS])]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    return args,ch,offsets

def execute(c):
    args,ch,offsets=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha(Path(old.__file__).read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase188-crypt-job-{c["name"]}-{tag}.json'
    if file.exists():s=json.loads(file.read_text(encoding='utf-8'));assert s['input']==args
    else:
        r=old.old.prior.old.base.cpu.run(EXE,args,list(ch.items()),p.f.OBS,old.ACTOR,len(ch[old.ACTOR]))
        s=dict(case=c,input=args,result=r);file.write_text(json.dumps(s,indent=2),encoding='utf-8')
    return args,ch,s,file,offsets

def describe(c):
    a,ch,s,f,ofs=execute(c);r=s['result'];post=post_context(ch,r);rows=[]
    for i in range(6):
        b=post[old.LOG][i*old.STRIDE:(i+1)*old.STRIDE];at=lambda k:b[ofs[k][0]:ofs[k][0]+ofs[k][1]]
        actor,room,owner=at('actor'),at('room'),at('owner')
        rows.append(dict(index=i,state=u(actor,0x25E),action=u(actor,0x3A9),HP=u(actor,0x40),selected_job=u(actor,0x24E+0x64),
            mood_mode=u(actor,0x24E+0x3C),roomtag=u(actor,0x3B9,2),registered=u(room,0x53,2),counters=list(owner[0x108A:0x109C])))
    definition=post[u(post[old.ACTOR],0x370)]
    out=dict(case=c,success=r['success'],error=r['emulation_error'],failures=r['protected_write_or_execution_failures'],entry_return=u(post[RESULT],0),
        source_preference=list(definition[0x63C:0x648]),preference_count=u(definition,0x78C,1),snapshots=rows,
        calls={hex(i):r['visited_instructions'].get(hex(i),0) for i in [0x4782C0,0x478800,0x483AC0,0x483790,0x483870,0x494480,0x46F390]},
        tail=r['trace_tail'][-20:],file=str(f.relative_to(ROOT)))
    print(json.dumps(out),flush=True);return out

def main():
    rows=[describe(c) for c in cases()]
    (ROOT/'phase188-crypt-job-discovery.json').write_text(json.dumps(dict(replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
