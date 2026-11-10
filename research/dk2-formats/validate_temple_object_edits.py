"""Flip only ObjectGold bit in82complete source containers; inspect original recipes."""
from pathlib import Path
import json,struct,hashlib
from collections import Counter
import dk2_formats as fmt
import native_temple_object_entry as api

ROOT=Path(__file__).resolve().parent
DATA=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor'
def sha(b):return hashlib.sha256(b).hexdigest()

def validate():
    sources=json.loads((ROOT/'object-lifetime-semantics.json').read_text())['corpus']['files'];rows=[];digest=hashlib.sha256()
    for row in sources:
        data=Path(row['file']).read_bytes();assert sha(data)==row['sha256']
        sections=fmt.parse(data);hits=[(si,ri,r) for si,s in enumerate(sections) if fmt.FAMILIES.get(s.signature)=='Objects'
            for ri,r in enumerate(s.records) if len(r)==894 and r[0x35A]==17]
        assert len(hits)==1;si,ri,raw=hits[0];s=sections[si]
        flags=struct.unpack_from('<I',raw,0x344)[0];new=api.patch_gold_bit(raw,not bool(flags&0x40))
        assert struct.unpack_from('<I',new,0x344)[0]==flags^0x40
        assert api.patch_gold_bit(new,bool(flags&0x40))==raw
        records=list(s.records);records[ri]=new
        clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
        packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
        expected=bytearray(data);start=s.offset+len(s.header)+ri*894;expected[start+0x344]^=0x40
        assert packed==expected and fmt.parse(packed)[si].records==records and packed!=data
        assert sha(Path(row['file']).read_bytes())==row['sha256'];digest.update(hashlib.sha256(packed).digest());rows.append(row)
    assert len(rows)==82
    objs={r[0x35A]:r for s in fmt.parse((DATA/'Objects.kwd').read_bytes()) if fmt.FAMILIES.get(s.signature)=='Objects' for r in s.records}
    gold=[dict(id=i,name=r[:32].split(b'\0')[0].decode('cp1252'),flags=struct.unpack_from('<I',r,0x344)[0])
        for i,r in objs.items() if struct.unpack_from('<I',r,0x344)[0]&0x40]
    recipes=[];kinds=Counter();gold_ingredients=[]
    for s in fmt.parse((DATA/'GlobalVariables.kwd').read_bytes()):
        for ri,r in enumerate(s.records):
            if len(r)!=16 or struct.unpack_from('<I',r)[0]!=43:continue
            ingredients=[]
            for index in range(3):
                kind,ident=r[4+index*2:6+index*2]
                if kind:
                    kinds[kind]+=1;ingredients.append([kind,ident])
                    if kind==2 and ident in objs and struct.unpack_from('<I',objs[ident],0x344)[0]&0x40:gold_ingredients.append([ri,index,ident])
            recipes.append(dict(record_index=ri,ingredients=ingredients,raw_hex=r.hex()))
    assert len(recipes)==44
    return dict(phase=135,files=rows,new_exact_container_rebuilds=82,inverse_record_edits=82,
        physical_changed_byte_offset=0x344,only_changed_mask=0x40,other_flags_and_all_other_container_bytes_preserved=True,
        ordered_patch_digest=digest.hexdigest(),source_writes=0,
        current_gold_objects=gold,current_recipes=recipes,current_recipe_count=44,
        current_ingredient_type_counts={str(k):v for k,v in sorted(kinds.items())},
        original_gold_recipe_ingredient_occurrences=gold_ingredients,
        source_catalogues=[dict(file=str(DATA/n),sha256=sha((DATA/n).read_bytes())) for n in ['Objects.kwd','GlobalVariables.kwd']],
        native_recipe_cases_diagnostic=True,scope=__doc__)

if __name__=='__main__':
    v=validate();(ROOT/'phase135-temple-object-api-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print('82exact Gold-bit edits; source recipe ingredient kinds',v['current_ingredient_type_counts'])
