"""Publish actual damage reduction, healing ceiling and raw65 HP coefficient."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
import emulate_possession_health as native
import native_possession_health as api
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    out={}
    for key,items in load('phase93-primary-manifest.json').items():
        out[key]=[]
        for r in items:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    return out
def results():
    manifest=load('phase93-possession-health-emulations.json');rows=manifest['replays'];by={c['name']:c for c in native.cases()};out=[];assert len(rows)==len(by)==31
    for row in rows:
        p=ROOT/'ghidra'/row['file'];v=json.loads(p.read_text());c=by[row['case']];args,e=native.build(c);r=v['result']
        assert v['case']==c and v['input']==args and v['expected']==e and digest(p.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==x for k,x in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['hp']).items())
        model=api.ordinary_stage(c['hp'],c['delta'],1000,c['level'],c['coefficient'],possessed=c['possessed'],word443=bool(c.get('word443')),word447=bool(c.get('word447')),
          owner1095=c.get('owner1095',0),pit_word=c['pit_percent']*128//100 if 'pit_percent' in c else None,period=c['period'])
        assert model=={k:v for k,v in e.items() if k!='checks'},(c,model,e)
        out.append(dict(case=c,expected=e,evidence=row))
    return rows,out
def corpus():
    files=[];records=patches=0;aggregate=hashlib.sha256()
    for row in load('phase88-sacrifice-corpus.json')['files']:
        p=Path(row['file']);data=p.read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data;local=0
        for si,s in enumerate(parts):
            if s.signature!=0xe6:continue
            for ri,raw in enumerate(s.records):
                if len(raw)!=16 or struct.unpack_from('<2I',raw)!=(65,1):continue
                records+=1
                for value in [100,150,200,0xffffffff]:
                    changed=api.patch_level_value(raw,value);rr=list(s.records);rr[ri]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
                    packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));expected=bytearray(data)
                    off=s.offset+len(s.header)+sum(len(x) for x in s.records[:ri])+8;struct.pack_into('<I',expected,off,value)
                    assert packed==expected and fmt.parse(packed)[si].records==rr and changed[12:]==raw[12:];aggregate.update(hashlib.sha256(packed).digest());patches+=1;local+=1
        files.append(dict(file=str(p),sha256=row['sha256'],health_level_patches=local))
    assert len(files)==557 and records>0 and patches==4*records
    return dict(files=files,source_files=557,type65_health_records=records,API_exact_container_patches=patches,ordered_patch_digest=aggregate.hexdigest(),scope='All accepted phase88 Variables sources hashchecked/roundtripped;all type65/stat1 records patched atDWORD8 only. Other records,levelDWORD12,unknown bytes,headers/order retained. No source writes or prior24 refusal repairs.')
def main():
    p=primary();rows,out=results();c=corpus();prior=ROOT/'phase93-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==71;prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    s=dict(phase=93,date='2026-10-28',profile=load('possession-speed-semantics.json')['profile'],primary=p,native_replays=rows,outcomes=out,corpus=c,
      api_helpers=[link(n) for n in ['native_possession_health.py','emulate_possession_health.py']],
      hp_storage=dict(creature_raw=0xf0c,raw_width=2,runtime_definition=0x692,instance_hp=0x40,level_byte=0x403,
        source_binding='Existing Creature stat/layout/native loader evidence; new HP consumer reads definitionWORD692',
        ceiling='Level1 uses baseWORD692 directly; otherwise signedWORD[global6E4154+E1h+2*level] * baseWORD692 /100, truncated towardzero',
        level_record=dict(type=65,stat_id=1,stat_dword=4,value_dword=8,level_dword=12,loader='51DC40',writer='low16 value→6F261D+2*(9*fullDWORDstat+fullDWORDlevel)+2 modulo32',
          tested='Raw65/1/150/2→healing ceiling1500 frombase1000;200→2000. Level1 cases also load coefficient but ceiling bypasses it; no recommendation to add level1 records.'),
        caution='Ceiling adjusts positive request before the damage-modifier stage; it is not an unconditional final HP clamp if HP already exceeds the ceiling.'),
      health_function=dict(function='4B2CE0',scope='Complete original function, ordinary alive/nonlethal fixtures; no earlier game helper replaced',
        entry='Writes low16 second argument toactorWORD36 even for zero delta or healing-at-cap early return',
        heal='Positive requested delta is reduced to ceiling−oldHP when signed oldHP+delta>ceiling. Effective delta0 returns early.',
        damage_scale='Start128. For effective negative delta: WORD447!=0→160; if WORD443!=0 OR possessed3E6/80,subtract64 ONCE; positive signed ownerBYTE1095 subtracts8*value; flag3E6/8000 adds loadedWORD6F29F6−128; clamp factor below0 to0.',
        arithmetic='deltaApplied=trunc_toward_zero(signed32(low32(factor*effectiveDelta))/128); newHP=low32(oldHP+deltaApplied)',
        ordinary_possessed='128−64=64: requested−100 applies−50;−101 applies−50;−1 applies0. This is a hardcoded contribution in this function, not a newly found file percentage.',
        interactions='WORD443 and possession share an OR subtraction, do not stack twice. WORD447+possession yields96/128. ownerBYTE2 gives48/128 with possession;127 clamps to0 in the fixture.',
        pit='Raw type239/value50 is loaded through original51CD90 toWORD6F29F6=64. With flag8000 and no possession result64/128; with possession factor0. Global labelPIT_PERCENTAGE... is baseline, exact bit/consumer arithmetic proved.',
        over_cap='Prepared oldHP1500,base ceiling1000,request+200: effective delta−500; ordinary→1000;possessed→1250 because negative delta is reduced by factor64. Reachability of this over-cap state through normal live gameplay not proved.',
        times='For non-early tested paths actorDWORD38/3C=currenttick + (low32(sessionDWORD8*3)>>2). Tick100/period0→100;period20→115.',
        preserved='Other supplied actor/world/map/owner/variables and256 actor guard bytes compared in full.'),
      damage_event=dict(function='48F2E0',when='Effective delta negative and possession3E6/80 before factor adjustment/clamp, even when appliedHP delta0',
        packet='FourDWORDs:[ownerWORD28,8,definitionWORD86F,12h]; fixture[7,8,123,18]',
        path=['48F2E0','517480 reads World+4','527150 increments SessionDWORD26C and invokes Session+10 interface vslot28'],
        boundary='Original game World/Session code executes; synthetic external session-bridge method copies all16bytes and acknowledges1. Actual event interpretation/rendering not simulated.',
        observed='Sessioncounter and external capture count/data fullychecked;zero reaction events in unpossessed/positive-heal cases except over-cap positive request turned into effective damage'),
      possession_health_parameters=dict(type224_stat1='0,100,FFFF on damage and0,30000 on healing-cap paths gave identical results in this function',
        scalars98_113_114='0,6,30000 pairs on damage;30000 on healing-cap path unchanged. No global-unused claim follows.',
        conclusion='Verified possession survival mechanism here is reduced damage, with ordinary ceiling from base CreatureHP and raw65 health-level coefficient. Full possession health behavior and potential other consumers remain open.'),
      scope=dict(chain=['raw224/1→51DC70','raw65/1/currentlevel→51DC40','optionalraw239→51CD90','actual48DEA0(1)','whole4B2CE0','original48F2E0/517480/527150','external bridge packet capture'],
        setup='Already-created actor/world image and prepared runtime definition inherited from verified phases90/92; no new allocation/actor images.',
        source_actor=load('possession-speed-semantics.json')['native_scope']['source_actor'],
        excluded='Invulnerability gates, lethal/death/rebirth transitions, special animation statesD1/FF/100/101, fullShot26→control and gameplayticks; no source modifications.'),
      counts=dict(native_cases=len(out),native_replays=len(rows),whole_native_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),
        source_files=c['source_files'],type65_health_records=c['type65_health_records'],API_exact_container_patches=c['API_exact_container_patches'],
        event_packets=sum(r['expected']['event'] for r in out),raw239_cases=sum('pit_percent' in r['case'] for r in out),new_actor_images=0,original_game_function_or_vtable_patches=0),
      limitations=['Exact supplied DX SHA;other game/editor versions not executed in this phase.',
        'Ordinary alive/nonlethal health paths only. ResultHP1 tested;lethal0/death,rebirth,immune/frozen gates and special combat states remain outside new native coverage.',
        'This proves a hardcoded damage contribution, not that every possession benefit is hardcoded or scalar98/113/114 globallyunused.',
        'Full Shot26/player possession action/control cycle, renderer response and real movement/combat ticks not composed.',
        'Prepared over-ceiling HP tests show conditional arithmetic;they do not establish a normal-game or original-editor bug.',
        'Runtime Creature definition and earlier native actor capture are explicit fixtures;raw65/224/239 callbacks execute,raw Creature loader/creation is not rerun.',
        'Other type224 stat consumers,Objects34C,Traps204,Doors236,Terrain198/224,Creature Priority/Jump and remaining15-format questions keep the goal open.'])
    (ROOT/'possession-health-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']))
if __name__=='__main__':main()
