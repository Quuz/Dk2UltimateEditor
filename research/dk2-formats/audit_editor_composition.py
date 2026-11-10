"""Validate exact Lean edits and linked Room/Terrain projections on user files."""
from pathlib import Path
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from native_editor_composition import terrain_lean,set_terrain_lean,room_terrain_projection,SCHEMA

ROOT=Path(__file__).resolve().parent

def main():
    checks=0
    fixture=bytes((i*37+11)&255 for i in range(552))
    for axis,start in [('horizontal',0x1CF),('vertical',0x1D3)]:
        for values in [(0,0,0),(127,128,255),(255,255,255),(1,2,3)]:
            changed=set_terrain_lean(fixture,axis,values)
            assert changed[:start]==fixture[:start] and changed[start+3:]==fixture[start+3:]
            assert changed[0x1AE:0x1CE]==fixture[0x1AE:0x1CE]
            assert changed[0x1CE]==fixture[0x1CE] and changed[0x1D2]==fixture[0x1D2]
            assert list(terrain_lean(changed)[axis].values())==list(values);checks+=1
    for axis,values in [('diagonal',[1,2,3]),('horizontal',[1,2]),('vertical',[1,2,256]),
                        ('horizontal',[-1,0,0]),('vertical',[1,2,3.0])]:
        try:set_terrain_lean(fixture,axis,values)
        except ValueError:pass
        else:raise AssertionError((axis,values))
        checks+=1
    room=bytearray(1055);room[0x394]=9
    terrain=bytearray(fixture);terrain[0x1D6]=9
    assert room_terrain_projection(room,[])['findings'][0]['code']=='missing_terrain';checks+=1
    assert room_terrain_projection(room,[terrain,terrain])['findings'][0]['code']=='duplicate_terrain_id';checks+=1
    for flags in [0,0x7FFF,0xFFFFFFFF,0x80000020,0x14]:
        test=bytearray(terrain);test[0x190:0x194]=flags.to_bytes(4,'little')
        for offset in [0x19A,0x19C,0x19E]:test[offset:offset+2]=b'\xff\xff'
        before=bytes(test);p=room_terrain_projection(room,[test]);out=int(p['forced_flags_projection']['after'],16)
        assert out&0x7FDF==0x14 and out&~0x7FDF==flags&~0x7FDF
        assert all(r['projected_after']==0 and r['before']==65535 for r in p['unconditional_scalar_resets'])
        assert bytes(test)==before;checks+=1
    scopes=[];patches=0
    for label,paths in [('standalone editor catalogues',[DATA/'Terrain.kwd',DATA/'Rooms.kwd']),
                        ('AutoSaveGlobals',[DATA/'AutoSaveGlobals.kld']),
                        ('referenced kop4Globals',[DATA/'maps/kop4Globals.kld'])]:
        terrains=[];rooms=[];inputs=[]
        for path in paths:
            data=path.read_bytes();digest=hashlib.sha256(data).hexdigest();sections=fmt.parse(data)
            inputs.append(dict(path=str(path),sha256=digest))
            for section in sections:
                if section.signature==0x78:rooms.extend(section.records)
                if section.signature!=0x6E:continue
                terrains.extend(section.records)
                for index,record in enumerate(section.records):
                    for axis in ['horizontal','vertical']:
                        edited=set_terrain_lean(record,axis,[0,128,255])
                        records=list(section.records);records[index]=edited
                        clone=fmt.Section(section.offset,section.signature,section.header,records,section.suffix)
                        clone.replace_records(records)
                        packed=clone.pack();reread=fmt.parse(packed)[0]
                        assert reread.records[index]==edited and len(packed)==len(section.pack())
                        assert all(a==b for n,(a,b) in enumerate(zip(reread.records,section.records)) if n!=index)
                        assert edited[0x1AE:0x1CE]==record[0x1AE:0x1CE]
                        patches+=1
            assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
        links=[];counts={}
        for record in rooms:
            tid=record[0x394];counts[tid]=counts.get(tid,0)+1
            result=room_terrain_projection(record,terrains)
            assert not result['findings']
            links.append(dict(room_name=fmt.string(record[:32]),room_id=record[0x392],projection=result))
        scopes.append(dict(label=label,inputs=inputs,terrain_records=len(terrains),room_records=len(rooms),
            room_terrain_links=links,shared_terrain_ids={str(k):v for k,v in counts.items() if v>1},
            scalar_reset_change_candidates=[r['room_name'] for r in links if any(v['would_change'] for v in r['projection']['unconditional_scalar_resets'])],
            forced_flag_change_candidates=[dict(room=r['room_name'],flags=r['projection']['forced_flags_projection'])
                for r in links if r['projection']['forced_flags_projection']['would_change']]))
    result=dict(synthetic_cases_passed=checks,actual_lean_patch_roundtrips=patches,
        original_inputs_unchanged=True,scopes=scopes,
        scope='Byte-preserving Lean codec checks and static known Room GUI assignments only; no modified game/editor execution.')
    (ROOT/'editor-composition-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Editor composition:',checks,'boundary cases;',patches,'actual Lean patch roundtrips')
    for s in scopes:print(s['label'],s['room_records'],'resolved links;',len(s['scalar_reset_change_candidates']),'scalar change candidates;',len(s['forced_flag_change_candidates']),'forced flag change candidates')

if __name__=='__main__':main()
