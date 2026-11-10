"""Publish successful native pool/factory/CreatureSpell execution and limits."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
from native_spell_writer import patch_raw
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    spec=load('phase80-primary-manifest.json');out=[];blocks=[]
    for r in spec['functions']:
        p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data']);a=ROOT/'ghidra'/r['assembly']
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append({**r,'assembly_sha256':digest(a.read_bytes()),'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        blocks.append({**r,'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    return out,blocks
def corpus():
    files={r['file']:dict(file=r['file'],sha256=r['sha256']) for r in load('creature-shot-request-semantics.json')['corpus']['files']}
    for r in files.values():assert digest(Path(r['file']).read_bytes())==r['sha256']
    patches=records=0
    for row in load('spell-writer-semantics.json')['corpus']['files']:
        p=Path(row['file']);original=p.read_bytes();assert digest(original)==row['sha256'];parts=fmt.parse(original);assert b''.join(s.pack() for s in parts)==original
        for si,s in enumerate(parts):
            if s.signature!=160:continue
            records+=len(s.records)
            if not s.records:continue
            ri=next((i for i,r in enumerate(s.records) if r[0xE6]==12),0);raw=s.records[ri]
            for offset in [0xC8,0xCC]:
              for value in [0,0x80000000,0xFFFFFFFF]:
                rr=list(s.records);rr[ri]=patch_raw(raw,offset,value);section=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);section.replace_records(rr)
                packed=b''.join(section.pack() if j==si else x.pack() for j,x in enumerate(parts));expected=bytearray(original);at=s.offset+len(s.header)+sum(map(len,s.records[:ri]))+offset;expected[at:at+4]=value.to_bytes(4,'little');assert packed==expected;patches+=1
    return dict(files=list(files.values()),unique_source_files=len(files),creature_spell_files=82,creature_spell_records=records,new_exact_container_patches=patches,
        scope='Hashcheck prior Creature/CreatureSpell inputs;new Data1/Data2 DWORD patches0/80000000/FFFFFFFF in one selected rawID12(orfirst) record per CreatureSpell file;all unknown bytes,alternativeIDs,headers,order and other sections preserved.')
def node_rows():
    from emulate_native_shot_success import cases as factory_cases
    from emulate_native_creature_shot import cases as cast_cases
    rows=[]
    for role,cases,prefix in [('factory',factory_cases(),'phase80-native-shot-'),('creature',cast_cases(),'phase80-native-creature-shot-')]:
        for c in cases:
            n='ghidra/'+prefix+c['name']+'.bin';b=(ROOT/n).read_bytes();assert len(b)==371
            allocated=role=='creature' or (c['gate']!=0 and not c['empty'] and c['block_size']>=384 and c['shot']!=0)
            rows.append(dict(role=role,case=c['name'],allocated=allocated,file=n,bytes=371,sha256=digest(b)))
    return rows
def main():
    functions,blocks=primary();pool=load('phase80-native-pool-emulations.json');factory=load('phase80-native-shot-success-emulations.json');creature=load('phase80-native-creature-shot-emulations.json')
    assert len(pool['cases'])==9 and len(factory['replays'])==42 and len(creature['replays'])==66
    base=(ROOT/'ghidra/phase80-native-shot-base.bin').read_bytes();zero=(ROOT/'ghidra/phase80-native-shot-seed-zero.bin').read_bytes();dependent=[i for i,(a,b) in enumerate(zip(base,zero)) if a!=b]
    shots=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Shots.kwd';r=next(r for s in fmt.parse(shots.read_bytes()) if s.signature==260 for r in s.records if r[0xBE]==30)
    assert r[:32].split(b'\0')[0]==b'Lightning Shot' and r[0xC7]==9
    s=dict(phase=80,date='2026-10-28',families=['Creatures','CreatureSpells','Shots'],executable=functions[0]['exe'],executable_sha256=digest(Path(functions[0]['exe']).read_bytes()),
      primary_program_bytes=functions,primary_data_ranges=blocks,native_pool_emulations=pool['cases'],native_factory_replays=factory['replays'],native_creature_replays=creature['replays'],raw_snapshots=node_rows(),corpus=corpus(),
      counts=dict(logical_scenarios=45,pool_scenarios=9,factory_scenarios=14,creature_scenarios=22,emulation_invocations=117,factory_success_scenarios=11,creature_success_scenarios=22,successful_allocation_replays=99,original_code_replacements=0),
      capture=dict(api='Existing emulate_function;return_registers XMM0..7',helper='memory_readout.py',method='After saving native return,synthetic observer loads128bytes into XMM registers. Three independent fresh identical-input executions assemble371 object bytes;not an atomic live-process snapshot.',
        full_checks='Each replay compares prepared globals/tag arrays/owner/map/pool structures,read-only inputs and FS state;the created object is fully captured,with explicit interpreted-field assertions and opaque remainder retained.',
        historical_diagnostic='phase80-emulation-native-shot-base-page0.json is an excluded failed experiment missing sound manager pointer. Only v2 factory replays and named creature/pool manifests are published.'),
      pool=dict(entry='0x554B30',request_bytes=371,aligned_payload=372,header_bytes=12,accounted_allocation=384,
        rule='First fitting finite free-list block. Split iff remaining>12,otherwise unlink whole block. Used-list insertion and pool30 += align4(request)+12. Absorbed12-byte remainder does not increase this accounted amount;no free/underflow consequence inferred.',
        scope='Prepared finite lists with distinct head/tail sentinels. Requests0/1/4 and371,split/exact/leftover12,small/empty checked. No OS allocation or malformed/cyclic list claim.'),
      registration=dict(native_constructor='0x507850',tag_pointer_array='0x6EE540',tag_alive_array='0x6ED540',available_count='DWORD6A4818',next_tag='WORD6A481C',forced_tag='WORD6A4820 whenDWORD6F2540!=0',
        result='Successful native allocation/construction records actual node pointer and alive byte,consumes one available tag,updates normal nextTag or clears forced flag. Actual kind/owner/spatial linked lists execute. Pool failure/gate0 leave these prepared structures unchanged.'),
      factory=dict(entry='0x4B7CD0',constructor='0x4B7D60',initializer='0x4AAC90',object_bytes=371,native_vtable='0x66D86C remains original',definition_bytes=118,
        effects='Original base/owner/map/resource constructors and original vslot8C setter execute;external gfx request returns0 and sound returns a fixed handle. ShotDefinitionWORD6D=0,resource type0. No successful render resource or effect child allocated.',
        source_dependent_object_bytes=dependent,opaque_rule='19 bytes retain some dependence on initial00/A5 fill. This is raw/bit dependence,not19 confirmed padding bytes or proof of a gameplay defect. No meaning or zeroing policy assigned.'),
      successful_cast=dict(entries=['0x4982B0','0x4984D0'],field_flow=[
        dict(family='Shots',raw_offsets=[0x98,0x9C],definition_offsets=[0x37,0x3B],instance_offsets=[0x13D,0x141],stage='initializer'),
        dict(family='CreatureSpells',raw_offsets=[0xC8,0xCC],definition_offsets=[0x1F,0x23],instance_offsets=[0x13D,0x141],stage='post-factory override')],
        bit_width='Both CreatureSpell parameters replace all32bits;FFFFFFFF/80000000 tested. This CreatureSpellsC8 is Data1,not unresolved KeeperSpellsC8.',
        creator='Both write actorID toWORD145;4982B0 additionally writesWORD16D. Later meanings of these tags are not all established.',
        flags='4984D0 setsDWORD151=1 when actor3E6&80;alsoDWORD165=1 when80000000 is set.4982B0 does not perform these writes;no universal rawCreature-flag gameplay name assigned.'),
      trajectory=dict(native=['0x4AB830','0x4AB8A0','0x4ABA10','0x65FB54','0x65FAE0'],
        speed='u16ShotDefinition4B<<4,then signed64-bit products with prepared trig coefficients and shift16. This differs from origin low32IMUL/SAR16 in phase79.',
        evidence='Cardinal+X target producesVX=speed<<4,VY=VZ=0,including speedFFFF→FFFF0. Actual integer sqrt/angle functions and ratio tables execute;original Shot.v68 flag helper also executes.',
        coefficient_scope='One prepared2560-DWORD shared array at6C1488,nonzero only cardinal indices512(+65536) and1536(-65536);cos accessor starts512entries later. This is a fixture,not proof of actual game table population/interpolation.',
        height='For tested empty terrain/default floor1000 and inputZ4000,actual4B5020 changes current Z to2000;initial previousXYZ remains inputZ4000. Floor collision below1000/RTTI reflection branches not exercised.'),
      hardcoded_id30=dict(entry='0x4984D0',identity='ID30,not ProcessType9,selects this branch',
        native_result='After successful create/parameter copy,if actor target tag is alive and kindDWORD12==0,storeWORD147 targetID andcopy targetXYZ via4AB830;return without ordinary4AB8A0 velocity setup. Invalid/missing/noncreature tag leaves target/velocity unset in these fixtures.',
        other_entry='4982B0 continues ordinary target/velocity setup for ID30 in these tests;do not generalize this branch to all creation routes.',
        source_example=dict(file=str(shots),sha256=digest(shots.read_bytes()),shot_id=30,name='Lightning Shot',process_type=9),
        limitation='Tests use prepared ShotDefinitionProcessType0 even withID30,proving numericID branch separately. Actual ProcessType9 tick/hit/damage/lightning immunity is not newly executed.'),
      alternatives=dict(raw_pairs=[[36,4],[0,0],[68,0],[255,255]],comparison='For each wrapper,the full371-byte created Shot is identical across these four loaded-definition variants. Checksum of CreatureSpell differs;parameters/primaryShot are held equal.',
        loader_scope='Definitions use previously verified phase55 projection from original rawID12;no new raw parser execution. All resources/providers/targets are as stated. Other versions,successful graphics and other consumers may differ;global nonuse remains unproven.'),
      limitations=['Only exact hashed DX dynamically checked. Original game/EXEs/parallel editor unchanged;no live gameplay.',
        'Null GFX and fixed sound providers remain synthetic. Resource type0,definition6D=0 and graphics handle0 avoid renderer/reflection/child resource branches;no collision or Shot process tick is executed.',
        'Prepared pools/world/maps/entities and cardinal coefficients constrain scope. Native script execution was not enabled;original code/vtable bytes are never overwritten.',
        'Every captured371-byte object is assembled from3 fresh deterministic replays;unknown bytes remain opaque. Readout observer SIMD instructions are research code,not an assertion that game code uses SIMD.',
        'The complete AI/animation-event dispatch to the two wrappers is not executed. AlternativeE8/E9 global use and all other original unknowns remain open.'])
    (ROOT/'native-shot-success-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    parent=load('shot-creation-semantics.json');parent['phase80_native_pool_and_cast']=s;(ROOT/'shot-creation-semantics.json').write_text(json.dumps(parent,ensure_ascii=False,indent=2),encoding='utf8')
    for n in ['creature-subrecords.json','spell-alternative-semantics.json','shot-semantics.json']:
        parent=load(n);parent['phase80_native_success_reference']=link('native-shot-success-semantics.json');(ROOT/n).write_text(json.dumps(parent,ensure_ascii=False,indent=2),encoding='utf8')
    write_doc(s);print('Phase80:45scenarios/117native invocations;33successful scenario states;40PE/8data;',s['corpus']['new_exact_container_patches'],'new exact patches')
def write_doc(s):
    c=s['corpus'];text=f'''# Успешное создание Shot и CreatureSpell: native-пул, регистрация, направление

28 октября2026, этап80. После этап79 выполнен успешный путь с **оригинальными allocator,базовыми конструкторами,ownership,map registration,resource setup и Shot vtable**. Ничьи машинные инструкции и исходные vtable-байты не подменяются. GFX-provider возвращает0, sound-provider — фиксированный handle; ограничения ниже остаются существенными.

45 логических сценариев:9 пула,14 фабрики,22 полных вызовов CreatureSpell.117 запусков эмулятора:9+42+66. Для каждого объекта3 свежих запуска с одинаковыми начальными данными считывают по128байт через post-return наблюдатель XMM0..7;полный371-байтный snapshot собирается из них. Это не атомарный дамп живого игрового процесса. Успешных состояний сценария33:11 фабрики и22 CreatureSpell;каждый повторяется3раза.3 фабричных сценария отказа не создают объект,их `.bin` — только снимок подготовленной невыделенной области.

## Пул и настоящая регистрация

554B30 округляет371 до372 и прибавляет12 байт заголовка,учётное выделение384. Берёт первый подходящий блок конечного free-list;делит его при остатке>12,иначе переносит целиком в used-list. Проверены384/396/400/512,нехватка380,пустойсписок и выравнивание0/1/4. При поглощении остатка12 счётчик увеличивается на384; последствия освобождения этим этапом не изучались.

Настоящий507850 назначает tagID,записывает pointer6EE540[id] иalive6ED540[id],уменьшает6A4818. Обычный поиск пропускает занятыйID;проверена также forced-ID ветвь. Выполнены реальные списки типа,владельца и клетки карты;сравниваются полные подготовленные структуры мира,карты,owner,tag arrays,пула иreadonly-входы. Gate0/нехватка пула не регистрируют новый Shot в этих состояниях.

## Параметры CreatureSpell заменяют параметры Shot

| Источник | Raw | Definition | Shot instance | Стадия |
|---|---|---|---|---|
|Shots Data1/Data2 |98/9C DWORD |37/3B DWORD |13D/141 DWORD |Инициализация |
|CreatureSpells Data1/Data2 |C8/CC DWORD |1F/23 DWORD |13D/141 DWORD |После успешной фабрики |

Оба wrapper4982B0/4984D0 действительно заменяют все32бита;провереныFFFFFFFF/80000000. Это **CreatureSpells.Data1 поC8**,отдельное от неизвестного **KeeperSpells.C8**. Назначение Data1/Data2 всё ещё определяется фактическим обработчиком Shot;новое универсальное имя вроде«урон»не назначается.

Оба wrapper записывают actorID вWORD145;4982B0 дополнительно пишетWORD16D.4984D0 при actor3E6 bit80 назначаетDWORD151=1,а при дополнительном80000000 — DWORD165=1. Дальнейшие значения этих instance-полей здесь не превращаются в общий набор raw-флагов редактора.

## Направление и высота

Исполнены настоящие65FB54 (угол),65FAE0 (целочисленный корень),4AB830,4AB8A0 и4ABA10. Скорость берётся из unsignedWORD definition4B и сдвигается влево4. **Здесь произведения с коэффициентами64-битные**,сдвиг16 выполняется после полного произведения. Для подготовленной цели по+X speedFFFF даётVX=FFFF0,VY/VZ=0. Это отличается от расчёта origin в этапе79,где берутся младшие32бита произведения доSAR16.

Коэффициенты заданы явно:общий массив2560DWORD с cardinal-значениями;реальное заполнение игровых таблиц не восстановлено. Эти данные не дают оснований обещать точное воспроизведение всех углов. Успешный renderer/его ресурсы не создаются.

Реальный4B5020 для проверенного пустого terrain/floor1000 при входномZ4000 оставляет текущийZ2000,а ранее скопированный previousXYZ сохраняетZ4000. Ветви отражения/RTTI приZ ниже пола не исполнялись.

## Особая ветвь ID30

В4984D0 число **ShotId30** выбирает отдельную ветвь. При живом target tag сkindDWORD12==0 записываетсяWORD147=targetID икопируетсяXYZ через4AB830;обычная настройка скорости4AB8A0 затем не вызывается. При отсутствующей цели илиkind2 объект в этих опытах всё равно создан,но target/velocity остаются неустановленными. В4982B0 тот жеID30 проходит обычную настройку направления. Такое поведение нельзя автоматически переносить на все способы создания Shot.

В исходном standalone Shots.kwd ID30 называетсяLightning Shot,ProcessType9. В этих опытах подготовленный ShotDefinition имелProcessType0:ветвь подтверждена как зависимость от **числаID**,а не отProcessType. Обработчик попадания,урон и lightning-immunity здесь заново не исполнялись.

## Alternative Shot/Room и неизвестные байты

Для каждого wrapper пары E8/E9 `(36,4),(0,0),(68,0),(255,255)` дали **полностью одинаковый371-байтный созданный Shot** при одинаковом основномID и перенесённых полях. Definition берётся из ранее проверенной phase55-проекции настоящей raw-записи12;нового исполнения parser/load в этом этапе нет. Это усиливает границу доказанного для данного успешного пути,но не доказывает глобальное неиспользование E8/E9 в других потребителях/версиях или при успешных ресурсных ветвях.

При начальном заполнении пула00/A5 зависимость сохранилась в19 байтах instance. Среди них есть частично сохраняемые биты;они не объявляются19 padding-байтами и не получают вымышленного смысла. Снимки сохраняются полностью. Из этого наблюдения не следует автоматически ошибка игры или требование обнулить KWD.

## Комплект, проверки и оставшаяся работа

40 целых code spans и8таблиц/слотов перечитаны из Ghidra после исполнения и совпадают с исходнымPE.117 эмуляционных входов проверяются на отсутствие перекрытия любого такого кода и оригинальной Shot vtable. Все4 исходныхEXE проверены поSHA. {c['unique_source_files']} входных Creature/CreatureSpell файлов сохраняютSHA;дополнительно зафиксирован standalone Shots.kwd для примераID30. Новых точечных пересборок **{c['new_exact_container_patches']}**: Data1/Data2 одного выбранного CreatureSpell в82файлах;меняются только4выбранных байта,остальные секции,unknown tails,E8/E9,заголовки и порядок сохраняются.

Схема `native-shot-success-semantics.json` встроена в `shot-creation-semantics.json.phase80_native_pool_and_cast`;ссылки добавлены в creature_subrecords,spell_alternative_semantics иshot_parameters. Пакет сохраняет67компонентов. Доказательства: `phase80-native-pool-emulations.json`,`phase80-native-shot-success-emulations.json`,`phase80-native-creature-shot-emulations.json`,`phase80-primary-manifest.json`,`phase80-validation.json`. Ранний неудачный base-прогон без sound manager pointer сохранён какдиагностика иисключён из итоговыхmanifest;фабричные валидные результаты имеютv2.

Не исполнялись полный AI/animation-event dispatch,последующие Shot ticks,попадание/коллизии,реальный звук/renderer,resource-type6/childEffect6D и прочие исходныеunknowns. Цель исследования всех15семейств остаётся активной. Воспроизведение: `emulate_native_shot_pool.py`,`emulate_native_shot_success.py`,`emulate_native_creature_shot.py`,`prepare_native_shot_success_primary.py`,`build_native_shot_success.py`,`audit_native_shot_success.py`.
''';(ROOT/'NATIVE_SHOT_SUCCESS_RU.md').write_text(text,encoding='utf8')
if __name__=='__main__':main()
