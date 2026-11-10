"""Resolve eight historical pending controls with actual independent Ghidra runs.

Keep historical component bytes intact; this component is a later validation
update, not new spell coverage or a claim that every damage path is understood.
"""
import json
from pathlib import Path
import hashlib
import crosscheck_shot_damage as shot
import crosscheck_poison as poison
from emulate_resistance import post_context
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent


def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))


def controls():
    out=[]
    for phase,module,manifest,pending in [
            (114,shot,'phase114-shot-damage-crosschecks.json','phase114-pending-crosschecks.json'),
            (115,poison,'phase115-poison-crosschecks.json','phase115-pending-crosschecks.json')]:
        rows=load(manifest)['replays'];assert len(rows)==len(module.CONTROLS)
        prepared={r['case']:r for r in load(pending)['controls']}
        for name,row in zip(module.CONTROLS,rows):
            assert row['case']==name
            args=module.build(name)
            input_hash=sha(json.dumps(args,sort_keys=True).encode())
            assert prepared[name]['input_sha256']==input_hash
            assert prepared[name]['prepared'] and not prepared[name]['executed']
            path=ROOT/'ghidra'/row['file'];saved=json.loads(path.read_text(encoding='utf8'))
            assert sha(path.read_bytes())==row['sha256'] and saved['input']==args
            result=saved['result']
            assert not saved['tool_is_error'] and result['success'] and result['hit_return']
            assert all(int(result['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
            if phase==114:
                case=next(c for c in module.native.probe.cases() if c['name']==name)
                expected=module.native.expected(case)
            else:
                case=next(c for c in module.native.cases() if c['name']==name)
                cpu_args,checks=module.native.probe.build(**module.native.inputs(case))
                prior=load(str(module.native.path_for(cpu_args).relative_to(ROOT)))
                assert prior['input']==cpu_args and prior['case']==case
                expected=post_context(checks,prior['result'])
            out.append(dict(historical_phase=phase,case=name,input_sha256=input_hash,
                result=link(str(path.relative_to(ROOT))),historical_pending_file=link(pending),
                original_input_unchanged=True,success=True,hit_return=True,
                registers={k:result['registers'][k] for k in ['EAX','EBP','EBX','ECX']},
                checked_regions=len(expected),checked_bytes=sum(len(b) for b in expected.values())))
    assert len(out)==8
    return out


def build():
    replays=controls()
    prior=ROOT/'phase118-prior-components.json'
    if not prior.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==96
        prior.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==96
    return dict(phase=118,date='2026-10-29',family='Independent Ghidra validation update for damage and poison',
        supersedes_validation_status=[dict(component='shot_damage_semantics',historical_phase=114,
            old_independent_Ghidra_CPU_controls=0,current_independent_Ghidra_CPU_controls=5),
            dict(component='poison_process_semantics',historical_phase=115,
            old_independent_Ghidra_CPU_controls=0,current_independent_Ghidra_CPU_controls=3)],
        history='Earlier pending files and schemas remain byte-identical historical evidence. All eight exact prepared inputs have now executed successfully; do not read old executed:false as current pending status.',
        review_recovery='After the stated13:56Europe/Moscow reset, the same reviewed local-MCP commands were allowed. Existing live JavaPID19308 was reused; no alternate network/approval route or restart.',
        controls=replays,verified_Ghidra_CPU_controls=8,remaining_prepared_controls=0,
        totals=dict(checked_regions=sum(r['checked_regions'] for r in replays),
            checked_bytes=sum(r['checked_bytes'] for r in replays),scope='Sums across eight cases,not unique memory or records.'),
        observations=['Ghidra agrees with all prepared-context expectations fordirecthit andconstantAoE withres100.',
            'DistanceAoE half-radius andBoulderfire/projectile controls agree.',
            'Poison ordinary/frozen/armoured tick contexts agree withUnicorn snapshots.',
            'This validates chosen native calculations/contexts,not everyspell,allAoEtargets orlivegame behaviour.'],
        input_helpers=[link(n) for n in ['crosscheck_shot_damage.py','crosscheck_poison.py']],
        result_manifests=[link(n) for n in ['phase114-shot-damage-crosschecks.json','phase115-poison-crosschecks.json']],
        source_executables_unchanged=source_executables(),
        new_gameplay_field_names=0,new_container_rebuilds=0,new_gameplay_function_substitutions=0,
        limitations=['Exact supplied DX and previously prepared scenes/resources/World. No live game,GUI or natural flight/timing.',
            'Independent emulator controls share explicit input fixtures and expected regions; this is not independent recovery of every opaque byte.',
            'Five damage andthreepoison controls close only thehistorical pending set; otherdamage/processbranches remain open.',
            'Earlier reported raw/container checks are historical and are not recounted as new runs.',
            'Significant unknown fields across15families remain unresolved; goal incomplete.'])


if __name__=='__main__':
    value=build()
    (ROOT/'combat-crosscheck-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(dict(controls=value['verified_Ghidra_CPU_controls'],remaining=value['remaining_prepared_controls'],totals=value['totals'])))
