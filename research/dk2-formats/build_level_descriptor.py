"""Evidence for Level DF scratch gaps, native strings and available-ID writes."""
from pathlib import Path
import hashlib,json
from build_effect_semantics import pe_read
from build_placed_creatures import whole
from native_level_descriptor import *
ROOT=Path(__file__).resolve().parent
def load(name):return json.loads((ROOT/name).read_text(encoding='utf8'))

def primary():
    out=[]
    for r in load('phase35-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==r['address'] and m['length']==r['length']==len(b)
        exe='Dungeon Keeper 2/'+('DKII-DX.exe' if r['program']=='DKII-DX.exe' else 'Data/editor/DK2 Editor pro.exe');assert b==pe_read((ROOT.parent.parent/exe).read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':hashlib.sha256(before.read_bytes()).hexdigest(),'memory_sha256':hashlib.sha256(memory.read_bytes()).hexdigest(),'program_bytes_sha256':hashlib.sha256(b).hexdigest()})
    return out

def main():
    result=dict(family='Level descriptor',signature=220,descriptor_tag=223,modern_payload_bytes=SIZE,intermediate_bytes=MID_SIZE,intermediate_payload_base=MID_BASE,
        executable_sha256=load('executable-hash-verification.json'),native_addresses=dict(writer='0x493200',game_converter='0x56c100',editor_converter='0x42bde0',editor_widen='0x48cf90',editor_ascii_copy='0x4ab3b0',stack_probe='0x4ad490'),
        opaque_fields=[dict(raw_offset=off,bytes=n,baseline_alias=name,converter='Both game/editor converters leave corresponding intermediate bytes unchanged; raw input range is not copied.',writer='No assignment in controlled bounded-string writer runs; prior scratch bytes are emitted, not calculated from a history object.') for (off,n),name in zip(OPAQUE,['UnknownHistory','Unknown63C1'])],
        unassigned_unique_byte=dict(raw_offset=0x63CA,bytes=1,baseline_alias='UniqueCreatureTypeId',
            game_and_editor_converter='Raw byte IS copied to intermediate64FE; unlike opaque520 and opaque2, it is not skipped.',
            writer='493200 does not assign this byte in controlled runs; emits scratch byte. No gameplay meaning of baseline alias proven by this phase.'),
        converter=dict(presence_dwords=77,fields='Known modern fields copied to intermediate+134 at corresponding raw offsets, with two opaque holes preserved from incoming intermediate seed. FlagsWORD uses OR with preexisting intermediate flags, not assignment.',
            strings='5 leading fixed UTF16 fields,20 messages512WCHAR and HeroName32WCHAR use bounded wide strncpy/padding and force finalWORD0. SoundCategory/TerrainPath32char use bounded strncpy and force finalbyte0.',
            physical_policy='Raw strings, trailing bytes and opaque ranges remain authoritative for lossless serialization. Native converter normalization is separate.'),
        writer=dict(payload_stack_start_entry_sp=-0x6460,body_sp_entry_sp=-0x6510,
            scratch_fields=[dict(raw_offset=o,bytes=n,entry_sp_start=-0x6460+o,entry_sp_end=-0x6460+o+n) for o,n in WRITER_UNASSIGNED],
            condition='Unassigned-gap result is for valid model pointers and strings that stay within field capacities, with explicit UI/date/file provider fixtures. Not a universal alias or real GUI save proof.',
            order='Construct12-byte DC header,36-byte DD header,8-byte DF envelope plus25683 payload,8-byte DE envelope, N*72 reference bytes and K*4 available-creature bytes. Whole size=0x6493+72*N+4*K; DE dataBytes=72*N+4*K.',
            player_count='Payload6404 stores low8(model A68 totalPlayers -2). Controlled total2/4 fixtures produce0/2.',
            automatic_flag04='Before serializing,48EA60 clears modelFlagsDC bit04. For any player model whose unsigned byte1C>=3 and byte40==0, sets that bit again. Other flags retained. Semantic labels of those player fields and gameplay meaning of flag04 are not established by this phase.',
            available_creatures='48C030 returns AL from model F8 byte array. Writer stores only AL into stack DWORD at entrySP-64F0 then writes4 bytes. Upper24 bits retain scratch seed in controlled full writer runs. Keep all4 physical bytes in a lossless editor.',
            output_policy='Do not reproduce uninitialized bytes as canonical data. Keep original opaque fields/upper creature bytes on save; for new files choose explicit initialization as our own format policy, not a claim that native always writes zero.'),
        editor_strings=dict(wide_helper='48CF90 widens each signed source byte to WORD:80->FF80,FF->FFFF. Uses CString stored length, copying all source bytes; count only pads when shorter and is NOT a truncation bound.',
            ascii_helper='4AB3B0 similarly copies all CString length then pads to count. Neither helper implements a general bounded copy.',
            overlong_case='With synthetic Information CString1027 bytes (1026 I bytes plus80), actual writer writes FF80 into raw1184, the firstWORD of the520-byte gap. Later trigger/TPS assignments repair the preceding two overwrittenWORDs. This is an isolated memory-only malformed-model fixture, not evidence the real GUI accepts that text length.',
            warning='Normal bounded-field scratch statement cannot be generalized to oversized CString models. This codec accepts direct UTF16 file storage; native signed-byte conversion is an optional diagnostic projection.'),
        evidence=[whole(r['before']) for r in load('phase35-primary-manifest.json')],primary_program_bytes=primary(),native_emulations=load('phase35-level-emulations.json')['cases'],
        unresolved_searches=dict(file='phase35-catalogue-unknown-full-text-candidates.json',sha256=hashlib.sha256((ROOT/'phase35-catalogue-unknown-full-text-candidates.json').read_bytes()).hexdigest(),
            result='No new confirmed direct consumer of Terrain198/224, Traps204 or Objects34C from full-text candidate scan. Absence is not proof of nonuse; no semantic rename based on it.'),
        limitations=['Modern payload25683 only in native emulation; existing physical schema still supports old25603. No claim of complete legacy parser equivalence.',
            '14 full writer calls:13 bounded-string cases with seeds00/A5/FF or an address-varying stack pattern;one long-Information case. Compare every output byte, including headers, descriptors, references and available-creature entries.',
            'Writer executes real stack probe,48CF90/4AB3B0 string helpers, flags setter, player/reward/introduction/speech/creature array getters. CString ownership, name/message providers, date and file write callbacks are fixtures. Real file/window operations and model allocation are not exercised.',
            '18 full converters:9 per EXE with3 raw opaque values and3 intermediate seeds; all25991 output bytes compared. Real bounded CRT copy helpers execute. Varying Unique byte is tested as copied; opaque520/2 remain from intermediate seed.',
            'Six standalone helpers verify padding, signed-byte widening and that requested count is not a copy limit. Allocated memory intentionally bounds all test writes.',
            'UnknownHistory is a retained baseline alias, not a proven history structure. Its intended historical purpose and the gameplay meaning of baseline UniqueCreatureTypeId remain unresolved.',
            'No real GUI save or gameplay run with modified files. All input and executable hashes rechecked.'])
    (ROOT/'level-descriptor-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('Level descriptor:',len(result['native_emulations']),'native calls;',len(result['primary_program_bytes']),'live ranges match PE')
if __name__=='__main__':main()
