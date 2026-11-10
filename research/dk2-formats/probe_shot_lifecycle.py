"""Original rawShot/native factory -> complete tick/end roots for damage5/6.

Reuse the validated two-target AoE scene. No source files or original code
modified. ProcessNONE is an explicit diagnostic raw record field when used.
"""
from pathlib import Path
import json,struct,sys
import emulate_area_damage as area
from emulate_element_colors import region
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
base=area.base;f=base.f;cpu=area.cpu
SOUND_UPDATE,SOUND_LOG=0x1B040000,0x1B040100
def build(c):
    scene=dict(area.cases()[0],mode=c.get('mode',5),damage=c.get('damage',80),data2=c.get('radius',2),repeats=0,health_delta=c.get('health_delta',0))
    args,_=area.build(scene)
    mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    if c.get('process_none',True):mem[base.RAW][0xC7]=0
    if 'flags' in c:area.put(mem[base.RAW],0xA8,c['flags'])
    # Full tick uses other renderer slots than the old creation/AoE scene.
    # Select their actual original implementations, not the resource-lookup
    # adapter with a different callee stack-cleanup contract.
    for slot in [0x64,0x80]:area.put(mem[f.GV],slot,int.from_bytes(area.pe_read(area.EXE.read_bytes(),0x670574+slot,4),'little'))
    # Existing audio IO returns a valid opaque handle. Whole tick also calls
    # positional update slot30(handle,XYZ*), absent from the old AoE scene.
    # This is an explicit audio adapter, not a replacement gameplay routine.
    mem[f.SV].extend(bytes(max(0,0x34-len(mem[f.SV]))));area.put(mem[f.SV],0x30,SOUND_UPDATE)
    code=bytearray(bytes.fromhex('ff05')+struct.pack('<I',SOUND_LOG)+bytes.fromhex('8b442404a3')+struct.pack('<I',SOUND_LOG+4)+bytes.fromhex('8b542408'))
    for off in [0,4,8]:code+=bytes.fromhex('8b4a')+bytes([off])+bytes.fromhex('890d')+struct.pack('<I',SOUND_LOG+8+off)
    code+=bytes.fromhex('c20800');mem[SOUND_UPDATE]=code;mem[SOUND_LOG]=bytearray(24)
    args['allowed_external_IO_code'] += [[SOUND_UPDATE,len(code)]]
    args['before_calls'][8]['stack_args'][-1]=c.get('creation_arg',0)
    args['before_calls'].append(dict(address='0x509860',registers=dict(ECX=hex(f.WORLD)),stack_args=[c.get('world_gate',0)]))
    args['before_calls'] += [dict(address='0x4ab180',registers=dict(ECX=hex(base.NODE)),stack_args=[]) for _ in range(c.get('pre_ticks',0))]
    args['address']='0x4ab180' if c.get('root','tick')=='tick' else ('0x4aaf70' if c['root']=='end' else '0x4b5c30')
    args['registers']=json.dumps(dict(ECX=hex(base.NODE if c.get('root','tick')!='factory' else area.TARGETS[0])))
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()]))
    args['observe_reads'] += [[base.NODE,371],[0,4096]]
    args['observe_writes'] += [[base.NODE,371],[0,4096]]
    excluded=[0x7FFEE000,f.GFX_CB,f.SOUND_CB,base.target.attack.EVENT,base.target.LOAD_GFX,base.target.LOAD_SOUND,SOUND_UPDATE]
    checks={a:bytes(b) for a,b in mem.items() if a not in excluded};checks[base.SHOT_VTABLE]=area.pe_read(area.EXE.read_bytes(),base.SHOT_VTABLE,0x100)
    return args,checks
def execute(c,verbose=True):
    args,checks=build(c);tag=area.sha(json.dumps(args,sort_keys=True).encode())[:12];p=ROOT/'ghidra'/f'phase148-shot-lifecycle-native-{tag}.json'
    if p.exists():saved=json.loads(p.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=cpu.run(area.EXE,args,list(checks.items()),f.OBS,base.NODE,371);saved=dict(case=c,input=args,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];post=post_context(checks,r);node=post[f.BLOCK][base.NODE-f.BLOCK:base.NODE-f.BLOCK+371]
    if verbose:print(json.dumps(dict(case=c,success=r['success'],error=r['emulation_error'],before=r['before_calls'],hp_requests=r['hp_requests'],HP=[int.from_bytes(post[a][0x40:0x44],'little') for a in area.TARGETS[:2]],
        first_flag=int.from_bytes(node[0x149:0x14D],'little'),tick_count=int.from_bytes(node[0x12D:0x131],'little'),shotHP=int.from_bytes(node[0x40:0x44],'little'),tag=int.from_bytes(node[4:6],'little'),state=int.from_bytes(node[0x16:0x1A],'little'),
        tail=r['trace_tail'],low_reads=[x for x in r['observed_reads'] if 0<int(x['address'],16)<4096][:3])),flush=True)
    return args,checks,saved,p
if __name__=='__main__':execute(dict(mode=int(sys.argv[1]) if len(sys.argv)>1 else 5,root=sys.argv[2] if len(sys.argv)>2 else 'tick'))
