"""Publish scoped Wander progress while retaining all unresolved original goals."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(name,text):
    p=ROOT/name;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')

def main():
    v=load('phase75-validation.json');assert v['native_compositions']==18
    insert('INTEGRATION_RU.md','Актуальный этап75: `WANDER_STATE_RU.md` / `placed-creature-semantics.json.phase75_wander_state`.18 полных native DX480750 композиций с реальными clock/PRNG/move requester/nav check. Отдельный contextAF ограничивает кандидатов в клетках;замена placementBYTE418 0/255 не меняет наблюдения в проверенном пути. Это не назначение единиц для rawWanderRadius.52world208 query/42 отклонённых native movement requests;9 полных PE (5 исполнялись),3 блока данных. Новый корпус109Things:6579 отдельных C6/C7 и5768 занятых C9 слотов имеютWander0;6744 пустых тоже0.24DX/23old exposure leads не доказывают globalnonuse.4EXE неизменны,67 компонентов,новых raw-патчей0.')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8').replace('Актуальный этап74:','Предыдущий этап74:',1);p.write_text(s,encoding='utf8')
    insert('PLACED_CREATURES_RU.md','Продолжение этап75 — `WANDER_STATE_RU.md`:18 native DX сценариев отделяют BYTE418 WanderRadius от distance-limit DWORD StateUtilsAF. Для последнего проверена клеточная метрика;единицы rawWanderRadius всё ещё неизвестны. Расширенный корпус включает5768 занятых участниковC9:у всехWander0. Успешное движение и полный state dispatch не проверялись.')
    insert('CREATURE_VERSION_LAYOUT_RU.md','Продолжение этап75 — `WANDER_STATE_RU.md`:новый wide-overlap/address-exposure поиск24DX/23old кандидата не присваивает WanderRadius игрового назначения.18 новых native сценариев алгоритма480750 относятся только к DX;контекстный AF не является автоматически byte418. Исторический профиль старого EXE и raw offsets не меняются.')
    insert('HERO_PARTIES_RU.md','Дополнение этап75 — `WANDER_STATE_RU.md`:всего782 реальныхC9(payload555),5768 занятых слотов и6744 пустых. ByteWanderRadius29(decimal,1Dhex) равен0 во всех слотах. Это новый подсчёт корпуса,не доказательство игрового эффекта и не разрешение очищать остальные байты пустых слотов.')
    insert('SOURCES.md','## Этап75: WanderRadius и отдельный StateUtils limit\n\n`WANDER_STATE_RU.md`,`wander-state-semantics.json`,`phase75-validation.json`,`phase75-primary-manifest.json`,`phase75-wander-state-emulations.json`,`phase75-wander-corpus.json`,`phase75-wander-exposure.json`;`native_wander_state.py`,`emulate_wander_state.py`,`scan_wander_radius_exposure.py`,`survey_wander_radius.py`,`prepare_wander_state_primary.py`,`build_wander_state.py`,`audit_wander_state.py`,`finish_phase75.py`.18 полныхDX композиций/5 исполнявшихся native функций;9 полныхPE/3 data. Пара placement0/255 не меняет проверенные наблюдения;единицы placementbyte не установлены.109Things/4EXE неизменны;67 компонентов.')
    s=load('phase74-search-status.json')
    s.update(phase=75,status='native_DX_wandering_candidate_limit_separated_from_placement_WanderRadius;global_placement_effect_open',
        previous_goal_turn_classification='progress:phase74published44Terraincachecompositions/90ordinalqueries/new561rootsearches;phase75addsnewWanderbranchandC9corpus',
        schema='wander-state-semantics.json;embedded placed-creature-semantics.phase75_wander_state',validation='phase75-validation.json',supplement='placed-creature-semantics.json.phase75_wander_state',
        findings=['18nativeDX480750compositions;realclock,PRNG,471890,4B35C0;52world208queries/42nav-rejectedrequests',
          'ContextAF cell-distance limit is separate fromplacement418;pairedplacement0/255leavesobservablesunchangedinthisscope',
          'OtherstatepathsreusecontextAFforXcoordinate;no globalRadiusrename',
          'Newwide-overlap/addressinventory24DX/23old candidates remainsuntyped;notglobalnonuse',
          '109Thingsfiles:6579standaloneC6/C7,5768occupiedC9,and6744emptyslotsallWander0',
          '9wholePEfunctions(5executed),3datablocks;4EXEsunchanged;67components;0newrawpatches'],
        scope='DirectDX480750helperwithprebuiltmemory/world208fixture andblockednavigation. Notfullstatemachine dispatch orsuccessfulmovement. Oldalgorithmnotemulated;globalplacementbytepurpose/unitsremainopen.',
        evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase75-wander-state-emulations.json','phase75-primary-manifest.json','phase75-wander-corpus.json','phase75-wander-exposure.json','phase75-validation.json']],
        next_action='Do not repeat18Wandercompositions/109filesurvey/exposureinventoryorcompletedTerrain/Creature/Keeper/Objectpasses. IfcontinuingWander,traceactualstateentry/contextAFpopulationorfullyexecute4808E0→480A00newproviders;do not equateplacementbytewithcontextlimit. OtherprioritiesAP04/08,KeeperC8specialproviders,CreatureSpellalternatives,typedTrap204/Door236. Preserveunknownbytes/orderandoriginalfullobjective;goalactive.')
    (ROOT/'phase75-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack=load('editor-format-package.json');assert len(pack['components'])==67
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==pack[key]
    print('Phase75published:18nativeWandercompositions/109Thingscorpus;67components;fullgoalactive')

if __name__=='__main__':main()
