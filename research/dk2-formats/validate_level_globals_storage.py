"""Exact inverse edits of existing Level scalar/array fields; no source writes."""
import struct
import dk2_formats as fmt
import native_level_globals as api
import emulate_level_globals as n


def build():
    data=n.SOURCE.read_bytes();section=next(s for s in fmt.parse(data) if s.signature==0xDC)
    size=fmt.u32(section.header,len(section.header)-4);assert size==api.SIZE
    raw=section.data[:size];at=section.offset+len(section.header);rows=[]
    for b in api.BINDINGS:
        values=[bytes([v])*b['size'] for v in [0,165,255]] if b.get('opaque_bytes') else [0,1,(1<<(8*b['size']))-1]
        for v in values:
            edited=api.patch(raw,b['name'],v);off=b['raw'];width=b['size']
            assert edited[:off]==raw[:off] and edited[off+width:]==raw[off+width:]
            packed=data[:at]+edited+data[at+size:]
            assert b''.join(s.pack() for s in fmt.parse(packed))==packed
            assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s) for s in fmt.parse(packed)]))==packed
            assert packed[:at+off]+raw[off:off+width]+packed[at+off+width:]==data
            rows.append(dict(name=b['name'],raw_offset=off,size=width,value=v.hex() if type(v)is bytes else v,exact_neighbors=True,inverse_restores_source=True))
    for v in [0,1,0x1554,0x8000,65535]:
        edited=fmt.set_scalar(raw,'Level_25683','Flags',v);assert edited==raw[:0x638C]+struct.pack('<H',v)+raw[0x638E:]
        packed=data[:at]+edited+data[at+size:];assert fmt.rebuild_decoded(dict(sections=[fmt.decoded(s) for s in fmt.parse(packed)]))==packed
        assert packed[:at+0x638C]+raw[0x638C:0x638E]+packed[at+0x638E:]==data
        rows.append(dict(name='Flags',raw_offset=0x638C,size=2,value=v,exact_neighbors=True,inverse_restores_source=True))
    for c in n.cases():
        args,ch,saved,file=n.execute(c);post,out=n.verify(c,args,ch,saved['result'])
        for b in api.project(ch[n.RAWLEVEL]):
            blob=post[n.WORLD] if b['target']=='World' else post[0x6F2957]
            if b['name']=='TicksPerSecond':continue  # a later Variables19 may overwrite it.
            off=b['offset'] if b['target']=='World' else b['offset']-0x6F2957
            assert blob[off:off+len(b['data'])]==b['data'],(c['name'],b['name'])
        assert api.flags(int.from_bytes(ch[0x6F2957][:4],'little'),c['flags'])==out['runtime_flag_bits']
    assert n.SOURCE.read_bytes()==data
    return dict(passed=True,source=dict(file=str(n.SOURCE),sha256=n.sha(data)),exact_field_edits=rows,
        native_projection_cases=len(n.cases()),source_writes=0,scope='Existing physical fields and exact container/decoded roundtrips; no original editor writer or original fileIO reader executed.')


if __name__=='__main__':
    r=build();n.dump('phase196-Level-storage.json',r);print('PASS',len(r['exact_field_edits']),'exact inverse edits;',r['native_projection_cases'],'native projections')
