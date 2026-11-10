"""Old PE inventory of Crypt source-file references and direct counter hints."""
from pathlib import Path
from collections import deque,Counter
import json,re,bisect
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM
from compare_rc3_converters import code_region
from native_shot_pair_trace import pe_image,sha
from native_memory_access import reads_memory
ROOT=Path(__file__).resolve().parent

def main():
    file=ROOT/'source-binaries/DKII-local13.exe';data=file.read_bytes()
    assert sha(data)=='93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342'
    imagebase,image=pe_image(data);lower=bytes(image).lower();pos=lower.find(b'crypt.cpp');strings=[]
    while pos!=-1:
        start=bytes(image).rfind(b'\0',max(0,pos-256),pos)+1;end=bytes(image).find(b'\0',pos)
        strings.append(dict(address=hex(imagebase+start),text=bytes(image[start:end]).decode('ascii',errors='replace')))
        pos=lower.find(b'crypt.cpp',pos+1)
    needles=[s['address'] for s in strings];base,code=code_region(data)
    md=Cs(CS_ARCH_X86,CS_MODE_32);detail=Cs(CS_ARCH_X86,CS_MODE_32);detail.detail=True;md.skipdata=True
    literal=re.compile(r'\b0x(?:108[a-f]|109[0-9ab])\b');calls=set();refs=[];fields=[];tail=deque(maxlen=6);decoded=0
    for pc,size,m,opstr in md.disasm_lite(code,base):
        decoded+=1
        if m=='call' and opstr.startswith('0x'):
            try:calls.add(int(opstr,16))
            except ValueError:pass
        if any(s in opstr for s in needles):refs.append(dict(address=hex(pc),instruction=m+' '+opstr,preceding=list(tail)))
        if literal.search(opstr):
            x=next(detail.disasm(code[pc-base:pc-base+15],pc,count=1),None)
            if x:
                for operand in x.operands:
                    if operand.type==X86_OP_MEM and 0x108A<=operand.mem.disp<=0x109B:
                        fields.append(dict(address=hex(pc),instruction=m+' '+opstr,displacement=hex(operand.mem.disp),
                            bytes=operand.size,read=reads_memory(x,operand),access=operand.access,preceding=list(tail)))
        tail.append(f'{pc:x}: {m} {opstr}')
    starts=sorted(calls)
    for row in refs+fields:
        pc=int(row['address'],16);i=bisect.bisect_right(starts,pc)-1
        row['nearest_direct_CALL_target_hint']=hex(starts[i]) if i>=0 else None
    out=dict(phase=189,status='untyped_static_leads',file=str(file),sha256=sha(data),source_strings=strings,
        source_references=refs,direct_counter_offset_hints=fields,decoded_items=decoded,direct_call_targets=len(calls),
        histogram=dict(Counter(r['displacement'] for r in fields)),
        scope='Directliteraloperandsinoneoldtextsection; missesindexed/offsetbases. Hintpointersnottyped. CounterlayoutmaydifferfromDX. Noabsenceofuseclaim.')
    (ROOT/'phase189-old-crypt-inventory.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
    print('strings',strings,'refs',[(r['address'],r['nearest_direct_CALL_target_hint']) for r in refs])
    print('directhints',len(fields),'histogram',out['histogram'])

if __name__=='__main__':main()
