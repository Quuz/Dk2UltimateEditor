"""OriginalbufferedTerraintextStat19 -> wholemodel/writer, unknown224rawbits.

Wholeinputbuffer/reader/columnmapprepared; no filename/header/fileI/O or GUI.
No newgameplayname/units; nativewriter signedlow32 conversion is observed.
"""
from pathlib import Path
from decimal import Decimal,ROUND_DOWN
import json,struct,math
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_terrain_text_stat as p
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
engine=p.engine
sha=p.sha

def cases():
    return [dict(name=n,text=t,**kw) for n,t,kw in [('zero','0',{}),('one','1',{}),('three-half','3.5',{}),
        ('raw-unit','0.000244140625',{}),('fraction','1.9999',{}),('minus-one','-1',{}),('minus-quarter','-0.25',{}),
        ('dash-skip','-',{}),('minus-zero-skip','-0',{}),('max-u32','1048575.999755859375',{}),('code20-ignore','3.5',dict(stat_code=20)),
        ('minus-one-reload','-1',dict(reload=True)),('three-half-reload','3.5',dict(reload=True))]]

def execute(c):return p.execute(c)

def verify(c,fixture,r):
    assert r['success'] and r['error'] is None and not r['failures'],(c,r['error'],r['failures'])
    assert r['source_exe_sha256']=='989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09' and r['code_substitutions']==0
    inputs={int(x['address'],16):bytes.fromhex(x['hex']) for x in fixture['memory']};post={int(x['address'],16):bytes.fromhex(x['hex']) for x in r['output']}
    raw=bytes.fromhex(fixture['raw_records_hex'][0]);initial=int.from_bytes(raw[0x224:0x228],'little')
    skip=c['text'] in ['-','-0'] or c.get('stat_code',19)!=19
    expected=initial if skip else int((Decimal(c['text'])*4096).to_integral_value(rounding=ROUND_DOWN))&0xFFFFFFFF
    wanted=raw[:0x224]+struct.pack('<I',expected)+raw[0x228:]
    output=post[engine.writer.OUTPUT];data=output[:608];parts=engine.fmt.parse(data)
    assert len(parts)==1 and parts[0].signature==110 and parts[0].records==[wanted]
    assert b''.join(s.pack() for s in parts)==data and output[608:]==bytes([165])*(len(output)-608)
    assert struct.unpack_from('<I',data,8)[0]==608 and struct.unpack_from('<I',data,20)[0]==1 and struct.unpack_from('<I',data,52)[0]==552
    assert struct.unpack('<3I',post[engine.writer.META])==(4,engine.writer.OUTPUT+608,0)
    loads=1+int(c.get('reload',False))
    vector,count,cap,grow=struct.unpack('<4I',post[0x54F960]);assert count==loads and cap>=count
    heap=post[engine.HEAP];ptr=struct.unpack_from('<I',heap,vector-engine.HEAP)[0];model=heap[ptr-engine.HEAP:ptr-engine.HEAP+0x3D8]
    assert model[:4]==struct.pack('<I',0x50B640) and model[0x168]==1 and model[0x3D0:0x3D2]==raw[0x198:0x19A]
    value=struct.unpack_from('<d',model,0x320)[0];wanted_value=initial/4096 if skip else float(c['text'])
    assert value==wanted_value or abs(value-wanted_value)<=math.ulp(wanted_value)
    reloaded=None
    if c.get('reload'):
        ptr2=struct.unpack_from('<I',heap,vector-engine.HEAP+4)[0]
        model2=heap[ptr2-engine.HEAP:ptr2-engine.HEAP+0x3D8];reloaded=struct.unpack_from('<d',model2,0x320)[0]
        assert model2[:4]==struct.pack('<I',0x50B640) and reloaded==expected/4096
        assert post[engine.MID+0x1000][-64:]==bytes(64)
    assert r['visited_instructions'].get('0x4a400f',0)==int(not skip)
    assert post[engine.RAW]==inputs[engine.RAW] and post[engine.MID][-64:]==bytes(64)
    assert post[p.READER][28:]==inputs[p.READER][28:] and post[p.COLUMNS]==inputs[p.COLUMNS]
    assert post[p.CTX+0x650]==inputs[p.CTX+0x650]
    assert post[0]==inputs[0]==bytes(4096)
    assert len(post[p.TEXT])==len(inputs[p.TEXT])
    assert all(x==y or x in [9,13] and y==0 for x,y in zip(inputs[p.TEXT],post[p.TEXT]))
    assert struct.unpack('<7I',post[p.READER][:28])[1:3]==(1,1)
    assert len(r['calls'])==4+2*int(c.get('reload',False)) and all(x['returned'] for x in r['calls'])
    for a in [0x42ED90,0x49F5E0,0x4250D0]:assert r['visited_instructions'].get(hex(a))==loads
    for a in [0x4A3D30,0x4A7690]:assert r['visited_instructions'].get(hex(a))==1
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=engine.EXE.read_bytes()
    for row in r['low_reads']+r['low_writes']:
        ins=next(md.disasm(pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert row['address']==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    assert struct.unpack_from('<I',post[engine.ALLOC],8)[0]==0
    return dict(initial_raw224=initial,stat_code=c.get('stat_code',19),cell_text=c['text'],model_double=value,saved_raw224=expected,reloaded_model_double=reloaded,
        selected_assignment_executed=not skip,whole_record_exact=True,other_fields_and_guards_preserved=True,
        low_page_preserved=True,only_attested_FS0_low_accesses=True,complete_output_bytes=608,native_steps=r['instruction_count'])

def main():
    rows=[]
    for c in cases():
        fixture,s,path=execute(c);out=verify(c,fixture,s['result']);rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out))
        print(c['name'],'model',out['model_double'],'raw',hex(out['saved_raw224']),'verified',flush=True)
    (ROOT/'phase165-terrain-text-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')

if __name__=='__main__':main()
