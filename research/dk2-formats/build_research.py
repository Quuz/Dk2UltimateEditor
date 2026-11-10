"""Generate research artifacts and verify file assembly without editing inputs."""
from collections import Counter
import csv
import hashlib
import json
from pathlib import Path
import re
import struct

import dk2_formats as fmt
from inventory import DATA, FILES

ROOT=Path(__file__).resolve().parent

def save(name,value):
    p=ROOT/name;p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')

def original_bindings():
    path=ROOT.parent.parent/'DK2-Map-Editor/Source/Sources/Compiler/Compiler_Generate.c'
    source=path.read_text(encoding='cp1252')
    pointer=r'\*\(\(\s*(?P<type>BYTE|WORD|LONG|DWORD|short)\s*\*\)\(\s*(?:Terrain|Room|Door|Trap|Spell|Creature|CrSpell)\s*\+\s*(?P<offset>0x[0-9A-Fa-f]+)\s*\)\)'
    prop=r'MapGlobalVariables\.(?P<group>\w+)\[Offset\]\.(?P<name>\w+(?:\[\d+\])?)'
    found=[]
    patterns=[(pointer+r'\s*=\s*(?:\([^)]+\))?\s*'+prop+r'\s*;',False),
              (r'if\s*\(\s*'+prop+r'\s*\)\s*'+pointer+r'\s*\|=\s*(?:\([^)]+\))?\s*(?P<mask>0x[0-9A-Fa-f]+)\s*;',True),
              (pointer+r'\s*\|=\s*(?:\([^)]+\))?\s*\(\s*\('+prop+r'\)\s*\?\s*(?P<mask>0x[0-9A-Fa-f]+)\s*:\s*0x00\s*\)\s*;',True)]
    mapping={'Terrains':'Terrain','Rooms':'Rooms','Doors':'Doors','Traps':'Traps','Spells':'KeeperSpells','Creatures':'Creatures','CrSpells':'CreatureSpells'}
    for pattern,flag in patterns:
        for m in re.finditer(pattern,source):
            t=m['type'];size=1 if t=='BYTE' else 2 if t in ('WORD','short') else 4
            found.append(dict(family=mapping[m['group']],name=m['name'],offset=int(m['offset'],16),size=size,
                              mask=int(m['mask'],16) if flag else None,line=source[:m.start()].count('\n')+1))
    save('legacy-bindings.json',found)
    compare=[]
    for family,layout in fmt.LAYOUTS.items():
        baseline=family.removesuffix('_5537')
        for f in layout['fields']:
            overlapping=[b for b in found if b['family']==baseline and b['offset']<f['offset']+f['size'] and f['offset']<b['offset']+b['size']]
            compare.append(dict(family=family,**f,legacy_names=[b['name'] for b in overlapping]))
    with (ROOT/'field-comparison.csv').open('w',encoding='utf-8-sig',newline='') as fp:
        writer=csv.DictWriter(fp,fieldnames=['family','offset_hex','offset','size','kind','name','enum','semantics','source_line','legacy_names'])
        writer.writeheader()
        for row in compare:row['legacy_names']='; '.join(row['legacy_names']);writer.writerow(row)
    return found,compare

def field_docs():
    lines=['# Смещения записей KWD/KLD','',
           'Смещения в таблицах считаются от начала записи, без 56-байтового заголовка каталога. Для Things — от начала полезной части после 8-байтового тега.',
           'Последовательность чтения взята из закреплённой версии OpenKeeper; все длины проверены на файлах пользователя. Названия отражают интерпретацию OpenKeeper. Это не независимое подтверждение игровой семантики каждого поля.',
           'Неизвестные поля, объединения ArtResource и нераспознанные биты нужно сохранять. `Unk`, `Unknown`, `X...` не заменять выдуманными названиями.',
           '`i32/4096` и `i32/65536` — целые числа с фиксированной точкой, НЕ IEEE float. `i16/4096` — знаковый 16-битный fixed-point. Все числа little-endian.',
           'Полная машинная схема: `layouts.json`, `thing-layouts.json`; перечисления и маски: `enums.json`.',
           'Исправления по движку находятся в `verified-overrides.json` и `ENGINE_FINDINGS_RU.md`. В частности Creatures `0xF20` игра читает как **u16, угловые единицы (2048 на полный оборот)**; показанное ниже i16/4096 является исходной трактовкой OpenKeeper и для игрового UI неверно. Дополнительные игровые флаги читаются по `0x1545`; редакторский слот `0x1599` хранить отдельно.', '']
    for name,layout in fmt.LAYOUTS.items():
        lines+=['## '+name+' — '+str(layout['record_size'])+' байт','',
                '| Смещение | Байт | Тип | Поле | Перечисление / статус |','|---|---:|---|---|---|']
        for f in layout['fields']:
            status=f.get('enum','')+(' НЕИЗВЕСТНО' if f['semantics']=='unresolved' else '')
            lines.append(f"| `{f['offset_hex']}` | {f['size']} | `{f['kind']}` | `{f['name']}` | {status} |")
        lines.append('')
    (ROOT/'FIELD_LAYOUTS.md').write_text('\n'.join(lines),encoding='utf-8')
    lines=['# Things: размещённые объекты карты','',
           'Общий заголовок секции — 56 байт. Далее идут записи: `u32 type`, `u32 payloadBytes`, затем полезная часть. Следующая запись начинается через `8+payloadBytes` байт. Таблицы ниже НЕ включают первые 8 байт.',
           'Схемы — интерпретации OpenKeeper. Типы и длины подтверждены оригинальным диспетчером/писателем и наблюдаемыми файлами; смысл не всех полей установлен.', '']
    for tag,layout in fmt.THINGS.items():
        lines+=['## '+tag+' '+layout['name']+' — '+str(layout['payload_size'])+' байт полезной части','',
                '| Смещение | Байт | Тип | Поле |','|---|---:|---|---|']
        for f in layout['fields']:lines.append(f"| `{f['offset_hex']}` | {f['size']} | `{f['kind']}` | `{f['name']}` |")
        lines.append('')
    (ROOT/'THINGS_LAYOUTS.md').write_text('\n'.join(lines),encoding='utf-8')
    lines=['# Перечисления и флаги','',
           'Названия и значения из закреплённой версии OpenKeeper. Неизвестные значения и биты сохранять. Название enum само по себе не доказывает игровой эффект.', '']
    for name,values in fmt.ENUMS.items():
        lines+=['## '+name,'','| Число | Hex | Имя |','|---:|---|---|']
        lines += [f'| {value} | `0x{int(value):X}` | `{label}` |' for value,label in values.items()]
        lines.append('')
    (ROOT/'ENUMS.md').write_text('\n'.join(lines),encoding='utf-8')

def workbook_compare():
    rows=json.loads((ROOT/'workbook-cells.json').read_text(encoding='utf-8'))
    b=(DATA/'Creatures.kwd').read_bytes();result={}
    for sheet,offset,size in [('Creature Hitboxes',0xEA0,4),('Creature Turning Speed',0xF20,2)]:
        xml='sheet8.xml' if offset==0xEA0 else 'sheet9.xml';items=[]
        for row in rows:
            rn=int(row['row'])
            if not row['sheet_xml'].endswith('/'+xml) or not 2<=rn<=47:continue
            cells={re.sub(r'\d+','',c['cell']):c for c in row['cells']}
            rec=rn-2;position=56+rec*5449+offset
            v=int(float(cells['D']['value']));actual=int.from_bytes(b[position:position+size],'little')
            name=fmt.string(b[56+rec*5449:88+rec*5449])
            items.append(dict(sheet_row=rn,name_in_sheet=cells['A']['value'],name_in_file=name,
                              sheet_raw=v,file_raw=actual,match=v==actual,file_offset=position))
        result[sheet]=dict(record_offset=f'0x{offset:X}',storage_bytes=size,workbook_scale=4096,
                           engine_units_note=('Width copied low16, consumers use width>>1 for half-width; raw fixed-point /4096' if offset==0xEA0 else 'Unsigned angular units: 2048/full turn; workbook /4096 is not game angle conversion'),comparisons=items,
                           matched=sum(x['match'] for x in items),total=len(items))
    save('workbook-comparison.json',result)
    return result

def main():
    output=ROOT/'decoded';output.mkdir(exist_ok=True)
    files=[DATA/name for name in FILES]+[DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']
    checks=[];summaries=[];sections_seen=0
    for path in files:
        original=path.read_bytes();document=fmt.inspect(path)
        save(Path('decoded')/(path.name+'.json'),document)
        rebuilt=fmt.rebuild_decoded(json.loads(json.dumps(document)))
        assert rebuilt==original,path.name
        parsed=fmt.parse(original);sections_seen+=len(parsed)
        summary=dict(file=path.name,size=len(original),sha256=hashlib.sha256(original).hexdigest(),sections=[])
        for section,item in zip(parsed,document['sections']):
            row={k:v for k,v in item.items() if k in ('offset','signature','family','count','stride','width','height','physical_size','data_size','data_offset','type_counts')}
            summary['sections'].append(row)
            if section.signature in fmt.FAMILIES:
                family=fmt.FAMILIES[section.signature]
                layout_name=family+('_5537' if section.stride==5537 else '')
                fields=fmt.LAYOUTS[layout_name]['fields']
                assert fields[-1]['offset']+fields[-1]['size']==section.stride
                assert all(a['offset']+a['size']==b['offset'] for a,b in zip(fields,fields[1:]))
                # Variable count build: grow/remove in memory; do not write inputs.
                section.replace_records(section.records+[section.records[-1]])
                reparsed=fmt.parse(section.pack())[0]
                assert len(reparsed.records)==len(section.records)
                assert u32check(reparsed.header,8)==len(reparsed.pack())
                assert u32check(reparsed.header,52)==len(reparsed.data)
                section.replace_records(section.records[:-1])
            elif section.signature==0xBE:
                section.replace_records(section.records+[section.records[-1]])
                assert len(fmt.parse(section.pack())[0].records)==len(section.records)
                section.replace_records(section.records[:-1])
        assert b''.join(s.pack() for s in parsed)==original
        summaries.append(summary);checks.append(dict(file=path.name,sha256=summary['sha256'],roundtrip_exact=True))
    # Typed patch checks, including a 32-bit scaled field and a 16-bit one.
    original=fmt.parse((DATA/'Creatures.kwd').read_bytes())[0].records[0]
    for name,value in [('Hp',1234),('Speed','1.25'),('Unk3cc','0.5')]:
        field=next(f for f in fmt.LAYOUTS['Creatures']['fields'] if f['name']==name)
        patched=fmt.set_scalar(original,'Creatures',name,value)
        start=field['offset'];end=start+field['size']
        assert original[:start]==patched[:start] and original[end:]==patched[end:]
        decoded=fmt.decode_field(patched,field)
        assert decoded==int(value) if '/' not in field['kind'] else decoded['value']==float(value)
    # Engine correction must permit the upper half of the unsigned angle range.
    for raw in (0,2048,32768,65535):
        patched=fmt.set_engine_scalar(original,'MaxAngleChangeRaw',raw)
        assert fmt.u16(patched,0xF20)==raw
        assert patched[:0xF20]==original[:0xF20] and patched[0xF22:]==original[0xF22:]
    for slot in (0,47):
        patched=fmt.set_footstep_frames(original,slot,17,65535)
        offset=0x1323+4*slot
        assert struct.unpack_from('<HH',patched,offset)==(17,65535)
        assert original[:offset]==patched[:offset] and original[offset+4:]==patched[offset+4:]
        pair=fmt.decode_engine_fields(patched)['FootstepFrames']['items'][slot]
        assert pair['LeftFrame']==17 and pair['RightFrame']==65535
    footsteps=fmt.decode_engine_fields(original)['FootstepFrames']['items']
    assert footsteps[1]['resource_field']=='Animation.WALK' and footsteps[1]['resource_raw_offset']=='0x74'
    assert footsteps[0]['loader_supports_resource_slot'] is False
    assert struct.unpack_from('<HH',original,0x1327)==(footsteps[1]['LeftFrame'],footsteps[1]['RightFrame'])
    spell_slots=fmt.decode_engine_fields(original)['CreatureSpellSlots']
    assert spell_slots['count']==3 and spell_slots['element_size']==24
    # Native low16 storage must not destroy the upper half of file fields.
    effect_section=fmt.parse((DATA/'Effects.kwd').read_bytes())[0]
    effect=bytearray(effect_section.records[0])
    struct.pack_into('<I',effect,0x8C,0x1234FFFF)
    effect[0xF2]=4
    struct.pack_into('<H',effect,0xE4,512)
    struct.pack_into('<H',effect,0xE8,128)
    struct.pack_into('<8H',effect,0xCA,1,0,1,0,0,0,0,0)
    for flags,target,circular in [(0,'EffectElements',-128),(0x80,'EffectElements',-128),
                                  (0x40,'Effects',128),(0xC0,'Effects',128)]:
        struct.pack_into('<I',effect,0xB4,flags)
        view=fmt.decode_effect_engine_fields(effect,'Effects')
        assert view['NarrowedScalars']['Mass']['raw_u32']==0x1234FFFF
        assert view['NarrowedScalars']['Mass']['effective_i16']==-1
        assert view['CubeGeneration']['target_family']==target
        assert view['CubeGeneration']['circular_converted_rate']==circular
        assert view['CubeGeneration']['whirlpool_converted_rate']==256 # Not a signed-byte cast.
        assert view['GenerateIdSlots']['ids']==[1,0,1,0,0,0,0,0]
        assert view['GenerateIdSlots']['random_slot_count']==3
    effect_section.replace_records([bytes(effect)]+effect_section.records[1:])
    exported=dict(sections=[fmt.decoded(effect_section)])
    assert fmt.rebuild_decoded(json.loads(json.dumps(exported)))==effect_section.pack()
    for value in (0,18,19,255):
        sample=fmt.set_scalar(effect_section.records[0],'Effects','GenerationType',value)
        effect_section.replace_records([sample]+effect_section.records[1:])
        findings=fmt.compatibility_findings(dict(sections=[fmt.decoded(effect_section)]))
        assert any(x['code']=='effect_generation_type_outside_table' and x['record_index']==0 for x in findings)==(value>=19)
    native_effects=fmt.parse((DATA/'Effects.kwd').read_bytes())[0].records
    frost=next(r for r in native_effects if fmt.u16(r,0xB8)==27)
    assert fmt.decode_effect_engine_fields(frost,'Effects')['SprayGeneration']['secondary_effect_id']==14
    for effect_id,base in [(22,27),(364,573)]:
        record=next(r for r in native_effects if fmt.u16(r,0xB8)==effect_id)
        assert fmt.decode_effect_engine_fields(record,'Effects')['DigitGeneration']['digit_element_ids']==list(range(base,base+10))
    # Distinct native16/32-bit timer arithmetic; do not confuse either with point cost.
    keeper_section=fmt.parse((DATA/'KeeperSpells.kwd').read_bytes())[0]
    keeper=bytearray(keeper_section.records[0])
    struct.pack_into('<H',keeper,0xD8,500);struct.pack_into('<H',keeper,0x111,1500)
    struct.pack_into('<I',keeper,0xCC,4096)
    projection=fmt.decode_spell_engine_fields(keeper,'KeeperSpells',simulation_tps=20)
    assert projection['ResearchTimeLoader']['runtime_value']==2
    assert projection['BonusResearchTimeLoader']['runtime_value']==7
    assert projection['RechargeTimeLoader']['runtime_value']==20
    struct.pack_into('<H',keeper,0xD8,500+32768)
    assert fmt.decode_spell_engine_fields(keeper,'KeeperSpells',20)['ResearchTimeLoader']['runtime_value']==2
    assert 'runtime_value' not in fmt.decode_spell_engine_fields(keeper,'KeeperSpells')['ResearchTimeLoader']
    for offset,valid_max,invalid in [(0xEF,5,6),(0xDA,6,7)]:
        for value in [valid_max,invalid,255]:
            changed=bytearray(keeper);changed[offset]=value
            # Keep the other selector valid while testing each independent guard.
            changed[0xDA if offset==0xEF else 0xEF]=1
            keeper_section.replace_records([bytes(changed)]+keeper_section.records[1:])
            findings=fmt.compatibility_findings(dict(sections=[fmt.decoded(keeper_section)]))
            assert any(x['code']=='keeper_spell_selector_rejected' and x['record_index']==0 for x in findings)==(value>valid_max)
    creature_spells=fmt.parse((DATA/'CreatureSpells.kwd').read_bytes())[0]
    archer=next(r for r in creature_spells.records if r[0xE6]==12)
    legacy=fmt.decode_spell_engine_fields(archer,'CreatureSpells')['UntransferredAlternativeReferences']
    assert (legacy['AlternativeShotId'],legacy['AlternativeRoomId'])==(36,4)
    for value in [5,6,255]:
        changed=bytearray(creature_spells.records[0]);changed[0xEE]=value
        changed[0xEF:]=bytes(range(27))
        creature_spells.replace_records([bytes(changed)]+creature_spells.records[1:])
        document=dict(sections=[fmt.decoded(creature_spells)])
        assert fmt.rebuild_decoded(json.loads(json.dumps(document)))==creature_spells.pack()
        assert any(x['code']=='creature_alternative_selector_rejected' and x['record_index']==0
                   for x in fmt.compatibility_findings(document))==(value>=6)
    long_record=next(s for s in fmt.parse((DATA/'AutoSaveGlobals.kld').read_bytes()) if s.signature==0xAA).records[0]
    patched=fmt.set_engine_scalar(long_record,'GameExtraFlagsRaw',0x80000009)
    assert fmt.u32(patched,0x1545)==0x80000009
    assert patched[0x1549:]==long_record[0x1549:] # Includes the independent editor slot.
    try:fmt.set_engine_scalar(original,'EditorExtraFlagsRaw',1)
    except ValueError:pass
    else:raise AssertionError('Short record unexpectedly accepts editor extension')
    level=fmt.inspect(DATA/'AutoSave.kwd')['sections'][0]
    assert len([k for k in level['properties_fields'] if k.startswith('Messages[')])==20
    assert len(level['creature_list'])==level['extra_word_count']==23
    # Format-valid inputs can still crash the original loader/runtime.
    shots=fmt.parse((DATA/'Shots.kwd').read_bytes())[0]
    sample=fmt.set_scalar(shots.records[0],'Shots','TimedEffectId',1)
    sample=fmt.set_scalar(sample,'Shots','TimedDelay',0)
    shots.replace_records([sample]+shots.records[1:])
    findings=fmt.compatibility_findings(dict(sections=[fmt.decoded(shots)]))
    assert any(x['code']=='zero_timed_effect_delay' and x['record_index']==0 for x in findings)
    creatures=fmt.parse((DATA/'Creatures.kwd').read_bytes())[0]
    changed_swipe=fmt.set_engine_scalar(creatures.records[0],'SwipeSpeedRaw',0)
    changed_swipe=bytearray(changed_swipe);changed_swipe[0xF42]=1
    creatures.replace_records([bytes(changed_swipe)]+creatures.records[1:])
    assert any(x['code']=='zero_swipe_speed' and x['record_index']==0
               for x in fmt.compatibility_findings(dict(sections=[fmt.decoded(creatures)])))
    changed_mass=fmt.set_engine_scalar(creatures.records[0],'MassRaw',0)
    creatures.replace_records([changed_mass]+creatures.records[1:])
    assert any(x['code']=='creature_mass_class_outside_table' and x['record_index']==0
               for x in fmt.compatibility_findings(dict(sections=[fmt.decoded(creatures)])))
    creatures.replace_records([])
    assert fmt.parse(creatures.pack())[0].records==[]
    assert any(x['code']=='zero_creature_count' for x in fmt.compatibility_findings(dict(sections=[fmt.decoded(creatures)])))
    for sig,family in fmt.FAMILIES.items():
        catalogue=fmt.parse((DATA/(family+'.kwd')).read_bytes())[0]
        catalogue.replace_records([])
        assert fmt.parse(catalogue.pack())[0].records==[]
        assert any(x['code'] in ('zero_creature_count','zero_catalogue_count') for x in
                   fmt.compatibility_findings(dict(sections=[fmt.decoded(catalogue)]))),family
    old_checks=json.loads((ROOT/'validation.json').read_text(encoding='utf-8'))
    previous={x['file']:x['sha256'] for x in old_checks['files']};current={x['file']:x['sha256'] for x in checks}
    assert all(current.get(name)==digest for name,digest in previous.items()),'Input files changed since prior validation'
    assert set(current)-set(previous)<={'kop4Globals.kld'},'Unexpected change to the verification dataset'
    # Unmapped flag bits survive decoding and assembly.
    flag=fmt.enum_value('Creature.CreatureFlag2',0x80000001,True)
    assert flag['unmapped_bits']=='0x80000000'
    # Corrupted envelopes and record lengths are rejected.
    bad_size=bytearray((DATA/'Objects.kwd').read_bytes())
    struct.pack_into('<I',bad_size,8,len(bad_size)+1)
    for invalid in [b'',bytes(19),(DATA/'Objects.kwd').read_bytes()[:-1],bytes(bad_size),
                    (DATA/'AutoSave.kwd').read_bytes()[:25742]]:
        try:fmt.parse(invalid)
        except (ValueError,struct.error):pass
        else:raise AssertionError('Malformed input accepted')
    bindings,comparison=original_bindings();field_docs();book=workbook_compare()
    save('validation.json',dict(files=checks,total_files=len(checks),total_sections=sections_seen,
         typed_patch_fields=['Hp','Speed','Unk3cc'],catalogue_append_remove_in_memory=True,
         things_append_remove_in_memory=True,unmapped_flag_bits_preserved=True,malformed_input_rejected=True,
         engine_unsigned_angle_boundaries=[0,2048,32768,65535],independent_flag_slots_preserved=True,
         level_message_layout_and_creature_list=True,inputs_unchanged_since_prior_validation=True,
         footstep_pair_patch_slots=[0,47],format_valid_game_crash_combinations_detected=True,
         mass_table_index_diagnostic=True,zero_swipe_speed_diagnostic=True,
         zero_count_catalogues_diagnosed=list(fmt.FAMILIES.values()),game_walk_animation_slot=1,
         creature_spell_array_size=[3,24],effect_narrowed_raw_preservation=True,
         cube_generation_flag_combinations=[0,128,64,192],effect_generation_bounds=[0,18,19,255],
         generate_id_holes_and_duplicates_preserved=True,spray_secondary_effect_reference=True,
         gold_and_mana_ten_digit_series=True,spell_timer16_vs32_arithmetic=True,
         spell_selector_bounds_and_unknown_bytes_preserved=True,spell_projection_requires_explicit_tps=True))
    save('summary.json',summaries)
    print('EXACT JSON ROUNDTRIP:',len(checks),'files,',sections_seen,'sections')
    print('Schemas:',len(fmt.LAYOUTS),'variants,',sum(len(x['fields']) for x in fmt.LAYOUTS.values()),'top-level fields')
    print('Legacy bindings:',len(bindings),'schema rows without legacy match:',sum(not x['legacy_names'] for x in comparison))
    print('Workbook:',[(k,v['matched'],v['total']) for k,v in book.items()])

def u32check(b,o):return struct.unpack_from('<I',b,o)[0]

if __name__=='__main__':main()
