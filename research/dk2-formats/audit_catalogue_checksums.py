"""Verify primary evidence, isolated native executions and Terrain-array preservation."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from native_catalogue_checksums import FAMILIES,definition_checksum,signed_world_accumulate,add_definition_to_family_accumulator
from emulate_catalogue_checksums import build_case

ROOT=Path(__file__).resolve().parent
def load(file):return json.loads((ROOT/file).read_text(encoding='utf8'))
def digest(data):return hashlib.sha256(data).hexdigest()

def main():
    schema=load('catalogue-checksums.json');cases=0
    for family,(length,offset) in FAMILIES.items():
        for value in [0,1,127,128,255]:
            data=bytes([value])*length
            assert definition_checksum(family,data)==value*length;cases+=1
            for previous in [0,1,0xFFFFFFFF]:
                assert definition_checksum(family,data,previous=previous)==value*length;cases+=1
                for gate in [1,0x80000000,0xFFFFFFFF]:
                    assert definition_checksum(family,data,gate_value=gate,previous=previous)==previous;cases+=1
        raw=bytearray(length);raw[0]=127;a=definition_checksum(family,raw);raw[0]=128
        assert definition_checksum(family,raw)-a==1;cases+=1
        # Equal byte sums are not collision-resistant and do not depend on order.
        data=bytes((i*31+19)&255 for i in range(length))
        assert definition_checksum(family,data)==definition_checksum(family,data[::-1]);cases+=1
    for previous in [0,1,0x80000000,0xFFFFFFFF]:
        for data in [b'',b'\xff',b'\x80',b'\x7f',bytes(range(256)),b'\xff'*5000]:
            expected=(previous+sum(struct.unpack('b',bytes([x]))[0] for x in data))&0xFFFFFFFF
            assert signed_world_accumulate(data,previous)==expected;cases+=1
    assert signed_world_accumulate(b'\x80')-signed_world_accumulate(b'\x7f')==0xFFFFFF01;cases+=1
    for previous in [0,1,0xFFFFFFFF]:
        for value in [0,1,0xFFFFFFFF]:
            assert add_definition_to_family_accumulator(previous,value)==(previous+value)&0xFFFFFFFF;cases+=1
    for family,(length,_) in FAMILIES.items():
        for data in [bytes(length-1),bytes(length+1),'x'*length]:
            try:definition_checksum(family,data)
            except ValueError:pass
            else:raise AssertionError('Invalid intermediate buffer accepted')
            cases+=1
    for action in [lambda:definition_checksum('Effects',b''),lambda:definition_checksum('Terrain',bytes(0x2E0),gate_value=True),
            lambda:signed_world_accumulate(b'',-1),lambda:signed_world_accumulate('x'),
            lambda:add_definition_to_family_accumulator(0,1<<32)]:
        try:action()
        except ValueError:pass
        else:raise AssertionError('Invalid checksum input accepted')
        cases+=1
    excerpts=0;instructions=0;hashes={}
    def walk(value):
        nonlocal excerpts,instructions
        if isinstance(value,dict):
            if 'file' in value and ('excerpt' in value or 'instructions' in value):
                path=ROOT/'ghidra'/value['file'];b=path.read_bytes();lines=path.read_text(encoding='utf8').splitlines()
                if 'excerpt' in value:assert lines[value['line']-1].strip()==value['excerpt'];excerpts+=1
                else:assert all(line in lines for line in value['instructions']);instructions+=len(value['instructions'])
                hashes[value['file']]=digest(b)
            for child in value.values():walk(child)
        elif isinstance(value,list):
            for child in value:walk(child)
    walk(schema)
    emulations=load('phase22-checksum-emulations.json')['cases'];assert len(emulations)==53
    for row in emulations:
        b=(ROOT/'ghidra'/row['file']).read_bytes();assert digest(b)==row['sha256'];saved=json.loads(b)
        args,expected,_=build_case(saved['case']);assert args==saved['input'] and expected==saved['expected_registers']
        assert saved['result']['success'] and saved['result']['hit_return']
        assert int(saved['result']['registers']['EAX'],16)==expected['EAX']
    unchanged_disassembly=[]
    for row in schema['families']:
        before=ROOT/'ghidra'/row['evidence']['file'];after=ROOT/'ghidra'/f'phase22-{row["family"].lower()}-checksum-after.txt'
        # The older Rooms export has one extra trailing blank line.
        assert before.read_text().rstrip()==after.read_text().rstrip();unchanged_disassembly.append(row['family'])
    # Record-level corpus observations do not claim to reconstruct intermediate checksums.
    files=[];total=0;nonzero_records=0;slots=[Counter() for _ in range(16)];vectors=Counter()
    for file in load('corpus-scan.json')['files']:
        if 'sections' not in file or not any(s['family']=='Terrain' for s in file['sections']):continue
        b=(DATA/'maps'/file['file']).read_bytes();assert digest(b)==file['sha256'];count=0
        for section in fmt.parse(b):
            if section.signature!=110:continue
            for record in section.records:
                values=struct.unpack_from('<16H',record,0x1AE);vectors[values]+=1;nonzero_records+=any(values)
                for counter,value in zip(slots,values):counter[value]+=1
                total+=1;count+=1
        files.append(dict(file=file['file'],records=count,sha256=file['sha256']))
    patches=0
    for path in [DATA/'Terrain.kwd',DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']:
        original=path.read_bytes();sections=fmt.parse(original)
        for si,section in enumerate(sections):
            if section.signature!=110:continue
            for ri,record in enumerate(section.records):
                for rel,replacement in [(0,struct.pack('<H',0xFFFF)),(30,struct.pack('<H',0x8000)),
                        (0,struct.pack('<16H',*[i*4097 for i in range(16)]))]:
                    records=list(section.records);r=bytearray(record);start=0x1AE+rel;r[start:start+len(replacement)]=replacement;records[ri]=bytes(r)
                    changed=fmt.Section(section.offset,section.signature,section.header,list(section.records),section.suffix)
                    changed.replace_records(records)
                    packed=b''.join(changed.pack() if i==si else s.pack() for i,s in enumerate(sections))
                    expected=bytearray(original);absolute=section.offset+len(section.header)+ri*len(record)+start
                    expected[absolute:absolute+len(replacement)]=replacement
                    assert packed==expected and b''.join(s.pack() for s in fmt.parse(packed))==packed
                    patches+=1
    inputs=[]
    for row in load('validation.json')['files']:
        path=DATA/row['file']
        if not path.exists():path=DATA/'maps'/row['file']
        assert digest(path.read_bytes())==row['sha256'];inputs.append(row['file'])
    executables=load('executable-hash-verification.json')
    for file,sha in executables.items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert package['catalogue_checksums']==schema and len(package['components'])>=33
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=22,date='2026-10-25',boundary_cases=cases,native_fragment_emulations=len(emulations),
        exact_in_memory_container_patches=patches,modified_gameplay_runs=0,
        primary_evidence=dict(excerpts_verified=excerpts,instructions_verified=instructions,sha256=hashes,unchanged_disassembly=unchanged_disassembly),
        terrain_array_corpus=dict(files=len(files),records=total,nonzero_records=nonzero_records,
            distinct_vectors=len(vectors),most_common_vectors=[dict(values=k,count=v) for k,v in vectors.most_common(15)],
            slot_counts=[dict(sorted(c.items())) for c in slots],file_details=files,
            scope='Raw array values only; no raw-to-intermediate checksum reconstruction.'),
        unchanged_inputs=inputs,unchanged_executables=executables,format_package_components=len(package['components']),limitations=schema['limitations'])
    (ROOT/'phase22-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:result[k] for k in ['phase','boundary_cases','native_fragment_emulations','exact_in_memory_container_patches']}))
    print('Terrain array corpus:',len(files),'files;',total,'records;',nonzero_records,'nonzero;',len(vectors),'distinct vectors')
    print('Evidence:',excerpts,'excerpts;',instructions,'instructions;9 functions unchanged after emulation')

if __name__=='__main__':main()
