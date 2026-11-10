"""Two raw loaders, native factory and real direct/area/range damage consumers.

Every scenario checks loaded fields, full375-byte Shot intermediate, target HP,
original entries/vtables and resistance-table observations. Three measured output
baselines support paired context predictions; opaque bytes are not decoded.
"""
import hashlib
import json
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_shot_hit as probe
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent


def digest(b):return hashlib.sha256(b).hexdigest()


def path_for(args):
    dependency=''.join(digest((ROOT/n).read_bytes()) for n in ['probe_shot_hit.py','native_unicorn_factorytrace.py'])
    tag=digest((json.dumps(args,sort_keys=True)+dependency).encode())[:12]
    return ROOT/'ghidra'/f'phase114-shot-damage-{tag}.json'


def outcome(c):
    damage=c['damage'] if c['damage']<0x80000000 else c['damage']-0x100000000
    if c['damage_type']==7:
        if damage<=0 or c['word445']:damage=0
        elif c['kind']=='boulder':damage=damage//4 if c['category'] in [4,6] else 0
    else:
        if c['owner']==7 or c['word445']:damage=0
        elif c['damage_type']==2:damage=damage*(c['radius']*4096-c['shot_dx'])//(c['radius']*4096)
    return dict(HP=1000-damage,damage=damage,HP_calls=int(damage!=0))


def expected(c):
    name='boulder-fire' if c['kind']=='boulder' else ('range-res0' if c['shot']==17 else 'hit-res0')
    base=next(x for x in probe.cases() if x['name']==name)
    ba,bchecks=probe.build(base);v=json.loads(path_for(ba).read_text(encoding='utf8'))
    assert v['input']==ba and v['case']==base
    wanted=post_context(bchecks,v['result']);args,checks=probe.build(c)
    wanted[probe.RAW]=bytearray(checks[probe.RAW])
    mid=bytearray(b'\x01\0\0\0'*34+checks[probe.RAW]);mid[0xA7]=mid[0x170]=0
    wanted[probe.MID]=mid
    d=wanted[probe.DEFINITION]
    for offset,value,width in [(0x33,sum(mid),4),(0x37,c['damage'],4),(0x3B,c['radius'],4),
                               (0x51,c['category'],1),(0x6B,c['damage_type'],1)]:probe.f.put(d,offset,value,width)
    for i in range(4):
        wanted[probe.target.RAW][0xE0D+2*i]=c['value']
        wanted[probe.target.MID][0x11FD+2*i]=c['value']
        wanted[probe.target.attack.TARGET_DEF][0x61D+2*i]=c['value']
    t=wanted[probe.target.attack.TARGET];initial=checks[probe.target.attack.TARGET]
    for o,n in [(0x28,2),(0x2A,4),(0x445,2)]:t[o:o+n]=initial[o:o+n]
    e=outcome(c);probe.f.put(t,0x40,e['HP'])
    if not e['HP_calls']:
        for o,n in [(0x36,2),(0x38,4),(0x3C,4)]:t[o:o+n]=initial[o:o+n]
    b=wanted[probe.f.BLOCK];o=probe.NODE-probe.f.BLOCK
    probe.f.put(b,o+0x13D,c['damage']);probe.f.put(b,o+0x141,c['radius'])
    if c['shot_dx']:
        # Original factory inserts into the new cell and saves requested origin.
        xyz=checks[probe.f.ARGS];x=int.from_bytes(xyz[:4],'little');y=int.from_bytes(xyz[4:8],'little')
        oldx=int.from_bytes(initial[0x1A:0x1E],'little');oldy=int.from_bytes(initial[0x1E:0x22],'little')
        oldcell=((oldy>>12)*32+(oldx>>12))*16;newcell=((y>>12)*32+(x>>12))*16
        assert oldcell!=newcell
        cells=wanted[probe.f.MAP];cells[oldcell:oldcell+2]=checks[probe.f.MAP][oldcell:oldcell+2]
        probe.f.put(cells,newcell,102,2)
        probe.f.put(b,o+0xC,int.from_bytes(checks[probe.f.MAP][newcell:newcell+2],'little'),2)
        probe.f.put(b,o+0x1A,x);probe.f.put(b,o+0xF6,x)
        t[0xA:0xC]=initial[0xA:0xC]
        wanted[probe.f.ARGS]=bytearray(xyz)
    return wanted


def verify(c,args,checks,r):
    expected=outcome(c);post=post_context(checks,r)
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures']
    assert len(r['before_calls'])==10 and all(x['returned'] for x in r['before_calls'])
    assert r['function_or_vtable_substitutions']==0
    assert post[probe.SHOT_VTABLE]==checks[probe.SHOT_VTABLE]
    target=post[probe.target.attack.TARGET]
    assert int.from_bytes(target[0x40:0x44],'little')==expected['HP'],(c['name'],expected)
    assert target.hex()==r['captured_hex']
    assert int.from_bytes(post[probe.OUTPUT],'little')==probe.NODE
    assert int.from_bytes(post[0x6EE540][102*4:103*4],'little')==probe.NODE
    assert post[0x6ED540][102]==1
    raw=checks[probe.RAW];mid=bytearray(b'\x01\0\0\0'*34+raw)
    mid[0xA7]=mid[0x170]=0
    assert post[probe.MID]==mid
    definition=post[probe.DEFINITION]
    assert definition[0x51]==c['category'] and definition[0x58]==c['shot']
    assert definition[0x6B]==c['damage_type'] and definition[0x6C]==raw[0xC7]
    assert int.from_bytes(definition[0x33:0x37],'little')==sum(mid)
    assert int.from_bytes(definition[0x37:0x3B],'little')==c['damage']
    assert int.from_bytes(definition[0x3B:0x3F],'little')==c['radius']
    block=post[probe.f.BLOCK];off=probe.NODE-probe.f.BLOCK
    assert int.from_bytes(block[off:off+4],'little')==probe.SHOT_VTABLE
    assert int.from_bytes(block[off+0x125:off+0x129],'little')==probe.DEFINITION
    assert int.from_bytes(block[off+0x13D:off+0x141],'little')==c['damage']
    assert int.from_bytes(block[off+0x141:off+0x145],'little')==c['radius']
    loaded=post[probe.target.attack.TARGET_DEF]
    assert loaded[0x61C:0x624]==bytes([c['res_type'],c['value']])*4 and loaded[0x78A]==4
    visited=r['visited_instructions']
    for a in [0x491DC0,0x568820,0x568E30,0x492320,0x4ACD60,0x56E540,0x56E690,0x4ACDF0,0x4B7CD0,0x4AAC90]:
        assert visited.get(hex(a),0)==1,(c['name'],hex(a))
    assert visited.get('0x4b2ce0',0)==expected['HP_calls']
    if c['damage_type']==7:assert visited.get('0x4ac420',0)==1 and not visited.get('0x4ac3c0',0)
    else:
        assert visited.get('0x4ac3c0',0)==1 and not visited.get('0x4ac420',0)
        assert visited.get(hex(0x4AC5A0 if c['damage_type']==1 else 0x4AC6D0),0)==1
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes();resreads=[]
    for x in r['observed_reads']:
        a,pc=int(x['address'],16),int(x['pc'],16)
        if a<4096:
            ins=next(md.disasm(pe_read(data,pc,15),pc))
            assert a==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
        if any(a<probe.target.attack.TARGET_DEF+o+n and a+x['size']>probe.target.attack.TARGET_DEF+o for o,n in [(0x61C,8),(0x78A,1)]):
            assert pc in [0x4936B4,0x4936C8] and a==probe.target.attack.TARGET_DEF+0x78A
            resreads.append(x)
    assert len(resreads)==8
    assert not any(int(x['address'],16)<probe.SHOT_VTABLE+0x100 and int(x['address'],16)+x['size']>probe.SHOT_VTABLE for x in r['observed_writes'])
    hp=[x for x in r['observed_writes'] if int(x['address'],16)==probe.target.attack.TARGET+0x40]
    assert len(hp)==expected['HP_calls'] and all(x['value']==expected['HP'] for x in hp)
    return post


def main():
    rows=[]
    for c in probe.cases():
        args,checks=probe.build(c);p=path_for(args)
        if p.exists():v=json.loads(p.read_text(encoding='utf8'));assert v['input']==args and v['case']==c
        else:
            r=probe.target.cpu.run(EXE,args,list(checks.items()),probe.f.OBS,probe.target.attack.TARGET,1599)
            v=dict(case=c,input=args,result=r);p.write_text(json.dumps(v,indent=2),encoding='utf8')
        post=verify(c,args,checks,v['result'])
        wanted=expected(c)
        assert post==wanted,c['name']
        rows.append(dict(case=c['name'],file=p.name,sha256=digest(p.read_bytes()),outcome=outcome(c),
                         checked_regions=len(post),checked_bytes=sum(map(len,post.values()))))
        print(c['name'],outcome(c),'native fields/invariants verified',flush=True)
    (ROOT/'phase114-shot-damage-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':main()
