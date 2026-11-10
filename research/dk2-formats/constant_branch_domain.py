"""Conservative concrete register/condition tracking for x86 pointer searches.

Only proven integer constants select a branch. Memory is always unknown;
pointer-relative offsets never stand in for numeric register values. Missing
semantics invalidate affected constants/flags. This is not a CPU emulator.
"""
from capstone.x86 import X86_OP_REG,X86_OP_IMM,X86_OP_MEM
from capstone import CS_AC_WRITE
from scan_interprocedural_creature_fields import GROUP

def branch(m,f):
    if m=='jmp':return True
    if f is None:return None
    groups=[(['je','jz'],'z',lambda z:z),(['jne','jnz'],'z',lambda z:not z),
        (['ja','jnbe'],'cz',lambda c,z:not c and not z),(['jae','jnb','jnc'],'c',lambda c:not c),
        (['jb','jc','jnae'],'c',lambda c:c),(['jbe','jna'],'cz',lambda c,z:c or z),
        (['jg','jnle'],'zso',lambda z,s,o:not z and s==o),(['jge','jnl'],'so',lambda s,o:s==o),
        (['jl','jnge'],'so',lambda s,o:s!=o),(['jle','jng'],'zso',lambda z,s,o:z or s!=o),
        (['js'],'s',lambda s:s),(['jns'],'s',lambda s:not s),(['jo'],'o',lambda o:o),(['jno'],'o',lambda o:not o),
        (['jp','jpe'],'p',lambda p:p),(['jnp','jpo'],'p',lambda p:not p)]
    for names,keys,fn in groups:
        if m in names:
            args=[f.get(k) for k in keys]
            return None if any(a is None for a in args) else fn(*args)
    return None

def step(x,values,flags):
    v=dict(values);ops=x.operands;m=x.mnemonic
    def group(o):
        r=x.reg_name(o.reg);return GROUP.get(r,r)
    def value(o):
        if o.type==X86_OP_IMM:return o.imm&((1<<(o.size*8))-1)
        if o.type==X86_OP_REG:
            a=v.get(group(o))
            if a is not None:return (a>>(8 if x.reg_name(o.reg) in ['ah','bh','ch','dh'] else 0))&((1<<(o.size*8))-1)
        return None
    def assign(o,n):
        if o.type!=X86_OP_REG:return
        d=group(o)
        if n is None:v.pop(d,None);return
        mask=(1<<(o.size*8))-1;n&=mask
        if o.size==4:v[d]=n
        elif d in v:
            shift=8 if x.reg_name(o.reg) in ['ah','bh','ch','dh'] else 0
            v[d]=(v[d]&~(mask<<shift))|(n<<shift)
        else:v.pop(d,None)
    def compare(a,b,size,test=False):
        if a is None or b is None:return None
        bits=8*size;mask=(1<<bits)-1;sign=1<<(bits-1);a&=mask;b&=mask
        r=(a&b) if test else ((a-b)&mask)
        return dict(z=r==0,s=bool(r&sign),c=False if test else a<b,
            o=False if test else bool((a^b)&(a^r)&sign),p=(r&255).bit_count()%2==0)
    if m in ['cmp','test']:
        return v,compare(value(ops[0]),value(ops[1]),ops[0].size,m=='test')
    if m=='call':
        for r in ['eax','ecx','edx','esp']:v.pop(r,None)
        return v,None
    if m.startswith('j') or m.startswith('ret'):return v,flags
    if m in ['mov','movzx','movsx','movsxd'] and len(ops)==2:
        a=value(ops[1])
        if a is not None and m in ['movsx','movsxd'] and a&(1<<(ops[1].size*8-1)):a-=1<<(ops[1].size*8)
        assign(ops[0],a);return v,flags
    if m=='lea' and len(ops)==2:
        o=ops[1];b=v.get(x.reg_name(o.mem.base)) if o.mem.base else 0;i=v.get(x.reg_name(o.mem.index)) if o.mem.index else 0
        assign(ops[0],None if b is None or i is None else b+i*o.mem.scale+o.mem.disp);return v,flags
    if m=='pop':assign(ops[0],None);v.pop('esp',None);return v,flags
    if m=='push':v.pop('esp',None);return v,flags
    if m=='leave':v.pop('esp',None);v.pop('ebp',None);return v,flags
    if m=='nop':return v,flags
    if len(ops)==2 and m in ['xor','add','sub','and','or','shl','sal','shr','sar','imul']:
        a,b=value(ops[0]),value(ops[1]);n=None
        if m=='xor' and ops[0].type==ops[1].type==X86_OP_REG and ops[0].reg==ops[1].reg:n=0
        elif a is not None and b is not None:
            if m=='add':n=a+b
            elif m=='sub':n=a-b
            elif m=='xor':n=a^b
            elif m=='and':n=a&b
            elif m=='or':n=a|b
            elif m in ['shl','sal']:n=a<<(b&31)
            elif m=='shr':n=a>>(b&31)
            elif m=='sar':
                bits=ops[0].size*8;n=(a-(1<<bits) if a&(1<<(bits-1)) else a)>>(b&31)
            elif m=='imul':n=a*b
        assign(ops[0],n)
        if n is not None and m in ['xor','and','or']:return v,compare(n,n,ops[0].size,True)
        if n is not None and m=='sub':return v,compare(a,b,ops[0].size)
        if n is not None and m=='add':
            bits=8*ops[0].size;mask=(1<<bits)-1;sign=1<<(bits-1);r=n&mask
            return v,dict(z=r==0,s=bool(r&sign),c=n>mask,o=bool(~(a^b)&(a^r)&sign),p=(r&255).bit_count()%2==0)
        return v,None
    if len(ops)==3 and m=='imul':
        a,b=value(ops[1]),value(ops[2]);assign(ops[0],None if a is None or b is None else a*b);return v,None
    if len(ops)==1 and m in ['inc','dec','neg','not']:
        a=value(ops[0]);assign(ops[0],None if a is None else a+1 if m=='inc' else a-1 if m=='dec' else -a if m=='neg' else ~a)
        if m=='not':return v,flags
        f=compare(a,1,ops[0].size) if m=='dec' else compare(0,a,ops[0].size) if m=='neg' else None
        if m=='inc' and a is not None:
            bits=8*ops[0].size;mask=(1<<bits)-1;sign=1<<(bits-1);r=(a+1)&mask
            f=dict(z=r==0,s=bool(r&sign),o=a==sign-1,p=(r&255).bit_count()%2==0)
        if f is not None and m in ['inc','dec']:f['c']=None if flags is None else flags.get('c')
        return v,f
    for o in ops:
        if o.type==X86_OP_REG and o.access&CS_AC_WRITE:assign(o,None)
    # Implicit written registers (MUL/CDQ/etc.) must also lose constants.
    try:
        for r in x.regs_access()[1]:
            name=x.reg_name(r)
            if name!='eflags':v.pop(GROUP.get(name,name),None)
    except Exception:
        v={}
    return v,None if x.eflags else flags
