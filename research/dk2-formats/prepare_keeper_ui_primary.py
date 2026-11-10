"""Attest complete native UI/queue chain,receiver origins and caller context."""
from pathlib import Path
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
EXECUTED=[0x4F7C70,0x504D60,0x56B990,0x56BAB0,0x6361A0,0x51FCD0,0x4F7CE0,0x50DD60,
 0x4115E0,0x416E20,0x411140,0x4BAFA0,0x508F10,0x50DE00,0x508C40,0x4094C0,0x40C180,0x422600,0x4346E0,0x527130,0x525F00,0x5096F0]
CALLERS=sorted({r['function_hint']['address'] for r in json.loads((ROOT/'phase84-order-call-candidates.json').read_text())['candidates']})
STATIC=sorted((set(CALLERS)|{0x402D00,0x4039A0,0x402670,0x4346A0,0x42EA70,0x42D820})-set(EXECUTED))
def main():
    q=[]
    if len(sys.argv)==1:
        for a in EXECUTED+STATIC:
            for label,tool in [('create','create_function'),('assembly','disassemble_function')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=f'phase84-final-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
        (ROOT/'phase84-final-code-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');return
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[]
    for a in EXECUTED+STATIC:
        assembly=f'phase84-final-DX-{a:x}-assembly.txt';aa=[int(s[:8],16) for s in (ROOT/'ghidra'/assembly).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',s)]
        assert aa and aa[0]==a;last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase84-final-DX-{a:x}-memory'
        functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,assembly=assembly,memory=key+'.txt',executed_path=a in EXECUTED))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    for a,n,label in [(0x66E3EC+0xFC,40,'OriginalWorld Keeper ID/count/order vslots'),(0x66E3EC+0x260,4,'OriginalWorld player-tag getter'),
       (0x66E3EC+0x2F8,4,'OriginalWorld synchronization-selection presence getter'),(0x66C4A4,92,'OriginalDefaultPlayerInterface vtable'),
       (0x68FD58,0x140,'PackedKeeperSpell button descriptor region;411140 pointer at+A6;not full mouse-event registration proof')]:
        key=f'phase84-final-data-{a:x}';blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'.txt',label=label))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase84-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase84-final-memory-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');print(len(functions),'code spans,',len(blocks),'original data ranges')
if __name__=='__main__':main()
