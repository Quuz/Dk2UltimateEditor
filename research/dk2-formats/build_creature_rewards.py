"""Publish native Creature reward evidence as a new, independent component."""
from pathlib import Path
import json,hashlib
import emulate_creature_factory as factory
import emulate_sacrifice_creatures as temple
import memory_readout_ymm as readout
from build_effect_semantics import pe_read
from creature_category_tables import initialized_categories
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    m=load('phase90-primary-manifest.json');out={}
    for key in ['functions','blocks']:
        rows=[]
        for r in m[key]:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==r['length']==v['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            rows.append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
        out[key]=rows
    assert any(r['address']==0x4872a0 for r in out['functions'])
    return out
def native_results(module,file,prefix):
    manifest=load(file);by={c['name']:c for c in module.cases()};images={};rows=manifest['replays'];chunks={};outcomes=[]
    assert len(rows)==6*len(by)
    for row in rows:
        c=by[row['case']];p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());args,e=module.build(c,row['page']);r=saved['result']
        assert saved['input']==args and saved['expected']==e and saved['case']==c and digest(p.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0,EDX=0).items()),(c['name'],r)
        chunks.setdefault(c['name'],{})[row['page']]=readout.decode(r['registers'])
    for name,c in by.items():
        assert set(chunks[name])==set(range(6));b=b''.join(chunks[name][i] for i in range(6))[:1343];e=module.build(c,0)[1];module.verify(c,b,e)
        file=f'ghidra/{prefix}-{name}.bin';assert (ROOT/file).read_bytes()==b
        images[name]=b;outcomes.append(dict(case=name,expected=e,capture=link(file)))
    return rows,outcomes,images
def main():
    p=primary();fr,fo,fi=native_results(factory,'phase90-creature-factory-emulations.json','phase90-creature-factory')
    tr,to,ti=native_results(temple,'phase90-sacrifice-creature-emulations.json','phase90-sacrifice-creature')
    assert ti['level0']==ti['level1']
    assert ti['level3']==ti['level3-high-bits']==ti['raw-level3']
    assert ti['level10']==ti['level11']==ti['level255']
    prior=ROOT/'phase90-prior-components.json'
    if not prior.exists():
        c=load('editor-format-package.json')['components'];assert len(c)==68
        prior.write_text(json.dumps(c,ensure_ascii=False,indent=2),encoding='utf8')
    s=dict(phase=90,date='2026-10-28',profile=load('object-reward-semantics.json')['profile'],primary=p,
      native_factory_replays=fr,native_reward_replays=tr,factory_outcomes=fo,reward_outcomes=to,
      api_helpers=[link(n) for n in ['emulate_creature_factory.py','emulate_sacrifice_creatures.py','creature_category_tables.py','fixture_read_probe.py']],
      prior_sacrifice_component=load('phase90-prior-components.json')['sacrifice_semantics'],
      instance_storage=dict(bytes=1343,native_vtable='66D524',constructor='4B5E10',factory_wrapper='4B78D0',factory='4B7900',initializer='4872A0',
        pool=dict(initializer='4B5A10',count=448,stride=1343,allocation_bytes=0x92e44,header_bytes=4,verified='Static original vector-constructor call; fixture executes one actual ctor, not the full448-slot pool allocation'),
        fields=[dict(offset=o,width=n,role=role) for o,n,role in [(4,2,'Registered entity tag'),(0x12,4,'Thing category0'),(0x1a,12,'PositionXYZ'),
          (0x28,2,'Owner tag'),(0x2a,4,'Owner pointer'),(0x40,4,'Health'),(0x24a,2,'Unused-list previous tag'),(0x24c,2,'Unused-list next tag'),
          (0x25e,4,'AI state'),(0x36f,1,'Creature catalogue ID'),(0x370,4,'Definition pointer'),(0x3b9,2,'Registered room tag'),(0x403,1,'Experience level')]],
        opaque='Complete instance images preserve unnamed bytes; ctor does not justify calling untouched/stale bytes padding.'),
      creation=dict(manager='Global6E504C aliases World+8; managerWORD1C=unused head; managerDWORD1E=active count',
        allocation='Factory reuses already constructed/registered actor from the unused list; no Object-pool allocation or fresh tag inside factory',
        fixture='One unused actor is constructed with native4B5E10 and tag100, then real4B78D0/4B7900 executes. Prepared Creature definition2199, owner7, positiveHP1000.',
        side_effects='Unused head→0, active count+1, world kind head/tag/map/owner creature lists and per-type count update; guard256 and Object pool unchanged',
        owner_timer=dict(writer='4BE330',called_at='48795E',flag_dword=0x76,flag_mask=0x200,start_tick=0xb4f,end_tick=0xb53,
          formula='end=(World tick + DWORD6F2611 * DWORD6F2590) modulo2^32; start=World tick; set flag200',
          tested='Fixture tick100,TPS20,global6F2611=0; both tick fields100. Meaning of this owner timer beyond the native read/write is not assigned.',
          remaining_reader='4BE180 returns end-current tick'),
        prng='491900 and4919C0 consume two native World+15A0 LCG draws; select WORD4ED and BYTE4EF avoiding values in the owner creature list where possible'),
      creature_reward=dict(dispatcher='4F1780',kind=1,id='value & FFh',requested_level='(value >> 16) & FFh',ignored_id_level_bits='BYTE1/BYTE3 do not affect the tested created actor; original DWORD remains preserved',
        owner='RoomWORD1D, not Object-owner global6F2548',position='Room tile chosen by4E4D90; World1D4→4545C0 supplies floor Z; metadata point does not select spawn',
        fixture='Tile5/6 yields XYZ5800h/6800h/1000h; argument point9800h/A800h/3000h is ignored for placement',
        levels='Factory resets level to1. Requests>1 call native48B120 requested−1 times; for the alive positiveHP fixture, helper stops at10. Requests0/1→1;3→3;10/11/255→10.',
        health='48B120 preserves health ratio using original arithmetic helpers; health WORD coefficients reside at Variables+E1+2*level (packed, odd). 100% base1000→1000; level2 coefficient150→1500.',
        raw_chain=dict(cases=['raw-level3'],functions=['51DB30','50F7E0','4F1600','4F1660','4F1780','4B78D0','4B7900','4872A0','48B120','48AD30','476D30'],
          input='Type43 recipe Creature16→Creature8, value8; native history event level3; matcher produces reward00030008',
          result='All1343 actor bytes equal direct runtime00030008 andFE03AB08 cases'),
        state_request='After creation/level calls,48AD30 requests6A through476D30; request is conditional, not a forced assignment',
        room_entry=dict(callback='47B8C0',checks=['TileWORD2 low12 resolves room via4559B0','4E7F80 room acceptance','4E53B0 capacity: usedWORD6B + definitionBYTE672 <= capacityWORD67'],
          registration='4E5A70 adds actor to room listWORD0A, writes actorWORD3B9=room tag, increments usedWORD6B and6D by required capacity',
          fixture='room13/tag200/owner7/capacity100; creature capacity1; native state6A and room list entry verified',
          full='Capacity0 still yields a creature and cleared history, but state remains1 and room registration0. No undo of awarded creature was observed.'),
        ai_initialization=dict(functions=['4798F0','47BBE0'],category_bytes=2048,category_bytes_written=initialized_categories()[1],state_bytes=7025,
          verification='Original initializers execute before reward. Every final byte is compared to independent straight-line projections; initial category bytes equal original PE, state bytes startA5. No callback table is patched to force success.')),
      external_providers=dict(gfx_a0='Null, five args RET20, factory creates one presentation request',gfx_90='Null, six args RET24, one request per successful level increment',
        sound='Fixed external provider, four args RET16; one call per successful level increment',
        IsBadReadPtr='Existing range-checked external OS provider; native RTTI/game functions remain original; zero calls in the passing phase90 fixtures'),
      counts=dict(factory_cases=len(fo),temple_cases=len(to),distinct_cases=len(fo)+len(to),native_replays=len(fr)+len(tr),complete_actor_images=len(fo)+len(to),
        complete_actor_image_bytes=1343,whole_native_function_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),raw_to_creature_cases=1,raw_to_creature_replays=6,
        original_game_function_or_vtable_adapters=0,new_raw_patch_checks=0,prior_type43_raw_patch_checks=1188),
      limitations=['Exact DX executable SHA only; Pro/RC3/local13 not executed in this phase.',
        'Prepared runtime Creature definition and level tables; full raw Creatures catalogue→definition→creation composition not executed here.',
        'One unused slot exercises actual ctor/factory; full448-slot allocation, reuse after death, alias/clone definitions and active448 refusal were not newly emulated.',
        'Six identical-input deterministic replays capture each1343-byte image; not an atomic live-process dump.',
        'Health-level table is prepared packed data. Earlier aligned-fill diagnostic reads0x6400 instead of100; it is not evidence of a game bug.',
        'Visuals/sound use external providers; real rendering/speech, gameplay ticks and removal of the creature from the temple remain untested.',
        'This closes successful Creature reward creation/level/room-entry in the stated fixtures; whole spell sacrifice event→callback→created creature was not composed here.',
        'Portal first-spawn lifecycle, possession bonus consumers, Objects34C, Traps204, Doors236, Terrain198/224, Creature Priority/Jump and other significant unknowns remain open.'])
    (ROOT/'creature-reward-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(s['counts']))
if __name__=='__main__':main()
