"""Verified ActionPoint geometry, timer/flags and native writer behavior."""
from pathlib import Path
import hashlib,json
from build_effect_semantics import pe_read,asm_range
from build_placed_creatures import whole
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def primary():
    out=[]
    for r in load('phase33-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];assert p.read_text().rstrip()==a.read_text().rstrip()
        f=ROOT/'ghidra'/r['memory'];m=json.loads(f.read_text());b=bytes(m['data']);assert m['length']==r['length'] and int(m['address'],16)==r['address']
        exe='Dungeon Keeper 2/'+('DKII-DX.exe' if r['program']=='DKII-DX.exe' else 'Data/editor/DK2 Editor pro.exe');assert b==pe_read((ROOT.parent.parent/exe).read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'memory_sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'program_bytes_sha256':hashlib.sha256(b).hexdigest()})
    return out
def main():
    r=dict(family='Things ActionPoint',tag=197,canonical_payload_bytes=56,executable_sha256=load('executable-hash-verification.json'),
        physical_fields=[dict(name=n,offset=o,bytes=w) for n,o,w in [('coordinate0',0,4),('coordinate1',4,4),('coordinate2',8,4),('coordinate3',12,4),('wait_delay',16,2),('flags',18,2),('trigger_id',20,2),('id',22,1),('next_waypoint_id',23,1),('name',24,32)]],
        parser=dict(game=hex(0x5700A0),editor=hex(0x430910),game_converter=hex(0x56F790),editor_converter=hex(0x42F8E0),
            size='For declared56, read56. Otherwise zero a56-byte stack buffer and read declared length into it. Bounded API supports0..56 and pads missing bytes; native larger length is not a safe variant and is not reproduced.',
            intermediate='92 bytes:9 presence DWORDs0..23 set1; four coordinate DWORDs24/28/2C/30, packed wait/flags DWORD34, triggerWORD38, ID3A, next3B, name32 at3C with byte5B forced0. Name strncpy32 pads after firstNUL.',
            geometry='If raw coordinate2 (baseline EndX) !=0: copy four DWORDs as corners. If it equals0: raw0/4 are centerX/Y, rawC radius; bounds=(x-r,y-r,x+r,y+r) modulo2^32. Both EXEs apply this unconditionally, including56-byte records. No ordering/clamping validation here.',
            checksum='Game loader5211A0 calls world checksum5108C0(mid,92) before narrowing; normalized name, upper coordinate bits and full raw flagsWORD still contribute to this intermediate path.'),
        game_load=dict(callback=hex(0x5211A0),creator=hex(0x4B7F20),initializer=hex(0x4B0420),
            coordinates='Low16 of each intermediate coordinate sent as two2-WORD position values to creator/init; runtime38/3A/3C/3E storeunsigned WORDs. No Q12 scaling at storage stage; patrol later centers selected tiles.',
            timer='runtime30 WORD=low16(raw WaitDelay * global6F2590 ticksPerSecond). This is a duration, not an absolute start tick. Multiplication uses packed wait+flagsDWORD but low16 equals wait-only product. No saturation; 32767*20->65516.',
            flags='Initializer zeros runtime32 byte. Loader invokes only masks04,08,10,01,20, giving rawFlags&3D. Raw02/40/80 and all high8 flags are not assigned. With nonzero preexisting32 and isolated setters, other bits would remain, but actual creator/init clears byte first.',
            next='raw next byte17 ->runtime33; raw ID16 ->runtime37. No next-ID existence check performed here.',
            trigger='4B0C90 does nothing for TriggerId0; otherwise storesWORD34 and looks up518090, copying trigger byteF to runtime36. Initializer cleared both fields; missing-trigger handling not established.',
            creation_scope='Native tests execute original creator/init/callback and all five flag setters; allocation/constructor/base CThing-init/checksum/trigger lookup are fixtures. Entire64-byte selected entity buffer compared, not full world/entity registration.'),
        flags=dict(raw_bits=16,game_loaded_mask=0x3D,editor_retained_mask=0xFF,
            labels=[dict(baseline_mask=1,dialog_text='Hero Lair'),dict(baseline_mask=16,dialog_text='Reveal Through Fog Of War'),dict(baseline_mask=32,dialog_text='Tool Box'),dict(baseline_mask=64,dialog_text='Ignore Solid')],
            label_source='Matching labels occur in native dialog278, controls1840/1836/1841/1842. Mask-name associations retained from baseline; this phase does not prove each control-to-bit binding. Mask04 has native writer dependency on waypoint links; gameplay semantic meaning of04/08 remains unresolved.',
            ignore_solid='Patrol471EB0 tests runtime32 mask40; when set and terrain runtime17B bit1 set, it rejects that candidate before movement helper. This does not mean pass through solid. Loader does not populate bit40 from raw. Exact EXE behavior only; other runtime setters/saved-state sources not excluded.',
            toolbox='510E20 walks owner AP linked list via owner12 head and AP8 next tag; for every runtime32 bit20 hit replaces return byte with AP37, so last matching entry wins. This consumer inspected statically, no full selection behavior emulation claimed.'),
        consumers=dict(area='4B07E0 owner0/type0 fast path returns low16((endX-startX+1)*(endY-startY+1)), inclusive rectangle. Other filtered branches are not emulated.',
            patrol='471EB0 selects centered Q12 XY, Z0. For unequal axis endpoints, random bound=end-start, without+1: ordered nondegenerate range excludes end coordinate. Equal axis uses fixed coordinate. Up to4 terrain/nav attempts. Full tests use explicit random outputs and simple terrain/ownership providers, not whole pathfinding.',
            advancement='472150 first checks low8 of creature objective DWORD40F; if0, does nothing. Otherwise virtual2C8 resolves AP, sets full objectiveDWORD40F=zeroextended nextBYTE33, deadlineDWORD413=low32(now+unsigned waitWORD30), requests stateAF. It schedules waiting even when nextBYTE0. No lookup success guard established.',
            wait='4721C0 compares unsigned now to deadline: now>deadline requests stateAD. Equality stays on waiting branch. Before expiry, tries up to4 nearby movement choices, then returns1 even if none succeeded. Tests block those choices via explicit tile provider; wrap comparison is unsigned, not signed elapsed-time arithmetic.'),
        native_editor=dict(loader=hex(0x4918B0),codec_set=hex(0x41B9D0),codec_get=hex(0x41B9B0),
            fields='Bounds copied as fullDWORDs to model24/28/2C/30, ID1C, next34. Packedwait+flags setter keeps WORD36 wait and byte38 flags, dropping high8 raw flags. Getter zeroextends each and returns wait|(flagsByte<<16).',
            legacy='If model major60<=2 and minor64<40, loader calls41B950(0) clearing HeroLair bit01 after codec. Full MFC loader/constructor/naming/insertion excluded from native tests.',
            writer='4947C0 iterates AP models. If current next !=0, OR model38 bit04. If current next==0, scan every AP: any nonzero next also ORs current model38 bit04. Never clears existing04. Thus any linked AP makes04 set on all AP models and mutates those models during serialization.',
            output='Writes full boundsDWORDs, waitWORD, flags byte widened toWORD with highbyte0, triggerWORD, ID/next bytes. Does not assign payloadName32 in inspected AP loop; eight full writer runs with seeds00/A5 retained those bytes exactly. Scope is controlled complete writer fixtures plus static instruction evidence, not universal alias proof.',
            raw_policy='Preserve full physical flagsWORD, all coordinate DWORDs, raw name and variant length by default. Native writer normalization/mutation is separate opt-in projection; recomputing game checksum from raw bytes is incorrect.'),
        evidence=[],primary_program_bytes=primary(),native_emulations=load('phase33-ap-emulations.json')['cases'],
        limitations=['Exact inspected EXEs only; no modified gameplay or real GUI save.',
            '14 full parsers use explicit read/callback fixtures and compare all92 intermediate bytes viaCMPSB. Short lengths0/16/24 and full56 tested. No oversized native stack read reproduced.',
            '17 full game loaders execute actual creator/init/flag setters with allocation, constructor, base-init, checksum and trigger provider fixtures. All64 entity bytes compared. No allocator/linked-list/world registration guarantee.',
            'Eight full Things writers use two AP models and zero counts for other families, actual AP array getter/packed-field getter, deterministic virtual trigger/file/date/count fixtures. All112 payload bytes and both mutated model flag bytes checked.',
            '11 full patrol functions use controlled PRNG outputs, AP/terrain/ownership virtual providers and movement fixture;5 full next-state functions use AP/clock/state providers;5 wait functions block neighbor movement. No actual nav/AI simulation.',
            'Four inclusive area fast paths and6 original editor packed-field codecs verified. Unknown04/08 gameplay effect, full editor property panel commit, UI legacy-major handling and exact field ranges remain separate unresolved work.'])
    for name in ['AP-parser','AP-converter','AP-editor-reader','AP-editor-converter','AP-loader','AP-create','AP-init','AP-trigger-setter','AP-patrol','AP-patrol-next','AP-patrol-wait','AP-editor-flags-setter','AP-editor-legacy-clear','AP-flag-search']:
        r['evidence'].append(whole('phase33-'+name+'-assembly.txt'))
    for va in [0x4B0AF0,0x4B0B10,0x4B0B30,0x4B0B50,0x4B0B70]:r['evidence'].append(whole(f'phase33-setter-{va:x}-assembly.txt'))
    r['evidence'] += [whole('phase32-prefix-callee-41b9b0.txt'),asm_range('phase33-AP-editor-load-assembly.txt',0x4919DE,0x491A19),asm_range('phase33-AP-random-room-assembly.txt',0x4B07E0,0x4B0824),asm_range('phase30-things-writer-before.txt',0x494EC1,0x494F70)]
    previous=ROOT/'action-point-semantics.json'
    if previous.exists():
        saved=json.loads(previous.read_text(encoding='utf8'))
        if 'phase64_retreat' in saved:r['phase64_retreat']=saved['phase64_retreat']
    if (ROOT/'ap-lookup-review.json').exists():r['phase76_lookup_review']=load('ap-lookup-review.json')
    (ROOT/'action-point-semantics.json').write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf8');print('ActionPoint:',len(r['native_emulations']),'native runs;',len(r['primary_program_bytes']),'Ghidra ranges match PE')
if __name__=='__main__':main()
