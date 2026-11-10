"""Verify editor views on explicit files, retaining raw bytes and original hashes."""
from pathlib import Path
import hashlib,json
import dk2_formats as fmt
from native_editor_views import editor_views,trap_commit_projection,SCHEMA
from inventory import DATA

ROOT=Path(__file__).resolve().parent

def main():
    checks=0
    # This is the signed editor presentation of u16 storage, not a file rewrite.
    for raw,expected in [(0,0),(32767,32767),(32768,-32768),(65526,-10),(65535,-1)]:
        record=bytearray(579);record[0x202:0x204]=raw.to_bytes(2,'little')
        view=next(v for v in editor_views(record,'Traps') if v['field']=='ManaUsage')
        assert view['editor_value']==expected and view['raw_unsigned']==raw;checks+=1
    for flags,cost,expected in [(0,250,0),(0x100,250,250),(0,0,0),(0x100,0,0),(0,0xffffffff,0)]:
        record=bytearray(579);record[0x1EC:0x1F0]=flags.to_bytes(4,'little');record[0x1D4:0x1D8]=cost.to_bytes(4,'little')
        before=bytes(record);result=trap_commit_projection(record)
        assert result['projected_raw_after']==expected and record==before;checks+=1
    for raw in [0,1,4095,4096,4628,0x7fffffff,0x80000000,0xffffffff]:
        # Binary scale by 2^12 is exact for these DWORD values in double precision.
        value=raw*SCHEMA['constants']['raw_to_editor_double']['value']
        assert int(value*SCHEMA['constants']['editor_double_to_raw']['value'])==raw;checks+=1
    unknown=next(x for x in SCHEMA['unknown_field_paths'] if x['family']=='Traps')
    assert unknown['editor_model_offset']=='0x238' and unknown['different_field']['editor_model_offset']=='0x158'
    assert unknown['different_field']['name']=='ResearchTime';checks+=1
    scopes=[]
    for label,paths in [('standalone editor catalogues',[DATA/'Terrain.kwd',DATA/'Traps.kwd']),
                        ('AutoSaveGlobals',[DATA/'AutoSaveGlobals.kld']),
                        ('referenced kop4Globals',[DATA/'maps/kop4Globals.kld'])]:
        rows=[];inputs=[]
        for path in paths:
            data=path.read_bytes();digest=hashlib.sha256(data).hexdigest()
            inputs.append(dict(path=str(path),sha256=digest))
            for section in fmt.parse(data):
                family=fmt.FAMILIES.get(section.signature)
                if family not in ('Terrain','Traps'):continue
                for index,record in enumerate(section.records):
                    views=editor_views(record,family)
                    item=dict(family=family,record=index,name=fmt.string(record[:32]),views=views)
                    if family=='Traps':
                        item['native_commit_cost_assignment']=trap_commit_projection(record)
                        item['unresolved_204_hex']=record[0x204:0x206].hex()
                    else:
                        item['unresolved_224_raw']=fmt.u32(record,0x224)
                        item['unresolved_224_editor_double']=fmt.u32(record,0x224)/4096
                    rows.append(item)
            assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
        scopes.append(dict(label=label,inputs=inputs,records=rows,
            out_of_editor_range=sum(not v['within_editor_validation'] for r in rows for v in r['views']),
            native_commit_cost_changes=[dict(name=r['name'],projection=r['native_commit_cost_assignment']) for r in rows
                if r['family']=='Traps' and r['native_commit_cost_assignment']['would_change']]))
    result=dict(synthetic_cases_passed=checks,scopes=scopes,
        scope='Static editor numeric/conditional-assignment projection; no original editor or altered game files executed.')
    (ROOT/'editor-bindings-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Editor projection:',checks,'boundary cases; raw inputs unchanged')
    for s in scopes:print(s['label'],len(s['records']),'records;',s['out_of_editor_range'],'values outside editor validation;',len(s['native_commit_cost_changes']),'conditional cost changes')

if __name__=='__main__':main()
