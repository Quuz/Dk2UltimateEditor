"""Preserve full DWORDs for PoisonSpit parameters despite runtime low16 use."""
import hashlib
import json
from pathlib import Path
import dk2_formats as fmt
import native_shot_damage as shot
from native_spell_writer import patch_raw

ROOT=Path(__file__).resolve().parent


def main():
    files=[];counts={};total=changed=views=0;aggregate=hashlib.sha256()
    for family,manifest,sig,offsets in [('Shots','shot-creation-semantics.json',0x104,[0x98,0x9C]),
                                      ('CreatureSpells','spell-alternative-semantics.json',0xA0,[0xC8,0xCC])]:
        rows=json.loads((ROOT/manifest).read_text(encoding='utf8'))['corpus']['files'];records=0
        for row in rows:
            data=Path(row['file']).read_bytes();assert hashlib.sha256(data).hexdigest()==row['sha256']
            sections=fmt.parse(data);assert b''.join(s.pack() for s in sections)==data
            hits=[(i,s) for i,s in enumerate(sections) if s.signature==sig];assert hits
            def patch(raw,off,value):
                return shot.patch(raw,**{('data1' if off==0x98 else 'data2'):value}) if family=='Shots' else patch_raw(raw,off,value)
            for _,s in hits:
                for raw in s.records:
                    for off in offsets:assert patch(raw,off,int.from_bytes(raw[off:off+4],'little'))==raw
                    views+=1;records+=1
            si,s=hits[0];raw=s.records[0]
            for off,mask in [(offsets[0],0x80),(offsets[1],0x80),(offsets[0],0x10000),(offsets[1],0x10000),(offsets[0],0)]:
                value=int.from_bytes(raw[off:off+4],'little')^mask;edited=patch(raw,off,value)
                records_=list(s.records);records_[0]=edited
                clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records_)
                packed=b''.join(clone.pack() if i==si else z.pack() for i,z in enumerate(sections))
                wanted=bytearray(data);start=s.offset+len(s.header)+off;wanted[start:start+4]=value.to_bytes(4,'little')
                assert packed==wanted and fmt.parse(packed)[si].records==records_
                total+=1;changed+=int(packed!=data);aggregate.update(hashlib.sha256(packed).digest())
            files.append(dict(family=family,file=row['file'],sha256=row['sha256']))
        counts[family]=dict(files=len(rows),identity_records=records,exact_rebuilds=5*len(rows))
    assert total==820 and changed==656 and len(files)==164
    out=dict(scope=__doc__,files=files,unique_source_files=len({r['file'] for r in files}),families=counts,
             identity_records=views,exact_rebuilds=total,changed=changed,identity=total-changed,
             ordered_patch_digest=aggregate.hexdigest(),source_writes=0,
             high_bits_preserved=True,no_new_serializer_normalization=True)
    (ROOT/'phase115-poison-api-validation.json').write_text(json.dumps(out,indent=2),encoding='utf8')
    print(counts,'unique files',out['unique_source_files'],'total',total,'changed',changed)


if __name__=='__main__':main()
