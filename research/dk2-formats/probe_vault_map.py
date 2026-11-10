"""Natural Z0 / enclosing CMap discovery; results not accepted until audited."""
import hashlib
import json
import struct
import sys
from pathlib import Path
import probe_mana_vault_factory as source
import native_unicorn_declared_imports as cpu
import fixture_read_probe as imported
from emulate_element_colors import region
from build_effect_semantics import EXE

ROOT=Path(__file__).resolve().parent
f=source.f


def add_import(args,memory):
    memory.pop(imported.ADDRESS,None)
    memory[imported.COUNTER]=bytearray(8)
    regions=[region(a,b) for a,b in memory.items()]
    bounds=imported.allowed_ranges(EXE.read_bytes(),regions)
    memory[imported.ADDRESS]=bytearray(imported.adapter(bounds))
    args['allowed_external_IO_code']=[[a,len(memory[a])] for a in [f.GFX_CB,f.SOUND_CB,imported.ADDRESS]]
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()]))


def build_z0(ident=101):
    args,e,_,case=source.build(ident,0)
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    f.put(memory[f.ARGS],8,0)
    add_import(args,memory)
    checks={a:bytes(b) for a,b in memory.items() if a not in [0x7FFEF000,f.GFX_CB,f.SOUND_CB,imported.ADDRESS]}
    return args,e,checks


def build_map(c=None):
    import emulate_navigation_map as nav
    import native_terrain_catalogue as terrain
    import dk2_formats as fmt
    c=dict(dict(new_terrain=32,old_terrain=3,old_owner=8,new_owner=7,old_object=False),**(c or {}))
    object_id={31:101,32:108}[c['new_terrain']]
    args,e,_,case=source.build(object_id,0)
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    nc=next(c for c in nav.cases() if c['name']=='raw-ground')
    na,_=nav.build(nc)
    nm={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(na['memory'])['regions']}
    memory[f.WORLD][0x9DA:0x15A0]=nm[f.WORLD][0x9DA:0x15A0]
    # Native navigation object and its initialized callback data, no actor copy.
    for a in [0x10400000,0x10410000,0x10430000,0x6ED408,0x6ECAE0,0x6EC9D0]:
        memory[a]=nm[a]
    for o,v,n in [(0x7239,0,1),(0x7243,0xFFFF,2)]:f.put(memory[f.WORLD],o,v,n)
    # Original message queue reads its World backpointer before its UI gate.
    # Tag0 must remain NULL: native cell-list traversal uses it as its terminator.
    f.put(memory[f.WORLD],0x2E58,f.WORLD)
    other=0x100A0000;memory[other]=bytearray(0x1200)
    f.put(memory[0x6EE540],8*4,other);memory[0x6ED540][8]=1
    f.put(memory[other],0xB3F,1)
    table=0x10800000;memory[table]=bytearray(460*33)
    cache=bytearray(1024)
    for i in range(33):
        memory[table][i*460+0x1C4]=i;f.put(cache,i*4,table+i*460)
    memory[0x6DA4A8]=cache;memory[0x6DA4A4]=bytearray(struct.pack('<I',f.WORLD))
    memory[0x6F2548]=bytearray(struct.pack('<H',8))
    for o,v in [(0x15A4,table),(0x15A8,33),(0x15AC,33)]:f.put(memory[f.WORLD],o,v)
    for i in range(32*32):memory[f.MAP][16*i+10]=3
    cell=(6*32+5)*16
    f.put(memory[f.MAP],cell+8,c['old_owner'],2)
    f.put(memory[f.MAP],cell+10,c['old_terrain'],1)
    calls=args['before_calls'][:4]
    if c['old_object']:
        memory[source.RAW]=bytearray(source.states.source_record(101))
        memory[f.DEFINITION].extend(bytes(285))
        f.put(memory[f.WORLD],0x15FC,2)
        memory[f.BLOCK].extend(bytes([0xA5])*(1280-len(memory[f.BLOCK])))
        f.put(memory[f.BLOCK],0,1024)
        raw2,mid2=source.RAW+0x2000,source.MID+0x2000
        memory[raw2]=bytearray(source.states.source_record(108));memory[mid2]=bytearray([0xA5])*1022
        calls += [dict(address='0x56d4f0',registers=dict(ECX=hex(mid2)),stack_args=[]),
                  dict(address='0x56d620',registers=dict(ECX=hex(mid2)),stack_args=[raw2]),
                  dict(address='0x49dd60',registers=dict(ECX=hex(f.DEFINITION+285)),stack_args=[mid2,0])]
        f.put(memory[f.ARGS],8,0)
        calls.append(dict(address='0x4b7a90',registers=dict(ECX=hex(f.WORLD+8)),stack_args=[{31:101,32:108}[c['old_terrain']],c['old_owner'],f.ARGS,f.OUT+4]))
        e.update(node=f.BLOCK+1024-2*408+12,old_node=f.BLOCK+1024-408+12,entity_id=101)
    records=[b for s in fmt.parse((ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Terrain.kwd').read_bytes())
             if s.signature==0x6E for b in s.records]
    for j,ident in enumerate([3,31,32]):
        raw=next(b for b in records if b[0x1D6]==ident)
        ra,mi=0x10810000+j*0x1000,0x10820000+j*0x1000
        memory[ra]=bytearray(raw);memory[mi]=bytearray([0xA5])*736
        calls += [dict(address='0x56eb60',registers=dict(ECX=hex(mi)),stack_args=[]),
                  dict(address='0x56ed20',registers=dict(ECX=hex(mi)),stack_args=[ra]),
                  dict(address='0x4f66e0',registers=dict(ECX=hex(table+ident*460)),stack_args=[mi,0])]
    calls.append(dict(address='0x44fc40',registers=dict(ECX=hex(f.WORLD+0x9DA)),stack_args=[5,6,c['new_terrain'],c['new_owner'],0,0]))
    args['before_calls']=calls
    args['registers']=json.dumps(dict(ECX=hex(e['node'])))
    add_import(args,memory)
    checks={a:bytes(b) for a,b in memory.items() if a not in [0x7FFEF000,f.GFX_CB,f.SOUND_CB,imported.ADDRESS]}
    args['observe_reads'] += [[table,33*460],[0,4096]]
    args['observe_writes'] += [[f.WORLD,0x8000],[f.MAP,16384]]
    return args,e,checks


def main():
    map_case='map' in sys.argv
    args,e,checks=build_map(dict(old_terrain=31,old_object=True)) if 'replace' in sys.argv else build_map() if map_case else build_z0()
    result=cpu.run(EXE,args,list(checks.items()),f.OBS,e['node'],393)
    tag=hashlib.sha256(json.dumps(args,sort_keys=True).encode()).hexdigest()[:10]
    path=ROOT/'ghidra'/f'phase112-{"map" if map_case else "z0"}-discovery-{tag}.json'
    path.write_text(json.dumps(dict(input=args,expected_factory=e,result=result),indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in result.items() if k not in ['visited_instructions','captured_hex','context_mismatches','observed_reads','observed_writes','stack_at_stop_hex','declared_import']}))
    if result['success']:
        obj=bytes.fromhex(result['captured_hex'])
        print('POSITION',struct.unpack_from('<3i',obj,0x1A),'STATE',struct.unpack_from('<I',obj,0x13A)[0])
    print('Changed contexts',[(r['address'],len(r['differences'])) for r in result['context_mismatches']])


if __name__=='__main__':main()
