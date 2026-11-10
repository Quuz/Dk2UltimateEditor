"""Index independently verified RC3 overlap and its lossless physical API."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(n,text):
    p=ROOT/n;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')
def main():
    v=load('phase87-validation.json');assert v['new_native_invocations']==13 and v['new_api_exact_container_patch_checks']==1722 and not v['goal_achieved']
    insert('INTEGRATION_RU.md','Актуальный этап 87: `RC3_HATE_JOBS_RU.md` / `rc3-compatibility-semantics.json.phase87_job_overlap`. Пересечение Hate Jobs3/4 сalternative0 независимо подтверждено ив RC3:7парныхnative-конвертаций и6полныхwriter. Полные6457байт промежуточныхзаписей сверены;четыре getter/setter модели совпадают сPro побайтово. RC3writer также теряет независимые HateJobs3/4 и оставляетraw14E1/14E5 заполнением стека. Различия5449/1545 и5537/1599 сохраняются. Добавлен `native_creature_jobs.py`: `view(raw,exactSHA256)` и `patch_field(raw,physical_field,unsigned_value)` длясемифизическихполей,безнеявныхисправлений.1722новыеAPI-проверки/82файла/3734записи,246profileviews/6refusals,16прежнихnative-сценариевсопоставленыбезповтора;10RC3wholePE/4EXEсверены,67компонентов. При13новыхзапускахкодProнепереносился;только6writer-clockhelpers замененывEmulatorHelper. ПолныйAI/GUIнепроверен;остальные существенныенеизвестные сохраняются.')
    p=ROOT/'INTEGRATION_RU.md';p.write_text(p.read_text(encoding='utf8').replace('Актуальный этап 86:','Предыдущий этап 86:',1),encoding='utf8')
    insert('SOURCES.md','## Этап87: независимая RC3-проверка HateJobs и API точечнойправки\n\n`RC3_HATE_JOBS_RU.md`,`rc3-job-overlap-semantics.json`,`phase87-primary-manifest.json`,`phase87-validation.json`;`emulate_rc3_hate_jobs.py`,`native_creature_jobs.py`,`prepare_rc3_jobs_primary.py`,`build_rc3_jobs.py`,`audit_rc3_jobs.py`,`finish_phase87.py`.13новыхRC3nativeкомпозиций/14init+converter/6полныхwriter,10wholePE. Пересечениеmid126C/1270 иwriterrawE7C/E80 подтверждено;4полныхмодельныхфункции совпадают сPro.1722проверки новойAPIреализации,82источника/3734физическиезаписи,246views,6отказов;16прежнихnativeDX/Proслучаевсопоставленыбезповторногоисполнения.6clock4B0B30 adapters только вEmulatorHelper;игровыеданные/EXEнеизменны. Предыдущие166RC3/22phase86случаевнеучтеныкакновые. СтарыйRC3пакет сохранён целиком плюс новый supplement;неглобальноесравнениеверсий.')
    files=['rc3-job-overlap-semantics.json','phase87-validation.json','phase87-primary-manifest.json','phase87-rc3-job-emulations.json','emulate_rc3_hate_jobs.py','native_creature_jobs.py','prepare_rc3_jobs_primary.py','build_rc3_jobs.py','audit_rc3_jobs.py','RC3_HATE_JOBS_RU.md']
    status=dict(phase=87,date='2026-10-28',status='RC3 HateJobs/alternative0 overlap independentlynativevalidated;exact-profilephysicalpatchAPIpublished',
      previous_goal_turn_classification='Progress:phase86publishednewDX/ProaliasandrealPrince-mapmembershipproof. ThisturnaddsindependentRC3evidenceandlosslesshelper,notrepetitionofpriorruns.',
      findings=['13newnativeRC3cases:7pairedinit/conversions compareall6457MIDbytes;6full5449writerscheckselectedrawbands/header/model/count/flags.',
        'RC3HateJobs3/4 overwritebyalternative0 matchesDX/Prolocalbehavior;4completeRC3/Promodelgetters/setters byteidentical. Notuniversalprofileequivalence.',
        'native_creature_jobs.py keeps7physicalfields/unknownbits/length/extension,exposes3exactSHAprofilesandshadowingdiagnostic;noautomaticrepair.',
        '1722newAPIexact-containerchecks/82sources/3734records;246views/6refusals;16oldDX/Procasesmatchedand7newRC3casesmatched,oldnativecasesnotrerun.',
        '10wholeRC3PEspans and4originalEXEhashes verified;only6explicitclockhelperadapters inwriteremulation. NoactualeditorGUI/fullAI/sourcewrites.',
        'PriorRC3schema preservedwithoutnew-supplementhash;newphase87embedded,phase85/86intact,67components.'],
      evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in files],significant_unknowns_remain=True,goal_achieved=False,
      next_action='RC3job-overlapverificationandAPIcomplete;donotrepeat13cases/1722newAPIchecks orpriorphase86/85nativecases. Returntounresolvedgameconsumers Terrain198/224,Traps204,Objects34C,Doors236,AP04/08,ElementB1/B2 orCreatureSpellE8/E9. Prefernewreceiver/dataflowevidenceovernewcopiesofsameformatchecks. Existingboundedaliasnegativepasses do notproveglobalnonuse. GhidraGUIprojectnowincludesDX,ProandRC3;reuseactualactiveproject,donotrestartforblankdiscovery. Full15familygoalremainsincomplete.',
      limitations=load('rc3-job-overlap-semantics.json')['limitations'])
    (ROOT/'phase87-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8');print('Phase87published;fullgoalactiveandincomplete.')
if __name__=='__main__':main()
