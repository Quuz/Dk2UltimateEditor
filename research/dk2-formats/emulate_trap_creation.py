"""Raw Traps -> original definition -> actual registered Trap -> full activation.

Prepared World/allocator arena/32x32 map/player; original code, vtables, tag and
owner/map lists. Explicit nullvisual/sound lookup0/playtoken providers. No livegame.
"""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_trap_creation as p
import native_trap_loading as layout
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=p.sha

def cases():
    return [dict(name='gas-factory'),dict(name='gas-active',activate=True),dict(name='gas-unknown0',activate=True,unknown=0),
        dict(name='gas-unknownFFFF',activate=True,unknown=65535),dict(name='gas-usage0',activate=True,usage=0),
        dict(name='gas-usage-minus1',activate=True,usage=65535),dict(name='gas-usage-min',activate=True,usage=32768),
        dict(name='gas-usage-max',activate=True,usage=32767),dict(name='trigger-factory',id=9),dict(name='trigger-active',id=9,activate=True),
        dict(name='trigger-unknown',id=9,activate=True,unknown=0xBEEF),dict(name='sentry-factory',id=3),
        dict(name='sentry-active',id=3,activate=True),dict(name='gas-twice',activate=True,twice=True)]
def execute(c):return p.execute(c)
def u(b,o,n=4):return int.from_bytes(b[o:o+n],'little')
def s16(v):return v-65536 if v&32768 else v

def verify(c,args,ch,node,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c,r['emulation_error'],r['trace_tail'])
    assert len(r['before_calls'])==4+int(c.get('activate',False))+int(c.get('twice',False)) and all(b['returned'] for b in r['before_calls'])
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=p.EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);assert 0x400000<=pc<0x670000,x
        ins=next(md.disasm(p.pe_read(blob,pc,15),pc));assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,x
    post=post_context(ch,r);raw=ch[p.RAW][:579];mid=layout.intermediate(raw,bytes(747));d=post[p.f.DEFINITION][:209]
    # Original resource constructors clear four27-byte resource representations;
    # scalar bytes are independently loaded; gap9E andchecksum6C handled byloader.
    prior=bytearray([165])*209;prior[:0x6C]=bytes(0x6C);prior[0x70:0x9E]=bytes(0x2E);prior[0xA0:]=bytes(209-0xA0)
    expected=layout.definition(mid,bytes(prior),20,layout.DX,0,0)
    assert d==expected and post[p.MID]==mid+ch[p.MID][747:]
    assert post[p.RAW]==ch[p.RAW] and post[p.f.DEFINITION][209:]==ch[p.f.DEFINITION][209:]
    assert u(d,0x98,2)==u(raw,0x204,2) and u(d,0x96,2)==u(raw,0x202,2)
    obj=post[p.f.BLOCK][node-p.f.BLOCK:node-p.f.BLOCK+291]
    assert obj==bytes.fromhex(r['captured_hex']) and u(obj,0)==0x66D754 and u(obj,4,2)==100 and u(obj,0x12)==3
    assert obj[0xF6]==raw[0x20C] and u(obj,0xF7)==p.f.DEFINITION
    assert u(obj,0x28,2)==7 and u(obj,0x2A)==p.f.PLAYER and u(obj,0x40)==u(d,0xA2,2)
    assert list(struct.unpack_from('<3I',obj,0x1A))==[0x5800,0x6800,0x2000]
    assert all(u(obj,off,2)==0 for off in [6,8,10,12,14,16])
    assert u(obj,0x103)==100 and obj[0x10C]&0x20==(0 if c.get('activate') else 0x20)
    assert u(obj,0x32)==0x13572468 and u(obj,0x2E)==0
    active=0 if not c.get('activate') else 2 if c.get('twice') else 1;usage=s16(u(raw,0x202,2))
    wanted={a:bytearray(b) for a,b in ch.items()}
    for off,v,n in [(0x32,1,4),(0x76,304,4)]:p.f.put(wanted[p.f.WORLD],off,v,n)
    for off,v,n in [(0xC,100,2),(0x22,1,4),(0x10D6,100+active*usage,4),(0x10E2,1000-active*usage,4)]:p.f.put(wanted[p.f.PLAYER],off,v,n)
    p.f.put(wanted[p.f.BLOCK],0,208);struct.pack_into('<III',wanted[p.f.BLOCK],208,304,p.f.UH,p.f.UT)
    p.f.put(wanted[p.f.UH],8,p.f.BLOCK+208);p.f.put(wanted[p.f.UT],4,p.f.BLOCK+208)
    p.f.put(wanted[0x6EE540],400,node);wanted[0x6ED540][100]=1;p.f.put(wanted[0x6A4818],0,4093);p.f.put(wanted[0x6A4818],4,101)
    p.f.put(wanted[p.f.KINDS],6,100,2)
    cell=(6*32+5)*16;p.f.put(wanted[p.f.MAP],cell,100,2)
    out=wanted[p.f.OUT];p.f.put(out,4,node);p.f.put(out,8,1)
    for address in [p.f.WORLD,p.f.PLAYER,p.f.FH,p.f.FT,p.f.UH,p.f.UT,0x6EE540,0x6ED540,0x6A4818,p.f.KINDS,p.f.MAP,p.f.OUT]:
        assert post[address]==wanted[address],(c['name'],hex(address),[(hex(i),a,b) for i,(a,b) in enumerate(zip(post[address],wanted[address])) if a!=b][:15])
    for a in [p.f.BLOCK]:
        assert post[a][:node-a]==wanted[a][:node-a] and post[a][node-a+291:]==wanted[a][node-a+291:]
    assert post[p.LOOKIO]==struct.pack('<I',1) and post[p.f.ARGS]==ch[p.f.ARGS]
    unknown=[x for x in r['observed_reads'] if int(x['address'],16)<p.f.DEFINITION+0x9A and int(x['address'],16)+x['size']>p.f.DEFINITION+0x98]
    assert not unknown
    assert r['visited_instructions'].get('0x4b7c10',0)==1 and r['visited_instructions'].get('0x4b7400',0)==1
    assert r['visited_instructions'].get('0x4c1b60',0)==active
    if c.get('activate'):
        path='0x4a9720' if raw[0x20B]==3 else '0x4a9a00';assert r['visited_instructions'].get(path,0)==active
    original={a for a in ch if 0x400000<=a<0x690000 and a in [0x66D754,0x66D464,0x66D4BC,0x66E3EC]}
    for a in original:assert post[a]==ch[a]
    return post,dict(actual_Trap=node,registered_tag=100,source_ID=raw[0x20C],definition_checksum=u(d,0x6C),
        raw_unknown_WORD=u(raw,0x204,2),loaded_unknown_WORD=u(d,0x98,2),signed_ManaUsage=usage,activation_calls=active,
        Player_upkeep=u(post[p.f.PLAYER],0x10D6),Player_income=u(post[p.f.PLAYER],0x10E2),runtime_flags10C=obj[0x10C],
        unknown_definition_reads=0,trigger_type=raw[0x20B],allocated_bytes=304,free_bytes=208,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def compare_pair(a,b,label):
    assert a.keys()==b.keys();allowed={p.RAW:[(0x204,2)],p.MID:[(0x2AC,2)],p.f.DEFINITION:[(0x6C,4),(0x98,2)]};diffs=[]
    for address,left in a.items():
        right=b[address];indices=[i for i,(x,y) in enumerate(zip(left,right)) if x!=y];assert len(left)==len(right)
        assert all(any(off<=i<off+n for off,n in allowed.get(address,[])) for i in indices),(label,hex(address),indices[:20])
        if indices:diffs.append(dict(region=hex(address),changed_byte_offsets=indices))
    assert {int(d['region'],16) for d in diffs}==set(allowed)
    return dict(case=label,compared_regions=len(a),compared_bytes=sum(map(len,a.values())),actual_differences=diffs,
        all_except_raw_MID_definition_copies_and_checksum_identical=True,global_nonuse_proved=False)

def main():
    rows=[];posts={}
    for c in cases():
        args,ch,node,s,path=execute(c);post,out=verify(c,args,ch,node,s['result']);posts[c['name']]=post
        rows.append(dict(case=c,outcome=out,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes())))
        print(c['name'],out['Player_upkeep'],out['Player_income'],'verified',flush=True)
    pairs=[compare_pair(posts['gas-active'],posts[n],n) for n in ['gas-unknown0','gas-unknownFFFF']]
    pairs.append(compare_pair(posts['trigger-active'],posts['trigger-unknown'],'trigger-unknown'))
    (ROOT/'phase161-trap-creation-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,paired_controls=pairs),indent=2),encoding='utf8')
if __name__=='__main__':main()
