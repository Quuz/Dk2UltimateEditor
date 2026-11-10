"""Survey standalone and embedded C9 placement radius bytes without rewriting.

Embedded slots are inspected at their physical variant offsets;no bounded or
unbounded legacy CString conversion is needed to observe these BYTE fields.
"""
from pathlib import Path
from collections import Counter
import json,hashlib
import dk2_formats as fmt
from inventory import DATA
ROOT=Path(__file__).resolve().parent
files=[];stats={k:Counter() for k in ['Neutral','Good','C9_occupied','C9_empty']};parties=Counter();nonzero=[]
for row in json.loads((ROOT/'corpus-scan.json').read_text())['files']:
    if 'sections' not in row or not any(s['family']=='Things' for s in row['sections']):continue
    path=DATA/'maps'/row['file'];raw=path.read_bytes();sha=hashlib.sha256(raw).hexdigest();assert sha==row['sha256'];local=Counter()
    for section in fmt.parse(raw):
        if section.signature!=190:continue
        for ordinal,record in enumerate(section.records):
            tag,n=fmt.u32(record,0),fmt.u32(record,4);p=record[8:]
            if tag in [198,199]:
                assert n=={198:24,199:32}[tag]
                name='Neutral' if tag==198 else 'Good';value=p[23 if tag==198 else 29];stats[name][value]+=1;local[name]+=1
                if value:nonzero.append(dict(file=str(path),record=ordinal,tag=tag,value=value))
            elif tag==201:
                assert n in [264,268,300,555];start,count,party=(43,16,p[34]) if n==555 else (8 if n==264 else 12,8,p[2]);parties[n]+=1;local['C9']+=1
                for slot in range(count):
                    b=p[start+32*slot:start+32*(slot+1)];name='C9_occupied' if b[28] else 'C9_empty';value=b[29];stats[name][value]+=1;local[name]+=1
                    if value:nonzero.append(dict(file=str(path),record=ordinal,tag=tag,party=party,slot=slot,creature_id=b[28],value=value))
    files.append(dict(file=str(path),sha256=sha,counts=dict(local)))
out=dict(scope=__doc__,files=files,file_count=len(files),radius_byte_counts={k:{str(a):n for a,n in sorted(v.items())} for k,v in stats.items()},party_payload_sizes=dict(parties),nonzero_instances=nonzero)
(ROOT/'phase75-wander-corpus.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8')
print('Wander corpus',len(files),'files',out['radius_byte_counts'],'party sizes',dict(parties),'nonzero',len(nonzero))
