"""Apply native effect views to three explicit, unchanged catalogue scopes."""
from collections import Counter
from pathlib import Path
import hashlib, json
import dk2_formats as fmt
from inventory import DATA

ROOT=Path(__file__).resolve().parent

def scope(label, paths):
    modes=Counter();records=Counter();holes=[];narrowed=[];next_links=[];collision_priority=[];invalid=[];special=[]
    inputs=[]
    for path in paths:
        data=path.read_bytes()
        inputs.append(dict(path=str(path),sha256=hashlib.sha256(data).hexdigest()))
        for section in fmt.parse(data):
            if section.signature not in (0x10E,0xFA):continue
            family=fmt.FAMILIES[section.signature]
            id_name='EffectId' if family=='Effects' else 'EffectElementId'
            for record in section.records:
                f=fmt.decode_fields(record,fmt.LAYOUTS[family]['fields'])
                view=fmt.decode_effect_engine_fields(record,family)
                identity=dict(family=family,id=f[id_name],name=f['Name'])
                records[family]+=1
                for name,value in view['NarrowedScalars'].items():
                    if value['raw_u32']>>16:
                        narrowed.append(dict(**identity,field=name,**value))
                if family=='Effects':
                    dispatch=view['GenerationDispatch'];modes[dispatch['raw']]+=1
                    for key in ['SprayGeneration','DigitGeneration','FourSlotGeneration']:
                        if key in view:special.append(dict(**identity,mode=dispatch['raw'],specialization=key,values=view[key]))
                    if not dispatch['defined']:invalid.append(dict(**identity,generation_type=dispatch['raw']))
                    slots=view['GenerateIdSlots'];last=slots['last_nonzero_index']
                    if any(x==0 for x in slots['ids'][:last]):
                        holes.append(dict(**identity,mode=dispatch['raw'],**slots))
                else:
                    if f['NextEffectId']:
                        next_links.append(dict(**identity,target_family='Effects',target_id=f['NextEffectId']))
                    candidates=[('HitLavaElementId',f['HitLavaElementId']),('HitWaterElementId',f['HitWaterElementId']),('HitSolidElementId',f['HitSolidElementId'])]
                    active=[(k,v) for k,v in candidates if v]
                    if active:
                        collision_priority.append(dict(**identity,below_surface_choice_field=active[0][0],
                            below_surface_choice_id=active[0][1],nonzero_candidates=dict(active)))
    return dict(scope=label,inputs=inputs,records=dict(records),generation_counts=dict(sorted(modes.items())),
        undefined_generation_types=invalid,generate_slot_holes=holes,upper_bits_not_stored_in_runtime=narrowed,
        element_next_effect_links=next_links,element_below_surface_choices=collision_priority,special_modes=special)

def main():
    scopes=[scope('standalone editor catalogues',[DATA/'Effects.kwd',DATA/'EffectElements.kwd']),
        scope('adjacent AutoSaveGlobals.kld',[DATA/'AutoSaveGlobals.kld']),
        scope('referenced maps/kop4Globals.kld',[DATA/'maps/kop4Globals.kld'])]
    result=dict(scope='Static views only. No original file changes or gameplay tests. The three scopes overlap and must not be counted as unique definitions.',scopes=scopes)
    (ROOT/'effect-catalogue-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    for value in scopes:
        print(value['scope'],value['records'],'undefined modes:',len(value['undefined_generation_types']),
            'holes:',len(value['generate_slot_holes']),'upper-bit fields:',len(value['upper_bits_not_stored_in_runtime']),
            'element->effect links:',value['element_next_effect_links'])

if __name__=='__main__':main()
