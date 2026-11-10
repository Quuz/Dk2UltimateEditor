"""Conservative, read-only catalogue link audit; unresolved links are diagnostics."""
from collections import Counter
from pathlib import Path
import json
import dk2_formats as fmt
from inventory import DATA

ROOT=Path(__file__).resolve().parent
ID_FIELDS={'Terrain':'TerrainId','Rooms':'RoomId','Traps':'TrapId','Doors':'DoorId',
 'KeeperSpells':'KeeperSpellId','CreatureSpells':'CreatureSpellId','Creatures':'CreatureId',
 'Objects':'ObjectId','Effects':'EffectId','EffectElements':'EffectElementId','Shots':'ShotId'}

def scalar(value):
    return value['raw'] if isinstance(value,dict) and 'raw' in value else value

def target(family,name):
    if name in ('TerrainId','MaxHealthTypeTerrainId','DestroyedTypeTerrainId'):return 'Terrain'
    if name=='CreatedCreatureId' or name=='CloneCreatureId':return 'Creatures'
    if name.endswith('.RoomId') or name=='AlternativeRoomId':return 'Rooms'
    if name.endswith('.CreatureSpellId'):return 'CreatureSpells'
    if name.endswith('.objectId') or name in ('ManufCrateObjectId','KeyObjectId','LairObjectId'):return 'Objects'
    if name in ('ShotTypeId','BonusShotTypeId','AlternativeShotId') or family=='Shots' and name.endswith('ShotId') and name!='ShotId':return 'Shots'
    if name.endswith('EffectId') and name not in ('EffectId','NextEffectId'):
        if family!='EffectElements':return 'Effects'
    if name.endswith('.effectId'):return 'Effects'
    if name=='NextEffectId' and family in ('Effects','EffectElements'):return 'Effects'
    if family=='EffectElements' and name in ('DeathElementId','HitSolidElementId','HitLavaElementId','HitWaterElementId'):return 'EffectElements'

def scope(sections,label):
    catalogs={s['family']:s for s in sections if s['family'] in ID_FIELDS}
    indices={};issues=[];checks=0
    for family,s in catalogs.items():
        counts=Counter(scalar(r['fields'][ID_FIELDS[family]]) for r in s['records'])
        indices[family]=set(counts)
        for ident,count in counts.items():
            if count>1:issues.append(dict(kind='duplicate_id',family=family,id=ident,count=count))
    for family,s in catalogs.items():
        for r in s['records']:
            source_id=scalar(r['fields'][ID_FIELDS[family]])
            for name,val in r['fields'].items():
                dest=target(family,name)
                if family=='Effects' and name.startswith('GenerateIds['):
                    # The digit renderer consumes ten derived consecutive IDs,
                    # not the eight physical fields. Check them below instead.
                    if scalar(r['fields']['GenerationType'])==14:continue
                    flags=scalar(r['fields']['Flags']);mask=flags&0xc0
                    if scalar(r['fields']['GenerationType'])==4:
                        dest='Effects' if flags&0x40 else 'EffectElements'
                    else:
                        dest={0x40:'Effects',0x80:'EffectElements'}.get(mask)
                if dest is None:continue
                value=scalar(val)
                if not isinstance(value,int) or value==0:continue
                checks+=1
                if value not in indices.get(dest,set()):issues.append(dict(kind='unresolved_reference',
                    family=family,source_id=source_id,field=name,target_family=dest,target_id=value))
            if family=='Effects':
                process=scalar(r['fields']['GenerationType'])
                extra=[]
                if process==6:
                    extra.append(('Spray.SecondaryEffectId','Effects',scalar(r['fields']['OuterOriginRange'])))
                if process==14:
                    base=scalar(r['fields']['GenerateIds[0].id'])
                    extra.extend((f'DigitElementId[{digit}]','EffectElements',(base+digit)&65535) for digit in range(10))
                for name,dest,value in extra:
                    if value==0:continue
                    checks+=1
                    if value not in indices.get(dest,set()):issues.append(dict(kind='unresolved_reference',
                        family=family,source_id=source_id,field=name,target_family=dest,target_id=value))
    return dict(scope=label,checks=checks,id_counts={k:len(v) for k,v in indices.items()},diagnostics=issues)

def main():
    files=[fmt.inspect(DATA/(family+'.kwd')) for family in ID_FIELDS]
    base=[s for document in files for s in document['sections']]
    snapshot=fmt.inspect(DATA/'AutoSaveGlobals.kld')['sections']
    referenced=fmt.inspect(DATA/'maps/kop4Globals.kld')['sections']
    result=dict(note='Three explicit scopes, not inferred effective AutoSave load order. Zero IDs skipped as null. Only listed field rules audited. Diagnostics do not authorize repairs.',
        scopes=[scope(base,'standalone editor catalogues'),scope(snapshot,'adjacent AutoSaveGlobals.kld snapshot'),scope(referenced,'referenced maps/kop4Globals.kld')])
    (ROOT/'reference-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    lines=['# Проверка ссылок каталогов','',result['note'],'',
      'Автозамена и перенумерация не выполнялись. Связи проверены в трёх отдельных наборах, включая действительно указанный в AutoSave kop4Globals.', '']
    for s in result['scopes']:
        lines += ['## '+s['scope'],'',f"Проверено ненулевых ссылок: {s['checks']}. Диагностик: {len(s['diagnostics'])}.",'',
            '| Семейство | ID записи | Поле | Цель |','|---|---:|---|---|']
        for d in s['diagnostics']:
            lines.append(f"| {d['family']} | {d.get('source_id',d.get('id'))} | {d.get('field',d['kind'])} | {d.get('target_family','duplicate')} {d.get('target_id','')} |")
        lines.append('')
    lines+=['Правила представлены в validate_references.py. Для CUBE_GEN (GenerationType4) GenerateIds разрешаются как Effects при бите0x40, иначе как EffectElements: это подтверждено веткой0x43895D. Для GOLD_COST(type14) проверяются десять derived ElementId=base+digit, а для SPRAY(type6) дополнительно проверяется SecondaryEffectId из raw+DA. Для остальных режимов оставлено прежнее консервативное правило0x40/0x80, неоднозначные комбинации и все внутренние ID числовых диапазонов здесь не проверяются. NextEffectId у EffectElements указывает на Effects, что подтверждено вызовом bridge+94 из0x436430. Графические имена ресурсов, текстовые таблицы, ссылки триггеров и все возможные Data1/Data2 здесь не проверяются. Наличие ссылки не доказывает достижимость или использование записи в игре.', '']
    (ROOT/'REFERENCE_AUDIT.md').write_text('\n'.join(lines),encoding='utf-8')
    for s in result['scopes']:print(s['scope'],s['checks'],'links,',len(s['diagnostics']),'diagnostics')

if __name__=='__main__':main()
