"""Build native KeeperSpells storage/serialization evidence with explicit scope."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from build_placed_creatures import whole
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def primary():
    rows=[]
    for r in load('phase44-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==r['address'] and len(b)==m['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        rows.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return rows

def corpus():
    rows=[];gaps=Counter();c8=Counter();nonzero_names=Counter();records=0;flags=Counter()
    for p in [DATA/'KeeperSpells.kwd',DATA/'AutoSaveGlobals.kld',*sorted((DATA/'maps').glob('*.kld'))]:
        if p.name in {'LinchpinVariables.kld','XXLevel16Variables.kld','XXlevel4Variables.kld','XXLevel6bVariables.kld'}:continue
        b=p.read_bytes();sections=fmt.parse(b);rs=[r for s in sections if s.signature==150 for r in s.records]
        if not rs:continue
        assert all(len(r)==406 for r in rs);records+=len(rs)
        for r in rs:
            gaps[r[0xE0:0xE2].hex()]+=1;v=struct.unpack_from('<I',r,0xC8)[0];c8[v]+=1;flags[struct.unpack_from('<I',r,0xDC)[0]]+=1
            if v:nonzero_names[r[:32].split(b'\0',1)[0].decode('cp1252')]+=1
        rows.append(dict(file=str(p),sha256=digest(b),records=len(rs),nonzero_c8_records=sum(bool(struct.unpack_from('<I',r,0xC8)[0]) for r in rs),nonzero_gap_records=sum(any(r[0xE0:0xE2]) for r in rs)))
    return dict(files=rows,file_count=len(rows),records=records,c8_bits_counts={hex(k):v for k,v in sorted(c8.items())},nonzero_c8_name_counts=dict(nonzero_names),gap_buffers=[dict(hex=k,records=v) for k,v in sorted(gaps.items())],flags_counts={hex(k):v for k,v in sorted(flags.items())},scope='Standalone, AutoSaveGlobals and every parseable map KLD with150; duplicated catalogue contents count in each source. Corpus pattern alone does not establish the historical producer.')

def main():
    from native_keeper_pipeline import WIDTHS
    schema=dict(family='KeeperSpells',record_bytes=406,executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=primary(),evidence=[whole(r['before']) for r in load('phase44-primary-manifest.json')],native_emulations=load('phase44-keeper-pipeline-emulations.json')['cases'],corpus=corpus(),
        intermediate=dict(bytes=526,presence_dwords=30,game_initializer='0x56B990',editor_initializer='0x42B600',game_converter='0x56BAB0',editor_converter='0x42B720',rule='Both initializers zero all526 bytes. Both complete converters agree on the526-byte projection. They skip rawE0/E1; mid158/159 retain their prior bytes. RawDC flags OR into mid154, so initialize before loading a standalone record. Names are bounded32 with forced final null; resource arrays copy84 each.'),
        unknown_c8=dict(raw_offset=200,bytes=4,intermediate_offset=320,presence_offset=24,runtime_offset=58,editor_model_offset=376,meaning='unresolved',
            game_rule='Presence18: copy DWORD mid140 to definition3A without TPS arithmetic or clipping. Absent presence preserves previous3A. Full scalar loader122 bytes checked with null resource provider and explicit sound handle fixture.',
            editor_rule='Callback4A273B..4A2780: zero-extend rawDWORD to x87 qword, divide4096, store model double178. Full writer4A99B0 multiplies4096 and calls real4ACDA8, stores low32 at rawC8.',
            boundary_rule='Six raw boundary values0,1,7FFFFFFF,80000000,FE0C0000,FFFFFFFF pass unsigned model view and complete native writer checks. Products4096 are exact integers; arbitrary fractional GUI inputs/rounding not certified.',
            example=dict(bits='0xFE0C0000',unsigned=0xFE0C0000,editor_double=0xFE0C0000/4096,signed_alternative=struct.unpack('<i',struct.pack('<I',0xFE0C0000))[0],warning='A signed interpretation of identical bits does not establish a gameplay meaning. Editor view is unsigned.'),
            main_panel='Phase26 actual Keeper panel451CC0/451FD0 exposes Recharge Time from model180/rawCC. It does not identify C8. Similar CreatureSpells panel reads cannot name this Keeper field.',
            consumer_search='Phase44 direct DWORD3A leads in mapped spans were inspected for obvious unrelated UI/generator/configuration objects. No semantic Keeper consumer established. This bounded negative search does not prove unused, and is not counted as a decoded gameplay field.'),
        gap=dict(raw_offset=224,bytes=2,rule='Complete Pro writer leaves rawE0/E1 as prior stack bytes for names bounded32. Seeds00/A5/FF/pattern and consecutive records verified by comparison of complete emitted files. Both complete converters ignore these two raw bytes.',
            stack_rule='InitialESP=S, final frame=S-1F0, record begins frame+50=S-1A0; rawE0/E1 are S-C0/S-BF. This describes inspected writer, not a value to regenerate.',
            exception='Real CString helper4AB3B0 copies full source length despite count32. Synthetic names225/226 overwrite first/both gap bytes after other field stores. No claim that GUI permits such names.',
            preservation='Preserve both original bytes. Never automatically zero, synthesize stack contents, or use them as a checksum.'),
        serialization=dict(writer='0x4A99B0',record_bytes=406,count_context_offset=1380,array_context_offset=1376,
            header='N=context564; S=56+406*N; D=406*N. Enumerate array560 in physical order; skip null slots without updating header N/S/D. Full output compared for all cases including empty/null/reordered arrays.',
            independent_editor='Count the records actually serialized; preserve raw record order and unknown bytes. Do not reproduce native invalid header sizes for sparse arrays.',
            flags='Writer uses original420230 encoder, projects model BOOLs to mask1E. Phase26 decoder drops unknown flags before this. Independent editor patches selected rawDC bits and preserves all others.',
            header_opaque='Header bytes46/47 are unassigned stack in the bounded date fixture; date helper itself is replaced. This does not establish native real-clock timestamp byte semantics.'),
        scalar_fields=[dict(raw_offset=o,bytes=n) for o,n in WIDTHS.items()],api=dict(file='native_keeper_pipeline.py',operations=['view','patch_raw','patch_flags','intermediate','runtime_projection'],raw_authoritative=True),
        limitations=['68 native cases:22 complete writers,6 complete initializers,12 complete converters,12 model x87 fragments and16 complete scalar loaders. All emitted file bytes, all526 intermediate bytes, and all122 definition bytes compared.',
            'Writer model arrays, date/header constants, file append callback and3 virtual scalar getters are fixtures. Resource getter41CE60, flag encoder420230, CString helper4AB3B0, getters424EA0/424EC0 and x87 ftol execute original instructions.',
            'Model tests execute4A273B..4A2780 with construction and registration skipped. They do not prove a full GUI callback or editable control for C8.',
            'Game loader uses null graphical resource provider and explicit sound virtual handler1C=11223344. It does not exercise resource success/failure branches or game casting.',
            'C8 gameplay meaning remains unresolved. E0/E1 being ignored by these converters and unassigned by this writer is not proof for other versions/patches or for every historical producer.',
            'No original game data/EXE modifications. No live GUI/gameplay runs. Saved Ghidra function ranges compared with live bytes and original PE after emulations.'])
    assert len(schema['native_emulations'])==68
    if (ROOT/'keeper-lookup-review.json').exists():schema['phase72_lookup_review']=load('keeper-lookup-review.json')
    if (ROOT/'keeper-array-semantics.json').exists():schema['phase83_runtime_array']=load('keeper-array-semantics.json')
    if (ROOT/'keeper-ui-semantics.json').exists():schema['phase84_ui_order']=load('keeper-ui-semantics.json')
    (ROOT/'keeper-pipeline-semantics.json').write_text(json.dumps(schema,ensure_ascii=False,indent=2),encoding='utf8');print('Keeper pipeline:',len(schema['native_emulations']),'native;',schema['corpus']['file_count'],'files;',schema['corpus']['records'],'records')
if __name__=='__main__':main()
