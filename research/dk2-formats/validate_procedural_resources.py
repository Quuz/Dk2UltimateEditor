"""Survey type7 resources and verify exact edits in complete original containers."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
import native_procedural_resources as api

ROOT=Path(__file__).resolve().parent
def sha(b):return hashlib.sha256(b).hexdigest()
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))


def sources():
    out={r['file']:r['sha256'] for n in ['phase114-shot-damage-api-validation.json','phase103-resource-corpus.json'] for r in load(n)['files']}
    for scope in load('effect-catalogue-audit.json')['scopes']:
        for row in scope['inputs']:
            if Path(row['path']).name=='Effects.kwd':out[row['path']]=row['sha256']
    return [dict(file=p,sha256=s) for p,s in sorted(out.items())]


def validate():
    families={0x104:'Shots',0x10E:'Effects',0xFA:'EffectElements'}
    files=sources();counts=Counter();ids={k:Counter() for k in families.values()}
    rebuilds=changed=seen=selected=0;digest=hashlib.sha256();examples=[]
    for source in files:
        data=Path(source['file']).read_bytes();assert sha(data)==source['sha256']
        sections=fmt.parse(data);assert b''.join(s.pack() for s in sections)==data
        for si,s in enumerate(sections):
            if s.signature not in families:continue
            family=families[s.signature]
            counts[family+'_files']+=1
            for ri,raw in enumerate(s.records):
                counts[family+'_records']+=1;seen+=1
                if raw[0x70]!=7:continue
                selected+=1;counts[family+'_type7_records']+=1
                art=raw[0x20:0x74];view=api.view_art(art)
                ids[family][view['procedure_id']]+=1
                assert api.patch_art(art,**{k:view[k] for k in ['procedure_id','opaque_1','opaque_2']})==art
                if len(examples)<12:
                    examples.append(dict(file=source['file'],family=family,record=ri,name=fmt.string(raw[:32]),**view))
                edits=[dict(procedure_id=view['procedure_id']^0x80000000),
                    dict(opaque_1=view['opaque_1']^0xFFFFFFFF),dict(opaque_2=view['opaque_2']^0x80000000),{}]
                for edit in edits:
                    patched=api.patch_art(art,**edit);record=raw[:0x20]+patched+raw[0x74:]
                    records=list(s.records);records[ri]=record
                    clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix)
                    clone.replace_records(records)
                    packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
                    expected=bytearray(data)
                    start=s.offset+len(s.header)+ri*len(raw)+0x20
                    expected[start:start+84]=patched
                    assert packed==expected and fmt.parse(packed)[si].records==records
                    # Changed values may alter 1..4 bytes; only the selected DWORD is editable.
                    allowed=set()
                    for k in edit:
                        o={'procedure_id':68,'opaque_1':72,'opaque_2':76}[k]
                        allowed.update(range(start+o,start+o+4))
                    assert all(i in allowed for i,(a,b) in enumerate(zip(data,packed)) if a!=b)
                    rebuilds+=1;changed+=packed!=data;digest.update(hashlib.sha256(packed).digest())
        assert sha(Path(source['file']).read_bytes())==source['sha256']
    good=bytearray(84);good[80]=7
    bad=[lambda:api.view_art(bytes(83)),lambda:api.view_art(bytes(85)),lambda:api.view_art(bytes(84)),
        lambda:api.patch_art(good,procedure_id=-1),lambda:api.patch_art(good,procedure_id=0x100000000),
        lambda:api.patch_art(good,procedure_id=True),lambda:api.patch_art(good,opaque_1='0'),
        lambda:api.patch_art(good,opaque_2=1.5)]
    for call in bad:
        try:call()
        except ValueError:pass
        else:raise AssertionError('Invalid input accepted')
    assert counts['EffectElements_type7_records']==0 and rebuilds==selected*4 and changed==selected*3
    return dict(phase=129,files=files,counts=dict(sorted(counts.items())),records_seen=seen,type7_records=selected,
        procedure_id_counts={k:{str(a):b for a,b in sorted(v.items())} for k,v in ids.items()},examples=examples,
        exact_container_rebuilds=rebuilds,changed_containers=changed,identity_containers=rebuilds-changed,
        API_refusals=len(bad),ordered_patch_digest=digest.hexdigest(),source_writes=0,
        scope='All type7 records in the established Shot/Effects/Element source corpus. Each selected DWORD is changed separately plus identity; other complete-container bytes, unknowns, headers and tails remain exact. API rebuilds are not original editor saves.')


if __name__=='__main__':
    value=validate();(ROOT/'phase129-procedural-api-validation.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Sources',len(value['files']),'records',value['records_seen'],'type7',value['type7_records'],'rebuilds',value['exact_container_rebuilds'])
