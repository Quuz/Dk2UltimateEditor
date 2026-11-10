"""Discovery: hostile Jack39 damage -> actual rawCreature46 loader/factory.

Prepared World/tag pool/Creature slot; all original gameplay instructions run.
Original scene/helpers remain frozen. New source records are retained verbatim.
"""
from pathlib import Path
import json,struct,sys
import probe_shot_area_combinations as old
import dk2_formats as fmt
import fixture_read_probe as readable
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=old.sha
EXE=old.EXE
p=old.p
JRAW,JMID,TABLE=0x1C000000,0x1C010000,0x1C020000
JDEF=TABLE+2199
KRAW,KMID,KDEF=0x1C030000,0x1C040000,0x1C050000
ACTOR,STATS,LOOKUP,LOOKUP_LOG=0x1C060000,0x1C070000,0x1C080000,0x1C081000
TRAW,TMID,TDEF,TBLOCK,TOUT=0x1C100000,0x1C110000,0x1C120000,0x1C130000,0x1C140000
TRAP=TBLOCK+208+12
FIRED_NODE=TBLOCK+128+12
TRIANGLES=0x1C150000
TRIANGLE_BYTES=4+29*2048

def shot_node(c):return FIRED_NODE if c.get('trap_fire') else p.base.NODE
def trap_node(c):return p.f.BLOCK+208+12 if c.get('trap_fire') else TRAP

def build(c):
    seed=dict(process=c.get('process',39),mode=c.get('mode',2),data1=c.get('data1',80),data2=c.get('data2',2),mask=c.get('mask',64),jack_target=True,
        owners=c.get('owners',[8,8]),pre_ticks=0)
    if c.get('source_shot'):
        file=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Shots.kwd'
        si=next(i for s in fmt.parse(file.read_bytes()) if s.signature==0x104 for i,r in enumerate(s.records) if r[0xBE]==64)
        seed['source_record']=dict(file=str(file),record_index=si)
    args,_=old.build(seed)
    mem={int(x['address'],16):bytearray.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
    if c.get('target_tag',2)!=2:
        tag=c['target_tag'];target=p.area.TARGETS[0]
        p.area.put(mem[0x6EE540],2*4,0);mem[0x6ED540][2]=0
        p.area.put(mem[0x6EE540],tag*4,target);mem[0x6ED540][tag]=1
        p.area.put(mem[target],4,tag,2)
        for actor in [p.base.cf.ACTOR]+p.area.TARGETS[:2]:
            for off in [0xA,0xC]:
                if int.from_bytes(mem[actor][off:off+2],'little')==2:p.area.put(mem[actor],off,tag,2)
        for off in range(0,len(mem[p.f.MAP]),16):
            if int.from_bytes(mem[p.f.MAP][off:off+2],'little')==2:p.area.put(mem[p.f.MAP],off,tag,2)
    if c.get('source_shot'):
        for name,offset in [('shot_data1',0x98),('shot_data2',0x9C),('shot_mask',0xA0)]:
            if name in c:p.area.put(mem[p.base.RAW],offset,c[name])
        if 'shot_mode' in c:mem[p.base.RAW][0xC5]=c['shot_mode']
    # Actor snapshots copied for targets need their native context owner
    # rebound before active state callbacks can run (476B40 sets these fields
    # for a newly created real Creature). Other actor bytes stay preserved.
    for actor in [p.base.cf.ACTOR]+p.area.TARGETS[:2]:
        p.area.put(mem[actor],0x256,actor);p.area.put(mem[actor],0x25A,p.f.WORLD)
    for i,actor in enumerate(p.area.TARGETS[:2]):
        p.area.put(mem[actor],0x40,c.get('HP',[1000,1000])[i])
        p.area.put(mem[actor],0x445,c.get('invulnerable',[0,0])[i],2)
    creature=next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Creatures.kwd').read_bytes()) if s.signature==170 for r in s.records if r[0xF4B]==46)
    keeper=next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/KeeperSpells.kwd').read_bytes()) if s.signature==150 for r in s.records if r[0xEE]==4)
    mem[JRAW]=bytearray(creature);mem[JMID]=bytearray([165])*6457
    mem[TABLE]=bytearray(mem[p.base.cf.DEFINITION][:2199])+bytearray([165])*2199+bytearray([165])*64
    mem[KRAW]=bytearray(keeper);mem[KMID]=bytearray([165])*526;mem[KDEF]=bytearray(122)
    mem[ACTOR]=bytearray(1343)+bytearray([165])*256;mem[ACTOR][0x403]=1
    mem[STATS]=bytearray(mem[p.base.cf.STATS])+bytearray(13)+bytearray([165])*64;mem[STATS][13]=46
    p.area.put(mem[p.f.PLAYER],0x4E,2);p.area.put(mem[p.f.PLAYER],0x52,STATS)
    for offset,value in [(0x15C4,TABLE),(0x15C8,2),(0x15CC,2),(0x1604,KDEF),(0x1608,1),(0x160C,1)]:p.area.put(mem[p.f.WORLD],offset,value)
    # Reserve unused tags through the original allocator. Existing prepared
    # Creature tags2/100/103 cannot collide with these new200/201 allocations.
    p.area.put(mem[0x6A4818],4,200)
    # One prepared unused Creature slot, as in the frozen phase90/164 scene.
    # Its constructor really acquires tag201 after Shot tag200; the prepared
    # free-list head refers to that real object before the callback consumes it.
    p.area.put(mem[p.f.WORLD],0x24,200 if c.get('trap_fire') else 201,2)
    if c.get('capacity') is not None:p.area.put(mem[p.f.WORLD],0x26,c['capacity'])
    mem[LOOKUP]=bytearray(bytes.fromhex('ff05')+struct.pack('<I',LOOKUP_LOG)+bytes.fromhex('33c0c20800'));mem[LOOKUP_LOG]=bytearray(4)
    p.area.put(mem[p.f.SV],0x1C,LOOKUP)
    mem[0x6FCDCF]=bytearray(4)
    calls=args['before_calls'][:4]+[
        dict(address='0x491dc0',registers=dict(ECX=hex(JDEF)),stack_args=[]),
        dict(address='0x568820',registers=dict(ECX=hex(JMID)),stack_args=[]),
        dict(address='0x568e30',registers=dict(ECX=hex(JMID)),stack_args=[JRAW]),
        dict(address='0x492320',registers=dict(ECX=hex(JDEF)),stack_args=[JMID,p.f.WORLD,0]),
        dict(address='0x56b990',registers=dict(ECX=hex(KMID)),stack_args=[]),
        dict(address='0x56bab0',registers=dict(ECX=hex(KMID)),stack_args=[KRAW]),
        dict(address='0x4f7ce0',registers=dict(ECX=hex(KDEF)),stack_args=[KMID,0])]+args['before_calls'][4:]+[
        dict(address='0x4b5e10',registers=dict(ECX=hex(ACTOR)),stack_args=[])]
    if c.get('trap'):
        source=next(r for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Traps.kwd').read_bytes()) if s.signature==130 for r in s.records if b'jack' in r[:32].lower())
        mem[TRAW]=bytearray(source);mem[TMID]=bytearray([165])*747;mem[TDEF]=bytearray([165])*209;mem[TOUT]=bytearray(16)
        for name,offset in [('trap_data1',0x1E0),('trap_data2',0x1E4)]:
            if name in c:p.area.put(mem[TRAW],offset,c[name])
        mem[TBLOCK]=bytearray([165])*768
        struct.pack_into('<III',mem[TBLOCK],0,512,p.f.BLOCK,p.f.FT)
        p.area.put(mem[p.f.BLOCK],8,TBLOCK);p.area.put(mem[p.f.FT],4,TBLOCK)
        for offset,value in [(0x15E4,TDEF),(0x15E8,1),(0x15EC,1)]:p.area.put(mem[p.f.WORLD],offset,value)
        for offset,value in [(0x10D6,100),(0x10E2,1000),(0x1121,1)]:p.area.put(mem[p.f.PLAYER],offset,value)
        assert len(mem[p.f.WORLD])>=0xA380
        mem[0x6F2548]=bytearray(struct.pack('<I',8))
        calls += [dict(address='0x4aa330',registers=dict(ECX=hex(TDEF)),stack_args=[]),
            dict(address='0x570b60',registers=dict(ECX=hex(TMID)),stack_args=[]),
            dict(address='0x570d10',registers=dict(ECX=hex(TMID)),stack_args=[TRAW]),
            dict(address='0x4aa440',registers=dict(ECX=hex(TDEF)),stack_args=[TMID,0]),
            dict(address='0x4b7c10',registers=dict(ECX=hex(p.f.WORLD+8)),stack_args=[source[0x20C],7,p.f.ARGS,TOUT])]
        if c.get('trap_active'):calls.append(dict(address='0x4a96d0',registers=dict(ECX=hex(trap_node(c))),stack_args=[]))
        if c.get('trap_fire'):
            p.area.put(mem[p.f.WORLD],0x9E2,p.f.WORLD)
            calls=[x for x in calls if x['address']!='0x4b7cd0']
            calls.append(dict(address='0x4a7fd0',registers=dict(ECX=hex(trap_node(c))),stack_args=[c.get('fire_target',c.get('target_tag',2))]))
            image_base,image=old.cpu.pe_image(EXE.read_bytes());navbase,navsize=0x6E5AC0,0x7520
            nav=bytearray(image[navbase-image_base:navbase-image_base+navsize])
            for address in list(mem):
                if navbase<=address<navbase+navsize:
                    b=mem.pop(address);nav[address-navbase:address-navbase+len(b)]=b
            mem[navbase]=nav;mem[TRIANGLES]=bytearray(TRIANGLE_BYTES)+bytearray([165])*64
            calls=[dict(address='0x4ce690',registers=dict(ECX='0'),stack_args=[TRIANGLES,TRIANGLE_BYTES]),
                dict(address='0x4ce740',registers=dict(ECX='0'),stack_args=[old.NAVDATA,32,32,32]),
                dict(address='0x4c8f30',registers=dict(ECX='0'),stack_args=[]),
                dict(address='0x4cef20',registers=dict(ECX='0'),stack_args=[])]+calls
    calls += [dict(address='0x4ab180',registers=dict(ECX=hex(shot_node(c))),stack_args=[]) for _ in range(c.get('pre_ticks',0))]
    if c.get('ai_tables',True):
        from spell_state_tables import BASE as SB,SIZE as SS
        from creature_category_tables import BASE as CB,SIZE as CS
        from portal_animation_tables import BASE as AB,initialized_animations
        image_base,image=old.cpu.pe_image(EXE.read_bytes())
        for address,size in [(SB,SS),(CB,CS),(AB,len(initialized_animations()))]:
            mem[address]=bytearray(image[address-image_base:address-image_base+size])
        calls=[dict(address=hex(fn),registers=dict(ECX='0'),stack_args=[]) for fn in [0x47BBE0,0x4798F0,0x4950C0]]+calls
    args['before_calls']=calls
    args['registers']=json.dumps(dict(ECX=hex(shot_node(c))))
    # Explicit OS read-validity import provider over the enlarged scene.
    mem[readable.COUNTER]=bytearray(8)
    mem[readable.ADDRESS]=bytearray(readable.adapter(readable.allowed_ranges(EXE.read_bytes(),[region(a,b) for a,b in mem.items() if a!=readable.ADDRESS])))
    args['allowed_external_IO_code']=[x for x in args['allowed_external_IO_code'] if x[0]!=readable.ADDRESS]+[[LOOKUP,len(mem[LOOKUP])],[readable.ADDRESS,len(mem[readable.ADDRESS])]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[ACTOR,len(mem[ACTOR])],[TABLE,len(mem[TABLE])],[JRAW,len(mem[JRAW])],[JMID,len(mem[JMID])]]
    args['observe_writes'] += [[ACTOR,len(mem[ACTOR])],[TABLE,len(mem[TABLE])],[STATS,len(mem[STATS])]]
    if c.get('trap'):
        args['observe_reads'] += [[trap_node(c),291],[TDEF,209],[shot_node(c),371]]
        args['observe_writes'] += [[TBLOCK,len(mem[TBLOCK])]]
    args['max_steps']=3000000
    excluded=[0x7FFEE000,p.f.GFX_CB,p.f.SOUND_CB,p.base.target.attack.EVENT,p.base.target.LOAD_GFX,p.base.target.LOAD_SOUND,p.SOUND_UPDATE,LOOKUP,readable.ADDRESS]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
    checks[p.base.SHOT_VTABLE]=p.area.pe_read(EXE.read_bytes(),p.base.SHOT_VTABLE,0x100)
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
    return args,checks

def execute(c):
    args,checks=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_shot_pair_trace.py').read_bytes())).encode())[:12]
    file=ROOT/'ghidra'/f'phase170-jack-discovery-{tag}.json'
    if file.exists():saved=json.loads(file.read_text(encoding='utf-8'));assert saved['input']==args
    else:
        r=old.cpu.run(EXE,args,list(checks.items()),p.f.OBS,shot_node(c),371)
        saved=dict(case=c,input=args,result=r);file.write_text(json.dumps(saved,indent=2),encoding='utf-8')
    post=post_context(checks,saved['result']);return args,checks,saved,file,post

if __name__=='__main__':
    a,ch,s,f,post=execute(dict(mask=int(sys.argv[1]) if len(sys.argv)>1 else 64))
    r=s['result'];actor=post[ACTOR]
    print(json.dumps(dict(success=r['success'],error=r['emulation_error'],before=r['before_calls'],tail=r['trace_tail'],
        requests=r['hp_requests'],events=r['process_events'],actor=dict(tag=int.from_bytes(actor[4:6],'little'),species=actor[0x36F],HP=int.from_bytes(actor[0x40:0x44],'little'),
        owner=int.from_bytes(actor[0x28:0x2A],'little'),value2F5=int.from_bytes(actor[0x2F5:0x2F7],'little')),
        low=[x for x in r['observed_reads'] if 0<int(x['address'],16)<4096][:8])),flush=True)
