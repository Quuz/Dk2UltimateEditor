"""Calibrate read candidates against native instructions and corrected passes."""
from pathlib import Path
from collections import Counter
import json,hashlib,capstone
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from native_memory_access import memory_read_reason
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def main():
    dx=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();old=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    rows=[]
    for program,data,a,wanted in [('DX',dx,0x510E36,True),('old',old,0x423645,True),('DX',dx,0x46122C,True),('DX',dx,0x461236,True),('DX',dx,0x4B0438,False),('DX',dx,0x461246,False),('DX',dx,0x426C44,False)]:
        ins=next(md.disasm(pe_read(data,a,15),a));mem=[o for o in ins.operands if o.type==X86_OP_MEM];assert len(mem)==1;reason=memory_read_reason(ins,mem[0]);assert bool(reason)==wanted
        rows.append(dict(program=program,address=hex(a),hex=bytes(ins.bytes).hex(),instruction=ins.mnemonic+' '+ins.op_str,capstone_access=mem[0].access,reason=reason,expected_data_read=wanted))
    assert rows[0]['capstone_access']==rows[1]['capstone_access']==0 and rows[0]['reason']==rows[1]['reason']=='opcode_read_fallback'
    before=load('phase63-old-creature-argument-leads.json');after=load('phase64-old-creature-argument-leads-v2.json');key=lambda h:(h['root']['address'],h['field'],h['read']['address'],h['read']['callee'],h['read']['call']);known={key(h) for h in before['candidates']};added=[h for h in after['candidates'] if key(h) not in known]
    assert len(before['producer_roots'])==len(after['producer_roots'])==822 and before['root_states']==after['root_states']==14506 and len(added)==21
    assert Counter(h['field'] for h in after['candidates'])==dict(controlCreatureFlags=225,controlHealth=131)
    assert {key(h) for h in before['candidates']}<={key(h) for h in after['candidates']}
    assert all(h['read']['instruction'].startswith('test ') and h['field']=='controlCreatureFlags' for h in added)
    ap0=load('phase64-ap-pointer-leads.json');ap=load('phase64-ap-pointer-leads-v2.json')
    assert len(ap['producer_roots'])==2623 and ap['root_states']==135454 and len(ap['candidates'])==274 and ap['limited_roots']==8 and ap['limited_callees']==28
    assert len(ap0['candidates'])==272 and ap0['root_states']==ap['root_states']
    apadded=[h for h in ap['candidates'] if key(h) not in {key(j) for j in ap0['candidates']}]
    assert len(apadded)==2 and all(h['read']['address']=='0x510e36' for h in apadded)
    entity=load('phase64-old-typed-consumer-leads-v4.json');previous=load('phase62-old-typed-consumer-leads-v3.json')
    assert entity['candidates']==previous['candidates'] and len(entity['producer_roots'])==408 and entity['root_states']==5966 and len(entity['candidates'])==10 and entity['limited_roots']==entity['limited_callees']==0
    keeper=load('phase64-keeper-cfg-alias-leads-v2.json');assert keeper['reads']==[] and keeper['seeds']==45 and sum(r['states'] for r in keeper['reports'])==838
    tables=load('phase64-offset-table-candidates.json');table_rows=[]
    for program in tables['programs']:
        b=Path(program['exe']).read_bytes();assert digest(b)==program['sha256']
        for r in program['candidates']:
            assert pe_read(b,r['address'],r['width'])==r['value'].to_bytes(r['width'],'little') and pe_read(b,r['context_address'],len(bytes.fromhex(r['context_hex'])))==bytes.fromhex(r['context_hex'])
        assert not any(r['value'] in {0x75C,0x6EB} and r['width']==4 for r in program['candidates'])
        large=[r for r in program['candidates'] if r['value'] in {0x75C,0x6EB}]
        for r in large:
            value=int.from_bytes(pe_read(b,r['address'],4),'little');assert r['width']==2 and value!=r['value']
            table_rows.append(dict(exe=program['exe'],address=hex(r['address']),numeric_word=r['value'],containing_dword=hex(value),rule='Low WORD coincidence inside pointer-sizedvalue,not a verified Creature offset descriptor.'))
    result=dict(phase=64,capstone_version=capstone.__version__,policy=link('native_memory_access.py'),native_calibration=rows,
                defect='Capstone5.0.7 reports memory operand access0 for observed TEST mem,reg. Historical READ-bit-only candidate scanners can omit these reads;not a change to actual nativecode or validatedemulations.',
                corrected_passes=[{**link('phase64-old-creature-argument-leads-v2.json'),'roots':822,'states':14506,'control_reads':356,'added_test_flag_checks':21,'unknown_field_purpose':'Still unresolved','limits':0},
                  {**link('phase64-old-typed-consumer-leads-v4.json'),'roots':408,'states':5966,'reads':10,'changed_candidates':0,'limits':0},
                  {**link('phase64-keeper-cfg-alias-leads-v2.json'),'roots':45,'states':838,'overlap_reads':0,'pointer_forwards':len(keeper['forwarded'])},
                  {**link('phase64-ap-pointer-leads-v2.json'),'roots':2623,'states':135454,'reads':274,'added_test_control_reads':2,'limited_roots':8,'limited_callees':28,'callee_contexts':609,'producer_types':dict(Counter(r['producer'] for r in ap['producer_roots'])),'scope':'Globaltagtable/vslotseeds are untyped candidates;AP class/fieldidentity needs independentnativeproof.'}],
                descriptor_search={**link('phase64-offset-table-candidates.json'),'programs':2,'candidates':sum(len(p['candidates']) for p in tables['programs']),'large_word_rejections':table_rows,'scope':'Initialized readable non-executable nonresource/nonreloc sections only;not runtime-built,byte-encoded,compressed or text-embeddedtables.'},
                limitations=['Native/read candidate classifier is not a global non-use proof.','Legacy READ-bit-only searches remain historical and have this additional blind spot. Four specified passes replayed;allhistorical scans are not retroactively declared corrected.','Fallback covers known TEST/CMP reads;completeCapstoneinstructionmetadata validation notclaimed.','CorrectedAP globaltagtable search has8bounded roots and28boundedcallees;no global AP04/08absenceclaim.','No new AbilityJump/PickUpPriority/Traps204/Objects34C/KeeperC8 gameplay purpose assigned.'])
    (ROOT/'memory-read-policy.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Readpolicy:7native calibrations;21Creature checks restored;2AP control reads restored;396descriptor candidates;purposesremainopen')
if __name__=='__main__':main()
