"""Native HeroParty C9 schemas, proof ranges and bounded helper contracts."""
from pathlib import Path
import hashlib,json
from build_effect_semantics import pe_read,asm_range
from build_placed_creatures import whole
from party_writer_stack_audit import inspect
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def primary():
    out=[]
    for r in load('phase32-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads((ROOT/'ghidra'/r['memory']).read_text());actual=bytes(m['data']);assert m['length']==r['length'] and int(m['address'],16)==r['address']
        rel='Dungeon Keeper 2/DKII-DX.exe' if r['program']=='DKII-DX.exe' else 'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'
        assert actual==pe_read((ROOT.parent.parent/rel).read_bytes(),r['address'],r['length'])
        out.append({**r,'bytes_sha256':hashlib.sha256(actual).hexdigest(),'before_sha256':hashlib.sha256(before.read_bytes()).hexdigest(),'memory_file_sha256':hashlib.sha256((ROOT/'ghidra'/r['memory']).read_bytes()).hexdigest()})
    return out

def main():
    r=dict(family='Things HeroParty',tag=201,canonical_payload_bytes=555,
        executable_sha256=load('executable-hash-verification.json'),
        canonical_fields=dict(name=dict(offset=0,bytes=32,kind='raw bytes; native name handling is separate'),trigger_id=dict(offset=32,bytes=2),party_id=dict(offset=34,bytes=1),
            opaque_header=dict(offset=35,bytes=8,baseline_fields=['X23 DWORD','X27 DWORD']),members=dict(offset=43,stride=32,slots=16,kind='embedded Good C7 payload, no per-member8-byte envelope')),
        variants=[dict(payload_bytes=264,header_bytes=8,member_slots=8,member_offset=8,name='No input name; zeroed canonical buffer yields empty name',trigger_offset=0,party_offset=2),
            dict(payload_bytes=268,header_bytes=12,member_slots=8,member_offset=12,name='Native scans CString at temporary buffer+268, outside read input; result depends on preexisting stack',trigger_offset=0,party_offset=2),
            dict(payload_bytes=300,header_bytes=12,member_slots=8,member_offset=12,name='Native scans CString starting input268, then unbounded copy to temporary name; bounded API requires NUL within32 bytes',trigger_offset=0,party_offset=2),
            dict(payload_bytes=555,header_bytes=43,member_slots=16,member_offset=43,name='Input0..31 -> strncpy32; force intermediate name31=0',trigger_offset=32,party_offset=34)],
        parsers=dict(game=hex(0x570210),editor=hex(0x430A70),editor_converter=hex(0x42F9E0),
            read_rule='Only payload264/268/300 have special branches; every other declared length requests555 bytes, including556. Independent file parser must respect actual envelopes and not reproduce this fallback on malformed records.',
            intermediate='631 bytes:19 DWORD presence flags0..4B all1, followed by555-byte canonical buffer4C..276. Header ID6E, trigger6C; member base77. Name padded after firstNUL, then byte6B forced0.',
            opaque_rule='Both raw DWORDs23/27 are not copied. Intermediate canonical corresponding8 bytes remain0; all16 member records are copied verbatim even when CreatureId0. Raw name32nd byte forced0; name does not reach world-store entity state.',
            legacy_rule='Legacy264 copies8 members from offset8,268/300 fromoffset12, then leaves8 remaining canonical slots0. Input legacy header gap bytes are not copied.268 name is not deterministic from file bytes alone.',
            checksum='Callback5212A0 calls world checksum5108C0(mid,631) before storage. Raw opaque23/27 do not affect that intermediate because they were replaced by0; normalized name and member unused bytes remain included in checksum path. Name not used by world-store is not globally unused.'),
        game_store=dict(callback=hex(0x5212A0),helper=hex(0x50FA70),world_base=hex(0x4030),runtime_stride=520,
            rule='world+4030+u8PartyId*520: assign triggerWORD at0, ID byte2, copy all16*32 member bytes to8. Runtime bytes3..7 retain previous contents. No name or reserved-header DWORD assignment.',
            index='No24 bounds check in helper; byte24 and255 still index by520. Editor count/get/writer iterate24 slots0..23. Raw ID255 additionally has auto-assignment meaning in editor array insertion48DE20, after member fields were assigned; full insertion/GUI consequences not emulated.',
            duplicate='Storage directly replaces the same indexed trigger/ID/member block. Later same-ID records replace these fields in input order; preserved runtime bytes3..7 are not supplied by either raw record.',
            spawn_scope='WorldTrigger519F90 subsequently loops16 member slots and skips only CreatureId0. Copies member raw1D/1E to instance418/417, not header PartyId. Header/embedded PartyId can disagree in hand-edited raw data. Complete spawn/action logic not emulated.'),
        native_editor=dict(callback=hex(0x491A40),
            selection='Loop16 presence DWORDs, skip absent flag or embedded CreatureId0. Constructs only nonempty models; add4211B0 fills first free party slot, compacting holes.',
            fields='After model creation: level=raw level byte (0 not normalized), PartyId=header byte overriding member raw1E, Wander=member1D, target/objective copied as bytes, AP DWORD narrowed by41CF40 to low8. Raw XYZ not assigned by this loop; model constructors determine position defaults.',
            modern_percent='For map model major60!=1: low8 gold/health then min100, matching modern individual fields. Major1 has separate absolute conversion, excluded from API/emulations.',
            flags='Loads all8 flags and low4 Flags2; no individual Good finalizer41CF20 and no ReturnToLair version reset here. If neither Leader nor Follower is set when adding, first member gets Leader, subsequent members Follower. Explicit Leader/Follower, including both set and multiple leaders, stay unchanged.',
            zero_objective='Party member loading retains Objective0 and low8 AP; does not use individual Good callback default Objective19/AP3. Later serializer4952F0 changes objective0->19 but leaves AP getter value.',
            naming='Callback formats Hero Party %u using headerID+1 into party model display name regardless original raw name. Full MFC naming/allocation/insertion path is inspected statically, not fully emulated.',
            unique='Definition unique/clone lookup can construct a base-ID model with marker5A. Member field fragments start after creation; do not model complete clone resolution.'),
        native_writer=dict(function=hex(0x4947C0),party_range=[0x494F76,0x495014],member_serializer=hex(0x4952F0),
            rule='Iterate24 party pointers and16 member slots. Write envelopeC9/555. In555-byte scratch, assign triggerWORD20, partyID22 and16 serialized32-byte members at2B; null member serializer produces32 zeros. Do not assign name00..1F or reserved23..2A.',
            unknown_scope='Name32 and reserved8 bytes retain scratch contents under the separately stated helper/write contracts, not stable metadata or member counts. Independent editor should preserve source raw bytes, not synthesize native stack residue.',
            stack_audit=inspect()),
        evidence=[],primary_program_bytes=primary(),native_emulations=load('phase32-party-emulations.json')['cases'],
        limitations=['Exact EXEs. No real GUI save/spawn, world simulation, modified game or changed original data.',
            '16 full parser runs have explicit read/callback fixtures. Original initializers/converters/strncpy execute. Successful callbacks compare all631 bytes using REPE CMPSB; failure return paths and fallback555 read are separately asserted. Editor error-UI helper is stubbed in failure case.',
            'Four complete game callbacks/world-store runs stub checksum and compare all520 target bytes with seeded runtime gap. IDs24/255 write only allocated EmulatorHelper fixture memory; no real game writes.',
            '24 editor member fragments start after model construction and stop after native4211B0. Explicit trigger virtual storage fixture; actual field assignments/AP setter/flag codecs/count/free/default leader/follower helpers execute. Entire16-slot MFC callback and final array insertion are not emulated.',
            'Six full Things writers use zero nonparty counts, one seeded party, three scratch patterns and empty/full16 member sets. Original party count/get, member get, definition lookup and full Good serializer execute; unrelated counts/date/virtual XY-trigger/file operations use fixtures. All555 captured bytes compared exactly.',
            'Static prefix CFG covers593 instruction states; direct stack writes/PUSHes checked through first C9 write/skip. Bounded helper and readonly file contracts are explicit; not a universal interprocedural alias verifier.',
            'Legacy268 name requires an explicit outside-input CString seed. Legacy300 API rejects unterminated/long names instead of reproducing unbounded native copy. Unknown raw fields remain unknown.'])
    for f in ['phase32-party-parser-assembly.txt','phase32-party-editor-reader-assembly.txt','phase32-party-editor-init-assembly.txt','phase32-party-editor-converter-assembly.txt','phase32-party-world-store-assembly.txt','phase32-party-callback-assembly.txt','phase32-party-member-add-assembly.txt','phase32-member-count-assembly.txt','phase32-member-first-free-assembly.txt','phase32-member-leader-setter-assembly.txt','phase32-member-follower-setter-assembly.txt','phase32-party-array-insert-assembly.txt']:
        r['evidence'].append(whole(f))
    r['evidence'] += [asm_range('phase32-party-editor-load-assembly.txt',0x491A8D,0x491DBE),asm_range('phase30-things-writer-before.txt',0x494F76,0x495014),
        asm_range('phase31-creature-trigger-party-assembly.txt',0x51A17E,0x51A1D5),asm_range('phase31-creature-trigger-party-assembly.txt',0x51A432,0x51A44D),asm_range('phase31-creature-trigger-party-assembly.txt',0x51A5D4,0x51A5DC)]
    (ROOT/'hero-party-semantics.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf8');print('HeroParty:',len(r['native_emulations']),'native cases;',len(r['primary_program_bytes']),'live Ghidra ranges match PE')
if __name__=='__main__':main()
