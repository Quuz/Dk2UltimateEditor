"""Publish native CreatureSpell eligibility and initial AI-category mapping."""
from pathlib import Path
from collections import Counter
import json,hashlib,struct
import dk2_formats as fmt
from build_effect_semantics import pe_read
from native_spell_predicates import patch_predicate
from spell_state_tables import initialized_states
ROOT=Path(__file__).resolve().parent
DATA=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor'
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def virtual_mapping(data,address,size):
    pe=struct.unpack_from('<I',data,60)[0];count=struct.unpack_from('<H',data,pe+6)[0];opt=struct.unpack_from('<H',data,pe+20)[0];base=struct.unpack_from('<I',data,pe+52)[0]
    for i in range(count):
        at=pe+24+opt+40*i;header=data[at:at+40];vs,rva,rs,ro=struct.unpack_from('<IIII',header,8);delta=address-base-rva
        if 0<=delta and delta+size<=max(vs,rs):
            backed=min(size,max(0,rs-delta))
            return dict(section_name=header[:8].rstrip(b'\0').decode('ascii'),section_header_file_offset=at,section_header_hex=header.hex(),virtual_size=vs,raw_size=rs,raw_offset=ro,file_backed_bytes=backed)
    raise ValueError(hex(address))

def primary():
    m=load('phase78-primary-manifest.json');functions=[];blocks=[]
    for r in m['functions']:
        before,after,memory=[ROOT/'ghidra'/r[k] for k in ['before','after','memory']];v=json.loads(memory.read_text());b=bytes(v['data']);data=Path(r['exe']).read_bytes()
        assert before.read_text().rstrip()==after.read_text().rstrip()
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(data,r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in m['blocks']:
        p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data']);data=Path(r['exe']).read_bytes();mapping=virtual_mapping(data,r['address'],r['length'])
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length']
        if mapping['file_backed_bytes']==r['length']:assert b==pe_read(data,r['address'],r['length'])
        else:assert mapping['file_backed_bytes']==0 and b==bytes(r['length'])
        blocks.append({**r,'memory_sha256':digest(p.read_bytes()),'saved_program_bytes_sha256':digest(b),'PE_mapping':mapping,
            'scope':'File-backed slot checked against PE bytes; virtual-only tables are zero in saved Ghidra image. Actual callback/category values are proved by executing original initializers from A5-filled data.'})
    return functions,blocks

def corpus():
    files=[];counts=Counter();patches=selected_records=records=0
    for row in load('spell-writer-semantics.json')['corpus']['files']:
        p=Path(row['file']);original=p.read_bytes();assert digest(original)==row['sha256'];parts=fmt.parse(original);assert b''.join(s.pack() for s in parts)==original
        local=0
        for si,s in enumerate(parts):
            if s.signature!=160:continue
            records+=len(s.records);chosen={};alternate=None
            for ri,raw in enumerate(s.records):
                counts[raw[0xEE]]+=1;chosen.setdefault(raw[0xEE],ri)
                if alternate is None and (raw[0xE8] or raw[0xE9]):alternate=ri
            indices=set(chosen.values())
            if alternate is not None:indices.add(alternate)
            selected_records+=len(indices)
            for ri in sorted(indices):
                raw=s.records[ri]
                for value in [0,5,6,255]:
                    replacement=patch_predicate(raw,value);rr=list(s.records);rr[ri]=replacement
                    section=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);section.replace_records(rr)
                    packed=b''.join(section.pack() if j==si else x.pack() for j,x in enumerate(parts));expected=bytearray(original)
                    at=s.offset+len(s.header)+sum(map(len,s.records[:ri]))+0xEE;expected[at]=value
                    assert packed==expected and replacement[0xE8:0xEA]==raw[0xE8:0xEA] and replacement[0xEF:]==raw[0xEF:]
                    local+=1;patches+=1
        files.append(dict(file=str(p),sha256=digest(original),exact_container_patches=local))
    return dict(files=files,file_count=len(files),records=records,predicate_counts={str(k):v for k,v in sorted(counts.items())},selected_records=selected_records,exact_container_patch_cases=patches,
        selection='First record of each observed rawEE value per CreatureSpell section, plus first record with nonzero E8/E9; four explicit u8 patches0/5/6/255. Whole containers compared byte-for-byte except selected byte; raw alternatives, opaque tails, headers, other sections/order retained.')

def example():
    paths=[DATA/(n+'.kwd') for n in ['CreatureSpells','Shots','Rooms']];sp,sh,ro=[fmt.parse(p.read_bytes())[0].records for p in paths]
    shot=next(r for r in sh if r[0xBE]==36);room=next(r for r in ro if r[0x392]==4)
    name=lambda r:r[:32].split(b'\0')[0].decode('cp1252')
    assert name(shot)=='Chicken Arrow' and name(room)=='Hatchery'
    selected=[dict(creature_spell_id=r[0xE6],name=name(r),alternative_shot_id=r[0xE8],alternative_room_id=r[0xE9]) for r in sp if r[0xE6] in [12,24]]
    assert len(selected)==2 and all((r['alternative_shot_id'],r['alternative_room_id'])==(36,4) for r in selected)
    return dict(files=[dict(file=str(p),sha256=digest(p.read_bytes())) for p in paths],spells=selected,alternative_shot_name=name(shot),alternative_room_name=name(room),
        scope='Source metadata suggests inspecting food/hunger code. Four inspected functions are static only; they neither establish E8/E9 activation nor prove global nonuse. No alternate replacement rule assigned.')

def main():
    functions,blocks=primary();table,rows=initialized_states();assert all(struct.unpack_from('<I',table,25*i+16)[0]==i for i in range(281))
    output=ROOT/'phase78-initial-ai-state-table.bin';output.write_bytes(table)
    native=load('phase78-spell-predicate-emulations.json');assert len(native['cases'])==56
    s=dict(phase=78,date='2026-10-28',family='CreatureSpells',record_bytes=266,executable=functions[0]['exe'],executable_sha256=digest(Path(functions[0]['exe']).read_bytes()),
        field=dict(raw_offset=238,raw_bytes=1,intermediate_offset=318,runtime_offset=69,native_label_context='Alternative Shot rule; separate from Alternative Shot ID rawE8 and Alternative Room ID rawE9',
            meaning='Selects a native eligibility predicate for the creature supplied as this/ECX. Does not apply a buff, select an alternate shot or guarantee complete AI cast eligibility.'),
        native_emulations=native['cases'],primary_program_bytes=functions,primary_data_ranges=blocks,corpus=corpus(),source_example=example(),
        dispatch=dict(initializer='0x49A6C0',wrapper='0x49A7A0',dispatcher='0x49A7B0',table='0x6E4AC8',rows=6,stride=17,
            lookup='Global World6E5050.vE8 is actual50DFA0; low8 spell ID lookup. Wrapper passes second argument0; handlers use creature this/ECX. One direct nonzero-second-argument control checked.',
            missing_definition='No null check after getter before reading definition+45. Out-of-range selector return0 is conditional on a valid readable definition, not a missing-ID guarantee.',
            rules=[dict(value=i,callback=hex(a) if a else None,condition=condition) for i,a,condition in [
                (0,0,'Return0: null callback'),(1,0x49A830,'Creature WORD459==0'),
                (2,0x49A840,'signed currentHP DWORD40 < logical-half of level-scaled maximum; exact arithmetic below'),
                (3,0x49A8C0,'Effective AI-state category22 and WORD443==0'),(4,0x49A8F0,'Effective AI-state category22 and WORD445==0'),(5,0x49A920,'Effective AI-state category22 and WORD44F==0')]],
            other_values='u8 selectors6..255 return0; tested6/128/255. Raw values preserved rather than normalized.'),
        health=dict(current_hp='Creature DWORD40 compared signed32',base_hp='CreatureDefinition WORD692 unsigned16',level='Creature BYTE403; level1 skips coefficient; all other BYTE values use table',
            coefficient='signed WORD from global6E4154+E1+2*level',arithmetic='level1:threshold=baseHP>>1. Otherwise:scaled=truncTowardZero(s16Coefficient*u16BaseHP/100);threshold=(scaled&FFFFFFFF)>>1. Return s32(currentHP)<threshold.',
            boundary='Equality is false. Negative coefficients cause logical unsigned halving, not a clamp at0; controlled inputs prove this arithmetic, not normal-game reachability of negative level coefficients.'),
        ai_state_table=dict(initializer='0x47BBE0',instructions=3113,base=0x6DA8B0,rows=281,stride=25,category_offset=23,state_number_offset=16,
            initialized_binary=dict(file=output.name,sha256=digest(table)),helper='0x478050',state_subobject='Creature+24E',current_state='DWORD at subobject+10 (Creature25E)',backup_state='DWORD at subobject+DF (Creature32D)',
            selection='Read category[current]; only when category==2 read category[backup], once. No recursion or native bounds check. A direct category ignores backup evenFFFFFFFF.',
            category22_states=[r['state'] for r in rows if r['category']==22],category2_states=[r['state'] for r in rows if r['category']==2],
            initial_categories=[dict(**r,raw_hex=table[25*r['state']:25*(r['state']+1)].hex()) for r in rows],
            scope='Initial mapping from actual native initializer, entire7025bytes checked in each composition. No claim that future patches or runtime table writes cannot change it; no enum names assigned to all categories/states.'),
        real_caller=dict(function='0x45CB00',executed=False,self_predicate_call='0x45DB53',other_creature_predicate_call='0x45DC2C',
            evidence='First call sets ECX=ESI (current creature), second ECX=EDI (selected creature), each gets spell ID from actual4943F0 return pointer byte0; predicates are not called on the shot/room definition.',
            scope='Whole caller inspected statically; no complete AI selection loop, cast animation, projectile creation or buff application executed. Outer caller has extra gates including category22 checks, so local Haste/Heal success alone does not guarantee selection.'),
        limitations=['Only exact hashed DX executable dynamically checked here; no new claims for old/RC3/Flame-patched code.',
            'World, creature/definition buffers and level coefficient table are explicit data fixtures. Native code bytes are never replaced. Both initializers, real World getter and all five predicates execute.',
            'Diagnostic helper478050 is additionally called by observer after native predicate EAX is saved. Final EAX/ECX/EBP are comparison results; saved OUT0 is the actual predicate return.',
            'AlternativeE8/E9 gameplay replacement remains unresolved. Food/Hatchery reference and static hunger path are search clues only. No real game, renderer, GUI or original file writes.'])
    (ROOT/'spell-predicate-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    alt=load('spell-alternative-semantics.json');alt['phase78_predicate_runtime']=s;(ROOT/'spell-alternative-semantics.json').write_text(json.dumps(alt,ensure_ascii=False,indent=2),encoding='utf8')
    write_doc(s)
    print('Phase78:',len(native['cases']),'native compositions;',s['corpus']['file_count'],'files;',s['corpus']['exact_container_patch_cases'],'exact patches; alternative IDs unresolved')

def write_doc(s):
    c=s['corpus'];states=', '.join(map(str,s['ai_state_table']['category22_states']))
    text=f'''# CreatureSpells: условия применения и категория состояния AI

28 октября2026, этап78. Native-проверка расширяет прежнее статическое описание rawEE; rawE8/E9 остаются отдельными нерасшифрованными игровыми ссылками. Исходные игровые файлы, EXE и параллельный редактор не менялись.

## Что теперь подтверждено исполнением

56 композиций начинают с настоящего `47BBE0`, заполняющего281 строку таблицы AI, затем выполняют `49A6C0` и цепочку wrapper49A7A0→dispatcher49A7B0→World getter50DFA0→predicate. Исполняются все пять callbacks, а для3/4/5 — настоящий478050. Код EXE не подменяется. Observer после сохранения настоящего EAX отдельно вызывает478050 и сравнивает полные буферы. Это проверка условий, а не полный запуск AI/заклинания.

Raw **EE BYTE** переносится в intermediate13E и runtime45. Selector0/null и6..255 возвращают0 при существующем определении. **Отсутствующий Spell ID этим не защищён:** диспетчер не проверяет EAX getter на null перед чтением `[EAX+45]`. Его ошибочный ID нельзя называть безопасно отклоняемым selector.

| Selector | Native callback | Локальное условие |
|---:|---|---|
|0 |null |0 |
|1 |49A830 |WORD459 существа равен0 |
|2 |49A840 |signed currentHP ниже вычисленного порога |
|3 |49A8C0 |Категория состояния AI22 и WORD443==0 |
|4 |49A8F0 |Категория состояния AI22 и WORD445==0 |
|5 |49A920 |Категория состояния AI22 и WORD44F==0 |

Смысловые названия Haste/Heal/Armour/Invulnerable/Invisible сохраняются как ранее сопоставленные имена; новые выводы опираются на адреса и инструкции, не на сторонний enum. Таймеры читаются какWORD:0100 неэквивалентен0.

## Категория22 принадлежит состоянию AI

`478050` получает подструктуру Creature+24E. Читает **DWORD+10**, берёт байт категории из таблицы6DA8B0 с шагом25, смещение23. Если категория равна2, один раз берёт категорию сохранённого **DWORD+DF**. Второе значение2 не разворачивается дальше. Проверки границ native-helper не делает; наш диагностический API их проверяет только для реально читаемого индекса. При прямой категории22 backupFFFFFFFF не читается.

Начальное соответствие категории22: **{states}**. Все девять проверены native-предикатом3, включая259/260: индекс не обрезается доBYTE. Полная начальная таблица7025байт в каждом сценарии совпала со статической проекцией3113 инструкций оригинального initializer. В ней также сохранены прочие необъяснённые байты25-байтных записей. Названия всех состояний/категорий не придуманы; возможные последующие изменения таблицы этим этапом не исключаются.

Таблицы AI и predicate в сохранённом проекте нулевые и находятся в virtual-only диапазоне PE. Указатели callbacks и категории доказываются **native-инициализаторами**, не выдаются за готовые указатели внутри файловых байтов EXE. Для опыта начальные таблицы заполненыA5, и сравниваются все7025/102байта после исполнения.

## Точная арифметика Heal

CurrentHP — DWORD40 с signed-сравнением; базовый максимум — unsignedWORD692 определения. При levelBYTE403==1 порог=`baseHP>>1`; коэффициент не читается. Для остальных уровней, включая0/255, коэффициент — **signedWORD** по global6E4154+E1+2*level. Сначала вычисляется деление произведения на100 с усечением к нулю; затем32-битный результат сдвигается **логически** вправо на1.

При базе100, level1 HP49 проходит,50/51 — нет. При150% level2 порог75, равенство не проходит. Отрицательный коэффициент не зажимается к0: база100 и−100% дают после логического сдвига порог2147483598, так что HP100 проходит. Это контролируемый арифметический край, а не утверждение, что отрицательные коэффициенты достижимы через штатный редактор или обычную игру.

## Реальный вызывающий код и границы

В целом статически просмотренном45CB00 вызов45DB53 передаёт ECX текущего существа,45DC2C — выбранного другого существа. Wrapper передаёт второй аргумент0; callbacks проверяют объектthis, а не этот аргумент. Один прямой nonzero-argument контроль также пройден. Во внешнем вызывающем коде есть дополнительные ограничения. Поэтому успешный локальный Haste/Heal вне категории22 не означает, что полный AI выберет заклинание в этом состоянии.

В standalone у стрелковых Spell12/24 Alternative36/Room4 разрешаются в Chicken Arrow/Hatchery. Четыре функции пути голода/еды4633D0,463500,463760,463BE0 просмотрены статически. Это зацепка для поиска; **механизм игрового использования rawE8/E9 не установлен**, глобальная неиспользуемость не заявлена. Полный вызов45CB00, выстрел и применение баффа здесь не исполнялись.

## Сохранение данных и воспроизведение

Проверены {c['file_count']} исходных файлов/{c['records']} CreatureSpell-записей; {c['exact_container_patch_cases']} новых точечных пересборок целых контейнеров. В каждой изменяется только выбранный EE BYTE (0/5/6/255), сохраняются E8/E9, неизвестный хвостEF..109, заголовки, порядок и остальные секции. Выбираются первые записи каждого реально встреченного selector в секции и первая с ненулевой парой альтернатив. Значения6..255 поддерживаются как физические байты и диагностируются по проверенному predicate-поведению, не заменяются0.

`spell-predicate-semantics.json` встроен в `spell-alternative-semantics.json.phase78_predicate_runtime`, пакет сохраняет67 компонентов. API: `native_spell_predicates.py`. Native-сценарии: `emulate_spell_predicates.py`; первичные байты: `prepare_spell_predicate_primary.py`, `ghidra_probe.py phase78-primary-queries.json`; сборка/аудит: `build_spell_predicates.py`, `build_format_package.py`, `audit_spell_predicates.py`. `phase78-initial-ai-state-table.bin` — воспроизводимый результат оригинального initializer в исследовательской папке, не патч игры.
'''
    (ROOT/'SPELL_PREDICATES_RU.md').write_text(text,encoding='utf8')
if __name__=='__main__':main()
