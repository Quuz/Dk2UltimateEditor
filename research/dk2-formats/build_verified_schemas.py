"""Build supplementary schemas; keep OpenKeeper's baseline source trace intact."""
from pathlib import Path
import json
import re

ROOT=Path(__file__).resolve().parent
def save(name,value):
    (ROOT/name).write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')

def level():
    fields=[];offset=0
    def add(name,kind,size,source='ufdada level_struct.bt; game 0x56C100'):
        nonlocal offset
        fields.append(dict(name=name,kind=kind,size=size,offset=offset,offset_hex=hex(offset),source=source))
        offset+=size
    for name,count in [('Name',64),('Description',1024),('Author',64),('Email',64),('Information',1024)]:add(name,'utf16le',count*2)
    add('TriggerId','u16',2);add('TicksPerSecond','u16',2)
    add('UnknownHistory','bytes',520,'Raw preserved; purpose unresolved')
    for i in range(20):add(f'Messages[{i}]','utf16le',1024)
    assert offset==0x638c
    add('Flags','flags16',2);fields[-1]['enum']='GameLevel.LevFlag'
    add('SoundCategory','string',32);add('TalismanPieces','u8',1)
    for group in ['RewardPrevious','RewardNext']:
        for i in range(4):add(f'{group}[{i}]','enum8',1);fields[-1]['enum']='GameLevel.LevelReward'
    add('SoundTrack','u8',1);add('TextTableId','u8',1)
    for name in ['Title','Plot','Debrief','Objective']:add('Text'+name+'Id','u16',2)
    add('Unknown63C1','bytes',2,'Raw preserved; purpose unresolved')
    for i in range(1,4):add(f'TextSubObjectiveId{i}','u16',2)
    add('SpecialLevelNumber','u8',1);add('UniqueCreatureTypeId','u8',1)
    for i in range(8):add(f'IntroductionCreatureId[{i}]','u8',1)
    for i in range(8):add(f'IntroductionTextId[{i}]','u16',2)
    add('TerrainPath','string',32)
    assert offset==25603
    old=list(fields)
    add('OneShotHornyLevel','u8',1);add('PlayerCount','u8',1)
    for name in ['Previous','Next']:add('Reward'+name+'Extra','enum8',1);fields[-1]['enum']='GameLevel.LevelReward'
    for name in ['Horny','Prelevel','PostlevelWin','PostlevelLost','PostlevelNews','PrelevelGeneral']:add('Speech'+name+'Id','u16',2)
    add('HeroName','utf16le',64)
    assert offset==25683
    layouts={'Level_25603':dict(record_size=25603,fields=old),'Level_25683':dict(record_size=offset,fields=fields)}
    save('level-layouts.json',layouts)
    lines=['# Параметры уровня AutoSave.kwd','',
      'Таблица описывает блок `0xDF`, смещения от начала его полезной части (в этом AutoSave — файл+0x38). Длина 25683 байта. Старый вариант 25603 заканчивается перед OneShotHornyLevel. Источники: закреплённый level_struct.bt и игровой конвертер 0x56C100.',
      'Игровой конвертер подтверждает **20 сообщений по 512 UTF-16 символов**, а не 512 сообщений по 20 символов в OpenKeeper. Поле SpecialLevelNumber — u8; следующий u8 является отдельным полем. Нулевые терминаторы и байты после них сохраняются в raw.',
      'Названия смыслов преимущественно из шаблона; чтение смещения и размера движком не доказывает всех игровых последствий. UnknownHistory и Unknown63C1 остаются нерасшифрованными.',
      '', '| Смещение DF | Смещение файла | Байт | Тип | Поле |','|---|---|---:|---|---|']
    lines += [f"| `{f['offset_hex']}` | `{f['offset']+56:#x}` | {f['size']} | {f['kind']} | {f['name']} |" for f in fields]
    lines += ['', 'После DF: `u32 0xDE`, `u32 referenceBlockBytes`, N ссылок по 72 байта, K DWORD списка существ. N и K — два u16 по +0x14 и +0x16 заголовка. Игра 0x56C4E0 читает DWORD каждого элемента списка, затем передаёт **младший байт как creature ID**. Сохранять все 4 байта; это не случайная дополнительная область.',
      'Ссылка: u32 тип каталога, u32 служебное поле, char path[64]. Длина блока = 72*N+4*K для этого файла. Порядок ссылок, неизвестное служебное поле и старшие байты creature ID сохранять.', '']
    (ROOT/'LEVEL_LAYOUT.md').write_text('\n'.join(lines),encoding='utf-8')

def engine():
    baseline=json.loads((ROOT/'layouts.json').read_text())['Creatures']['fields']
    fields=[]
    def add(name,offset,size,kind,confidence,evidence,note=''):
        old=[f['name'] for f in baseline if f['offset']<=offset<f['offset']+f['size']]
        fields.append(dict(name=name,offset=offset,offset_hex=hex(offset),size=size,kind=kind,
            baseline_names=old,confidence=confidence,evidence=evidence,note=note))
    add('MaxAngleChangeRaw',0xf20,2,'u16','engine units and unsigned read',
        ['game-creature-convert.txt','game-creature-type.txt','game-turning-apply.txt','game-turning-tick.txt'],
        '2048 angular units per full turn. Per-tick limit = floor(raw / simulationTPS); positive raw clamped to at least 1. Degrees per second before quantization = raw*360/2048. Not signed /4096.')
    add('GameExtraFlagsRaw',0x1545,4,'u32','engine read confirmed',
        ['game-creature-convert.txt','game-creature-type.txt'],
        'Runtime +0x68A. Game converter reads this slot for both measured strides. Do not auto-copy the editor slot.')
    add('EditorExtraFlagsRaw',0x1599,4,'u32','editor read confirmed',
        ['creature-record-decoder.txt'],'Only present in 5537-byte variant. Separate from GameExtraFlagsRaw.')
    # Names below are template interpretations; the converter confirms storage only.
    for name,offset,size in [('WidthRaw',0xea0,4),('MassRaw',0xea8,4),('AirFrictionRaw',0xeac,4),
          ('ScoreRatingRaw',0xf4c,1),('RoomCapacityNeededRaw',0xf4d,1),('DisarmFailurePercentageRaw',0xf51,1),
          ('SatisfyAngerTreatRaw',0xfcb,2),('ScaleRaw',0xfcd,4),('SniperZoomMultiplierRaw',0x107b,4),
          ('AbilityCreepPercentageRaw',0xee4,4),('SwipeSpeedRaw',0xcec,2),
          ('WorkModifierDefaultRaw',0xcee,4),('WorkModifierLevelIncreaseRaw',0xcf2,4)]:
        add(name,offset,size,'u'+str(size*8),'storage read confirmed; semantic name from template',
            ['game-creature-convert.txt','game-creature-type.txt','../sources/creatures_struct.bt'],
            'Raw integer view. Signedness, displayed scale and full gameplay semantics not inferred from memcpy.')
    evidence_updates={
      'WidthRaw':('consumer confirms half-width', ['game-creature-radius-getter.txt','game-creature-width-consumer.txt','game-creature-width-use.txt'],
        'Raw fixed-point width; loader stores low16 in runtime+0x63A. Consumers use width>>1; 0x48F690 adds clearance 0x80. Not an independently stored radius.'),
      'MassRaw':('collision consumer and table confirmed', ['game-creature-mass-collision.txt','game-creature-mass-ratio.txt','game-creature-mass-ratio-assembly.txt','game-creature-mass-collision-assembly.txt'],
        'Loader stores low16 in runtime+0x624. Collision helper uses (mass>>12) as a 1..5 class in a 5x5 lookup; fractional bits do not affect that lookup. No bounds clamp in helper.'),
      'AirFrictionRaw':('storage and instance initialization confirmed', ['game-creature-instance-init.txt'],
        'Low16 copied into runtime+0x690 and instance+0x11E at initialization; full physical interpretation not proven.'),
      'AbilityCreepPercentageRaw':('runtime consumer confirmed; source names qualified', ['game-creature-possession-sneak1.txt','game-creature-possession-sneak2.txt'],
        'Runtime+0x760 used under instance flag0x1000; level-scaled by stat26, then multiplies a squared-distance quantity /100 in both inspected consumers. Do not present as ordinary movement speed.'),
      'WorkModifierDefaultRaw':('normal constructor bypass confirmed', ['game-creature-instance-init.txt'],
        'Copied to type+0x793, but normal instance constructor sets work modifier+0x3AF to literal0xCCC (~0.8), independently of this field. No universal unused claim.'),
      'WorkModifierLevelIncreaseRaw':('level-up consumer confirmed', ['game-creature-work-levelup.txt','game-creature-level-up.txt'],
        'Type+0x797 getter 0x48B330 feeds addition to instance+0x3AF in 0x48B120. Current two catalogues contain zero for all46 creatures.'),
      'ScoreRatingRaw':('numeric score consumers confirmed', ['game-creature-score-a.txt','game-creature-score-b.txt'],
        'Runtime+0x787 u8. 0x4BCB20 sums values over a creature list; 0x4FA8D0 compares candidates to select a minimum. Full AI strategy semantics are not inferred.'),
      'RoomCapacityNeededRaw':('room capacity consumer confirmed', ['game-creature-room-capacity.txt'],
        'Runtime+0x672 u8. Room routine 0x4E69B0 subtracts this amount for processed occupants; zero skips that subtraction path.'),
      'DisarmFailurePercentageRaw':('failure threshold and RNG bound confirmed', ['game-creature-disarm.txt','game-random-bound.txt'],
        'Runtime+0x5E0 u8, stat20 growth. Disarm routine compares random(100), range0..99, against level-scaled threshold; grown threshold clamped0..254. Values>=100 always enter the compared branch.'),
      'SatisfyAngerTreatRaw':('unsigned consumer and gift IDs confirmed', ['game-creature-treat-anger.txt','game-emotionless-event-a.txt'],
        'Runtime+0x6E1 read as u16; gold IDs1..3 / Chicken ID9 gift branches call mood handler category5 with negative of this value. Emotionless bit4 suppresses the inspected handler.'),
      'SwipeSpeedRaw':('UI use and divide constraint confirmed', ['game-creature-swipe-ui.txt','game-swipe-speed-use.txt','game-swipe-speed-use-assembly.txt'],
        'Runtime+0x791 u16 passed with swipe ID to0x5075A0. Nonzero swipe computes22000/speed; IDIV0 at0x507615 when speed=0. Both current catalogues store75 for all46 creatures.'),
      'SniperZoomMultiplierRaw':('quantized multiplier consumer confirmed', ['game-creature-sniper-use.txt'],
        'Runtime+0x79B u32. 0x40FDF0 uses raw>>12, returns when result<2, and multiplies an ability range parameter by that integer in one toggle branch. Fractions are discarded; this is not proof of continuous camera zoom.')}
    for f in fields:
        if f['name'] in evidence_updates:
            confidence,evidence,note=evidence_updates[f['name']]
            f.update(confidence=confidence,evidence=f['evidence']+evidence,note=note)
    save('verified-overrides.json',dict(program='DKII-DX.exe',
      sha256='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a',
      creature_fields=fields,creature_arrays=[dict(name='FootstepFrames',offset=0x1323,size=192,count=48,
        element_size=4,element_fields=[dict(name='LeftFrame',offset=0,size=2,kind='u16'),dict(name='RightFrame',offset=2,size=2,kind='u16')],
        evidence=['game-creature-convert.txt','game-creature-type.txt','game-creature-footstep-tick.txt','game-creature-footstep-access.txt'],
        note='Runtime data +0x7A3: pairs per animation slot. Instance phases = floor(frame*4096 / loadedAnimationFrameCount).')],
      extra_flags=dict(game_offset=0x1545,editor_offset=0x1599,
        editor_writer_known_mask=15,editor_writer_evidence=['editor-creature-extra-flags-save-assembly.txt','editor-creature-writer-assembly.txt'],
        observed_runtime_bits={'1':'checked in lightning damage path 0x4AF1E0',
          '4':'skips call in 0x478CE0; emotionless name from template',
          '8':'hero portal room 0x19 eligibility in 0x4DBF90'},
        bit_2='Stone Knight name from sources; gameplay effect not independently verified')))
    # Trace raw -> intermediate scalar copies and match stat setter runtime slots.
    s=(ROOT/'ghidra/game-creature-convert.txt').read_text().replace('\r','')
    intermediate={}
    for m in re.finditer(r'(\w+) = \*\((\w+) \*\)\(param_2 \+ (0x[0-9a-f]+)\);',s):
        chunk=s[m.end():m.end()+450]
        a=re.search(r'(param_1\[(0x[0-9a-f]+|\d+)\]|\*\([^\n]+?\)\(([^\n]+?)\)) = '+m[1]+r';',chunk)
        if not a:continue
        if a[2]:dst=int(a[2],0)*4
        else:
            off=re.search(r'\+ (0x[0-9a-f]+|\d+)',a[3])
            if not off:continue
            dst=int(off[1],0)*(1 if '(int)param_1' in a[3] else 4)
        intermediate[dst]=int(m[3],0)
    # runtime slot -> loader's intermediate slot, manually audited in 0x492320.
    slots={0:(0x648,0x1294),1:(0x692,0x12fc),2:(0x696,0x1300),3:(0x698,0x1302),
      4:(0x5c4,0x1304),5:(0x69a,0x130c),6:(0x69c,0x130e),7:(0x69e,0x1324),
      9:(0x66d,0x133e),10:(0x6a2,0x1308),12:(0x787,0x133c),13:(0x6b5,0x12f0),
      15:(0x6b7,0x12f4),16:(0x6b9,0x12f6),17:(0x6bb,0x12f8),18:(0x6bd,0x12fa),
      19:(0x6bf,0x1312),20:(0x5e0,0x1341),21:(0x5b4,0x12a4),22:(0x5b8,0x12a8),
      23:(0x68e,0x12e0),24:(0x76c,0x12e4),25:(0x75c,0x12d0),26:(0x760,0x12d4),
      27:(0x764,0x12d8),28:(0x6a6,0x12da),29:(0x626,0x13bd),30:(0x77f,0x12bc),31:(0x5c6,0x12e8)}
    intermediate.update({0x133e:0xf4e,0x133c:0xf4c,0x1341:0xf51})
    enums=json.loads((ROOT/'enums.json').read_text())['Variable.CreatureStats.StatType']
    rows=[]
    for stat in range(32):
        row=dict(stat_id=stat,baseline_name=enums[str(stat)])
        if stat in slots:
            runtime,mid=slots[stat];raw=intermediate[mid]
            field=next(f for f in baseline if f['offset']==raw)
            row.update(runtime_offset=hex(runtime),intermediate_offset=hex(mid),creature_record_offset=hex(raw),
                       baseline_field=field['name'],baseline_kind=field['kind'],
                       note='Variable 77 assigns runtime value; do not assume raw-file scaling. See setter decompilation.')
        else:row['note']='No case in variable-77 setter in this build; not proof of unused growth-table slot.'
        rows.append(row)
    save('creature-stat-runtime-map.json',rows)
    lines=['# Stat ID → поле существа в движке','',
      'Связаны загрузчик 0x568E30, преобразование 0x492320 и setter 0x494A70, вызываемый Variables type 77. Названия OpenKeeper оставлены для сопоставления. Для 8, 11, 14 setter не делает присваивания. Это не означает, что таблицы типов 65/224 не имеют этих слотов.',
      'Параметры type 77: creatureID, statID (setter принимает u8), runtimeValue. Значение пишется **после** загрузки и преобразований; единицы могут отличаться от сырых полей Creatures. В частности 21/22/23 делятся на TPS, 24 обрезается до u8, 29 записывается как u16 масштаба. Полный switch сохранён в ghidra/game-creature-stat-set.txt.',
      '', '| Stat ID | Имя OpenKeeper | Offset Creatures | Поле базовой схемы | Offset runtime |','|---:|---|---|---|---|']
    lines += [f"| {r['stat_id']} | {r['baseline_name']} | {r.get('creature_record_offset','—')} | {r.get('baseline_field','нет ветки setter')} | {r.get('runtime_offset','—')} |" for r in rows]
    lines += ['', 'Сопоставление уточняет Unknown: stat12 попадает в ScoreRating (0xF4C), stat20 — DisarmFailurePercentage (0xF51), stat23 — поле 0xEF0, stat25 — поле 0xEE0, stat26 — AbilityCreepPercentage (0xEE4), stat29 — Scale (0xFCD). Имена дополнительных шаблонов не доказывают единицы или все последствия каждого поля.', '']
    (ROOT/'CREATURE_STATS.md').write_text('\n'.join(lines),encoding='utf-8')

def variables():
    schemas={}
    def add(tag,name,fields,evidence):
        descriptors=[dict(name='Type',offset=0,offset_hex='0x0',kind='u32',size=4)]
        for field,offset,kind,size in fields:
            descriptors.append(dict(name=field,offset=offset,offset_hex=hex(offset),kind=kind,size=size))
        assert sum(f['size'] for f in descriptors)==16
        schemas[str(tag)]=dict(name=name,record_size=16,fields=descriptors,evidence=evidence)
    add(1,'CreaturePool',[('CreatureId',4,'i32',4),('Count',8,'i32',4),('PlayerId',12,'i32',4)],'0x51C9C0; player 0 iterates eligible players, not neutral player only')
    add(2,'Availability',[('AvailabilityType',4,'u16',2),('PlayerId',6,'u16',2),('DefinitionId',8,'i32',4),('Value',12,'i32',4)],'0x51CA60 -> 0x51CAE0; player 0 iterates player list')
    add(17,'ResearchOrder',[('ResearchCategory',4,'u32',4),('DefinitionId',8,'u32',4),('ResearchPointsRaw',12,'u32',4)],'0x51DB00 -> 0x506FF0; category names from template')
    f=[]
    for i in range(3):f.extend([(f'Input[{i}].Type',4+2*i,'u8',1),(f'Input[{i}].Id',5+2*i,'u8',1)])
    add(43,'Sacrifice',f+[('RewardType',10,'u8',1),('SpeechId',11,'u8',1),('RewardValue',12,'i32',4)],'0x51DB30; OpenKeeper')
    for tag in (61,62,63,64):
        add(tag,'NoOpInOriginal170',[('Ignored1',4,'u32',4),('Ignored2',8,'u32',4),('Ignored3',12,'u32',4)],'0x51DC20 -> 0x401200 (empty function in this image)')
    add(65,'CreatureLevelStat',[('StatId',4,'u32',4),('ValueRaw',8,'u32',4),('CreatureLevel',12,'u32',4)],'0x51DC40; low 16 bits of ValueRaw stored; observed levels 2..10')
    add(66,'Alliance',[('Player1',4,'u32',4),('Player2',8,'u32',4),('Unused',12,'u32',4)],'0x51DC90; player IDs truncated to u8; symmetric write')
    add(77,'ModifyCreatureStat',[('CreatureId',4,'u32',4),('StatId',8,'u32',4),('RuntimeValueRaw',12,'u32',4)],'0x51DD10 -> 0x494A70; stat ID u8; runtime units')
    add(224,'FirstPersonStat',[('StatId',4,'u32',4),('ValueRaw',8,'u32',4),('Unused',12,'u32',4)],'0x51DC70; third payload DWORD ignored; low 16 bits of value stored')
    add('generic','MiscOrUnknown',[('Value',4,'i32',4),('Extra1',8,'i32',4),('Extra2',12,'i32',4)],'Raw 4-DWORD container; signed view and names from OpenKeeper, semantics vary by Type')
    save('variable-layouts.json',schemas)
    lines=['# GlobalVariables: записи и варианты','',
      'Variables E6: общий заголовок 56 байт; записи всегда 16 байт, первый DWORD выбирает вариант. Повторения типов, порядок, дополнительные параметры и отдельную секцию камер сохранять.',
      'Скалярные типы 3..242 сопоставлены с именами в enums.json (Variable.MiscVariable.MiscType). Эти имена — источник OpenKeeper, не подтверждение всех единиц. Для неизвестного типа сохранять все 16 байт. Диспетчер исследованного движка находится в variable-dispatch.json; исходные указатели/метаданные не являются бинарными полями KWD.', '']
    for tag,schema in schemas.items():
        lines+=['## '+str(tag)+' '+schema['name'],'',schema['evidence'],'',
            '| Offset записи | Байт | Тип | Поле |','|---|---:|---|---|']
        lines += [f"| {f['offset_hex']} | {f['size']} | {f['kind']} | {f['name']} |" for f in schema['fields']]
        lines.append('')
    lines+=['Категории ResearchOrder: 1 Room, 2 Door, 3 Trap, 4 KeeperSpell. Категории Availability: 1 Room, 2 Creature, 3 Trap, 4 Door, 5 KeeperSpell. Не объединять enum.',
      'Статистика type 65 имеет измеренные уровни 2..10; третье слово type 224 в файле равно 10, но движок его не использует. Поэтому общего ключа (type, stat, level) для этих двух таблиц нет. Изменения type 77 проходят другой путь, чем процентные/уровневые таблицы: см. CREATURE_STATS.md.', '']
    (ROOT/'VARIABLES_LAYOUT.md').write_text('\n'.join(lines),encoding='utf-8')

if __name__=='__main__':level();engine();variables();print('Built supplementary level/engine/stat/variable schemas')
