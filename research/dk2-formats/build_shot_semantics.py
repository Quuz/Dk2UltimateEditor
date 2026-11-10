"""Attach auditable meanings to both independent shot dispatch systems."""
from pathlib import Path
import json,re,hashlib

ROOT=Path(__file__).resolve().parent
def compact(name):
    s=(ROOT/'ghidra'/name).read_text(encoding='utf-8').replace('\r','')
    return '\n'.join(l for l in s.splitlines() if l.strip())

def main():
    rows=json.loads((ROOT/'shot-process-dispatch.json').read_text())
    # Meanings refer to instance fields, after optional spell/trap overrides.
    notes={
      1:('Creature ID (low u8)','Number to create (signed i32, positive loop bound)','Special handling for creature ID 9; created in a ring.'),
      2:('Object ID (low u8)','Gold amount when created object ID is 1','Special handling for object ID 10.'),
      3:('Signed health delta (positive heals, capped to missing health)','No read in this process callback','Shot ID 43 has a hard-coded 5%-health path; normal damage is a separate dispatcher.'),
      4:('Speed multiplier in percent (100 = same speed)','Duration seconds, low 16 bits then multiplied by TPS','0x48CAD0 applies speed bounds and a 16-bit timer.'),
      5:('Armour timer raw ticks (low 16 bits)','No read in this process callback','Writes creature+0x443 directly; this is not an armour percentage.'),
      6:('Mana amount transferred between target owner and caster','Health delta for target creature','Health restoration is capped to missing health.'),
      7:('Unsigned selector: >1 gains one level-worth of XP, 0/1 loses it','No read in this process callback','Not an arbitrary signed number of levels.'),
      8:('No read in this process callback','No read in this process callback','Uses HungerFill from creature data/level tables, multiplied by 5.'),
      9:('Damage magnitude, abs(Data1), doubled on checked terrain flag','No read in this process callback','Runtime extra flag 1 can prevent lightning damage.'),
      10:('Duration seconds; low 16 bits multiplied by TPS into invulnerability timer','No read in this process callback',''),
      11:('Duration seconds multiplied by TPS; helper writes freeze timer','No read in this process callback',''),
      12:('No read in this process callback','No read in this process callback','Signals alarm using current shot position and owner.'),
      13:('Duration seconds; low 16 bits multiplied by TPS into turncoat timer','No read in this process callback','Also stores shot owner as the temporary allegiance player.'),
      14:('Duration seconds; low 16 bits multiplied by TPS into conceal timer','No read in this process callback',''),
      15:('Duration seconds; low 16 bits multiplied by TPS into chicken timer','No read in this process callback','Primary flag 0x20000 blocks this process.'),
      16:('Duration seconds; low 16 bits multiplied by TPS into scapegoat timer','No read in this process callback',''),
      17:('Integer radius in map cells','Percentage used in distance-dependent damage','Radius converted to 4096 coordinate units inside handler.'),
      18:('Creature ID (low u8) to create at dead-body positions','No read in this process callback','Deletes processed dead bodies.'),
      19:('Duration seconds multiplied by TPS into obedient timer','No read in this process callback',''),
      20:('Duration seconds; low 16 bits multiplied by TPS into invisible timer','No read in this process callback',''),
      21:('No read in callback','No read in callback','FEAR callback at 0x484940 is a no-op in this image; other damage logic may still run.'),
      22:('No read in this process callback','No read in this process callback','Creates fixed object ID 0x4A; health/speed use global variables.'),
      23:('Speed multiplier in percent','Duration seconds; low 16 bits multiplied by TPS','Confirmed from assembly, including setter RET 4 and helper RET 8. Decompiled indirect-call stack cleanup was wrong.'),
      24:('No read in this process callback','No read in this process callback','Changes target owner to shot owner under checked conditions.'),
      25:('Nonzero enables initial event; low u16 packed in event payload','No read in this process callback','Scope/radius interpretation from names alone is not asserted.'),
      26:('No read in this process callback','No read in this process callback','Possession uses target and player state.'),
      27:('Low 16 bits multiplied by TPS for tremor duration','Low 16 bits shifted left 12, then cast to u16; helper caps at 0x5000','This is not an unrestricted 32-bit magnitude.'),
      28:('No read in this process callback','No read in this process callback','Guidance uses shot Speed, target position, angle and elapsed ticks.'),
      29:('No read in this process callback','No read in this process callback','Uses fixed 0xFF argument to target state routine.'),
      30:('Damage, negated for health modification','Integer radius in map cells','Damages targets within the checked radius, with special first-tick radius.'),
      31:('No read in this process callback','Integer area extent in cells','Normal DamageType may independently read Data1; do not mark it globally unused.'),
      32:('No read in this process callback','No read in this process callback','Signals using definition GeneralEffectId; exact triggered system name not inferred.'),
      33:('Optional reaper lifetime seconds; nonzero changes level selection path','No read in this process callback','Creature type and alternate starting level obtained from world/global state.'),
      34:('No read in this process callback','No read in this process callback','Moves shot to target position and forces collision.'),
      35:('Percentage of caster maximum health to drain, capped to target current health','No read in this process callback','Percentage basis is caster maximum health, not target maximum health.'),
      36:('Creature ID (low u8)','Starting level: creates level 1 then repeats level-up while level < Data2','Special handling for creature ID 9; low/negative values do not produce level zero.'),
      37:('Duration seconds, low 16 bits passed to speed modifier','Integer radius in map cells','Fixed speed multiplier -90; helper multiplies duration by TPS and applies speed bounds.'),
      38:('Poison severity (low u16)','Poison timer (low u16), stored without multiplication by TPS','0x48CC50 stores target+0x45D=Data2 and +0x45F=Data1; timer names from Flame map.'),
      39:('Damage, negated in checked branch','Entity tag ID (low u16), used to index live entities','Confirmed in assembly: damages referenced entity, creates creature ID 0x2E (Jack) and records prior owner; decompiler lost local stack variables.')}
    sight_path=ROOT/'sight-event-semantics.json'
    sight_reference=None
    if sight_path.exists():
        sight_reference=dict(file=sight_path.name,sha256=hashlib.sha256(sight_path.read_bytes()).hexdigest())
        notes[25]=('Nonzero DWORD enables initial event; low u16 is cell-coordinate radius capped at7 by Bridge0D',
                   'No read in this process callback',
                   'Phase77 executes process25→World→Session→Bridge0D→region setup. Data1=65536 enables radius0 event,unlike literal0. Final visible mask/renderer and KeeperC8 purpose remain unproved;see SIGHT_OF_EVIL_RU.md.')
    for row in rows:
        i=row['process_type'];s=compact(f'game-shot-process-{i:02d}.txt')
        if s.startswith('{"error"'):raise ValueError((i,s))
        row.update(data1=notes[i][0],data2=notes[i][1],note=notes[i][2],
            evidence=f'ghidra/game-shot-process-{i:02d}.txt',
            direct_instance_reads=dict(data1=bool(re.search(r'0x13d|\+ 317\b',s)),data2=bool(re.search(r'0x141|\+ 321\b',s))))
        if i==25 and sight_reference is not None:row['native_event_consumer']=sight_reference
    rows.insert(0,dict(process_type=0,name='NONE',handler=None,data1='No process callback; inspect DamageType',data2='No process callback; inspect DamageType',note='NONE does not disable ordinary projectile damage.'))
    damage={
      '0':dict(name='NONE',data1='No normal damage dispatcher',data2='No normal damage dispatcher',handler=None),
      '1':dict(name='CONSTANT',data1='Damage per call',data2='Integer area extent in cells',handler='0x4AC5A0',trigger='Every tick'),
      '2':dict(name='RANGE_FROM_ORIGIN',data1='Damage modified by distance from origin',data2='Integer radius in cells',handler='0x4AC6D0',trigger='Every tick'),
      '3':dict(name='TIME',data1='Damage * currentHealth / initialHealth',data2='Definition Data2 >> 12, not instance Data2',handler='0x4AC940',trigger='Every tick'),
      '4':dict(name='RING_CONSTANT',data1='Damage per call',data2='(initialHealth-currentHealth) * (instanceData2 >> 12) / initialHealth',handler='0x4ACAF0',trigger='Every tick'),
      '5':dict(name='AT_START',data1='Damage per call',data2='Integer area extent in cells',handler='0x4AC5A0',trigger='Once on first tick (0x4AB180)'),
      '6':dict(name='AT_END',data1='Damage per call',data2='Integer area extent in cells',handler='0x4AC5A0',trigger='End routine 0x4AAF70 under checked world condition'),
      '7':dict(name='HIT_THING',data1='Positive damage; nonpositive ignored; sniper doubles it',data2='Not read in hit routine',handler='0x4AC420',trigger='Collision 0x4ABD60'),
      '8':dict(name='CONSTANT_FOR_TREMOR',data1='Damage applied to map terrain',data2='Instance Data2 >> 12, area extent',handler='0x4ACCA0',trigger='Every tick')}
    result=dict(program='DKII-DX.exe',sha256='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a',
      file_offsets=dict(Data1=0x98,Data2=0x9c,ProcessFlags=0xa0,DamageType=0xc5,ProcessType=0xc7),
      instance_offsets=dict(Data1=0x13d,Data2=0x141),processes=rows,damage_types=damage,
      note='Both dispatchers can read the same parameters. Callback-only non-use is not global non-use. Names/semantics tied to this game image; original editor presets are separate Shot IDs.')
    if (ROOT/'native-shot-success-semantics.json').exists():result['phase80_native_success_reference']=dict(file='native-shot-success-semantics.json',sha256=hashlib.sha256((ROOT/'native-shot-success-semantics.json').read_bytes()).hexdigest())
    (ROOT/'shot-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    lines=['# Shots: Data1/Data2 и реальные обработчики','',
      'В файле Data1 и Data2 — DWORD по +0x98/+0x9C. Конвертер 0x56E690 переносит их в промежуточные +0x120/+0x124; загрузчик 0x4ACDF0 — в определение +0x37/+0x3B; создание 0x4AAC90 — в экземпляр CShot+0x13D/+0x141. Заклинания/ловушки могут переопределять параметры экземпляра.',
      'Две независимые системы: ProcessType по +0xC7 и DamageType по +0xC5. ProcessFlags по +0xA0 выбирают события для специального процесса. Нулевой ProcessType **не отключает обычный урон**. Не строить UI только по одному enum.',
      'Имя снаряда и его Shot ID не являются ProcessType. 65 отдельных Shot ID в исходном каталоге используют 0..39 процессов; соседний AutoSaveGlobals содержит 103 Shot ID. Историческое меню из 66 позиций не является полным списком обработчиков или лимитом файла.',
      'Таблица всех адресов процессов извлечена из 0x4AD1B0, все 39 callback декомпилированы. В строках «нет чтения» речь только об этом callback; Data1/Data2 могут использоваться системой обычного урона.', '',
      '| ProcessType | Имя | Data1 | Data2 | Callback |','|---:|---|---|---|---|']
    for r in rows:lines.append(f"| {r['process_type']} | {r['name']} | {r['data1']} | {r['data2']} | {r.get('handler') or '—'} |")
    lines+=['','## Обычный урон','','| DamageType | Имя | Data1 | Data2 | Вызов |','|---:|---|---|---|---|']
    for i,r in damage.items():lines.append(f"| {i} | {r['name']} | {r['data1']} | {r['data2']} | {r.get('trigger','—')} |")
    lines+=['','## Практические отличия и ограничения','',
      '- Для DamageType 1/2/5/6 область берётся из целого Data2. Для 3/4/8 код использует сдвиг >>12. У type 3 берётся значение определения, а не переопределённое значение экземпляра.',
      '- ModifyArmour записывает Data1 в таймер напрямую. Многие другие статусы умножают Data1 на TPS; применённые таймеры 16-битные, большие значения обрезаются.',
      '- PoisonSpit записывает Data2 в poisonedTimer и Data1 в poisonedSeverity без умножения на TPS в этом callback/helper. Это отличается от Freeze/Invulnerable.',
      '- Threat по +0xE9 и BurnDuration по +0xEB читаются конвертером в промежуточную структуру, но **не переносятся** в MyShotDataObj загрузчиком 0x4ACDF0 и не используются 0x521810 кроме общего учёта/контрольной суммы. Эффект этих полей в игровом пути данного образа не подтверждён; не обещать, что их изменение меняет угрозу/горение.',
      '- TimedEffectDelay — byte по +0xC0; в 0x4AB180 используется как интервал **тиков**, через age % delay. При ненулевом TimedEffectId и нулевом delay путь содержит деление на ноль. Значения по умолчанию с TimedEffectId=0 этому условию не соответствуют.',
      '- ProcessType 21 FEAR в данном образе указывает на функцию, которая сразу возвращает 0. Это не доказательство отсутствия любой системы страха в игре.',
      '- Для процессов 23 и 39 дополнительно проверены инструкции. У Alert Data1 — процент скорости, Data2 — длительность, умножаемая на TPS. У JackInTheBox Data2 — ID живой сущности, а не ID определения или радиус. Декомпилятор ошибался из-за неизвестных сигнатур косвенных вызовов; сохранены соответствующие assembly-файлы.',
      '', 'Полный машинный результат и ссылки на сохранённые декомпиляции — shot-semantics.json. Смысл каждого callback рассмотрен вместе с DamageType; одинаковые DWORD не означают одинаковые единицы.', '']
    if sight_reference is not None:lines[2:2]=['Продолжение этап77 — `SIGHT_OF_EVIL_RU.md`:полная native-цепочка Process25→Bridge0D уточнила Data1 как полуразмер области в клеточных координатах,low16 с ограничением7. Полный DWORD управляет отправкой события;0 и65536 неэквивалентны. KeeperC8 остаётся неизвестным.','']
    (ROOT/'SHOT_PARAMETERS.md').write_text('\n'.join(lines),encoding='utf-8')
    print('Built semantics for',len(rows)-1,'process handlers and',len(damage),'damage types')

if __name__=='__main__':main()
