"""New normal-EXE isolated controls using prior exact DX fixtures/expectations.

One actual rawTrap load/create/activate/remove cycle and two fullCLight render
paths. ActualnormalPE isloaded;no WindowssystemDLLs loaded. Complete outputs
also comparedwith priorDX native evidence;not independent implementationproof.
"""
from pathlib import Path
import json
import scan_game_executable_variants as variants
import emulate_trap_removal as trap
import emulate_room_torch_light as light

ROOT=Path(__file__).resolve().parent
NORMAL=variants.GAME/'DKII.EXE'

def cases():return ['rawTrap204-FFFF','point-128','other-255']

def build(name):
    if name==cases()[0]:
        c=next(c for c in trap.cases() if c['name']=='trap1-active-65535')
        args,checks,expected=trap.probe.build(**trap.inputs(c));prior=trap.path_for(args)
        saved=json.loads(prior.read_text(encoding='utf8'));assert saved['input']==args
        return dict(kind='trap',case=c,args=args,checks=checks,expected=expected,prior=prior,
            stop=trap.probe.f.OBS,capture_address=expected['node'],capture_size=291)
    c=next(c for c in light.cases() if c['name']==name);args,checks=light.build(c)
    prior=light.path_for(args);saved=json.loads(prior.read_text(encoding='utf8'));assert saved['input']==args
    return dict(kind='light',case=c,args=args,checks=checks,expected=None,prior=prior,
        stop=light.OBS,capture_address=light.HEAP,capture_size=192)

def verify(name,prepared,result):
    prior=json.loads(prepared['prior'].read_text(encoding='utf8'))['result']
    assert result['source_exe_sha256']==variants.PROFILES['normal'][1]
    if prepared['kind']=='trap':
        state,outcome=trap.verify(prepared['case'],prepared['args'],prepared['checks'],prepared['expected'],result)
        old,_=trap.verify(prepared['case'],prepared['args'],prepared['checks'],prepared['expected'],prior)
        assert state==old
    else:
        outcome=light.verify(prepared['case'],prepared['args'],prepared['checks'],result)
        light.verify(prepared['case'],prepared['args'],prepared['checks'],prior)
    # Actualinstructions,alltrackedmemoryreads/writes,capture/contexts andGPRs
    # agree. Onlysourcefilehash/label can differ in the engine result record.
    for key in ['success','hit_return','emulation_error','registers','context_mismatches',
        'protected_write_or_execution_failures','instruction_count','visited_instructions',
        'captured_hex','trace_tail','before_calls','observed_writes','observed_reads','stack_at_stop_hex']:
        assert result[key]==prior[key],(name,key)
    return outcome

def path_for(name,args):
    tag=light.sha((json.dumps(args,sort_keys=True)+variants.PROFILES['normal'][1]).encode())[:12]
    return ROOT/'ghidra'/f'phase124-normal-{name}-{tag}.json'

def main():
    variants.build();rows=[]
    for name in cases():
        c=build(name);r=light.cpu.run(NORMAL,c['args'],list(c['checks'].items()),c['stop'],c['capture_address'],c['capture_size'])
        p=path_for(name,c['args']);p.write_text(json.dumps(dict(scope=__doc__,case=name,input=c['args'],result=r),indent=2),encoding='utf8')
        outcome=verify(name,c,r)
        rows.append(dict(case=name,file=str(p.relative_to(ROOT)),sha256=light.sha(p.read_bytes()),
            source_executable=str(NORMAL),source_executable_sha256=r['source_exe_sha256'],
            inherited_DX_result=dict(file=str(c['prior'].relative_to(ROOT)),sha256=light.sha(c['prior'].read_bytes())),
            instruction_count=r['instruction_count'],tracked_memory_and_instruction_trace_equal=True,outcome=outcome))
        print(name,'normalPE passed;full trace andcontexts matchDX',r['instruction_count'])
    (ROOT/'phase124-normal-executable-controls.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,
        new_Ghidra_CPU_controls=0,reason='Normalprogramnotimported;priorDXGhidracontrols arehistorical,not counted anew.'),indent=2),encoding='utf8')

if __name__=='__main__':main()
