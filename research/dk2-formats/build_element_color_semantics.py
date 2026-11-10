"""Native EffectElements initial color and fade rules, with isolated emulation evidence."""
from pathlib import Path
import hashlib,json
from build_effect_semantics import asm_range
from build_phase17_semantics import proof

ROOT=Path(__file__).resolve().parent

def main():
    paths=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']['EffectElements']['joined_paths']
    fields=[]
    for offset,width,name in [(0xAE,1,'Color byte0'),(0xAF,1,'Color byte1'),(0xB0,1,'Color byte2'),
        (0xB1,1,'RandomColorIndex'),(0xB2,1,'TableColorIndex'),(0xB3,1,'FadePercentage')]:
        p=next(p for p in paths if int(p['raw_offset'],16)==offset);assert p['raw_bytes']==p['runtime_bytes']==width
        fields.append(dict(name=name,raw_offset=offset,raw_bytes=width,intermediate_offset=int(p['intermediate_offset'],16),
            runtime_offset=int(p['runtime_offset'],16),status='storage only; palette behavior unresolved' if offset in [0xB1,0xB2] else 'native color/fade consumer confirmed',
            evidence=[proof('game-effectelements-convert.txt',f'cVar1 = param_2[{hex(offset)}]'),
                proof('game-effectelements-convert.txt',p['converter_evidence'][-1]['assignment']),
                proof('game-element-definition-load.txt',f'param_1 + {p["runtime_offset"]}) =')]))
    emulations=json.loads((ROOT/'phase21-color-emulations.json').read_text(encoding='utf8'))['cases'];assert len(emulations)==26
    schema=dict(family='EffectElements',record_bytes=182,
        executable_sha256=json.loads((ROOT/'executable-hash-verification.json').read_text()),fields=fields,
        functions=dict(raw_converter=0x56B570,definition_loader=0x4366A0,initializer=0x435F20,tick=0x436430),
        instance_offsets=dict(definition_pointer=0x103,packed_color=0x6E,lifetime=0x109,alpha_threshold=0x10D,
            resource_pointer=0x4E,renderer_flags=0x81),
        resource=dict(raw_type_offset=0x70,raw_flags_offset=0x60,runtime_type_offset=0xA,runtime_flags_offset=0xB,
            player_coloured_mask=2,element_fade_flag_raw_offset=0x9C,element_fade_mask=8),
        initial_color=dict(rule_order=['resource type1 -> all channels255',
            'otherwise resource flag2 -> resolved player channels, each max(channel,48), alpha255',
            'otherwise raw color bytes+AE/+AF/+B0, alpha255'],
            packed_order='byte0 | byte1<<8 | byte2<<16 | alpha<<24; no implicit channel swap',
            player_provider='CBridge(global6DA458).vC4 supplies local owner. Matching owner uses connected engine(global6DA45C).v298(0);otherwise engine.v290(owner,0). Projection takes externally resolved RGB. Correct interface provenance confirmed in phase81.',
            evidence=asm_range('phase21-element-init-assembly.txt',0x43630B,0x4363B4)),
        fade_threshold=dict(raw_offset=0xB3,initial_hp_source='Selected u16 in inclusive MinHp..MaxHp interval; initial HP is supplied to the projection.',
            rule='threshold_bits = low32(H_initial - trunc(H_initial * FadePercentage /100))',
            compare='Threshold bits later compared as signed32, but used as unsigned32 divisor.',
            pct100='threshold0; alpha update skipped in this path',
            evidence=asm_range('phase21-element-init-assembly.txt',0x4361CB,0x4361FA)),
        fade_update=dict(order='Subtract elapsed ticks once, then evaluate fade once per update call.',
            gate='definition flags&8 and signed remaining lifetime!=0',
            rgb_gate='remaining lifetime <= maximum of current three color bytes',
            rgb='For each current channel: low8(trunc(s32(low32((H-1)*channel))/H)). Signed IDIV, not floating-point interpolation.',
            alpha_gate='threshold_bits !=0 and s32(H)<s32(threshold_bits)',
            alpha='low8(unsigned32(low32(H*255))/unsigned32(threshold_bits)); unsigned DIV',
            renderer='Only when alpha is written: resource types2,3,5 set bit0 of instance renderer flags; others unchanged.',
            batching='RGB can differ between one update(delta2) and two updates(delta1); this branch is not inside the physics substep loop.',
            negative_hp='Gate tests!=0, not>0. Projection preserves signed overflow/narrowing and does not clamp negative remaining life.',
            evidence=[asm_range('phase21-element-tick-assembly.txt',0x436430,0x436445),
                asm_range('phase21-element-tick-assembly.txt',0x4364DD,0x43659C)]),
        palette_indices=dict(raw_offsets=[0xB1,0xB2],runtime_offsets=[0x4D,0x4E],meaning='unresolved',
            inspected_scope='Neither index read in the inspected initializer color branch or tick fade branch.',
            warning='This does not prove global non-use. Packed/adjusted-pointer consumers and other binaries remain possible.'),
        native_emulations=emulations,
        emulation_scope='26 runs execute original color/threshold/fade branch instructions. Surrounding setup and exit readout are adapters in EmulatorHelper memory. Player providers are explicit stub palettes. EXE and Ghidra program bytes are not patched.',
        limitations=['No framebuffer/rendering or modified-map gameplay validation.',
            'Caller lifetime/deletion, resource loading and player-color lookup are outside the read-only projection.',
            'Renderer resource type is an explicit input to the tick model; it need not be inferred from a raw record in every runtime situation.',
            'Palette index semantics remain unresolved; nonzero index experiment only checks the isolated initializer color branch.'])
    extra=ROOT/'element-initialization-semantics.json'
    if extra.exists():schema['phase81_complete_initialization']=json.loads(extra.read_text(encoding='utf8'))
    (ROOT/'element-color-semantics.json').write_text(json.dumps(schema,ensure_ascii=False,indent=2),encoding='utf8')
    print('Element color schema:6 physical bytes, initial-color priority, RGB/alpha fade and26 isolated native emulations')

if __name__=='__main__':main()
