"""Publish the verified pre-allocation CreatureSpell request and raw preservation."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from native_creature_shot_request import patch_slot
from native_spell_writer import patch_raw
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    spec=load('phase79-primary-manifest.json');functions=[];blocks=[]
    for r in spec['functions']:
        before,after,memory=[ROOT/'ghidra'/r[k] for k in ['before','after','memory']];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert before.read_text().rstrip()==after.read_text().rstrip()
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        blocks.append({**r,'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    return functions,blocks
def rebuild(original,parts,si,ri,replacement,relative,n):
    s=parts[si];rr=list(s.records);rr[ri]=replacement;section=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);section.replace_records(rr)
    packed=b''.join(section.pack() if j==si else p.pack() for j,p in enumerate(parts));expected=bytearray(original);at=s.offset+len(s.header)+sum(map(len,s.records[:ri]))+relative;expected[at:at+n]=replacement[relative:relative+n];assert packed==expected
def corpus():
    files=[];counts={170:0,160:0};patches={170:0,160:0}
    for family,manifest in [(170,load('creature-lookup-review.json')['corpus']['files']),(160,load('spell-writer-semantics.json')['corpus']['files'])]:
      for row in manifest:
        p=Path(row['file']);original=p.read_bytes();assert digest(original)==row['sha256'];parts=fmt.parse(original);assert b''.join(s.pack() for s in parts)==original;local=0
        for si,s in enumerate(parts):
            if s.signature!=family:continue
            counts[family]+=len(s.records)
            if not s.records:continue
            if family==170:
                raw=s.records[0]
                for slot in range(3):
                  for field,offset,n,values in [('x',0,4,[0,0x80000000,0xFFFFFFFF]),('delay',16,4,[0,4096,0xFFFFFFFF]),('play_animation',13,1,[0,1,0x80,0xFF])]:
                    for value in values:
                        replacement=patch_slot(raw,slot,field,value);rebuild(original,parts,si,0,replacement,0xDC4+24*slot+offset,n);local+=1
            else:
                chosen={0}
                for pred in [lambda r:r[0xE6]==12,lambda r:bool(r[0xE8] or r[0xE9])]:
                    ri=next((i for i,r in enumerate(s.records) if pred(r)),None)
                    if ri is not None:chosen.add(ri)
                for ri in sorted(chosen):
                    raw=s.records[ri]
                    for value in [0,17,255]:rebuild(original,parts,si,ri,patch_raw(raw,0xE7,value),0xE7,1);local+=1
        files.append(dict(file=str(p),sha256=digest(original),family=fmt.FAMILIES[family],exact_patches=local));patches[family]+=local
    return dict(files=files,source_file_roles=len(files),unique_source_files=len({r['file'] for r in files}),creature_records=counts[170],creature_spell_records=counts[160],
        creature_slot_patches=patches[170],primary_shot_patches=patches[160],exact_container_patch_cases=sum(patches.values()),
        scope='Per Creature source: first physical record,3slots,x/delay/animation boundary values. Per CreatureSpell source:first record/ID12/first nonzero alternative. Whole containers preserve every byte outside selected field, including unknown slot bytes, alternatives, tails and physical order.')
def main():
    functions,blocks=primary();native=load('phase79-creature-shot-request-emulations.json');assert len(native['cases'])==52
    source=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/CreatureSpells.kwd'
    s=dict(phase=79,date='2026-10-28',families=['Creatures','CreatureSpells','Shots'],executable=functions[0]['exe'],executable_sha256=digest(Path(functions[0]['exe']).read_bytes()),
      primary_program_bytes=functions,primary_vslot_data_ranges=blocks,native_emulations=native['cases'],source_example=dict(file=str(source),sha256=digest(source.read_bytes()),creature_spell_id=12),corpus=corpus(),
      executed_scope='Two complete request wrappers4982B0/4984D0 execute into actual4B7CD0. Factory gate6A4818=0 stops before allocation; no new Shot is constructed, no post-success fields/motion/buff application are executed.',
      input_scope='Prepared Creature/World/definitions. CreatureSpell definition projected from actual rawID12 with previously verified phase55 load contract, not a newly executed raw loader. Slot fallback guard6E4168 bit1 is already set; no atexit initialization path run. Explicit rotation-coefficient fixtures, no game angle-table population proof.',
      slot=dict(raw_base=0xDC4,physical_count=3,raw_stride=24,runtime_base=0x5E1,runtime_stride=19,count_in_definition=0x789,
        getter='0x4943F0 uses full DWORD index and BYTEcount; invalid index returns shared fallback6E4178,not a temporary stack vector. Native lazy initializer sets19fallback bytes0; tests start after guard initialization.',
        current_slot_in_creature=0x399,delay_in_slot=2,XYZ_in_slot=6,play_animation_in_slot=18,
        animation_consumer='If the selected SpellId appears in the scanned runtime slots, copy bit0 of selected slotBYTE18 to CreatureDWORD3EA bit1000, preserving all other bits. Nonzero80 behaves as clear; no full animation playback proved.'),
      readiness=dict(clock='actual517490:World+4→session DWORD4',last_in_creature=0x3A5,delay_source='selected19-byte Creature spell slot DWORD2',
        rule='Unless CreatureDWORD3E6&80, return1 while tick<=u32(last+slotDelay). Strictly greater unsigned tick continues. Arithmetic wraps32bits; this is not a signed wrap-aware time comparison.',
        return_meaning='Local1 on waiting; local0 after attempt or missing spell. Do not interpret wrapper return1 as successful projectile creation.'),
      lookup=dict(creature_spell_exists='actual50E080 (WorldE4) precedes definition50DFA0 (WorldE8); missing ID stops safely in these two wrappers.',
        primary_shot='CreatureSpell runtimeBYTE44/rawE7 → low8 nativefactory argument. Both wrappers still call factory forShot0; actual50E2D0 rejects0 in fixtures. Third498870 has a separate static zero guard and is not dynamically covered.',
        factory='actual4B7CD0 receives World+8 this,ownerlow16,computedXYZ,angle pointer,output pointer,sound1; observer records real factory return address4B7CE5.',
        shot_exists='Actual50E2D0 reads World1624/162C,118-byte rows,IDBYTE58. Vtable diagnostic wrapper captures inputs then calls this original function; no constant existence result substituted.'),
      origin=dict(coefficients='DWORD A=[6C1488+4*angleWORD],B=[6C1C88+4*angleWORD]; supplied data fixtures',
        multiply='M(p,q)=s32(u32(p*q))>>16; low32 product before arithmetic shift,not a64-bit fixed-point multiply.',
        formula='dx=u32(M(slotX,B)-M(slotY,A));dy=u32(M(slotX,A)+M(slotY,B));if Creature3E6&80 then dx=dy=0. Origin=u32(actorXYZ+[dx,dy,slotZ]).',
        angle_units='Not established by these coefficient fixtures. No floating-point normalization or angle→degrees rule assigned.'),
      alternatives=dict(prior_loader_review=link('spell-catalogue-validation.json'),values_tested=[[36,4],[0,0],[68,0],[255,255]],
        result='With identical primaryShot and transferred fields, these previously projected loaded definitions produce identical pre-allocation request observations; definition checksum differs. This does not prove a global E8/E9 nonuse or absence of a different successful-cast consumer.'),
      static_third_method=dict(address='0x498870',executed=False,note='Reads same primary runtime44; requires nonzeroShot and a valid tagged target whose native kind is not9. Success trajectory and formatted diagnostic are not executed here.634DB0 is a buffer formatter,not proof of a log sink.'),
      limitations=['Only exact hashed DX executable dynamically checked. No new old/RC3/Flame claim. All original code remains unchanged and is checked against PE after execution.',
        'Gate0 is an explicit pre-allocation stop. Native success branch,allocator/constructor/worldregistration,target motion,actualshoot/buff effects remain outside this phase.',
        'The complete AI choice/animation event dispatch to these wrappers is not newly executed. Supplied runtime slot count/order is not proof of raw→runtime compaction policy.',
        '52 native compositions include prepared data and vtable capture wrapper; diagnostic OUT contains saved native result before comparison. No game process or original input modifications.'])
    (ROOT/'creature-shot-request-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    subs=load('creature-subrecords.json');subs['phase79_cast_request']=s;(ROOT/'creature-subrecords.json').write_text(json.dumps(subs,ensure_ascii=False,indent=2),encoding='utf8')
    alt=load('spell-alternative-semantics.json');alt['phase79_cast_request_reference']=link('creature-shot-request-semantics.json');(ROOT/'spell-alternative-semantics.json').write_text(json.dumps(alt,ensure_ascii=False,indent=2),encoding='utf8')
    write_doc(s);print('Phase79:',len(native['cases']),'native requests;',s['corpus']['exact_container_patch_cases'],'exact patches;successful allocation and alternative gameplay remain open')
def write_doc(s):
    c=s['corpus'];text=f'''# CreatureSpell: запрос выстрела, задержка и начало траектории

28 октября2026, этап79. Проверены52 native-композиции двух функций4982B0/4984D0 с настоящими getter,exists,clock и фабрикой4B7CD0. **Фабрика останавливается до выделения памяти** на её gate6A4818=0. Это проверка параметров запроса, а не успешного создания выстрела. Оригинальный код не подменяется.

## Основной ID и наличие определения

Обе функции берут runtime19-байтный слот существа через4943F0, вызывают настоящий CreatureSpell exists50E080, затем getter50DFA0. Это отдельная проверка наличия: ошибочный SpellId здесь не достигает чтения null-определения. Она не распространяется автоматически на predicate49A7B0 из этап78, у которого такого guard нет.

ID Shot берётся из **runtime44/rawE7**, передаётся в настоящую фабрику. В её вызове настоящего Shot-exists50E2D0 диагностический vtable-wrapper записывает ID,owner,XYZ,angle иsound; затем вызывает оригинальный exists. В нём используется118-байтная строка Shot иIDBYTE58. После отказа оба wrapper возвращаются обычным native-эпилогом; FS:[0] у4984D0 восстановлен.

В этих двух функциях ShotId0 всё ещё достигает фабрики, где existence-check его отвергает в проверенных мирах. Третья функция498870 просмотрена целиком статически: у неё есть отдельный guardShot!=0, а также условие tagged-target/kind. Она динамически не проверена; её дополнительные ветви не считаются исполненными.634DB0 форматирует строку в буфер, сам вызов не доказывает вывод в лог.

## Слот и задержка

4943F0 использует полныйDWORD индекса Creature399 иBYTEcount определения789. Элемент начинается в definition5E1 с шагом19. При неверном индексе выдаёт общий fallback6E4178; это не getter сырой24-байтной записи569D20 с другим поведением. Начальная19-байтная fallback-запись обнуляется lazy-initializer; в опыте guard уже установлен, регистрация atexit не исполнялась. Индекс256 не превращается в слот0.

Если Creature3E6 bit80 снят, блокировка длится, пока **unsigned tick <= u32(last3A5+slotDWORD2)**. Продолжение требует строгого `>`; равенство ещё блокирует. При overflow сумма обрезается до32бит. Возврат1 означает локальную ветвь ожидания и не является признаком созданного объекта. Задержка здесь идёт из слота **Creatures**, не из CreatureSpells.RechargeTime. Предшествующий storage-путь rawDC4+24*i+10 → runtimeSlot2 и преобразование raw×TPS→SHR12 сохраняются из ранее подтверждённого loader; заново полный loader не исполнялся.

## PlayAnimation: читается bit0

Если ID выбранного слота встречен в сканируемых runtime-слотах, **bit0 выбранного slotBYTE18** записывается в CreatureDWORD3EA bit1000. Остальные биты сохраняются. Для raw это PlayAnimation поDC4+24*i+0D. Значение80 ведёт себя как снятыйbit0, а не как1; отдельно проверена очистка ранее установленногоbit1000. Реальное воспроизведение анимации и все дальнейшие читатели actor flag здесь не исполнялись. Полные8бит файла сохраняются.

## Позиция: переполнение до сдвига

Для наблюдаемых DWORD-коэффициентов A/B:

`M(p,q)=s32(u32(p*q))>>16`

`dx=M(slotX,B)-M(slotY,A)`; `dy=M(slotX,A)+M(slotY,B)`; сложения обрезаются до32бит. При bit80 dx/dy становятся0, ноslotZ всё равно прибавляется. ИтоговыйXYZ — actorXYZ плюс эти смещения, также modulo2^32. Важно, что IMUL оставляет **младшие32бита до SAR16**; preview с64-битным произведением может дать другой результат. Проверены отрицательные коэффициенты, product/position wrap и обход XY приbit80. Таблицы коэффициентов заданы как явные данные опыта; кодировку угла в градусах и реальное заполнение этих таблиц этот опыт не устанавливает.

## Альтернативы и границы

В loaded-definition опытах пары E8/E9 `(36,4),(0,0),(68,0),(255,255)` изменяют ранее доказанный checksum, но оставляют тот же наблюдаемый запрос при одинаковом основном Shot. Definition получен проверенной проекцией прежнего phase55 loader из настоящей raw-записи12, а не новым исполнением raw-load в этом этапе. **Глобальная неиспользуемость E8/E9 не доказана**, успешные ветви после выделения Shot ещё надо проверить.

Данные мира, существа, определений и коэффициентов подготовлены явно. Не исполнялись allocator/constructor,регистрация объекта,полныйAI/animation-event dispatch,успешные target/motion callbacks и применение баффа. Начальное число/порядок runtime-слотов в fixture не доказывают raw→runtime compaction.

## Комплект и проверки

{c['creature_records']} Creature-записей и{c['creature_spell_records']} CreatureSpell-записей;{c['source_file_roles']} файловых ролей/{c['unique_source_files']} уникальных исходных файлов проверены поSHA. Новых точечных whole-container пересборок **{c['exact_container_patch_cases']}**: {c['creature_slot_patches']} для физическихXYZ-X/delay/animation полей3слотов первой записи каждого Creature-контейнера и{c['primary_shot_patches']} для основного ShotId. Вне выбранного диапазона сохраняется каждый байт, включая неизвестные0C/0E/0F/14/15, альтернативные ID, хвосты и порядок.

Схема `creature-shot-request-semantics.json` встроена в `creature-subrecords.json.phase79_cast_request`, ссылка добавлена в spell-alternative-semantics;пакет67компонентов. API `native_creature_shot_request.py`. Воспроизведение: `emulate_creature_shot_request.py`, `prepare_creature_shot_request_primary.py`, `ghidra_probe.py phase79-primary-queries.json`, `build_creature_shot_request.py`, `build_format_package.py`, `audit_creature_shot_request.py`.
''';(ROOT/'CREATURE_SHOT_REQUEST_RU.md').write_text(text,encoding='utf8')
if __name__=='__main__':main()
