"""Reject six apparent Misc reads that require exceeding native slot-loop bounds."""
from pathlib import Path
import json,re
from build_effect_semantics import EXE,pe_read
from emulate_effect_mode18 import sha

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def build():
    leads=load('phase143-effect-misc-argument-leads.json')
    hits=[h for h in leads['candidates'] if h['field'] in ['Misc2','Misc3']]
    assert len(hits)==6
    addresses={int(h['read']['address'],16) for h in hits};assert addresses=={0x438B5B,0x438F14,0x439157}
    cube=(ROOT/'ghidra/phase143-cube-bounds-assembly.txt').read_text(encoding='utf8')
    mode=(ROOT/'ghidra/phase143-mode18-assembly.txt').read_text(encoding='utf8')
    lines=[s.strip() for s in (cube+'\n'+mode).splitlines() if s.strip()]
    required=['00438b46: ADD EBX,0x5d','00438b49: MOV dword ptr [ESP + 0x2c],0x8',
        '00438b5b: MOV BX,word ptr [EAX]','00438cca: SUB EDX,0x2','00438ccd: TEST EAX,EAX','00438cd7: JA 0x00438b55',
        '00438eff: ADD EBX,0x5d','00438f02: MOV dword ptr [ESP + 0x30],0x8','00438f14: MOV BX,word ptr [EAX]',
        '0043908a: SUB EDX,0x2','0043908d: TEST EAX,EAX','00439097: JA 0x00438f0e',
        '0043914a: ADD EBP,0x55','0043914d: MOV dword ptr [ESP + 0x14],0x4','00439157: MOV DI,word ptr [EBP]',
        '004391db: SUB EBP,0x2','004391de: DEC EAX','004391df: TEST EAX,EAX','004391e5: JA 0x00439155']
    assert all(s in lines for s in required)
    # Reuse exact whole Cube Ghidra/PE attestation; no new Cube CPU execution.
    old=load('phase141-effect-scale-primary.json');row=next(r for r in old['functions'] if r['address']==0x4386F0)
    saved=load(row['memory']);data=bytes(saved['data'])
    assert sha((ROOT/row['memory']).read_bytes())==row['memory_sha256'] and data==pe_read(EXE.read_bytes(),row['address'],row['length'])
    current=load('phase143-effect-mode18-primary.json');assert any(r['address']==0x439100 for r in current['functions'])
    classified=[]
    for h in hits:
        pc=int(h['read']['address'],16);count,start=(4,0x55) if pc==0x439157 else (8,0x5D)
        offsets=[start-2*i for i in range(count)]
        assert min(offsets)==0x4F and all(o>=0x4F for o in offsets)
        classified.append(dict(root=h['root']['address'],read=h['read']['address'],claimed_field=h['field'],claimed_offset=h['read']['offset'],
            real_slot_offsets=offsets,classification='Infeasible extra iteration: native counter reaches0 before4D/4B.',
            assumption='Valid native stack/ordinarycallee ABI; no external mutation of loop counter. Genericwalker deliberately lacks numericloop bounds.'))
    invalid='ghidra/phase143-mode18-native-a3cd9af77bde.json';probe=load(invalid)
    assert len(bytes.fromhex(next(x['hex'] for x in json.loads(probe['input']['memory'])['regions'] if int(x['address'],16)==0x10500000)))==656
    bad=[x for x in probe['result']['observed_reads'] if 0<int(x['address'],16)<4096]
    assert bad
    return dict(phase=143,scope=__doc__,search=dict(producer_roots=len(leads['producer_roots']),root_states=leads['root_states'],
            limited_roots=leads['limited_roots'],callee_summaries=leads['callee_summaries'],limited_callees=leads['limited_callees'],all_field_leads=len(leads['candidates']),misc_leads=6),
        classifications=classified,canonical_instruction_evidence=required,reused_whole_Cube_attestation=row,
        excluded_initial_probe=dict(file=invalid,sha256=sha((ROOT/invalid).read_bytes()),
            reason='Only4definitions loaded; nativeNext references143/221 missing. CPUreturned but readNULLdefinition fields from mapped lowpage. Semanticinvalid, excluded from accepted12cases/controls.',
            repair='Load complete originalclosure[1,2,3,4,143,221]; acceptedverifier rejects all lowpage accesses except originalFS:[0].'),
        references=[dict(file=n,sha256=sha((ROOT/n).read_bytes())) for n in ['phase143-effect-misc-argument-leads.json','ghidra/phase143-cube-bounds-assembly.txt','ghidra/phase143-mode18-assembly.txt']],
        conclusion='No new genuine Misc2/Misc3 consumer established. Onlysix specific staticleads rejected; other roots/limits/aliases/indirect/indexed paths remainopen. No globalunusedclaim.')

if __name__=='__main__':
    v=build();(ROOT/'phase143-effect-misc-argument-review.json').write_text(json.dumps(v,indent=2),encoding='utf8');print(v['search'])
