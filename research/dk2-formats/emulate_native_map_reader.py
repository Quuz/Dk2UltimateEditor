"""Original56D2D0 -> original MyFile/Disc -> declared Windows API adapters.

World cases execute original51EC60 callbacks. Capture cases exercise the generic
native parser and report exactly its callback data without constructing a world.
Original PE code, IAT and vtables remain unchanged. Native World header callback
is NULL: World dimensions/backing arrays remain explicitly prepared.
"""
from pathlib import Path
import json,struct,sys
import probe_map_room_placement as world
import native_unicorn_mapreader as cpu
import native_file_api_fixture as win
from emulate_element_colors import region
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read
from native_map_cells import intermediate_cell

ROOT=Path(__file__).resolve().parent
REF,FILE,STREAM,CFG,DATA=0x18500000,0x18510000,0x18520000,0x18530000,0x18540000
STATE,LOG,READCODE,SEEKCODE=0x18600000,0x18610000,0x18620000,0x18630000
CAPTURE,CAPSTATE,CALLBACK=0x18800000,0x18710000,0x18720000
WALLRAW,WALLMID=0x18580000,0x18590000
BUFFER,BUFFER_BYTES=0x746EC0,65536
STOP=0x18900000
sha=world.old.sha

def cases():
    square=[(x,y) for y in range(1,4) for x in range(1,4)]
    base=dict(profile='capture',width=3,height=2,points=[],flags=0,fourth=0,truncate=0,declared=None,header_tag=101,data_tag=102,header_size=8,callback=True)
    rows=[('world-square',dict(profile='world',width=5,height=5,points=square)),
        ('world-fourthFF',dict(profile='world',width=5,height=5,points=square,fourth=255)),
        ('world-flags2',dict(profile='world',width=5,height=5,points=square,flags=2)),
        ('world-missing-diagonal',dict(profile='world',width=5,height=5,points=[pt for pt in square if pt!=(3,1)])),
        ('world-declared0',dict(profile='world',width=5,height=5,points=square,declared=0)),
        ('chunk-crossing',dict(width=129,height=128)),('chunk-fourthFF',dict(width=129,height=128,fourth=255)),
        ('truncated-last-fourth',dict(truncate=1)),('truncated-last-flags',dict(truncate=2)),
        ('empty-payload',dict(truncate=24)),('wrong-header-tag',dict(header_tag=100)),
        ('wrong-data-tag',dict(data_tag=103)),('unchecked-header-size',dict(header_size=0xA5A5A5A5)),
        ('skip-payload',dict(callback=False)),('skip-beyondEOF',dict(callback=False,declared=1000))]
    source=world.old.prior.SOURCE.parent/'AutoSaveMap.kld';data=source.read_bytes();width,height=struct.unpack_from('<II',data,20)
    rows.append(('source-AutoSaveMap',dict(width=width,height=height,source_file=str(source))))
    rows.append(('source-AutoSaveMap-opaque-edit',dict(width=width,height=height,source_file=str(source),patch_opaque_cell=(width*height)//2)))
    return [dict(base,**v,name=n) for n,v in rows]

def raw_file(c):
    if c.get('source_file'):
        data=bytearray(Path(c['source_file']).read_bytes())
        if 'patch_opaque_cell' in c:data[36+4*c['patch_opaque_cell']+3]^=0x80
        return bytes(data)
    cells=bytearray();points={tuple(pt) for pt in c['points']}
    for y in range(c['height']):
        for x in range(c['width']):
            i=y*c['width']+x
            if c['profile']=='world':
                terrain=1 if x in [0,c['width']-1] or y in [0,c['height']-1] else 22 if (x,y) in points else 3
                owner=2;flags=c['flags'] if terrain==22 else 0
            else:terrain=(17*i+1)&255;owner=(13*i+2)&255;flags=(7*i)&255
            cells+=bytes([terrain,owner,flags,c['fourth']])
    declared=len(cells) if c['declared'] is None else c['declared']
    head=struct.pack('<9I',100,4,len(cells)+36,c['header_tag'],c['header_size'],c['width'],c['height'],c['data_tag'],declared)
    return head+cells[:len(cells)-c['truncate']] if c['truncate'] else head+cells

def capture_callback(limit):
    code=bytearray(bytes.fromhex('535657a1')+win.d(CAPSTATE)+b'\x3D'+win.d(limit)+bytes.fromhex('7300'))
    # Indexed capture has a fixed valid capacity equal to declared cell count.
    jump_at=len(code)-1
    code+=bytes.fromhex('6bc0188db8')+win.d(CAPTURE)+bytes.fromhex('8b5c2410891f8b5c2414895f048b74241883c708b910000000f3a4ff05')+win.d(CAPSTATE)+b'\xB8'+win.d(1)+bytes.fromhex('5f5e5bc3')
    bad=len(code);code+=bytes.fromhex('31c05f5e5bc3');assert bad-(jump_at+1)<128;code[jump_at]=bad-jump_at-1
    return code

def build(c):
    if c['profile']=='world':
        args,_=world.build([]);memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
        excluded={world.old.prior.SCB,world.old.prior.PLAY,world.old.LOOKUP,world.old.QUEUE,world.old.VFX,world.OVERLAY,world.GFXNULL,world.SOUND_RELEASE,0x7FFEE000}
        # Ground3 is not ownable: native44FC40 replaces rawPlayerId2 by the
        # real neutral tag configured in6F2548. Every player tag must resolve.
        memory[0x6F2548]=bytearray(struct.pack('<H',8))
        for ident in range(1,9):
            world.put(memory[0x6EE540],ident*4,world.PLAYERS+(ident-1)*0x1125);memory[0x6ED540][ident]=1
        world.put(memory[world.PLAYERS],0x1125+0xB3F,9);world.put(memory[world.PLAYERS],7*0x1125+0xB3F,16)
        # Solid native perimeter avoids the non-solid placement helper's
        # unchecked exterior neighbour reads. No exterior guard is fabricated.
        p=world.old.prior
        for y in range(5):
            for x in range(5):
                if x in [0,4] or y in [0,4]:
                    at=16*(5*y+x);memory[p.MAP][at+10]=1;world.put(memory[p.MAP],at+8,8,2)
        raw=next(r for s in world.fmt.parse((p.SOURCE.parent/'Terrain.kwd').read_bytes()) if world.fmt.FAMILIES.get(s.signature)=='Terrain' for r in s.records if r[0x1D6]==1)
        memory[WALLRAW]=bytearray(raw);memory[WALLMID]=bytearray([165])*736
        args['before_calls'] += [dict(address='0x56eb60',registers=dict(ECX=hex(WALLMID)),stack_args=[]),
            dict(address='0x56ed20',registers=dict(ECX=hex(WALLMID)),stack_args=[WALLRAW]),
            dict(address='0x4f66e0',registers=dict(ECX=hex(world.old.TABLE+460)),stack_args=[WALLMID,0])]
        # Beforecalls are all native setup/loaders/constructors, no Map callback.
        assert all(call['address']!='0x51ec60' for call in args['before_calls'])
    else:
        memory={0x7FFEE000:bytearray([165])*0x4000,0:bytes(4)};excluded={0x7FFEE000}
        args=dict(program='DKII-DX.exe',registers=json.dumps(dict(ECX='0')),before_calls=[],allowed_external_IO_code=[])
    data=raw_file(c);memory[DATA]=bytearray(data)+bytes([165])*64
    memory[REF]=bytearray(win.d(FILE));memory[FILE]=bytearray([165])*(8+64)
    stream=bytearray(32);struct.pack_into('<I',stream,0,0x672FE8);struct.pack_into('<II',stream,12,win.HANDLE,12)
    memory[STREAM]=stream+bytes([165])*64
    cfg=bytearray(0x20);struct.pack_into('<I',cfg,0x18,(0x51EC60 if c['profile']=='world' else CALLBACK) if c['callback'] else 0)
    struct.pack_into('<I',cfg,0x1C,world.old.prior.WORLD if c['profile']=='world' else CAPSTATE);memory[CFG]=cfg
    memory[STATE]=bytearray(struct.pack('<5I',12,0,0,0,0));memory[LOG]=bytearray([165])*(64*20)
    memory[BUFFER]=bytearray([165])*(BUFFER_BYTES+64)
    memory[CAPSTATE]=bytearray(4);memory[CAPTURE]=bytearray([165])*(c['width']*c['height']*24+64)
    adapters=win.build(EXE.read_bytes(),source=DATA,size=len(data),state=STATE,log=LOG,read_code=READCODE,seek_code=SEEKCODE,ref_address=STREAM+8)
    for a,b in adapters.items():memory[a]=bytearray(b);args['allowed_external_IO_code'].append([a,len(b)]);excluded.add(a)
    memory[CALLBACK]=capture_callback(c['width']*c['height']);args['allowed_external_IO_code'].append([CALLBACK,len(memory[CALLBACK])]);excluded.add(CALLBACK)
    args['before_calls'] += [dict(address='0x5b6e40',registers=dict(ECX=hex(FILE)),stack_args=[]),dict(address='0x5b6f20',registers=dict(ECX=hex(FILE)),stack_args=[STREAM])]
    args.update(address='0x56d2d0',registers=json.dumps(dict(ECX='0')),stack_args=[REF,CFG],max_steps=3000000,
        memory=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()])),
        observe_reads=[[DATA,len(data)],[BUFFER,BUFFER_BYTES],[0,4096]],
        observe_writes=[[FILE,72],[STREAM,96],[STATE,20],[BUFFER,BUFFER_BYTES+64],[0,4096]])
    checks={a:bytes(b) for a,b in memory.items() if a not in excluded}
    spans=sorted((a,len(b)) for a,b in memory.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    return args,checks

def execute(c):
    args,checks=build(c);key=sha((json.dumps(args,sort_keys=True)+sha((ROOT/'native_unicorn_mapreader.py').read_bytes())).encode())[:12]
    path=ROOT/'ghidra'/f'phase139-native-map-reader-{key}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        result=cpu.run(EXE,args,list(checks.items()),STOP,STREAM,96)
        saved=dict(case=c,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result']
    if not r['success']:print(c['name'],r['emulation_error'],r['registers'],r['trace_tail'],r['before_calls'][-3:],flush=True)
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures']
    return args,checks,saved,path

def main():
    selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    for c in selected:
        args,checks,saved,path=execute(c);post=post_context(checks,saved['result'])
        print(c['name'],path.name,'EAX',saved['result']['registers']['EAX'],'state',struct.unpack('<5I',post[STATE]),'capture',int.from_bytes(post[CAPSTATE],'little'),flush=True)

if __name__=='__main__':main()
