"""Publish verified indexed-pointer origins, keeping Objects34C unresolved."""
from pathlib import Path
import json
import struct
import capstone
from build_effect_semantics import pe_read
from build_trap_readiness import digest

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def primary():
    spec = load('phase70-primary-manifest.json')
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
    data = (ROOT/'source-binaries/DKII-local13.exe').read_bytes()
    assert pe_read(data,0x4FE595,10) == bytes.fromhex('c70548f06c00d8d56d00')
    assert pe_read(data,0x4FEDFA,6) == bytes.fromhex('893d48f06c00')
    for a in [0x4F0068,0x4F01CD]:
        assert pe_read(data,a,3) == bytes.fromhex('83e003')
    for a in [0x4F0072,0x4F01D0]:
        assert pe_read(data,a,3) == bytes.fromhex('83f803')
    # Canonical CEEffectElement getter begins with an operand-size prefix.
    assert pe_read(data,0x42F0F0,7) == bytes.fromhex('668b8106010000')
    return functions,blocks


def link(name):
    return dict(file=name,sha256=digest((ROOT/name).read_bytes()))


def main():
    functions,blocks = primary()
    scan = load('phase70-old-object-field-candidates-v2.json')
    review = load('phase70-indexed-origin-review.json')
    assert len(functions)==11 and len(blocks)==3 and len(review['reads'])==17
    assert all(r['origin_verified_on_reached_states'] for r in review['reads'])
    assert sum(r['states'] for r in review['functions'])==4883 and sum(len(r['indirect_branch_stops']) for r in review['functions'])==0
    assert sum(r['recovered_instruction_count'] for r in review['functions'])==130
    s = dict(phase=70,family='Objects34C indexed consumer search review',executable_sha256=scan['sha256'],
        raw_field=dict(offset=0x34C,bytes=2,intermediate=0x3CC,definition=0x106,gameplay_meaning='unresolved',native_GUI_label='Mana Gen;hidden template control,not gameplay proof'),
        primary_code_extents=functions,primary_code_and_data_ranges=blocks,
        search={**link('phase70-old-object-field-candidates-v2.json'),
            'decoded_instructions':scan['decoded_instructions'],'all_candidates':len(scan['candidates']),
            'new_address_candidates':len(scan['novel_address_candidates']),
            'new_indexed_WORD_leads':17,'new_other_anchored_leads':7,
            'read_policy':scan['policy'],'capstone_version':capstone.__version__,
            'scope':'Updated TEST/CMP read policy,indexed memory operands and displacement-anchored decoding. Unlikephase61,indices are included. Anchored reads remain untrusted until native boundary verification.'},
        indexed_origin_review=review,
        pointer_setup=dict(global_pointer=0x6CF048,target_on_initialization=0x6DD5D8,
            setup_instruction=0x4FE595,reset_instruction=0x4FEDFA,
            observation='Initializer fragment fills nine-WORD rows with100 and sets global pointer to fixed6DD5D8;reset clears pointer. This is a separate global coefficient table,not the Objects catalogue definition pointer.',
            consumer_shape='Reads use signedWORD[table+105+2*index],with nearby byte402 level and definitionWORD698 paths. No new name/units assigned to Objects34C.'),
        computed_branches=dict(function=0x4EFB70,switches=[dict(jump=0x4F007B,table=0x4F0A08,count=4),dict(jump=0x4F01D5,table=0x4F0A18,count=4)],
            finding='Initial Ghidra body omitted switch arms. PE targets plus nativeAND3/CMP3 recover130 reachable instructions;after recovery alias analysis visits4883 states with no unresolved indirect jumps in these contexts.',
            boundary='Last Ghidra instruction is a back-jump4F0A01→4EFC46,not finalRET. Code extent attested explicitly;do not count a missing terminalRET as missing executable bytes.'),
        prefix_rejection=dict(candidate=0x42F0F1,canonical_instruction=0x42F0F0,
            bytes='668b8106010000',finding='Starting one byte late drops66 and invents a DWORD read. Canonical WORD getter is already typed CEEffectElement,not Objects.',previous=link('trap-loading-semantics.json')),
        remaining_anchored_candidates=[r for r in scan['novel_address_candidates'] if r['bytes']!=2 and int(r['address'],16)!=0x42F0F1],
        corpus=load('entity-definition-semantics.json')['corpus'],
        reproducibility=dict(analyzer='review_old_object_indexed.py',primary='phase70-primary-manifest.json',
            boundaries='phase70-indexed-boundaries.json',pointer_writes='phase70-old-growth-pointer-writes.json',
            corrected_hints='The nearest direct-call seed470DB0/49F3A0 did not own4710BA/49FA90. Complete-body checks rejected those hints;verified following entries470E50/49F510 used instead.'),
        limitations=[
            'Static proof only:11 code extents,including10 ordinary full bodies andone reconstructed-switch extent;3 explicit code/data ranges. No new native function executions or gameplay runs.',
            'Alias proof uses ordinary x86 callee-saved ABI;calls invalidate volatile EAX/ECX/EDX. It concerns these17 local consumer paths and the attested initialization contract,not arbitrary corrupted pointers or whole-program non-use.',
            '17 indexed WORD candidates do not establish an Objects106 consumer. Six new non-WORD anchored candidates remain unclassified;historical wider/alias/other-EXE questions remain open.',
            'Global coefficient table context is not a new gameplay interpretation of the hidden Mana Gen editor control. Unknown raw bytes remain preserved.',
            'No new raw patch cases. Existing phase62 13705 patches and84-file roundtrips remain separate prior evidence,not fresh counts.',
        ])
    assert len(s['remaining_anchored_candidates'])==6
    (ROOT/'object-indexed-consumer-review.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    obj = load('object-property-semantics.json')
    obj['phase70_indexed_review'] = s
    (ROOT/'object-property-semantics.json').write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf8')
    print('Objects review:17 indexed roots verified;130 switch instructions recovered;6 anchored candidates remain;34C meaning open')


if __name__=='__main__':
    main()
