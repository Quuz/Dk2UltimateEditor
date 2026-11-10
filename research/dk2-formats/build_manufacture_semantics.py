"""Evidence-backed manufacture storage, rates and native queue consumers."""
from pathlib import Path
import hashlib,json
from build_effect_semantics import asm_range

ROOT=Path(__file__).resolve().parent

def main():
    def proof(file,needle):
        lines=(ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines()
        i=next(i for i,line in enumerate(lines) if needle in line)
        return dict(file=file,line=i+1,excerpt=lines[i].strip())
    scalar=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']
    def path(family,raw):return next(p for p in scalar[family]['joined_paths'] if int(p['raw_offset'],16)==raw)
    exes=json.loads((ROOT/'executable-hash-verification.json').read_text(encoding='utf8'))
    for file,digest in exes.items():assert hashlib.sha256((ROOT.parent.parent/file).read_bytes()).hexdigest()==digest
    emulations=[]
    for p in sorted((ROOT/'ghidra').glob('phase15-emulation-*.json')):
        v=json.loads(p.read_text());r=v['result']
        if v.get('expected_fault'):
            assert r['success'] is False and r['hit_return'] is False and r['final_pc']=='00506b93' and r['emulation_error']=='Divide by 0'
        else:assert r['success'] and r['hit_return'] and int(r['registers']['EAX'],16)==v['expected_eax']
        emulations.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),name=v['name'],expected_eax=v['expected_eax'],expected_fault=v.get('expected_fault',False)))
    assert len(emulations)==14
    result=dict(executables=exes,
        scope='Selected static and isolated P-code emulation results for this exact DKII-DX.exe; no complete game/workshop simulation.',
        manufacture_fields=[dict(family=f,record_bytes=n,kind=k,id_offset=i,crate_object_id_offset=c,
            storage_path=path(f,o),interpretation='Unscaled unsigned16 manufacture points copied into queue item+0, not a32-bit requirement.')
            for f,n,k,i,c,o in [('Doors',616,0,0x244,0x246,0x234),('Traps',579,1,0x20C,0x20E,0x1FA)]],
        queue=dict(runtime_only=True,bytes=2056,capacity=128,item_bytes=16,
            fields=[dict(offset=0,bytes=2,name='manufacture_required',read='unsigned16 in completion/percent helpers'),
                dict(offset=2,bytes=1,name='kind',values={'0':'Doors','1':'Traps'}),
                dict(offset=3,bytes=1,name='catalogue_id'),dict(offset=4,bytes=12,name='position_xyz')],
            indices=dict(opened=0x800,head=0x801,next_free=0x802,count=0x803,world_pointer=0x804),
            add_function='0x5066e0',get_function='0x506930',pop_function='0x506970',
            add_note='Rejects count>=128, invalid kind or missing definition. May return1 without insertion when existing matching crates already cover counted blueprints.',
            demand_note='Blueprint count matches the requested catalogue ID. Crate count matches kind and definition ManufCrateObjectId; shared crate IDs can affect coverage.',
            add_argument_note='Decompiler stack parameters in this function are misleading: item+2 gets first stack argument(kind), item+3 gets second(ID). Assembly stores are authoritative.',
            evidence=[asm_range('phase15-manufacture-add-task-assembly.txt',0x5066E9,0x506704),
                asm_range('phase15-manufacture-add-task-assembly.txt',0x506725,0x506732),
                asm_range('phase15-manufacture-add-task-assembly.txt',0x5067DF,0x506807),
                asm_range('phase15-manufacture-add-task-assembly.txt',0x5068A5,0x506902),
                asm_range('phase15-manufacture-get-assembly.txt',0x50693A,0x506961),
                asm_range('phase15-manufacture-pop-assembly.txt',0x50697A,0x5069C4)]),
        queue_threshold_discrepancy=dict(completion_function='0x506b50',percent_function='0x506b70',
            completion='count!=0 and u16(progress)>=u16(queue+0)',
            percent='count==0 ?0: floor((u16(progress)*100)/u16(queue+0)); no100-percent clamp',
            threshold_address='queue+0, physical item0; not queue+16*head',
            task_read_address='queue+16*head',
            caller_queue_address='player+0x219 in both progress update and percent wrapper',
            zero_cost='When count!=0 and physical slot0 requirement==0, percent helper reaches IDIV0 at0x506B93. Completion helper returns true. This does not establish a whole-game crash for every zero-cost item.',
            reproduction=dict(first_slot_required=3000,head_index=1,current_head_required=12000,progress=4000,native_completed=True,native_percent=133),
            evidence=[asm_range('phase15-manufacture-completed-assembly.txt',0x506B50,0x506B6A),
                asm_range('phase15-manufacture-percent-core-assembly.txt',0x506B70,0x506B9B),
                asm_range('phase15-manufacture-player-tick-assembly.txt',0x4C3282,0x4C329E),
                asm_range('phase15-manufacture-percent-wrapper-assembly.txt',0x4C31B0,0x4C31C3)]),
        progress=dict(player_runtime_offset=0x217,bytes=2,read='unsigned16 in threshold helper',update='low16(old_progress+contribution)',
            apply_function='0x4c3200',completion_reset='Only after completion predicate and additional placement/capacity gate0x4C3B50 pass: progress=0, pop item, attempt spawn.',
            excess='Successful reset discards excess progress; there is no subtraction of requirement in this branch.',
            failed_placement_gate='Progress remains accumulated; a later addition can wrap before rechecking completion.',
            caveat='Local assignments only. Spawn results, worker scheduling, changes by other callers and queue maintenance are not simulated.',
            evidence=[asm_range('phase15-manufacture-player-tick-assembly.txt',0x4C3282,0x4C32A5),
                asm_range('phase15-manufacture-player-tick-assembly.txt',0x4C32FD,0x4C3323)]),
        creature_rate=dict(family='Creatures',raw_offset=0xF0A,bytes=2,intermediate_offset=0x12FA,runtime_offset=0x6BD,
            load_function='0x492320',load='raw==0?0:max(1,floor(u16(raw)/positive_TPS))',
            contribution_function='0x47b190',level_instance_offset=0x403,
            level_one='Uses unsigned runtime word directly; skips level multiplier and65534 clamp.',
            other_levels='clamp(truncate_toward_zero(signed16(level_multiplier)*unsigned16(runtime_rate)/100),0,65534)',
            multiplier_address='global_variables_runtime+0x213+2*unsigned_level; table value must be supplied explicitly',
            setter_override='Creature stat setter0x494A70 case0x12 directly writes low16(argument) into runtime+0x6BD without TPS division; do not apply raw load scaling twice.',
            evidence=[proof('game-creature-convert.txt','param_2 + 0xf0a'),
                asm_range('game-creature-type-assembly.txt',0x4938A4,0x4938CF),
                asm_range('phase15-manufacture-contribution-assembly.txt',0x47B195,0x47B214),
                proof('game-creature-stat-set.txt','param_1 + 0x6bd')]),
        unresolved_door_236=dict(raw_offset=0x236,runtime_offset=0x99,status='meaning remains unresolved',
            search_file='phase15-door-word-candidates.json',
            rejection='Mapped exact-displacement word reads at0x49BA45/0x49BA53 belong to CObject stream serialization of three signed coordinate words0x95/0x97/0x99, not MyDoorDataObj.',
            evidence=[proof('phase15-rejected-object-word-reader.txt','iStack_c = (int)*(short *)(param_1 + 0x95)'),
                proof('phase15-rejected-object-word-reader.txt','iStack_4 = (int)*(short *)(param_1 + 0x99)')],
            limitation='Mapped exact-displacement searches do not rule out aliases, computed pointers, other executables or unknown functions.'),
        native_emulations=emulations,
        emulation_note='Isolated original Ghidra P-code with initialized queue/stack memory.13 normal returns and1 expected IDIV0 fault; does not execute the game process or write original files.')
    (ROOT/'manufacture-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Manufacture schema:2 file families, creature rate,128-item native queue,14 isolated native emulations')

if __name__=='__main__':main()
