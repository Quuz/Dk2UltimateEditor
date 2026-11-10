"""Audit native pipeline supplement, metamorphic alternative fields and raw safety."""
from pathlib import Path
from collections import defaultdict
import json,struct
import dk2_formats as fmt
from build_spell_catalogue_validation import ROOT,load,digest,primary,corpus
from emulate_spell_catalogue import cases,build_case,LOOKUP_IDS
from native_spell_catalogue import lookup_index
from native_spell_writer import patch_raw
def main():
    s=load('spell-catalogue-validation.json');assert s['primary_program_bytes']==primary() and s['corpus']==corpus();assert len(cases())==len(s['native_emulations'])==18
    groups=defaultdict(list);getter_calls=0
    for row,c in zip(s['native_emulations'],cases()):
        p=ROOT/'ghidra'/row['file'];v=load('ghidra/'+row['file']);args,want=build_case(c);r=v['result'];assert 'catalogue-v3-' in row['file'] and digest(p.read_bytes())==row['sha256'] and c==v['case'] and args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==n for k,n in want.items());m={int(x['address'],16):bytes.fromhex(x['hex']) for x in json.loads(args['memory'])['regions']}
        assert all(a not in m for a in [0x56A6D0,0x56A490,0x56A560,0x51FE00,0x4F7860,0x50DFA0])
        getter_calls+=len(LOOKUP_IDS)
        if c['name']=='ids-0-1-2':assert m[0x10037000][:12]==bytes(12)
        if c['name'].startswith('pipeline-'):
            definitions=bytearray(m[0x10003000]);sums=[struct.unpack_from('<I',definitions,70*i+27)[0] for i in range(3)]
            for i in range(3):definitions[70*i+27:70*i+31]=bytes(4)
            groups[c['gate'],c['update']].append(dict(pair=(c['shot'],c['room']),definitions=bytes(definitions),checksums=sums,world=m[0x10020000]))
    assert len(groups)==4 and all(len(v)==3 and len({r['definitions'] for r in v})==1 for v in groups.values())
    for (gate,update),rows in groups.items():
        bypair={r['pair']:r for r in rows};base=bypair[0,0]
        for pair,row in bypair.items():
            assert all(a-b==(0 if gate else sum(pair)) for a,b in zip(row['checksums'],base['checksums']))
            before=struct.unpack_from('<I',base['world'],0x1620)[0];after=struct.unpack_from('<I',row['world'],0x1620)[0]
            assert (after-before)&0xFFFFFFFF==(0 if gate or update else 3*sum(pair))
    patches=records=roundtrips=0
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);assert b''.join(q.pack() for q in parts)==b;roundtrips+=1
        for si,sec in enumerate(parts):
            if sec.signature!=160:continue
            rel=len(sec.header)
            for ri,raw in enumerate(sec.records):
                records+=1
                if raw[232] or raw[233]:
                    for off in [232,233]:
                        changed=patch_raw(raw,off,raw[off]^255);rs=list(sec.records);rs[ri]=changed;new=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);new.replace_records(rs);packed=b''.join(new.pack() if j==si else q.pack() for j,q in enumerate(parts));expected=bytearray(b);expected[sec.offset+rel+off]=changed[off]
                        assert packed==expected and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
                rel+=len(raw)
    api=0
    for ids,ident,expect in [([],0,None),([12,12,24],12,1),([0,1,2],0,None),([0,1,2],1,None),([0,1,2],2,None),([1,2,3],2,1),([12,24,45],45,2)]:assert lookup_index(ids,ident)==expect;api+=1
    for bad in [-1,256,True,'1']:
        try:lookup_index([1,2,3],bad)
        except ValueError:api+=1
        else:raise AssertionError(bad)
    for row in s['xrefs']:assert digest((ROOT/'ghidra'/row['file']).read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    pack=load('editor-format-package.json');assert len(pack['components'])==65 and pack['spell_alternative_semantics']['phase55_pipeline']==s
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==pack[key]
    v=dict(phase=55,date='2026-10-27',native_parser_executions=18,native_input_records=54,native_lookup_calls=getter_calls,complete_primary_pe_ranges=12,metamorphic_alternative_groups=4,overlapping_native_trap_initializer_tests=2,complete_cworld_bytes_per_case=0xA3CC,whole_file_roundtrips=roundtrips,creature_spell_records=records,exact_in_memory_alternative_patches=patches,api_checks=api,original_executables_unchanged=4,package_components=65,new_components=0,research_writes_to_game_data=0,modified_gameplay_runs=0,limitations=s['limitations'])
    assert patches==314 and records==3535 and getter_calls==126
    (ROOT/'phase55-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:n for k,n in v.items() if k!='limitations'}))
if __name__=='__main__':main()
