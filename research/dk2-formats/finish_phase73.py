"""Publish actual Creature lookup returns without closing unknown purposes."""
from pathlib import Path
import json,hashlib
from finish_phase44 import insert_after_title
ROOT=Path(__file__).resolve().parent


def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))


def main():
    v=load('phase73-validation.json');assert v['native_full_getters']==34 and v['complete_executed_native_functions']==3 and v['new_jump_or_priority_semantic_consumers']==0
    p=ROOT/'INTEGRATION_RU.md';t=p.read_text(encoding='utf8').replace('Актуальный этап72: пакет сохраняет67 компонентов.','Предыдущий этап72: пакет сохраняет67 компонентов.');p.write_text(t,encoding='utf8')
    insert_after_title('INTEGRATION_RU.md','Актуальный этап73: пакет сохраняет67 компонентов. `CREATURE_LOOKUP_RU.md` / `catalogue-loading.json.phase73_creature_lookup`:34 полных native getter,реальный494360 helper. C0 сравнивает clone при unique,1C8 собственныйID;перестановка unique32/clone12 и ordinary13 меняет доступность32/13. Готовыеruntimeмассивы,неполныйparser;rawпорядок не нормализовать. Новый currentCreature739roots/12032states/48callees,357controlleads;Object246roots/3828states/17callees,3controls;по1callee limited. Priority/Jump consumer не установлен.3 wholePE/2vslots,82sourcehashes/4EXEs unchanged,новыхrawpatches0.')
    insert_after_title('REPORT_RU.md','Этап73,27 октября: `CREATURE_LOOKUP_RU.md` —34 native effective/raw getters с оригинальным494360;уникальный clone-key против собственногоID,обратный порядок может скрыть ordinary13. Это native-проверка прежних статических правил,не новая расшифровкаPriority. Current3-level pointer-search739roots357controls иObject246roots3controls,по1callee limited,unknownsopen.3 PEfunctions/2vslots,82hashcheckedfiles/4EXEs unchanged,67components. Server discovery/TCP discrepancy устранена guard в research-only launcher;существующий сервер не останавливался.')
    insert_after_title('CATALOGUE_LOADING_RU.md','Продолжение этап73 — `CREATURE_LOOKUP_RU.md`:два Creature getters и realeffective-key helper теперь проверены34 native-вызовами. В runtime массиве[unique32/clone12,ordinary13] raw32 не находится;в обратном порядке effective13 не находится. Полный insertion/parser здесь не исполнялся,rawrecords не сортировать автоматически. Существенные unknown gameplay fields остаются открытыми.')
    insert_after_title('CREATURE_PRIORITY_RU.md','Проверка этап73 — `CREATURE_LOOKUP_RU.md`:новый currentDX3-level ECX/stack-argument поиск739 roots/12032 states/48callees не установил consumer6EB/75C;357 контрольных чтений,1callee limited. ObjectPriority112 контроль246roots/3Healthleads/1callee limited также не расшифровал ordering.34 native Creature getter cases уточняют unique-key lookup,не игровую семантикуPriority/Jump.')
    insert_after_title('SOURCES.md','## Этап73: Creature effective/raw lookup и pointer-search\n\n`CREATURE_LOOKUP_RU.md`,`creature-lookup-review.json`,`phase73-validation.json`,`phase73-primary-manifest.json`,`phase73-creature-lookup-emulations.json`;`native_creature_lookup.py`,`emulate_creature_lookup.py`,`prepare_creature_lookup_primary.py`,`build_creature_lookup_review.py`,`audit_creature_lookup_review.py`,`finish_phase73.py`.34wholegettercases/3completePE/2vslotranges. NewcurrentCreature739roots/357controlleadsandObject246roots/3controls;1limitedcalleeineach,noPriority/Jumpmeaning. `phase73-runtime-launch-review.json` records reusedlive8032 server and TCPguard;noexistingserverstop/projectlockremoval.82sourcehashes/4EXEsunchanged;nonewrawpatches.')
    s=load('phase72-search-status.json')
    s.update(phase=73,status='native_Creature_effective_raw_lookup_and_new_current_argument_search_published;unknown_gameplay_purposes_open',
        previous_goal_turn_classification='progress:phase72Keeperlookups/searchpublished;phase73adds34nativeCreaturelookupswithrealunique-keyhelperandcurrentCreature/Objectargumentsearches',
        schema='creature-lookup-review.json;embedded catalogue-loading.phase73_creature_lookup',validation='phase73-validation.json',supplement='catalogue-loading.json.phase73_creature_lookup',
        findings=['34fullnativeeffective/rawlookups,17perroute;real494360helperexecutes','Order[unique32clone12,ordinary13]hidesraw32;reversedorderhideseffective13;fullruntimearraysunchanged','NewcurrentCreature739roots12032states48callees357controls,1calleelimited;newObject246roots3828states17callees3controls,1calleelimited','Exactaddress-exposure9operationsperEXEdoesnotestablishPriority/Jumpusage','3completeexecutedPEfunctions/2vslots;82sources/4EXEsunchanged,67components,0newrawpatches'],
        scope='Nativegetterswithprebuiltruntimearrays;notwholeparser/insertion/gameplay. Pointersearchboundedstaticwithuncertaingetter/vslothints;indirect/index/limitblindspotsretained.',
        evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase73-creature-lookup-emulations.json','phase73-primary-manifest.json','phase73-DX-creature-argument-leads.json','phase73-object-priority-pointer-leads.json','phase73-validation.json']],
        next_action='Do not repeat34Creaturegetters,739/246rootsearchesorearliercompletedKeeper/Objectpipelines. CurrentPriority/Jumpdirectargumentpathsremainunresolvedwithonecalleelimitineach;onlynewindirectreturn/indexed/dispatch/type-provedpathsjustifyfurthersearch. PrioritizeTerrain198224/AP0408/WanderRadius/CreatureSpellalternativesorKeeperC8specialproviders;fullCreatureinsertionwouldextendstaticassemblyproofbutmustnotreplaceunknownsemanticgoal. Preserveallrawunknownsandphysicalorder;goalremainsfulloriginalobjective.')
    (ROOT/'phase73-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack=load('editor-format-package.json');assert len(pack['components'])==67
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==pack[key]
    print('Phase73published:34nativeCreaturegetters/newboundedsearches;67components;fullgoalactive')


if __name__=='__main__':main()
