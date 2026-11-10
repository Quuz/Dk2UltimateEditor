"""RC3 ActionPoint control bindings and bounded native load/write verification."""
from pathlib import Path
import json
from build_effect_semantics import pe_read,asm_range
from build_keeper_pipeline import digest
from native_rc3_action_points import MASKS,CONTROL_IDS
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def primary():
    rows=[]
    for r in load('phase51-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];m=ROOT/'ghidra'/r['memory'];assert p.read_text().rstrip()==a.read_text().rstrip()
        v=json.loads(m.read_text());b=bytes(v['data']);assert len(b)==v['length']==r['length'] and int(v['address'],16)==r['address'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        rows.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(b)})
    return rows
def dialogs():
    rs=load('phase49-editor-dialog-comparison.json');out={}
    for suffix,key in [('pro.exe','pro'),('rc3.exe','rc3')]:
        row=next(v for v in rs if v['file'].endswith(suffix));assert digest(Path(row['file']).read_bytes())==row['sha256'];out[key]=dict(file=row['file'],sha256=row['sha256'],dialog=next(d for d in row['dialogs'] if d['resource_path'][1]==278))
    return out
def main():
    ds=dialogs();rc={c['id']:c for c in ds['rc3']['dialog']['controls']};pro={c['id']:c for c in ds['pro']['dialog']['controls']}
    assert 1835 not in pro and rc[1835]['title']=='&Hero Gate' and rc[1835]['template_visible'] and not rc[1835]['template_disabled']
    bindings=[dict(control_id=n,native_title=rc[n]['title'],mask=m,panel_dword_offset=0x68+4*i,model_byte_offset=0x38,raw_flags_word_offset=18,setter=0x425DB0+32*i) for i,(n,m) in enumerate(zip(CONTROL_IDS,MASKS))]
    result=dict(family='RC3 ActionPoint panel and HeroGate08 native label',tag=197,canonical_payload_bytes=56,profiles=ds,
        precedence='Adds RC3-specific UI binding to phase33/34 Pro evidence. Hero Gate is now an established native editor label for AP08; its gameplay algorithm remains unresolved. Do not substitute RC3 panel offsets into Pro.',
        primary_program_bytes=primary(),resource_evidence=dict(file='phase49-editor-dialog-comparison.json',sha256=digest((ROOT/'phase49-editor-dialog-comparison.json').read_bytes())),
        native_emulations=load('phase51-rc3-ap-emulations.json')['cases'],flag_bindings=bindings,
        addresses=dict(parser=0x421FD0,initializer=0x421050,converter=0x4210C0,load_callback=0x496FD0,packed_getter=0x425E50,packed_setter=0x425E70,fill=0x432860,ddx=0x432A00,apply=0x432D30,things_writer=0x499DD0,array_getter=0x490150),
        panel=dict(model_pointer=0x7C,wait_dword=0x60,next_index_dword=0x64,next_array_pointer=0x84,editable_mask=0x79,preserved_byte_mask=0x86,checkbox_truth='Apply compares each DWORD to1; SETZ passes a lowBYTE0/1 to original setters. Direct setters inspect lowBYTE!=0, not wholeDWORD!=0.',
            fill='Verified fragment432974..4329BE extracts waitWORD36 and five single flags into five panelDWORDs. The rest of full432860 including MFC/window setup was not executed.',
            apply='Full432D30 changes only nextBYTE34,waitWORD36,flagsBYTE38 in complete60-byte model comparisons; UpdateData/modal-close are fixtures. Wait low16 copied. Next selection indexing has no local range guard; diagnostic API rejects invalid indices.',
            wait_ui='DDX432A00 passes0..32767 for control1837. Full DDX caller verified with helper fixtures capturing all five checkbox(control,pointer)pairs; no real window opened.'),
        disk_path=dict(raw_wait_word=16,raw_flags_word=18,mid_packed_dword=52,mid_presence_dword=32,model_wait_word=54,model_flags_byte=56,
            parsing='30 full421FD0 cases compare all92 callback bytes. Four raw flag patterns across payload sizes0/16/18/19/20/24/56 plus radius/extreme-coordinate cases. Actual421050/4210C0 run; file I/O and callback are fixtures. Native bounded truncation zero-fills missing raw bytes, not an instruction to extend source files.',
            loading='24 full496FD0 callback executions compare all60 model bytes. Allocator,model constructor,CString/context/registration helpers are fixtures. All coordinate/ID/next assignments, presence branch and real425E70 execute; wait+low8 flags load only when mid20 is nonzero. No full constructor/resource claim.',
            legacy='Callback clears HeroLair01 when unsigned map+60<=2 AND map+64<40. Twelve cases exercise2/39 vs2/40 with presence on/off. HeroGate08 survives both tested branches. Version field names beyond these native comparisons are not inferred.',
            codec='425E70 writes WORD36 from low16 and BYTE38 from bits16..23;425E50 returns zero-extended(waitWORD | flagsBYTE<<16). Twelve full paired-codec cases compare all60 bytes and EAX. Original raw flags bits8..15 are not represented by this model.',
            writer='32 full499DD0 executions,2 APs each:4 initial flag bytes ×4 next arrays ×2 stack seeds. Compare both complete56-byte payloads, write count and model flag bytes. Real490150/425E50 run; date/getter dependencies outside AP path and file I/O are fixtures. WholeThingsheader/otherrecordtypes not certified.',
            writer_flags='If any AP nextBYTE34 is nonzero, writer ORs04 into each AP modelBYTE38 and serializes it. Existing08 remains; zero links do not clear preexisting04. Getter yields only8 flags bits, so upper raw flags byte is zeroed by native serialization. Raw name32 was deliberately seeded and preserved under the supplied virtual trigger/I/O fixtures; this does not verify native CString/name serialization.'),
        api=dict(file='native_rc3_action_points.py',operations=['panel_fields','apply_panel','patch_hero_gate'],policy='patch_hero_gate changes only08 in physically present rawFlagsWORD18, preserves the upper raw flags byte, allotherbits, original record length, name/tail and container headers. It does not apply native writer normalization.'),
        gameplay=dict(status='Native editor label and raw bit established; actual game algorithm for08 unresolved',known='Separate phase33 runtime loader retains08 in runtimeAP32 via4B0B10; phase36 lightningimmunity is a different Creature field.',negative_search='Two bounded current-DX scans found no verified AP08 effect consumer. Stack-slot mask matches excluded. Absence from these scans does not prove unused or unsupported.',distinct_entity='Do not equate AP08 with placed HeroGate object/entity behavior.'),
        evidence=[asm_range('phase51-rc3-432a00-disassemble_function.txt',0x432A1C,0x432A94),asm_range('phase51-rc3-432d30-disassemble_function.txt',0x432D30,0x432DB8),asm_range('phase51-rc3-496fd0-disassemble_function.txt',0x497118,0x497146),asm_range('phase51-rc3-499dd0-disassemble_function.txt',0x49A51D,0x49A56B)],
        other_rc3_leads=dict(plinth='RC3 dialog334 exists, but no on-diskCE binding or payload schema established. Resource/constant searches are discovery only. Immediate14E often CB_SETCURSEL, CE also internal class enum; neither proves Things type. Player parser comparisonCD is a205-byte stride, not a Things dispatcher limit.'),
        limitations=['156 cases are separate native function/fragment checks, not a single end-to-end GUI load/edit/save sequence. No liveGUI,registry,game process or original data writes.',
            'Nine fills are scoped fragments;36 applies,one DDX,12 direct setters,12 codecs,32 writers,24 loader callbacks and30 parsers have the boundaries and fixtures described above.',
            'HeroGate is a native label, not yet a verified hero-spawn mechanism. AP04 gameplay meaning and other unresolved catalogue fields stay open.',
            'Research corpus uses phase47 independent snapshots where provided; does not claim all live AutoSave inputs unchanged.'])
    assert len(result['native_emulations'])==156
    (ROOT/'rc3-action-point-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('RC3 AP:156 native cases;',len(result['primary_program_bytes']),'PE ranges')
if __name__=='__main__':main()
