"""Bind AP editor controls to native setters and preserve UI/file distinctions."""
from pathlib import Path
import hashlib,json
from build_effect_semantics import pe_read
from build_placed_creatures import whole
ROOT=Path(__file__).resolve().parent

def load(name):return json.loads((ROOT/name).read_text(encoding='utf8'))

def primary():
    out=[]
    for r in load('phase34-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory']
        assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==r['address'] and m['length']==r['length']==len(b)
        assert b==pe_read((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':hashlib.sha256(before.read_bytes()).hexdigest(),'memory_sha256':hashlib.sha256(memory.read_bytes()).hexdigest(),'program_bytes_sha256':hashlib.sha256(b).hexdigest()})
    return out

def main():
    resources=load('editor-dialogs.json');dialog=next(d for d in resources['dialogs'] if d['resource_path']==[5,278,2057]);controls={c['id']:c for c in dialog['controls']}
    assert int(controls[1449]['style'],16)&0x800 # ES_READONLY
    bindings=[]
    for control,field,mask,setter in [(1836,0x68,16,0x41B930),(1840,0x6C,1,0x41B950),(1841,0x70,32,0x41B970),(1842,0x74,64,0x41B990)]:
        bindings.append(dict(control_id=control,native_title=controls[control]['title'],panel_dword_offset=hex(field),model_byte_offset='0x38',raw_flags_word_offset='0x12',mask=mask,setter=hex(setter)))
    result=dict(family='Things ActionPoint editor panel',tag=197,canonical_payload_bytes=56,
        precedence='Extends action_point_semantics with proven control-to-bit bindings and UI command limits; raw field widths and phase33 gameplay results remain unchanged.',
        executable_sha256=load('executable-hash-verification.json'),
        native_addresses=dict(constructor='0x4322b0',fill='0x4323a0',ddx='0x432530',apply='0x432850',build_choices='0x4325c0',find_choice='0x432820',auto_id='0x41b840',array_getter='0x48a910'),
        dialog=dict(resource_path=dialog['resource_path'],title=dialog['title'],resource_bytes=dialog['resource_bytes'],source_file='editor-dialogs.json',source_sha256=hashlib.sha256((ROOT/'editor-dialogs.json').read_bytes()).hexdigest(),controls=[controls[n] for n in [1449,1837,1838,1453,2140,1836,1840,1841,1842]]),
        flag_bindings=bindings,
        panel_apply=dict(editable_mask=0x71,preserved_byte_mask=0x8E,
            checkbox_truth='Each panel DWORD must equal1 to set its bit; any other DWORD clears that bit. Setters themselves accept nonzero byte arguments, but apply normalizes via SETZ before calling them.',
            writes='Only next BYTE model34, wait WORD model36 and flags BYTE model38 are assigned in the inspected apply function after UpdateData. Model ID1C, coordinates24..33, trigger and remaining model bytes remain untouched in full60-byte comparisons with UI helpers stubbed.',
            unknown_flags='Bits04/08 are outside this panel mask and survive apply. This does not establish their gameplay meaning or exclude writers/other commands changing them. Phase33 writer can OR04 into all AP models when any next link is nonzero.'),
        wait_delay=dict(raw_offset=16,raw_bits=16,panel_dword_offset='0x60',control_id=1837,spinner_id=1838,
            ui_min=0,ui_max=32767,range_proof='DDX432530 passes0/0x7FFF to4E4B5D; fill4323A0 passes the same limits via SendMessage0x465 to spinner1838.',
            storage='Apply copies low16 of panel DWORD to model WORD36. A direct unvalidated DWORD fixture verifies truncation; it does not prove out-of-range input is accepted by the real GUI.',
            units='Phase34 establishes the UI range only. Game duration is low16(rawWait*ticksPerSecond), as separately proven in action_point_semantics.'),
        identifier=dict(raw_offset=22,raw_bits=8,control_id=1449,panel_dword_offset='0x5c',readonly_template=True,
            auto_min=1,auto_max=80,algorithm='41B840 selects first unoccupied unsigned byte in1..80 by scanning actual AP array. IDs0 and81..255 do not consume these candidate values. On exhaustion AL=0 and existing modelID remains unchanged.',
            name='On success stores chosen ID in model1C then formats Action_Point_%u through CString helpers. String helpers are stubbed in emulation; ID scan and actual48A910 array access execute.',
            policy='1..80 is the native auto-assignment command range, not the physical byte width or a proven game loader maximum. Preserve raw IDs81..255 and0; do not clamp on file load/save.'),
        next_waypoint=dict(raw_offset=23,raw_bits=8,control_id=1453,label_id=2140,panel_index_dword='0x64',choice_array_pointer='0x80',allocated_array_count='0x84',
            choices='One initial <none> maps to byte0; then AP IDs in source array order excluding every AP whose ID equals current ID. Exclusion compares IDs, not object identity. Duplicate IDs are not deduplicated.',
            allocated_tail='Array count equals total AP count; visible list may be shorter when multiple APs share current ID. Controlled native fixtures confirm unused bytes stay zero in freshly initialized storage. Diagnostic next_choices returns logical visible entries without that tail.',
            lookup='432820 scans allocated byte array and returns first matching index; if no match returns0. Thus a missing or excluded self-link maps to <none>. Apply stores selected byte from array without a local bounds check; bounded diagnostic API rejects invalid indices.',
            single='Fill disables combo1453 and label2140 when totalAPcount==1. No real window state is exercised in emulation.',
            policy='Preserve original next byte unless the user explicitly applies a property edit. Applying the native panel can change a missing/self target to0; that is a UI projection, not a lossless repair.'),
        evidence=[whole(r['before']) for r in load('phase34-primary-manifest.json')],
        primary_program_bytes=primary(),native_emulations=load('phase34-ap-editor-emulations.json')['cases'],
        limitations=['Exact inspected Editor Pro executable; no actual GUI save or modified gameplay run.',
            'Seven fill fragments execute4324B4..4324F2; MFC dialog initialization is not run in full.',
            'Seventeen apply tests read output fields; three additional complete apply tests compare all60 model bytes. UpdateData and modal-close helpers are explicit fixtures; wait validation bounds are static native evidence.',
            'Two full DDX callers execute with DDX helper fixtures; one records control IDs, one compares allfour(controlID,fieldPointer) pairs byte-for-byte.',
            'Five full auto-ID functions execute with original bounded array getter; map parent and CString helpers are fixtures.',
            'Eight full choice builders:four use preallocated zeroed arrays and array-reset/zeroing fixtures;four start with null array pointer, allocate via a cdecl fixture and execute actual zero helper403CA0 over A5-filled storage. Array-reset, CString, GetDlgItem and SendMessage are fixtures; actual allocator/reallocator and GUI labels are not simulated. All choice bytes compared, including duplicate-current-ID tail.',
            'Four full choice-index lookups cover none, duplicate first match and missing target. No validity guarantee for malformed AP models.',
            'Unknown04/08 gameplay meanings, WanderRadius effect and unrelated unresolved catalogue fields remain open.'])
    (ROOT/'action-point-editor-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('AP editor:',len(result['native_emulations']),'native runs;',len(result['primary_program_bytes']),'live program ranges match PE')

if __name__=='__main__':main()
