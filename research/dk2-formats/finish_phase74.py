"""Record phase74 progress without closing or shrinking the full research goal."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def insert(name,text):
    p=ROOT/name;s=p.read_text(encoding='utf8');head,rest=s.split('\n',1)
    if text not in s:p.write_text(head+'\n\n'+text+'\n'+rest,encoding='utf8')

def main():
    v=load('phase74-validation.json');assert v['native_complete_compositions']==44
    insert('INTEGRATION_RU.md','Актуальный этап74: `TERRAIN_CACHE_RU.md` / `terrain-lighting-semantics.json.phase74_cache`.44 полные native-композиции двух EXE:18 заполнений глобального кэша и26 выборов определения для света;90 дополнительных ordinal lookup. Перебор пропускает слот0; лишние итерации могут заполнить cache0 указателем слота0. Bool=0 иногда сопровождается записью выходного указателя.11 полных PE-функций (10 исполнялись),6 vslot-диапазонов;561 новая точка поиска/469 контрольных зацепок,1 callee limited. Назначение Terrain198/224 не установлено.82 входных файла/4 EXE неизменны,67 компонентов,новых raw-патчей0.')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8');s=s.replace('Актуальный этап73:','Предыдущий этап73:',1);p.write_text(s,encoding='utf8')
    insert('TERRAIN_LOADING_RU.md','Продолжение этап74 — `TERRAIN_CACHE_RU.md`:44 native-композиции уточняют ordinal enumeration,глобальный кэш и выбор определения для света. Getter возвращает прямой слот;перечисление — отдельная операция,пропускающая слот0. Полный allocator/parser этим этапом не исполнялся. Unknown198/224 остаются открытыми.')
    insert('TERRAIN_LIGHTING_RU.md','Продолжение этап74 — `TERRAIN_CACHE_RU.md`:helper452DC0/old44B910 полностью исполнен в26 случаях;выходной указатель может меняться при bool0. Геометрический vslot21C — явный capture/result адаптер,renderer не запускался.18 native заполнений глобального кэша и90 ordinal lookup;назначение198/224 не установлено.')
    insert('SOURCES.md','## Этап74: Terrain enumeration, глобальный кэш и выбор определения\n\n`TERRAIN_CACHE_RU.md`,`terrain-cache-semantics.json`,`phase74-validation.json`,`phase74-primary-manifest.json`,`phase74-terrain-cache-emulations.json`;`native_terrain_cache.py`,`emulate_terrain_cache.py`,`prepare_terrain_argument_search.py`,`prepare_terrain_cache_search.py`,`prepare_terrain_cache_queries.py`,`prepare_terrain_cache_primary.py`,`build_terrain_cache.py`,`audit_terrain_cache.py`,`finish_phase74.py`.44 native композиции/90 дополнительных ordinal query,11 полных PE/6 vslot-диапазонов;561 новая точка bounded поиска,469 контрольных зацепок,1 limited callee. Unknown198/224 не расшифрованы;82 источника и4 EXE неизменны.')
    s=load('phase73-search-status.json')
    s.update(phase=74,status='native_Terrain_enumeration_global_cache_and_lighting_selection_published;unknown_gameplay_purposes_open',
        previous_goal_turn_classification='no_progress:previous user-facing turn clarified false Traps leads without new authoritative research;currentturn revalidatedphase73 and made newTerrain cache/selection evidence',
        schema='terrain-cache-semantics.json;embedded terrain-lighting-semantics.phase74_cache',validation='phase74-validation.json',supplement='terrain-lighting-semantics.json.phase74_cache',
        findings=['44fullnativecompositions:18cacheinit,26selection;90additionalordinalqueries;10completeexecutedfunctions',
          'Enumeration scansslots1..<capacity andreturnsALID;extra loaded-countiterationssetcache0todirectslot0',
          'Globalworld suppliesenumeratedIDs;argumentworld suppliescount/definitionaddresses;differentworldsareexplicitfixturesonly',
          'Selectioncanwriteoutputonfalse;nativegetter38fallbackverified;geometry21Cisfixture',
          'New561roots/11173states/24callees/469controlleads,1limitedcallee;unknown198/224stillopen',
          '11completePEfunctions/6vslotranges;82sources3306Terrain/4EXEsunchanged;67components;0newrawpatches'],
        scope='Prebuiltruntimeworlds/definitionarrays,nativecacheenumerationandselection. Notnewallocator/parser/rawload/fullrenderer. Unknownsemanticgoalsnotclosed.',
        evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in ['phase74-terrain-cache-emulations.json','phase74-primary-manifest.json','phase74-DX-terrain-argument-leads.json','phase74-DX-terrain-cache-leads.json','phase74-old-terrain-argument-leads.json','phase74-old-terrain-cache-leads.json','phase74-validation.json']],
        next_action='Do not repeat44Terraincompositions/561rootsearches or previousCreature/Keeper/Objectcompletedpasses. Terrainunknownsneed genuinelynewindirect/out-pointee/indexed/provider paths,not anotherdisplacementscan. PrioritizeAP04/08,WanderRadius418,CreatureSpellalternatives,KeeperC8specialproviders,orunresolvedtypedTraps204/Door236consumers. Preserveallrawbytes/order;originalfullobjectiveandactivegoalremain.')
    (ROOT/'phase74-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack=load('editor-format-package.json');assert len(pack['components'])==67
    for key,row in pack['components'].items():
        b=(ROOT/row['file']).read_bytes();assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==pack[key]
    print('Phase74published:44nativeTerraincompositions/90ordinalqueries;67components;fullgoalactive')

if __name__=='__main__':main()
