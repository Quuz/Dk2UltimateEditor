"""Evidence-backed native trap flags, trigger dispatch, range and charge counter."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import pe_read,asm_range

ROOT=Path(__file__).resolve().parent
GAME=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'
EDITOR=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'

def main():
    game=GAME.read_bytes();editor=EDITOR.read_bytes()
    def proof(file,needle):
        text=(ROOT/'ghidra'/file).read_text(encoding='utf8').replace('\r','')
        assert not text.startswith(('Automatic approval','The automatic permission','Error:'))
        line=next(i for i,v in enumerate(text.splitlines(),1) if needle in v)
        return dict(file=file,line=line,excerpt=text.splitlines()[line-1].strip())
    trigger_labels=[]
    for i in range(8):
        address=0x514768+i*8;value,pointer=struct.unpack('<iI',pe_read(editor,address,8))
        if value==-1:break
        raw=pe_read(editor,pointer,128)
        n=next(k for k in range(0,128,2) if raw[k:k+2]==b'\0\0')
        trigger_labels.append(dict(value=value,label=raw[:n].decode('utf-16-le'),entry_address=hex(address)))
    assert [(r['value'],r['label']) for r in trigger_labels]==[(1,'Line Of Sight'),(2,'Pressure'),(3,'Trigger')]
    dispatch=[]
    for i in range(4):
        value,callback=struct.unpack('<II',pe_read(game,0x66D430+i*8,8))
        if value==0:assert callback==0;break
        dispatch.append(dict(value=value,callback=hex(callback),source_entry_address=hex(0x66D430+i*8)))
    assert [r['callback'] for r in dispatch]==['0x4a9290','0x4a92c0','0x4a9350']
    paths=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']['Traps']['joined_paths']
    def path(raw):return next(p for p in paths if p['raw_offset']==hex(raw))
    dialogs=json.loads((ROOT/'editor-dialogs.json').read_text(encoding='utf8'))
    dialog=next(d for d in dialogs['dialogs'] if d['resource_path'][1]==326)
    controls={c['id']:c for c in dialog['controls']}
    # Explicit DDX bindings. The gaps are not filled from nearby static labels.
    checkbox={0:(0x104,146),1:(0x108,1791),4:(0x114,1794),5:(0x118,1793),
        7:(0x11C,1797),8:(0x120,1798),9:(0x124,1800),11:(0x12C,1803),
        12:(0x130,1804),13:(0x134,1806),15:(0x13C,1808),16:(0x140,2105),17:(0x144,2106)}
    flags=[];enc=(ROOT/'ghidra/phase12-traps-flags-encode.txt').read_text()
    dec=(ROOT/'ghidra/phase12-traps-flags-decode.txt').read_text()
    enums=json.loads((ROOT/'enums.json').read_text(encoding='utf8'))['Trap.TrapFlag']
    for bit in [0,1,2,3,4,5,7,8,9,10,11,12,13,14,15,16,17]:
        index=bit if bit<6 else bit-1;model=0x2A0+index*4;mask=1<<bit
        spelling='700' if model==0x2BC else hex(model)
        assert f'+ {spelling})' in enc and f'+ {spelling})' in dec
        row=dict(mask=mask,baseline_name=enums.get(str(mask)),editor_model_offset=model,checkbox=None)
        if bit in checkbox:
            panel,cid=checkbox[bit];control=controls[cid]
            panel_spelling='300' if panel==0x12C else hex(panel)
            needle=f'param_1,0x{cid:x},(int *)((int)this + {panel_spelling})'
            row['checkbox']=dict(control_id=cid,label=control['title'],panel_offset=panel,
                template_visible=control['template_visible'],template_disabled=control['template_disabled'],
                evidence=proof('editor-phase8-traps-ddx.txt',needle))
        flags.append(row)
    supported=sum(r['mask'] for r in flags);assert supported==0x3FFBF
    result=dict(family='Traps',record_bytes=579,game_sha256=hashlib.sha256(game).hexdigest(),editor_sha256=hashlib.sha256(editor).hexdigest(),
        trigger_types=dict(storage_path=path(0x20B),native_editor_options=trigger_labels,editor_initializer='0x470df0',editor_lookup='0x471790',
            omitted_editor_value=0,game_dispatch_source='0x66d430',runtime_table='0x6e4fd8',initializer='0x4a93a0',callbacks=dispatch,
            behavior='Tick checks TriggerType<4, then nonnull table callback. Value0 and values>=4 produce no positive result in this dispatch.',
            evidence=[proof('phase12-traps-trigger-options.txt','local_24 = &DAT_00514768'),
                proof('phase12-trap-trigger-init.txt','&DAT_006e4fd8 + iVar2 * 4'),
                asm_range('phase12-trap-tick-assembly.txt',0x4A8BC1,0x4A8BEB)]),
        editor_flags=dict(raw_offset=0x1EC,raw_bytes=4,intermediate_offset=0x294,game_runtime_offset=0x88,
            decode='0x4260e0',encode='0x426000',writer='0x4a8f40',supported_mask=supported,dropped_mask=0xFFFFFFFF^supported,flags=flags,
            rule='Full native read/write of this flag field reconstructs only supported bits. Raw-preserving serialization must not silently apply this projection.',
            evidence=[proof('editor-phase8-traps-convert.txt','param_1 + 0x1ec'),
                asm_range('phase12-traps-flags-load-assembly.txt',0x4A1C0D,0x4A1C16),
                proof('phase12-traps-flags-decode.txt','this + 0x2a8'),proof('phase12-traps-flags-encode.txt','bVar2 | 4'),
                proof('phase12-traps-flags-encode.txt','uVar1 | 0x4000'),
                proof('editor-phase8-traps-writer.txt','uStack_78 = FUN_00426000'),
                proof('editor-phase8-traps-writer.txt','&uStack_264,0x243')]),
        line_of_sight_data=dict(storage_path=path(0x1DC),consumer='0x4a9400',normal_argument=0,tracking_argument=1,tracking_radius_add=2,
            threshold_rule='((TriggerData + (tracking?2:0))**2 <<12) modulo2**32',
            distance_rule='Unsigned low32((dx*dx)>>12)+low32((dy*dy)>>12), wrapped to32 bits; ordinary world coordinates use4096 units/tile.',
            fresh_candidate_comparison='distance<threshold and distance<best_distance',
            cached_previous_tick_comparison='distance<=threshold if the other cached-target checks pass',
            same_tick_cache='Returns the cached target for the same tick before recomputing the range, even if caller mode differs.',
            meaning='For Line Of Sight targeting this DWORD is an unscaled radius in tile units, not4096-scaled distance.',
            evidence=[asm_range('phase12-trap-trigger-los-assembly.txt',0x4A9290,0x4A92A3),
                asm_range('phase12-trap-target-range-assembly.txt',0x4A9417,0x4A9459),
                asm_range('phase12-trap-target-range-assembly.txt',0x4A9526,0x4A9534),
                asm_range('phase12-trap-target-range-assembly.txt',0x4A9614,0x4A961E),
                asm_range('phase12-trap-tick-assembly.txt',0x4A8AB4,0x4A8AC1)]),
        charge_counter=dict(storage_path=path(0x1CC),loader='0x4aa440',instance_counter_offset=0x10B,instance_counter_bytes=1,
            threshold_rule='((raw_ChargeTime * ticks_per_second) modulo2**32) >>12; logical shift',
            step_rule='On a positive trigger callback result: counter=(counter+1)&255, then compare unsigned counter>=threshold. Negative result resets counter=0.',
            reachability='threshold>255 cannot be reached by this byte counter in the inspected tick branch; threshold0 still requires a positive callback result.',
            limitation='This is one polling/charge gate, not a complete trap lifecycle or guarantee that a shot fires; subsequent state and mana checks remain.',
            evidence=[asm_range('phase12-trap-loader-assembly.txt',0x4AA9D6,0x4AA9E9),
                asm_range('phase12-trap-tick-assembly.txt',0x4A8BE9,0x4A8C38),
                asm_range('phase12-trap-tick-assembly.txt',0x4A8C64,0x4A8C64)]),
        unresolved=dict(fields=['Traps+0x204','Terrain+0x198','Terrain+0x224'],
            rejected_candidate=dict(address='0x4f8750',instruction='0x4f8c1c',reason='This is the MyComputerPlayer initialization routine, with config-style source offsets. Identity of source+0x98 as a Trap definition is not established; it is excluded as semantic evidence for Traps+0x204.',
                evidence=proof('phase12-trap98-candidate-init.txt','uVar1 = *(ushort *)(param_2 + 0x98)')),
            observation='Exact field-displacement scan did not establish a semantic consumer. Pointer aliases, adjusted bases and unscanned code may evade it.',
            preserve='Keep both raw bytes of Traps+0x204 and all unresolved Terrain bytes; no ManaGen label is asserted.'),
        limitation='Static analysis of the exact binaries. GUI labels, enum tables, read/write projection and consumer arithmetic are distinct. No modified files were run in game/editor.')
    if (ROOT/'structure-overlay-semantics.json').exists():result['phase59_structure_overlay']=json.loads((ROOT/'structure-overlay-semantics.json').read_text(encoding='utf8'))
    if (ROOT/'trap-writer-semantics.json').exists():result['phase60_editor_writer']=json.loads((ROOT/'trap-writer-semantics.json').read_text(encoding='utf8'))
    if (ROOT/'trap-loading-semantics.json').exists():result['phase61_full_loading']=json.loads((ROOT/'trap-loading-semantics.json').read_text(encoding='utf8'))
    if (ROOT/'entity-definition-semantics.json').exists():result['phase62_old_entity_definitions']=json.loads((ROOT/'entity-definition-semantics.json').read_text(encoding='utf8'))
    if (ROOT/'trap-readiness-semantics.json').exists():result['phase68_readiness']=json.loads((ROOT/'trap-readiness-semantics.json').read_text(encoding='utf8'))
    if (ROOT/'entity-kind-filter-semantics.json').exists():result['phase69_entity_filter']=json.loads((ROOT/'entity-kind-filter-semantics.json').read_text(encoding='utf8'))
    (ROOT/'trap-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Trap semantics:3 native trigger options/callbacks;17 supported flag bits;LOS radius;byte charge gate')

if __name__=='__main__':main()
