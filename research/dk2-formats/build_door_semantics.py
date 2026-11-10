"""Native Doors scalar boundaries, flags and the separate door firing path."""
from pathlib import Path
import hashlib,json
from build_effect_semantics import asm_range

ROOT=Path(__file__).resolve().parent

def main():
    def proof(file,needle):
        s=(ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines()
        assert not any(s[0].startswith(v) for v in ['Unknown tool:','Error:','The automatic','Automatic approval'])
        i=next(i for i,line in enumerate(s) if needle in line)
        return dict(file=file,line=i+1,excerpt=s[i].strip())
    paths=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']['Doors']['joined_paths']
    def path(offset):return next(p for p in paths if int(p['raw_offset'],16)==offset)
    dialogs=json.loads((ROOT/'editor-dialogs.json').read_text(encoding='utf8'))['dialogs']
    controls={c['id']:c for c in next(d for d in dialogs if d['resource_path'][1]==254)['controls']}
    scalar=[]
    for raw,model,panel,cid,label,lo,hi in [
        (0x22E,0x288,0x5C,1269,'Gold &Cost:',0,32767),
        (0x22C,0x28E,0x60,1264,'&Health:',0,32767),
        (0x21C,0x290,0x64,1265,'Health Gain:',0,32767),
        (0x238,0x28C,0x68,1263,'&Mana Cost:',-32767,32767),
        (0x234,0x292,0x70,12691,'Manf. To &Build:',0,32767)]:
        scalar.append(dict(storage_path=path(raw),editor_model_offset=model,panel_offset=panel,
            native_edit_control=cid,label=label,edit_range=[lo,hi],template_visible=controls[cid]['template_visible']))
    exes=json.loads((ROOT/'executable-hash-verification.json').read_text(encoding='utf8'))
    for file,digest in exes.items():assert hashlib.sha256((ROOT.parent.parent/file).read_bytes()).hexdigest()==digest
    result=dict(family='Doors',record_bytes=616,executables=exes,
        functions=dict(game_converter='0x56aa40',game_loader='0x4a7100',runtime_constructor='0x4a7040',
            editor_converter='0x42a5d0',editor_model_load='0x4a1320',editor_writer='0x4a8bf0',
            editor_fill='0x447590',editor_commit='0x447880',editor_ddx='0x4466f0',game_tick='0x4a55f0'),
        native_scalar_bindings=scalar,
        manufacture_split=dict(baseline_name='ManufToBuild',baseline_raw_offset=0x234,baseline_raw_bytes=4,
            native_low_word=dict(raw_offset=0x234,bytes=2,intermediate_offset=0x2A8,runtime_offset=0x95,editor_model_offset=0x292,
                label='Manf. To &Build:',meaning_status='native editor label; game loader copy'),
            native_high_word=dict(raw_offset=0x236,bytes=2,intermediate_offset=0x2AA,runtime_offset=0x99,editor_model_offset=0x28A,
                meaning_status='unresolved; copied and written separately, no binding in inspected door properties panel'),
            rule='Expose two physical u16 spans in the supplemental schema. Preserve the upper word when editing manufacture requirement.',
            evidence=[proof('game-doors-convert.txt','param_2 + 0x234'),proof('game-doors-convert.txt','param_2 + 0x236'),
                proof('game-doors-runtime.txt','param_1 + 0x95'),proof('game-doors-runtime.txt','param_1 + 0x99'),
                proof('phase14-editor-door-model-load.txt','this + 0x28a'),proof('phase14-editor-door-model-load.txt','this + 0x292'),
                proof('doors-catalogue-writer.txt','uStack_4a ='),proof('doors-catalogue-writer.txt','uStack_4c ='),
                proof('phase14-editor-door-fill.txt','this + 0x70'),proof('phase14-editor-door-ddx-verified.txt','0x3193')]),
        health_gain=dict(raw_offset=0x21C,bytes=2,runtime_offset=0x93,game_read='signed16',gui_range=[0,32767],
            health_max=dict(raw_offset=0x22C,runtime_offset=0x91,read='unsigned16'),
            next_tick_instance_offset=0x11A,gate='not dead and unsigned current_tick > next_health_tick and signed16 HealthGain !=0',
            delta_rule='s16(low16(min_signed32(s32(u16(max_health)-current_health),s16(HealthGain))))',
            next_tick_rule='old next_health_tick + ticks_per_second modulo2**32, including a zero resulting delta',
            caveat='One update per tick invocation; does not catch up with a loop. Full lifecycle and other tick behavior are outside this projection.',
            evidence=[asm_range('phase14-door-tick-assembly.txt',0x4A5617,0x4A5680),
                proof('phase14-door-health-apply.txt','param_4 = param_1[0x10] + param_2'),
                proof('phase14-editor-door-fill.txt','(int)*(short *)(iVar4 + 0x290)'),
                proof('phase14-editor-door-ddx-verified.txt','param_1 + 100),0,0x7fff')]),
        editor_flags=dict(raw_offset=0x228,bytes=4,intermediate_offset=0x29C,runtime_offset=0x8B,
            supported_mask=0xE0,dropped_mask=0xFFFFFF1F,decode='0x41f510',encode='0x41f4e0',
            bits=[dict(mask=0x20,model_offset=0x29C,checkbox=1784,label='&Secret'),
                dict(mask=0x40,model_offset=0x2A0,checkbox=1786,label='Barricade'),
                dict(mask=0x80,model_offset=0x2A4,checkbox=1787,label='Good')],
            rule='Native full decode/encode reconstructs only bits5,6,7; lossless serialization must retain all other raw bits.',
            evidence=[proof('phase14-editor-door-flags-decode.txt','param_1 >> 5 & 1'),
                proof('phase14-editor-door-flags-encode.txt','uVar1 | 0x80'),
                proof('doors-catalogue-writer.txt','uStack_58 = FUN_0041f4e0'),
                proof('phase14-editor-door-ddx-verified.txt','param_1 + 0xa4')]),
        untransferred_spans=[dict(raw_offset=o,bytes=n,meaning_status='unresolved',
            observation='No field transfer in the inspected game/editor raw converters. Preserve bytes; do not claim global non-use.')
            for o,n in [(0x21E,2),(0x220,4),(0x230,2)]],
        door_shot=dict(trap_type_path=path(0x227),trap_getter_vtable_offset=0x160,
            prerequisites='Nonzero TrapTypeId, due unsigned shot timer, selected eligible target and closed door; full target selection is not modeled.',
            mana_gate='Trap.ManaCostToFire read as signed32; helper returns cost==0 or cost<=player_mana (signed comparison).',
            initial_origin='door XYZ, with z+=0x800 modulo2**32',uses_trap_shot_offsets=False,
            shot_type=dict(trap_raw_offset=0x20D,trap_runtime_offset=0xBB),
            payload=[dict(trap_raw_offset=0x1E0,trap_runtime_offset=0x7C,shot_offset=0x13D),
                dict(trap_raw_offset=0x1E4,trap_runtime_offset=0x80,shot_offset=0x141)],
            source_tag_shot_offset=0x145,target_tag_shot_offset=0x147,target_helper='0x4ab830',
            trap16_override=False,uses_trap_charge_gate=False,uses_trap_threat_reset=False,
            failed_factory='On the mana-allowed attempt path, factory failure jumps to mana accounting and recharge deadline update. Payload/tag assignments are success-only.',
            deadline='current_tick + Trap.RechargeTime runtime ticks modulo2**32',
            unavailable_mana='Optional ManaCostToFireEffectId at door XYZ, then exits without deadline update in this branch.',
            scope='Selected assignments in this separate native door shot path; other projectile motion and full door tick are not modeled.',
            evidence=[asm_range('phase14-door-tick-assembly.txt',0x4A6186,0x4A61AB),
                asm_range('phase14-door-tick-assembly.txt',0x4A629D,0x4A62E5),
                asm_range('phase14-door-tick-assembly.txt',0x4A6328,0x4A6378),
                asm_range('phase14-door-tick-assembly.txt',0x4A63C9,0x4A640C),
                asm_range('phase14-door-tick-assembly.txt',0x4A6431,0x4A6469),
                proof('phase14-door-mana-gate.txt','return param_2 <='),
                proof('phase14-door-mana-adjust.txt','param_1 + 0x82) + param_2')]),
        exploratory_query_notes='Some phase14 filenames reflect search guesses: flags-decode query0x41f520 returned entry0x41f510; commit query0x447980 returned0x447880. door-convert0x42b050 was only a header-copy helper. Only verified function entries above are used.',
        limitation='Static Ghidra evidence and read-only projections; no modified game data executed. Upper manufacture word and remaining unknown spans retain unresolved status.')
    if (ROOT/'structure-overlay-semantics.json').exists():
        import hashlib
        p=ROOT/'structure-overlay-semantics.json';s=json.loads(p.read_text(encoding='utf8'));result['phase59_height_consumer']=dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),field=s['fields']['Doors'],scope=s['consumer'])
    if (ROOT/'entity-definition-semantics.json').exists():
        from build_entity_definition_semantics import link
        result['phase62_old_entity_definitions']=link('Doors')
    previous=ROOT/'door-semantics.json'
    if previous.exists():
        saved=json.loads(previous.read_text(encoding='utf8'))
        if 'phase66_writer' in saved:result['phase66_writer']=saved['phase66_writer']
        if 'phase67_loading' in saved:result['phase67_loading']=saved['phase67_loading']
        if 'phase68_readiness_review' in saved:result['phase68_readiness_review']=saved['phase68_readiness_review']
        if 'phase69_consumer_review' in saved:result['phase69_consumer_review']=saved['phase69_consumer_review']
    (ROOT/'door-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Door semantics:5 scalar GUI bindings, manufacture split,3 flag bits, health tick and separate shot path')

if __name__=='__main__':main()
