"""Discovery: realTerrainmodel -> wholeStatDataTerrain19 bufferedreader -> fullwriter.

Originalconverter/modelallocation/registration, TSVtokenizer/atof and writer
execute. Preparedbufferedreader/columnmap; outerfilename/headerparsernotrun.
"""
from pathlib import Path
import json,struct
import emulate_terrain_model_chain as old
import native_editor_text_executor as executor
ROOT=Path(__file__).resolve().parent
engine=old.engine
TEXT,READER,COLUMNS=0x17500000,0x17510000,0x17520000
CTX=0x54F550
sha=old.sha

def build(c):
    fixture=old.build(dict(ids=[1],unknown198=None,unknown224=None,pattern=False))
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in fixture['memory']}
    struct.pack_into('<I',mem[engine.RAW],0x224,c.get('initial_raw',0x1234))
    fixture['raw_records_hex']=[bytes(mem[engine.RAW][:552]).hex()]
    # Root4A3D30 initially advances from the preparedheader line to firstrow.
    data=bytearray(('header\tname\tvalue\r\n1\tTerrain1\t'+c['text']+'\r\n').encode('ascii'));end=len(data);delimiter=data.index(9);data[delimiter]=0
    mem[TEXT]=data+bytes([0])+bytes([165])*64
    mem[READER]=bytearray(struct.pack('<7I',1,0,0,TEXT,TEXT+end,TEXT,TEXT+delimiter))+bytes([165])*64
    mem[COLUMNS]=bytearray(struct.pack('<I',c.get('stat_code',19)));mem[CTX+0x650]=bytearray(struct.pack('<2I',COLUMNS,1))
    fixture.update(memory=[engine.region(a,b) for a,b in sorted(mem.items())],
        calls=fixture['calls'][:-1]+[dict(address=0x4A3D30,ecx=CTX,stack=[READER])]+fixture['calls'][-1:])
    if c.get('reload'):
        mem[engine.MID+0x1000]=bytearray(736+64)
        fixture['memory']=[engine.region(a,b) for a,b in sorted(mem.items())]
        fixture['calls'] += [dict(address=0x42ED90,ecx=engine.MID+0x1000,stack=[engine.writer.OUTPUT+56]),
            dict(address=0x49F5E0,ecx=0,stack=[engine.MID+0x1000])]
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return fixture

def execute(c):
    fixture=build(c);backend=sha(Path(executor.__file__).read_bytes())
    tag=sha(json.dumps(fixture,sort_keys=True).encode()+backend.encode())[:12];path=ROOT/'ghidra'/f'phase165-Terrain-text-{tag}.json'
    if path.exists():
        saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==fixture and saved['backend_sha256']==backend
    else:
        saved=dict(case=c,input=fixture,backend_sha256=backend,result=executor.run(fixture));path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return fixture,saved,path

if __name__=='__main__':
    f,s,p=execute(dict(name='three-half',text='3.5'));r=s['result'];print(p.name)
    print(json.dumps({k:r[k] for k in ['success','error','failures','registers','calls']}))
    if r['success']:
        post={int(v['address'],16):bytes.fromhex(v['hex']) for v in r['output']};blob=post[engine.writer.OUTPUT][:608]
        print('saved224',blob[56+0x224:56+0x228].hex())
