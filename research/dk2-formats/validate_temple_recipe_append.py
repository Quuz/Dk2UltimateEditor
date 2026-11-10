"""One canonical wholeGlobalVariables append, cross-checked by manual byte splice."""
from pathlib import Path
import json,struct,hashlib
import dk2_formats as fmt
from native_sacrifice import raw_recipe
import append_temple_recipe as api

ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/GlobalVariables.kwd'
def sha(b):return hashlib.sha256(b).hexdigest()

def example():
    source=SOURCE.read_bytes();recipe=raw_recipe([(1,5)],4,5)
    return source,api.append(source,recipe),recipe

def validate():
    source,packed,recipe=example();before=fmt.parse(source);after=fmt.parse(packed)
    assert len(before)==len(after)==2 and before[0].signature==0xE6 and before[1].signature==0xBE
    a,b=before[0],after[0];assert len(a.records)==621 and len(b.records)==622 and b.records[:-1]==a.records and b.records[-1]==recipe
    assert a.offset==b.offset==0 and a.suffix==b.suffix
    expected=bytearray(source[:9992]+recipe+source[9992:])
    for offset,value in [(8,10008),(20,622),(52,9952)]:struct.pack_into('<I',expected,offset,value)
    assert packed==expected and before[1].pack()==after[1].pack() and after[1].offset==before[1].offset+16
    assert len(source)==13636 and len(packed)==13652
    assert len(api.recipes(source))==44 and len(api.recipes(packed))==45 and api.recipes(packed)[-1]==recipe
    assert sha(SOURCE.read_bytes())==sha(source)
    return dict(phase=136,source=dict(file=str(SOURCE),sha256=sha(source)),source_bytes=len(source),rebuilt_bytes=len(packed),
        source_variable_records=621,rebuilt_variable_records=622,source_recipes=44,rebuilt_recipes=45,
        new_recipe_hex=recipe.hex(),appended_section_relative_offset=9992,
        changed_header_fields=[dict(offset=8,old=9992,new=10008),dict(offset=20,old=621,new=622),dict(offset=52,old=9936,new=9952)],
        entire_other_section_byte_exact=True,all_prior_records_order_header_unknowns_and_suffix_preserved=True,
        rebuilt_sha256=sha(packed),new_complete_container_append_checks=1,source_writes=0,
        native_full_file_parser_executed=False,scope=__doc__)

if __name__=='__main__':
    v=validate();(ROOT/'phase136-temple-recipe-append-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print('GlobalVariables621→622records;44→45recipes; whole other section unchanged')
