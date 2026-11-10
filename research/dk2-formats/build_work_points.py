"""Publish raw work-rate quantization and finite player contribution consumers."""
import hashlib
import json
from pathlib import Path
import emulate_work_points as native
import native_work_rates as api
import prepare_work_points_primary as primary
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent


def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))


def build():
    v=load('phase116-work-point-emulations.json');cases=native.cases();assert len(v['replays'])==len(cases)==28
    assert v['scope']==native.__doc__ and len(v['paired_common_context_cases'])==4
    post={}
    for c,row in zip(cases,v['replays']):
        args,checks=native.probe.build(**native.inputs(c));p=native.path_for(args);value=load('ghidra/'+p.name)
        assert value['input']==args and value['case']==c and row['file']==p.name and row['case']==c['name']
        assert row['sha256']==digest(p.read_bytes()) and row['outcome']==native.outcome(c)
        assert api.loaded_rate(c['base'],c['tps'])==row['outcome']['loaded_rate']
        assert api.contribution(c['base'],c['tps'],c['level'],c['coefficient'])==row['outcome']['contribution']
        post[c['name']]=native.verify(c,args,checks,value['result'])
    for mode in ['manufacture','research']:
        for w in [1,2]:
            name=mode+('-wizard-one' if w==1 else '-wizard-two');base=post[mode+'-wizard-control']
            c=next(c for c in cases if c['name']==name);_,checks=native.probe.build(**native.inputs(c))
            wanted={a:bytearray(b) for a,b in base.items()}
            native.probe.f.put(wanted[native.probe.target.attack.TARGET],0x3B3,205*w)
            native.probe.f.put(wanted[native.probe.OWNER],0x1094,w,1)
            for tag,a in [(200,native.probe.ROOM),(110,native.probe.MEMBER)]:
                native.probe.f.put(wanted[0x6EE540],4*tag,a);wanted[0x6ED540][tag]=1
            for a,b in checks.items():
                if a not in wanted:wanted[a]=bytearray(b)
            native.probe.f.put(wanted[native.probe.ROOM],0x53,110,2)
            assert post[name]==wanted
    assert load('phase116-primary-manifest.json')==primary.build()
    corpus=load('phase116-work-rate-api-validation.json')
    for row in corpus['files']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    frozen=load('poison-process-semantics.json')
    for row in frozen['helpers']+frozen['dependencies']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    prior=ROOT/'phase116-prior-components.json'
    if not prior.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==94
        prior.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==94
    return dict(phase=116,date='2026-10-29',family='Creature research/manufacture rates and Crypt WorkEnhancement consumer boundary',
        executable=primary.build()['executable'],executable_sha256=primary.build()['sha256'],
        primary_evidence=link('phase116-primary-manifest.json'),native_replays=v['replays'],
        physical=dict(record_bytes=[5449,5537],raw_research=0xF08,raw_manufacture=0xF0A,width=2,
            intermediate_research=0x12F8,intermediate_manufacture=0x12FA,
            loaded_research=0x6BB,loaded_manufacture=0x6BD,loaded_width=2),
        loading=dict(constructor=0x491DC0,initializer=0x568820,converter=0x568E30,loader=0x492320,
            formula='For positive signed32 TPS: raw0 ->0; positiveu16 raw -> max(1,floor(raw/TPS)).',
            examples_TPS20={'0':0,'1':1,'19':1,'39':1,'40':2,'100':5},example_TPS25={'100':4},
            boundary='Do not write quantized runtime values back to file as the original raw rates.'),
        level=dict(research_StatId=17,manufacture_StatId=18,raw_GlobalVariables_type=65,loader=0x51DC40,
            coefficient_read='signed16; base rate read unsigned16',
            coefficient_offsets='Research statsBase+201h+2*level; manufacture statsBase+213h+2*level.',
            formula='level1 uses loaded rate directly; otherlevels use clamp(trunc0(loaded*signed16coefficient/100),0,65534).',
            tested='Raw100/TPS20 -> loaded5; level2coef125 ->6; FFFFh(-1)->0; rawFFFF/TPS20,coef7FFFh ->65534.'),
        contribution=dict(research_callback=0x47B000,research_player_handler=0x4C2DC0,research_progress=0x20F,research_progress_width=4,
            manufacture_callback=0x47B190,manufacture_player_handler=0x4C3200,manufacture_progress=0x217,manufacture_progress_width=2,
            research_zero='Callback always invokes player with0 and returns1.',
            manufacture_zero='Callback skips player handler and returns0 when computed low16 contribution is0.',
            arithmetic='Selected positive contributions add with physical counter wrap: researchu32,manufactureu16.',
            examples='Loaded5 with initial10 ->15. Level2cap65534 with initial10 ->65544research,8manufacture.',
            scope='Whole calculation/player handlers with no active research index and empty manufacture queue; completion and rewards not reached. Clock101 avoids TPS-periodic experience/other bonus branch.'),
        Crypt=dict(registration=0x4D8E60,room=26,species=16,owner_counter=0x1094,recipient_species=8,
            full_Creature_tick=0x488930,WorkEnhancement_offset=0x3B3,
            observed='Native registration+tick yields205/410 for counts1/2. For raw20000/TPS20,each chosen callback adds1000 with all three counts0/1/2.',
            read_observation='No reads of target3B3 during selected full chain; field receives original tick output.',
            conclusion='This stored Crypt value does not change the amount in these isolated research/manufacture contributions.',
            limits='Doesnot establish all job output persecond,animation/callback frequency,activequeue rewards or all WorkEnhancement consumers. 496E00 digging consumer remains static-only.'),
        fixture=dict(raw_target='Vampire8 fully loaded; work-rate fields changed explicitly.',
            state='Prepared CState context points at actual target andWorld; AI entry into research/workshop is not executed.',
            clock=101,full_ticks=28,periodic_experience_executed=False,
            navigation='Valid empty32x32 navigation grid,original initialized state/category/action tables.',
            resources='Declared absent-resource/audio IO; real game andnative class vtables unchanged.'),
        verification=dict(native_raw_load_tick_contribution_chains=28,raw65_loads=28,Crypt_registrations=4,
            paired_full_context_cases=v['paired_common_context_cases'],
            baseline='Complete base output measured; four Crypt variants predict all prepared bytes including room/member registrations. Not independent decoding of opaque snapshots.',
            original_PE_ranges=5,inherited_Ghidra_byte_ranges=7,independent_Ghidra_CPU_controls=0,
            pending_review='No new Ghidra request after prior approval-review usage-limit failure; earlier phase114/115 eight controls still pending.'),
        corpus=dict(evidence=link('phase116-work-rate-api-validation.json'),files=82,identity_records=3734,
            exact_rebuilds=410,changed=328,identity=82,API_refusals=8,source_writes=0),
        helpers=[link(n) for n in ['probe_work_points.py','emulate_work_points.py','native_work_rates.py',
                                  'validate_work_rates.py','prepare_work_points_primary.py']],
        dependencies=[link(n) for n in ['probe_poison.py','probe_creature_resistance.py','probe_creature_tick.py',
            'native_unicorn_factorytrace.py','poison-process-semantics.json','crypt-tick-semantics.json']],
        unchanged_source_executables=source_executables(),
        limitations=['Exact supplied DX,prepared AI state/World andno live renderer/audio/GUI or original editor save.',
            'Native full rate loading,full Creature update andcontribution/player functions executed; autonomous job selection/action scheduling andcompletions notexecuted.',
            'Research index0 andinactive manufacturing queue isolate progress calculation; not a complete active task scene.',
            'Clock101 skips periodic experience branch; other bonuses in that branch remain outside thisproof.',
            'WorkEnhancement nonapplication is about amount perselected callback,not universal absence of other work/animation effects.',
            'Four paired complete contexts; all28semantic controls/captures. No independent GhidraCPU replay thisphase.',
            'No newly assigned name for remainingunknown rawfields; overall15family objective remains incomplete.'])


if __name__=='__main__':
    s=build();(ROOT/'work-point-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    print('phase116:',s['verification'])
