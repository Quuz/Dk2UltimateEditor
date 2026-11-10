"""Discovery for the remaining target ProcessType/DamageType compositions.

Frozen phase168 raw loaders/CreatureSpell wrapper select the target through
the real collision routine. Original AI initializers provide live state tables.
No original gameplay instructions, vtables or input files are changed.
"""
from pathlib import Path
import hashlib,json,struct,sys
import emulate_shot_combinations as old
import native_shot_pair_trace as cpu
import dk2_formats as fmt
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
p=old.p
EXE=old.EXE
sha=old.sha
EXTRA=0x1D000000
TERRAIN=0x1D010000

def build(c):
    seed=dict(process=c.get('process',9),mode=c.get('mode',7),data1=c.get('data1',80),data2=c.get('data2',2),
        mask=c.get('mask',4),ticks=0,armour=c.get('armour',0),invulnerable=c.get('invulnerable',0),initial_HP=c.get('HP',1000))
    args,_=old.build(seed)
    mem={int(x['address'],16):bytearray.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
    if c.get('source_record'):
        q=c['source_record'];raw=next(s.records[q['record_index']] for s in fmt.parse(Path(q['file']).read_bytes()) if s.signature==0x104)
        assert (raw[0xC7],raw[0xC5])==(seed['process'],seed['mode'])
        mem[p.shot.RAW]=bytearray(raw)
        for off in [0x98,0x9C]:p.f.put(mem[p.RAWSPELL],off+0x30,int.from_bytes(raw[off:off+4],'little'))
        mem[p.RAWSPELL][0xE7]=raw[0xBE]
    for name,offset in [('shot_data1',0x98),('shot_data2',0x9C),('shot_mask',0xA0)]:
        if name in c:
            p.f.put(mem[p.shot.RAW],offset,c[name])
            if offset in [0x98,0x9C]:p.f.put(mem[p.RAWSPELL],offset+0x30,c[name])
    if 'shot_mode' in c:mem[p.shot.RAW][0xC5]=c['shot_mode']
    if 'shot_flags' in c:p.f.put(mem[p.shot.RAW],0xA8,c['shot_flags'])
    for name,offset in [('extra_flags',0x1545),('creature_flags',0xEFC)]:
        if name in c:p.f.put(mem[p.target.RAW],offset,c[name])
    if 'extra_flags' in c:
        # Long rawCreature5449 is required for the genuine extension loader.
        assert len(mem[p.target.RAW])>=0x1549
    target=p.target.attack.TARGET
    p.f.put(mem[p.f.WORLD],0x9E2,p.f.WORLD)
    p.f.put(mem[target],0x3E6,c.get('instance_flags',int.from_bytes(mem[target][0x3E6:0x3EA],'little')))
    for actor in [p.cf.ACTOR,target]:
        p.f.put(mem[actor],0x256,actor);p.f.put(mem[actor],0x25A,p.f.WORLD)
        definition=int.from_bytes(mem[actor][0x370:0x374],'little')
        # Snapshot's current ArtResource must reference a real loaded record.
        # The frozen attack scene never entered the full animation setter;
        # retaining its null6C would produce an invalid low-page read at4967BA.
        p.f.put(mem[actor],0x6C,definition+27)
    p.f.put(mem[target],0x4E3,c.get('reaction_timer',0),2)
    # A valid terrain definition selected by the real target map cell.
    mem[0x6DA4A8]=bytearray(256*4)
    mem[TERRAIN]=bytearray(0x228);mem[TERRAIN][0x17B]=0x80 if c.get('conductive') else 0
    for off in range(0,len(mem[0x6DA4A8]),4):p.f.put(mem[0x6DA4A8],off,TERRAIN)
    if c.get('extra_target',False):
        mem[EXTRA]=bytearray(mem[target]);p.f.put(mem[EXTRA],4,103,2)
        p.f.put(mem[EXTRA],0x1A,int.from_bytes(mem[target][0x1A:0x1E],'little')+1024)
        p.f.put(mem[EXTRA],0x256,EXTRA)
        p.f.put(mem[target],0xC,103,2);p.f.put(mem[EXTRA],0xA,101,2);p.f.put(mem[EXTRA],0xC,100,2);p.f.put(mem[p.cf.ACTOR],0xA,103,2)
        mem[0x6ED540][103]=1;p.f.put(mem[0x6EE540],103*4,EXTRA)
    calls=args['before_calls']
    # Keep collision on demand, allowing a real whole Shot tick instead.
    assert calls[-1]['address']=='0x4abd60'
    if c.get('root','collision')=='tick' and not c.get('pre_collision'):calls=calls[:-1]
    if c.get('ai_tables',True):
        from spell_state_tables import BASE as SB,SIZE as SS
        from creature_category_tables import BASE as CB,SIZE as CS
        from portal_animation_tables import BASE as AB,initialized_animations
        base,image=cpu.pe_image(EXE.read_bytes())
        for address,size in [(SB,SS),(CB,CS),(AB,len(initialized_animations()))]:mem[address]=bytearray(image[address-base:address-base+size])
        calls=[dict(address=hex(fn),registers=dict(ECX='0'),stack_args=[]) for fn in [0x47BBE0,0x4798F0,0x4950C0]]+calls
    # Whole Shot tick uses these original graphics slots (not IO lookup).
    for slot in [0x64,0x80]:p.f.put(mem[p.f.GV],slot,int.from_bytes(p.pe_read(EXE.read_bytes(),0x670574+slot,4),'little'))
    args['before_calls']=calls
    args['address']='0x4ab180' if c.get('root','collision')=='tick' else '0x4b5c30'
    args['registers']=json.dumps(dict(ECX=hex(p.shot.NODE if c.get('root','collision')=='tick' else target)))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[0,4096],[TERRAIN,len(mem[TERRAIN])],[0x6DA4A8,1024]]
    args['observe_writes'] += [[0,4096]]+([[EXTRA,len(mem[EXTRA])]] if EXTRA in mem else [])
    args['max_steps']=2000000
    excluded=[0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.target.attack.EVENT,p.target.LOAD_GFX,p.target.LOAD_SOUND,0x28CFA4]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    checks[p.shot.SHOT_VTABLE]=p.pe_read(EXE.read_bytes(),p.shot.SHOT_VTABLE,0x100)
    return args,checks

def execute(c):
    args,checks=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase171-target-pair-discovery-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=cpu.run(EXE,args,list(checks.items()),p.f.OBS,p.shot.NODE,371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    post=post_context(checks,saved['result']);return args,checks,saved,file,post

def describe(c):
    args,checks,saved,file,post=execute(c);r=saved['result'];target=post[p.target.attack.TARGET]
    u=lambda o,n=4:int.from_bytes(target[o:o+n],'little')
    node=post[p.f.BLOCK][p.shot.NODE-p.f.BLOCK:p.shot.NODE-p.f.BLOCK+371]
    out=dict(success=r['success'],error=r['emulation_error'],requests=r['hp_requests'],HP=u(0x40),state=u(0x25E),reaction=u(0x4E3,2),
        process=r['process_events'],shotHP=int.from_bytes(node[0x40:0x44],'little'),shot_target=int.from_bytes(node[0x147:0x149],'little'),
        level=u(0x403,1),XP=u(0x3FF),before=r['before_calls'][-5:],tail=r['trace_tail'][-10:],
        low=[x for x in r['observed_reads'] if int(x['address'],16)<4096][:8])
    print(json.dumps(out),flush=True)

if __name__=='__main__':describe(json.loads(sys.argv[1]) if len(sys.argv)>1 else dict(process=9))
