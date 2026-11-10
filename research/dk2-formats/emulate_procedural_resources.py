"""Raw type7 Shot/Effect resource to native descriptor and procedural consumer.

Graphics slot54 is the original CPCEngineInterface implementation, not an IO
adapter. Type7 succeeds without file IO and returns handle0. The sound lookup
is an explicitly declared null external provider. RenderInfo, entity registry,
transform and a finite free engine heap are prepared fixtures, not a live scene.
"""
from pathlib import Path
import hashlib, json, struct, sys
import dk2_formats as fmt
import native_unicorn_factorytrace as cpu
from emulate_resistance import post_context
from emulate_element_colors import region
from build_effect_semantics import EXE, pe_read

ROOT = Path(__file__).resolve().parent
RAW, MID, DEFINITION, GFX = 0x18000000, 0x18010000, 0x18020000, 0x18030000
SOUND, SV, SCB, IO = 0x18040000, 0x18041000, 0x18042000, 0x18043000
RENDER, POSITION, HEAP, QUEUE = 0x18050000, 0x18060000, 0x18070000, 0x18090000
SCRATCH, STOP, ENTITY, CASTER = 0x180A0000, 0x180B0000, 0x180C0000, 0x180D0000
CRT_HEAP, CRT_STATE, CRT_CODE, TLS, CRIT = 0x18100000, 0x18150000, 0x18160000, 0x18170000, 0x18180000
CRT_SIZE=0x40000


def sha(blob): return hashlib.sha256(blob).hexdigest()
def put(blob, offset, value, size=4): blob[offset:offset+size] = value.to_bytes(size, 'little')


def providers():
    p32=lambda x:struct.pack('<I',x)
    # HeapAlloc boundary, preserving original operator new/malloc and RNG bodies.
    malloc=bytearray.fromhex('8b44240c8b0d')+p32(CRT_STATE)
    malloc+=bytes.fromhex('8d140181fa')+p32(CRT_SIZE)+bytes.fromhex('771f')
    malloc+=bytes.fromhex('8915')+p32(CRT_STATE)+bytes.fromhex('81c1')+p32(CRT_HEAP)
    malloc+=bytes.fromhex('8b15')+p32(CRT_STATE+4)
    malloc+=bytes.fromhex('8904d5')+p32(CRT_STATE+0x100)
    malloc+=bytes.fromhex('890cd5')+p32(CRT_STATE+0x104)
    malloc+=bytes.fromhex('ff05')+p32(CRT_STATE+4)+bytes.fromhex('8bc1c20c00')
    fail=len(malloc)
    malloc+=bytes.fromhex('c705')+p32(CRT_STATE+8)+p32(1)+bytes.fromhex('33c0c20c00')
    at=malloc.index(bytes.fromhex('771f'));malloc[at+1]=fail-at-2
    free=bytes.fromhex('8b44240c8b0d')+p32(CRT_STATE+12)
    free+=bytes.fromhex('89048d')+p32(CRT_STATE+0x4000)
    free+=bytes.fromhex('ff05')+p32(CRT_STATE+12)+bytes.fromhex('b801000000c20c00')
    return {CRT_CODE:bytes(malloc),CRT_CODE+0x100:free,
        CRT_CODE+0x200:bytes.fromhex('c20400'),
        CRT_CODE+0x300:b'\xb8'+p32(TLS)+bytes.fromhex('c20400'),
        CRT_CODE+0x400:bytes.fromhex('33c0c3'),CRT_CODE+0x500:bytes.fromhex('c20400')}


def originals():
    rows = []
    for family, sig in [('Shots', 0x104), ('Effects', 0x10E)]:
        path = ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor' / (family+'.kwd')
        for si, section in enumerate(fmt.parse(path.read_bytes())):
            if section.signature != sig: continue
            for ri, raw in enumerate(section.records):
                if raw[0x70] == 7:
                    rows.append(dict(name=f'{family}-{ri}', family=family, section=si, record=ri,
                        source=str(path), source_sha256=sha(path.read_bytes()), raw=raw,
                        record_name=fmt.string(raw[:32]), union=list(struct.unpack_from('<3I',raw,0x64))))
    assert len(rows)==7
    return rows


def cases():
    out = []
    for row in originals():
        out.append(dict(name=row['name'], family=row['family'], record=row['record'], render=False))
    for family, record in [('Effects',27),('Effects',3),('Shots',20),('Shots',29)]:
        out.append(dict(name=f'{family}-{record}-render', family=family, record=record, render=True))
    for name,family,record,union in [
        ('ring-opaque-ffffffff','Effects',27,[4,0xFFFFFFFF,0]),
        ('ring-opaque-highbit','Effects',27,[4,0x80000000,0xFFFFFFFF]),
        ('beam-opaque-ffffffff','Shots',20,[2,0,0xFFFFFFFF]),
        ('beam-opaque-highbit','Shots',20,[2,0x80000000,0]),
        ('selector-zero','Effects',27,[0,1,0x009F025C]),
        ('selector-five','Effects',27,[5,1,0x009F025C]),
        ('selector-ffffffff','Effects',27,[0xFFFFFFFF,1,0x009F025C]),
        ('selector-one','Effects',27,[1,1,0x009F025C]),
        ('selector-six','Effects',27,[6,1,0x009F025C])]:
        out.append(dict(name=name,family=family,record=record,render=True,union=union))
    return out


def build(case):
    source = next(r for r in originals() if r['family']==case['family'] and r['record']==case['record'])
    raw = bytearray(source['raw'])
    if 'union' in case: struct.pack_into('<3I', raw, 0x64, *case['union'])
    shot = case['family']=='Shots'
    mid_size, def_size = (375,118) if shot else (426,148)
    gfx = bytearray(0x300); put(gfx, 0, 0x670574); put(gfx, 0x8A, 0x1234)
    put(gfx, 0x92, 0x5678, 2)
    sv = bytearray(0x24); put(sv,0x1C,SCB)
    sound = struct.pack('<I',SV)
    scb = bytes.fromhex('ff05')+struct.pack('<I',IO)+bytes.fromhex('33c0c20800')
    definition = bytearray(def_size)
    render = bytearray(0xC0); put(render,0x28,DEFINITION); put(render,0x4F,100,2)
    heap = bytearray([165])*(0x10000+64); heap[:20]=bytes(20)
    put(heap,12,0x10000); put(heap,16,64,2); heap[19]=1
    allocator=bytearray(0x300);put(allocator,0xC,64);put(allocator,0xA0+4*64,HEAP)
    registry=bytearray(102*4);put(registry,100*4,ENTITY);put(registry,101*4,CASTER)
    exists=bytearray(102);exists[100]=exists[101]=1
    entity=bytearray(371);put(entity,0x12,1);put(entity,0x147,101,2)
    caster=bytearray(64);put(caster,0x1A,8192);put(caster,0x1E,12288);put(caster,0x22,(-16384+512)&0xFFFFFFFF)
    matrix=bytearray(36)
    for i in range(3):struct.pack_into('<f',matrix,(i*3+i)*4,1.0)
    memory={RAW:raw,MID:bytes([165])*mid_size,DEFINITION:definition,GFX:gfx,
        SOUND:sound,SV:sv,SCB:scb,IO:bytes(4),RENDER:render,
        POSITION:struct.pack('<3f',2.0,3.0,4.0),HEAP:heap,QUEUE:bytes(0x80),SCRATCH:bytes(32),
        ENTITY:entity,CASTER:caster,0x6ED540:exists,0x6EE540:registry,
        0x765B00:allocator,0x760B80:struct.pack('<I',QUEUE),0x760AC4:matrix,
        0x75B468:struct.pack('<I',SOUND),0x6FCDCF:bytes(1),0x6D6321:bytes(1),
        0x795840:bytes(16),0x6BE8F0:pe_read(EXE.read_bytes(),0x6BE8F0,4),
        0x740C20:bytes(32),
        0:bytes(4),0x7FFEC000:bytes([165])*0x6000}
    memory.update(providers())
    memory[CRT_HEAP]=bytes([165])*CRT_SIZE
    memory[CRT_STATE]=bytes(0x8000)
    tls=bytearray(0x74);put(tls,0x14,1);memory[TLS]=tls;memory[CRIT]=bytes(24)
    memory[0x6CBAD4]=bytes(4) # original small-block threshold, use Windows heap
    memory[0x7A5DA8]=struct.pack('<I',0x1234)
    memory[0x6C9238+9*4]=struct.pack('<I',CRIT)
    memory[0x6C9AB0]=struct.pack('<6I',0x6C9AB0,0,0,0,0,0)
    for slot,target in [(0x66C14C,CRT_CODE),(0x66C148,CRT_CODE+0x100),
        (0x66C22C,CRT_CODE+0x200),(0x66C118,CRT_CODE+0x200),
        (0x66C0F4,CRT_CODE+0x300),(0x66C0A4,CRT_CODE+0x400),(0x66C0F8,CRT_CODE+0x500)]:
        memory[slot]=struct.pack('<I',target)
    # Whole resource initializer; Effects loader receives a zero-initialized
    # remaining definition, explicitly not its complete constructor.
    calls=[]
    if shot:
        calls.append(dict(address='0x4acd60',registers={'ECX':hex(DEFINITION)},stack_args=[]))
        calls.append(dict(address='0x56e540',registers={'ECX':hex(MID)},stack_args=[]))
    else:
        calls.append(dict(address='0x504d60',registers={'ECX':hex(DEFINITION)},stack_args=[]))
    calls.append(dict(address=hex(0x56E690 if shot else 0x56AFE0),registers={'ECX':hex(MID)},stack_args=[RAW]))
    calls.append(dict(address=hex(0x4ACDF0 if shot else 0x4358C0),registers={'ECX':hex(DEFINITION)},stack_args=[MID,GFX]))
    if case['render']:
        calls.append(dict(address='0x5a01a0',registers={'ECX':hex(GFX)},stack_args=[RENDER,POSITION,0]))
    spans=sorted((a,len(b)) for a,b in memory.items())
    assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])), 'Overlapping input fixtures'
    args=dict(program='DKII-DX.exe', address='0x504d60', registers=json.dumps({'ECX':hex(SCRATCH)}),
        memory=json.dumps({'regions':[region(a,b) for a,b in memory.items()]}),before_calls=calls,max_steps=500000,
        observe_reads=[[RAW,len(raw)],[MID,mid_size],[DEFINITION,def_size],[0,4096]],
        observe_writes=[[HEAP,len(heap)],[QUEUE,0x80],[DEFINITION,def_size],[0x400000,0x3B0000]],
        allowed_external_IO_code=[[SCB,len(scb)]]+[[a,len(b)] for a,b in providers().items()])
    checks={a:bytes(b) for a,b in memory.items() if a not in [SCB,0x7FFEC000] and a not in providers()}
    checks[0x66FE04]=pe_read(EXE.read_bytes(),0x66FE04,0x40)
    return args,checks,source


def path_for(args):
    tag=sha((json.dumps(args,sort_keys=True)+sha((ROOT/'native_unicorn_factorytrace.py').read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase129-procedural-native-{tag}.json'


def verify(case,args,checks,source,result):
    assert result['success'] and result['hit_return'] and not result['emulation_error']
    assert not result['protected_write_or_execution_failures']
    assert result['function_or_vtable_substitutions']==0
    assert len(result['before_calls'])==len(args['before_calls']) and all(c['returned'] for c in result['before_calls'])
    post=post_context(checks,result)
    raw=checks[RAW];selector=int.from_bytes(raw[0x64:0x68],'little')
    expected=bytearray(27);put(expected,0,selector);expected[10]=7;put(expected,11,int.from_bytes(raw[0x60:0x64],'little')|1)
    assert post[DEFINITION][:27]==expected
    assert post[RAW]==raw and post[0x66FE04]==checks[0x66FE04]
    resource_mid=0xA8 if case['family']=='Shots' else 0xD4
    assert post[MID][resource_mid:resource_mid+84]==raw[0x20:0x74]
    if case['family']=='Shots':assert int.from_bytes(post[DEFINITION][0x33:0x37],'little')==sum(post[MID])
    # Never accept convenient mapped zero-page NULL+offset reads as valid state.
    low=[r for r in result['observed_reads'] if int(r['address'],16)<4096]
    assert all(int(r['address'],16)==0 and r['size']==4 and pe_read(EXE.read_bytes(),int(r['pc'],16),6)[:2]==bytes.fromhex('64a1') for r in low),low
    assert not [r for r in result['observed_writes'] if 0x66FE04<=int(r['address'],16)<0x66FE44]
    assert all(any(a<=int(w['address'],16) and int(w['address'],16)+w['size']<=a+len(b) for a,b in checks.items())
        for w in result['observed_writes'] if 0x400000<=int(w['address'],16)<0x7B0000), 'Untracked original-PE global write'
    head=int.from_bytes(post[QUEUE][48:52],'little');nodes=[]
    while head:
        assert HEAP+20<=head and head+92<=HEAP+0x10000 and head not in [n['address'] for n in nodes]
        z=post[HEAP][head-HEAP:head-HEAP+92]
        assert int.from_bytes(z[:4],'little')==0x66FE04
        n=dict(address=head,vertices=int.from_bytes(z[8:12],'little'),faces=int.from_bytes(z[12:16],'little'),
            texture=int.from_bytes(z[0x54:0x58],'little'))
        nodes.append(n);head=int.from_bytes(z[4:8],'little')
        assert len(nodes)<256
    visits=result['visited_instructions']
    if case['render']:
        assert visits.get('0x5a01a0')==1
        assert any(r['pc']=='0x5a01d4' and int(r['address'],16)==DEFINITION and r['size']==4 for r in result['observed_reads'])
        if selector in [0,5,0xFFFFFFFF]:assert not nodes and post[HEAP]==checks[HEAP]
        elif selector in [4,6]:assert len(nodes)==2 and all(n['vertices']==n['faces']==16 and n['texture']==0x5678 for n in nodes)
        elif selector==1:assert len(nodes)==1 and nodes[0]['vertices']==5 and nodes[0]['faces']==4
        elif selector in [2,7,8]:assert nodes and all(n['vertices']==4 and n['faces']==2 and n['texture']==0x1234 for n in nodes)
        else:raise AssertionError('Unreviewed selector')
    else:assert not nodes and post[HEAP]==checks[HEAP]
    allocations=int.from_bytes(post[CRT_STATE][4:8],'little');frees=int.from_bytes(post[CRT_STATE][12:16],'little')
    assert int.from_bytes(post[CRT_STATE][8:12],'little')==0 and allocations==frees
    allocated=[struct.unpack_from('<2I',post[CRT_STATE],0x100+8*i) for i in range(allocations)]
    freed=[int.from_bytes(post[CRT_STATE][0x4000+4*i:0x4004+4*i],'little') for i in range(frees)]
    assert sorted(p for _,p in allocated)==sorted(freed)
    return dict(selector=selector,descriptor_hex=expected.hex(),queue_objects=len(nodes),meshes=nodes,
        CRT_allocations=allocations,CRT_frees=frees,procedure_calls=visits.get('0x5a0030',0),
        instructions=result['instruction_count'],whole_prepared_regions=len(checks),whole_prepared_bytes=sum(map(len,checks.values())),
        raw_and_resource_mid_preserved=True,prepared_entity_registry=case['render'],live_scene_or_GPU=False)


def main():
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    rows=[]
    for case in selected:
        args,checks,source=build(case);path=path_for(args)
        if path.exists():
            saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
        else:
            result=cpu.run(EXE,args,list(checks.items()),STOP,DEFINITION,len(checks[DEFINITION]))
            saved=dict(case=case,input=args,result=result)
            path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        result=saved['result'];post=post_context(checks,result)
        if not result['success']:
            print(case['name'],result['emulation_error'],result['trace_tail'],flush=True)
        outcome=verify(case,args,checks,source,result)
        rows.append(dict(case=case['name'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=outcome))
        print(case['name'],'verified','selector',outcome['selector'],'meshes',outcome['queue_objects'],flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase129-procedural-native-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':main()
