"""Publish native Threat distribution and exact Crypt modifier dependencies."""
from pathlib import Path
import hashlib
import json
import struct
import emulate_crypt_threat as native
from build_effect_semantics import EXE, pe_read
from audit_targeted_spell import executable_ranges
from build_possession_damage import source_executables

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def link(name):
    return dict(file=name, sha256=digest((ROOT/name).read_bytes()))


def primary():
    data, output = EXE.read_bytes(), {}
    for key, rows in load('phase106-primary-manifest.json').items():
        output[key] = []
        for row in rows:
            path = ROOT/'ghidra'/row['memory']
            value = json.loads(path.read_text(encoding='utf8'))
            blob = bytes(value['data'])
            assert int(value['address'], 16) == row['address']
            assert len(blob) == value['length'] == row['length']
            assert blob == pe_read(data, row['address'], row['length'])
            output[key].append(dict(row, evidence_sha256=digest(path.read_bytes()), code_or_data_sha256=digest(blob)))
    assert len(output['functions']) == 12 and sum(r['executed'] for r in output['functions']) == 9
    assert len(output['blocks']) == 5
    # Actual source register dependency, not a decompiler naming inference.
    assert pe_read(data, 0x45E638, 6) == bytes.fromhex('8a9e8e100000')
    assert pe_read(data, 0x45E667, 6) == bytes.fromhex('8a868f100000')
    assert pe_read(data, 0x45E671, 3) == bytes.fromhex('0fbec3')
    assert b'CryptPowersPeopleActive' in pe_read(data, 0x69F578, 30)
    return output


def replays():
    cases = {c['name']: c for c in native.cases()}
    manifest = load('phase106-threat-emulations.json')
    assert manifest['scope'] == native.__doc__
    protected = executable_ranges(EXE.read_bytes()) + [(0x66D524, 0x130), (0x66E3EC, 0x3A0),
                (0x4D9274, 180), (0x4D9328, 180)]
    rows = []
    for row in manifest['replays']:
        path = ROOT/'ghidra'/row['file']
        value = json.loads(path.read_text(encoding='utf8'))
        c = cases[row['case']]
        args, expected = native.build(c)
        assert value['case'] == c and value['input'] == args and value['expected'] == expected
        assert digest(path.read_bytes()) == row['sha256']
        result = value['result']
        assert result['success'] and result['hit_return']
        assert all(int(result['registers'][k], 16) == v for k, v in
                   dict(EAX=1, EBP=1, EBX=0, ECX=0, EDX=expected['outcome']['targets']['101']['value']).items())
        memory = {int(r['address'], 16): bytes.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
        assert all(a+len(b) <= p or a >= p+n for a, b in memory.items() for p, n in protected)
        for a in [native.cf.ACTOR, native.TARGET, native.TARGET2]:
            assert memory[a][:4] == struct.pack('<I', 0x66D524)
        checked = {int(r['address'], 16): r['bytes'] for r in expected['checks']}
        assert all(checked[a] == 1343+256 for a in [native.cf.ACTOR, native.TARGET, native.TARGET2])
        assert checked[native.sp.VARS] == 4096
        assert all(a in checked for a in [native.f.WORLD, native.f.PLAYER, native.TARGET_OWNER,
                                         native.cf.DEFINITION, native.sp.RAW, 0x6ED540, 0x6EE540])
        rows.append(dict(row, case_inputs=c, expected=expected))
    assert len(rows) == len(cases) == 39
    assert len(list((ROOT/'ghidra').glob('phase106-threat-*-??????????.json'))) == 39
    return rows


def build():
    p, rows = primary(), replays()
    prior = ROOT/'phase106-prior-components.json'
    if not prior.exists():
        previous = load('editor-format-package.json')['components']
        assert len(previous) == 84
        prior.write_text(json.dumps(previous, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(prior.name)) == 84
    api = load('phase106-threat-api-validation.json')
    assert api['source_files'] == 82 and api['exact_container_rebuilds'] == 328
    for row in api['files']:
        assert digest(Path(row['file']).read_bytes()) == row['sha256']
    old = load('room-species-bonus-semantics.json')
    groups = {r['owner_byte_offset']: r for r in old['species_groups']}
    assert groups[0x108D]['creature_ids'] == [45] and groups[0x108E]['creature_ids'] == [8] and groups[0x108F]['creature_ids'] == [42]
    label = next(r for r in load('creature-editor-scalars.json')['fields'] if r['raw_offset'] == 0xF12)
    assert label['native_label'] == 'Threat' and label['raw_bytes'] == 2
    stats = next(r for r in load('creature-stat-runtime-map.json') if r['stat_id'] == 3)
    assert stats['runtime_offset'] == '0x698' and stats['creature_record_offset'] == '0xf12'
    outcomes = {r['case']: r['expected']['outcome'] for r in rows}
    cases = native.cases()
    return dict(phase=106, date='2026-10-29', family='Creatures/GlobalVariables/Rooms',
        profile=dict(file=str(EXE), sha256=digest(EXE.read_bytes())),
        primary=p, native_replays=rows, outcomes=outcomes, corpus=api,
        unchanged_source_executables=source_executables(),
        helpers=[link(n) for n in ['native_threat.py', 'emulate_crypt_threat.py', 'validate_threat_api.py',
                                   'prepare_threat_primary.py', 'scan_crypt_consumers.py']],
        dependencies=[link(n) for n in ['creature-editor-scalars.json', 'creature-stat-runtime-map.json',
                                       'creature-definition-semantics.json', 'room-species-bonus-semantics.json',
                                       'melee-attack-semantics.json', 'emulate_possession_speed.py',
                                       'native_room_counters.py', 'ghidra/phase90-creature-factory-base.bin']],
        editable_field=dict(raw_Creature_offset=0xF12, bytes=2, interpretation='unsigned16 Threat',
                            native_editor_property_id=label['property_id'], native_editor_label=label['native_label'],
                            intermediate_offset=0x1302, definition_offset=0x698,
                            lineage='Prior verified nativeconverter/complete loader;new nativecomposition preparesruntime definition.',
                            level_coefficients='GlobalVariables65/Stat3 DWORD8 ->low16 atVariables105+2*level,readsigned16;level1 bypasses.'),
        native_chain=dict(callback65=0x51DC40, full_distribution=0x45E4F0, direct_add=0x45C5D0,
                          source_state_gate=0x48F350, owner_reference_point=0x4C5F50, visibility=0x4512F0,
                          room_setter=0x4D8E60),
        source_distribution=dict(gates='Return without change forstate209/255/256/257 ornonzeroWORD44B.',
            level='Level1 usesunsigned baseWORD698;otherwise signed32 low32(signed16 coefficient*base)/100 towardzero.',
            slots=dict(special=0x502, lower_start=0x504, upper_start=0x514, words_per_array=8),
            denominator='Counts every nonzero slot before existence/kind/visibility checks;duplicateIDs countandreapply;no deduplication.',
            formula='DividebaseAfterLevel bynonzeroSlotCount towardzero;then applyCrypt108E andconditional108F withseparate signed32/100 truncation.',
            recipients='Each activeCreature passingoriginal4512F0 receives the adjustedvalue. Newfixtures sameXY:earlyvisibilitysuccess.',
            previous_accumulator='Before distribution:sourceWORD526=low16(sourceWORD524*min(10,Chebyshev(tileXY,ownerWORD_B43/B45))/10);sourceWORD524=0.',
            owner_reference='4C5F50 returnsowner+B43. Exactmeaning/sourceofthatreferencepoint notnewlyestablished;do not labelasheartlocation solelyfromfixture.'),
        crypt_effects=[dict(species=8, current_name=groups[0x108E]['current_names'][0], owner_byte=0x108E,
                            side='emitter owner', condition='signedcount>0',
                            formula='D=trunc0(s32(low32((100+30*count)*D))/100)', ordinary_percent_per_count=30),
                       dict(species=42, current_name=groups[0x108F]['current_names'][0], owner_byte=0x108F,
                            side='emitter owner', condition='signed108F>0', coefficient_source=0x108E,
                            formula='D=trunc0(s32(low32((100+40*signed108E)*D))/100)',
                            caveat='108F isonlygate;not40percent timesUniqueVampirecount. Exactregisterdependencyconfirmed.',
                            native_instruction_dependency=['45E638:MOVBL,[ESI+108E]', '45E667:MOVAL,[ESI+108F]', '45E671:MOVSX EAX,BL']),
                       dict(species=45, current_name=groups[0x108D]['current_names'][0], owner_byte=0x108D,
                            side='recipient owner', condition='signedcount>0',
                            formula='D=trunc0(s32(low32((10-count)*D*10))/100)',
                            ordinary_reduction_percent_per_count=10, clamp=False)],
        accumulator=dict(value_WORD=0x524, source_tag_WORD=0x533, threshold_WORD=0x535,
                         add='WORD524=(old+low16(adjustedDelta))&FFFF;not saturating.',
                         source_update='If signed32 adjustedDelta>unsigned WORD535:setWORD533=low16sourceTag. Strict>,doesnot compareaccumulatedtotal.',
                         diagnostic='maiden10 yields0;maiden11 turns100 into-10 andinitial0 accumulatorinto65526,no localclamp. '
                                    'Thisisnot proof ofglobalfearlessnessorintendedbalancedbehavior.'),
        verified_examples=dict(base100=100, vampire1=130, unique1_alone=100, unique3_alone=100,
                               vampire1_unique1=182, vampire1_unique3=182, vampire2_unique1=288,
                               maiden1=90, both1_maiden1=163, level2=125,
                               duplicate_three_slots_accumulate=99, inactive_slot_leaves_valid_target=50),
        scan=dict(evidence=link('phase106-crypt-consumer-leads.json'),
                  classified_indexed_108A_reads=['4BCB60 checksum loopover20bytes', '4BCF10 nativeCryptPowersPeopleActive diagnosticloopover20bytes'],
                  static_candidates=['488930/owner108C speed-like vslot90→8C;owner1094→actor3B3;wholefunction notnewlyexecuted.'],
                  scope='Linearoperand leads pluswholefunctions forselectedreceivers. Noabsence/globalnonuse claim;20diagnosticbytesdonot mean20mappedCrypt speciesgroups.'),
        counts=dict(native_cases=39, raw_callbacks=39, complete_distribution_calls=sum(c['mode'] == 'distribute' for c in cases),
                    complete_direct_add_calls=sum(c['mode'] == 'add' for c in cases),
                    crypt_compositions=sum('crypt_species' in c for c in cases),
                    full_code_spans=12, executed_path_code_spans=9, PE_blocks=5,
                    raw_source_files=82, raw_record_identity_views=3734,
                    exact_container_rebuilds=328, changed_containers=246, identity_containers=82,
                    API_refusals=5, crypt_groups_assigned_total=6, crypt_groups_remaining=12),
        limitations=['Exact suppliedDX only;otherversions notnewlyexecuted.',
                     'Prepared post-factory source/twoCreaturetargets/owner/world. No new fullrawCreatureloader,factoryorworldtick.',
                     'Type65/stat3 recordcallback executes;wholeVariablescontainer parser notnewlyexecuted.',
                     'SameXY targets exercise originalvisibility earlysuccess;wall/door/interveningmap branches notcoveredhere.',
                     'Crypt room/minimalmember prepared;actualsetter executes,butautomaticentry/exit/fullroomscene notexecuted.',
                     'Threataccumulationisconfirmed;subsequentfear/retreatAIactions andglobalbehavior notexecuted.',
                     'UniqueVampire108F gate/reused108E multiplier isobservednativebehavior,not a proof ofdesignerintent;do not silentlyfixKWD.',
                     'Maiden11/WORDoverflow arediagnostics,not recommendedUIranges orproofofvalidnormalgameplay.',
                     'Indexedchecksum/diagnosticreads do notclose remaining12group meanings;static488930 leads remainopen.',
                     'Jump/Priority/scalar113114 andother majorunknownsall15families keepgoalopen.'])


def main():
    schema = build()
    (ROOT/'crypt-threat-semantics.json').write_text(json.dumps(schema, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(schema['counts']))


if __name__ == '__main__':
    main()
