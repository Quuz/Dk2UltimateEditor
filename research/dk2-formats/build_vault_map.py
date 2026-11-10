"""Publish complete prepared CMap replacement with old/new Objects and mana."""
import hashlib
import json
import struct
from pathlib import Path
import emulate_vault_map as native
import crosscheck_vault_map as cross
from build_effect_semantics import EXE,pe_read
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent


def load(name):return json.loads((ROOT/name).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(name):return dict(file=name,sha256=digest((ROOT/name).read_bytes()))


def build():
    data=EXE.read_bytes();primary={}
    for key,rows in load('phase112-primary-manifest.json').items():
        primary[key]=[]
        for row in rows:
            p=ROOT/'ghidra'/row['memory'];value=json.loads(p.read_text(encoding='utf8'));blob=bytes(value['data'])
            assert value['length']==len(blob)==row['length'] and int(value['address'],16)==row['address']
            assert blob==pe_read(data,row['address'],row['length'])
            primary[key].append(dict(row,evidence_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(blob)))
    assert len(primary['functions'])==24 and len(primary['blocks'])==4
    cases={c['name']:c for c in native.cases()};manifest=load('phase112-vault-map-emulations.json')
    assert manifest['scope']==native.__doc__ and len(manifest['replays'])==len(cases)==7
    rows=[];visits={};total_factories=total_count_helpers=total_RTTI=total_cost_helpers=0
    for row in manifest['replays']:
        value=load('ghidra/'+row['file']);c=cases[row['case']];args,e,checks,wanted=native.build(c)
        assert value['case']==c and value['input']==args and value['expected']==e
        assert row['sha256']==digest((ROOT/'ghidra'/row['file']).read_bytes())
        r=value['result'];native.verify(c,args,e,checks,wanted,r)
        total_factories+=r['visited_instructions']['0x4b7a90'];total_cost_helpers+=r['visited_instructions']['0x4c17a0']
        total_count_helpers+=r['visited_instructions'].get('0x4be070',0)
        total_RTTI+=e['factories']
        assert r['visited_instructions'].get(hex(native.probe.imported.ADDRESS),0)==e['factories']
        for a,n in r['visited_instructions'].items():visits[a]=visits.get(a,0)+n
        other_cap=native.mana.s32(int.from_bytes(wanted[native.OTHER][0x10DE:0x10E2],'little'))
        rows.append(dict(row,inputs=c,expected=dict(e,other_player_ceiling=other_cap),
                         compared_regions=len(wanted),compared_bytes=sum(map(len,wanted.values())),
                         instruction_count=r['instruction_count']))
    assert all(hex(r['address']) in visits for r in primary['functions'])
    controls=load('phase112-vault-map-crosschecks.json')['replays'];assert [r['case'] for r in controls]==cross.CONTROLS
    for row in controls:
        v=load('ghidra/'+row['file']);args,e=cross.build(row['case'])
        assert v['input']==args and v['expected']==e and not v['tool_is_error']
        assert row['sha256']==digest((ROOT/'ghidra'/row['file']).read_bytes())
        r=v['result'];assert r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==n for k,n in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    source_objects=load('phase110-object-state-api-validation.json')['files']
    for row in source_objects:assert digest(Path(row['file']).read_bytes())==row['sha256']
    terrain=native.probe.ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Terrain.kwd'
    source_terrain=next(r for r in load('terrain-catalogue-validation.json')['corpus']['files'] if r['file'].lower()==str(terrain).lower())
    assert digest(terrain.read_bytes())==source_terrain['sha256']
    frozen=load('vault-runtime-semantics.json')
    for row in frozen['helpers']+frozen['dependencies']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    p=ROOT/'phase112-prior-components.json'
    if not p.exists():
        prior=load('editor-format-package.json')['components'];assert len(prior)==90
        p.write_text(json.dumps(prior,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(p.name))==90
    return dict(
        phase=112,date='2026-10-29',family='CMap Vault terrain replacement / Object lifetime staging / mana aggregation',
        profile=dict(path=str(EXE),sha256=digest(data)),primary=primary,
        native_replays=rows,Ghidra_crosschecks=controls,
        helpers=[link(n) for n in ['probe_vault_map.py','emulate_vault_map.py','native_unicorn_declared_imports.py',
                                   'crosscheck_vault_map.py','prepare_vault_map_primary.py']],
        dependencies=[link(n) for n in ['vault-runtime-semantics.json','native_unicorn_factorytrace.py',
            'fixture_read_probe.py','probe_mana_vault_factory.py','emulate_object_factory.py','native_object_loading.py',
            'native_terrain_catalogue.py','emulate_factory_mana.py','emulate_navigation_map.py',
            'object_state_tables.py','ghidra/phase89-object-factory-base.bin']],
        unchanged_source_executables=source_executables(),unchanged_sources=dict(Object_files=len(source_objects),
            Object_manifest=link('phase110-object-state-api-validation.json'),Terrain=source_terrain,source_writes=0),
        enclosing_call=dict(function=0x44FC40,this='World+9DA embedded CMap',
            stack_arguments=['X','Y','TerrainID','requested_owner_tag','flag5','flag6'],
            checked_XY=[5,6],World7239=0,flags5and6=[0,0],loading_flag40_used=False,
            scene='Prepared32x32 runtime cells; ground-to-ground navigation category. Old tile and old Object exist before replacement in six cases.',
            distinction='Complete44FC40 from native entry to return plus full mana executes; higher-level player interaction/AI capture caller and full World scheduler do not.'),
        creation=dict(mapping=[dict(TerrainID=31,ObjectID=101),dict(TerrainID=32,ObjectID=108)],
            constructor_state=42,period=40,TPS=20,
            original_requested_XYZ=[0x5800,0x6800,0],final_XYZ=[0x5800,0x6800,0x1000],
            floor_scope='Original floor/RTTI path in prepared ground cells; not arbitrary terrain height.',
            ownership='Terrain32 retains requested owner7; Terrain31 uses runtime WORD6F2548, prepared as owner tag8. Tags7/8 are runtime fixtures, not raw KLD owner BYTE values.'),
        old_object=dict(find=0x4537E0,virtual_slot=0x20,actual_function=0x4B2AD0,
            native_effect='Store DWORD40 HP=0 only. New object allocated separately; old one retains its original type, tag, state and registrations at return.',
            examples='Old101/tag100/HP0 and new108/tag101/HP1000 remain in same cell; reverse108->101 also checked.',
            caveat='Not immediate deletion or an in-place ObjectID conversion. Subsequent oldObject tick/destructor/World scheduler not run.'),
        terrain_mana_aggregation=dict(type_change=0x455B90,owner_change=0x4559D0,tile_count_helper=0x4BE070,
            fields=[dict(raw_offset=0x19C,size=2,runtime_offset=0x187,meaning='signed16 income contribution'),
                    dict(raw_offset=0x19E,size=2,runtime_offset=0x189,meaning='signed16 income-ceiling contribution')],
            type_change_rule='For prepared oldownerA: A.income += newGain-oldGain; A.ceiling unchanged.',
            transfer_rule='If actualownerB differs: subtract newGain/newCeiling from A and add them to B; decrement/increment tile counts. If same owner with flag80 clear, this owner-change stage returns unchanged.',
            consequence='Cannot replace this route with subtract-old-ceiling/add-new-ceiling. Old31(0,0)->new32(100,100),8->7 gives oldowner ceiling500->400; same owner7 leaves its ceiling80 unchanged and mana becomes1080.',
            status='Exact prepared ordinary-mode path. Not a demonstrated global gameplay defect, designer intent or all future World recomputation behavior.'),
        unresolved=dict(ObjectManaGen=dict(raw=0x34C,runtime=0x106,resolved=False,
            check='FFFF in both loaded Object records leaves selected replacement/income outcome unchanged; loaded definition field not read on these paths. No global-unused claim.'),
            Terrain198224='No new meaning established. No reads overlap their loaded183/1B5 fields in these selected ground-route cases.'),
        external_import=dict(name='IsBadReadPtr',IAT=0x66C1B8,target=0x28CFA4,IAT_patches=0,
            policy='Existing outside-image import target executes declared adapter: complete read span must fit a declared/readable-PE range; zero length allowed; 32bit overflow rejected.',
            other_external='Exact null graphics query11bytes and null sound registration14bytes; original PE RTTI/gameplay code retained.',
            zero_page='Only four-byte FS exception-chain accesses at address0 accepted; reads at NULL+19A from an invalid owner are not accepted.'),
        discarded_diagnostics=dict(files='phase112-map-discovery-* and phase112-z0-discovery-* not accepted manifests',
            problems=['Missing queue World backpointer stopped first fixture.',
                      'Assigning Player pointer to tag0 broke NULL-terminated entity traversal; discarded.',
                      'Oldowner0 route read NULL+19A inside mapped zero page; excluded, not a live-game crash.',
                      'Initial expected allocator insertion direction was wrong; corrected to actual tail insertion, all full contexts compared.']),
        counts=dict(native_complete_map_chains=7,full_Object_factories=total_factories,old_HP0_requests=6,
                    full_Player_mana_calls=7,actual_cost_helper_calls=total_cost_helpers,
                    actual_tile_count_helper_calls=total_count_helpers,bounded_import_calls=total_RTTI,
                    Ghidra_controls=len(controls),whole_primary_functions=24,primary_data_blocks=4,
                    new_raw_container_rebuilds=0,newly_named_unknown_fields=0),
        limitations=['Runtime scene/players/pools prepared; no raw KLD map loader or full World startup.',
                     'Full44FC40 selected ground cases; heavy navigation rebuild, room/wall/bridge branches and flags5/6 nonzero not tested.',
                     'AI-status recipients skip UI queue; no sound output, renderer, particles or user notification proved.',
                     'Old HP0 remains registered; scheduler deletion not executed.',
                     'No full player interaction/capture event or living Windows game session.',
                     'All fifteen file families remain in goal scope; major unknown raw fields remain unresolved.'])


def main():
    s=build();(ROOT/'vault-map-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    print('published draft schema',s['counts'])


if __name__=='__main__':main()
