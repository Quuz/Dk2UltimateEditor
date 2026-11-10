"""Build supplemental schemas from saved native evidence, without contacting Ghidra."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import asm_range,pe_read
from extract_editor_strings import EXE as EDITOR,extract

ROOT=Path(__file__).resolve().parent
GAME=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def proof(file,contains):
    lines=(ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines()
    hits=[(i+1,line.strip()) for i,line in enumerate(lines) if contains in line]
    assert len(hits)==1,(file,contains,len(hits))
    line,excerpt=hits[0]
    return dict(file=file,line=line,excerpt=excerpt)

def save(file,data):
    (ROOT/file).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf8')

def main():
    game=GAME.read_bytes();editor=EDITOR.read_bytes()
    table=[]
    for slot,target in [(0x90,0x440420),(0x94,0x43A8E0),(0x98,0x440350),(0x184,0x440BC0)]:
        raw=pe_read(game,0x66CC94+slot,4);assert struct.unpack('<I',raw)[0]==target
        table.append(dict(slot=slot,table_address=0x66CC94+slot,target=target,bytes_hex=raw.hex()))
    chain=dict(executable_sha256=hashlib.sha256(game).hexdigest(),family='Effects',record_bytes=246,
        field=dict(name='NextEffectId',raw_offset=0xC0,raw_bytes=2,raw_type='u16',
                   intermediate_offset=0x174,runtime_offset=0x7B,runtime_bytes=2,zero='stop chain'),
        effect_id=dict(raw_offset=0xB8,raw_bytes=2,runtime_offset=0x79),
        creation=dict(function=0x440350,bridge_slot=0x98,timing='immediate same-call creation loop',
            definition_pointer_instance_offset=0x107,global_head_bridge_offset=0x336,
            created_list_order='Each new instance is inserted at the head; reverse of creation order.',
            output='Only the first successfully created instance is written to the caller output pointer.',
            return_value='1 if any instance was created, otherwise0; partial creation is not rolled back in this caller.',
            early_exit='ID0, creation-enable global0, or allocation helper returning0.',
            enable_global_address=0x69793C,allocation_request_bytes=0x159,
            allocation_capacity=None,cycle_guard_in_inspected_loop=False,
            arguments='The four arguments following EffectId are forwarded unchanged to every initializer.'),
        definition_lookup=dict(function=0x440BC0,id_type='u16',record_stride=0x94,
            bridge_array_offset=0x86,bridge_count_offset=0x8E,
            algorithm='Start min(id-1,count-1), walk backwards in the sorted array; return equal key, stop below key.',
            duplicate_policy='Diagnostic tools stop on ambiguous duplicate IDs; no last-wins policy is inferred.'),
        bridge_vtable=table,
        evidence=[asm_range('phase17-effect-chain-create-assembly.txt',0x440350,0x440410),
            proof('game-effects-convert.txt','param_2 + 0xc0'),
            proof('game-effects-convert.txt','param_1 + 0x5d'),
            proof('game-effect-definition-load.txt','param_1 + 0x7b'),
            proof('phase17-effect-definition-getter.txt','puVar3 = (ushort *)'),
            proof('phase17-effect-chain-init.txt','+ 0x107) =')],
        limitations=['Static native control-flow evidence; no modified-map gameplay or new native emulation in phase17.',
            'Missing definitions are invalid input to the projection; initializer null handling outside this path is not established.',
            'Allocator internals and pool capacity remain uninspected. 0x159 is allocation size, not a chain limit.',
            'A diagnostic step budget is a tool safeguard, not a native maximum. Cycles can repeatedly allocate while conditions allow.',
            'This field differs from EffectElements.NextEffectId, emitted once per element update in the separately documented path.'])
    save('effect-chain-semantics.json',chain)
    label=next(s for s in extract(editor) if s['id']==0xF855);assert label['text']=='Pick-Up Priority'
    creature=dict(executable_sha256={'game':hashlib.sha256(game).hexdigest(),'editor':hashlib.sha256(editor).hexdigest()},
        family='Creatures',record_variants=[5449,5537],baseline_name='Unk3f2',native_label='Pick-Up Priority',
        field=dict(raw_offset=0xF55,raw_bytes=1,raw_type='u8',
            game_intermediate_offset=0x1345,editor_intermediate_offset=0x1355,presence_offset=0x1E8,
            game_runtime_offset=0x6EB,editor_model_offset=0x16AE,editor_panel_offset=0x17EE,property_id=0xF855),
        label_resource=label,
        native_editor=dict(fill_function=0x43F2A0,read_function=0x4405E0,
            update_function=0x43FD60,commit_function=0x441160,writer_function=0x4A7F00,
            display='Zero-extended byte converted to double.',
            commit='__ftol result narrowed to byte; then panel byte copied to model byte.',
            validated_gui_range=None,
            writer=dict(record_bytes=5537,buffer_stack_offset=0x94,field_stack_offset=0xFE9,
                        field_offset_calculation='0xFE9 - 0x94 = 0xF55')),
        distinct_spell_swipe=dict(raw_offset=0xF45,raw_bytes=1,game_intermediate_offset=0x1335,
            editor_intermediate_offset=0x1345,game_runtime_offset=0x684,editor_model_offset=0x16B6,
            consumer_function=0x4100F0,
            warning='Editor+0x1345 belongs to SpellSwipe; game+0x1345 belongs to Pick-Up Priority. These are different structures.'),
        evidence=[asm_range('phase17-creature-editor-convert-assembly.txt',0x428D9B,0x428DA7),
            asm_range('phase17-creature-editor-convert-assembly.txt',0x429310,0x42931E),
            asm_range('game-creature-type-assembly.txt',0x49261C,0x49262A),
            asm_range('game-creature-type-assembly.txt',0x4941E3,0x4941E9),
            asm_range('phase17-creature-writer-assembly.txt',0x4A841E,0x4A8424),
            asm_range('phase17-creature-writer-assembly.txt',0x4A8A43,0x4A8A4B),
            asm_range('phase17-creature-writer-assembly.txt',0x4A8B7D,0x4A8B84),
            proof('game-creature-convert.txt','cVar1 = param_2[0xf55]'),
            proof('game-creature-convert.txt','+ 0x1345) = cVar1'),
            proof('phase17-creature-editor-model.txt','this + 0x16ae) ='),
            proof('phase17-creature-main-panel-fill.txt','this + 0x17ee) ='),
            proof('phase17-creature-main-panel-commit.txt','+ 0x16ae) ='),
            proof('phase17-creature-f55-panel-extract.txt','+ 0x17ee);'),
            proof('phase17-creature-f55-panel-update.txt','+ 0x17ee) ='),
            proof('phase17-creature-swipe-game-consumer.txt','iVar7 + 0x684'),
            proof('phase17-creature-swipe-game-consumer.txt','FUN_005075a0(cVar6,uVar3)')],
        limitations=['Native label, storage and editor binding confirmed; gameplay ordering and comparison rule remain unresolved.',
            '0..255 is the storage range, not an established native GUI validation range.',
            'The inspected native writer emits5537 bytes; read-only views also support the existing5449-byte record variant.'])
    save('creature-priority-semantics.json',creature)
    print('Built two phase17 schemas from saved native evidence.')

if __name__=='__main__':main()
