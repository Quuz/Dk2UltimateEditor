"""Integrate only new context/file-loading extension of completed phase92."""
from copy import deepcopy
from pathlib import Path
import json,struct
import emulate_possession_speed_context as n
import crosscheck_possession_speed_context as cross
import native_speed_context as api
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha
KEY,SCHEMA='possession_speed_context_semantics','possession-speed-context-semantics.json'
link=lambda name:dict(file=name,sha256=sha((ROOT/name).read_bytes()))
canonical=lambda x:sha(json.dumps(x,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['probe_possession_speed_context.py','emulate_possession_speed_context.py','crosscheck_possession_speed_context.py','native_speed_context.py','prepare_possession_speed_context_primary.py','build_possession_speed_context.py','audit_possession_speed_context.py','probe_creature_work_level.py','emulate_possession_speed.py','emulate_variables_reader.py','native_unicorn_mapreader.py']


def physical():
    fmt=n.p.files.fmt;paths=[ROOT.parents[1]/'Dungeon Keeper 2/Data/editor/Creatures.kwd',n.p.files.GLOBAL];rows=[]
    for path in paths:
        raw=path.read_bytes();parts=fmt.parse(raw);assert b''.join(s.pack() for s in parts)==raw
        if path.name=='Creatures.kwd':
            si,s=next((i,s) for i,s in enumerate(parts) if s.signature==170);ri=next(i for i,r in enumerate(s.records) if r[0xF4B]==8)
            fields=[(off,v) for off in [0xEB4,0xEB8,0xD37] for v in [0,1,20*65536,0xffffffff]]
            flags=struct.unpack_from('<I',s.records[ri],0xEFC)[0];fields += [(0xEFC,flags&~1),(0xEFC,flags|1)]
        else:
            si,s=next((i,s) for i,s in enumerate(parts) if s.signature==230);ri=next(i for i,r in enumerate(s.records) if struct.unpack('<4I',r)[:2]==(224,21))
            assert struct.unpack('<4I',s.records[ri])==(224,21,200,10)
            fields=[(8,v) for v in [0,200,0xABCD00C8,0x8000,0xffffff9c]]
        for offset,value in fields:
            parts=fmt.parse(raw);records=parts[si].records.copy();record=records[ri];records[ri]=record[:offset]+struct.pack('<I',value)+record[offset+4:]
            parts[si].replace_records(records);out=b''.join(x.pack() for x in parts)
            absolute=s.offset+len(s.header)+sum(len(r) for r in s.records[:ri])+offset
            assert out==raw[:absolute]+struct.pack('<I',value)+raw[absolute+4:]
            assert b''.join(x.pack() for x in fmt.parse(out))==out
            assert out[:absolute]+raw[absolute:absolute+4]+out[absolute+4:]==raw
            rows.append(dict(source=str(path),source_sha256=sha(raw),record=ri,field_offset=offset,file_offset=absolute,value=value,exact_neighbors_and_inverse=True))
    return dict(exact_DWORD_field_edits=len(rows),edits=rows,source_Vampire_raw_record_bytes=5449,Type224_source_record=[224,21,200,10],headers_unknown_bytes_opaque_WORDs_and_other_sections_preserved=True,source_writes=0)


def build():
    old=load('phase201-prior-package-state.json');assert len(old['components'])==178
    for ref in old['components'].values():assert ref==link(ref['file'])
    assert old['codec']==link('dk2_formats.py')
    for ref in load('phase194-source-baseline.json')['source_files']+load('phase194-source-baseline.json')['executable_paths']:assert sha(Path(ref['file']).read_bytes())==ref['sha256'],ref['file']
    accepted=load('phase201-speed-context-emulations.json');assert len(accepted['replays'])==32
    api_rows=[]
    for c,row in zip(n.cases(),accepted['replays']):
        args,ch,meta,saved,file=n.execute(c);post,out=n.verify(c,args,ch,meta,saved['result']);assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
        d=post[n.p.DEFINITION];a=post[n.p.ACTOR];var=post[n.p.VARS];raw=post[n.p.prior.RAW]
        loaded=tuple(api.load_base_speed(struct.unpack_from('<I',raw,o)[0],20) for o in [0xEB4,0xEB8,0xD37]);assert loaded==struct.unpack_from('<3I',d,0x5B4)
        flags=struct.unpack_from('<I',d,0x686)[0];af=struct.unpack_from('<I',a,0x3E6)[0];af2=struct.unpack_from('<I',a,0x3EA)[0];level=a[0x403]
        terms=api.speed_terms(walk=loaded[0],run=loaded[1],shuffle=loaded[2],mode=c.get('mode',0),definition_HP=struct.unpack_from('<H',d,0x692)[0],definition_flags=flags,current_HP=struct.unpack_from('<I',a,0x40)[0],level=level,
            health_growth_word=struct.unpack_from('<H',var,0xE1+2*level)[0],terrain_flags=struct.unpack_from('<I',post[meta['terrain']],0x17B)[0],terrain_v88_result=api.regular_actor_v88(flags,af),actor_flags=af,actor_flags2=af2,stat21_word=struct.unpack_from('<H',var,0x33D)[0])
        assert all(out[k]==v for k,v in terms.items()),(c['name'],out,terms)
        api_rows.append(dict(case=c['name'],loaded_speeds=list(loaded),terms=terms,native_result=out['result']))
    controls=load('phase201-speed-context-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        args,count,size=cross.build(row['case']);saved=load(row['file']);r=saved['result'];assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert (count,size)==(row['checked_regions'],row['checked_bytes']) and not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    primary=load('phase201-speed-context-primary.json');assert primary['all_executed_original_PCs_covered']
    for row in primary['functions']:
        assert sha((ROOT/row['memory']).read_bytes())==row['memory_sha256'] and sha((ROOT/row['assembly']).read_bytes())==row['assembly_sha256']
    counts=dict(native_compositions=32,Variable_records=sum(r['outcome']['file_records'] for r in accepted['replays']),native_instructions=sum(r['outcome']['native_steps'] for r in accepted['replays']),
        Ghidra_full_context_controls=8,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        public_API_native_matches=len(api_rows),original_PCs=primary['unique_executed_original_PCs'],function_byte_extents=len(primary['functions']),prior_components_preserved=178,source_files_preserved=420,EXE_paths_preserved=7,source_writes=0,new_physical_fields=0)
    outcomes={r['case']['name']:r['outcome'] for r in accepted['replays']}
    return dict(phase=201,date='2026-11-11',status='fresh_definition_and_file_loading_health_surface_speed_composition_verified',profile=primary['profile'],counts=counts,
        extension_of=dict(component='possession_speed_semantics',phase=92,scope='The basic Stat21 formula, other flag branch and mode selection were already confirmed. New evidence composes fresh rawCreature definition loading, actual file/World callbacks, HP/growth/surface branches and speed load boundaries.'),
        chain=dict(Creature_initializer=0x491DC0,MID_initializer=0x568820,raw_converter=0x568E30,definition_loader=0x492320,outer_file_section=0x56CBD0,Variables_parser=0x5716E0,World_callback=0x521790,Type224=0x51DC70,Type65=0x51DC40,possession_setter=0x48DEA0,speed=0x48ABB0,World_vcall1D4=0x508FE0,regular_Actor_vcall88=0x48ED40,stat_getter=0x495080),
        inputs=dict(Type224=dict(record=[224,21,200,10],raw_stat_offset=4,raw_value_offset=8,raw_opaque_offset=12,runtime_WORD=0x6F288D,relative_WORD=0x33D,read_as='low16 read as signed16; high16 of ValueRaw and the opaque lastDWORD do not enter this speed arithmetic, but the completeMID contributes to World checksum'),
            Type65=dict(HP_stat=1,level=2,example_value=125,effect='Writes growth coefficient read by speed inline HP threshold;125 gives maxHP1250 and threshold250 for definitionHP1000.'),
            raw_Creature_fields=dict(walk=0xEB4,run=0xEB8,shuffle=0xD37,Flags=0xEFC,HP=0xF0C),TPS=20,mode_selection='1->run,2->shuffle,allother->walk. All three use Type224 Stat21 in48ABB0, not Stat22.'),
        speed_base_loading=dict(fields=[dict(raw=0xEB4,MID=0x12A4,runtime=0x5B4),dict(raw=0xEB8,MID=0x12A8,runtime=0x5B8),dict(raw=0xD37,MID=0x1127,runtime=0x5BC)],
            exact='q=(u32(raw)//u32(TPS))&FFFFh; if q==0 and raw!=0 store1 else q. Runtime storage is DWORD with this narrowed initial value.',
            native_operation='XOR EDX,EDX; DIV ESI; AND EAX,FFFFh; conditional nonzero-raw minimum1,4924BD..492536',
            examples=[dict(raw=0,tps=20,result=0),dict(raw=1,tps=20,result=1),dict(raw=20*65536,tps=20,result=1),dict(raw=0xffffffff,tps=20,result=52428),dict(raw=2000000000,tps=20,result=57600)],
            scope='Only this original definition-load path. Later stat77 or other runtime assignments are not proven to have the same narrowing. Other TPS/division-fault cases not newly executed; unsigned raw bits differ from historical signed Q12 display.'),
        order=dict(sequence=['select loaded mode speed','conditional20%HP slowdown','conditionalTerrain50%or150%','conditionalType224 Stat21 multiplier'],
            health='If Definition686bit0 clear: maxHP=definitionu16HP692 at level1, otherwise trunc0(s16(growthWORD E1+2*level)*HP/100). threshold=u32(maxHP*20)//100. Compare signed currentHP40 < signed threshold. True ->S=u32(S*20)//100.',
            surface='NativeWorld1D4 reads actual16-byte cell at current Q12XY; cellA chooses definition in6DA4A8. IfActorvcall88==0: TerrainDWORD17Bbit80 ->S=u32(S*50)//100, elsebit100 ->S=u32(S*150)//100.80wins ifboth.',
            final='If ActorDWORD3E6bit80: S=u32(S*(100+s16(Stat21WORD)))//100. Else if ActorDWORD3EAbit8: same numerator//200. Else unchanged. First flag takes priority. Every stage keeps its own integer truncation/wrap.',
            example=dict(walk_base=109,HP=199,definition_HP=1000,Terrain_bit=128,stat21=200,stages=[109,21,10,30],combined_single_division_result=32),
            health_boundary=dict(definition_HP=1000,HP199_result=30,HP200_result=162,condition='strictly below20%, with same Water/Stat21 inputs'),
            growth_example=dict(level=2,Type65_HP_coefficient=125,threshold=250,HP249_result=30,HP250_result=162)),
        flag_boundaries=dict(raw_definition_test=dict(raw_flags_offset=0xEFC,MID_flags_offset=0x12EC,runtime_flags_offset=0x686,mask=1),
            regular_vtable=0x66D524,regular_v88='48ED40 returns true only if Definition686bit0 and Actor3E6bit100. Original code/vtables are executed.',
            observed='Definitionbit0 skips the lowHP branch even with status100clear. The surface branch skips only when actual vcall88 returns nonzero. Four relevant numeric flag combinations tested.',
            naming_limit='No new gameplay name is assigned to bit0/status100 or the other3EA.8 flag. Tests keep a prepared ordinaryCCreature class; natural factory subclass selection for modified rawFlags and other overrides are not claimed.'),
        representative_outcomes={name:outcomes[name] for name in ['wounded-water-rounding','at-health-boundary-water','wounded-lava','water-and-lava','wounded-other-flag','definition-bit0-ground','definition-bit0-status100','large-raw-speed-narrowing','walk-raw-quotient65536-minimum','walk-raw-all-ones','negative-coefficient-wrap','HP-zero-getter-only']},
        physical_preservation=physical(),native_executions=accepted,independent_Ghidra_controls=controls,public_API_matches=api_rows,primary=primary,helpers=[link(x) for x in HELPERS],
        fixture_boundaries=dict(Actor='Verified prior phase90 factory Actor snapshot, not fresh allocation. Level/HP/status inputs prepared, actual48DEA0 setter enables possession. Fresh Creature definition is loaded from a copied real Vampire row with explicit test field edits.',
            World='Original World/Actor vtables retained. Prepared32x32World/cells and Terrain definition flags; real508FE0 and48ED40 execute. Terrain raw file parser and natural surface traversal not composed.',
            files='ActualMyFile/Disc/stream and56CBD0/5716E0/521790 read a synthetic one-section memory file containing real source Stat21 record or controlled variants and optionally Type65. Header callback and bounded Win32 file providers explicitly declared. WholeFileEOF/Level-reference traversal is phase198, not repeated.',
            external_services='Inherited null resource/sound/graphics lookup interfaces and declared file/TLS providers; original game code, IAT and game vtables are not substituted. No real rendering, audio, disk changes or live process injection.',
            numeric_results='Returned internal speed DWORD, not measured tiles/sec or actual movement. HP0 still yields getter30 in preparedcase; this does not prove a dead creature moves.'),
        limitations=['General15-family objective incomplete; no all-Globals closure or percentage increase. Unknown29/186 meanings unchanged.',
            'No whole natural possession/control/AI movement scheduler, release-possession path, all class overrides or creature birth with modifiedFlags.',
            'Type224 Stat1/4/31 and previous scalar98/113/114 work remain separate earlier components; they are not re-proved or newly named here.',
            'Variable77 overrides, arbitrary invalid levels/negative growth, otherTPS and otherEXEs are outside these32 accepted cases.',
            'Global parameters still active. Crypt deferred; ten counter meanings remain open. Creature resistances/flags follow globals.'],
        next=['Remaining global scalar/union consumers and writer paths; keep phase92/93/95/96/101/104 completed work as references.',
            'Creature resistances and flags after remaining global work; Crypt deferred.'])


NOTE='possession_speed_context_semantics phase201 extendsphase92only:32freshCreature+realVariablesfile+48DEA0+48ABB0compositions/8Ghidra. LowHP20%->surface50or150%->Stat21 stages truncate separately;109->21->10->30,strictHP199vs200boundary,Type65HPgrowth125->threshold250. Original loader speeds u32raw/TPS thenlow16 andnonzero minimum1,FFFFFFFF/TPS20->52428. NumericDefinitionbit0/Actor100 boundaries scopedtoordinaryvtable;no newflagname.19inverseedits/API32native;178priorcomponents/codec/420sources/7EXEspreserved,package179,Globalsactive,Cryptdeferred.'


def main():
    package=load('editor-format-package.json');path=ROOT/'phase201-prior-package-state.json'
    if not path.exists():
        assert len(package['components'])==178
        dump(path.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(path.name)
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    s=build();dump(SCHEMA,s);package[KEY]=s;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package);dump('phase201-speed-context-storage.json',s['physical_preservation'])
    dump('phase201-search-status.json',dict(phase=201,goal_status='active',current_block='Global parameters',block_complete=False,counts=s['counts'],schema=link(SCHEMA),next=s['next'],Crypt_deferred=True))
    print(json.dumps(s['counts']));print('physical inverse edits',s['physical_preservation']['exact_DWORD_field_edits'])


if __name__=='__main__':main()
