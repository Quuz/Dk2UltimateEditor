"""Phase172 discovery: pretargeted InstantHit and correctly scaled XP pairs.

Unpublished discovery. Frozen171 and170 sources/evidence remain unchanged.
Targeted original Trap firing gives real Shot147 before its complete tick.
Diagnostic Trap template ID17 avoids JackID16-specific Data2 replacement.
"""
from pathlib import Path
import json,struct,sys
import probe_jack_in_box as trap
import probe_shot_target_combinations as target
import fixture_read_probe as readable
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
EXE=trap.EXE
sha=trap.sha
LEVELS=0x1E000000

def source(process,mode):
    rows=json.loads((ROOT/'phase168-shot-combinations-storage.json').read_text(encoding='utf-8'))['observed_pairs']
    v=next(r for r in rows if (r['ProcessType'],r['DamageType'])==(process,mode))['variants'][0]
    return dict(file=v['first_source'],record_index=v['first_record_index'])

def build(c):
    if c.get('process',34)==34:
        args,_=trap.build(dict(trap=True,trap_active=True,trap_fire=True,source_shot=True,target_tag=c.get('target_tag',2),HP=[1000,1000]))
        mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
        q=source(34,c.get('mode',2));raw=next(s.records[q['record_index']] for s in target.fmt.parse(Path(q['file']).read_bytes()) if s.signature==260)
        mem[trap.p.base.RAW]=bytearray(raw)
        for name,off in [('data1',0x98),('data2',0x9C),('mask',0xA0),('flags',0xA8)]:
            if name in c:target.p.f.put(mem[trap.p.base.RAW],off,c[name])
        if 'mode_override' in c:mem[trap.p.base.RAW][0xC5]=c['mode_override']
        mem[trap.TRAW][0x20C]=17;mem[trap.TRAW][0x20D]=raw[0xBE]
        for off,s in [(0x1E0,0x98),(0x1E4,0x9C)]:target.p.f.put(mem[trap.TRAW],off,int.from_bytes(mem[trap.p.base.RAW][s:s+4],'little'))
        for call in args['before_calls']:
            if call['address']=='0x4b7c10':call['stack_args'][0]=17
        if 'invulnerable' in c:target.p.f.put(mem[trap.p.area.TARGETS[0]],0x445,c['invulnerable'],2)
        if c.get('target_dx'):
            for actor in trap.p.area.TARGETS[:2]:
                target.p.f.put(mem[actor],0x1A,int.from_bytes(mem[actor][0x1A:0x1E],'little')+c['target_dx'])
            # Rebuild the prepared valid cell lists after changing scene XY.
            for off in range(0,len(mem[trap.p.f.MAP]),16):target.p.f.put(mem[trap.p.f.MAP],off,0,2)
            tails={}
            for actor in [trap.p.base.cf.ACTOR]+trap.p.area.TARGETS[:2]:
                tag=int.from_bytes(mem[actor][4:6],'little')
                for off in [0xA,0xC]:target.p.f.put(mem[actor],off,0,2)
                x,y=struct.unpack_from('<2i',mem[actor],0x1A);cell=((y>>12)*32+(x>>12))*16
                assert 0<=cell<len(mem[trap.p.f.MAP])
                if cell in tails:
                    previous,pt=tails[cell];target.p.f.put(mem[previous],0xC,tag,2);target.p.f.put(mem[actor],0xA,pt,2)
                else:target.p.f.put(mem[trap.p.f.MAP],cell,tag,2)
                tails[cell]=(actor,tag)
        if c.get('no_fire'):
            # Use original Shotfactory directly, without target setter.
            args['before_calls']=[call for call in args['before_calls'] if call['address']!='0x4a7fd0']
            args['before_calls'].append(dict(address='0x4b7cd0',registers=dict(ECX=hex(trap.p.f.WORLD+8)),stack_args=[raw[0xBE],7,trap.p.f.ARGS,trap.p.base.ANGLE,trap.p.base.OUTPUT,0]))
        node=trap.shot_node(dict(trap_fire=True))
        for _ in range(c.get('pre_ticks',0)):args['before_calls'].append(dict(address='0x4ab180',registers=dict(ECX=hex(node)),stack_args=[]))
        captures=[trap.p.base.cf.ACTOR]+trap.p.area.TARGETS[:2]
    else:
        q=source(7,7)
        args,_=target.build(dict(process=7,mode=7,source_record=q,root=c.get('root','collision'),invulnerable=c.get('invulnerable',0)))
        mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
        for name,off in [('data1',0x98),('data2',0x9C),('mask',0xA0),('flags',0xA8)]:
            if name in c:
                target.p.f.put(mem[target.p.shot.RAW],off,c[name])
                if off in [0x98,0x9C]:target.p.f.put(mem[target.p.RAWSPELL],off+0x30,c[name])
        if 'mode_override' in c:mem[target.p.shot.RAW][0xC5]=c['mode_override']
        who=target.p.target.attack.TARGET
        target.p.f.put(mem[who],0x403,c.get('level',1),1);target.p.f.put(mem[who],0x3FF,c.get('XP',0))
        target.p.f.put(mem[who],0x40,c.get('HP',1000))
        gv=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd'
        raws=[r for s in target.fmt.parse(gv.read_bytes()) if s.signature==230 for r in s.records if int.from_bytes(r[:4],'little')==65]
        mem[LEVELS]=bytearray(b''.join(bytes(16)+r for r in raws))
        loads=[dict(address='0x51dc40',registers=dict(ECX='0'),stack_args=[LEVELS+32*i]) for i in range(len(raws))]
        # Raw realGlobalVariables65 table is loaded before all scalar consumers.
        args['before_calls']=args['before_calls'][:3]+loads+args['before_calls'][3:]
        node=target.p.shot.NODE;captures=[who]
    if readable.ADDRESS in mem:
        mem[readable.COUNTER]=bytearray(8)
        mem[readable.ADDRESS]=bytearray(readable.adapter(readable.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=readable.ADDRESS])))
        args['allowed_external_IO_code']=[r for r in args['allowed_external_IO_code'] if r[0]!=readable.ADDRESS]+[[readable.ADDRESS,len(mem[readable.ADDRESS])]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    excluded={0x7FFEE000,trap.p.f.GFX_CB,trap.p.f.SOUND_CB,target.p.target.attack.EVENT,target.p.target.LOAD_GFX,target.p.target.LOAD_SOUND,trap.p.SOUND_UPDATE,trap.LOOKUP,readable.ADDRESS,0x28CFA4}
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    for address,size in [(0x66D524,0x130),(0x66D754,0x100),(0x66D86C,0x100),(0x66E3EC,0x3A0),(0x670574,0x200)]:
        checks[address]=target.p.pe_read(EXE.read_bytes(),address,size)
    args['observe_reads'] += [[0,4096]]
    args['observe_writes'] += [[0,4096]]
    return args,checks,node,captures

def execute(c):
    args,checks,node,captures=build(c)
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase172-instant-xp-discovery-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=target.cpu.run(EXE,args,list(checks.items()),target.p.f.OBS,node,371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    post=post_context(checks,saved['result']);return args,checks,saved,file,post,node,captures

def describe(c):
    args,checks,saved,file,post,node,actors=execute(c);r=saved['result'];u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    shot=next(b[node-a:node-a+371] for a,b in post.items() if a<=node<node+371<=a+len(b))
    print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],requests=r['hp_requests'],events=r['process_events'],
        actors=[dict(address=hex(a),HP=u(post[a],0x40),level=u(post[a],0x403,1),XP=u(post[a],0x3FF)) for a in actors],
        shot=dict(HP=u(shot,0x40),Data1=u(shot,0x13D),Data2=u(shot,0x141),target=u(shot,0x147,2),forced=u(shot,0x14D)),
        before=r['before_calls'][-5:],tail=r['trace_tail'][-8:],
        low=[x for x in r['observed_reads']+r['observed_writes'] if 0<int(x['address'],16)<4096][:8])),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else dict(process=34,mode=2))
