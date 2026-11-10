"""Full Pro/RC3 camera writers;Pro parser->callback->constructor->writer.

    File read/write, date, allocation and pointer-array insertion are fixtures.
    Parser431220,init42FD70,converter42FE20,callback49F550,constructor41CB70,
    writer4AB210 and getter4826F0 are original instructions. RC3 writer4B0A20
    and getter488030 also original. Four ordered-bank cases execute full39
    game callbacks5215F0 and stores449A30,with provider/checksum fixtures.
    No GUI,game process or original-file writes.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_structures import load_abs
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from emulate_cameras import raw_record
from native_camera_catalogue import model_from_raw,writer_section,bank_projection
from native_cameras import intermediate
from inventory import DATA
import dk2_formats as fmt
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
DATE=bytes.fromhex('ea070a1a00000c2238')

def real_records():
    return [r for s in fmt.parse((DATA/'GlobalVariables.kwd').read_bytes()) if s.signature==190 for r in s.records if fmt.u32(r)==205]

def cases():
    rows=[]
    for program in ['pro','rc3']:
        for seed in [0,165,255,'pattern']:
            for slots in [[],[1],[2,1,2]]:rows.append(dict(name=f'writer-{program}-{seed}-{len(slots)}',kind='writer',program=program,seed=seed,slots=slots))
        rows.append(dict(name=f'writer-{program}-real39',kind='writer',program=program,seed=165,real=True))
    for i in range(len(real_records())):rows.append(dict(name=f'chain-real-{i}',kind='chain',record_index=i,seed=165))
    for size,tail in [(0,0),(48,255),(72,255),(82,255),(83,255),(84,0),(84,255)]:rows.append(dict(name=f'chain-size-{size}-tail-{tail}',kind='chain',size=size,tail=tail,seed='pattern'))
    for seed in [0,165,255]:
        for flag in [0,1]:rows.append(dict(name=f'registration-{seed}-{flag}',kind='registration',seed=seed,flag=flag))
    for spec in [4,5]:
        for reverse in [False,True]:rows.append(dict(name=f'bank-{spec}-{reverse}',kind='bank',spec=spec,reverse=reverse))
    return rows

def build_case(c):
    mem=[];regs={};program='DK2 Editor pro.exe'
    if c['kind']=='bank':
        # Unrelated Terrain entry is only a harness jump;all39 callback/store
        # invocations execute fulloriginal5215F0/449A30 bodies unpatched.
        program='DKII-DX.exe';entry=0x4F66E0;rs=real_records()[::-1] if c['reverse'] else real_records();bank=bytes([165])*0xC00;expected=bank_projection(rs,c['spec'],bank);mid=b''.join(intermediate(r) for r in rs)
        loop=b'\xBE'+pack32(0x10020000)+b'\xBF'+pack32(len(rs));start=0x10022000+len(loop)
        loop+=b'\x68'+pack32(0x10005000)+b'\x56';at=0x10022000+len(loop);loop+=b'\xE8'+struct.pack('<i',0x5215F0-(at+5));loop+=bytes.fromhex('83c40881c6')+pack32(152)+b'\x4F';at=0x10022000+len(loop);loop+=bytes.fromhex('0f85')+struct.pack('<i',start-(at+6));loop+=compare_code(0x10001000,0x10003000,0xC00)+load_abs(3,0x1000B000)+END
        child=bytearray(0x14);struct.pack_into('<I',child,0x10,0x10008000);vt=bytearray(0xD4);struct.pack_into('<I',vt,0xD0,0x1000A000)
        mem=[region(entry,jump(entry,0x10022000)),region(0x10020000,mid),region(0x10022000,loop),region(0x10001000,bank),region(0x10003000,expected),region(0x10005000,pack32(0)+pack32(0x10007000)),region(0x10007000,child),region(0x10008000,pack32(0x10009000)),region(0x10009000,vt),region(0x1000A000,return_const(0x10001000)),region(0x1000B000,bytes(4)),region(0x5108C0,bytes.fromhex('ff05')+pack32(0x1000B000)+bytes.fromhex('c20800')),region(0x759AEC,pack32(c['spec']))];want=dict(EAX=1,ECX=0,EBX=39)
    elif c['kind']=='registration':
        # Bypass string/file setup;execute real context init,conditional registration.
        entry=0x47D5F0;sp=0x7FFF0000;ctx=sp+0x18;adapter=b'\xB9'+pack32(ctx)+b'\xE8'+struct.pack('<i',0x42C730-(0x10000000+10))+bytes.fromhex('b0')+bytes([c['flag']])+jump(0x10000000+12,0x47D6E4)
        readout=load_abs(0,ctx+0xA8)+load_abs(3,ctx+0xD8)+load_abs(2,ctx+0xDC)+END
        mem=[region(entry,jump(entry,0x10000000)),region(0x10000000,adapter),region(0x47D709,readout),region(sp,bytes([c['seed']])*0x180)];want=dict(EAX=0x49F550 if c['flag'] else 0,EBX=0x49EC50,EDX=0x49EC90)
    else:
        rc3=c.get('program')=='rc3';program='DK2-Editor-rc3.exe' if rc3 else program;writer=0x4B0A20 if rc3 else 0x4AB210;datefn=0x4B0B30 if rc3 else 0x4AB320;countoff=0x60C if rc3 else 0x610;context=0x10000000
        stack=bytes((i*73+19)&255 for i in range(0x400)) if c['seed']=='pattern' else bytes([c['seed']])*0x400
        # Writer frameSP=S-4C;header startsSP+1C=S-30,emits56,with1-byte
        # gaps atheader37 and47. Header overlap with stack args is controlled.
        header_seed=stack[0x3D0:]+bytes(8);assert len(header_seed)==56
        state=bytearray(0x680);vt=bytearray(0x3C);struct.pack_into('<I',vt,0x38,0x1000A000)
        if c['kind']=='writer':
            if c.get('real'):
                model_data=[r[8:] for r in real_records()];count=len(model_data);ptrs=b''.join(pack32(0x11000000+i*256) for i in range(count));mem.extend(region(0x11000000+i*256,pack32(0xDEADC0DE)+r) for i,r in enumerate(model_data))
            else:
                models={n:bytes((i*31+n)&255 for i in range(84)) for n in [1,2]};slots=c['slots'];model_data=[models[n] for n in slots];count=len(slots)
                mem.extend([region(0x10002000,pack32(0xDEADC0DE)+models[1]),region(0x10003000,pack32(0xDEADC0DE)+models[2])]);ptrs=b''.join(pack32(0x10002000+(n-1)*0x1000) for n in slots) or bytes(4)
            entry=writer;struct.pack_into('<2I',state,countoff-4,0x10001000,count)
            mem.append(region(0x7FFF0000,pack32(0x10016000)+pack32(0x1000C000)));regs=dict(ECX=hex(context));expected_calls=3+2*count
        else:
            raw=real_records()[c['record_index']] if 'record_index' in c else raw_record(c['size'],opaque=c['tail']);model_data=[model_from_raw(raw)];count=1;entry=0x431220;ptrs=bytes(4);dispatch=bytearray(0xB4);struct.pack_into('<I',dispatch,0xA8,0x49F550)
            # Parser return -> writer,then common readout. Discard parser cdecl
            # arguments before invoking thiscall writer with its own output.
            header_seed=bytearray(56);header_seed[37]=165;header_seed[47]=90
            adapter=bytes.fromhex('83c40cc605')+pack32(0x7FFEFFFD)+bytes([165])+bytes.fromhex('c605')+pack32(0x7FFF0007)+bytes([90])+b'\xB9'+pack32(0x54F550)+b'\x68'+pack32(0x1000C000)+b'\x68'+pack32(0x10016000)
            adapter+=jump(0x10017000+len(adapter),writer)
            # allocator returns known88-byte object;append fixture links it into
            # original global model context,not a substitute camera projection.
            append=bytes.fromhex('8b442404a3')+pack32(0x10001000)+bytes.fromhex('c705')+pack32(0x54F550+0x60C)+pack32(0x10001000)+bytes.fromhex('c705')+pack32(0x54F550+0x610)+pack32(1)+b'\xB8'+pack32(1)+bytes.fromhex('c20400')
            # Existing native read ABI fromphase40:result/status,count anddest.
            io=bytes.fromhex('5657be')+pack32(0x10005000)+bytes.fromhex('8b7c24148b4c2418890d')+pack32(0x10011008)+bytes.fromhex('f3a48b44240cc700000000008b54241c8b0d')+pack32(0x10011008)+bytes.fromhex('890a5f5ec3')
            mem.extend([region(0x10002000,bytes([165])*88),region(0x10005000,bytes(raw[8:]).ljust(84,b'\0')),region(0x10006000,dispatch),region(0x10007000,raw[:8]),region(0x10008000,pack32(1)),region(0x10017000,adapter),region(0x4D8A63,return_const(0x10002000)),region(0x482750,append),region(0x4B7F10,io),region(0x7FFF0000,pack32(0x10017000)+pack32(0x10007000)+pack32(0x10008000)+pack32(0x10006000))]);expected_calls=5
            # Writer uses the same global context the original callback sets.
            mem.append(region(0x54F550,state))
        expected=writer_section(model_data,header_seed,DATE)
        capture=bytes.fromhex('56578b74240c8b4c2410ff05')+pack32(0x10011000)+bytes.fromhex('8b3d')+pack32(0x10011004)+bytes.fromhex('01cf893d')+pack32(0x10011004)+bytes.fromhex('29cff3a45f5eb801000000c20800')
        date_stub=bytes.fromhex('8b442404c700')+DATE[:4]+bytes.fromhex('c74004')+DATE[4:8]+bytes.fromhex('c64008')+DATE[8:]+bytes.fromhex('c20400')
        readout=bytes.fromhex('0fb6d0')+compare_code(0x10012000,0x10014000,len(expected))+load_abs(3,0x10011000)+load_abs(5,0x10011004)+END
        mem.extend([region(context,state),region(0x10001000,ptrs),region(0x1000A000,capture),region(0x1000C000,pack32(0x1000D000)),region(0x1000D000,vt),region(0x10011000,pack32(0)+pack32(0x10012000)+pack32(0)),region(0x10012000,bytes(0x1000)),region(0x10014000,expected),region(0x10016000,readout),region(datefn,date_stub),region(0x7FFEFC00,stack)])
        want=dict(EAX=1,ECX=0,EBX=expected_calls,EBP=0x10012000+len(expected),EDX=1)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program=program,address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*want,'ESP','EIP'])),want

async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase49-emulation-camera-catalogue-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase49-camera-catalogue-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
