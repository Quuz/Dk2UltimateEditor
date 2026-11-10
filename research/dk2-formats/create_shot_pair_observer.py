"""Create an observation-only derivative; frozen phase131 engine is unchanged."""
from pathlib import Path
from hashlib import sha256
ROOT=Path(__file__).resolve().parent
def expected():
    source=(ROOT/'native_aoe_trace.py').read_text(encoding='utf-8')
    before='    hp_requests, aoe_entries, call_edges = [], [], set()'
    after=before+'\n    process_events, pending_process_returns = [], []'
    assert source.count(before)==1;source=source.replace(before,after)
    before='        nonlocal previous_pc, previous_size\n'
    after=before+'''        # Observation only: original process entry and actual native return.
        if pending_process_returns and address == pending_process_returns[-1]['caller_return']:
            event = pending_process_returns.pop()
            event['return_EAX'] = uc.reg_read(xc.UC_X86_REG_EAX)
        if address == 0x4AD730:
            sp = uc.reg_read(xc.UC_X86_REG_ESP)
            node = uc.reg_read(xc.UC_X86_REG_ECX)
            caller, mask = struct.unpack('<2I', bytes(uc.mem_read(sp,8)))
            event = dict(caller_return=caller, event_mask=mask, instance=node,
                tick=struct.unpack('<I',bytes(uc.mem_read(node+0x12D,4)))[0],
                Data1=struct.unpack('<I',bytes(uc.mem_read(node+0x13D,4)))[0],
                Data2=struct.unpack('<I',bytes(uc.mem_read(node+0x141,4)))[0])
            process_events.append(event);pending_process_returns.append(event)
'''
    assert source.count(before)==1;source=source.replace(before,after)
    before='                  hp_requests=hp_requests, aoe_entries=aoe_entries, observed_call_edges=sorted(call_edges),'
    after=before+'\n                  process_events=process_events,'
    assert source.count(before)==1;source=source.replace(before,after)
    return source
def main():
    file=ROOT/'native_shot_pair_trace.py';text=expected()
    if file.exists():assert file.read_text(encoding='utf-8')==text
    else:file.write_text(text,encoding='utf-8')
    print('Observation-only derivative verified',sha256(file.read_bytes()).hexdigest())
if __name__=='__main__':main()
