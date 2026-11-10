"""Publish exact runtime filtering and scoped Trap pointer provenance."""
from pathlib import Path
import json
import struct
from build_effect_semantics import pe_read
from build_trap_readiness import digest
from native_entity_kind_filter import PROFILES

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def primary():
    spec = load('phase69-primary-manifest.json')
    functions,blocks = [],[]
    for r in spec['functions']:
        before,after,memory = [ROOT/'ghidra'/r[k] for k in ['before','after','memory']]
        v = json.loads(memory.read_text())
        b = bytes(v['data'])
        assert before.read_text().rstrip() == after.read_text().rstrip()
        assert int(v['address'],16) == r['address'] and len(b) == v['length'] == r['length']
        assert b == pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        memory = ROOT/'ghidra'/r['memory']
        v = json.loads(memory.read_text())
        b = bytes(v['data'])
        assert int(v['address'],16) == r['address'] and len(b) == v['length'] == r['length']
        assert b == pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        blocks.append({**r,'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    assert struct.unpack('<5I',bytes.fromhex(blocks[0]['hex']))[3] == 0x4A0ACE
    match = load('phase69-kind-filter-match.json')
    assert match['length'] == 91 and len(match['relocations']) == 2
    dx = (ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    old = (ROOT/'source-binaries/DKII-local13.exe').read_bytes()
    expected = bytearray(pe_read(dx,match['DX'],91))
    for r in match['relocations']:
        assert int.from_bytes(expected[r['offset']:r['offset']+4],'little') == r['DX']
        expected[r['offset']:r['offset']+4] = r['old'].to_bytes(4,'little')
    assert bytes(expected) == pe_read(old,match['old'],91)
    assert [r['length'] for r in functions if r['executed']] == [91,91]
    # Primary pointer-provenance instructions, independent of decompiler names.
    assert pe_read(dx,0x4A8F6C,2) == bytes.fromhex('6a03')
    assert pe_read(dx,0x4A8F7D,3) == bytes.fromhex('8b75fc')
    assert pe_read(dx,0x458F99,4) == bytes.fromhex('837d1203')
    assert pe_read(dx,0x4A8F70,1) == b'\xe8'
    assert 0x4A8F75+int.from_bytes(pe_read(dx,0x4A8F71,4),'little',signed=True) == 0x453700
    assert pe_read(dx,0x513744,1) == b'\xe8'
    assert 0x513749+int.from_bytes(pe_read(dx,0x513745,4),'little',signed=True) == 0x4A7E70
    return functions,blocks,match


def link(name):
    return dict(file=name,sha256=digest((ROOT/name).read_bytes()))


def main():
    functions,blocks,match = primary()
    emu = load('phase69-entity-kind-filter-emulations.json')
    assert len(functions) == 9 and sum(r['executed'] for r in functions) == 2 and len(emu['cases']) == 32
    s = dict(phase=69,family='Runtime entity kind/owner list filter and Trap consumer provenance',
        profiles=PROFILES,primary_program_bytes=functions,primary_data_ranges=blocks,whole_function_equivalence=match,
        native_emulations=emu['cases'],native_execution_scope=emu['scope'],
        algorithm=dict(head='Only firstWORD at ECX is read;resolve global table[head]. NextWORD cell-head entry is not searched.',
            next='Follow entityWORD0C through same global tag table until a NULL pointer.',
            predicate='entityDWORD12 == full32 argument kind AND entityWORD28 == low16 argument owner',
            result='First linked-list match:write its pointer through output argument and return1. No match:return0 and preserve prior outputDWORD.',
            caveats='No local tag-validity array,vtable/RTTI validation,owner wildcard or cycle check. Slot0 is read normally;head0 can succeed in a synthetic non-NULL slot0 fixture. This is not proof normal worlds allocate entity0.'),
        pointer_review=dict(
            previously_type_rooted_DX_reads=[0x4A7D58,0x4A9C99],
            newly_type_rooted_DX_reads=[0x4A7DD9,0x4A9163,0x4A8381,0x4A839B],
            direct_kind_guard=dict(function=0x458F60,kind_compare=0x458F99,kind=3,call_this=0x4590B9,call=0x4590BB,target=0x4A7D90,read=0x4A7DD9),
            switch_guard=dict(function=0x4A0930,kind_read=0x4A0992,table=0x4A0E14,case=3,target=0x4A0ACE,call=0x4A0B58,target_function=0x4A7D90),
            iterator=dict(function=0x4A8E70,kind_push=0x4A8F6C,kind=3,filter_call=0x4A8F70,output_to_ESI=0x4A8F7D,definition_read=0x4A9145,cost_read=0x4A9163,
                creator_this=0x4A920E,creator_call=0x4A9210,creator=0x4A80E0,creator_reads=[0x4A8381,0x4A839B]),
            meaning='These six instruction addresses have verified routes from runtime kind3/Trap pointers. DWORD9A is the separately confirmed Trap mana-cost field. This does not prove those machine instructions can never receive another kind through malformed/untyped calls.',
            class_roots=[link('entity-definition-semantics.json'),link('trap-readiness-semantics.json')]),
        untyped_route=dict(handler=0x513730,bytes=33,tag='low16 messageDWORD0',argument='messageDWORD4 passed to4A7E70',
            pointer='global table[tag] passed as ECX without a local validity-array,kind or NULL check',call=0x513744,target=0x4A7E70,read=0x4A7EBF,
            warning='Caller is an untyped command route;upstream producer/dispatch contracts not proved. A deliberate wrong-kind input does not establish a legitimate DoorWORD236 consumer or editor file corruption. Same creator function is reachable from this route,so six typed routes are not global exclusivity proofs.'),
        corpus_hashes=load('trap-writer-semantics.json')['corpus']['files'],
        api=dict(file='native_entity_kind_filter.py',operation='first_match',policy='Read-only runtime fixture projection. Missing entry means NULL table slot. Cycle diagnostic belongs to API,not to native validation. Not a map serializer.'),
        limitations=[
            '32 complete native filter calls,16 per exactEXE;2 executed functions. Seven other complete DX functions statically attested,not counted as executed.',
            'The two91-byte bodies differ only in two verified global tag-table operands. Initial89-byte matching prefix corrected before publication;finalRET immediate bytes now included. Native executions always ran full functions.',
            'Returned Boolean saved separately;outputDWORD compared including unchanged failure valueCAFEBABE. All supplied list-head,table-slot and64-byte entity fixture regions compared unchanged.',
            'No full CTrap tick,creator,network/GUI command dispatch,normal-world entity allocation or gameplay session executed here.',
            'Traps204/Door236 purposes and other requested unknown gameplay fields remain open. Old seven candidate consumer types were not newly reviewed merely because the filter matches between versions.',
            'No new raw patch cases;previous file roundtrip/patch proofs remain separate and are not counted again.',
        ])
    (ROOT/'entity-kind-filter-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    trap = load('trap-semantics.json')
    trap['phase69_entity_filter'] = s
    (ROOT/'trap-semantics.json').write_text(json.dumps(trap,ensure_ascii=False,indent=2),encoding='utf8')
    door = load('door-semantics.json')
    door['phase69_consumer_review'] = {**link('entity-kind-filter-semantics.json'),'scope':s['pointer_review'],'untyped_route':s['untyped_route']}
    (ROOT/'door-semantics.json').write_text(json.dumps(door,ensure_ascii=False,indent=2),encoding='utf8')
    print('Entity filter:32 native calls,2 full91-byte bodies;6 type-rooted DX routes,1 untyped command route;unknown meanings remain open')


if __name__=='__main__':
    main()
