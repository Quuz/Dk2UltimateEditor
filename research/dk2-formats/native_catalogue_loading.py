"""Read-only projection of native ID placement and lookup, not gameplay emulation.

Supply the files visited in a single insertion/count scope, in loading order.
This does not resolve descriptor references, simulate runtime payloads, resource
loading, checksums, update mode, allocation failure or integer-size overflow.
"""
from collections import Counter
from pathlib import Path
import argparse,hashlib,json
import dk2_formats as fmt

ROOT=Path(__file__).resolve().parent
SCHEMA=json.loads((ROOT/'catalogue-loading.json').read_text(encoding='utf8'))

def effective_key(family,row):
    if family=='Creatures' and row.get('unique'):return row['clone_id']
    return row['id']

def lookup_slot(slots,ident,family,ordinary_creature=False):
    """The inspected backward search. Returns a zero-based runtime slot or None."""
    ident &= 255
    index=min(ident-1,len(slots)-1)
    while index>=0:
        key=effective_key(family,slots[index]) if ordinary_creature else slots[index]['id']
        if key==ident:return index
        if key<ident:return None
        index-=1
    return None

def project_records(family,rows,header_count=None):
    """Project only ID metadata. Caller may pass the total counted N explicitly."""
    rule=SCHEMA['families'][family]
    count=len(rows) if header_count is None else header_count
    if count<0:raise ValueError('Negative header count')
    for row in rows:
        if not 0<=row['id']<=255:raise ValueError('ID does not fit native byte')
        if family=='Creatures' and row.get('unique') and not 0<=row['clone_id']<=255:
            raise ValueError('Clone ID does not fit native byte')
    findings=[];slots=[];writes={};capacity=count+(family=='Terrain')
    for row in rows:
        if family=='Terrain':
            if row['id']>=capacity:
                findings.append(dict(code='terrain_id_outside_counted_allocation',id=row['id'],
                    allocated_slots=capacity,record=row,
                    note='Native direct write has no capacity guard; a larger actual count scope can change this result.'))
            else:writes.setdefault(row['id'],[]).append(row)
        elif len(slots)>=capacity:
            findings.append(dict(code='record_ignored_at_capacity',record=row))
        else:
            index=len(slots)
            while index and effective_key(family,slots[index-1])>=row['id']:index-=1
            slots.insert(index,row)
    counts=Counter(row['id'] for row in rows)
    duplicates=[dict(id=k,count=v) for k,v in sorted(counts.items()) if v>1]
    if duplicates:findings.append(dict(code='duplicate_definition_ids',ids=duplicates,
        note='Duplicate insertion is not an update. Runtime loaders may retain prior fields; no first/last-wins payload claim is made.'))
    queries={};ordinary={}
    if family!='Terrain':
        if 0 in counts:findings.append(dict(code='zero_id_not_returned_by_lookup',count=counts[0]))
        for ident in sorted(counts):
            slot=lookup_slot(slots,ident,family)
            queries[str(ident)]=dict(slot=slot,record=None if slot is None else slots[slot])
            if slot is None:findings.append(dict(code='loaded_id_not_found',id=ident))
        if family=='Creatures':
            for ident in sorted({effective_key(family,row) for row in rows}):
                slot=lookup_slot(slots,ident,family,ordinary_creature=True)
                ordinary[str(ident)]=dict(slot=slot,record=None if slot is None else slots[slot])
    return dict(family=family,header_count=count,allocated_slots=capacity,record_count=len(rows),
        input_ids=[row['id'] for row in rows],runtime_ids=[row['id'] for row in slots],
        terrain_slot_write_order={str(k):v for k,v in sorted(writes.items())},
        lookup_by_raw_id=queries,creature_ordinary_lookup=ordinary,findings=findings)

def project_files(paths):
    """Explicit scope only; paths are ordered and are never modified."""
    rows={};counts=Counter();inputs=[]
    for path in map(Path,paths):
        data=path.read_bytes();inputs.append(dict(path=str(path.resolve()),sha256=hashlib.sha256(data).hexdigest()))
        for section_index,section in enumerate(fmt.parse(data)):
            family=fmt.FAMILIES.get(section.signature)
            if family not in SCHEMA['families']:continue
            rule=SCHEMA['families'][family];counts[family]+=len(section.records)
            for index,raw in enumerate(section.records):
                row=dict(id=raw[rule['raw_id_offset']],name=fmt.string(raw[:32]),
                    path=str(path.resolve()),section=section_index,record=index)
                if family=='Creatures':row.update(unique=bool(fmt.u32(raw,0xEFC)&0x08000000),clone_id=raw[0xD3B])
                rows.setdefault(family,[]).append(row)
    return dict(scope='Only the supplied files, in the supplied order; caller must establish the real count/load scope.',
        inputs=inputs,families={family:project_records(family,values,counts[family]) for family,values in rows.items()})

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('paths',nargs='+',type=Path);parser.add_argument('--output',required=True,type=Path)
    args=parser.parse_args();result=project_files(args.paths)
    args.output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    for family,row in result['families'].items():
        print(family,row['record_count'],'records;',len(row['findings']),'findings')

if __name__=='__main__':main()
