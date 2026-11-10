"""Publish Object-specific state42 creation and Terrain-derived vault income."""
import hashlib
import json
import struct
from pathlib import Path
import emulate_vault_creation as creation
import emulate_vault_income as income
import crosscheck_vault_runtime as cross
from build_effect_semantics import EXE,pe_read
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent


def load(name):return json.loads((ROOT/name).read_text(encoding='utf8'))
def digest(blob):return hashlib.sha256(blob).hexdigest()
def link(name):return dict(file=name,sha256=digest((ROOT/name).read_bytes()))


def primary():
    out={};data=EXE.read_bytes()
    for key,rows in load('phase111-primary-manifest.json').items():
        out[key]=[]
        for row in rows:
            path=ROOT/'ghidra'/row['memory'];value=json.loads(path.read_text(encoding='utf8'));blob=bytes(value['data'])
            assert int(value['address'],16)==row['address'] and len(blob)==value['length']==row['length']
            assert blob==pe_read(data,row['address'],row['length'])
            out[key].append(dict(row,evidence_sha256=digest(path.read_bytes()),program_bytes_sha256=digest(blob)))
    assert len(out['functions'])==22 and sum(r['executed'] for r in out['functions'])==21 and len(out['blocks'])==5
    return out


def replay_rows(native,manifest_name,kind):
    cases={c['name']:c for c in native.cases()};manifest=load(manifest_name)
    assert manifest['scope']==native.__doc__ and len(manifest['replays'])==len(cases)
    rows=[];visits=set()
    for row in manifest['replays']:
        value=load('ghidra/'+row['file']);c=cases[row['case']]
        built=native.build(c);args,e,checks,wanted=built[:4]
        assert value['case']==c and value['input']==args and value['expected']==e
        assert digest((ROOT/'ghidra'/row['file']).read_bytes())==row['sha256']
        r=value['result']
        if kind=='creation':
            node=built[4];assert value['node']==node
            native.verify(c,args,e,checks,wanted,node,r)
            mem={int(x['address'],16):bytes.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
            assert args['allowed_external_IO_code']==[[a,len(mem[a])] for a in [native.f.GFX_CB,native.f.SOUND_CB]]
            assert mem[native.f.SOUND_CB][6:11]==b'\xB8'+bytes(4)
            outside={int(a,16) for a in r['visited_instructions'] if int(a,16)>=0x10000000}
            allowed={a+i for a,n in args['allowed_external_IO_code'] for i in range(n)}
            assert outside<=allowed
            assert r['visited_instructions']['0x4b7a90']==1
            assert r['visited_instructions'].get('0x49bd20',0)==c['ticks']
        else:native.verify(c,args,e,checks,wanted,r)
        if kind=='income':
            assert r['visited_instructions']['0x4c10e0']==1
            assert r['visited_instructions']['0x4c17a0']==2
        visits.update(r['visited_instructions'])
        rows.append(dict(row,inputs=c,expected=e,instruction_count=r['instruction_count'],
                         compared_contexts=len(wanted),compared_bytes=sum(map(len,wanted.values()))))
    return rows,visits


def build():
    p=primary()
    creators,cv=replay_rows(creation,'phase111-vault-creation-emulations.json','creation')
    owners,ov=replay_rows(income,'phase111-vault-income-emulations.json','income')
    assert len(creators)==18 and len(owners)==6
    assert sum(r['inputs']['ticks'] for r in creators)==423
    assert sum(r['inputs']['fault'] for r in creators)==1
    assert sum(r['expected']['effect_requests'] for r in creators)==10
    assert sum(r['expected']['original_count_helper_calls'] for r in owners)==6
    assert all(hex(r['address']) in cv|ov for r in p['functions'] if r['executed'])
    controls=load('phase111-vault-runtime-crosschecks.json')['replays']
    assert [(r['kind'],r['case']) for r in controls]==cross.CONTROLS
    for row in controls:
        value=load('ghidra/'+row['file']);args,e=cross.build(row['kind'],row['case'])
        assert value['input']==args and value['expected']==e and not value['tool_is_error']
        assert digest((ROOT/'ghidra'/row['file']).read_bytes())==row['sha256']
        r=value['result'];assert r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    data=EXE.read_bytes();indices=pe_read(data,0x49B57C,100)
    targets=struct.unpack('<6I',pe_read(data,0x49B564,24))
    overrides=[i+31 for i,x in enumerate(indices) if targets[x]==0x49B512]
    assert overrides==[101,108,114]
    old_objects=load('phase110-object-state-api-validation.json')['files']
    for row in old_objects:assert digest(Path(row['file']).read_bytes())==row['sha256']
    terrain_source=next(r for r in load('terrain-catalogue-validation.json')['corpus']['files']
                        if r['file'].lower()==str(income.TERRAIN_FILE).lower())
    assert digest(income.TERRAIN_FILE.read_bytes())==terrain_source['sha256']
    source_objects=[]
    for ident in [101,108,114]:
        raw=creation.probe.states.source_record(ident)
        source_objects.append(dict(id=ident,name=raw[:32].split(b'\0')[0].decode('ascii'),
                                   start_state=raw[0x35B],misc_effect_id=int.from_bytes(raw[0x358:0x35A],'little'),
                                   mana_gen_bits=int.from_bytes(raw[0x34C:0x34E],'little')))
    frozen=next(r for r in load('object-lifetime-semantics.json')['helpers'] if r['file']=='native_unicorn_fieldtrace.py')
    assert digest((ROOT/frozen['file']).read_bytes())==frozen['sha256']
    prior=ROOT/'phase111-prior-components.json'
    if not prior.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==89
        prior.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==89
    return dict(
        phase=111,date='2026-10-29',family='Object idle-effect initialization and Terrain mana ownership',
        profile=dict(file=str(EXE),sha256=digest(data)),primary=p,
        creation_replays=creators,income_replays=owners,Ghidra_crosschecks=controls,
        helpers=[link(n) for n in ['probe_mana_vault_factory.py','emulate_vault_creation.py','emulate_vault_income.py',
                                   'crosscheck_vault_runtime.py','prepare_vault_runtime_primary.py']],
        dependencies=[link(n) for n in ['native_unicorn_factorytrace.py','native_unicorn_fieldtrace.py','native_object_loading.py',
                                       'native_terrain_catalogue.py','object_state_tables.py','emulate_object_factory.py',
                                       'emulate_factory_mana.py','object-state-semantics.json','object-lifetime-semantics.json','terrain-catalogue-validation.json',
                                       'ghidra/phase89-object-factory-base.bin']],
        unchanged_source_executables=source_executables(),
        unchanged_source_data=dict(Object_files=len(old_objects),Object_corpus=link('phase110-object-state-api-validation.json'),
                                   Terrain_file=terrain_source,source_writes=0),
        fresh_creation=dict(
            function=0x49B1C0,raw_start_state=0x35B,definition_start_state=0x11B,
            ordinary='Fresh constructor copies definition StartState directly into activeDWORD13A and clears age13E; it need not wait for inactive-state fallback.',
            Object_ID_overrides=overrides,override_state=42,stored_period_offset=0x14A,
            native_period_label='EffectTriggerRepeatTimer',native_period_label_address=0x69DE84,
            override_rule='After ordinary initialization, IDs101/108/114 execute49EF60(42),clearage,storelow32(2*TPS_at_creation)inDWORD14A.',
            tested_requested_start_states=[0,59,255],ordinary_state16_preserved=True,
            source_objects=source_objects,
            importance='Registered callback address alone is not sufficient state compatibility; both ObjectID-specific initialization and handler preconditions matter.'),
        idle_effect=dict(
            state=42,callback=0x4A3530,accepted_Object_IDs=[101,108,114],
            period='Cached DWORD14A set at creation; unlike state39 this handler does not recalculate period from current TPS.',
            effect_source=dict(raw_offset=0x358,bytes=2,runtime_offset=0x117),
            trigger='Unsigned stateage after increment modulo stored period equals0; reads low16 MiscEffectId and requests even ID0.',
            tested_source_effects=[287,288,310],tested_edited_effects=[0,123],
            placement='Requests use source instanceXYZ and owner; original null CPCEngineInterface90 returns0 in fixtures.',
            failure=dict(case='ordinary-state42-divide-zero',ObjectID=17,raw_StartState=42,
                         reason='Handler leaves divisorECX0 for an unrecognized ObjectID, regardless of cached union data.',
                         address=0x4A3568,instruction='DIV ECX',observed='Unicorn UC_ERR_EXCEPTION withECX0 after full native factory; no live Windows crash.'),
            positive_graphics_return='Effect-object linking after nonzero graphics result remains static-only; actual particle creation/rendering untested.'),
        mana_income=dict(
            source='Terrain ownership contributions, independently of tested Object idle-effect behavior.',
            physical_fields=[dict(raw_offset=0x19C,bytes=2,runtime_offset=0x187,semantic='signed16 ManaGain -> Player10E2'),
                             dict(raw_offset=0x19E,bytes=2,runtime_offset=0x189,semantic='signed16 MaxManaGain -> Player10DE income ceiling')],
            supplied_Terrain31=dict(name='Mana Vault',ManaGain=0,MaxManaGain=0),
            supplied_Terrain32=dict(name='Claimed Vault',ManaGain=100,MaxManaGain=100),
            native_chain=[0x56EB60,0x56ED20,0x4F66E0,0x4559D0,0x4BE070,0x4C10E0,0x4C17A0],
            count_helper='Original4BE070 updates PlayerDWORD_B3F; no historical RET4 count stub used.',
            transfer='Selected8->7 transfer subtracts oldplayer gain/ceiling/count and adds newplayer contributions, preserving owner highnibble.',
            samples='PreparedbaseCurrent1000,extra20,basic100,ceiling500:Terrain31 leads1120;Terrain32 leads1220;edited125/200 leads1245;FFFF/FFF6 leads1119.',
            scope='Raw Terrain load,ownership and complete mana function in sameCPU; World/players prepared; outer CMap replacement not executed.'),
        enclosing_map_route=dict(
            function=0x44FC40,executed=False,evidence=link('ghidra/phase111-vault-44fc40-disassemble_function.txt'),
            native_call_arguments='CMap this inECX; six stack args: X,Y,Terrain selector,owner,flag5,flag6; decompiler inferred prototype is unreliable.',
            mapping=[dict(Terrain_ID=31,Object_ID=101),dict(Terrain_ID=32,Object_ID=108)],
            creation_position='(cellX*4096+2048,cellY*4096+2048,0); native Object factory later handles floor height.',
            call_sites=[0x44FF12,0x44FFB3],
            caution='Only static instructions establish enclosing route. New native factory fixtures used Z2000h; do not claim full natural vault capture or enclosing neighbor/navigation/event processing.'),
        unknown_mana_gen=dict(raw_offset=0x34C,definition_offset=0x106,tested_values=[0,100,65535],meaning_resolved=False,
                              conclusion='Raw-loader/fullfactory/registration/selectedstate42ticks read no106/107; selected Terrain income path explains contributions without assigning this field. No global nonuse claim.'),
        external_IO=dict(graphics_query='Originalphase89 11-byte null query provider; called once per factory.',
                         sound_registration='Explicit14-byte registration provider increments call count and returns0; called twice per factory. Different from historical fixed13572468 return.',
                         periodic_request='Original World/CPCEngine code executes; no synthetic periodic-effect provider.',
                         gameplay_function_replacements=0,unchanged_class_vtables=True),
        counts=dict(native_creation_compositions=18,normal_creation_compositions=17,expected_divide_by_zero=1,
                    full_Object_factories=18,native_Object_tick_entries=423,native_Object_ticks_returned=422,
                    periodic_effect_requests=10,native_raw_Terrain_ownership_income_compositions=6,
                    original_tile_count_helper_calls=6,full_Player_mana_calls=6,actual_cost_helper_calls=12,
                    Ghidra_controls=4,selected_whole_primary_functions=22,executed_primary_functions=21,
                    primary_data_blocks=5,source_Object_files_unchanged=82,source_Terrain_files_unchanged=1,
                    new_raw_container_rebuilds=0,newly_named_unknown_raw_fields=0),
        limitations=[
            'Exact supplied DX; old/RC3 or runtime-patched state tables not newly tested.',
            'Two separate native compositions: Object creation/idle-effect and Terrain ownership/income. No unified full CMap capture event claimed.',
            'Factory fixtures use prepared World/pool/map/players and null visual/sound resources; creationXYZ5800h6800h2000h is not natural CMap spawnZ0.',
            'Loading gate1 skips resource/sound/checksum refresh; scalar conversions and loaders execute original code and all prepared buffers compare.',
            'External graphics query and null sound registration providers are explicit. Periodic requests reach original null graphics implementation; no visible effect created.',
            'Income fixtures disable mode4 side effects/active possession/ability/imp costs as historicalpreparedPlayer scope; native tile-count helper now runs unmodified.',
            'One expected CPU divide fault is counted separately from17normallyreturning creation runs; no game process or live Windows crash.',
            'No new raw API/container tests; previous738exact StartState rebuilds remain historical and are not recounted.',
            'ManaGen and significant unknown fields across all15 families remain open; goal incomplete.',
        ])


def main():
    value=build();(ROOT/'vault-runtime-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(value['counts']))


if __name__=='__main__':main()
