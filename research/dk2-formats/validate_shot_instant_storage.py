"""Exact in-memory edits of Shot39 pair and Trap shared firing parameters."""
from pathlib import Path
import json,struct
import dk2_formats as fmt
import emulate_shot_instant_experience as native
ROOT=Path(__file__).resolve().parent
sha=native.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))

def validate():
    specs=[('Shots',0x104,load('area-damage-semantics.json')['corpus']['files'],
        [dict(Data1=50,Data2=0,ShotProcessFlags=4,DamageType=7,ProcessType=7),
         dict(Data1=80,Data2=2,ShotProcessFlags=2,DamageType=2,ProcessType=34),
         dict(Data1=80,Data2=0,ShotProcessFlags=2,DamageType=7,ProcessType=34)]),
        ('Traps',0x82,load('trap-creation-activation-semantics.json')['storage_validation']['files'],
        [dict(TrapId=17,ShotTypeId=58,ShotData1=80,ShotData2=2),
         dict(TrapId=17,ShotTypeId=89,ShotData1=80,ShotData2=0)])]
    out=[];total=0;records={}
    for family,signature,refs,patches in specs:
        fields={f['name']:f for f in fmt.catalogue_layout(family)['fields']};rows=[];count=0
        assert len(refs)==82
        for ref in refs:
            path=Path(ref['file']);data=path.read_bytes();assert sha(data)==ref['sha256']
            sections=fmt.parse(data);si,s=next((i,s) for i,s in enumerate(sections) if s.signature==signature);count+=len(s.records)
            edits=[]
            for values in patches:
                raw=s.records[0];edited=raw;exact=bytearray(raw)
                for k,v in values.items():
                    edited=fmt.set_scalar(edited,family,k,v);f=fields[k]
                    exact[f['offset']:f['offset']+f['size']]=v.to_bytes(f['size'],'little')
                assert edited==exact
                reverse=edited
                for k in values:
                    f=fields[k];reverse=fmt.set_scalar(reverse,family,k,int.from_bytes(raw[f['offset']:f['offset']+f['size']],'little'))
                assert reverse==raw
                rr=list(s.records);rr[0]=edited
                clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
                packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
                wanted=bytearray(data);start=s.offset+len(s.header);wanted[start:start+len(raw)]=edited
                assert packed==wanted and fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
                edits.append(dict(values=values,packed_sha256=sha(packed),changed_bytes=sum(a!=b for a,b in zip(data,packed))))
                total+=1
            assert sha(path.read_bytes())==ref['sha256'];rows.append(dict(file=str(path),sha256=ref['sha256'],edits=edits))
        records[family]=count;out.append(dict(family=family,source_files=82,source_records=count,files=rows))
    assert records==dict(Shots=5551,Traps=1293) and total==410
    return dict(phase=170,families=out,records=records,exact_container_edits=total,public_decode_rebuild_controls=total,
        all_other_bytes_preserved=True,inverse_patches_restore_source=True,source_writes=0,
        codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())),
        scope='Physicalexactfieldeditsonly; runtimeData2overridesarenotserializedbackintothesourcebythesescenes.')

if __name__=='__main__':
    d=validate();(ROOT/'phase172-instant-xp-storage.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
    print(d['exact_container_edits'],'exact edits',d['records'])
