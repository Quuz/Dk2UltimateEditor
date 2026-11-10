"""Read-only structural survey of all original editor map files."""
from collections import Counter,defaultdict
from pathlib import Path
import hashlib,json
import dk2_formats as fmt
from inventory import DATA

ROOT=Path(__file__).resolve().parent
def main():
    directory=DATA/'maps'
    files=sorted(p for p in directory.iterdir() if p.is_file() and p.suffix.lower() in ('.kwd','.kld'))
    rows=[];errors=[];strides=defaultdict(Counter);thing_sizes=defaultdict(Counter);level_sizes=Counter();families=Counter();total=0
    for path in files:
        b=path.read_bytes();total+=len(b)
        row=dict(file=path.name,size=len(b),sha256=hashlib.sha256(b).hexdigest())
        try:
            sections=fmt.parse(b)
            assert b''.join(s.pack() for s in sections)==b
            row['sections']=[]
            for s in sections:
                family=fmt.NAMES.get(s.signature,hex(s.signature));families[family]+=1
                entry=dict(offset=s.offset,family=family,size=len(s.pack()))
                if s.signature in fmt.FAMILIES:
                    strides[family][str(s.stride)]+=1
                    entry.update(count=len(s.records),stride=s.stride)
                elif s.signature==0xBE:
                    for r in s.records:thing_sizes[hex(fmt.u32(r))][str(fmt.u32(r,4))]+=1
                    entry['count']=len(s.records)
                elif s.signature==0xDC:
                    size=fmt.u32(s.header,len(s.header)-4);level_sizes[str(size)]+=1
                    entry['property_bytes']=size
                elif s.signature==0x64:entry.update(width=fmt.u32(s.header,20),height=fmt.u32(s.header,24))
                row['sections'].append(entry)
        except Exception as e:
            row['error']=str(e);errors.append(dict(file=path.name,error=str(e)))
        rows.append(row)
    result=dict(directory=str(directory.resolve()),total_files=len(rows),total_bytes=total,
        structurally_parsed=len(rows)-len(errors),errors=errors,families=dict(families),
        catalogue_strides={k:dict(v) for k,v in strides.items()},things_payload_sizes={k:dict(v) for k,v in thing_sizes.items()},
        level_property_sizes=dict(level_sizes),files=rows,
        note='Container bounds/record lengths surveyed. Players remain raw; Triggers have tagged envelopes/common fields and pinned-source parameter schemas. No full gameplay-semantic coverage claim.')
    (ROOT/'corpus-scan.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Corpus:',result['total_files'],'files;',result['structurally_parsed'],'parsed;',len(errors),'errors;',total,'bytes')
    print('Strides:',result['catalogue_strides']);print('Level sizes:',result['level_property_sizes']);print('Things:',result['things_payload_sizes'])
    for e in errors[:15]:print(e)

if __name__=='__main__':main()
