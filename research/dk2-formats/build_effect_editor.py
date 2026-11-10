"""Effects native writer fidelity: opaque tail, flags projection, nonzero overlay."""
from pathlib import Path
import json
from build_effect_semantics import asm_range
from build_phase17_semantics import proof
from effect_writer_stack_audit import inspect
from native_effect_editor import FLAG_MODEL_BITS
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def main():
    result=dict(family='Effects',record_variants=[246],executable_sha256=load('executable-hash-verification.json'),
        flags=dict(raw_offset=0xB4,raw_bytes=4,game_intermediate_offset=0x168,game_runtime_offset=0x5F,
            game_rule='Converter ORs raw flags with intermediate DWORD; loader copies full DWORD when presence4C is set. No editor maskFFF7 in these game fragments.',
            editor_decoder=0x41FF90,editor_encoder=0x41FEC0,editor_callback=0x4A2F50,
            model_fields=[dict(mask=1<<bit,model_offset=0x1F0+4*i) for i,bit in enumerate(FLAG_MODEL_BITS)],
            pure_decode_encode_mask=0xFFF7,
            callback_default='After decoding flags, if both model204(mask40) and model208(mask80) are0, callback calls41FDA0(1), setting model208=1 and model204=0. If either is nonzero it leaves the pair unchanged, including both nonzero.',
            loaded_save_projection='v=rawFlags & FFF7; if !(v &C0): v |=80. Thus raw0 ->80, raw8 ->80, raw40 ->40, rawC0 ->C0, FFFFFFFF ->FFF7.',
            evidence=[asm_range('phase27-effects-flags-decode-assembly.txt',0x41FF90,0x420060),asm_range('phase27-effects-flags-assembly.txt',0x41FEC0,0x41FF81),
                asm_range('phase27-effects-model-assembly.txt',0x4A3267,0x4A328E),asm_range('phase27-effect-generation-default-assembly.txt',0x41FDA0,0x41FDB8),
                asm_range('phase27-effect-writer-assembly.txt',0x4AA322,0x4AA32F),asm_range('phase27-effect-converter-assembly.txt',0x56B18B,0x56B196),
                asm_range('phase27-effect-loader-assembly.txt',0x435B14,0x435B21)]),
        opaque_f4=dict(raw_offset=0xF4,raw_bytes=2,baseline_name='Unknown3',
            game='Full inspected converter56AFE0 handles12 WORDs DA..F0 plus generation/type bytesF2/F3. It does not transfer F4 into intermediate. Four whole-body converter fixtures differing only in F4 have identical complete-intermediate FNV readout and selected field readouts.',
            writer='Writer4AA040 passes246 bytes at frameESP+4C to file vcall38. Assigned fields end at rawF3; no ordinary field store initializes rawF4/F5. The remaining WORD carries preexisting frameESP+140 bytes (entryESP-4).',
            stack_audit=inspect(),
            indirect_write_ranges=[dict(name='GenerateIds',raw_start=0xCA,bytes=16,model_offset=0x1C4),dict(name='12 tail WORDs',raw_start=0xDA,bytes=24,model_offset=0x1D4),
                dict(name='Light',raw_start=0x74,bytes=24,model_offset=0x13C),dict(name='ArtResource',raw_start=0x20,bytes=84,model_offset=0xCC),dict(name='Name',raw_start=0,bytes=32,precondition='CString length<=32')],
            name_helper='4AB3B0 copies CString length without bounding copy to supplied32; then zero-pads only if length<32. Tail claim assumes ordinary name length<=32. Oversized internal CString memory behavior is outside codec projection.',
            header_helper='Earlier4AB320 writes date components only within supplied buffer+0..8, passed at writer frame-header area, separately from rawF4. It does not initialize the record tail.',
            preservation='Keep original opaque WORD in an independent editor. Do not recompute it as a checksum, force it to zero, or intentionally reproduce uninitialized stack content. Current evidence establishes this native writer path, not global absence of every possible consumer.',
            evidence=[asm_range('phase27-effect-writer-assembly.txt',0x4AA2BF,0x4AA36D),asm_range('phase27-effect-converter-assembly.txt',0x56B1CF,0x56B1FF),
                asm_range('phase27-effect-converter-assembly.txt',0x56B086,0x56B08F),asm_range('phase27-effect-converter-assembly.txt',0x56B10D,0x56B116),
                asm_range('phase27-effect-string-writer-assembly.txt',0x4AB3B0,0x4AB3F7),asm_range('phase27-effect-resource-getter-assembly.txt',0x41CE60,0x41CE63),
                asm_range('phase27-header-time-writer-assembly.txt',0x4AB320,0x4AB3A9)]),
        tail_overlay=dict(raw_offset=0xDA,count=12,item_bytes=2,intermediate_offset=0x18E,runtime_offset=0x37,
            rule='For each slot: replace old runtime WORD only when incoming WORD is nonzero. Incoming0 keeps old value. Presence flags are not tested in this loop; this differs from GenerateIds presence-driven copy.',
            native_editor_model_offset=0x1D4,writer_rule='Copies all12 WORDs, including zeros. Game overlay behavior must not be used to skip zero bytes during file serialization.',
            evidence=[asm_range('phase27-effect-loader-assembly.txt',0x435BBA,0x435BDA),asm_range('phase27-effects-model-assembly.txt',0x4A3214,0x4A323E),
                asm_range('phase27-effect-writer-assembly.txt',0x4AA2E0,0x4AA2FF)]),
        native_emulations=load('phase27-effect-editor-emulations.json')['cases'],
        limitations=['Exact supplied EXEs. No real GUI save, live-game rendering or modification of files.',
            'Writer suffix fixtures enter4AA2BF after unrelated scalar/x87 assignments and use an emulator file-capture callback. Original array/light/resource/name code and flags encoder execute. Full record FNV, byte length, tail WORD, flags and last assigned DWORD are checked; skipped fields retain explicit fixture bytes.',
            'Whole converter uses no-op strncpy because raw name and zeroed destination are both32 zero bytes. Other original converter instructions execute; FNV is a fixture checksum, not a game-file checksum or a general collision-free equivalence proof.',
            'Stack CFG audit covers direct ESP stores/PUSHes with known callee cleanup; indirect buffers are separately bounded by copy evidence. Name precondition<=32 and ordinary read-only file write callback are explicit.',
            'The first decompile of41FEC0 declared void and hid returned register computation; discarded as semantic evidence. Full assembly and original PE bytes establish encoder behavior.'])
    assert len(result['native_emulations'])==31
    (ROOT/'effect-editor-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('Effects save: opaque F4 stack bytes, flags projection,12-word overlay;31 native runs')
if __name__=='__main__':main()
