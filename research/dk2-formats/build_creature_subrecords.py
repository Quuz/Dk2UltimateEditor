"""Describe fixed creature subrecords with explicit game animation slot numbers."""
from pathlib import Path
import json,hashlib

ROOT=Path(__file__).resolve().parent

def main():
    fields=json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))['Creatures']['fields']
    at={f['offset']:f for f in fields};slots=[]
    extra={37:0xFD1,38:0x12CF,39:0x13E3,40:0x1437,41:0x148B,
        42:0x1083,43:0x10D7,44:0x112B,45:0x117F,46:0x11D3,47:0x1227}
    for index in range(48):
        offset=0x20+84*index if index<37 else extra[index]
        f=at[offset]
        assert f['size']==84
        slots.append(dict(game_animation_slot=index,raw_resource_offset=offset,
            raw_resource_offset_hex=hex(offset),resource_size=84,baseline_field=f['name'],
            raw_footstep_pair_offset=0x1323+4*index,
            intermediate_resource_offset=(0x410+84*index if index<37 else
              {37:0x13C1,38:0x16BF,39:0x17D3,40:0x1827,41:0x187B,
               42:0x1473,43:0x14C7,44:0x151B,45:0x156F,46:0x15C3,47:0x1617}[index]),
            runtime_resource_offset=0xB+27*index,
            runtime_loader_has_slot_path=index!=0))
    assert slots[1]['baseline_field']=='Animation.WALK'
    assert slots[1]['raw_footstep_pair_offset']==0x1327
    spell_fields=[dict(name='ShotOffsetX',offset=0,size=4,kind='i32/4096'),
        dict(name='ShotOffsetY',offset=4,size=4,kind='i32/4096'),dict(name='ShotOffsetZ',offset=8,size=4,kind='i32/4096'),
        dict(name='Unknown0C',offset=12,size=1,kind='u8'),dict(name='PlayAnimation',offset=13,size=1,kind='u8'),
        dict(name='Unknown0E',offset=14,size=1,kind='u8'),dict(name='Unknown0F',offset=15,size=1,kind='u8'),
        dict(name='ShotDelayRaw',offset=16,size=4,kind='u32'),dict(name='Unknown14',offset=20,size=1,kind='u8'),
        dict(name='Unknown15',offset=21,size=1,kind='u8'),dict(name='CreatureSpellId',offset=22,size=1,kind='u8'),
        dict(name='LevelAvailable',offset=23,size=1,kind='u8')]
    result=dict(animation_slots=slots,
        animation_slot_note='Game slot1=WALK; slot0 is copied to intermediate but excluded from resource load. Do not use zero-based OpenKeeper animation enum ordinals as FootstepFrames indices.',
        spell_array=dict(raw_offset=0xDC4,count=3,element_size=24,fields=spell_fields,
            intermediate_offset=0x11B4,intermediate_stride=24,runtime_offset=0x5E1,runtime_stride=19,
            runtime_fields=[dict(name='CreatureSpellId',offset=0,size=1),dict(name='LevelAvailable',offset=1,size=1),
                dict(name='ShotDelayTicks',offset=2,size=4),dict(name='ShotOffsetXYZ',offset=6,size=12),
                dict(name='PlayAnimation',offset=18,size=1)],
            delay_transform='uint32(rawShotDelay * simulationTPS) >> 12 in the inspected loader; intermediate product wraps32 bits',
            unknown_bytes_read_by_converter=[12],unknown_bytes_not_read_by_converter=[14,15,20,21],
            unknown_bytes_copied_into_runtime_spell_entry=[],
            warning='Exactly3 physical slots in the measured variants. Increasing catalogue record count does not add a fourth creature spell slot. Keep unused IDs/unknown bytes and slot order.',
            getter_evidence=['game-creature-spell-get-present.txt','game-creature-spell-get-id.txt','game-creature-spell-get-level.txt',
                'game-creature-spell-get-delay.txt','game-creature-spell-get-offset.txt','game-creature-spell-get-animation.txt']),
        evidence=['game-creature-convert.txt','game-creature-type.txt','game-creature-footstep-access.txt','game-creature-footstep-tick.txt'],
        scope='Storage/transfer paths in the inspected executable; baseline animation names do not prove every state that requests the animation.')
    if (ROOT/'creature-shot-request-semantics.json').exists():result['phase79_cast_request']=json.loads((ROOT/'creature-shot-request-semantics.json').read_text(encoding='utf8'))
    if (ROOT/'native-shot-success-semantics.json').exists():result['phase80_native_success_reference']=dict(file='native-shot-success-semantics.json',sha256=hashlib.sha256((ROOT/'native-shot-success-semantics.json').read_bytes()).hexdigest())
    if (ROOT/'targeted-spell-semantics.json').exists():result['phase82_targeted_cast_reference']=dict(file='targeted-spell-semantics.json',sha256=hashlib.sha256((ROOT/'targeted-spell-semantics.json').read_bytes()).hexdigest())
    if (ROOT/'creature-job-overlap-semantics.json').exists():result['phase86_job_overlap']=json.loads((ROOT/'creature-job-overlap-semantics.json').read_text(encoding='utf8'))
    (ROOT/'creature-subrecords.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    lines=['# Вложенные записи Creatures: номера анимаций и заклинания','',result['animation_slot_note'],'',
        '## Анимации и кадры шагов','',
        'Номера ниже подтверждаются переносом ресурсов в 0x568E30 и загрузчиком 0x492320. Первый общий цикл копирует raw+0x20 как slot0, но runtime-цикл начинается с1. Данные FootstepFrames по raw+0x1323 индексируются тем же игровым номером. Ходьба использует пару по raw+0x1327, а не первую пару. Например, Imp имеет пару0=(0,0), пару1=(2,8).','',
        '| Игровой слот | Поле базовой схемы | Raw ресурс | Raw пара шагов | Runtime ресурс |','|---:|---|---|---|---|']
    for row in slots:lines.append(f"| {row['game_animation_slot']} | {row['baseline_field']} | {row['raw_resource_offset']:#x} | {row['raw_footstep_pair_offset']:#x} | {row['runtime_resource_offset']:#x} |")
    lines+=['','Слот0 не загружается как ресурс в просмотренном runtime loader. Это не основание удалять его84 байта: converter копирует их в intermediate. У первой записи отдельно сохранён raw; нельзя пытаться прочитать из неё имя и затем восстановить весь буфер только по этому имени.','',
        '## Три определения заклинаний внутри записи существа','',
        'Начало raw+0xDC4, ровно3 слота по24 байта. Это ссылки/настройки применения CreatureSpells, а не сами266-байтовые записи каталога CreatureSpells. Размер внешнего Creatures и число существ не меняют размер этого массива.','',
        '| Offset внутри24 байт | Байт | Поле |','|---|---:|---|']
    for f in spell_fields:lines.append(f"| {f['offset']:#x} | {f['size']} | {f['name']} |")
    lines+=['','Converter переносит XYZ, Unknown0C, PlayAnimation, ShotDelay, ID и уровень в промежуточные слоты по24 байта. Четыре байта по+0E/+0F/+14/+15 он не читает. Getter-цепочка0x569CA0..0x569D50 и loader используют только XYZ, PlayAnimation, ShotDelay, ID и уровень: Unknown0C не попадает в runtime-элемент.','',
        'Runtime-элемент имеет19 байт: ID u8; level u8; delayTicks u32; XYZ12 байт; PlayAnimation u8. Задержка преобразуется как uint32(raw×TPS)>>12. Не сохранять19-байтовый runtime-элемент вместо24-байтовой записи файла.','',
        'Никакие неизвестные байты не обнуляются. Имена/значения PlayAnimation сохраняются как u8: нормализация произвольного ненулевого байта в1 была бы отдельным изменением данных.','']
    if (ROOT/'creature-definition-semantics.json').exists():lines[2:2]=['Этап85 — `CREATURE_DEFINITION_RU.md`: полный конструктор/загрузчик2199 байт,8XYZ-троек,семь дополнительных presence-флагов и пересечение искусственногоmid308 сWORD6CD. Схема: `catalogue_loading.phase85_complete_definition`.','']
    if (ROOT/'creature-job-overlap-semantics.json').exists():lines[2:2]=['Этап86 — `CREATURE_HATE_JOBS_RU.md`: подтверждено пересечение HateJobs3/4 с альтернативной работой0 в игровых/редакторных intermediates ив writerPro. raw14E1/14E5 читаются,затем перезаписываются;альтернативная работа влияет на native job-membership. Схема: `creature_subrecords.phase86_job_overlap`.','']
    (ROOT/'CREATURE_SUBRECORDS.md').write_text('\n'.join(lines),encoding='utf8')
    print('Creature subrecords:48 game animation slots,3 fixed spell entries')

if __name__=='__main__':main()
