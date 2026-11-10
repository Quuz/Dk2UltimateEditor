"""Publish wholefactory->mana chain and bounded unknownObject106 constraints."""
from pathlib import Path
import hashlib
import json
import emulate_factory_mana as native
import crosscheck_factory_mana as cross
from build_effect_semantics import EXE, pe_read
from build_possession_damage import source_executables

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def link(name):
    return dict(file=name, sha256=digest((ROOT/name).read_bytes()))


def primary():
    out, data = {}, EXE.read_bytes()
    for key, rows in load('phase109-primary-manifest.json').items():
        out[key] = []
        for row in rows:
            path = ROOT/'ghidra'/row['memory']
            value = json.loads(path.read_text(encoding='utf8'))
            blob = bytes(value['data'])
            assert int(value['address'], 16) == row['address']
            assert len(blob) == value['length'] == row['length']
            assert blob == pe_read(data, row['address'], row['length'])
            out[key].append(dict(row, evidence_sha256=digest(path.read_bytes()), program_bytes_sha256=digest(blob)))
    assert len(out['functions']) == 6 and sum(r['executed'] for r in out['functions']) == 4
    return out


def replays():
    cases = {c['name']: c for c in native.cases()}
    manifest = load('phase109-factory-mana-emulations.json')
    assert manifest['scope'] == native.__doc__
    rows, visits, total_helpers = [], set(), 0
    for row in manifest['replays']:
        value = load('ghidra/'+row['file'])
        c = cases[row['case']]
        args, expected, wanted, node = native.build(c)
        assert value['case'] == c and value['input'] == args and value['expected'] == expected and value['node'] == node
        assert digest((ROOT/'ghidra'/row['file']).read_bytes()) == row['sha256']
        native.verify(c, args, expected, wanted, node, value['result'])
        memory = {int(r['address'], 16): bytes.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
        assert args['allowed_external_IO_code'] == [[a, len(memory[a])] for a in [native.f.GFX_CB, native.f.SOUND_CB]]
        original_args, _ = native.f.build(native.f.cases()[0], 0)
        original = {int(r['address'], 16): bytes.fromhex(r['hex']) for r in json.loads(original_args['memory'])['regions']}
        assert all(memory[a] == original[a] for a in [native.f.GFX_CB, native.f.SOUND_CB])
        r = value['result']
        outside = {int(a, 16) for a in r['visited_instructions'] if int(a, 16) >= 0x10000000}
        allowed = {p+i for p, n in args['allowed_external_IO_code'] for i in range(n)}
        assert outside <= allowed
        assert r['visited_instructions'][hex(native.f.SOUND_CB)] == 2
        assert r['visited_instructions'][hex(native.f.GFX_CB)] == 1
        total_helpers += r['visited_instructions'].get('0x4c17a0', 0)
        visits.update(r['visited_instructions'])
        rows.append(dict(row, inputs=c, expected=expected, created_node=node,
                         observed_definition_reads=r['observed_reads'], instruction_count=r['instruction_count'],
                         compared_contexts=len(wanted), compared_bytes=sum(map(len, wanted.values()))))
    assert len(rows) == len(cases) == 14 and total_helpers == 26
    return rows, visits, total_helpers


def build():
    p = primary()
    rows, visits, helper_count = replays()
    assert all(hex(r['address']) in visits for r in p['functions'] if r['executed'])
    prior = ROOT/'phase109-prior-components.json'
    if not prior.exists():
        parts = load('editor-format-package.json')['components']
        assert len(parts) == 87
        prior.write_text(json.dumps(parts, ensure_ascii=False, indent=2), encoding='utf8')
    assert len(load(prior.name)) == 87
    controls = load('phase109-factory-mana-crosschecks.json')['replays']
    assert len(controls) == 2
    for row in controls:
        value = load('ghidra/'+row['file'])
        args, expected = cross.build(row['case'])
        assert value['input'] == args and value['expected'] == expected
        assert digest((ROOT/'ghidra'/row['file']).read_bytes()) == row['sha256']
        r = value['result']
        assert r['success'] and r['hit_return']
        assert all(int(r['registers'][k], 16) == v for k, v in dict(EAX=1, EBP=1, EBX=0, ECX=0).items())
    frozen = next(r for r in load('object-lifetime-semantics.json')['helpers'] if r['file'] == 'native_unicorn_fieldtrace.py')
    assert digest((ROOT/frozen['file']).read_bytes()) == frozen['sha256']
    corpus = load('phase108-object-lifetime-api-validation.json')
    for row in corpus['files']:
        assert digest(Path(row['file']).read_bytes()) == row['sha256']
    return dict(phase=109, date='2026-10-29', family='Objects creation/Player mana runtime',
        profile=dict(file=str(EXE), sha256=digest(EXE.read_bytes())), primary=p,
        native_replays=rows, Ghidra_crosschecks=controls,
        helpers=[link(n) for n in ['native_unicorn_factorytrace.py', 'probe_object_factory_reads.py',
                                   'emulate_factory_mana.py', 'crosscheck_factory_mana.py', 'prepare_factory_mana_primary.py']],
        dependencies=[link(n) for n in ['native_unicorn_fieldtrace.py', 'object-lifetime-semantics.json',
                                       'mana-semantics.json', 'emulate_object_factory.py', 'ghidra/phase89-object-factory-base.bin']],
        unchanged_source_executables=source_executables(),
        scope='Originalfull4B7A90/pool/Objectconstructor/registration thenfull4C10E0 andactual4C17A0 inoneCPU memory. '
              'PreparedPlayer/World fields;not allWorld startup orallmana conditions.',
        native_chain=dict(factory=0x4B7A90, player_income=0x4C10E0, actual_cost_helper=0x4C17A0,
                          current_ability_gate=0x4C0090, same_CPU_memory=True),
        income=dict(deadline='unsignednow>=low32(last+TPS);equalityruns,endwritesnowtoPlayer9A.',
                    fields=dict(current=0x82, positive_rate_multiplier=0x86, last=0x9A,
                                aggregate_cost=0x10D6, ceiling=0x10DE, extra=0x10E2,
                                basic=0x10E6, stored_capacity=0x10EA, one_gain=0x10EE, one_loss=0x10F2),
                    formula='On duepathclearone_gain/one_loss;cost=actual4C17A0. gross=min_signed32(s32(extra+basic+cost+new_one_gain),ceiling); '
                            'net=s32(gross-secondactualCost). Multiplyonlypositivenetbyrate,thenapplymode3setting0/2. '
                            'UpdateCurrentwithsignedwrapping;negative-net branch clampsnegativeCurrent0,otherbranch capsCurrentatcapacity.',
                    caveat='Costhelper hasrealpossession/ability/imp gates. Newcases exercise aggregateupkeep withnoactivepossession/ability/imp cost; '
                           'do nottreatitsreturnvalue asconstantinallWorld states.',
                    samples={'current1000_extra20_basic100': 1120, 'positiveRate2': 1240, 'ceiling80': 1080,
                             'current9990_capacity10000': 10000, 'extraMinus150': 950,
                             'extraMinus80_upkeep100_ceiling100': 1000},
                    one_shot='Preparedoldcountervalues25/15arezeroedbeforecostcalculationinthispathanddonotadd/subtractagain. '
                             'Actualoperationscreditingmana elsewhere notnewlyexecuted.'),
        object_unknown=dict(raw_offset=0x34C, runtime_definition_offset=0x106, meaning_resolved=False,
                            tested_values=[0, 100, 65535], observed_reads=0,
                            constraint='Ordinarydefinition/ID17;realfactoryplusownerregistration+selectedfullPlayerincome do notread106/107 '
                                       'orchangeincome fromthosevalues. No globalnonuse claim;otherstates/transitions/versions remainpossible.'),
        external_IO=dict(allowed_code='Exactlythe11byteGFX-nulland14bytefixedsoundproviders inheritedfromphase89.',
                         per_factory='OneGFXqueryreturns0;two soundregistrationcallsreturn13572468. No actualaudio/GPU.',
                         protected='EntireoriginalPEcode andclass/World/graphicsvtables remainunchanged. AnyothernonPEexecutionfatal.',
                         gameplay_function_replacements=0),
        object_creation=dict(prepared='DefinitionHP1000/ID17/position5800h6800h2000h/owner7,pool/tag/tablecontext.',
                             captured='Wholepool,object393bytes,map/owner/count/list/lookupstructures comparetopriorindependentexpectations andnativephase89snapshot.',
                             warning='RuntimeManaGenmodifiedinprepareddefinition,not newrawKWDloadercomposition. No sourcefilewrites.'),
        counts=dict(native_compositions=14, Ghidra_full_compositions=2, created_Objects_in_accepted_runs=16,
                    full_Player_income_calls=16, actual_cost_helper_calls_in_Unicorn=helper_count,
                    selected_primary_whole_functions=6, executed_path_primary_functions=4,
                    source_Object_files_unchanged=82, new_raw_container_edits=0, unknown_semantic_fields_closed=0),
        limitations=['ExactsuppliedDX only;otherversions notnewlyexecuted.',
                     'PreparedWorld/Player anddefinition/pool;not completeWorldstartup orCPlayerconstructor.',
                     'OrdinaryObjectID17/statecontext;nocurrentpossession,CallToArms/abilityorimp upkeep;zeroLowMana threshold.',
                     'Mana0warnings/emergencyunpossession/manyother4C10E0sidepaths notcovered;selectedfullfunctionsreturnnormally.',
                     'Originalcost helper executes;oldarithmeticfragmenttests arehistorical,not recounted asnewcoverage.',
                     'ExplicitexternalnullGFX/fixedsoundadapters;gameplayoriginal,butnotactualfrontend/audio.',
                     'ManaGenunknown:boundedabsenceofreadsdoesnotproveglobalunused oroldversionbehavior.',
                     'No newrawpatch/API orrebuildcount;previous574edits remainhistorical.',
                     'Significantunknownsall15families remainopen;fullgoalnotachieved.'])


def main():
    schema = build()
    (ROOT/'factory-mana-semantics.json').write_text(json.dumps(schema, ensure_ascii=False, indent=2), encoding='utf8')
    print(json.dumps(schema['counts']))


if __name__ == '__main__':
    main()
