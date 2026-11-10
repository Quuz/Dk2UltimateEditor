"""Phase175 accepted unchanged Freeze/Poison sources and Crystal child chain.

Whole original methods on prepared class contexts. Host-selected call order,
not complete World scheduling or real combat DPS. All source bytes preserved.
"""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_shot_source_status as probe
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p=probe.p
EXE=probe.EXE
sha=probe.sha
u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')

def cases():
    out=[dict(name='wave-first'),dict(name='wave-expiry',parent_ticks=8),
        dict(name='wave-child-first',child_ticks=[[0,1]]),dict(name='wave-child-three',child_ticks=[[0,3]]),
        dict(name='wall-inherited',parent_ticks=8,child_ticks=[[1,1]]),
        dict(name='wall-source',parent=163),dict(name='wall-source-child',parent=163,child_ticks=[[0,1]]),
        dict(name='wave-invulnerable',invulnerable=1,child_ticks=[[0,1]]),
        dict(name='wave-trap80',trap_data1=80,child_ticks=[[0,1]]),
        dict(name='wave-trap0',trap_data1=0,child_ticks=[[0,1]])]
    directory=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor'
    for ID in [85,88,95]:out.append(dict(name=f'poison{ID}',source_file=str(directory/'AutoSaveGlobals.kld'),IDs=[ID],parent=ID))
    out.append(dict(name='poison85-alternate',source_file=str(directory/'maps/kop3Globals.kld'),IDs=[85],parent=85))
    out.append(dict(name='poison85-invulnerable',source_file=str(directory/'AutoSaveGlobals.kld'),IDs=[85],parent=85,invulnerable=1))
    return out

def execute(c):
    args,checks,node,actors=probe.build(c)
    deps=''.join(sha((ROOT/n).read_bytes()) for n in ['probe_shot_source_status.py','native_shot_pair_trace.py'])
    tag=sha((json.dumps(args,sort_keys=True)+deps).encode())[:12]
    file=ROOT/'ghidra'/f'phase175-source-status-{c["name"]}-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args and saved['case']==c
    else:
        r=probe.old.target.cpu.run(EXE,args,list(checks.items()),p.f.OBS,node,371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    return args,checks,saved,file

def reference(c,checks):
    parent=c.get('parent',162);damage=u(checks[probe.old.trap.TRAW],0x1E0);radius=u(checks[probe.old.trap.TRAW],0x1E4)
    hp=[c.get('HP',100000)]*3;freeze=[0]*3;poison=[0]*3;severity=[0]*3;requests=[]
    actors=[probe.old.trap.p.base.cf.ACTOR]+probe.old.trap.p.area.TARGETS[:2]
    def request(i,caller,scale=128):
        requests.append(dict(target=actors[i],delta=-damage,before=hp[i],caller=caller))
        hp[i]-=damage*scale//128
    if parent in [162,163]:freeze[1]=(damage*20)&65535
    else:poison[1]=damage&65535;severity[1]=radius&65535
    if damage>0 and not c.get('invulnerable'):request(1,'0x4ac58e',160 if freeze[1] else 128)
    child_ticks=dict(c.get('child_ticks',[]))
    if parent in [162,163]:
        for _ in range(child_ticks.get(0,0)):
            if not c.get('invulnerable'):request(1,'0x4ac666',160 if freeze[1] else 128)
            request(2,'0x4ac666')
    n=c.get('parent_ticks',1)
    parent_hp=(8-n if parent==162 else 20-n if parent==163 else 0xFFFFFFFF if parent==95 else 0)
    shots=[dict(tag=202,address=probe.old.trap.FIRED_NODE,ID=parent,HP=parent_hp,tick=n,Data1=damage,Data2=radius,target=2)]
    if parent in [162,163]:shots.append(dict(tag=203,address=probe.ARENAS+0x8C,ID=164,HP=3-child_ticks.get(0,0),tick=child_ticks.get(0,0),Data1=damage,Data2=radius,target=2))
    if parent==162 and n==8:shots.append(dict(tag=204,address=probe.ARENAS+4096+0x8C,ID=163,HP=20-child_ticks.get(1,0),tick=child_ticks.get(1,0),Data1=damage,Data2=radius,target=2))
    return dict(HP=hp,freeze=freeze,poison=poison,severity=severity,shots=shots),requests

def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);actors=[probe.old.trap.p.base.cf.ACTOR]+probe.old.trap.p.area.TARGETS[:2]
    out=probe.inspect(post,actors);expected,requests=reference(c,checks)
    actual_req=[dict(target=x['target'],delta=x['delta'],before=x['HP_before'],caller=x['caller_return']) for x in r['hp_requests']]
    assert out==expected and actual_req==requests,(c['name'],out,expected,actual_req,requests)
    file,ids,raws=probe.source(c);copies=[]
    assert post[probe.RAWS]==checks[probe.RAWS] and post[probe.DEFS][len(ids)*118:]==checks[probe.DEFS][len(ids)*118:]
    for i,ID in enumerate(ids):
        raw=raws[ID];d=post[probe.DEFS][118*i:118*(i+1)]
        assert checks[probe.RAWS][239*i:239*(i+1)]==raw
        assert u(d,0x37)==u(raw,0x98) and u(d,0x3B)==u(raw,0x9C) and d[0x58]==ID and d[0x6C]==raw[0xC7] and d[0x6B]==raw[0xC5]
        mid=bytearray(b'\1\0\0\0'*34+raw);mid[0xA7]=mid[0x170]=0;assert u(d,0x33)==sum(mid)
    for shot in out['shots']:
        address=shot['address'];tag=shot['tag'];assert post[0x6ED540][tag]==1 and u(post[0x6EE540],tag*4)==address
        b=next(b[address-a:address-a+371] for a,b in post.items() if a<=address<address+371<=a+len(b))
        assert u(b,0x28,2)==7 and u(b,0x145,2)==0
        if tag==202:continue
        pcs=[0x4AB61B,0x4AB621] if shot['ID']==163 else [0x4ABF7D,0x4ABF83]
        for pc,off,key in zip(pcs,[0x13D,0x141],['Data1','Data2']):
            found=[x for x in r['observed_writes'] if int(x['pc'],16)==pc and int(x['address'],16)==address+off]
            assert len(found)==1 and found[0]['value']==shot[key],(c['name'],shot,found)
            copies.append(dict(ShotID=shot['ID'],field=key,write=found[0]))
    assert u(post[0x6A4818],4)==202+len(out['shots'])
    assert u(post[p.f.WORLD],0x36)==u(checks[p.f.WORLD],0x36)+len(out['shots'])
    for a in [p.shot.RAW,p.target.RAW,probe.old.trap.JRAW,probe.old.trap.KRAW,probe.old.trap.TRAW,0x66D524,0x66D754,0x66D86C,0x66E3EC,0x670574]:
        assert post[a]==checks[a]
    assert post[probe.MID][375:]==checks[probe.MID][375:]
    for i in range(16):
        address=probe.ARENAS+4096*i;assert post[address][512:]==checks[address][512:]
    assert u(post[probe.old.readable.COUNTER],4)==0
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    # Callback zero at collision must still be followed by independent hit.
    events=r['process_events'];collision=[e for e in events if e['event_mask']==4]
    assert collision and collision[0]['return_EAX']==0
    parent_process=11 if c.get('parent',162) in [162,163] else 38
    callback=0x4AE1D0 if parent_process==11 else 0x4B0270
    assert r['visited_instructions'].get(hex(callback),0)==1
    return post,dict(out,requests=actual_req,parameter_copy_writes=copies,events=events,full_regions=len(post),full_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def main():
    rows=[]
    for c in cases():
        args,checks,saved,file=execute(c);_,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out))
        print(c['name'],out['HP'],out['freeze'],out['poison'],flush=True)
    (ROOT/'phase175-source-status-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')

if __name__=='__main__':main()
