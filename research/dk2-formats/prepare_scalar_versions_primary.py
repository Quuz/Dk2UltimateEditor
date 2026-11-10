from pathlib import Path
import json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def main():
    md=Cs(CS_ARCH_X86,CS_MODE_32);rows=[];blocks=[];q=[]
    for profile,program,exe,addresses in [('old','DKII-local13.exe',ROOT/'source-binaries/DKII-local13.exe',[0x510af0,0x476600]),('DX','DKII-DX.exe',ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',[0x51cd90])]:
        for a in addresses:
            file=f'phase101-{profile}-{a:x}-assembly.txt';aa=[int(x[:8],16) for x in (ROOT/'ghidra'/file).read_text(encoding='utf8').splitlines() if re.match('^[0-9a-fA-F]{8}:',x)]
            last=next(md.disasm(pe_read(exe.read_bytes(),aa[-1],15),aa[-1]));n=last.address+last.size-a;key=f'phase101-primary-{profile}-{a:x}'
            rows.append(dict(program=program,exe=str(exe),address=a,length=n,assembly=file,memory=key+'.txt',scope='Wholecodeextent;switch tables separatelyattested;callbacks executed,476600staticonly.'))
            q.append(dict(id=key,tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=5))
        specs=[(0x511478,237*4,'scalar-switch-table'),(0x4767de,17,'overlap113-masked-fragment')] if profile=='old' else [(0x51d740,240*4,'scalar-switch-table'),(0x47bb4e,17,'overlap113-masked-fragment')]
        for a,n,label in specs:
            key=f'phase101-block-{profile}-{a:x}';blocks.append(dict(program=program,exe=str(exe),address=a,length=n,label=label,memory=key+'.txt'))
            q.append(dict(id=key,tool='read_memory',args=dict(program=program,address=hex(a),length=n),print_limit=5))
        q.append(dict(id='phase101-save-'+profile,tool='save_program',args=dict(program=program),print_limit=5))
    (ROOT/'phase101-primary-manifest.json').write_text(json.dumps(dict(functions=rows,blocks=blocks),indent=2),encoding='utf8');(ROOT/'phase101-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')
if __name__=='__main__':main()
