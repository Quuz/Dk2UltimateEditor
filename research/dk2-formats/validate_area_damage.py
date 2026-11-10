"""Source mode census and exact whole-container edits for newly executed modes.

One Shot17 per source container: native physical damage mode/Data fields are
edited for three diagnostic modes, preserving every other byte. No source writes.
"""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
import native_shot_damage as api

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()


def validate():
    files=json.loads((ROOT/'phase114-shot-damage-api-validation.json').read_text())['files']
    counts=Counter();examples=[];rebuilds=changed=records_seen=0;digest=hashlib.sha256()
    for source in files:
        data=Path(source['file']).read_bytes();assert sha(data)==source['sha256']
        sections=fmt.parse(data);assert b''.join(s.pack() for s in sections)==data
        selected=[]
        for si,s in enumerate(sections):
            if s.signature!=0x104:continue
            for ri,raw in enumerate(s.records):
                view=api.view(raw);counts[view['damage_type']]+=1;records_seen+=1
                if raw[0xBE]==17:selected.append((si,ri,raw))
        assert len(selected)==1,source['file']
        si,ri,raw=selected[0];s=sections[si]
        for mode,data2 in [(2,2),(3,8192),(4,16384)]:
            patched=api.patch(raw,damage_type=mode,data1=80,data2=data2)
            assert api.view(patched)==dict(api.view(raw),damage_type=mode,data1=80,data2=data2)
            expected_record=bytearray(raw);expected_record[0xC5]=mode
            expected_record[0x98:0xA0]=(80).to_bytes(4,'little')+data2.to_bytes(4,'little')
            assert patched==expected_record
            records=list(s.records);records[ri]=patched
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
            packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
            start=s.offset+len(s.header)+ri*239;expected=bytearray(data);expected[start:start+239]=patched
            assert packed==expected and fmt.parse(packed)[si].records==records
            rebuilds+=1;changed+=packed!=data;digest.update(hashlib.sha256(packed).digest())
        if len(examples)<5:examples.append(dict(file=source['file'],ShotId=17,name=fmt.string(raw[:32]),**api.view(raw)))
        assert sha(Path(source['file']).read_bytes())==source['sha256']
    assert counts[3]==counts[4]==0 and len(files)==82 and rebuilds==246 and changed==246
    return dict(phase=131,files=files,records_seen=records_seen,damage_mode_counts={str(k):v for k,v in sorted(counts.items())},
        diagnostic_modes_not_observed_in_corpus=[3,4],examples=examples,exact_container_rebuilds=rebuilds,
        changed_containers=changed,identity_containers=0,ordered_patch_digest=digest.hexdigest(),source_writes=0,
        scope=__doc__)


if __name__=='__main__':
    value=validate();(ROOT/'phase131-area-api-validation.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Sources',len(value['files']),'records',value['records_seen'],'modes',value['damage_mode_counts'],'exact rebuilds',value['exact_container_rebuilds'])
