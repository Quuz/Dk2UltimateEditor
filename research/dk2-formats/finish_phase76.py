"""Record AP lookup/search progress without closing any unresolved semantics."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(name,text):
    p=ROOT/name;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')

def main():
    v=load('phase76-validation.json');assert v['native_compositions']==36
    insert('INTEGRATION_RU.md','Актуальный этап76: `AP_LOOKUP_RU.md` / `action-point-semantics.json.phase76_lookup_review`.36 native-сценариев:32 AP getter двух EXE и4 DX getter→PRNG→maplookup композиции. ID0/255 ищутся;первое совпадение linked-list выигрывает,flags04/08 lookup не читает. Исправлено ограничение поиска:сохранение локального AP-spill через проверенные CALL восстановило известный Patrol40 consumer47205E. Новый8-root проход/351 состояние;расширение2623root/135428state/614callee имеет8 root и28 callee limits,новый адресFlags только47205E. Gameplay04/08 остаётся неизвестным.5 wholePE (4 исполнялись),3vslot ranges;106AP inputs/4EXE hashchecked,AutoSave snapshot,67 компонентов,новых raw-патчей0.')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8').replace('Актуальный этап75:','Предыдущий этап75:',1);p.write_text(s,encoding='utf8')
    insert('ACTION_POINTS_RU.md','Этап76 — `AP_LOOKUP_RU.md`:36 native-сценариев уточняют AP getter и сохранение stack spill через PRNG/maplookup. Первое совпадение ID выигрывает;ID0/255 допускаются этим getter. Старый поиск терял локальные pointers на CALL и пропускал известный Patrol40. Новый поиск восстанавливает47205E;назначения масок04/08 по-прежнему открыты. Не путать маски с raw payload offsets.')
    insert('RC3_ACTION_POINTS_RU.md','Продолжение этап76 — `AP_LOOKUP_RU.md`:новый searchreview обнаружил потерю stack-spill aliases на CALL и восстановил известное чтение40 в патруле. Исследованы getter DX/old и stack contracts,но HeroGate08 не получает нового игрового назначения. Название RC3 и UI-проверки этап51 сохраняют свой прежний охват.')
    insert('SOURCES.md','## Этап76: AP linked lookup и lifetime указателя\n\n`AP_LOOKUP_RU.md`,`ap-lookup-review.json`,`phase76-validation.json`,`phase76-primary-manifest.json`,`phase76-ap-lookup-emulations.json`,`phase76-ap-spill-leads.json`,`phase76-ap-tag-spill-leads.json`;`native_ap_lookup.py`,`emulate_ap_lookup.py`,`prepare_ap_lookup_primary.py`,`prepare_ap_spill_search.py`,`prepare_ap_tag_spill_search.py`,`build_ap_lookup_review.py`,`audit_ap_lookup_review.py`,`finish_phase76.py`.36nativecases/4executedfunctions/5wholePE/3vslotranges. Поиск сохраняет локальные aliases только через явно проверенные ABI;восстановлен47205E/Patrol40. Старые negative passes оставляют stack-lifetime blindspot;новая политика не является универсальным контрактом всех CALL. Расширение2623roots имеет8root/28callee limits. Flags04/08 gameplay open;106AP inputs учитывают snapshot AutoSave,4EXE unchanged.')
    s=load('phase75-search-status.json')
    s.update(phase=76,status='native_AP_lookup_and_known_call_stack_lifetime_positive_control_published;AP04_08_gameplay_open',
        previous_goal_turn_classification='progress:phase75published18nativeWandercompositions/corpus;phase76findsnewsearchblindspot,recoversknownAPcontrolandverifiesrealgetters/contracts',
        schema='ap-lookup-review.json;embedded action-point-semantics.phase76_lookup_review',validation='phase76-validation.json',supplement='action-point-semantics.json.phase76_lookup_review',
        findings=['36native:32APgetters(16perEXE),4DXgetterPRNGmaplookupcallercompositions',
          'APlookupusesargumentlow8/firstlinkedIDmatch;0/255accepted;flags/kind/ownerunread;tag0pointerfixturesnotnormalgamereachability',
          'EarliersearchclearedallstackaliasesonCALLandmissedknownPatrol40;explicitknown-callcontractspreserveAPspillandrecover47205E',
          'New8root/351statepassandextended2623root/135428states/614callees;8root/28calleelimits;onlynewflagsreadaddress47205E;04/08stillunknown',
          '5wholePEfunctions(4executed)/3vslots;106APinputs3501recordswithAutoSavesnapshot,4EXEsunchanged;67components;0newrawpatches'],
        scope='PrebuiltruntimeAPtagtables/nativegetters;postreturncallercontractsnotfullPatrolorworldsimulation. Typehints/pointeeescape/unknownCALL/indirect/indexblindspotsremain;globalunknownflagsemanticsnotclosed.',
        evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase76-ap-lookup-emulations.json','phase76-primary-manifest.json','phase76-ap-spill-leads.json','phase76-ap-tag-spill-leads.json','phase76-validation.json']],
        next_action='Do not repeat36APlookupcompositionsor8/2623rootpasses. Reuseknown-callspillpolicyonlywhereactualABI/pointeecontractsareproved;historicalnegativeCreature/Object/Terrain/Keeperpassesremainbounded. AP04/08neednewtypedindexed/indirect/escapeorotherbuildconsumerpaths;otherprioritiesKeeperC8specialproviders,CreatureSpellalternatives,Wanderstateentry. Preserveallrawbytes/orderandoriginalfullobjective;goalactive.')
    (ROOT/'phase76-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack=load('editor-format-package.json');assert len(pack['components'])==67
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==pack[key]
    print('Phase76published:36nativeAP/compositions,stack-lifetimecontrolrecovered;67components;fullgoalactive')

if __name__=='__main__':main()
