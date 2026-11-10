"""Verify native AP consumer snapshots and exact raw flag edits in Things."""
from pathlib import Path
import json,struct
import dk2_formats as fmt
from inventory import DATA
from native_action_points import patch_raw,record_view
from native_action_point_visibility import reveal_projection,toolbox_id
from emulate_action_point_visibility import cases,build_case
from build_action_point_visibility import primary,corpus,digest
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('action-point-visibility-semantics.json');assert s['primary_program_bytes']==primary() and s['corpus']==corpus();assert len(s['native_emulations'])==len(cases())==36
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];assert digest(p.read_bytes())==row['sha256'];v=json.loads(p.read_text());args,want=build_case(v['case']);r=v['result'];assert args==v['input'] and want==v['expected_registers'] and r['success'] and r['hit_return'];assert all(int(r['registers'][k],16)==n for k,n in want.items())
    checks=0;cells=bytes(4*4*16)
    for flags in range(256):
        ps=[dict(flags=flags,id=255,bounds=(1,1,2,2))];out,events,refresh,rooms=reveal_projection(4,4,cells,ps);expected=bytearray(cells)
        if flags&16:
            for x,y in [(1,1),(2,1),(1,2),(2,2)]:struct.pack_into('<H',expected,(y*4+x)*16+8,0x2000)
        assert out==expected and len(events)==(5 if flags&16 else 0) and len(refresh)==(4 if flags&16 else 0) and not rooms;assert toolbox_id(ps)==(255 if flags&32 else 0);checks+=1
        b,e,r,q=reveal_projection(4,4,cells,ps,64);assert b==cells and e==r==q==[];checks+=1
    for x0 in range(4):
        for y0 in range(4):
            for x1 in range(x0,4):
                for y1 in range(y0,4):
                    p=dict(flags=16,id=7,bounds=(x0,y0,x1,y1));out,e,r,q=reveal_projection(4,4,cells,[p,p]);area=(x1-x0+1)*(y1-y0+1);assert len(r)==area and len(e)==area+2 and not q;assert sum(struct.unpack_from('<H',out,i*16+8)[0]==0x2000 for i in range(16))==area;checks+=1
    for t in range(10):
        b=bytearray(cells);struct.pack_into('<H',b,(1*4+1)*16+2,5);out,e,r,q=reveal_projection(4,4,b,[dict(flags=16,id=7,bounds=(1,1,1,1))],room_types={5:t});special=t in [3,4,8];assert len(q)==int(special) and len(r)==int(not special) and len(e)==(1 if special else 2);checks+=1
    for bounds in [(0,0,4,0),(0,0,0,4),(65535,0,65535,0)]:
        try:reveal_projection(4,4,cells,[dict(flags=16,id=1,bounds=bounds)])
        except ValueError:checks+=1
        else:raise AssertionError('Unsafe map bounds accepted')
    for row in s['corpus']['files']:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'] and b''.join(q.pack() for q in fmt.parse(b))==b
    patches=0
    for p in [DATA/'AutoSaveThings.kld',DATA/'maps/level1Things.kld',DATA/'maps/kop4Things.kld']:
        original=p.read_bytes();parts=fmt.parse(original);si=next(i for i,q in enumerate(parts) if q.signature==190);sec=parts[si];selected=[i for i,r in enumerate(sec.records) if fmt.u32(r,0)==197]
        for ri in [selected[0],selected[-1]]:
            raw=sec.records[ri];old=record_view(raw)['flags']
            for mask in [4,8,16,32,0x8000]:
                for enabled in [False,True]:
                    value=old|mask if enabled else old&~mask;changed=patch_raw(raw,18,value);rs=list(sec.records);rs[ri]=changed;updated=fmt.Section(sec.offset,sec.signature,sec.header,list(sec.records),sec.suffix,sec.stride);updated.replace_records(rs);packed=b''.join(updated.pack() if j==si else q.pack() for j,q in enumerate(parts));absolute=sec.offset+len(sec.header)+sum(len(r) for r in sec.records[:ri])+8+18;wanted=bytearray(original);wanted[absolute:absolute+2]=value.to_bytes(2,'little');assert packed==wanted and b''.join(q.pack() for q in fmt.parse(packed))==packed;patches+=1
    for row in load('validation.json')['files']:
        p=DATA/row['file']
        if not p.exists():p=DATA/'maps'/row['file']
        assert digest(p.read_bytes())==row['sha256']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    package=load('editor-format-package.json');assert len(package['components'])>=56 and package['action_point_visibility_semantics']==s
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    result=dict(phase=45,native_emulations=36,native_reveal_fragments=30,native_toolbox_functions=6,api_boundary_cases=checks,exact_in_memory_container_patches=patches,corpus_files=s['corpus']['file_count'],corpus_records=s['corpus']['records'],whole_file_roundtrips=s['corpus']['file_count'],reveal_records=s['corpus']['reveal_records'],toolbox_records=s['corpus']['toolbox_records'],primary_ranges_matching_pe=len(s['primary_program_bytes']),evidence_instructions=sum(len(e['instructions']) for e in s['evidence']),original_main_inputs_unchanged=17,original_main_executables_unchanged=2,format_package_components=len(package['components']),modified_gameplay_runs=0,limitations=s['limitations'])
    (ROOT/'phase45-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in result.items() if k!='limitations'}))
if __name__=='__main__':main()
