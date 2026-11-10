"""Three raw loaders -> Creature cast -> real collision -> poison -> full ticks.

Prepared common cell and cardinal trig coefficients; no full World scheduler.
Exact parameters/state/HP/timer writes, original entries and resistance reads
checked; selected pairs also compare every prepared context. Opaque baseline
images are measured, not independently decoded.
"""
import hashlib
import json
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_poison as probe
from emulate_resistance import post_context
from native_spell_alternatives import full_intermediate,runtime_projection
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent


def digest(b):return hashlib.sha256(b).hexdigest()


def cases():
    base=dict(ticks=1,duration=50,severity=5,value=0,freeze=0,invulnerable=0,armour=0,reapply=False,defaults=None)
    changes=[('apply-only',dict(ticks=0)),('tick1',{}),('res100',dict(value=100)),('res255',dict(value=255)),
        ('ends-on-one',dict(duration=1)),('ends-before-third',dict(duration=2,ticks=3)),
        ('widebits',dict(duration=0x10002,severity=0x10005)),('zero-duration',dict(duration=0)),
        ('zero-severity',dict(severity=0)),('frozen',dict(freeze=1)),('thawed',dict(freeze=1,ticks=2)),
        ('invulnerable',dict(invulnerable=2)),('armoured',dict(armour=2)),
        ('reapply-callback',dict(reapply=True)),('defaults-overridden',dict(defaults=[123,9])),
        ('tick2',dict(ticks=2))]
    return [dict(base,**kw,name=name) for name,kw in changes]


def inputs(c):return {k:v for k,v in c.items() if k!='name'}


def path_for(args):
    deps=''.join(digest((ROOT/n).read_bytes()) for n in ['probe_poison.py','native_unicorn_factorytrace.py'])
    return ROOT/'ghidra'/('phase115-poison-'+digest((json.dumps(args,sort_keys=True)+deps).encode())[:12]+'.json')


def outcome(c):
    duration,severity=c['duration']&0xFFFF,c['severity']&0xFFFF
    timer=duration;freeze=c['freeze'];armour=c['armour'];inv=c['invulnerable'];hp=1000;calls=0
    timers=[timer];hpwrites=[];actor_attributes=[]
    for i in range(c['ticks']+int(c['reapply'])):
        if c['reapply'] and i==c['ticks']:timer=duration;timers.append(timer)
        armour=max(0,armour-1)
        if freeze:freeze-=1
        elif timer:
            timer-=1;timers.append(timer);calls+=1
            damage=(severity*(64 if armour else 128))//128
            if severity:
                hp-=damage;hpwrites.append(hp)
            actor_attributes.append(7)
        inv=max(0,inv-1)
    return dict(HP=hp,timer=timer,severity=severity,owner=7,freeze=freeze,armour=armour,invulnerable=inv,
                ticks=c['ticks']+int(c['reapply']),HP_calls=calls,timer_writes=timers,HP_writes=hpwrites,
                applications=1+int(c['reapply']))


def paired_expected(c,baseline):
    wanted={a:bytearray(b) for a,b in baseline.items()}
    args,checks=probe.build(**inputs(c))
    for i in range(4):
        wanted[probe.target.RAW][0xE0D+2*i]=c['value']
        wanted[probe.target.MID][0x11FD+2*i]=c['value']
        wanted[probe.target.attack.TARGET_DEF][0x61D+2*i]=c['value']
    for a in [probe.RAWSPELL,probe.shot.RAW]:wanted[a]=bytearray(checks[a])
    mid=full_intermediate(checks[probe.RAWSPELL])
    wanted[probe.MIDSPELL]=bytearray(mid);wanted[probe.SPELL]=bytearray(runtime_projection(mid))
    smid=bytearray(b'\x01\0\0\0'*34+checks[probe.shot.RAW]);smid[0xA7]=smid[0x170]=0
    wanted[probe.shot.MID]=smid
    for off,value in [(0x33,sum(smid)),(0x37,int.from_bytes(checks[probe.shot.RAW][0x98:0x9C],'little')),
                       (0x3B,int.from_bytes(checks[probe.shot.RAW][0x9C:0xA0],'little'))]:
        probe.f.put(wanted[probe.shot.DEFINITION],off,value)
    for off,value in [(0x13D,c['duration']),(0x141,c['severity'])]:probe.f.put(wanted[probe.f.BLOCK],probe.shot.NODE-probe.f.BLOCK+off,value)
    probe.f.put(wanted[probe.target.attack.TARGET],0x45D,outcome(c)['timer'],2)
    return wanted


def verify(c,args,checks,r):
    e=outcome(c);post=post_context(checks,r)
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures']
    assert len(r['before_calls'])==15+c['ticks']+2*int(c['reapply']) and all(x['returned'] for x in r['before_calls'])
    actor=post[probe.target.attack.TARGET]
    assert actor.hex()==r['captured_hex']
    for o,n,v in [(0x40,4,e['HP']),(0x45D,2,e['timer']),(0x45F,2,e['severity']),
                  (0x461,2,7),(0x447,2,e['freeze']),(0x445,2,e['invulnerable']),(0x443,2,e['armour'])]:
        assert int.from_bytes(actor[o:o+n],'little')==v,(c['name'],hex(o),actor[o:o+n].hex(),v)
    assert actor[1343:]==bytes([0xA5])*256
    raw=checks[probe.RAWSPELL];mid=full_intermediate(raw)
    assert post[probe.MIDSPELL]==mid
    assert post[probe.SPELL]==runtime_projection(mid,tps=20)
    block=post[probe.f.BLOCK];o=probe.shot.NODE-probe.f.BLOCK
    assert int.from_bytes(block[o:o+4],'little')==probe.shot.SHOT_VTABLE
    assert int.from_bytes(block[o+4:o+6],'little')==102
    assert int.from_bytes(block[o+0x13D:o+0x141],'little')==c['duration']
    assert int.from_bytes(block[o+0x141:o+0x145],'little')==c['severity']
    assert int.from_bytes(block[o+0x145:o+0x147],'little')==100
    assert int.from_bytes(block[o+0x147:o+0x149],'little')==101
    # DIE_WHEN_HIT_THING requests HP0; no scheduler deletion was executed.
    assert int.from_bytes(block[o+0x40:o+0x44],'little')==0 and post[0x6ED540][102]==1
    assert post[probe.shot.SHOT_VTABLE]==checks[probe.shot.SHOT_VTABLE]
    loaded=post[probe.target.attack.TARGET_DEF]
    assert loaded[0x61C:0x624]==bytes([41,c['value']])*4 and loaded[0x78A]==4
    visits=r['visited_instructions']
    for a in [0x491DC0,0x568820,0x568E30,0x492320,0x4ACD60,0x56E540,0x56E690,0x4ACDF0,
              0x56A490,0x56A560,0x4F7860,0x4984D0,0x4B7CD0,0x4AAC90,0x4ABCA0,0x4AD1B0,0x4ABD60]:
        assert visits.get(hex(a),0)==1,(c['name'],hex(a))
    assert visits.get('0x4b0270',0)==visits.get('0x48cc50',0)==e['applications']
    assert visits.get('0x488930',0)==visits.get('0x48e160',0)==e['ticks']
    assert visits.get('0x4b2ce0',0)==e['HP_calls']
    assert not visits.get('0x4ac420',0)
    w=r['observed_writes']
    for offset,expected in [(0x45D,e['timer_writes']),(0x40,e['HP_writes'])]:
        actual=[x['value'] for x in w if int(x['address'],16)==probe.target.attack.TARGET+offset]
        assert actual==expected,(c['name'],hex(offset),actual,expected)
    assert all(int(x['pc'],16)==0x4B02CD for x in w if int(x['address'],16)==probe.target.attack.TARGET+0x461)
    for x in w:
        assert not (int(x['address'],16)<probe.shot.SHOT_VTABLE+0x100 and
                    int(x['address'],16)+x['size']>probe.shot.SHOT_VTABLE)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes();watched=[]
    for x in r['observed_reads']:
        a,pc=int(x['address'],16),int(x['pc'],16)
        if a<4096:
            i=next(md.disasm(pe_read(data,pc,15),pc));assert a==0 and x['size']==4 and 0x64 in i.prefix and 'fs:[0]' in i.op_str
        if any(a<probe.target.attack.TARGET_DEF+off+n and a+x['size']>probe.target.attack.TARGET_DEF+off for off,n in [(0x61C,8),(0x78A,1)]):
            assert pc in [0x4936B4,0x4936C8] and a==probe.target.attack.TARGET_DEF+0x78A;watched.append(x)
    assert len(watched)==8
    return post


def main():
    rows=[];posts={}
    for c in cases():
        args,checks=probe.build(**inputs(c));p=path_for(args)
        if p.exists():v=json.loads(p.read_text(encoding='utf8'));assert v['case']==c and v['input']==args
        else:
            r=probe.target.cpu.run(EXE,args,list(checks.items()),probe.f.OBS,probe.target.attack.TARGET,1599)
            v=dict(case=c,input=args,result=r);p.write_text(json.dumps(v,indent=2),encoding='utf8')
        posts[c['name']]=verify(c,args,checks,v['result'])
        rows.append(dict(case=c['name'],file=p.name,sha256=digest(p.read_bytes()),outcome=outcome(c),
                         captured_contexts=len(checks),captured_bytes=sum(map(len,checks.values()))))
        print(c['name'],outcome(c),'native chain verified',flush=True)
    # Four complete changed-byte predictions from one measured tick1 baseline.
    paired=['res100','res255','widebits','defaults-overridden']
    for name in paired:
        c=next(x for x in cases() if x['name']==name)
        assert paired_expected(c,posts['tick1'])==posts[name],name
    (ROOT/'phase115-poison-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,
        paired_full_context_cases=paired,independent_Ghidra_CPU_controls=0),indent=2),encoding='utf8')


if __name__=='__main__':main()
