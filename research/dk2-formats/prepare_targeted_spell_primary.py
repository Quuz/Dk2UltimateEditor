"""Post-run Ghidra/PE evidence for498870 and preserved native delegates."""
from pathlib import Path
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
BASE=[r['address'] for r in json.loads((ROOT/'phase80-primary-manifest.json').read_text())['functions'] if r['address'] not in [0x4982B0,0x4984D0]]
NEW=[0x498870,0x4B5D40,0x4B5D80,0x402AD0,0x4B7E10,0x634DB0,0x63A490,0x63AE20,0x63AE70,0x63AEB0,0x63BF20,0x63BF40]

def main():
    q=[]
    if len(sys.argv)==1:
        for a in BASE+NEW:
            for label,tool in [('create','create_function'),('assembly','disassemble_function')]:
                args=dict(program='DKII-DX.exe',address=hex(a))
                if tool=='create_function':args['disassemble_first']=True
                q.append(dict(id=f'phase82-final-DX-{a:x}-{label}',tool=tool,args=args,print_limit=5))
        (ROOT/'phase82-final-code-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8');return
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);functions=[];blocks=[]
    for a in BASE+NEW:
        assembly=f'phase82-final-DX-{a:x}-assembly.txt'
        aa=[int(line[:8],16) for line in (ROOT/'ghidra'/assembly).read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
        assert aa and aa[0]==a
        last=next(md.disasm(pe_read(data,aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase82-final-DX-{a:x}-memory'
        functions.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,assembly=assembly,memory=key+'.txt',
          role='Reused native factory/resource/math/getter chain' if a in BASE else 'New targeted-wrapper/target getter/CRT evidence',
          scope='Complete code span attested;not a claim every branch or every listed CRT helper executed.'))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    original=json.loads((ROOT/'phase80-primary-manifest.json').read_text())['blocks']
    extra=[(0x66D524+0x2C,4,'Creature previous-position getter'),(0x66D524+0x40,4,'Creature height getter'),
      (0x66D654+0x2C,4,'Object previous-position getter'),(0x66D654+0x40,4,'Object zero-height getter'),
      (0x66D86C+0x2C,4,'Shot previous-position getter'),(0x66D86C+0x40,4,'Shot doubled-height getter'),
      (0x69DB9C,46,'Complete native diagnostic format including null'),(0x67BDE8-0x20,160,'CRT character/parser table region'),(0x63AD0C,0xC0,'CRT dispatch table region')]
    allblocks=[(r['address'],r['length'],r['label']) for r in original]+extra
    for a,n,label in allblocks:
        key=f'phase82-final-data-{a:x}'+('-v2' if a==0x69DB9C else '');blocks.append(dict(program='DKII-DX.exe',exe=str(exe),address=a,length=n,memory=key+'.txt',label=label))
        q.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    (ROOT/'phase82-primary-manifest.json').write_text(json.dumps(dict(functions=functions,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase82-final-memory-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
    print(len(functions),'native function spans;',len(blocks),'file-backed data ranges')

if __name__=='__main__':main()
