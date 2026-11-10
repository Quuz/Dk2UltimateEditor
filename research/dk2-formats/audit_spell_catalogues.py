"""Read-only native spell audit for three explicit definition scopes."""
from collections import Counter
from pathlib import Path
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA

ROOT=Path(__file__).resolve().parent

def scope(label,paths):
    counts=Counter();selectors={};unused=[];tails=[];keepers=[];inputs=[];rejected=[]
    for path in paths:
        data=path.read_bytes();inputs.append(dict(path=str(path),sha256=hashlib.sha256(data).hexdigest()))
        for section in fmt.parse(data):
            if section.signature not in (0x96,0xA0):continue
            family=fmt.FAMILIES[section.signature]
            for raw in section.records:
                f=fmt.decode_fields(raw,fmt.LAYOUTS[family]['fields'])
                view=fmt.decode_spell_engine_fields(raw,family,simulation_tps=20)
                id_field='CreatureSpellId' if family=='CreatureSpells' else 'KeeperSpellId'
                ident=dict(family=family,id=f[id_field],name=f['Name']);counts[family]+=1
                if family=='CreatureSpells':
                    value=view['AlternativeEligibility']['raw']
                    selectors[value]=selectors.get(value,0)+1
                    if value>=6:rejected.append(dict(**ident,field='AlternativeShot',value=value))
                    if raw[0xE8] or raw[0xE9]:unused.append(dict(**ident,alternative_shot_id=raw[0xE8],alternative_room_id=raw[0xE9]))
                    if any(raw[0xEF:]):tails.append(dict(**ident,tail_hex=raw[0xEF:].hex()))
                else:
                    for key,limit in [('CastRule',6),('TargetRule',7)]:
                        if view[key]['raw']>=limit:rejected.append(dict(**ident,field=key,value=view[key]['raw']))
                    keepers.append(dict(**ident,ResearchTime=f['ResearchTime'],BonusRTime=f['BonusRTime'],
                        explicit_example_tps=20,research_runtime_word=view['ResearchTimeLoader']['runtime_value'],
                        bonus_research_runtime_word=view['BonusResearchTimeLoader']['runtime_value'],
                        shot_tuples=view['ShotTuples'],cast_rule=view['CastRule'],target_rule=view['TargetRule']))
    return dict(scope=label,inputs=inputs,counts=dict(counts),creature_alternative_counts=selectors,
        selector_rejections=rejected,nonzero_untransferred_alternative_references=unused,
        nonzero_unparsed_creature_tails=tails,keeper_spell_views=keepers)

def main():
    scopes=[scope('standalone editor catalogues',[DATA/'CreatureSpells.kwd',DATA/'KeeperSpells.kwd']),
        scope('adjacent AutoSaveGlobals.kld',[DATA/'AutoSaveGlobals.kld']),
        scope('referenced maps/kop4Globals.kld',[DATA/'maps/kop4Globals.kld'])]
    globals_path=DATA/'GlobalVariables.kwd';globals_data=globals_path.read_bytes();research=[]
    for section in fmt.parse(globals_data):
        if section.signature!=0xE6:continue
        for raw in section.records:
            tag,category,ident,points=struct.unpack('<4I',raw)
            if tag==17:research.append(dict(category=category,definition_id=ident,research_points_raw=points))
    result=dict(scope='Static storage/predicate evidence; TPS20 is an explicit arithmetic example, not an inferred live setting. Scopes overlap.',
        scopes=scopes,research_order_example=dict(path=str(globals_path),sha256=hashlib.sha256(globals_data).hexdigest(),entries=research))
    (ROOT/'spell-catalogue-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    for item in scopes:
        print(item['scope'],item['counts'],'selector rejections',len(item['selector_rejections']),
            'untransferred pairs',len(item['nonzero_untransferred_alternative_references']),
            'nonzero tails',len(item['nonzero_unparsed_creature_tails']))
    print('GlobalVariables ResearchOrder entries:',len(research))

if __name__=='__main__':main()
