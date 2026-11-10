"""Publish native Object loading while retaining unresolved gameplay scope."""
from pathlib import Path
import json
import hashlib
from finish_phase44 import insert_after_title

ROOT=Path(__file__).resolve().parent


def load(name):return json.loads((ROOT/name).read_text(encoding='utf8'))


def main():
    v=load('phase71-validation.json')
    assert v['successful_native_cases']==86 and v['expected_native_DIV0_stops']==4 and v['new_exact_light_span_patches']==23512
    p=ROOT/'INTEGRATION_RU.md';t=p.read_text(encoding='utf8').replace('Актуальный этап70: пакет сохраняет67 компонентов.','Предыдущий этап70: пакет сохраняет67 компонентов.');p.write_text(t,encoding='utf8')
    insert_after_title('INTEGRATION_RU.md','Актуальный этап71: пакет сохраняет67 компонентов. `OBJECT_LOADING_RU.md` / `object-property-semantics.json.phase71_loading`:86 успешных native-сценариев полной Object загрузки и4 ожидаемых DIV0 остановки двух EXE. Unknown34C→def106 сохраняет16 бит;raw341..343 не переносятся. Цветовые328..32A устанавливают LightFlags bit0,чёрный не сбрасывает;32B только копируется. Speed делит на TPS до нулевой проверки,а presence0 пропускаетDIV.6 remaining anchors отвергнуты по реальным границам,одна switch ветка восстановлена.15 code extents/8 executed/3 data,23512 новых light-пересборок/84 файла11756 Objects;4 EXE неизменны. Gameplay unknowns и visual renderer открыты.')
    insert_after_title('REPORT_RU.md','Этап71,27 октября: `OBJECT_LOADING_RU.md` —86 успешных Object init/converter/loader cases+4 ожидаемых остановкиDIV при TPS0. Полные894/1022/285 buffers сравнены при null visual provider. LightFlags|=anyRGB,black сохраняетbit0,четвёртый byte не включает;unknown34C16бит,3rawpaddingнепереносятся.6 anchored false starts rejected,5-target switch recovered.15 PE extents/3data,8 executedfunctions,23512 новых rawlightpatches/84 sources,4 EXE unchanged. НулевойTPS defect scoped to loadercontract;реальнаяигроваядоступностьнепроверена.')
    insert_after_title('OBJECT_PROPERTIES_RU.md','Продолжение этап71 — `OBJECT_LOADING_RU.md`: полный loader в двух EXE проверен с null visual provider.86 успехов+4 ожидаемых DIV0;LightFlagsbit0 устанавливается по любому из RGB328..32A,чёрный не очищает. Speed presence44==0 пропускает деление при TPS0;при presence!=0 DIV срабатывает даже Speed0. Unknown34C переносится целиком,игровой эффект открыт.23512 новых точечных light-пересборок;6 ложных anchors этапа70 классифицированы.')
    insert_after_title('SOURCES.md','## Этап71: полные Object загрузчики и Light\n\n`OBJECT_LOADING_RU.md`,`object-loading-semantics.json`,`phase71-validation.json`,`phase71-primary-manifest.json`,`phase71-object-loading-emulations.json`,`phase71-canonical-boundary-review.json`;`native_object_loading.py`,`emulate_object_loading.py`,`prepare_object_loading_primary.py`,`build_object_loading_semantics.py`,`audit_object_loading.py`,`finish_phase71.py`.86 successfulnativecases+4expectedDIV0stops;8executed/15codeextents/3data;full894/1022/285comparisons(successonly).23512newlightpatches/84sources11756Objects/4EXEsunchanged;6falseanchorsrejected;gameplay34Copen.')
    s=load('phase70-search-status.json')
    s.update(phase=71,status='complete_Object_loading_light_rules_and_expected_DIV0_contract_published;unknown_gameplay_purposes_open',
        previous_goal_turn_classification='progress:phase70staticindexedreviewpublished;phase71adds86successfulnativeObjectloadcases,4expectedDIV0stops,6canonicalrejectionsand23512newexactlightpatches',
        schema='object-loading-semantics.json;embedded object-property-semantics.phase71_loading',validation='phase71-validation.json',supplement='object-property-semantics.json.phase71_loading',
        findings=['86native success+4expectedDIV0stops;all894raw/1022mid/285defbufferscomparedonsuccess','Unknown34C/pres50→def10616bits;raw341..343ignored;midpaddingretainedunlessinit','LightFlagsbit0ORanyRGB;blackdoesnotclear;fourthcopiedcolorbytenotatrigger','SpeedunsignedDIVbeforezero-check;TPS0faultsevenSpeed0;presence0retainsandavoidsDIV','6remainingnewanchorsrejectedusingnativeboundariesand5-targetswitchrecovery','15codeextents,8executednativefunctions,3data;23512newlightspanpatches/84sources11756Objects/4EXEsunchanged'],
        scope='Fullnativeinitializer/converter/loadersatnullvisualprovider;audioexplicitcapture/tokenadapter. ExpectedfaultscaptureDIVPC/erroronly. Staticparser/boundarycontextsseparate;nogameplay/renderer/fullworldinsertion.',
        evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase71-object-loading-emulations.json','phase71-primary-manifest.json','phase71-canonical-boundary-review.json','phase71-validation.json']],
        next_action='Do not repeatcompleted86Objectsuccess/4fault/23512lightpatches,17oldindexedrootsoroldoffsetscans. Prioritizeotherremainingunknownactualconsumers:KeeperC8,CreatureAbilityJump/PickUpPriority,Terrain198224,AP0408,Spellalternatives,WanderRadius,Traps204/Doors236/Objects34Cviaas-yetunverifiedindexed/indirectpaths. Fullvisual-resourceandfile/worldassemblypathsremainlesstested;expandonlywhenclarifyingactualformatconstraintsorunknowns. NativeDIV0doesnotbyitselfproveordinaryGUIpermitsTPS0. Preserveallrawunknowns.')
    (ROOT/'phase71-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack=load('editor-format-package.json');assert len(pack['components'])==67
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==pack[key]
    print('Phase71published:86success+4DIV0/23512patches;67components;goalremainsactive')


if __name__=='__main__':main()
