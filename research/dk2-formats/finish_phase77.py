"""Publish new SightData1 semantics while preserving the full active objective."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(name,text):
    p=ROOT/name;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')

def main():
    v=load('phase77-validation.json');assert v['native_Sight_event_compositions']==30
    insert('INTEGRATION_RU.md','Актуальный этап77: `SIGHT_OF_EVIL_RU.md` / `keeper-cast-semantics.json.phase77_sight_event`.30 native DX Process25→World→Session→Bridge0D→region композиций. Установлен Data1 consumer:полный DWORD включает начальное событие,low16 становится полуразмером области в клеточных координатах с ограничением7 у получателя.0 и65536 неэквивалентны. Проверены state bounds/tick wrap/processedDWORD/звук при полномDWORD40==20. Final visibility/renderer не проверены.8 настоящих функций,11 wholePE/7data;6 C8 вариантов не меняют наблюдения под явными graphics providers,назначениеC8 открыто.656 точечных пересборок/82 Keeper-файла;4 EXE неизменны,67 компонентов. ShotProcess25 notes уточнены.')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8').replace('Актуальный этап76:','Предыдущий этап76:',1);p.write_text(s,encoding='utf8')
    insert('KEEPER_CAST_RU.md','Продолжение этап77 — `SIGHT_OF_EVIL_RU.md`:новая native-цепочка Process25→Bridge0D устанавливает Data1 как полуразмер области в клеточных координатах:ненулевой полныйDWORD включает событие,low16 у получателя ограничивается7. KeeperrawD0/114 достигают этого параметра через ранее доказанный cast path только при реальномProcess25. Нативный receiver/region теперь исполнены;графика/звук — providers. C8 по-прежнему неизвестен.')
    insert('KEEPER_PIPELINE_RU.md','Продолжение этап77 — `SIGHT_OF_EVIL_RU.md`:расширенная цепочка SightProcess25→World→Session→Bridge0D не дала C8 игрового назначения под указанными графическими providers. Установлено отдельное назначение Data1,не C8. Все32 исходных битаC8 сохраняются;полная глобальная неиспользуемость не заявляется.')
    insert('SOURCES.md','## Этап77: Sight-of-Evil receiver и Data1\n\n`SIGHT_OF_EVIL_RU.md`,`sight-event-semantics.json`,`phase77-validation.json`,`phase77-primary-manifest.json`,`phase77-sight-event-emulations.json`;`native_sight_event.py`,`emulate_sight_event.py`,`prepare_sight_primary.py`,`build_sight_event.py`,`audit_sight_event.py`,`finish_phase77.py`.30 native композиций/8 исполнявшихся функций;11 wholePE/7data. Первичный table-pair13→448580,process25 registry initializer4AD1B0;нулевая таблица в PE не заменяется вымышленной статической регистрацией. Data1cellhalfextent=min(low16,7),fullDWORDgate отличает0/65536. Graphics providers/final mask остаются оговорёнными.656 exact patches/82Keeperfiles/4EXE unchanged;C8unknown сохраняется.')
    s=load('phase76-search-status.json')
    s.update(phase=77,status='native_Sight_event_receiver_Data1_cell_halfextent_published;KeeperC8_gameplay_still_open',
        previous_goal_turn_classification='progress:phase76publishedAPlookupandstacklifetimepositivecontrol;phase77tracesnewindirecteventconsumerandaddsactualData1semantics',
        schema='sight-event-semantics.json;embedded keeper-cast-semantics.phase77_sight_event;ShotProcess25reference',validation='phase77-validation.json',supplement='keeper-cast-semantics.json.phase77_sight_event',
        findings=['30nativeDXProcess25→World→Session→Bridge0D→regioncompositions;8realexecutedfunctions',
          'Data1fullDWORD!=0enablesevent;low16iscell-coordinatehalfextent,capped7byreceiver448580;0and65536differ',
          'Nativebridgebounds/tickwrap/redrawevents/processedDWORD169/fullDWORD40soundgateverified;actualrenderer/providersremainfixtures',
          'C8sixpatternsunchangedobservablesunderproviders;noC8meaningassigned',
          '11wholePE/7data;processregistrywritesprovedstatically;656exactcontainerpatches/82Keeperfiles1066records;4EXEsunchanged;67components'],
        scope='NewnativeShotprocess-through-clientBridgeeventreceiver,notnewrawload/fullcast/renderer. MeaningData1onlyforactualProcess25. GlobalC8/otherunknownsnotclosed.',
        evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase77-sight-event-emulations.json','phase77-primary-manifest.json','phase77-validation.json','sight-event-semantics.json']],
        next_action='Do not repeat30Sightcompositionsor656patches. KeeperC8needsnewactualproviders/othernativepaths;do not relabelitfromData1region. CandidatefuturepathsincludeactualEngineeffectparameter/globaldefinitions,castspecialvalidators,orCreatureSpellalternativefields. Otheroriginalunknownsremain. Preserveallrawbytes/orderandfulloriginalscope;goalactive.')
    (ROOT/'phase77-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack=load('editor-format-package.json');assert len(pack['components'])==67
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==pack[key]
    print('Phase77published:SightData1nativeconsumer/30compositions/656patches;67components;fullgoalactive')

if __name__=='__main__':main()
