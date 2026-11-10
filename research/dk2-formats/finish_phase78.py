"""Publish phase78 without shrinking the full original reverse-engineering goal."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(name,text):
    p=ROOT/name;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')
def main():
    v=load('phase78-validation.json');assert v['native_compositions']==56 and v['exact_container_patch_cases']==2248
    insert('INTEGRATION_RU.md','Актуальный этап78: `SPELL_PREDICATES_RU.md` / `spell-alternative-semantics.json.phase78_predicate_runtime`.56 native композиций с оригинальными initializer таблиц AI281rows/7025bytes и predicate6rows/102bytes,actualWorldgetter и всеми5 callbacks. Категория22 принадлежит AI-state,при категории2 однократно читается backup;9 прямых состояний проверены,включая259/260. Уточнены signedHP/signedкоэффициент/логическое деление пополам и отсутствие null-guard определения.16 wholePE (11 исполнялись),3data;2248 exact patches/82файла/3535записей,4EXE unchanged,67компонентов. RawE8/E9 игрового назначения не получили;полныйAI/выстрел не исполнялся.')
    p=ROOT/'INTEGRATION_RU.md';p.write_text(p.read_text(encoding='utf8').replace('Актуальный этап77:','Предыдущий этап77:',1),encoding='utf8')
    insert('SPELL_ALTERNATIVES_RU.md','Дополнение этап78 — `SPELL_PREDICATES_RU.md`:56 native-проверок rawEE eligibility с настоящими инициализаторами281 AI-state/6predicate. Категория22 — категория AI-состояния;девять начальных состояний и однократный backup подтверждены. Уточнены Heal и null-guard. Это не расшифровка применения альтернативных ID E8/E9; они остаются открытыми.')
    insert('SPELLS_RUNTIME_RU.md','Уточнение этап78 — `SPELL_PREDICATES_RU.md`: rawEE теперь проверен56 native-композициями. Категория0x16 вычисляется из подструктуры состояния AI,не является постоянной категорией типа существа;при категории2 берётся backup один раз. Heal использует signedWORD коэффициент и логическийSHR результата;missing SpellID не проверяется наnull перед чтением selector. Полный AI caller статически имеет дополнительные ограничения. E8/E9 gameplay остаётся неизвестным. Состояния/ограничения/API включены в пакет через spell_alternative_semantics.phase78_predicate_runtime.')
    insert('SOURCES.md','## Этап78: native CreatureSpell predicates и таблица AI\n\n`SPELL_PREDICATES_RU.md`,`spell-predicate-semantics.json`,`phase78-validation.json`,`phase78-primary-manifest.json`,`phase78-spell-predicate-emulations.json`;`native_spell_predicates.py`,`spell_state_tables.py`,`emulate_spell_predicates.py`,`prepare_spell_predicate_primary.py`,`build_spell_predicates.py`,`audit_spell_predicates.py`,`finish_phase78.py`.56 native-композиций,11 исполнявшихся/5 статических wholePE функций,3data. Native47BBE0 и49A6C0 заполняютA5-initialized buffers;всяAIтаблица7025bytes/281rows проверена. Значенияcallback/category не выдаются за файловые указатели в virtual-only диапазоне PE.2248 exactcontainerpatches,82inputs/4EXEs unchanged. E8/E9 remainunknown,AIcastnotrun.')
    status=load('phase77-search-status.json')
    status.update(phase=78,status='native_CreatureSpell_predicates_and_AI_state_categories_published;AlternativeIDs_gameplay_open',
      previous_goal_turn_classification='progress:researchGhidrarestoredandguardfixed;newnativeAIinitializer/predicatecompositionsandwholecallerprimaryevidence',
      schema='spell-predicate-semantics.json;embedded spell-alternative-semantics.phase78_predicate_runtime',validation='phase78-validation.json',supplement='spell-alternative-semantics.json.phase78_predicate_runtime',
      findings=['56nativeDXcompositions;11executedfunctions;actual281-rowAIand6-rowpredicateinitializers;all5callbacksandrealWorldgetter',
        'Category22isAIstatecategory;conditionalone-hopcategory2backup;9directstatesverifiedincluding259/260',
        'Heal:signedHP,signed16coefficient,trunc100thenlogicalhalf;missingdefinitionhasnonullcheck;localpredicateisnotfullAIeligibility',
        'Whole45CB00callerand4foodfunctionsstatic;ChickenArrow36/Hatchery4referencesarecluesnotE8/E9consumerproof',
        '16wholePE/3data;2248exactcontainerpatches/82inputs3535records;4EXEsunchanged;67components'],
      scope='Newnativeinitializers/getter/predicatesandinitialstatecategorymapping,notfullAIorprojectile/buffapplication. E8/E9 replacement andalloriginalunknownsremain.',
      evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase78-spell-predicate-emulations.json','phase78-primary-manifest.json','phase78-validation.json','spell-predicate-semantics.json']],
      next_action='Do not repeat56predicatecompositionsor2248patches. TraceactualCreatureSpellcast/projectileconsumerornewmajorunknownnativeconsumer. Stateinitializer47BBE0/281rowsmayhelpmapactualmovement/combatcallbacks,butcategorylabelsalonearenotAbilityJump/Prioritysemantics. E8/E9stillnotpersistedbyverifiedloader;foodmetadataaloneisnotusage. Preserveoriginalscope,unknownrawbytesandphysicalorder;goalactive.')
    (ROOT/'phase78-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print('Phase78published;alloriginalfamiliesremaininscope;goalactive')
if __name__=='__main__':main()
