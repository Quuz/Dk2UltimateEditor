"""Audit explicit AutoSave inputs against the bundle named by its descriptor."""
from pathlib import Path
from collections import Counter
import json
import dk2_formats as fmt
from inventory import DATA
from validate_references import ID_FIELDS,scalar

ROOT=Path(__file__).resolve().parent

def main():
    bundle=fmt.inspect(DATA/'maps/kop4Globals.kld')
    catalogues={s['family']:s for s in bundle['sections']}
    ids={family:{scalar(r['fields'][ID_FIELDS[family]]) for r in s['records']} for family,s in catalogues.items()}
    diagnostics=[];checks=Counter();nulls=Counter()
    def check(source,record,field,family,value):
        if not value:nulls[source]+=1;return
        checks[source]+=1
        if value not in ids[family]:diagnostics.append(dict(source=source,record=record,field=field,target_family=family,target_id=value))
    level=fmt.inspect(DATA/'AutoSave.kwd')['sections'][0]
    refs=[];game_root=DATA.parent.parent
    for ref in level['references']:
        target=game_root/Path(ref['path'])
        refs.append(dict(**ref,resolved=str(target.resolve()),exists=target.is_file()))
    for i,c in enumerate(level['creature_list']):check('descriptor_creature_list',i,'CreatureId','Creatures',c['creature_id_used_by_game'])
    things=fmt.inspect(DATA/'AutoSaveThings.kld')['sections'][0]
    direct={'CreatureId':'Creatures','ObjectId':'Objects','DoorId':'Doors','TrapId':'Traps','RoomType':'Rooms'}
    for r in things['records']:
        for name,value in r['fields'].items():
            dest=direct.get(name.rsplit('.',1)[-1])
            if name.endswith('.effectId'):dest='Effects'
            if dest:check('adjacent_AutoSaveThings',r['index'],name,dest,scalar(value))
    map_section=fmt.parse((DATA/'AutoSaveMap.kld').read_bytes())[0]
    for terrain,count in Counter(map_section.data[::4]).items():
        check('adjacent_AutoSaveMap_distinct_terrain',None,'TerrainId','Terrain',terrain)
    variables=fmt.parse((DATA/'GlobalVariables.kwd').read_bytes())
    for s in variables:
        if s.signature!=0xE6:continue
        for i,b in enumerate(s.records):
            tag,a,c,d=(fmt.u32(b,j) for j in (0,4,8,12))
            if tag==1:check('GlobalVariables',i,'CreaturePool.CreatureId','Creatures',a)
            elif tag==2:
                dest={1:'Rooms',2:'Creatures',3:'Traps',4:'Doors',5:'KeeperSpells'}.get(a&65535)
                if dest:check('GlobalVariables',i,'Availability.TypeId',dest,c)
            elif tag==17:
                dest={1:'Rooms',2:'Doors',3:'Traps',4:'KeeperSpells'}.get(a)
                if dest:check('GlobalVariables',i,'ResearchOrder.DefinitionId',dest,c)
            elif tag==77:check('GlobalVariables',i,'ModifyCreatureStat.CreatureId','Creatures',a&255)
    for r in catalogues['Shots']['records']:
        f=r['fields'];process=scalar(f['ProcessType']);dest={1:'Creatures',2:'Objects',18:'Creatures',36:'Creatures'}.get(process)
        if dest:check('kop4Globals_process_Data1',scalar(f['ShotId']),'Data1 low8',dest,scalar(f['Data1'])&255)
    result=dict(bundle_source=bundle['source'],bundle_sha256=bundle['sha256'],descriptor_references=refs,
        checked_nonzero_by_source=dict(checks),skipped_zero_by_source=dict(nulls),diagnostics=diagnostics,
        note='Adjacent AutoSaveThings/Map are explicitly named comparison inputs: their descriptor paths under Maps do not exist. This does not silently resolve or repair those paths, nor claim the complete map loads. Player/AP/trigger/resource references are separate namespaces.')
    (ROOT/'autosave-link-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    lines=['# Согласование AutoSave с указанными определениями','',result['note'],'',
        '| Источник проверки | Проверено ненулевых |','|---|---:|']
    for source,count in checks.items():lines.append(f'| {source} | {count} |')
    lines+=['',f'Диагностик отсутствующих ID: {len(diagnostics)}.','',
        '| Путь из descriptor | Существует ровно по этому пути |','|---|---|']
    for ref in refs:lines.append(f"| {ref['path']} | {'да' if ref['exists'] else 'нет'} |")
    lines+=['','Process Data1 проверен как ID только для процессов создания существа/объекта/воскрешения. Data2 Jack является live tag ID и не проверяется по справочнику Creatures. У нулевых значений и других контекстных полей нет автоматического переназначения. Полные диагностики и пути: `autosave-link-audit.json`.','']
    (ROOT/'AUTOSAVE_LINKS.md').write_text('\n'.join(lines),encoding='utf8')
    print('AutoSave link checks:',dict(checks),'diagnostics:',len(diagnostics),'existing paths:',sum(r['exists'] for r in refs),'of',len(refs))

if __name__=='__main__':main()
