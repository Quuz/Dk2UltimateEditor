"""Exact trigger RepeatTimes and AP flag edits through existing lossless codec.

Writes research report only. Diagnostic edited containers stay in memory;
headers, counts, links, padding, unknown bits and other records must match.
"""
from pathlib import Path
from collections import Counter
import json,hashlib
import dk2_formats as fmt
from native_action_points import patch_raw
ROOT=Path(__file__).resolve().parent
MAPS=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/maps'
sha=lambda b:hashlib.sha256(b).hexdigest()

def validate():
    refs=[];stats={};digest=hashlib.sha256();sources={}
    survey=json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'))
    triggers=[]
    for row in survey['files']:
        if 'error' in row:continue
        p=MAPS/row['file'];data=p.read_bytes();assert sha(data)==row['sha256']
        if any(s.signature==0xD2 for s in fmt.parse(data)):triggers.append(dict(file=str(p),sha256=sha(data)))
    aps=json.loads((ROOT/'ap-lookup-review.json').read_text(encoding='utf8'))['corpus']['files']
    for family,rows,sig in [('Triggers',triggers,0xD2),('ActionPoints',aps,0xBE)]:
        records=edits=empty=0;dist=Counter();file_rows=[]
        for row in rows:
            p=Path(row.get('input_path',row['file']));data=p.read_bytes();assert sha(data)==row['sha256'];parts=fmt.parse(data)
            si,s=next((i,s) for i,s in enumerate(parts) if s.signature==sig)
            indices=[i for i,r in enumerate(s.records) if family=='Triggers' or fmt.u32(r)==197]
            if not indices:
                assert family=='Triggers';empty+=1
                assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s)]))==s.pack()
                assert b''.join(x.pack() for x in parts)==data
                sources[str(p)]=row['sha256'];file_rows.append(dict(file=str(p),sha256=row['sha256'],records=0));continue
            for i in indices:
                r=s.records[i];records+=1
                dist[str(r[23] if family=='Triggers' else fmt.u16(r,26)&12)]+=1
            ri=indices[0];raw=s.records[ri];off=23 if family=='Triggers' else 26
            for value in ([0,1,2,255] if family=='Triggers' else [0,4,8,12]):
                if family=='Triggers':edited=raw[:off]+bytes([value])+raw[off+1:]
                else:
                    word=(fmt.u16(raw,26)&~12)|value;edited=patch_raw(raw,0x12,word)
                rr=list(s.records);rr[ri]=edited;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix,s.stride);clone.replace_records(rr)
                packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts))
                at=s.offset+len(s.header)+sum(map(len,s.records[:ri]))+off
                expected=data[:at]+edited[off:off+(1 if family=='Triggers' else 2)]+data[at+(1 if family=='Triggers' else 2):]
                assert packed==expected and clone.header==s.header
                assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(clone)]))==clone.pack()
                restored=bytearray(packed);restored[at:at+(1 if family=='Triggers' else 2)]=raw[off:off+(1 if family=='Triggers' else 2)];assert restored==data
                digest.update(hashlib.sha256(packed).digest());edits+=1
            assert sha(p.read_bytes())==row['sha256'];sources[str(p)]=row['sha256']
            file_rows.append(dict(file=str(p),sha256=row['sha256'],records=len(indices)))
        stats[family]=dict(files=len(rows),empty_files_identity_only=empty,records=records,exact_container_edits=edits,value_distribution=dict(dist));refs.extend(dict(family=family,**r) for r in file_rows)
    return dict(phase=156,counts=stats,files=refs,source_files=[dict(file=p,sha256=h) for p,h in sources.items()],
        exact_container_edits=sum(s['exact_container_edits'] for s in stats.values()),
        codec=dict(file='dk2_formats.py',sha256=sha((ROOT/'dk2_formats.py').read_bytes())),
        ordered_patch_digest=digest.hexdigest(),all_other_bytes_preserved=True,headers_and_links_preserved=True,
        public_decode_rebuild_all_passed=True,inverse_edits_restore_source=True,source_writes=0,
        scope='Storage tests use real full containers; no game run or valid new gameplay graph implied. AutoSave AP uses explicitly named frozen phase47 snapshot, not live AutoSave proof.')
if __name__=='__main__':
    d=validate();(ROOT/'phase156-AP-tree-storage-validation.json').write_text(json.dumps(d,indent=2),encoding='utf8');print(d['counts']);print('Exact edits',d['exact_container_edits'])
