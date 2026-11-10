"""Whole original Trap raw converter -> allocated model registration -> serializer.

Reuse the frozen original-editor heap/CString/IAT scene; no game code changes.
Unknown204 is tested as one WORD; no gameplay name or signedness inferred.
"""
from pathlib import Path
import json,struct,sys
import emulate_room_model_chain as engine
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
SOURCE=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Traps.kwd'
VECTOR=0x54FA40
MIDSIZE=747
MODELSIZE=0x2E8
sha=lambda b:__import__('hashlib').sha256(b).hexdigest()
def source_records():return [r for s in engine.fmt.parse(SOURCE.read_bytes()) if s.signature==0x82 for r in s.records]
def cases():
    return [dict(name='catalogue16',indices=list(range(len(source_records()))),word=None)]+[dict(name=f'word-{v:04x}',indices=[0],word=v) for v in [0,1,0x7FFF,0x8000,0xFFFF]]
def build(c):
    prior=engine.build();mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in prior['memory']}
    del mem[engine.RAW];del mem[engine.MID];del mem[0x54F998]
    mem[VECTOR]=bytearray(16);mem[0x550954]=mem.pop(0x550936)
    raws=[]
    for i,idx in enumerate(c['indices']):
        raw=bytearray(source_records()[idx]);assert len(raw)==579
        if c['word'] is not None:struct.pack_into('<H',raw,0x204,c['word'])
        raws.append(bytes(raw));mem[engine.RAW+i*0x1000]=raw+bytes([165])*64
        mem[engine.MID+i*0x1000]=bytearray(MIDSIZE+64)
    n=max(0x4000,(56+579*len(raws)+4095)&~4095)
    mem[engine.writer.OUTPUT]=bytearray([165])*n
    mem[engine.writer.CODE]=bytearray(bytes(mem[engine.writer.CODE]).replace(engine.p32(engine.writer.OUTPUT+0x4000),engine.p32(engine.writer.OUTPUT+n)))
    calls=[]
    for i in range(len(raws)):
        calls += [dict(address=0x431530,ecx=engine.MID+i*0x1000,stack=[engine.RAW+i*0x1000]),dict(address=0x4A1770,ecx=0,stack=[engine.MID+i*0x1000])]
    calls.append(dict(address=0x4A8F40,ecx=0x54F550,stack=[engine.writer.FILE]))
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return dict(raw_records_hex=[r.hex() for r in raws],memory=[engine.region(a,b) for a,b in sorted(mem.items())],providers=prior['providers'],calls=calls)
def path_for(c,fixture):return ROOT/'ghidra'/f'phase146-Traps-model-{c["name"]}-{sha(json.dumps(fixture,sort_keys=True).encode())[:12]}.json'
def execute(c):
    fixture=build(c);p=path_for(c,fixture)
    if p.exists():saved=json.loads(p.read_text(encoding='utf8'));assert saved['case']==c and saved['input']==fixture
    else:saved=dict(case=c,input=fixture,result=engine.run(fixture));p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    return fixture,saved,p
def verify(c,fixture,r):
    assert r['success'] and r['error'] is None and not r['failures'],(c,r['error'],r['registers'],r['calls'][-3:])
    assert r['source_exe_sha256']=='989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09' and r['code_substitutions']==0
    inputs={int(x['address'],16):bytes.fromhex(x['hex']) for x in fixture['memory']}
    post={int(x['address'],16):bytes.fromhex(x['hex']) for x in r['output']};raws=[bytes.fromhex(x) for x in fixture['raw_records_hex']]
    data=post[engine.writer.OUTPUT][:56+579*len(raws)];parts=engine.fmt.parse(data)
    assert len(parts)==1 and parts[0].signature==0x82 and len(parts[0].records)==len(raws)
    diffs=[dict(record=i,changes=[(hex(j),a,b) for j,(a,b) in enumerate(zip(raw,out)) if a!=b]) for i,(raw,out) in enumerate(zip(raws,parts[0].records)) if raw!=out]
    assert parts[0].records==raws,diffs
    assert b''.join(s.pack() for s in parts)==data
    vector,count,cap,grow=struct.unpack('<4I',post[VECTOR]);assert count==len(raws) and cap>=count
    heap=post[engine.HEAP];models=[]
    for i,raw in enumerate(raws):
        ptr=struct.unpack_from('<I',heap,vector-engine.HEAP+4*i)[0];m=heap[ptr-engine.HEAP:ptr-engine.HEAP+MODELSIZE]
        assert len(m)==MODELSIZE and m[0x238:0x23A]==raw[0x204:0x206]
        assert post[engine.MID+i*0x1000][0x2AC:0x2AE]==raw[0x204:0x206]
        assert post[engine.RAW+i*0x1000]==inputs[engine.RAW+i*0x1000]
        assert post[engine.MID+i*0x1000][-64:]==bytes(64)
        models.append(dict(address=hex(ptr),sha256=sha(m)))
    assert post[engine.writer.OUTPUT][len(data):]==bytes([165])*(len(post[engine.writer.OUTPUT])-len(data))
    assert struct.unpack('<3I',post[engine.writer.META])==(3+len(raws),engine.writer.OUTPUT+len(data),0)
    assert struct.unpack_from('<I',data,8)[0]==len(data) and struct.unpack_from('<I',data,20)[0]==len(raws) and struct.unpack_from('<I',data,52)[0]==579*len(raws)
    assert len(r['calls'])==2*len(raws)+1 and all(x['returned'] for x in r['calls'])
    for a,n in [(0x431530,len(raws)),(0x4A1770,len(raws)),(0x425C20,len(raws)),(0x4A8F40,1)]:assert r['visited_instructions'].get(hex(a))==n
    from capstone import Cs,CS_ARCH_X86,CS_MODE_32
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=engine.EXE.read_bytes()
    for row in r['low_reads']:
        pc=int(row['pc'],16);ins=next(md.disasm(pe_read(blob,pc,15),pc))
        assert row['address']==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    assert struct.unpack_from('<I',post[engine.ALLOC],8)[0]==0
    return dict(records=len(raws),full_record_bytes_preserved=True,unknown204_WORD_preserved=True,complete_output_bytes=len(data),native_array_capacity=cap,
        model_bytes_fingerprinted=MODELSIZE*len(raws),models=models,instructions=r['instruction_count'])
def main():
    selected=cases();rows=[]
    if len(sys.argv)>1:selected=[c for c in selected if c['name'] in sys.argv[1:]];assert selected
    for c in selected:
        fixture,saved,p=execute(c);out=verify(c,fixture,saved['result']);rows.append(dict(case=c,file=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes()),outcome=out));print(c['name'],out,flush=True)
    if len(selected)==len(cases()):(ROOT/'phase146-Traps-model-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
