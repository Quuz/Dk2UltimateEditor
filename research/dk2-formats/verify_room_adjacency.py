"""Whole prepared-memory oracle, bounded rawloader baselines and FS-only SEH."""
import json,struct
from functools import cache
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_room_adjacency as n
import native_room_adjacency as api
import native_terrain_catalogue as terrain
from native_sacrifice import history_append
from build_effect_semantics import EXE,pe_read
from emulate_resistance import post_context

BASE_NAMES=['all','mode1','mode6','prison','combat-pit']
MEASURED=[n.prior.MID,n.prior.DEFINITION,n.prior.OUT,n.prior.NAV_BSS,n.prior.STATEBASE,n.prior.CATBASE,n.ROOMMID,n.prior.ROOMDEF,n.TERRAINMID,n.TABLE]

@cache
def baseline(name):
    c=next(c for c in n.cases() if c['name']==name)
    args,checks,saved,path=n.execute(c,True);post=post_context(checks,saved['result'])
    p=n.prior
    assert post[p.DEFINITION]==(n.ROOT/'ghidra/phase85-raw-creature-definition-raw-warlock.bin').read_bytes()+bytes([165])*64
    assert post[n.TERRAINMID]==terrain.intermediate(checks[n.TERRAINRAW][:552])+bytes([165])*64
    assert post[n.ROOMMID][0x469]==post[p.ROOMDEF][0x452]==c['mode']
    assert post[n.ROOMMID][0x466]==post[p.ROOMDEF][0x44D]==c['room_id']
    assert int.from_bytes(post[p.ROOMDEF][0xA2:0xA6],'little')==sum(post[n.ROOMMID][:1267])
    wanted={a:post[a] if a in MEASURED else b for a,b in checks.items()}
    w=bytearray(wanted[p.WORLD]);w[0x2E70:0x2E70+34]=post[p.WORLD][0x2E70:0x2E70+34];w[0x731B]=1;wanted[p.WORLD]=bytes(w)
    io=bytearray(wanted[n.IO]);n.put(io,0,int(post[n.ROOMMID][0x49B]!=0));wanted[n.IO]=bytes(io)
    assert post==wanted,[(hex(a),[hex(i) for i,(x,y) in enumerate(zip(post[a],wanted[a])) if x!=y][:8]) for a in wanted if post[a]!=wanted[a]]
    for a,size in [(n.ROOMMID,1267),(p.ROOMDEF,1158),(n.TERRAINMID,736),(n.ROOMRAW,1055),(n.TERRAINRAW,552)]:assert post[a][size:]==bytes([165])*64
    return post,dict(case=name,file=str(path.relative_to(n.ROOT)),sha256=n.sha(path.read_bytes()),
        scope='Full RoomMID1267/RoomDefinition1158 measured rawload baseline; their opaque fields are NOT newly named or semantically decoded. Creature2199 matches historical Ghidra image; TerrainMID736 matches independent established oracle.')

def expected(c,checks):
    p=n.prior
    name='prison' if c['room_id']==11 else 'combat-pit' if c['room_id']==16 else 'mode'+str(c['mode']) if c['mode']!=7 else 'all'
    base,_=baseline(name)
    out={a:bytearray(base[a] if a in MEASURED else b) for a,b in checks.items()}
    out[p.WORLD][0x2E70:0x2E70+34]=base[p.WORLD][0x2E70:0x2E70+34];out[p.WORLD][0x731B]=1
    n.put(out[n.IO],0,int(base[n.ROOMMID][0x49B]!=0))
    x,y=c['xy'];at=16*(y*5+x);terrain_id=checks[n.ROOMRAW][0x394]
    mask=api.construction7_mask(checks[p.MAP],5,5,x,y,terrain_id)
    cellC=mask if c['mode']==7 else 0 if c['mode']==1 else 165
    out[p.MAP][at+12]=cellC
    if c['mode']==7:
        n.put(out[n.IO],4,1);out[n.IO][8:24]=api.queue_request(x,y,5,5);n.put(out[p.SESSION],0x26C,1)
    accepted=c['entry'] and cellC==255
    if c['entry']:
        if accepted:
            out[p.ROOM][:]=history_append(out[p.ROOM],(1,5,3));out[p.ROOM][0x5B:0x5E]=bytes([255,0,0])
            n.put(out[p.ACTOR],0x40,0);n.put(out[p.ACTOR],0x2E,p.PLAY_TOKEN)
            flags=int.from_bytes(out[p.ACTOR][0x3E6:0x3EA],'little');n.put(out[p.ACTOR],0x3E6,(flags&~0x20)|0x40)
            out[p.OUT][4:]=struct.pack('<5I',1,0,p.LOOKUP_TOKEN,0xE2,p.ACTOR+0x1A)
            n.put(out[p.WORLD],0x15A0,0x3C88596C);n.put(out[p.OWNER],0x82,55);n.put(out[p.OWNER],0x10EE,5)
            n.put(out[n.IO],24,int.from_bytes(base[p.ROOMDEF][0x429:0x42B],'little')!=0)
        else:n.put(out[p.ACTOR],0x25E,0x67);n.put(out[p.ACTOR],0x266,0)
    return {a:bytes(b) for a,b in out.items()},dict(cellC=cellC,accepted_sacrifice=bool(accepted),transition_to67=bool(c['entry'] and not accepted))

def verify(c,args,checks,r):
    wanted,outcome=expected(c,checks);post=post_context(checks,r)
    assert post==wanted,(c['name'],[dict(address=hex(a),different=sum(x!=y for x,y in zip(post[a],wanted[a])),first=[hex(i) for i,(x,y) in enumerate(zip(post[a],wanted[a])) if x!=y][:12]) for a in wanted if post[a]!=wanted[a]])
    assert len(r['before_calls'])==16 and all(x['returned'] for x in r['before_calls'])
    assert int(r['registers']['EAX'],16)==(int(outcome['transition_to67']) if c['entry'] else 256)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(pe_read(blob,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    pcs={int(a,16) for a in r['visited_instructions']}
    assert (0x4EFDE0 in pcs)==(c['mode']==7)
    assert (0x4F1600 in pcs)==outcome['accepted_sacrifice']
    assert (0x4D1930 in pcs)==(c['mode']==7 and outcome['cellC']==255)
    assert 0x4CE770 not in pcs # NAV4 prestate; no full remesh coverage claimed.
    outcome.update(complete_regions=len(wanted),complete_bytes=sum(map(len,wanted.values())),steps=r['instruction_count'],nav_setter_called=0x4D1930 in pcs,full_remesh_executed=False,
        HP=int.from_bytes(post[n.prior.ACTOR][0x40:0x44],'little'),mana=int.from_bytes(post[n.prior.OWNER][0x82:0x86],'little'))
    return wanted,outcome

def main():
    baselines=[baseline(name)[1] for name in BASE_NAMES];rows=[]
    for c in n.cases():
        args,checks,saved,path=n.execute(c);wanted,outcome=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c['name'],file=str(path.relative_to(n.ROOT)),sha256=n.sha(path.read_bytes()),outcome=outcome))
        print(c['name'],outcome,flush=True)
    report=dict(phase=137,baselines=baselines,replays=rows,scope=__doc__)
    (n.ROOT/'phase137-room-adjacency-emulations.json').write_text(json.dumps(report,indent=2),encoding='utf8')

if __name__=='__main__':main()
