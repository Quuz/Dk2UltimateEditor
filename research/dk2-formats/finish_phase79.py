"""Publish phase79 evidence and preserve all original unresolved requirements."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(name,text):
    p=ROOT/name;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')
def main():
    v=load('phase79-validation.json');assert v['native_request_compositions']==52
    insert('INTEGRATION_RU.md','Актуальный этап79: `CREATURE_SHOT_REQUEST_RU.md` / `creature-subrecords.json.phase79_cast_request`.52 native запроса4982B0/4984D0→реальнаяfactory4B7CD0 с остановкой gate0 до allocation. Подтверждены primaryShot runtime44/rawE7, реальная exists/get цепочка,strict unsigned cooldown из Creature-slotDelay,PlayAnimationbit0→actor3EA1000 и low32IMUL→SAR16 origin. Третий498870 только static.10wholePE (8исполнялись)/3vslots;2961 exactpatches/164source roles/4EXE unchanged;67компонентов. Успешный spawn/дальнейшая trajectory/E8E9gameplay остаются открытыми.')
    p=ROOT/'INTEGRATION_RU.md';p.write_text(p.read_text(encoding='utf8').replace('Актуальный этап78:','Предыдущий этап78:',1),encoding='utf8')
    insert('CREATURE_SUBRECORDS.md','Дополнение этап79 — `CREATURE_SHOT_REQUEST_RU.md`:runtimeслот теперь прослежен до настоящейShotfactory в52 native pre-allocation опытах. PlayAnimation используетbit0,delay сравниваетсяunsigned соstrict tick>u32(last+delay),XYZ rotate имеет32-bit productwrap передSAR16. Это не полныйуспешныйвыстрел и не новаяполитикакомпактизации3слотов. Все неизвестныебайты сохраняются.')
    insert('SPELL_ALTERNATIVES_RU.md','Дополнение этап79 — `CREATURE_SHOT_REQUEST_RU.md`:два настоящих Creature-shot wrappers передают primaryShot rawE7/runtime44 в nativefactory. Четыре парыE8/E9 дают одинаковый pre-allocation запрос при одинаковых перенесённых полях. Gate0 останавливает фабрику доallocation;успешные альтернативные/trajectory ветви ещё не проверены. Глобальное неиспользованиеE8/E9 не доказано.')
    insert('SOURCES.md','## Этап79: CreatureSpell pre-allocation Shot request\n\n`CREATURE_SHOT_REQUEST_RU.md`,`creature-shot-request-semantics.json`,`phase79-validation.json`,`phase79-primary-manifest.json`,`phase79-creature-shot-request-emulations.json`;`native_creature_shot_request.py`,`emulate_creature_shot_request.py`,`prepare_creature_shot_request_primary.py`,`build_creature_shot_request.py`,`audit_creature_shot_request.py`,`finish_phase79.py`.52 native/8executed/2static wholePE;3vslots. Factorygate0/explicitcoefficientdata/initializedfallbackguard scopes retained.2961exactpatches,4EXEunchanged,67components. No newlysuccessfulallocation,noE8E9meaningassigned.')
    status=load('phase78-search-status.json');status.update(phase=79,status='native_CreatureSpell_preallocation_request_published;successful_spawn_and_alternative_gameplay_open',
      previous_goal_turn_classification='progress:phase78publishednativeAIpredicateproof;phase79tracesrealCreaturecastwrappersintoactualfactorywithoutcodepatches',
      schema='creature-shot-request-semantics.json;embedded creature-subrecords.phase79_cast_request',validation='phase79-validation.json',supplement='creature-subrecords.json.phase79_cast_request',
      findings=['52native4982B0/4984D0requests;8realfunctions;nativefactorygate0stopsbeforeallocation',
        'PrimaryShotrawE7/runtime44reachesfactory;actualCreatureSpellandShotexists/gethelpers;0stillattemptedintwopaths',
        'RuntimegetterfullDWORDindex/sharedfallback;unsignedstrictcooldownfromslotDelay;PlayAnimationbit0and32bitrotationproductwrap',
        'Threealternativepairsplusbaselineunchangedpreallocationrequest;successpathandglobalE8E9useunresolved',
        '10wholePE/3vslots;2961exactpatches/164sourceRoles;4EXEsunchanged;67components'],
      evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase79-creature-shot-request-emulations.json','phase79-primary-manifest.json','phase79-validation.json','creature-shot-request-semantics.json']],
      scope='NewrealCreaturecastpreallocationrequestandfieldconsumers. No newlysuccessfulShotallocation,fullAIdispatch,postsuccessmotionorAlternativeE8/E9globalnonuseclaim.',
      next_action='Do not repeat52preallocationrequestsor2961patches. Extendactualsuccesspathusingnativepoolallocator/baseconstructorswithprepareddataandvtableproviderswithoutoverwritingoriginalcode,ortraceanotherremainingunknownconsumer. 498870formatsbufferthrough634DB0(printf-style),notlog-sink;itscompletepathnotrun. Raw/runtimeCreature-slotcompactionstillnotnewlyproved. Keepalloriginalfamiliesandunresolveds;goalactive.')
    (ROOT/'phase79-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8');print('Phase79published;fulloriginalgoalactive;successfulcreationandremainingunknownsopen')
if __name__=='__main__':main()
