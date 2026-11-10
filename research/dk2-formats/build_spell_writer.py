"""Evidence schema for native CreatureSpells serialization, not gameplay guesses."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from build_placed_creatures import whole
from native_spell_writer import FIELD_WIDTHS,FLAG_MODEL
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def primary():
    rows=[]
    for r in load('phase41-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==r['address'] and len(b)==m['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        rows.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return rows

def corpus():
    rows=[];tails=Counter();flags=Counter();total=0;nonzero=0
    for p in [DATA/'CreatureSpells.kwd',DATA/'AutoSaveGlobals.kld',*sorted((DATA/'maps').glob('*.kld'))]:
        if p.name in {'LinchpinVariables.kld','XXLevel16Variables.kld','XXlevel4Variables.kld','XXLevel6bVariables.kld'}:continue
        b=p.read_bytes()
        try:sections=fmt.parse(b)
        except ValueError:raise
        records=[r for s in sections if s.signature==160 for r in s.records]
        if not records:continue
        assert all(len(r)==266 for r in records)
        count_nonzero=sum(any(r[239:]) for r in records);nonzero+=count_nonzero;total+=len(records)
        for r in records:tails[r[239:].hex()]+=1;flags[fmt.u32(r,212)]+=1
        rows.append(dict(file=str(p),sha256=digest(b),records=len(records),nonzero_tail_records=count_nonzero,distinct_tails=len({r[239:] for r in records}),records_with_bits_outside_editor_mask=sum(bool(fmt.u32(r,212)&~0x1EF) for r in records)))
    return dict(files=rows,file_count=len(rows),records=total,nonzero_tail_records=nonzero,distinct_tail_buffers=len(tails),flags_counts={str(k):v for k,v in sorted(flags.items())},tail_buffers=[dict(hex=k,records=v) for k,v in sorted(tails.items())],scope='Standalone, AutoSaveGlobals and every parseable map KLD containing signature160; scopes overlap. Corpus pattern does not establish origin of a particular historical tail.')

def main():
    schema=dict(family='CreatureSpells',record_bytes=266,executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=primary(),evidence=[whole(r['before']) for r in load('phase41-primary-manifest.json')],native_emulations=load('phase41-spell-writer-emulations.json')['cases'],corpus=corpus(),
        converter=dict(game='0x56A560',editor='0x42A0D0',intermediate_bytes=346,phase41_compared_prefix_bytes=320,presence_dwords=20,flags_offset=292,rule='Both converters copy raw0..EE, with reordered fields/resources, OR rawDWORDD4 into intermediateDWORD124, and force name byte31=0. RawEF..109 never copied. Phase41 compared a320-byte prefix across12 fixtures with varied tails and seed0/A5; byte13F remains seed. Phase42 establishes full intermediate346, both zero initializers and complete-buffer comparisons; see spell-alternative-semantics.json.'),
        tail=dict(raw_offset=239,bytes=27,name='OpaqueTail',editor_writer='0x4A9730',writer_record_stack_offset=76,writer_tail_stack_offset=315,
            rule='The full writer does not initialize or assign rawEF..109 for names bounded to32 bytes. It emits all266 bytes anyway. Four seeds00/A5/FF/pattern and consecutive records preserve exactly the prior27 stack bytes.',
            exception='Real4AB3B0 copies full CString length despite count32. Synthetic length240 overwrites raw0..EF, including the first tail byte, after scalar/resource assignments. No GUI acceptance of a240-byte name claimed.',
            preserve='Keep all27 original bytes. This writer behavior is not proof that every historical tail has the same origin or that other builds/patches ignore it.'),
        flags=dict(raw_offset=212,raw_bytes=4,editor_model_fields=[dict(mask=b,offset=o) for b,o in FLAG_MODEL],decode='0x41D760',encode='0x41D6F0',model_callback='0x4A2260:0x4A2466..0x4A2474',writer_mask=495,
            rule='PresenceDWORD4C !=0: callback calls decoder on midDWORD124. Decoder stores eight bool DWORDs, dropping all other bits. Encoder interprets each modelDWORD as nonzero/zero and returns only mask1EF. Writer stores completeDWORD. Absent presence leaves model bytes unchanged.',
            game_scope='Both converters retain full32 input flags via OR. Editor mask does not establish allowed gameplay bits or effects of unknown bits. Flag names remain baseline until native consumers/UI are traced.',
            independent_editor='Patch selected bits in originalDWORD; do not apply native mask on ordinary save.'),
        serialization=dict(writer='0x4A9730',record_bytes=266,count_offset=20,whole_size_offset=8,data_size_offset=52,
            rule='Header N uses contextDWORD59C, S=56+266*N, D=266*N. Writer enumerates physical pointer array598 in order and skips null pointers without correcting N/S/D. Full output including header compared for0 records, null slots and reordered non-null records.',
            sparse_array='Fixtures slots[1,0,2] and[0,2] emit fewer records than declared; [0] emits no payload with N1. This is a native writer invariant requirement, not a format feature. Independent codec counts serialized records.'),
        scalar_fields=[dict(raw_offset=o,bytes=n) for o,n in FIELD_WIDTHS.items()],
        limitations=['61 native cases:19 full writers,10 encoders,20 model flag fragments,12 full game/editor converters. Entire output bytes and320-byte intermediate prefixes compared, not only a checksum. Full structure size346 and broader checks are documented in phase42.',
            'Full writers use explicit virtual getters, file append capture, date/header constants and prebuilt model arrays. Real resource getter, flag encoder, CString copy and ftol run. Model callbacks execute only flags fragment with skipped construction/array registration.',
            'Floating fixtures are exactly representable values whose product4096 is an integer. General x87 fractional rounding and GUI numeric parsing not established.',
            'Tail stack preservation applies to bounded names. A240-byte CString overwrites a tail byte; native helper count32 does not cap copying. This is a synthetic model fixture, not a demonstrated GUI input.',
            'No game process, native GUI open/save or original input changes. Live Ghidra initialized ranges match original PE after emulations. No universal claim about other builds or runtime Flame patches.'])
    assert len(schema['native_emulations'])==61
    (ROOT/'spell-writer-semantics.json').write_text(json.dumps(schema,ensure_ascii=False,indent=2),encoding='utf8');print('Spell writer:',len(schema['native_emulations']),'native cases;',schema['corpus']['file_count'],'files;',schema['corpus']['records'],'records')
if __name__=='__main__':main()
