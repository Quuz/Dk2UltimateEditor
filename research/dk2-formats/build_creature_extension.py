"""Native origin of Creature long-extension bytes; keep raw data authoritative."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from build_placed_creatures import whole
from native_creature_extension import view,native_stack_tail
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def primary():
    rows=[]
    for r in load('phase43-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==r['address'] and len(b)==m['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        rows.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return rows

def corpus():
    files=[];extensions=Counter();tails=Counter();pairs=Counter();variants=Counter();examples=[];nonzero_game_long=0;editor_nonzero=0;editor_immunity=0
    for p in [DATA/'Creatures.kwd',DATA/'AutoSaveGlobals.kld',*sorted((DATA/'maps').glob('*Globals.kld'))]:
        b=p.read_bytes();parts=fmt.parse(b);records=[r for s in parts if s.signature==170 for r in s.records]
        if not records:continue
        for i,raw in enumerate(records):
            v=view(raw);variants[len(raw)]+=1;pairs[(v['game_flags'],v['editor_flags'])]+=1
            if len(raw)==5537:
                extensions[v['opaque80_hex']]+=1;tails[v['last_words_hex']]+=1;nonzero_game_long+=bool(v['game_flags']);editor_nonzero+=bool(v['editor_flags']);editor_immunity+=bool(v['editor_flags']&1)
            if raw[0xF4B]==21:examples.append(dict(file=str(p),record_index=i,name=raw[:32].split(b'\0')[0].decode('cp1252'),creature_id=21,**v))
        files.append(dict(file=str(p),sha256=digest(b),records=len(records),record_bytes=sorted({len(r) for r in records})))
    return dict(files=files,file_count=len(files),records=sum(variants.values()),record_size_counts={str(k):v for k,v in sorted(variants.items())},extension80_variants=[dict(hex=k,records=v) for k,v in sorted(extensions.items())],last4_variants=[dict(hex=k,records=v) for k,v in sorted(tails.items())],game_editor_flag_pairs=[dict(game_flags=k[0],editor_flags=k[1],records=v) for k,v in sorted(pairs.items(),key=lambda x:(x[0][0],-1 if x[0][1] is None else x[0][1]))],long_records_nonzero_game_flags=nonzero_game_long,long_records_nonzero_editor_flags=editor_nonzero,long_records_editor_bit1=editor_immunity,king_examples=examples,normal_context_tail_hex=native_stack_tail(0x54F550).hex(),scope='Standalone plus AutoSaveGlobals and every map*Globals.kld containing Creatures. Repeated map data counted as physical records, not independent builds. Exact tail match is compatible with inspected writer; unique historical producer/version not proven.')

def main():
    s=dict(family='Creatures',record_variants=[5449,5537],executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=primary(),evidence=[whole(r['before']) for r in load('phase43-primary-manifest.json')],native_emulations=load('phase43-creature-extension-emulations.json')['cases'],corpus=corpus(),
        fields=[dict(raw_offset=5445,bytes=4,name='GameExtraFlags',consumer='game568E30→intermediate1935→definition68A; gameplay bit1 Process9 immunity established in phase36',writer='4A7F00 does not assign this slot; emitted bytes retain preexisting stack for bounded names.'),dict(raw_offset=5449,bytes=80,name='OpaqueExtension80',consumer='Neither full converter copies or semantically reads these bytes in paired complete-buffer checks.',writer='Preexisting stack, not an80-byte property block emitted from model.'),dict(raw_offset=5529,bytes=4,name='EditorExtraFlags',consumer='editor4287C0 ORs into intermediate1999 with presence264.',writer='Actual41EB80 encodes model DWORD1880/1884/1888/1828 into bits1/2/4/8; writer stores at raw1599. This is not game flag slot1545.'),dict(raw_offset=5533,bytes=4,name='OpaqueLastWords',consumer='Both converters ignore the last4 bytes.',writer='Writer does not assign these bytes as fields. Original stack probe leaves caller ECX bytes1..3 followed by low byte10 of return4A7F10.')],
        writer=dict(function='0x4A7F00',stack_probe='0x4AD490',model_bytes=6288,frame_sp_from_entry_sp=-5696,record_from_frame_sp=148,suffix_from_raw=5445,suffix_compared_bytes=92,
            suffix='For bounded names: raw1545..1598 copies84 preexisting stack bytes; raw1599..159C gets encoder flags&F; raw159D..15A0 gets bytes(ECX>>8,3little)+low8(4A7F10).',
            stack_proof='Entry PUSH EBP and AND ESP,-8; CALL probe pushes return4A7F10, probe PUSH ECX saves context. Its source stack slots persist after allocation. Final4 payload bytes overlap saved ECX bytes1..3 and return byte0. These are not restored from the input Creature record.',
            fixtures='18 complete writer runs: four stack seeds×three flag values, two consecutive models/animation update, three context addresses (including native global54F550), two overlong names. Full writer/getters/flags/resources/ftol/CString/_chkstk execute; only date helper and file I/O substituted. Only final92 bytes, record count and selected flags are asserted.',
            string_exception='Main name CString count32 is not a maximum. Length5450 overwrites game flag and first opaque byte; length5530 overwrites whole84-byte scratch block plus first editor-flag byte. These synthetic models do not establish allowed GUI input.'),
        converters=dict(game='0x568E30',game_initializer='0x568820',game_intermediate_bytes=6457,editor='0x4287C0',editor_initializer='0x4281B0',editor_intermediate_bytes=6561,
            tests='12 paired complete init->convert runs compare full6457/6561 bytes for modified80-byte extension, last4 bytes and opposite flag slot. Game uses1545; editor uses1599. Both ignore80 bytes and last4 in these paths. No claim all bytes of arbitrary future record variants are unused.'),
        parsers=dict(game='0x56A250',editor='0x429D90',game_raw_address=7630528,game_raw_bytes=5449,game_intermediate_address=7635977,editor_raw_address=5502272,editor_raw_bytes=5537,editor_intermediate_address=5507809,
            adjacency='game746EC0+1549=748409; editor53F540+15A1=540AE1. Parser initializes intermediate before reading raw. A game read of5537 therefore overwrites the first88 bytes of its adjacent intermediate.',
            result='Eight complete native parser runs with5449/5537 stride and1/2 records execute real initializer/converter and compare every callback buffer to an isolated native reference. For game5537, I/O fixture additionally verifies exact88-byte overwrite immediately after raw read; converter replaces this prefix and final buffer matches reference.',
            scope='Only known lengths5449/5537 tested. Generic native old-length branch reads declared stride without the bounded API restriction; other larger lengths not certified safe. Editor short path zeroes5537-byte raw buffer before reading5449, so its absent long flag slot reads0.'),
        practical_rules=['Preserve every original raw byte and source record length. Do not emulate stack output in a new compatible writer. Do not normalize the observed nonzero80 bytes or last4 bytes to0 on ordinary save.',
            'Game/editor flag slots remain independent; selected immunity edits must target intended slot explicitly. Native Pro save is not proven to carry over existing game slot1545.',
            'The phase36 Process9 consumer makes a writer-generated game-slot bit1 behaviorally relevant if such output is loaded. This is a consequence of combined native facts, not a new full-game experiment.'],
        limitations=['38 native cases:18 full writers,12 paired full converter chains and8 full parsers. Writer assertions cover final92 payload bytes, not every field of all5537 output bytes.',
            'Models are prebuilt synthetic buffers with zero numeric data except declared flags/IDs/strings; all original getter bodies execute. Exact-zero x87 inputs avoid the known fractional rounding limitation. Normal GUI construction, gameplay and graphics are not run.',
            'Parser I/O and callback are explicit fixtures. Full intermediate compared to native isolated reference, which tests buffer adjacency/reading/conversion consistency; this is not an independent semantic oracle for every field.',
            'Initial writer experiment incorrectly expected last4 bytes to retain stack seed; saved as superseded after recognizing real stack probe writes. Initial parser callbacks missed callee-saved ESI/EDI preservation; all8 cases rerun with corrected ABI, old snapshots retained and excluded.',
            'Observed tailF5540010 matches native global context54F550/probe return4A7F10. This supports the byte-origin explanation without identifying every historical producer. Other builds/Flame runtime patches not covered.'])
    assert len(s['native_emulations'])==38
    # Addresses in machine fields are derived, not hand-transcribed.
    s['parsers'].update(game_raw_address=0x746EC0,game_intermediate_address=0x748409,editor_raw_address=0x53F540,editor_intermediate_address=0x540AE1)
    (ROOT/'creature-extension-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Creature extension:',len(s['native_emulations']),'native cases;',s['corpus']['records'],'records;',s['corpus']['record_size_counts'],'sizes;',len(s['primary_program_bytes']),'PE ranges')
if __name__=='__main__':main()
