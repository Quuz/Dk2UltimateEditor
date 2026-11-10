"""Verified placed Trap/Door behavior, distinct from catalogue definitions."""
from pathlib import Path
import json
from build_effect_semantics import asm_range
from build_phase17_semantics import proof
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def main():
    result=dict(family='Things',executable_sha256=load('executable-hash-verification.json'),
        envelope='u32 tag,u32 payloadBytes,payload. All offsets below are relative to payload, not envelope.',
        trap=dict(tag=195,payload_bytes=16,converter=0x56F690,callback=0x520E50,
            physical_fields=dict(x=dict(offset=0,bytes=4),y=dict(offset=4,bytes=4),opaque_position_word=dict(offset=8,bytes=4),
                activation_selector=dict(offset=12,bytes=1,baseline_name='NumberOfShots'),trap_id=dict(offset=13,bytes=1),player_id=dict(offset=14,bytes=1),opaque_flags=dict(offset=15,bytes=1)),
            storage='Converter copies position XYZ DWORDs into intermediate14/18/1C; activation byteC ->20, trap IDD ->21, playerE ->22; opaque flagsF ORed into23.',
            position='Callback uses only X/Y for placement: low32(rawTileBits<<12)+800 modulo2^32. Z passed to creator is0, independent of raw+8. Raw+8 is still copied and contributes to preceding intermediate checksum path; do not call globally unused.',
            selector_rule='If raw byteC!=0, calls4A96D0 with created instance asECX and no numeric quantity argument. All1..255 choose this same branch. Raw0 requests manufacture task insertion5066E0(type1) and adds entity to companion list505680.',
            activation='Original4A96D0 clears instance10C bit20; calls4A9F10 refresh; selects4A9720 if definition+BD==3, otherwise4A9A00; then4C1B60 upkeep. This does not initialize remaining shot count from placement byte.',
            task_scope='Manufacture request can be satisfied/rejected by actual task helper (limits/existing crates etc.). API models branch/request, not guaranteed task insertion.',
            opaque_flags='RawF reaches intermediate23 but no semantic read of that byte occurs in inspected placement callback. Preserve all8 bits; checksum path is separate.',
            evidence=[asm_range('phase29-thing-trap-converter-assembly.txt',0x56F690,0x56F6DE),asm_range('phase29-thing-trap-assembly.txt',0x520E50,0x520F3E),
                asm_range('phase29-thing-trap-activate-assembly.txt',0x4A96D0,0x4A9711)]),
        door=dict(tag=196,payload_bytes=20,converter=0x56F600,callback=0x520F40,raw_flag_offset=16,raw_flag_bytes=1,raw_flag_kind='flags8',baseline_kind='enum8',
            masks=[dict(mask=1,name='Locked request',native_effect='calls4A6800(3) after blueprint setter'),dict(mask=2,name='Blueprint',native_effect='sets instance DWORD109 to1; manufacture branch follows if nonzero')],
            storage='RawXYZ0/4/8 ->intermediate14/18/1C; raw triggerC WORD->20, door IDE->22, ownerF->23, raw flag10 ORed into24. Raw tail11..13 not read by converter.',
            position='Callback centers X/Y in Q12 and passesZ0; no use of intermediate1C for placement. Orientation is derived from door definition/map neighbors, not this raw+8 word.',
            rule='First blueprint=(flags>>1)&1 is passed to4A66C0, storing instance109. Then if flags&1, calls4A6800(3). State helper immediately skips when instance109!=0. Thus combined flags3 are representable; blueprint blocks this lock-state write.',
            lock_state='With blueprint0 and bit1 request, setter writes instance111=3 and updates resource/navigation state. Absence of bit1 does not itself call an unlock setter here.',
            game_unknown_bits='Inspected callback flags branch reads only low2 bits. Higher raw bits stay preserved in file and intermediate, without an assigned gameplay meaning here.',
            native_editor=dict(decoder=0x41ED30,encoder=0x41ED10,locked_model_offset=0x1C,blueprint_model_offset=0x20,projection='After conversion to two modelBOOLs, re-encoding returns low8(raw)&3. Encoder only definesAL; upper EAX not a semantic result.',
                evidence=[asm_range('phase29-door-flags-decoder-assembly.txt',0x41ED30,0x41ED49),asm_range('phase29-door-flags-encoder-assembly.txt',0x41ED10,0x41ED24),
                    asm_range('phase29-things-writer.txt',0x494CCE,0x494CE8)]),
            evidence=[asm_range('phase29-thing-door-converter-assembly.txt',0x56F600,0x56F64A),asm_range('phase29-thing-door-assembly.txt',0x521116,0x521141),
                asm_range('phase29-thing-door-blueprint-assembly.txt',0x4A66C0,0x4A66E4),asm_range('phase29-thing-door-state-assembly.txt',0x4A6800,0x4A689A)]),
        editor_zeroed_position=dict(writer=0x4947C0,rule='Editor writer explicitly writes raw+8 DWORD0 for both Door andTrap payloads. This is serialization behavior, not evidence all source raw words should be normalized.',
            evidence=[asm_range('phase29-things-writer.txt',0x494CAA,0x494CB1),asm_range('phase29-things-writer.txt',0x494D74,0x494D90)]),
        subsequent_work='Placed Objects callback520BB0 and converter56F730 were collected as next evidence, but this component establishes only Trap/Door rules.',
        native_emulations=load('phase29-placed-emulations.json')['cases'],
        limitations=['Exact inspected EXEs only. No complete level load, real GUI, navigation simulation, audio, renderer or modified gameplay run.',
            'Eight original full Trap callbacks use explicit creation/checksum/manufacture/neighbor/upkeep fixtures. Actual4A96D0 activation body executes; downstream refresh/neighbor scanning and mana contribution are stubbed. Asserted outcome is branch choice and selected state/position fields, not a full entity.',
            'Seven Door runs start after creation at flag assignment, execute original4A66C0 and4A6800, and use explicit resource/nav providers with skipped navigation/visual refresh helpers. Candidate orientation and creation/task branches are not emulated.',
            'Seven original editor decoder/encoder runs and six converter runs verify physical fields. Unknown flags/padding and raw+8 remain losslessly preserved by independent codec.'])
    assert len(result['native_emulations'])==28
    (ROOT/'placed-structure-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('Things Trap/Door:activation selector,bit flags,raw position preservation;28 native runs')
if __name__=='__main__':main()
