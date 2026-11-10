"""Attest static Crypt action evidence; do not claim whole AI/job execution."""
from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_objects as probe
from portal_animation_tables import initialized_animations
ROOT=Path(__file__).resolve().parent

def check():
    blob=probe.EXE.read_bytes()
    assert probe.sha(blob)=='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
    paths=['ghidra/phase179-crypt-action-499ca0-memory.txt','ghidra/phase147-primary-4950c0-memory.txt',
           'ghidra/phase91-primary-496aa0.txt','ghidra/crypt-gameplay-primary-483870.txt']
    spans=[]
    for name in paths:
        row=json.loads((ROOT/name).read_text(encoding='utf-8'))
        a=int(row['address'],16) if isinstance(row['address'],str) else row['address']
        raw=bytes(row['data']) if 'data' in row else bytes.fromhex(row['hex'])
        assert raw==probe.p.pe_read(blob,a,len(raw)),name
        spans.append(dict(file=name,address=a,length=len(raw),file_sha256=probe.sha((ROOT/name).read_bytes()),bytes_sha256=probe.sha(raw)))
    table=initialized_animations();action=table[80*29:81*29]
    assert action.hex()=='a09c4900000000000000000000000000500000000500000000d7000001'
    assert int.from_bytes(action[:4],'little')==0x499CA0 and action[20]==5
    assert action[21:25]==bytes(4) and action[27]==0
    assert table[17*29+20]==5
    asmname='ghidra/phase179-crypt-action-499ca0-assembly.txt'
    assembly=(ROOT/asmname).read_text(encoding='utf-8')
    pcs=[int(s,16) for s in re.findall(r'^([0-9a-fA-F]{8}):',assembly,re.M)]
    assert pcs[0]==0x499CA0 and all(0x499CA0<=pc<0x499CA0+3072 for pc in pcs)
    md=Cs(CS_ARCH_X86,CS_MODE_32)
    calls=sorted({int(i.op_str,16) for pc in pcs for i in [next(md.disasm(probe.p.pe_read(blob,pc,15),pc))]
                  if i.mnemonic=='call' and i.op_str.startswith('0x')})
    assert {0x517490,0x4B2CE0,0x4B7A90}<=set(calls)
    # The earlier original Crypt state has an actual CALL to this action
    # activator, with action80 pushed; it is not a guessed adjacent function.
    assert probe.p.pe_read(blob,0x48395F,2)==bytes.fromhex('6a50')
    assert probe.p.pe_read(blob,0x483961,5)==bytes.fromhex('e89a2d0100')
    return dict(status='static_primary_bytes_passed',executable_sha256=probe.sha(blob),primary_spans=spans,
        action=dict(ID=80,initializer=0x4950C0,callback=0x499CA0,animation_selector=5,selector_also_used_by_melee17=True,
                    mode=0,duration=0,callback_returns_nonzero_in_all_read_paths=True),
        callback_assembly=dict(file=asmname,sha256=probe.sha((ROOT/asmname).read_bytes()),instruction_addresses=len(pcs),direct_calls=calls),
        interpretations=dict(continuous_activity='Supported by action80 table, original496AA0 dispatcher and callback returning1.',
            sacrifice='No mandatory consumption of participant established in the read chain.',
            periodic_warlock='Static original499CA0: after clock threshold, iterate owner Creature list and request positive5 via4B2CE0. Not an executed healing amount.',
            bell_strike='Action uses melee resource selector5; visible hitting of Bell is still an inference, not rendered observation.'),
        scope='Static action/caller/dispatcher reading. No whole automatic entry/exit, lifetime, live animation or new assignment of the10 unknown owner counters.',
        phase179_publication='Object scenes have bounded native checks; complete Ghidra crosschecks and package audit remain pending.')

if __name__=='__main__':
    d=check();(ROOT/'phase179-crypt-action-static-checks.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
    print(d['status'],len(d['primary_spans']),'PE ranges',d['callback_assembly']['instruction_addresses'],'callback instructions')
