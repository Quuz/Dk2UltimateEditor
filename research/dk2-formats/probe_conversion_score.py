"""Phase192 discovery of Variables233 in the original conversion-score path."""
from pathlib import Path
import json,struct,sys
import emulate_temple_creature_entry as fixture
import native_unicorn_mapreader as cpu
from emulate_element_colors import region
ROOT=Path(__file__).resolve().parent
EXE,sha=fixture.EXE,fixture.sha
RAW,RETURN=0x1A000000,0x1A010000
OLD_COUNTS,NEW_COUNTS=0x1A020000,0x1A030000


def build(c):
    base=dict(fixture.cases()[0],room_kind=8,room=True)
    mem,calls=fixture.memory_and_calls(base)
    mem={a:bytearray(b) for a,b in mem.items()}
    # Genuine CPlayer species-count getter4BAA70 requires a sorted13-byte
    # species row. The bare temple fixture never changed ownership before.
    for owner,counts,n in [(fixture.OWNER,OLD_COUNTS,1),(fixture.OWNER+0x2000,NEW_COUNTS,0)]:
        fixture.put(mem[owner],4,2 if owner==fixture.OWNER else 3,2)
        fixture.put(mem[owner],0x4E,1);fixture.put(mem[owner],0x52,counts)
        mem[counts]=bytearray(13);mem[counts][0]=5;fixture.put(mem[counts],9,n)
        fixture.put(mem[owner],0x10FC,1,1)
    fixture.put(mem[fixture.OWNER],6,100,2)
    fixture.put(mem[fixture.OWNER],0x16,1)
    fixture.put(mem[fixture.OWNER],0x10F8,1,1)
    mem[0x6DC424]=bytearray(struct.pack('<I',fixture.WORLD))
    mem[0x6E5054]=bytearray(struct.pack('<I',fixture.WORLD))
    fixture.put(mem[fixture.ROOM],0x1D,3,2)
    fixture.put(mem[fixture.OWNER+0x2000],0x10FC,c.get('player_type',3),1)
    fixture.put(mem[fixture.WORLD],0x72A2,c.get('score',10))
    mem[0x75AD4B]=bytearray(struct.pack('<I',c.get('mode',4)))
    raw=struct.pack('<4I',233,c.get('value',40)&0xFFFFFFFF,0x11223344,0x55667788)
    if c.get('source_record'):
        import dk2_formats as fmt
        data=(fixture.SOURCE.parent/'GlobalVariables.kwd').read_bytes()
        raw=next(r for s in fmt.parse(data) if s.signature==230 for r in s.records if int.from_bytes(r[:4],'little')==233)
    mem[RAW]=bytearray(bytes(16)+raw+bytes([165])*64)
    mem[0x6F29E9]=bytearray([165])*4
    calls.append(dict(address='0x51cd90',registers=dict(ECX=hex(fixture.WORLD)),stack_args=[RAW]))
    calls.append(dict(address='0x4f2800',registers=dict(ECX=hex(fixture.ROOM)),stack_args=[fixture.ACTOR]))
    args=dict(program='DKII-DX.exe',address='0x4e52f0',registers=json.dumps(dict(ECX=hex(fixture.ROOM))),
        memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),before_calls=calls,max_steps=400000,
        allowed_external_IO_code=[[fixture.SCB,len(mem[fixture.SCB])],[fixture.PLAY,len(mem[fixture.PLAY])]],
        observe_reads=[[0x6F29E9,4],[0,4096]],observe_writes=[[fixture.WORLD+0x72A2,4],[fixture.ACTOR+0x28,6],[0,4096]])
    return args,mem


def main():
    c=json.loads(sys.argv[1]) if len(sys.argv)>1 else {}
    args,mem=build(c);tag=sha(json.dumps(args,sort_keys=True).encode())[:12]
    f=ROOT/'ghidra'/f'phase192-conversion-discovery-{tag}.json'
    if f.exists():s=json.loads(f.read_text());assert s['input']==args
    else:
        r=cpu.run(EXE,args,[(fixture.OWNER,bytes(mem[fixture.OWNER])),(fixture.OWNER+0x2000,bytes(mem[fixture.OWNER+0x2000]))],fixture.STOP,fixture.WORLD,len(mem[fixture.WORLD]))
        s=dict(case=c,input=args,result=r);f.write_text(json.dumps(s,indent=2),encoding='utf8')
    r=s['result'];world=bytes.fromhex(r['captured_hex'])
    print(json.dumps(dict(file=str(f.relative_to(ROOT)),success=r['success'],error=r['emulation_error'],score=int.from_bytes(world[0x72A2:0x72A6],'little'),
        failures=r['protected_write_or_execution_failures'],before=r['before_calls'][-4:],reads=r['observed_reads'][-8:],writes=r['observed_writes'][-8:],tail=r['trace_tail'][-20:])),flush=True)


if __name__=='__main__':main()
