"""Fresh whole native functions and constructor dispatch tables for phase111."""
import json
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import ROOT,EXE,pe_read


def main():
    old={}
    for name in ['phase89-primary-manifest.json','phase109-primary-manifest.json','phase110-primary-manifest.json']:
        for r in json.loads((ROOT/name).read_text(encoding='utf8'))['functions']:
            old[r['address']]=r
    for r in json.loads((ROOT/'terrain-catalogue-validation.json').read_text(encoding='utf8'))['primary_program_bytes']:
        if r['program']=='DKII-DX.exe':old[r['address']]=dict(r,assembly=r['after'])
    executed=[0x49E600,0x56D4F0,0x56D620,0x49DD60,0x4B7A90,0x49B1C0,0x49EF60,0x49EF80,
              0x49BD20,0x49EED0,0x4A3530,0x509580,0x50E490,0x4430D0,
              0x56EB60,0x56ED20,0x4F66E0,0x4559D0,0x4BE070,0x4C10E0,0x4C17A0]
    static=[0x44FC40]
    current={a:f'phase111-vault-{a:x}-disassemble_function.txt' for a in [0x49EF60,0x49EF80,0x4A3530,0x4BE070,0x44FC40]}
    current[0x4559D0]='phase23-terrain-mana-node-update-assembly.txt'
    rows,queries=[],[];data=EXE.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    for a in sorted(executed+static):
        if a in current:
            assembly=current[a]
            aa=[int(s[:8],16) for s in (ROOT/'ghidra'/assembly).read_text(encoding='utf8').splitlines() if re.match(r'^[0-9a-fA-F]{8}:',s)]
            assert min(aa)==a
            last=max(aa);tail=next(md.disasm(pe_read(data,last,15),last));size=tail.address+tail.size-a
        else:
            r=old[a];assembly=r.get('assembly',r.get('after',r.get('before')));size=r['length']
        key=f'phase111-primary-{a:x}'
        rows.append(dict(address=a,length=size,assembly=assembly,memory=key+'.txt',executed=a in executed,
                         scope='Whole native extent; selected paths only. Static44FC40 creation route not executed.'))
        queries.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=size),print_limit=5))
    blocks=[]
    for a,n,label in [(0x49B564,24,'constructor six dispatch targets'),(0x49B57C,100,'constructor ObjectID31..130 dispatch indices'),
                      (0x66E3EC+0x388,8,'original World effect bridge slots'),(0x670574+0x90,4,'original null graphics slot90'),
                      (0x69DE84,37,'native EffectTriggerRepeatTimer debug label')]:
        key=f'phase111-block-{a:x}';blocks.append(dict(address=a,length=n,label=label,memory=key+'.txt'))
        queries.append(dict(id=key,tool='read_memory',args=dict(program='DKII-DX.exe',address=hex(a),length=n),print_limit=5))
    queries.append(dict(id='phase111-save-DX',tool='save_program',args=dict(program='DKII-DX.exe'),print_limit=5))
    (ROOT/'phase111-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8')
    (ROOT/'phase111-primary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')
    print(len(rows),'wholefunctions;',len(executed),'executed;',len(blocks),'blocks')


if __name__=='__main__':main()
