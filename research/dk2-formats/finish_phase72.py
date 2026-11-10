"""Publish native Keeper lookups and a bounded search without unknown renaming."""
from pathlib import Path
import json,hashlib
from finish_phase44 import insert_after_title

ROOT=Path(__file__).resolve().parent


def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))


def main():
    v=load('phase72-validation.json');assert v['native_lookup_cases']==26 and v['DX_search_roots']==45 and v['old_search_roots']==48
    p=ROOT/'INTEGRATION_RU.md';t=p.read_text(encoding='utf8').replace('Актуальный этап71: пакет сохраняет67 компонентов.','Предыдущий этап71: пакет сохраняет67 компонентов.');p.write_text(t,encoding='utf8')
    insert_after_title('INTEGRATION_RU.md','Актуальный этап72: пакет сохраняет67 компонентов. `KEEPER_LOOKUP_RU.md` / `keeper-pipeline-semantics.json.phase72_lookup_review`:26 полных native lookup двух EXE. ID0 не находится;[0,1,2] скрывает1;несортированный[1,7,3] скрывает7;разреженный[1,3,7] работает. Это runtime диагностика,не правило сортировки raw. Новый3-level argument поиск DX45/old48 roots,54 control leads,C8 read не установлен;нулевые budget limits не означают глобальный охват.2 полных PE getter/2 explicit ranges,82 source hashes/4 EXE неизменны;новых raw-патчей0. Существенные unknowns открыты.')
    insert_after_title('REPORT_RU.md','Этап72,27 октября: `KEEPER_LOOKUP_RU.md` —26 native lookup cases old5027A0/DX50DD60,все world/table bytes сохранены. Подтверждены ID0/начальныйindex/раннийвыход и дубликаты;физические records не нормализуются. Direct-callee argument поиск расширен на oldEXE:45/48 roots,835/711states,9/5callee summaries,54 controls,0C8candidate без глобальногоabsenceclaim.2 fullPE+2fragments/data,82sources/4EXEs unchanged,67components;C8purposeopen.')
    insert_after_title('KEEPER_PIPELINE_RU.md','Продолжение этап72 — `KEEPER_LOOKUP_RU.md`:полные old/DX getters проверены26 раз, включая физическийID0 и несортированные runtime-массивы. Новый argument search через3 direct-callee уровня не нашёл назначенияC8;это ограниченная отрицательная проверка. Static old4EC919 подтверждает mid140/pres18→def3A,не полный oldpipeline. RawC8 сохраняется,порядок физических records не меняется автоматически.')
    insert_after_title('SOURCES.md','## Этап72: Keeper lookup и argument-forward search\n\n`KEEPER_LOOKUP_RU.md`,`keeper-lookup-review.json`,`phase72-validation.json`,`phase72-primary-manifest.json`,`phase72-keeper-lookup-emulations.json`;`native_keeper_lookup.py`,`emulate_keeper_lookup.py`,`prepare_keeper_lookup_primary.py`,`prepare_keeper_pointer_search.py`,`scan_keeper_pointer_arguments.py`,`scan_old_keeper_pointer_arguments.py`,`build_keeper_lookup_review.py`,`audit_keeper_lookup_review.py`,`finish_phase72.py`.26fullnativegetters/2completePE/2explicitdata-codefragmentranges;DX45old48roots/54controlleads/0C8purpose.82sourcehashes/4EXEsunchanged;nonewrawpatches.')
    s=load('phase71-search-status.json')
    s.update(phase=72,status='native_old_DX_Keeper_lookup_and_bounded_argument_search_published;unknown_gameplay_purposes_open',
        previous_goal_turn_classification='progress:phase71Objectfullpipelinepublished;phase72adds26nativeold/DXgettercases,oldtypedvslot/loaderfragmentand3levelargumentsearchbothEXEs',
        schema='keeper-lookup-review.json;embedded keeper-pipeline-semantics.phase72_lookup_review',validation='phase72-validation.json',supplement='keeper-pipeline-semantics.json.phase72_lookup_review',
        findings=['26fullKeepergettercalls;all5888world/tablebytesunchanged;ID0/sparse/unsorted/duplicate/highargcontractsverified','Oldnativeworldvslot110=5027A0;oldscalarfragmentmid140/pres18→definition3Averifiedstatic','DX45roots835states9callees/old48roots711states5callees;54controlleads,0C8readcandidate','No budgetlimitsaturated,butindirect/depth/index/ABIlimitsremain;noglobalnon-useclaim','2completeexecutedPEfunctions/2datarange-codefragments;82sourcehashes/4EXEsunchanged,67components,0newrawpatches'],
        scope='Wholelookupfunctionswithprebuiltworldarrays;no normalworldinsertion/oldfullLoader/gameplay. Pointersearchboundedstatic,virtual110untypeduntilproved.',
        evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase72-keeper-lookup-emulations.json','phase72-primary-manifest.json','phase72-DX-keeper-pointer-argument-leads.json','phase72-old-keeper-pointer-argument-leads.json','phase72-validation.json']],
        next_action='Do not repeat26lookupcases,45/48rootargumentsearchorpreviouscompletedObjectpipelines. Prioritizeunresolvedactualconsumersviaas-yet-uncheckedpaths:CreatureAbilityJump/PickUpPriority,Terrain198224,AP0408,Spellalternatives,WanderRadiusandKeeperC8specialcast/Shotprocessproviders. C8couldbeusedthroughunresolvedindirectproviders;do notlabelunusedorassignunitsfromFE0C0000pattern. SignificantTraps204/Objects34C/Doors236andfullrender/worldassemblyremainopen. Preserveallrawbytes/fullobjective.')
    (ROOT/'phase72-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack=load('editor-format-package.json');assert len(pack['components'])==67
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==pack[key]
    print('Phase72published:26native/45+48rootsearch;67components;goalremainsactive')


if __name__=='__main__':main()
