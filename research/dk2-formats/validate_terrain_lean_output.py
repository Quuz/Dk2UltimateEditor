"""Independent raw→derived samples→Lean→final grid projection for selected maps.

Uniform Terrain8, zero Wibble, integer cursor(2,3), unchanged default height
option. Navigation varies, actual World methods execute. Python sin is checked
against actual native FSIN at the five selected finite angles5..7, not claimed
bit-equivalent to x87 for all arguments. Every output byte must match.
"""
from pathlib import Path
import json,math,struct
import emulate_terrain_lean as native

ROOT=Path(__file__).resolve().parent


def validate():
    manifest=json.loads((ROOT/'phase133-Lean-emulations.json').read_text())
    rows=[];seen={}
    for case,row in zip(native.cases(),manifest['replays']):
        args,checks,source,parent=native.build(case);saved=json.loads((ROOT/row['file']).read_text())
        assert saved['input']==args and native.base.sha((ROOT/row['file']).read_bytes())==row['sha256']
        native.verify(case,args,checks,source,parent,saved['result'])
        sh=saved['result']['shape_calls'][0];raw=checks[native.base.TERRAIN_RAW]
        assert raw[0x1CE]==raw[0x1D2]==0
        derived=saved['result']['derived_sample_calls'];assert len(derived)==1
        expected_samples=bytearray.fromhex(derived[0]['before_hex'])
        for z in range(3):
            for y in range(3):
                for x in range(3):
                    wave=-0.15-math.sin((10+x+y)*0.5)*0.125 if z==0 else 0
                    offset=(z*9+y*3+x)*20
                    struct.pack_into('<4f',expected_samples,offset,0,0,raw[0x1CF+z]/512,raw[0x1D3+z]/512)
                    if z==0:struct.pack_into('<f',expected_samples,offset+16,wave)
        samples=bytes(expected_samples)
        assert samples==bytes.fromhex(sh['samples_hex'])==bytes.fromhex(derived[0]['after_hex']),case['name']
        assert bytes.fromhex(sh['input_grid_hex'])==bytes(324)
        shape,temp=native.model.project(bytes(324),struct.pack('<16I',*native.occupancy_grid(case)),samples)
        assert shape==bytes.fromhex(sh['output_grid_hex']) and temp==bytes.fromhex(saved['result']['shape_stages'][0]['hex'])
        lattice=native.base.expected_grid(parent)
        vectors=list(struct.iter_unpack('<3f',shape))
        expected_final=b''.join(struct.pack('<3f',*[native.base.f32(a+b) for a,b in zip(p,d)]) for p,d in zip(lattice,vectors))
        actual_final=bytes.fromhex(saved['result']['deform_captures'][0]['hex'])
        assert actual_final==expected_final,case['name']
        seen[case['name']]=expected_final
        rows.append(dict(case=case['name'],derived_samples_bytes=540,derived_sample_bytes_preserved_from_before=72,temporary_shape_bytes=324,shape_output_bytes=324,
            entire_final_grid_bytes=324,expected_final_sha256=native.base.sha(expected_final)))
    assert seen['east-H']==seen['east-H-dynamic']
    assert seen['east-H-fluid']==seen['free-H']
    assert seen['free-H']==seen['free-zero']==seen['free-V']
    changes={}
    for case in native.cases():
        name=case['name'];baseline=case['topology']+'-zero'
        p=list(struct.iter_unpack('<3f',seen[name]));b=list(struct.iter_unpack('<3f',seen[baseline]))
        changes[name]=dict(changed_points_per_axis=[sum(a[j]!=z[j] for a,z in zip(p,b)) for j in range(3)],
            deltas_per_point=[[a[j]-z[j] for j in range(3)] for a,z in zip(p,b)])
    assert changes['east-H']['changed_points_per_axis']==[18,0,0]
    assert changes['east-V']['changed_points_per_axis']==[0,0,9]
    assert changes['east-H']['deltas_per_point'][:3]==[[0,0,0],[0.125,0,0],[0.25,0,0]]
    return dict(phase=133,scope=__doc__,cases=rows,case_count=len(rows),
        complete_independently_predicted_bytes=sum(sum(r[k] for k in ['derived_samples_bytes','temporary_shape_bytes','shape_output_bytes','entire_final_grid_bytes']) for r in rows),
        actual_geometry_changes=changes,raw_field_source='Terrain1CF..1D1/1D3..1D5; current source-derived exact bytes plus explicit diagnostic changes.')


if __name__=='__main__':
    value=validate();(ROOT/'phase133-Lean-output-validation.json').write_text(json.dumps(value,indent=2),encoding='utf8')
    print('Cases',value['case_count'],'independently predicted bytes',value['complete_independently_predicted_bytes'])
