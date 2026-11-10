"""Publish verified portal offset consumer, raw patch helper and exact evidence."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
import emulate_portal_exit as native
import native_portal_offsets as api
import memory_readout_ymm as readout
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    out={}
    for key,items in load('phase91-primary-manifest.json').items():
        out[key]=[]
        for row in items:
            p=ROOT/'ghidra'/row['memory'];r=json.loads(p.read_text());b=bytes(r['data'])
            assert int(r['address'],16)==row['address'] and len(b)==r['length']==row['length'] and b==pe_read(Path(row['exe']).read_bytes(),row['address'],row['length'])
            out[key].append(dict(row,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    return out
def native_results():
    rows=load('phase91-portal-emulations.json')['replays'];cases={c['name']:c for c in native.cases()};pages={};outcomes=[];images={}
    assert len(rows)==6*len(cases)
    for row in rows:
        p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c=cases[row['case']];args,e=native.build(c,row['page']);r=saved['result']
        assert saved['input']==args and saved['expected']==e and saved['case']==c and digest(p.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0,EDX=0).items())
        pages.setdefault(c['name'],{})[row['page']]=readout.decode(r['registers'])
    for name,c in cases.items():
        assert set(pages[name])==set(range(6));b=b''.join(pages[name][i] for i in range(6))[:1343];e=native.build(c,0)[1];native.verify(c,b,e)
        file=f'ghidra/phase91-portal-{name}.bin';assert (ROOT/file).read_bytes()==b;images[name]=b;outcomes.append(dict(case=name,expected=e,capture=link(file)))
    assert images['spawn']==images['spawn-nonzero']==images['waiting']==images['waiting-nonzero']
    assert images['done-zero']==images['done-z'] and images['done-x']==images['tick-x']
    return rows,outcomes,images
def corpus():
    files=[];records=views=patches=0;aggregate=hashlib.sha256()
    for row in load('creature-lookup-review.json')['corpus']['files']:
        p=Path(row['file']);data=p.read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data
        hits=[(i,s) for i,s in enumerate(parts) if s.signature==170];assert hits
        for _,section in hits:
            for raw in section.records:
                v=api.view(raw,api.DX);assert v['xyz_signed']==list(struct.unpack_from('<3i',raw,0xe94));assert v['xyz_bits']==list(struct.unpack_from('<3I',raw,0xe94));views+=1
            records+=len(section.records)
        si,s=hits[0];raw=s.records[0]
        for axis,off in api.FIELDS.items():
            for value in [-4096,-1,4096]:
                changed=api.patch_signed(raw,axis,value,api.DX);assert changed==api.patch_bits(raw,axis,value&0xffffffff,api.DX)
                rr=list(s.records);rr[0]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
                packed=b''.join(clone.pack() if i==si else a.pack() for i,a in enumerate(parts));expected=bytearray(data);struct.pack_into('<i',expected,s.offset+len(s.header)+off,value)
                assert packed==expected and fmt.parse(packed)[si].records==rr;aggregate.update(hashlib.sha256(packed).digest());patches+=1
        files.append(dict(file=str(p),sha256=row['sha256'],records=sum(len(s.records) for _,s in hits)))
    assert len(files)==82 and records==views==3734 and patches==738
    return dict(files=files,file_count=len(files),records=records,profile_views=views,api_exact_container_patches=patches,ordered_patch_digest=aggregate.hexdigest(),
      scope='Nine API patches on first Creature record of each82 containers;3734 record views; all other bytes/sections/header/order/extensions retained. Source files never written. Not native emulation count.')
def examples():
    doc=load('decoded/Creatures.kwd.json');p=Path(doc['source']);data=p.read_bytes();assert digest(data)==doc['sha256'];rows=[]
    for r in doc['sections'][0]['records']:
        if r['fields']['CreatureId'] in [1,8,24]:
            raw=bytes.fromhex(r['raw_hex']);assert data[r['offset']:r['offset']+len(raw)]==raw
            rows.append(dict(creature_id=r['fields']['CreatureId'],name=r['fields']['Name'],record_offset=r['offset'],**api.view(raw,api.DX)))
    return dict(file=str(p),sha256=doc['sha256'],records=rows,scope='Current source bytes only; these concrete values were not separately composed with full raw Creature loader→spawn in phase91')
def refusals():
    raw=bytes(5449);count=0
    calls=[lambda:api.view(raw,'unknown'),lambda:api.view(bytes(5448),api.DX),lambda:api.patch_bits(raw,'q',0,api.DX),
      lambda:api.patch_bits(raw,'x',True,api.DX),lambda:api.patch_bits(raw,'x',-1,api.DX),lambda:api.patch_bits(raw,'x',1<<32,api.DX),
      lambda:api.patch_signed(raw,'y',1<<31,api.DX),lambda:api.patch_signed(raw,'z',-(1<<31)-1,api.DX)]
    for call in calls:
        try:call()
        except ValueError:count+=1
        else:raise AssertionError('Missing expected API refusal')
    return count
def main():
    p=primary();rows,outcomes,images=native_results();raw=corpus();ex=examples();rejected=refusals()
    prior=ROOT/'phase91-prior-components.json'
    if not prior.exists():
        c=load('editor-format-package.json')['components'];assert len(c)==69;prior.write_text(json.dumps(c,ensure_ascii=False,indent=2),encoding='utf8')
    s=dict(phase=91,date='2026-10-28',profile=load('creature-reward-semantics.json')['profile'],primary=p,native_replays=rows,outcomes=outcomes,
      corpus=raw,current_examples=ex,api_helpers=[link(n) for n in ['native_portal_offsets.py','emulate_portal_exit.py','portal_animation_tables.py']],
      physical_fields=[dict(axis=axis,raw_offset=off,width=4,storage='All32 physical bits; signed interpretation in addition',runtime_offset=0x6ec+4*i) for i,(axis,off) in enumerate(api.FIELDS.items())],
      source_to_runtime=dict(reference=link('creature-definition-semantics.json'),verified_phase=85,raw_start=0xe94,intermediate_start=0x1284,runtime_start=0x6ec,
        load='Unconditional12-byte transfer in exact DX loader; prior phase85 full raw/init/converter/loader/getter evidence retained without re-running'),
      original_spawn=dict(function='4DC9A0',callers=['4DBF90','4DC8F0'],room_type='4DC8F0 searches owner room list for type3; automatic attraction caller4DBF90 inspected statically only',
        gate='Owner DWORD76 bit400 required by4DC9A0',position='X=(roomBYTE2D<<12)+1800h; Y=(roomBYTE2E<<12)+1800h; requestedZ0',
        output='Actual Creature factory4B78D0/4B7900/4872A0; owner=roomWORD1D',
        initial_point='Creature XYZ offset triple does not select this initial point; full1343-byte spawn images equal for zero and nonzero offsets',
        other_writes='WORD F4=400h; request action31h; request stateC1h except room type19h; WORD95/97=0,WORD99=1000h; WORD4EB=source room tag; Creature flag3E6 bit2 set and retained after tested exit',
        owner_side_effects='5064B0 decrements the matching5-byte stock row;4C04A0 marks first-seen per-type BYTE; normal creation owner/population stats also update',
        fixture='Portal type3/tag200/anchor5,6,owner7; prepared stockID8=5→4; base spawnXYZ6800h/7800h/0; central cell floor0; message gateowner19A1'),
      actual_consumer=dict(function='459C20',state=193,category=7,initializer='47BBE0 at47EE22 writes callback459C20 into state193 row',
        dispatcher='476FC0 reads native state callback from6DA8B0+25*state; tested waiting and relocation through dispatcher',
        gate='496D00 is true for action other than0/21; while action49 active callback returns1 without relocation',
        animation='Native4950C0 initializes81×29-byte action rows. Action49 uses resource enum30, no direct action callback.496700/496980 set it during portal spawn.',
        native_tick='496AA0 clears action49 when actor graphics fieldDC is0 (null GFX fixture); this native tick followed by476FC0 produces actual relocation and state1. Real rendered-frame timing remains untested.',
        controlled_reset='Other done-* cases deliberately invoke original4966A0 on actor+38D to isolate completion; exactly same1343-byte result as actual496AA0 in done-x/tick-x comparison',
        arithmetic='Copy currentXYZ; add definition6EC/6F0/6F4 modulo2^32; use targetXY to resolve tile; replaceZ with4545C0 floor; native4B5560 moves/registers entity',
        units='4096 fixed-point units per map cell; X/Y apply directly in world axes in this consumer, no F4 rotation is applied',
        z='Raw/runtimeZ is read and added but overwritten before placement; done-zero and done-z7FFFFFFF complete actor images are identical',
        after_move='Writes finalXYZ into actorF6/FA/FE; clears WORD95/97/99; requests state1 via48AD30/476D30; calls46F390 follow-up (gated by prepared definition5D6=0)',
        sound='E0 sound only when ownerWORD28 equals World vslot264 (WorldWORD7243); matched7 plays once, local8 plays none',
        bounds='This callback calls World position→tile without new coordinate bounds check; safe editor preview must not claim arbitrary signed offsets stay on map'),
      data_initialization=dict(native=['4798F0','47BBE0','4950C0'],category_bytes=2048,state_bytes=7025,animation_bytes=2349,
        verification='All bytes compared after native initializers against independent instruction projections; category starts from PE, virtual state/animation tables startA5',
        lazy_static='For completed cases,494650 null-vector guard6E4144=1 and staticzero6E4148 are prepared as already initialized. First atexit registration and CRT shutdown are excluded; failure diagnostic retained.'),
      api=dict(file='native_portal_offsets.py',functions=['view(raw,exactDXSHA)','patch_bits(raw,axis,unsigned32,exactDXSHA)','patch_signed(raw,axis,signed32,exactDXSHA)'],
        advice='Expose X/Y as post-appearance portal exit offset. Preserve rawZ and unknown bits/bytes; do not label as initial spawn cell or forceZ to0.',refusals=rejected),
      counts=dict(distinct_native_cases=len(outcomes),native_replays=len(rows),complete_actor_images=len(outcomes),complete_actor_bytes=1343,
        whole_native_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),native_animation_tick_cases=2,native_AI_dispatcher_cases=2,
        API_exact_container_patches=raw['api_exact_container_patches'],source_files=raw['file_count'],source_records=raw['records'],api_refusals=rejected,
        original_game_function_or_vtable_patches=0),
      limitations=['Exact DX SHA only; Pro/RC3/local13 runtime behavior is not newly verified.',
        'Runtime Creature definition and stock/owner/room/grid are explicit prepared data. Raw→definition mapping is prior phase85 evidence, not one newly composed raw-Creatures→spawn run.',
        'Actual animation tick uses null-renderer fallbackDC0. Real resource/model frames, elapsed animation duration, movement/physics ticks and full process rendering are untested.',
        '4DBF90 automatic creature-selection/attraction scheduler and4DC8F0 selection wrapper are static evidence only; actual requestedID8 spawn function executes.',
        'Room type19 special branchstate116, dungeon portal selection among multiple rooms, stock exhaustion refusal and off-map offsets are not newly emulated.',
        'Other consumers of XYZ index0 at4598B0,472F31,48124F and other XYZ indices remain separate; this Z-overwrite rule must not be generalized to them.',
        'Captures combine six deterministic identical-input native replays, not an atomic live process dump; unnamed actor bytes remain opaque.',
        'Possession bonuses,Objects34C,Traps204,Doors236,Terrain198/224,Creature Priority/Jump and other major questions keep the full15-format goal open.'])
    (ROOT/'portal-exit-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']))
if __name__=='__main__':main()
