"""FullRC3APapply/DDX,fillfragment anddirectlowBYTEflagsetter.

MFCUpdateData/modal/DDXhelpersfixtures;originalall5settersrun. Whole60model
comparedforapply;fillcompareswholepanel;DDXcaptured(control,pointer)pairs.
NoGUIororiginalfilewrites.
"""
from pathlib import Path
import asyncio,hashlib,json,os,struct,sys
from mcp import ClientSession,StdioServerParameters
from mcp.client.stdio import stdio_client
from emulate_element_colors import region,pack32,jump
from emulate_placed_objects import return_const
from emulate_hero_parties import compare_code
from native_rc3_action_points import MASKS,CONTROL_IDS,panel_fields,apply_panel
ROOT=Path(__file__).resolve().parent
END=b'\x68'+pack32(0xDEADBEEF)+b'\xC3'
def cases():
    rows=[]
    for flags in [0,1,4,8,16,32,64,128,255]:rows.append(dict(name=f'fill-{flags}',kind='fill',flags=flags))
    for pat in range(32):rows.append(dict(name=f'apply-{pat}',kind='apply',flags=0x86,checks=[int(bool(pat&(1<<i))) for i in range(5)],wait=32767,index=2))
    for checks in [[2,0xFFFFFFFF,1,0,1],[1,0,2,1,0xFFFFFFFF],[0]*5,[1]*5]:rows.append(dict(name=f'apply-nonbool-{len(rows)}',kind='apply',flags=255,checks=checks,wait=0xFFFF1234,index=1))
    for flags in [0,255]:
        for value in [0,1,2,255,256,0xFFFFFFFF]:rows.append(dict(name=f'setter-{flags}-{value}',kind='setter',flags=flags,value=value))
    rows.append(dict(name='ddx-five-bindings',kind='ddx'))
    for flags in [0,1,8,0x80,0x100,0xFFFF]:
        for wait in [0,65535]:rows.append(dict(name=f'codec-{flags}-{wait}',kind='codec',flags=flags,wait=wait))
    for flags in [0,8,0x86,255]:
        for nxt in [[0,0],[2,0],[0,2],[2,1]]:
            for seed in [0,165]:rows.append(dict(name=f'writer-{flags}-{nxt[0]}-{nxt[1]}-{seed}',kind='writer',flags=flags,nexts=nxt,seed=seed))
    for flags in [0,1,8,9,255,0xFF08]:
        for legacy in [False,True]:
            for present in [False,True]:rows.append(dict(name=f'loader-{flags}-{int(legacy)}-{int(present)}',kind='loader',flags=flags,legacy=legacy,present=present))
    for flags in [0,8,0xFF08,65535]:
        for size in [0,16,18,19,20,24,56]:rows.append(dict(name=f'parser-{flags}-{size}',kind='parser',flags=flags,size=size,xy=[3,4,8,9]))
    for xy in [[3,4,0,2],[0xFFFFFFFF,0x10000,0x10001,0xFFFFFFFF]]:rows.append(dict(name=f'parser-coords-{len(rows)}',kind='parser',flags=8,size=56,xy=xy))
    return rows
def build_case(c):
    mem=[];regs={};panel=bytearray(0x94);model=bytearray((i*37+19)&255 for i in range(60));model[0x38]=c.get('flags',0)&255;struct.pack_into('<H',model,0x36,500);struct.pack_into('<I',panel,0x7C,0x10001000);struct.pack_into('<I',panel,0x84,0x10002000);struct.pack_into('<I',panel,0x64,c.get('index',2));entry=0x432D30
    if c['kind']=='apply':
        struct.pack_into('<I',panel,0x60,c['wait'])
        for i,n in enumerate(c['checks']):struct.pack_into('<I',panel,0x68+4*i,n)
        wantbuf=bytearray(model);out=apply_panel(c['flags'],c['wait'],c['checks'],[0,7,255],c['index']);struct.pack_into('<H',wantbuf,0x36,out['wait_word']);wantbuf[0x34]=out['next_id'];wantbuf[0x38]=out['flags'];readout=compare_code(0x10001000,0x10006000,len(model))+END
        mem=[region(0x10000000,panel),region(0x10001000,model),region(0x10002000,bytes([0,7,255])),region(0x10006000,wantbuf),region(0x10003000,readout),region(0x4E95DF,return_const(1,4)),region(0x4E6C17,b'\xC3'),region(0x7FFF0000,pack32(0x10003000))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0)
    elif c['kind']=='fill':
        entry=0x432860;expected=bytearray(panel);v=panel_fields(c['flags'],500);struct.pack_into('<I',expected,0x60,500)
        for i,n in enumerate(v['checkboxes']):struct.pack_into('<I',expected,0x68+4*i,n)
        # fragmenthasPUSH0passedtoUpdateData;exitadapterrestoresitbeforeend.
        readout=bytes.fromhex('83c404')+compare_code(0x10000000,0x10006000,len(panel))+END
        mem=[region(0x10000000,panel),region(0x10001000,model),region(0x10006000,expected),region(entry,jump(entry,0x432974)),region(0x4329C1,readout)];regs=dict(ESI='0x10000000');want=dict(EAX=1,ECX=0)
    elif c['kind']=='setter':
        entry=0x425DB0;expected=bytearray(model);expected[0x38]=c['flags']|8 if c['value']&255 else c['flags']&~8
        mem=[region(0x10000000,model),region(0x10006000,expected),region(0x10003000,compare_code(0x10000000,0x10006000,len(model))+END),region(0x7FFF0000,pack32(0x10003000)+pack32(c['value']))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0)
    elif c['kind']=='ddx':
        entry=0x432A00;check=bytes.fromhex('8b15')+pack32(0x10003000)+bytes.fromhex('8d14d5')+pack32(0x10003010)+bytes.fromhex('8b44240889028b44240c894204ff05')+pack32(0x10003000)+bytes.fromhex('c20c00');expected=b''.join(struct.pack('<II',control,0x10000068+i*4) for i,control in enumerate(CONTROL_IDS));readout=compare_code(0x10003010,0x10006000,len(expected))+bytes.fromhex('8b1d')+pack32(0x10003000)+END
        mem=[region(0x10000000,panel),region(0x10003000,bytes(0x40)),region(0x10004000,readout),region(0x10005000,bytes(16)),region(0x10006000,expected),region(0x4F1809,check),region(0x4F172A,bytes.fromhex('c20c00')),region(0x4F192E,bytes.fromhex('c20c00')),region(0x4F1A5C,bytes.fromhex('c21000')),region(0x7FFF0000,pack32(0x10004000)+pack32(0x10005000))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EBX=5)
    elif c['kind']=='codec':
        entry=0x425E70;expected=bytearray(model);struct.pack_into('<H',expected,0x36,c['wait']);expected[0x38]=c['flags']&255
        adapter=b'\xB9'+pack32(0x10000000)+b'\xE8'+struct.pack('<i',0x425E50-(0x10003005+5))+b'\xA3'+pack32(0x10004000)+compare_code(0x10000000,0x10006000,60)+bytes.fromhex('8b1d')+pack32(0x10004000)+END
        mem=[region(0x10000000,model),region(0x10003000,adapter),region(0x10004000,bytes(4)),region(0x10006000,expected),region(0x7FFF0000,pack32(0x10003000)+pack32(c['wait']|(c['flags']<<16)))];regs=dict(ECX='0x10000000');want=dict(EAX=1,ECX=0,EBX=c['wait']|((c['flags']&255)<<16))
    elif c['kind']=='writer':
        from emulate_action_points import build_case as old_case
        from native_action_points import writer_payload,editor_saved_flags
        import re
        pairs=json.loads((ROOT/'phase51-writer-helper-pairs.json').read_text())
        mapping={p['pro']:p['rc3'] for p in pairs}
        for p in pairs:
            text=(ROOT/'ghidra'/f"phase51-rc3-{p['rc3']:x}-disassemble_function.txt").read_text()
            cleanups={int(x,0) if x else 0 for x in re.findall(r'^\w{8}: RET(?: (0x[0-9a-f]+))?\s*$',text,re.M)}
            assert cleanups=={p['cleanup']},p
        args,want=old_case(dict(kind='writer',nexts=c['nexts'],seed=c['seed']))
        mem=json.loads(args['memory'])['regions'];entry=0x499DD0;regs=json.loads(args['registers'])
        for r in mem:
            a=int(r['address'],16);b=bytearray.fromhex(r['hex'])
            if a in mapping:r['address']=hex(mapping[a])
            if a==0x10000000:struct.pack_into('<II',b,0xB9C,0x10001000,2)
            if a in [0x10002000,0x10003000]:b[0x38]=c['flags']
            if a==0x10009000:
                b=bytearray(b''.join(writer_payload(dict(bounds=[3+i,4,8+i,9],wait=500,flags=c['flags'],trigger=0x1234,id=i+1,next_id=nxt),bytes([c['seed']])*32,c['nexts']) for i,nxt in enumerate(c['nexts'])))
            r['hex']=b.hex()
        flags=editor_saved_flags(c['flags'],c['nexts']);want.update(EDX=flags,ESI=flags)
    elif c['kind']=='loader':
        from native_action_points import intermediate
        from emulate_action_points import raw_record
        entry=0x496FD0;mid=bytearray(intermediate(raw_record(wait=65535,flags=c['flags'],next_id=255,ident=17)));struct.pack_into('<I',mid,0,0);struct.pack_into('<I',mid,0x18,0);struct.pack_into('<I',mid,0x20,int(c['present']))
        expected=bytearray(model);expected[0x1C]=17;expected[0x24:0x34]=mid[0x24:0x34];expected[0x34]=255
        if c['present']:struct.pack_into('<H',expected,0x36,65535);expected[0x38]=c['flags']&255
        if c['legacy']:expected[0x38]&=~1
        context=bytearray(0x40);struct.pack_into('<I',context,0x3C,0x10002000);mapmodel=bytearray(0x100);struct.pack_into('<II',mapmodel,0x60,2,39 if c['legacy'] else 40)
        readout=compare_code(0x10000000,0x10006000,60)+bytes.fromhex('8b1d')+pack32(0x10004000)+END
        mem=[region(0x10000000,model),region(0x10001000,mid),region(0x10002000,mapmodel),region(0x10003000,context),region(0x10004000,bytes(4)),region(0x10005000,readout),region(0x10006000,expected),region(0x49B4C0,return_const(0x10003000)),region(0x4E585A,return_const(0x10000000)),region(0x425B40,return_const(0x10000000,4)),region(0x4E3106,return_const(0)),region(0x4EADB0,return_const(0,4)),region(0x4EAC77,b'\xC3'),region(0x490170,bytes.fromhex('ff05')+pack32(0x10004000)+bytes.fromhex('c20400')),region(0x7FFF0000,pack32(0x10005000)+pack32(0x10001000))];want=dict(EAX=1,ECX=0,EBX=1)
    elif c['kind']=='parser':
        from emulate_action_points import build_case as old_case,raw_record
        from native_action_points import intermediate
        args,want=old_case(dict(kind='parser',program='editor',size=c['size'],xy=c['xy']));entry=0x421FD0;mem=json.loads(args['memory'])['regions'];regs=json.loads(args['registers']);raw=raw_record(c['xy'],flags=c['flags'],size=c['size'])
        for r in mem:
            a=int(r['address'],16)
            if a==0x4B7F10:r['address']=hex(0x4C53A0)
            if a==0x10000000:r['hex']=(raw[8:] or b'\0').hex()
            if a==0x10001000:r['hex']=intermediate(raw).hex()
    else:raise AssertionError(c)
    spans=sorted((int(r['address'],16),len(bytes.fromhex(r['hex']))) for r in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),c['name']
    return dict(program='DK2-Editor-rc3.exe',address=hex(entry),max_steps=30000,registers=json.dumps(regs),memory=json.dumps(dict(regions=mem)),return_registers=','.join([*want,'ESP','EIP'])),want
async def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1] or c['kind']==sys.argv[1]];assert selected
    params=StdioServerParameters(command=str(ROOT.parent/'ghidra-mcp-runtime/venv/Scripts/python.exe'),args=[str(ROOT.parent/'ghidra-mcp/bridge_mcp_ghidra.py'),'--transport','stdio'],env={**os.environ,'GHIDRA_MCP_URL':'http://127.0.0.1:8089'})
    manifest=[]
    async with stdio_client(params) as (rd,wr):
        async with ClientSession(rd,wr) as session:
            await session.initialize()
            for c in selected:
                args,want=build_case(c);p=ROOT/'ghidra'/('phase51-emulation-rc3-ap-'+c['name']+'.json')
                if p.exists():saved=json.loads(p.read_text());assert saved['input']==args and saved['expected_registers']==want
                else:
                    response=await session.call_tool('emulate_function',args);r=json.loads('\n'.join(b.text for b in response.content if b.type=='text'))
                    if response.isError or 'error' in r:raise RuntimeError(str(r))
                    r=r.get('data',r);saved=dict(case=c,input=args,expected_registers=want,result=r);p.write_text(json.dumps(saved,indent=2),encoding='utf8')
                r=saved['result'];assert r['success'] and r['hit_return'],(c['name'],r);assert all(int(r['registers'][reg],16)==v for reg,v in want.items()),(c['name'],r,want)
                manifest.append(dict(file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),case=c['name']));print(c['name'],'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase51-rc3-ap-emulations.json').write_text(json.dumps(dict(cases=manifest,scope=__doc__),indent=2),encoding='utf8')
if __name__=='__main__':asyncio.run(main())
