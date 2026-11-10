"""Integrate verified table-index behavior; unknown29/186 meanings stay open."""
from copy import deepcopy
from pathlib import Path
import json,struct
import emulate_global_table_indices as n
import crosscheck_global_table_indices as cross
import trace_global_parameter_aliases as aliases
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha
KEY,SCHEMA='global_table_index_semantics','global-table-index-semantics.json'
link=lambda name:dict(file=name,sha256=sha((ROOT/name).read_bytes()))
canonical=lambda x:sha(json.dumps(x,sort_keys=True,ensure_ascii=False).encode())
HELPERS=['emulate_global_table_indices.py','crosscheck_global_table_indices.py','trace_global_parameter_aliases.py','probe_global_stat_getter.py','prepare_global_table_indices_primary.py','build_global_table_indices.py','audit_global_table_indices.py','emulate_variables_reader.py','native_unicorn_mapreader.py','native_variables_file_api.py']


def physical():
    raw=n.old.GLOBAL.read_bytes();parts=n.old.fmt.parse(raw);assert b''.join(s.pack() for s in parts)==raw
    target=next((i,s) for i,s in enumerate(parts) if s.signature==230);si,s=target
    groups={tag:[(i,struct.unpack('<4I',r)) for i,r in enumerate(s.records) if int.from_bytes(r[:4],'little')==tag] for tag in [65,224]}
    assert {(r[1],r[3]) for _,r in groups[65]}=={(stat,level) for stat in range(32) for level in range(2,11)} and len(groups[65])==288
    assert {r[1] for _,r in groups[224]}==set(range(32)) and len(groups[224])==32
    edits=[]
    for tag in [65,224]:
        ri,_=groups[tag][0]
        for field in [4,8,12]:
            off=s.offset+len(s.header)+16*ri+field
            for value in [0,31,0x80000000,0xffffffff]:
                p=n.old.fmt.parse(raw);records=p[si].records.copy();r=records[ri];records[ri]=r[:field]+struct.pack('<I',value)+r[field+4:];p[si].replace_records(records)
                changed=b''.join(x.pack() for x in p);assert changed==raw[:off]+struct.pack('<I',value)+raw[off+4:]
                assert b''.join(x.pack() for x in n.old.fmt.parse(changed))==changed
                assert changed[:off]+raw[off:off+4]+changed[off+4:]==raw
                edits.append(dict(type=tag,record=ri,field_offset=field,file_offset=off,value=value,exact_neighbors_and_inverse=True))
    return dict(source=dict(file=str(n.old.GLOBAL),sha256=sha(raw)),source_growth_records=288,source_growth_matrix='StatId0..31 x Level2..10, exact32x9',source_possession_records=32,source_possession_stat_ids=list(range(32)),
        full_container_and_all_other_sections_roundtrip=True,exact_DWORD_field_edits=len(edits),edits=edits,unknown_bytes_preserved=True,source_writes=0)


def build():
    old=load('phase200-prior-package-state.json');assert len(old['components'])==177
    for r in old['components'].values():assert r==link(r['file'])
    assert old['codec']==link('dk2_formats.py')
    base=load('phase194-source-baseline.json')
    for r in base['source_files']+base['executable_paths']:assert sha(Path(r['file']).read_bytes())==r['sha256'],r['file']
    native=load('phase200-global-index-emulations.json');assert len(native['replays'])==19
    for c,row in zip(n.cases(),native['replays']):
        args,ch,saved,file=n.execute(c);_,out=n.verify(c,args,ch,saved['result']);assert saved['result']['source_exe_sha256']==sha(n.EXE.read_bytes())
        assert row==dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out)
    controls=load('phase200-global-index-crosschecks.json')['replays'];assert [x['case'] for x in controls]==cross.CONTROLS
    for row in controls:
        args,count,size=cross.build(row['case']);saved=load(row['file']);r=saved['result'];assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert (count,size)==(row['checked_regions'],row['checked_bytes']) and not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    getter=load('phase200-stat-getter-native.json');assert getter['native_calls']==512 and getter['unknown29_186_reads']==0 and getter['whole_prepared_table_preserved']
    getter_controls=[]
    for q in load('phase200-stat-getter-queries.json'):
        path='ghidra/'+q['id']+'.txt';r=load(path);r=r.get('data',r);arg=int(q['id'].rsplit('-',1)[1],16);i=arg&255
        expected=((100+i if i<31 else -7) if i<32 else 100)&0xffffffff
        assert r['success'] and r['hit_return'] and int(r['registers']['EAX'],16)==expected
        getter_controls.append(dict(argument=arg,result_bits=expected,evidence=link(path)))
    search=load('phase200-global-alias-search.json');assert search['counts']==dict(seeds=171,functions=109,states=3345,bounded_scenes=0,reads=183,overlap_candidates=21,exact_overlap_candidates=0,pointer_forwards=0,pointer_escapes=0)
    assert all(x['type']==186 for x in search['field_overlap_candidates'])
    assert search['primary_functions']==[r for r,_ in aliases.primary()]
    primary=load('phase200-global-index-primary.json');assert primary['all_executed_original_PCs_covered']
    for row in primary['functions']+primary['alias_search_functions']:
        assert sha((ROOT/row['memory']).read_bytes())==row['memory_sha256'] and sha((ROOT/row['assembly']).read_bytes())==row['assembly_sha256']
    candidates=[]
    for row in search['field_overlap_candidates']:
        if row['access']==0x495093:
            disposition='rejected_by_full495080_lowbyte<32_guard;512native/5Ghidra controls'
        else:
            solutions=row['match']['solutions'];assert all(x['value']>10 for x in solutions)
            disposition='out_of_ordinary_growth_level_range_address_candidate; not observed ordinary gameplay or intended186 consumer'
        candidates.append(dict(access=row['access'],function=row['function'],type=186,instruction=row['instruction'],required_indices=[x['value'] for x in row['match']['solutions']],disposition=disposition))
    counts=dict(real_file_reader_scenes=19,records_applied=sum(r['outcome']['records_applied'] for r in native['replays']),native_reader_instructions=sum(r['outcome']['native_steps'] for r in native['replays']),
        Ghidra_full_context_controls=6,Ghidra_compared_regions=sum(r['checked_regions'] for r in controls),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in controls),
        native_whole_getter_calls=512,getter_instructions=getter['native_instructions'],Ghidra_getter_return_controls=5,
        original_PCs=primary['unique_executed_original_PCs'],executed_function_byte_extents=len(primary['functions']),static_alias_functions=109,static_seed_producers=171,static_states=3345,
        static_exact29_186_reads=0,static_index_candidates=21,getter_guard_rejections=1,out_of_ordinary_level_address_candidates=20,
        source_files_preserved=420,EXE_paths_preserved=7,prior_components_preserved=177,source_writes=0,new_physical_fields=0)
    return dict(phase=200,date='2026-11-11',status='unchecked_Variables65_224_table_writes_and_bounded_alias_search_verified',profile=primary['profile'],counts=counts,
        original_chain=dict(section_dispatch=0x56CBD0,Variables_reader=0x5716E0,World_callback=0x521790,growth_handler=0x51DC40,possession_handler=0x51DC70,possession_getter=0x495080),
        records=dict(physical_bytes=16,MID_prefix_bytes=16,Type65=dict(fields=['Type:u32','StatId:u32','ValueRaw:u32','CreatureLevel:u32'],
            exact_destination='u32(0x6F261F + low32(2*low32(9*StatId+CreatureLevel)))',relative_destination='u32(0xCF + low32(2*low32(9*StatId+CreatureLevel)))',write_bytes=2,stored_value='low16(ValueRaw)',index_validation_in_handler=False),
            Type224=dict(fields=['Type:u32','StatId:u32','ValueRaw:u32','Unused:u32'],exact_destination='u32(0x6F2863 + low32(2*StatId))',relative_destination='u32(0x313+low32(2*StatId))',write_bytes=2,stored_value='low16(ValueRaw)',index_validation_in_handler=False,last_payload_DWORD_ignored_by_51DC70=True,World_checksum_note='521790 signed-byte checksum still includes the complete32-byteMID, including this otherwise unused payloadDWORD.')),
        intended_table_projection=dict(growth='Source matrix32statistics x9levels2..10,18bytes per statistic, valid source WORD interval relative D3..312 inclusive. Level1 uses definition values in inspected consumers.',
            possession='32sourceWORDs at313..352 inclusive. Whole495080 accepts lowbyte argument<32, reads signedWORD; otherwise returns100.',
            scope='These are observed source dimensions and confirmed consumers; no claim that arbitrary internal creature levels are universally clamped. Existing48B120 compares exactlylevel10, not >=10.'),
        native_effects=dict(first_possession_collision=['Type65 Stat31/Level11 -> relative313, same WORD asType224 Stat0.','Type65 Stat32/Level2 -> same313.','Type65 Stat31/Level10 ->311, last source growthWORD.'],
            order='65(31,Value150,Level11) then224(0,Value200) ->200; reverse ->150. Real file reader/World callbacks accept all records and return1.',
            wrap=['Type65 Stat0/LevelFFFFFFD1 ->relative71, writes lowWORD of unknown29.','Type224 StatFFFFFEAF ->relative71, likewise.','Type224 Stat80000000 aliasesStat0 after32bit index arithmetic.'],
            unknown186='Type65 Stat0/Levels421/422/423 andType224 Stats131/132/133 writeWORDs at419/41B/41D, partly overwriting relative41A DWORD unknown186. ValuesBEEF yield556677BE,55BEEF88,EF667788 from55667788.',
            interpretation='Out-of-table indexing can change unrelated parameters. This is not a gameplay meaning of unknown29/186 or a valid mapping of those scalar types to growth/possession.'),
        alias_search=dict(evidence=link('phase200-global-alias-search.json'),counts=search['counts'],source_candidates=link('phase200-global-alias-seeds.json'),
            established='All170shared-pointer loads have matching Ghidra READ xrefs; one directbase producer.109wholefunctionbyteextents, including19owners initially missing fromGhidra. Source-map names/bounds were only hints for missing entries, not semantic evidence.',
            candidate_dispositions=candidates,unknown29='No remaining overlap candidate after explicitFF masks, including pre-seed514B05 proven on unique predecessor chain.',
            unknown186='One stat getter candidate rejected by its actualAL<32 guard;20growth candidates require indices160..252, outside observed2..10 matrix. These are static possible addresses under abnormal inputs, not completed ordinary consumers.',
            limits=search['limits']+['One explicit write-only REP STOSD initializer stop remains; loops/calls/flags not promoted to an unbounded whole-program absence proof.','Shared-base seed set does not cover arbitrary forged addresses or every conceivable pointer construction. Unknown29/186 purpose remains unresolved.']),
        native_file_executions=native,independent_Ghidra_full_context_controls=controls,whole_stat_getter=dict(native=link('phase200-stat-getter-native.json'),controls=getter_controls),primary=primary,
        physical_preservation=physical(),helpers=[link(x) for x in HELPERS],
        editor_guidance=['Keep physical StatId/Level/ValueRaw asu32 and all16rawbytes, including Type224 unusedDWORD and high16ValueRaw. Native WORD write does not justify deleting highbits in the file.',
            'For ordinary creation/editing use source-supported StatId0..31 and growthLevel2..10; represent Level1 base stats separately. Diagnose out-of-matrix records and their actual effects; do not silently clamp, drop or reinterpret preserved inputs.',
            'Keep growth and possession matrices distinct. Their adjacent runtime addresses permit malformed65 records to overwrite224 coefficients.',
            'Record application order remains meaningful. Original load success does not validate indices or intended table membership.'],
        limitations=['No whole liveWorld/GUI/normal scheduler, all union handlers, other EXEs, exception/disk recovery or arbitrary invalid-index safety claimed.',
            'Prepared World registration/definition state and virtual file providers inherited fromphase197; real original file/stream/section/World/handler functions execute. No IAT or original function substitution.',
            'Static alias search does not decode unknown29/186 meanings; no overall completion percentage increase.',
            'Global parameters remain active and incomplete. Crypt deferred, ten counter meanings open. Creature resistances/flags follow globals.'],
        next=['Trace other unresolved global parameters and input/consumer boundaries; do not re-label29/186 from out-of-bounds aliases.','Remaining Variables union writer paths and creature resistances/flags in the user priority order.'])


NOTE='global_table_index_semantics phase200:19realVariablesfileloads/6fullcontextGhidra;unchecked65/224indexarithmeticwrites16bits,65Stat31Level11overwrites224Stat0;orderlastwins,wrapcanwrite29/186butdoesnotdecodetheirpurpose.512whole495080calls/5GhidrareturncontrolsproveAL<32guard;171baseseeds/109byteextents/3345staticstates:0exact29/186reads,20abnormalleveladdresscandidatesaftergetterrejection.24rawDWORDinverseedits;177priorcomponents/codec/420sources/7EXEspreserved,package178,Globalsactive,Cryptdeferred.'


def main():
    package=load('editor-format-package.json');path=ROOT/'phase200-prior-package-state.json'
    if not path.exists():
        assert len(package['components'])==177
        dump(path.name,dict(components=deepcopy(package['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),preserved_top_level={k:canonical(v) for k,v in package.items() if k not in ['components','precedence',KEY]}))
    old=load(path.name)
    for k,h in old['preserved_top_level'].items():assert canonical(package[k])==h
    assert set(package['components']) in [set(old['components']),set(old['components'])|{KEY}]
    schema=build();dump(SCHEMA,schema);package[KEY]=schema;package['components'][KEY]=link(SCHEMA)
    if NOTE not in package['precedence']:package['precedence'].append(NOTE)
    dump('editor-format-package.json',package);dump('phase200-global-index-storage.json',schema['physical_preservation'])
    dump('phase200-search-status.json',dict(phase=200,goal_status='active',current_block='Global parameters',block_complete=False,counts=schema['counts'],schema=link(SCHEMA),next=schema['next'],Crypt_deferred=True))
    print(json.dumps(schema['counts']));print('physical inverse edits',schema['physical_preservation']['exact_DWORD_field_edits'])


if __name__=='__main__':main()
