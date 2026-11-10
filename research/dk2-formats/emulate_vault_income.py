"""Raw Terrain -> original ownership/count update -> full Player mana handler.

Both4559D0 and its real4BE070 helper execute unmodified. Prepared players/world;
not the enclosing full CMap terrain-replacement or complete vault capture event.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path
import dk2_formats as fmt
import emulate_factory_mana as source
import native_terrain_catalogue as terrain
import native_unicorn_fieldtrace as cpu
from build_effect_semantics import EXE
from emulate_element_colors import region

ROOT=Path(__file__).resolve().parent
f=source.f
RAW,MID,NODE,OTHER=0x10092000,0x10093000,0x10094000,0x100A0000
TERRAIN_FILE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Terrain.kwd'


def cases():
    base=dict(terrain_id=32,gain=None,ceiling=None,old_owner=0,new_owner=7)
    return [dict(base,**v,name=n) for n,v in [
        ('unclaimed-terrain',dict(terrain_id=31)),('claimed-terrain',{}),
        ('claimed-edited-gain',dict(gain=125,ceiling=200)),
        ('claimed-signed-words',dict(gain=65535,ceiling=65526)),
        ('same-owner',dict(old_owner=7)),('transfer8-to7',dict(old_owner=8)),
    ]]


def raw_record(c):
    rows=[b for s in fmt.parse(TERRAIN_FILE.read_bytes()) if s.signature==0x6E for b in s.records if b[0x1D6]==c['terrain_id']]
    assert len(rows)==1
    raw=bytearray(rows[0])
    for offset,name in [(0x19C,'gain'),(0x19E,'ceiling')]:
        if c[name] is not None:struct.pack_into('<H',raw,offset,c[name])
    return bytes(raw)


def build(c):
    args,_,_,_=source.build(source.cases()[0])
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    raw=raw_record(c);mid=terrain.intermediate(raw);definition,_=terrain.definition(mid,bytes(460),1)
    memory[RAW]=bytearray(raw);memory[MID]=bytearray([0xA5])*736
    memory[f.TERRAIN]=bytearray(460)
    memory[NODE]=bytearray(16)
    f.put(memory[NODE],8,0xA000|c['old_owner'],2);f.put(memory[NODE],10,c['terrain_id'],1)
    memory[0x6FCDCF]=bytearray(struct.pack('<I',1))
    memory[0x6DA4A4]=bytearray(struct.pack('<I',f.WORLD))
    cache=bytearray(1024);f.put(cache,4*c['terrain_id'],f.TERRAIN)
    memory[0x6DA4A8]=cache
    if c['old_owner']==8:
        memory[OTHER]=bytearray(0x1200)
        for off,value in [(0xB3F,1),(0x10E2,20),(0x10DE,500)]:f.put(memory[OTHER],off,value)
        f.put(memory[0x6EE540],8*4,OTHER);memory[0x6ED540][8]=1
    checks={a:bytes(b) for a,b in memory.items() if a not in [0x7FFEF000,f.GFX_CB,f.SOUND_CB]}
    wanted={a:bytearray(b) for a,b in checks.items()}
    wanted[MID],wanted[f.TERRAIN]=bytearray(mid),bytearray(definition)
    gain,cap=struct.unpack_from('<hh',raw,0x19C)
    changed=c['old_owner']!=c['new_owner']
    if changed:
        f.put(wanted[NODE],8,0xA000|c['new_owner'],2)
        for off,value in [(0xB3F,1),(0x10E2,20+gain),(0x10DE,500+cap)]:f.put(wanted[f.PLAYER],off,value)
        if c['old_owner']==8:
            for off,value in [(0xB3F,0),(0x10E2,20-gain),(0x10DE,500-cap)]:f.put(wanted[OTHER],off,value)
    extra=20+(gain if changed else 0);ceiling=500+(cap if changed else 0)
    delta=min(extra+100,ceiling)
    current=min(1000+delta,10000) if delta>=0 else max(1000+delta,0)
    for off,value in [(0x82,current),(0x9A,100),(0x76,0x100)]:f.put(wanted[f.PLAYER],off,value)
    calls=[dict(address='0x56eb60',registers=dict(ECX=hex(MID)),stack_args=[]),
           dict(address='0x56ed20',registers=dict(ECX=hex(MID)),stack_args=[RAW]),
           dict(address='0x4f66e0',registers=dict(ECX=hex(f.TERRAIN)),stack_args=[MID,0]),
           dict(address='0x4559d0',registers=dict(ECX=hex(NODE)),stack_args=[c['new_owner']])]
    args.pop('allowed_external_IO_code',None)
    args.update(address='0x4c10e0',registers=json.dumps(dict(ECX=hex(f.PLAYER))),before_calls=calls,
                memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),
                observe_reads=[[f.TERRAIN,460]],observe_writes=[[f.PLAYER,0x1200],[OTHER,0x1200]])
    e=dict(gain=gain,ceiling_contribution=cap,current=current,extra=extra,ceiling=ceiling,
           tile_count_delta=int(changed),original_count_helper_calls=int(changed)*(1+int(c['old_owner']!=0)))
    return args,e,checks,wanted


def verify(c,args,e,checks,wanted,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures']
    assert len(r['before_calls'])==4 and all(x['returned'] for x in r['before_calls'])
    post={a:bytearray(b) for a,b in checks.items()}
    for row in r['context_mismatches']:
        a=int(row['address'],16);assert len(post[a])==row['length']
        for d in row['differences']:
            assert post[a][d['offset']]==d['expected'];post[a][d['offset']]=d['actual']
    assert post==wanted,(c['name'],[(hex(a),[(hex(i),x,y) for i,(x,y) in enumerate(zip(post[a],wanted[a])) if x!=y][:20]) for a in post if post[a]!=wanted[a]])
    assert bytes.fromhex(r['captured_hex'])==bytes(post[f.PLAYER])
    visits=r['visited_instructions']
    assert all(hex(a) in visits for a in [0x56EB60,0x56ED20,0x4F66E0,0x4559D0,0x4C10E0,0x4C17A0])
    assert visits.get('0x4be070',0)==e['original_count_helper_calls']
    assert not any(int(a,16)>=0x10000000 for a in visits)


def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    rows=[];backend=hashlib.sha256((ROOT/'native_unicorn_fieldtrace.py').read_bytes()).hexdigest()
    for c in selected:
        args,e,checks,wanted=build(c)
        tag=hashlib.sha256((json.dumps(args,sort_keys=True)+backend).encode()).hexdigest()[:10]
        p=ROOT/'ghidra'/f'phase111-vault-income-{c["name"]}-{tag}.json'
        if p.exists():
            value=json.loads(p.read_text(encoding='utf8'));assert value['case']==c and value['input']==args and value['expected']==e
        else:
            result=cpu.run(EXE,args,list(checks.items()),f.OBS,f.PLAYER,0x1200)
            value=dict(case=c,input=args,expected=e,result=result);p.write_text(json.dumps(value,indent=2),encoding='utf8')
        verify(c,args,e,checks,wanted,value['result'])
        rows.append(dict(case=c['name'],file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
        print(c['name'],e,'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase111-vault-income-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':main()
