"""Native effect dispatch and storage semantics from saved executable evidence."""
from pathlib import Path
import hashlib, json, re, struct

ROOT = Path(__file__).resolve().parent
EXE = ROOT.parent.parent / 'Dungeon Keeper 2/DKII-DX.exe'

def save(name, value):
    (ROOT/name).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf8')

def pe_read(data, address, size):
    pe = struct.unpack_from('<I', data, 60)[0]
    count = struct.unpack_from('<H', data, pe+6)[0]
    optional_size = struct.unpack_from('<H', data, pe+20)[0]
    image_base = struct.unpack_from('<I', data, pe+52)[0]
    for i in range(count):
        offset = pe+24+optional_size+40*i
        virtual_size, rva, raw_size, raw_offset = struct.unpack_from('<IIII', data, offset+8)
        delta = address-image_base-rva
        if 0 <= delta < max(virtual_size, raw_size):
            assert delta+size <= raw_size
            return data[raw_offset+delta:raw_offset+delta+size]
    raise ValueError(hex(address))

def asm_range(file, first, last):
    lines = (ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines()
    selected = [line for line in lines if re.match(r'^[0-9A-Fa-f]{8}:', line)
                and first <= int(line[:8],16) <= last]
    assert selected and int(selected[0][:8],16) == first
    return dict(file=file, first=hex(first), last=hex(last), instructions=selected)

def main():
    data = EXE.read_bytes()
    layouts = json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))
    enums = json.loads((ROOT/'enums.json').read_text(encoding='utf8'))
    paths = json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']
    dispatch = []
    for i in range(19):
        raw = pe_read(data, 0x697668+5*i, 5)
        assert raw[0] == i
        target = struct.unpack_from('<I',raw,1)[0]
        dispatch.append(dict(value=i, baseline_name=enums['Effect.GenerationType'][str(i)],
            callback=hex(target), table_entry_address=hex(0x697668+5*i),
            evidence_file=None if target==0 else 'game-effect-generation-'+str(13 if i==17 else i)+'.txt'))
    assert dispatch[13]['callback']==dispatch[17]['callback']=='0x4375a0'
    assert dispatch[16]['callback']=='0x5282f0'
    rules = [
        dict(id='generation_type_unchecked_index', family='Effects', raw_offset=0xF2,
             description='u8 indexes packed 5-byte table at0x697668 without a range check. Defined entries0..18. Entry0 has a null callback.',
             evidence=asm_range('game-effect-tick-assembly.txt',0x4350E1,0x435103)),
        dict(id='cube_generation_gate', family='Effects', raw_offset=0xEC,
             description='CUBE_GEN reads GenerateRandomness as signed16. Calls RNG(s16(raw)+1) and emits only when result==0 and instance lifetime>0. RNG(0) logs and returns0, not a division crash.',
             evidence=asm_range('game-effect-generation-4-assembly.txt',0x438841,0x43886E)),
        dict(id='cube_target_table', family='Effects', raw_offset=0xB4,
             description='CUBE_GEN uses flag0x40 to choose Effects. With the bit clear it uses EffectElements; bit0x80 is not required by this branch.',
             evidence=asm_range('game-effect-generation-4-assembly.txt',0x43895A,0x43897A)),
        dict(id='cube_whirlpool_signed_conversion', family='Effects', raw_offset=0xE4,
             description='Read signed16 r; if r>=128 subtract256 once; pass r*16 to whirlpool helper. Applies only in the element branch. This is not a low-byte cast for arbitrary raw values.',
             evidence=asm_range('game-effect-generation-4-assembly.txt',0x43870F,0x438738)),
        dict(id='cube_circular_signed_conversion', family='Effects', raw_offset=0xE8,
             description='Element branch: signed16 r, subtract256 once if r>=128, then helper clamps to[-1024,1023]. Effect branch: clamp signed16 directly, without subtract256.',
             evidence=asm_range('game-effect-generation-4-assembly.txt',0x43873D,0x43882D)),
        dict(id='cube_radius_height_units', family='Effects', raw_offset=0xDA,
             description='CUBE_GEN sign-extends tail range words. Horizontal radius raw*23 native position units; height raw*16. Position coordinates use4096 units per tile. Keep signedness and raw u16 storage distinct.',
             evidence=asm_range('game-effect-generation-4-assembly.txt',0x438880,0x438923)),
        dict(id='element_collision_priority', family='EffectElements', raw_offset=0xAA,
             description='In the below-surface element branch, choose first nonzero runtime slot+46,+48,+44: raw HitLavaElementId, then HitWaterElementId, then HitSolidElementId. No terrain-kind test occurs within this branch. The separate horizontal collision path uses HitSolidElementId.',
             evidence=asm_range('game-effect-location-apply-assembly.txt',0x4471EE,0x4472B3)),
        dict(id='element_next_effect_per_update', family='EffectElements', raw_offset=0xB4,
             description='After decreasing element lifetime, its update calls bridge+94 with runtime NextEffectId+40 whenever nonzero. Bridge+94 forwards to effect creation, not element creation. This path runs once per update call, outside the physics substep loop.',
             evidence=asm_range('game-element-tick-assembly.txt',0x436430,0x4364A0)),
        dict(id='spray_overloaded_origin_field', family='Effects', raw_offset=0xDA,
             description='SPRAY(type6) uses runtime+37, normally OuterOriginRange, as a secondary EffectId when elapsedTicks%3==0. The normal particles are selected from the numeric first..last ElementId range.',
             evidence=asm_range('game-effect-generation-6-assembly.txt',0x439409,0x439461)),
        dict(id='gold_cost_digit_series', family='Effects', raw_offset=0xCA,
             description='GOLD_COST(type14) uses first GenerateId as baseElementId and creates base+decimalDigit from instance+12F. Ten consecutive element definitions are implied; eight file slots are not the full digit set.',
             evidence=asm_range('game-effect-generation-14-assembly.txt',0x439539,0x439581)),
        dict(id='test3_four_slot_batches', family='Effects', raw_offset=0xF3,
             description='TEST_3(type18) loops floor(ElementsPerTurn/4) batches, each visiting physical GenerateIds3..0 and creating Effects only when flag0x40 is set. Slots4..7 are not generated by this handler.',
             evidence=asm_range('game-effect-generation-18-assembly.txt',0x439127,0x439169)),
    ]
    narrow = {}
    signed_fields = {'Mass','MinSpeedXy','MaxSpeedXy','MinSpeedYz','MaxSpeedYz'}
    for family in ['Effects','EffectElements']:
        narrow[family] = []
        for p in paths[family]['joined_paths']:
            if p['raw_bytes']==4 and p['runtime_bytes']==2:
                name = p['baseline_field']
                row = dict(field=name, raw_offset=int(p['raw_offset'],16), raw_size=4,
                    runtime_offset=int(p['runtime_offset'],16), runtime_size=2,
                    intermediate_offset=int(p['intermediate_offset'],16),
                    consumer_interpretation='signed16' if name in signed_fields else 'unsigned16' if name in {'AirFriction','Elasticity','MinScale','MaxScale'} else 'low16; signedness not established here')
                narrow[family].append(row)
    tail = []
    for i in range(12):
        raw_offset=0xDA+2*i
        f=next(x for x in layouts['Effects']['fields'] if x['offset']==raw_offset)
        tail.append(dict(field=f['name'],raw_offset=raw_offset,size=2,
            intermediate_offset=0x18E+2*i,runtime_offset=0x37+2*i,
            update_condition='runtime slot replaced only when intermediate value !=0 in0x4358C0'))
    result = dict(executable_sha256=hashlib.sha256(data).hexdigest(),
        scope='Version-specific native storage and branch semantics; no modified-map gameplay validation.',
        generation_dispatch=dict(address='0x697668',entry_bytes=5,entries=dispatch),
        runtime_definition_bytes={'Effects':148,'EffectElements':79},
        narrowed_fields=narrow, effect_tail_fields=tail, rules=rules,
        generate_ids=dict(raw_offset=0xCA,count=8,element_bytes=2,runtime_offset=0x4F,
            last_index_offset=0x93,last_index_rule='scan7..1 for last nonzero; otherwise0',
            selection='random index0..last inclusive. Holes and duplicate ids are retained; first/last accessors use the physical slots.',
            evidence_files=['game-effect-definition-load.txt','game-effect-pick-first.txt','game-effect-pick-last.txt','game-effect-pick-random.txt']),
        mode_specific_selection={
            '4':dict(kind='physical_slots',target='Effects if flag0x40 else EffectElements',
                selector='RANDOM_DISTRIBUTION flag0x400 chooses random physical slot; otherwise visit eight slots and skip0'),
            '5':dict(kind='numeric_id_range',target='EffectElements',bounds='first..last inclusive'),
            '6':dict(kind='numeric_id_range',target='EffectElements',bounds='first..last inclusive',
                overloaded_field=dict(raw_offset=0xDA,baseline_name='OuterOriginRange',native_role='SecondaryEffectId',target='Effects')),
            '7':dict(kind='numeric_id_range',target='EffectElements',bounds='first..last inclusive'),
            '8':dict(kind='numeric_id_range',target='EffectElements',bounds='first..last inclusive'),
            '9':dict(kind='numeric_id_range',target='EffectElements',bounds='first inclusive, last exclusive',
                evidence_file='game-effect-generation-9-assembly.txt'),
            '14':dict(kind='digit_element_series',target='EffectElements',base_slot=0,count=10,
                rule='base+digit0..9, low16 used by element lookup; independent of ElementsPerTurn'),
            '15':dict(kind='numeric_id_range',target='EffectElements',bounds='first..last inclusive'),
            '18':dict(kind='four_physical_slots',target='Effects',slots=[0,1,2,3],
                count='floor(ElementsPerTurn/4) batches',requires_flag=0x40)},
        physics=dict(function='0x4476E0',position_units_per_tile=4096,velocity_units_per_position_unit=16,
            update_order=['verticalVelocity -= signed16(MassRaw)',
                'each velocity component = trunc32(component*(4096-unsigned16(AirFrictionRaw)))/4096, truncating toward zero',
                'movement and collision; vertical position increment is velocityZ>>4'],
            friction_note='32-bit intermediate multiplication wraps. The file baseline AirFriction/65536 differs from the per-step raw/4096 coefficient; retain both views.',
            evidence_files=['game-effect-physics.txt','game-effect-physics-assembly.txt']),
        whirlpool=dict(function='0x438520',angle_units_per_turn=2048,
            formula='clamp(trunc((convertedRate*16*128)/(square(dx>>5)+square(dy>>5)+128)),-1024,1023)',
            effect='add position-displacement impulse to child+14/+18, scaled by16; does not directly overwrite ordinary velocity',
            warning='Integer arithmetic/overflow and trig-table quantization matter; formula is not the distance-linear approximation used in OpenKeeper.',
            evidence_files=['game-effect-cube-whirlpool.txt']),
        subsequent_findings={'Effects.NextEffectId':'See effect-chain-semantics.json: immediate chain creation in0x440350.',
            'EffectElements.ColorAndFade':'See element-color-semantics.json: initial color priority and distinct native RGB/alpha fade, with26 isolated emulations.'},
        unresolved=['Some generation modes have hard-coded behavior; names are OpenKeeper labels, not independent confirmation of every intended use.',
            'EffectElements RandomColorIndex/TableColorIndex: storage confirmed, full palette behavior unresolved.',
            'Effects raw+F4 is not read by inspected converter; preserve it.'])
    # Every asserted instruction range and referenced file must be locally reviewable.
    for row in dispatch:
        if row['evidence_file']:assert (ROOT/'ghidra'/row['evidence_file']).exists()
    assert len(narrow['Effects'])==10 and len(narrow['EffectElements'])==9
    save('effect-semantics.json',result)
    save('effect-generation-dispatch.json',result['generation_dispatch'])
    print('Native effect schema:19 generation values,19 narrowed fields,12 tail slots,',len(rules),'verified branch rules')

if __name__=='__main__':main()
