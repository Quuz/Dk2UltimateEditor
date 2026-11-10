"""Build first-party creature Things semantics, including native unknown-byte labels."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import asm_range,pe_read,EXE
from native_placed_creatures import *
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def whole(name):
    p=ROOT/'ghidra'/name;lines=p.read_text().splitlines();addresses=[int(s[:8],16) for s in lines if len(s)>9 and s[8]==':']
    return asm_range(name,min(addresses),max(addresses))

def native_data():
    b=EXE.read_bytes();labels=[];constants=[]
    for va,text in [(0x69CF54,'PartyId'),(0x69CF3C,'WanderRadius')]:
        raw=pe_read(b,va,64).split(b'\0',1)[0];assert text in raw.decode('ascii');labels.append(dict(address=hex(va),text=raw.decode('ascii'),hex=raw.hex(),sha256=hashlib.sha256(raw).hexdigest()))
    for va,want in [(0x66EB28,0.01),(0x66EB30,1.0)]:
        raw=pe_read(b,va,8);assert struct.unpack('<d',raw)[0]==want;constants.append(dict(address=hex(va),hex=raw.hex(),double=want))
    return dict(debug_labels=labels,floating_constants=constants)

def main():
    result=dict(family='Things Creatures',tags={str(t):dict(payload_bytes=SIZES[t],game_converter=hex(CONVERTERS[t]),editor_converter=hex(EDITOR_CONVERTERS[t]),intermediate=MID[t]) for t in SIZES},
        executable_sha256=load('executable-hash-verification.json'),native_data=native_data(),
        converter='Both executables preserve raw field widths into corresponding56/84/64-byte intermediates. Raw flags are ORed into preinitialized intermediate flag bytes. Input initializers must zero those bytes; standalone converter API assumes zero seed.',
        position='All three game callbacks copy XYZ via572CD0, then creator receives centered Q12 X/Y and Z0. Raw Z still reaches the preceding full intermediate checksum. Editor writers explicitly emit DWORD Z0.',
        percentages=dict(gold=dict(raw_offset=12,raw_bytes=2,meaning='initial carried gold percentage of level-adjusted Creature MaxGoldHeld, not a literal number of coins',base_runtime_offset=hex(0x69C),growth_table_offset=hex(0x13B),
                flow='unsigned raw WORD -> x87 *double0.01 *unsigned32 level-adjusted capacity -> ftol64 -> low32 passed to48AE70. Helper adds modulo2^32 to instance3FB, then compares unsigned against level-adjusted capacity and caps to that unsigned value. Initial instance amount is a creation input.',helper=hex(0x48AE70)),
            health=dict(raw_offsets={'198':16,'199':20,'200':16},raw_bytes=4,meaning='initial health percentage of level-adjusted Creature Health, not absolute hit points',base_runtime_offset=hex(0x692),growth_table_offset=hex(0xE1),
                flow='First health delta1-currentHealth to4B2CE0 (or490120 for Good actual ID1C). Then unsigned raw DWORD -> x87 *double0.01 *unsigned32 level-adjusted health -double1.0 -> ftol64 -> low32 delta to same helper. Damage/helper constraints and dying actions may change final state; no unconditional finalHealth=percentage formula is claimed.'),
            growth='If raw level byte==1, use unsigned base WORD. Otherwise sign-extend growth WORD at [global6E4154+offset+2*level], multiply by unsigned base WORD and divide signed by100 truncating toward0. Product of these16-bit inputs fits signed32. The quotient is then zero-extended as DWORD into x87 qword, so negative growth quotient becomes a large unsigned32 value. Level0 is not changed to1 by the game path.',
            level='Creator is followed by max(rawLevel-1,0) calls48B120. No10 clamp appears in these callbacks. Later helper uses actual instance level403; fixtures for standalone48AE70 set that level explicitly.',
            ftol='Original634F30 saves control word, sets rounding bits0C00, FISTP signed qword, restores control word, returns EDX:EAX. Native health-expression tests stop before this call and capture a rounded64-bit view of ST0. No exhaustive x87 rounding or final health simulation.'),
        native_unknown_bytes=dict(wander=dict(raw_offsets={'198':23,'199':29},intermediate_offsets={'198':55,'199':81},runtime_offset=hex(0x418),bytes=1,native_label='WanderRadius',
                meaning_scope='Native diagnostic label and copy path established. No operational radius calculation or coordinate units found in inspected mapped-span search. Keep full byte; label is not proof of active gameplay effect.'),
            party=dict(raw_offset_good=30,intermediate_offset=82,runtime_offset=hex(0x417),bytes=1,native_label='PartyId',
                consumer='458C70: acting creature must have runtime46F Leader bit2. Iterate owner linked creature list, select same byte417 plus Follower bit4 and state25E==DE or pending32D==DE. Call objective48BE20(0C,leader40D,0) then state48AD30(E2). Both FF party bytes compare equal; there is no FF sentinel exclusion in this consumer.',
                scope='This is one verified same-party consumer, not a complete hero-party state machine. C9 embedded records/party spawn have additional paths not established here.')),
        flags=dict(primary=dict(raw_offset=15,runtime_offset=hex(0x46F),bytes=1,good_mask=255,neutral_keeper_mask=0xF9,
                rule='Good invokes all8 original boolean setters. Neutral/Keeper skip Leader/Follower masks2/4, preserving preexisting runtime bits instead. Other6 input bits replace their matching runtime bits. START_AS_DYING bit80 triggers subsequent threshold/kill handling, skipped by runtime fragment emulations.'),
            secondary=dict(raw_offset_good=31,bytes=1,runtime_offset=hex(0x3EA),runtime_bytes=4,
                masks=[dict(raw=1,runtime=0x80),dict(raw=2,runtime=0x100),dict(raw=4,runtime=0x800),dict(raw=8,runtime=0x200)],
                rule='Only low4 raw bits inspected. Bit8 setter48E0D0 additionally writes instanceF0 to1 or0. Other runtime3EA bits preserved. Preserve full raw byte independently.'),
            trigger='49A950 stores low16 TriggerId at419; if nonzero, looks up trigger518090 and copies byteF to instance4F9. No range/missing-trigger recovery established.'),
        native_editor=dict(callbacks={'198':hex(0x491120),'199':hex(0x490EB0),'200':hex(0x491340)},
            modern='For map model major DWORD60!=1, each raw gold WORD and health DWORD is narrowed to low8 first, then min(byte,100). Thus300->44,256->0,255->100,FFFFFFFF->100. Health/gold retained as model bytes1A/1B. Raw level0 becomes1;255 stays255.',
            legacy='Map model major60==1 takes separate x87 absolute-to-percentage conversion using definition Health WORD15EA/MaxGold WORD1622, then low8/min100. This branch was inspected statically; no API numeric projection or native test for it is claimed. These map-model version fields are not automatically equated with an arbitrary KWD section header version.',
            good='Good callback always sets model21 PartyId=FF, does not consume input intermediate52. It normalizes objective0 to19 and AP3, otherwise AP setter41CF40 retains only low8(raw AP DWORD). It loads flags then41CF20 clears model Leader/Follower. If unsigned major60<=2 and minor64<50, additionally clears ReturnToHeroLair mask10. Neutral/Keeper do not call this finalizer here.',
            keeper='Keeps AP DWORD in model1C. Do not apply the Good AP byte rule to Keeper.',
            save='Full Good serializer4952F0 zeroes32-byte destination, writes Z0, zero-extended gold model1B->WORD and health model1A->DWORD, model20/21->raw1D/1E, flags codec all8/low4. If model objective22 is0 it mutates model22 to19 before writing. AP getter41CF30 explicitly zero-extends byte24 into raw DWORD10. Null model emits32 zero bytes.',
            neutral_keeper_save='Main writer4947C0 independently emits Z0, model gold byte1B zero-extended toWORD and model health byte1A zero-extended toDWORD. These serialization paths are inspected statically; full32-byte Good serializer is executed natively.',
            distinction='Independent raw codec must preserve all original bits, C7 PartyId, high flags2, high AP/gold/health bits and Z. Native GUI load/save projection is opt-in and separately reports loss. No full C9 party load/save claim.'),
        evidence=[],native_emulations=load('phase31-creature-emulations.json')['cases'],
        limitations=['Exact inspected EXEs; no real level/editor GUI load-save, world simulation or modified gameplay.',
            '27 full editor callbacks use explicit constructors/array fixtures and deterministic model definition. Original setters/getters/vector/flag codec/Good finalizer execute; map major1 floating legacy branch and unique-clone construction are excluded.',
            '18 runtime placement fragments begin at trigger/extra-byte handling after creation/level/health/gold, execute actual flag and trigger setters, stop before START_AS_DYING handling. Trigger provider/objective/keeper command helpers use fixtures.',
            '12 standalone native48AE70 helper runs test modulo addition, unsigned cap and signed-growth capacity. Seven original Neutral health expression fragments capture ST0 as64-bit value before ftol64, not exact80-bit state, final damage result or all three entire callbacks.',
            'Six full Good serializer runs in paired first24/tail8 readouts cover all32 bytes for three model objectives. Definition lookup/flag/AP getters are real; virtual XY/trigger providers are fixtures. Six same-party consumer runs use explicit one-node linked-list/objective/state fixtures.',
            'Six game and six editor converter runs assert selected fields. Search candidates are leads, not proof of all consumers. WanderRadius effect/units remain unresolved.'])
    for n in ['neutral-converter','good-converter','keeper-converter','neutral-editor-converter','good-editor-converter','keeper-editor-converter','neutral-placement','good-placement','keeper-placement',
              'neutral-editor-load','good-editor-load','keeper-editor-load','good-editor-writer','creature-definition-lookup','creature-clone-id','AP-setter','good-AP-getter','creature-editor-flags','editor-flags-decode','editor-flags2-encode','editor-flags2-decode','editor-finalize','position-vector','trigger-setter','gold-helper','ftol','party-same-group']:
        result['evidence'].append(whole('phase31-'+n+'-assembly.txt'))
    for va in [0x48DF10,0x48DF40,0x48DF70,0x48DFA0,0x48DFD0,0x48E000,0x48E030,0x48E060,0x48E090,0x48E0B0,0x48E0D0,0x48E140]:result['evidence'].append(whole(f'phase31-setter-{va:x}.txt'))
    result['evidence'] += [asm_range('phase31-creature-dump-assembly.txt',0x490CD8,0x490D04),asm_range('phase30-things-writer-before.txt',0x494ADD,0x494B3E),asm_range('phase30-things-writer-before.txt',0x494B9D,0x494C10)]
    if (ROOT/'wander-state-semantics.json').exists():result['phase75_wander_state']=load('wander-state-semantics.json')
    (ROOT/'placed-creature-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('Placed Creatures:',len(result['native_emulations']),'native cases; percentages, PartyId/WanderRadius, editor normalization')
if __name__=='__main__':main()
