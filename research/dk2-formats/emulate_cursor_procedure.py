"""Native raw type7 selector3 through actual generic graphics dispatcher.

Prepared resource IO, clock boundary, engine heap and Session/Bridge/World
context. Initially engine C5=0 (terrain deformation disabled); not a live game.
"""
from pathlib import Path
import json,math,struct,sys
import emulate_procedural_resources as base
import native_cursor_trace as cpu
from emulate_resistance import post_context
from emulate_element_colors import region
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
BRIDGE,SESSION,WORLD,COORDS,ANGLES,CLOCK=0x18200000,0x18210000,0x18220000,0x18230000,0x18240000,0x18250000
TERRAIN_RAW,TERRAIN_MID,TERRAIN_DEF,MAP,NAVDESC,NAV=0x18300000,0x18310000,0x18320000,0x18330000,0x18340000,0x18350000
GLOBALS=0x795720

def sha(b):return base.sha(b)
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(n*8))-1)).to_bytes(n,'little')
def f32(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def fb(v):return struct.unpack('<I',struct.pack('<f',v))[0]


def cases():
    seed=dict(family='Effects',record=27,mask=31,phase=0.5,texture=0x4321,frame_ms=1000,
        previous_ms=1000,direction=0,grid_option=0,repeats=1,coords=[9216,15360,-8192],offsets=[0,0,0],opaque=[0,0])
    variations=[('full-frame',{}),('mask-zero',dict(mask=0)),('mask-bit1',dict(mask=1)),('mask-bit2',dict(mask=2)),
        ('mask-bit4',dict(mask=4)),('mask-bit8',dict(mask=8)),('mask-bit16',dict(mask=16)),
        ('full-frame-allowed',dict(mask=63)),('phase-zero',dict(phase=0)),('phase-one',dict(phase=1)),
        ('pulse-decrease',dict(frame_ms=1100)),('pulse-wrap-low',dict(phase=0.01,frame_ms=10000)),
        ('pulse-increase',dict(direction=1,frame_ms=1100)),('pulse-wrap-high',dict(direction=1,phase=0.99,frame_ms=10000)),
        ('grid-option',dict(grid_option=1)),('shifted',dict(coords=[-1024,9728,-4096],offsets=[512,-256,128])),
        ('opaque-pair',dict(opaque=[0xFFFFFFFF,0x80000000])),
        ('Shot-source',dict(family='Shots',record=20)),
        ('deform-global-zero',dict(deform=True,cell_flags=0)),
        ('deform-Terrain-source',dict(deform=True,cell_flags=0x2000)),
        ('deform-Terrain-zero',dict(deform=True,cell_flags=0x2000,terrain_values=[0]*8)),
        ('deform-Terrain-minimal',dict(deform=True,cell_flags=0x2000,terrain_values=[1,0,0,0,1,0,0,0])),
        ('deform-Lean-high',dict(deform=True,cell_flags=0x2000,terrain_values=[0,255,255,255,0,255,255,255])),
        ('deform-Wibble-H',dict(deform=True,cell_flags=0x2000,terrain_values=[128,0,0,0,0,0,0,0])),
        ('deform-Wibble-V',dict(deform=True,cell_flags=0x2000,terrain_values=[0,0,0,0,128,0,0,0])),
        ('integer-frame',dict(coords=[8192,12288,-8192])),
        ('integer-frame-allowed',dict(coords=[8192,12288,-8192],mask=63)),
        ('integer-deform-source',dict(coords=[8192,12288,-8192],deform=True,cell_flags=0x2000))]
    return [dict(seed,**kw,name=name) for name,kw in variations]


def build(case):
    initial=dict(name=case['name'],family=case['family'],record=case['record'],render=False,union=[3,*case['opaque']])
    args,_,source=base.build(initial)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    put(mem[base.GFX],8,BRIDGE);put(mem[base.GFX],0x8E,case['texture']);put(mem[base.GFX],0xC5,0)
    bridge=bytearray(64);put(bridge,4,SESSION);mem[BRIDGE]=bridge
    session=bytearray(64);put(session,0x14,WORLD);mem[SESSION]=session
    world=bytearray(64);put(world,0,0x66E3EC);mem[WORLD]=world
    if case.get('deform'):
        import dk2_formats as fmt
        source_path=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Terrain.kwd'
        raw=bytearray(next(r for s in fmt.parse(source_path.read_bytes()) for r in s.records if len(r)==552 and r[0x1D6]==8))
        if 'terrain_values' in case:raw[0x1CE:0x1D6]=bytes(case['terrain_values'])
        mem[TERRAIN_RAW]=raw;mem[TERRAIN_MID]=bytearray(736);mem[TERRAIN_DEF]=bytearray(460)
        put(mem[base.GFX],0xC5,1)
        world=bytearray(0xA3D0);put(world,0,0x66E3EC)
        for o,v in [(4,SESSION),(0x9E6,32),(0x9EA,32),(0x9EE,MAP),(0x9F2,NAVDESC)]:put(world,o,v)
        mem[WORLD]=world;map_=bytearray(16*32*32)
        for cell in range(32*32):put(map_,16*cell+8,case['cell_flags'],2);map_[16*cell+10]=8
        mem[MAP]=map_;navdesc=bytearray(24);put(navdesc,8,32);put(navdesc,16,NAV);mem[NAVDESC]=navdesc;mem[NAV]=bytearray([8])*1024
        definitions=bytearray(256*4);put(definitions,8*4,TERRAIN_DEF);mem[0x6DA4A8]=definitions
        mem[0x6DA490]=bytearray(struct.pack('<I',32));mem[0x6DA494]=bytearray(struct.pack('<I',32))
        mem[0x6F25F9]=bytearray(8)
    mem[COORDS]=bytearray(struct.pack('<3i',*case['coords']));mem[ANGLES]=bytearray(6)
    render=mem[base.RENDER];put(render,0xAF,1,1);put(render,0x5B,case['mask'])
    struct.pack_into('<3h',render,0x51,*case['offsets'])
    mem.pop(0x795840);globals_=bytearray([165])*0x130
    put(globals_,0x124,case['previous_ms']);struct.pack_into('<f',globals_,0x128,case['phase'])
    mem[GLOBALS]=globals_;mem[0x6BE8EC]=bytearray(struct.pack('<I',case['direction']))
    mem[0x79D3F8]=bytearray(24) # Native clock takes timeGetTime branch.
    mem[CLOCK]=bytearray(b'\xb8'+struct.pack('<I',case['frame_ms'])+b'\xc3')
    mem[0x66C360]=bytearray(struct.pack('<I',CLOCK))
    mem[0x759AE8]=bytearray(struct.pack('<I',case['grid_option']))
    mem[0x759AD0]=bytearray(4);mem[0x75AEAF]=bytearray(4)
    calls=[dict(address='0x59f760',registers={},stack_args=[])]+args['before_calls']
    if case.get('deform'):
        calls += [dict(address='0x4f6570',registers={'ECX':hex(TERRAIN_DEF)},stack_args=[]),
            dict(address='0x56eb60',registers={'ECX':hex(TERRAIN_MID)},stack_args=[]),
            dict(address='0x56ed20',registers={'ECX':hex(TERRAIN_MID)},stack_args=[TERRAIN_RAW]),
            dict(address='0x4f66e0',registers={'ECX':hex(TERRAIN_DEF)},stack_args=[TERRAIN_MID,0])]
    calls += [dict(address='0x59b880',registers={'ECX':hex(base.GFX)},
        stack_args=[base.RENDER,COORDS,COORDS,0,ANGLES,0]) for _ in range(case['repeats'])]
    args['before_calls']=calls;args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['max_steps']=1000000
    args['allowed_external_IO_code'] += [[CLOCK,len(mem[CLOCK])]]
    args['observe_reads'] += [[GLOBALS,len(globals_)],[base.RENDER,len(render)],[BRIDGE,64],[SESSION,64],[WORLD,64]]
    if case.get('deform'):args['observe_reads'] += [[TERRAIN_DEF,460],[TERRAIN_RAW,552],[TERRAIN_MID,736]]
    args['observe_writes'] += [[GLOBALS,len(globals_)]]
    spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
    exclude=[base.SCB,0x7FFEC000,CLOCK,*base.providers()]
    checks={a:bytes(b) for a,b in mem.items() if a not in exclude}
    checks[0x66FE04]=pe_read(EXE.read_bytes(),0x66FE04,64)
    checks[0x670574]=pe_read(EXE.read_bytes(),0x670574,0x200)
    return args,checks,source


def path_for(args):
    tag=sha((json.dumps(args,sort_keys=True)+sha(Path(__file__).read_bytes())+sha((ROOT/'native_cursor_trace.py').read_bytes())).encode())[:12]
    return ROOT/'ghidra'/f'phase132-cursor-native-{tag}.json'


def expected_grid(case):
    # Original wrapper59C2A0 converts fixed-point current coordinates+offsets.
    pos=[f32((x+o)/4096) for x,o in zip(case['coords'],case['offsets'])];pos[2]=-pos[2]
    x,y=math.floor(pos[0]),math.floor(pos[1])
    divisor=struct.unpack('<f',pe_read(EXE.read_bytes(),0x6706A0 if case['grid_option'] else 0x670714,4))[0]
    step=struct.unpack('<f',pe_read(EXE.read_bytes(),0x670704,4))[0]
    bias=0 if case['grid_option'] else 0.5
    shift=struct.unpack('<d',pe_read(EXE.read_bytes(),0x670718,8))[0]
    return [[f32(x-ix*step),f32(y-iy*step),f32(iz/divisor+bias-2.0-shift)]
        for iz in range(3) for iy in range(3) for ix in range(3)]


def verify(case,args,checks,source,result):
    assert result['success'] and result['hit_return'] and not result['emulation_error']
    assert not result['protected_write_or_execution_failures'] and result['function_or_vtable_substitutions']==0
    assert all(c['returned'] for c in result['before_calls']) and len(result['before_calls'])==len(args['before_calls'])
    post=post_context(checks,result);visits=result['visited_instructions']
    for address in [0x59F760,0x59B880,0x59C2A0,0x5A01A0,0x59F3C0,0x5B5E90,0x5B5E00]:
        assert visits.get(hex(address),0)==(1 if address==0x59F760 else case['repeats']),hex(address)
    if case.get('deform'):
        assert visits.get('0x5a1070')==1 and visits.get('0x59e480')==1 and visits.get('0x59ed80')==1
        assert len(result['deform_captures'])==1
        assert len(result['terrain_samples'])==1
        raw=checks[TERRAIN_RAW];assert post[TERRAIN_RAW]==raw
        for ro,do in [(0x1CE,0x1A1),(0x1D2,0x1A5)]+[(0x1CF+i,0x1A2+i) for i in range(3)]+[(0x1D3+i,0x1A6+i) for i in range(3)]:
            assert post[TERRAIN_DEF][do]==raw[ro]
        expected_samples=bytearray.fromhex(result['terrain_samples'][0]['before_hex'])
        for z in range(3):
            for y in range(3):
                for x in range(3):
                    h=(raw[0x1CE]<<7) if case['cell_flags']&0x2000 else 0
                    v=(raw[0x1D2]<<7) if case['cell_flags']&0x2000 else 0
                    lean_h=f32(raw[0x1CF+z]/512) if case['cell_flags']&0x2000 else 0
                    lean_v=f32(raw[0x1D3+z]/512) if case['cell_flags']&0x2000 else 0
                    struct.pack_into('<2I2f',expected_samples,(z*9+y*3+x)*20,h,v,lean_h,lean_v)
        assert bytes.fromhex(result['terrain_samples'][0]['hex'])==expected_samples
    else:assert not visits.get('0x5a1070') and not visits.get('0x59e480')
    assert post[base.RAW]==checks[base.RAW] and post[base.RENDER]==checks[base.RENDER]
    for a in [BRIDGE,SESSION,WORLD,COORDS,ANGLES,0x66FE04,0x670574]:assert post[a]==checks[a]
    mid_offset=0xA8 if case['family']=='Shots' else 0xD4
    assert post[base.MID][mid_offset:mid_offset+84]==checks[base.RAW][0x20:0x74]
    definition=post[base.DEFINITION];assert int.from_bytes(definition[:4],'little')==3 and definition[10]==7
    assert definition[11:15]==(int.from_bytes(checks[base.RAW][0x60:0x64],'little')|1).to_bytes(4,'little')
    if case['family']=='Shots':assert int.from_bytes(definition[0x33:0x37],'little')==sum(post[base.MID])
    grid=expected_grid(case);assert len(result['lattice_captures'])==case['repeats']
    for capture in result['lattice_captures']:
        assert bytes.fromhex(capture['hex'])==b''.join(struct.pack('<3f',*v) for v in grid),(case['name'],capture,grid)
    edges=list(struct.iter_unpack('<3I',post[GLOBALS][:288]));assert len(edges)==24 and all(a<27 and b<27 for a,b,m in edges)
    selected=[(a,b,m) for a,b,m in reversed(edges) if (m&(case['mask']&255))==m]
    lines=result['line_calls'];assert len(lines)==len(selected)*case['repeats']
    red,blue=(0,f32(case['phase']*struct.unpack('<d',pe_read(EXE.read_bytes(),0x670758,8))[0])) if case['mask']&32 else (case['phase'],0)
    final_grid=list(struct.iter_unpack('<3f',bytes.fromhex(result['deform_captures'][0]['hex']))) if case.get('deform') else grid
    expected_lines=[list(final_grid[a])+list(final_grid[b])+[f32(0.03),red,0,blue] for a,b,m in selected]
    assert [r['arguments_u32'] for r in lines]==[[fb(x) for x in v] for v in expected_lines]*case['repeats'],case['name']
    head=int.from_bytes(post[base.QUEUE][48:52],'little');nodes=[]
    while head:
        assert base.HEAP+20<=head<head+92<=base.HEAP+0x10000 and head not in nodes
        node=post[base.HEAP][head-base.HEAP:head-base.HEAP+92]
        assert int.from_bytes(node[:4],'little')==0x66FE04
        assert struct.unpack_from('<2I',node,8)==(8,4)
        assert int.from_bytes(node[0x54:0x58],'little')==case['texture']&65535
        nodes.append(head);head=int.from_bytes(node[4:8],'little')
    assert len(nodes)==2*len(lines) and visits.get('0x59dd30',0)==len(lines)
    assert int.from_bytes(post[GLOBALS][0x120:0x124],'little')==case['texture']
    assert int.from_bytes(post[GLOBALS][0x124:0x128],'little')==case['frame_ms']
    elapsed=(case['frame_ms']-case['previous_ms'])&0xFFFFFFFF
    constant=struct.unpack('<f',pe_read(EXE.read_bytes(),0x670760 if case['direction'] else 0x670770,4))[0]
    phase_exact=case['phase']-elapsed*constant
    expected_phase=f32(min(1,phase_exact) if case['direction'] else max(0,phase_exact))
    expected_direction=0 if case['direction'] and phase_exact>1 else 1 if not case['direction'] and phase_exact<0 else case['direction']
    assert post[GLOBALS][0x128:0x12C]==struct.pack('<f',expected_phase)
    assert int.from_bytes(post[0x6BE8EC],'little')==expected_direction
    assert all(int(r['address'],16)==0 and r['size']==4 and pe_read(EXE.read_bytes(),int(r['pc'],16),2)==bytes.fromhex('64a1') for r in result['observed_reads'] if int(r['address'],16)<4096)
    assert all(any(a<=int(w['address'],16) and int(w['address'],16)+w['size']<=a+len(b) for a,b in checks.items())
        for w in result['observed_writes'] if 0x400000<=int(w['address'],16)<0x7B0000)
    return dict(selector=3,edge_table=[dict(a=a,b=b,mask=m) for a,b,m in edges],mask=case['mask'],selected_edges=len(selected),
        line_meshes=len(nodes),vertices_per_mesh=8,triangles_per_mesh=4,texture=case['texture']&65535,
        lattice_27_points_verified=True,line_arguments_independently_verified=True,
        phase_after=struct.unpack_from('<f',post[GLOBALS],0x128)[0],direction_after=int.from_bytes(post[0x6BE8EC],'little'),
        instruction_count=result['instruction_count'],whole_prepared_regions=len(post),whole_prepared_bytes=sum(map(len,post.values())),
        terrain_deformation=bool(case.get('deform')),raw_resource_and_render_preserved=True)


if __name__=='__main__':
    rows=[];selected=[c for c in cases() if len(sys.argv)==1 or c['name'] in sys.argv[1:]]
    for case in selected:
        args,checks,source=build(case);path=path_for(args)
        if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
        else:
            result=cpu.run(EXE,args,list(checks.items()),base.STOP,base.DEFINITION,len(checks[base.DEFINITION]))
            saved=dict(case=case,input=args,result=result);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
        if not saved['result']['success']:print(case['name'],saved['result']['emulation_error'],saved['result']['trace_tail'],flush=True)
        outcome=verify(case,args,checks,source,saved['result'])
        rows.append(dict(case=case['name'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=outcome))
        print(case['name'],'verified edges',outcome['selected_edges'],'phase',outcome['phase_after'],flush=True)
    if len(selected)==len(cases()):
        (ROOT/'phase132-cursor-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
