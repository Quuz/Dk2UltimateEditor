"""Source pair inventory and exact lossless edits of shared Shot parameters."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
import emulate_shot_combinations as native
ROOT=Path(__file__).resolve().parent
sha=native.sha

def validate():
    refs=json.loads((ROOT/'area-damage-semantics.json').read_text(encoding='utf-8'))['corpus']['files']
    counts=Counter();examples={};byfile=[];records=0
    patches=[dict(Data1=80,Data2=5,ShotProcessFlags=4,ProcessType=5,DamageType=7),
             dict(Data1=80,Data2=5,ShotProcessFlags=4,ProcessType=10,DamageType=7),
             dict(Data1=0xFFFFFFFF,Data2=0x10005,ShotProcessFlags=4,ProcessType=38,DamageType=7)]
    fields={x['name']:x for x in fmt.catalogue_layout('Shots')['fields']}
    assert fields['Data1']['kind']=='u32' and fields['Data2']['kind']=='u32'
    for ref in refs:
        file=Path(ref['file']);data=file.read_bytes();assert sha(data)==ref['sha256']
        sections=fmt.parse(data);si,s=next((i,x) for i,x in enumerate(sections) if x.signature==0x104)
        records+=len(s.records)
        for i,b in enumerate(s.records):
            assert len(b)==239
            pair=(b[0xC7],b[0xC5]);counts[pair]+=1
            key=(b[0xBE],b[:32].split(bytes(1))[0].decode('cp1252',errors='replace'),
                int.from_bytes(b[0xA0:0xA4],'little'),int.from_bytes(b[0x98:0x9C],'little'),int.from_bytes(b[0x9C:0xA0],'little'))
            variants=examples.setdefault(pair,{})
            if key not in variants:variants[key]=dict(shot_ID=key[0],name=key[1],ProcessFlags=key[2],Data1=key[3],Data2=key[4],
                first_source=str(file),first_record_index=i,count=0)
            variants[key]['count']+=1
        edits=[]
        for values in patches:
            raw=s.records[0];edited=raw
            for k,v in values.items():edited=fmt.set_scalar(edited,'Shots',k,v)
            exact=bytearray(raw)
            struct.pack_into('<III',exact,0x98,values['Data1'],values['Data2'],values['ShotProcessFlags'])
            exact[0xC5]=values['DamageType'];exact[0xC7]=values['ProcessType'];assert edited==exact
            restored=edited
            for k in values:
                f=fields[k];restored=fmt.set_scalar(restored,'Shots',k,int.from_bytes(raw[f['offset']:f['offset']+f['size']],'little'))
            assert restored==raw
            rr=list(s.records);rr[0]=edited
            clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
            packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(sections))
            wanted=bytearray(data);start=s.offset+len(s.header);wanted[start:start+239]=edited
            assert packed==wanted
            doc=fmt.decoded(clone);assert fmt.rebuild_decoded(dict(sections=[doc]))==clone.pack()
            edits.append(dict(values=values,sha256=sha(packed),changed_bytes=sum(a!=b for a,b in zip(data,packed))))
        assert sha(file.read_bytes())==ref['sha256'];byfile.append(dict(**ref,edits=edits))
    assert len(refs)==82 and records==5551 and len(counts)==49
    pairs=[]
    for pair,count in sorted(counts.items()):
        cases=[c['name'] for c in native.cases() if (c['process'],c['mode'])==pair]
        pairs.append(dict(ProcessType=pair[0],DamageType=pair[1],source_records=count,
            variants=list(examples[pair].values()),phase168_native_cases=cases,
            status='selected_diagnostic_collision_scenes' if cases else 'not_executed_in_phase168',
            scope='Case membership is bounded evidence for selected data/events/targets; not all source variants or pair combinations. Earlier individual-handler evidence is preserved separately.'))
    mixed=[dict(ProcessType=p,DamageType=d,source_records=v) for (p,d),v in sorted(counts.items()) if p and d]
    assert len(mixed)==14
    return dict(phase=168,files=byfile,source_files=len(refs),source_records=records,observed_pairs=pairs,mixed_pairs=mixed,
        source_scope='All82 supplied readable Shot containers, including editor globals and map/mod variants. This is not a claim that all are shipped Bullfrog definitions.',
        observed_pair_count=49,mixed_pair_count=14,new_exact_container_edits=len(refs)*len(patches),public_decode_rebuild_controls=len(refs)*len(patches),
        raw_fields=dict(Data1_u32=0x98,Data2_u32=0x9C,ProcessFlags_u32=0xA0,DamageType_u8=0xC5,ProcessType_u8=0xC7),
        all_other_bytes_preserved=True,inverse_patches_restore_original=True,source_writes=0,
        codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())))

if __name__=='__main__':
    d=validate();(ROOT/'phase168-shot-combinations-storage.json').write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
    print(d['observed_pair_count'],'pairs',d['mixed_pair_count'],'mixed',d['new_exact_container_edits'],'exact edits')
