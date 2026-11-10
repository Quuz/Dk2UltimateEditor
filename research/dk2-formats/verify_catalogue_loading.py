"""Boundary/permutation cases derived from the native allocation/search code."""
import itertools,json
from pathlib import Path
from native_catalogue_loading import project_records,project_files,SCHEMA
from inventory import DATA

def codes(view):return {row['code'] for row in view['findings']}
def rows(ids):return [dict(id=n) for n in ids]

def main():
    checked=0
    # All permutations and gaps are supported for the seven simple sorted families.
    for family in SCHEMA['families']:
        if family in ('Terrain','Creatures'):continue
        for ids in itertools.permutations([1,4,14,255]):
            result=project_records(family,rows(ids))
            assert result['runtime_ids']==[1,4,14,255] and not result['findings']
            checked+=1
    result=project_records('Terrain',rows([1,2,3]))
    assert result['allocated_slots']==4 and not result['findings'];checked+=1
    assert 'terrain_id_outside_counted_allocation' in codes(project_records('Terrain',rows([1,2,4])));checked+=1
    assert not project_records('Terrain',rows([1,2,4]),header_count=4)['findings'];checked+=1
    assert 'zero_id_not_returned_by_lookup' in codes(project_records('Traps',rows([0,1])));checked+=1
    result=project_records('Traps',[dict(id=1,label='old'),dict(id=1,label='new'),dict(id=2)])
    assert result['runtime_ids']==[1,1,2]
    assert result['lookup_by_raw_id']['1']['record']['label']=='new'
    assert 'loaded_id_not_found' in codes(result) # ID2 is present but search starts at slot1, sees ID1.
    checked+=1
    result=project_records('Traps',[dict(id=5,label='old'),dict(id=5,label='new')])
    assert result['lookup_by_raw_id']['5']['record']['label']=='old';checked+=1
    result=project_records('Traps',rows([1,2,3]),header_count=2)
    assert 'record_ignored_at_capacity' in codes(result) and result['runtime_ids']==[1,2];checked+=1
    # The incoming raw ID is compared with the EXISTING effective creature key.
    unique=dict(id=32,unique=True,clone_id=12)
    result=project_records('Creatures',[unique,dict(id=13)])
    assert result['runtime_ids']==[32,13]
    assert result['lookup_by_raw_id']['32']['slot'] is None
    assert result['creature_ordinary_lookup']['12']['record']==unique;checked+=1
    result=project_records('Creatures',[dict(id=13),unique])
    assert result['runtime_ids']==[13,32] and result['lookup_by_raw_id']['32']['slot']==1;checked+=1
    scopes=[]
    for label,paths in [('standalone editor catalogues',[DATA/(name+'.kwd') for name in SCHEMA['families']]),
                        ('AutoSaveGlobals',[DATA/'AutoSaveGlobals.kld']),
                        ('referenced kop4Globals',[DATA/'maps/kop4Globals.kld'])]:
        result=project_files(paths);result['label']=label
        assert len(result['families'])==9
        assert not any(item['findings'] for item in result['families'].values())
        scopes.append(result)
    output=Path(__file__).resolve().parent/'catalogue-loading-audit.json'
    output.write_text(json.dumps(dict(synthetic_cases_passed=checked,scopes=scopes),ensure_ascii=False,indent=2),encoding='utf8')
    print('Native ID projection:',checked,'boundary/permutation cases; 3 explicit scopes, 9 families each, no findings')

if __name__=='__main__':main()
