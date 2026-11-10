"""Strict full CPlayer writer checks with an independent expected byte stream."""
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_crypt_state_writer as probe
from emulate_resistance import post_context
ROOT,p,EXE,sha,u=probe.ROOT,probe.p,probe.EXE,probe.sha,probe.u
cases,execute=probe.cases,probe.execute


def verify(c,args,checks,r):
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    assert int(r['registers']['EAX'],16)==1
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(p.pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,row
    post=post_context(checks,r);owner=post[probe.BEFORE]
    assert post[probe.OWNER]==owner and owner[probe.SIZE:]==checks[probe.OWNER][probe.SIZE:]==bytes([165])*64
    assert u(owner,0)==0x66D99C and u(owner,4,2)==1 and u(owner,0x1109)==0x66E3AC
    assert post[0x6ED540][1]==1 and u(post[0x6EE540],4)==probe.OWNER
    assert owner[0x108A:0x109E]==bytes.fromhex(c['counters'])
    assert u(post[probe.STATE],8)==0
    chunks=[owner[0x10FC:0x10FD],owner[4:6],owner[0x76:0x109E]]
    for tag,count_offset in [(0x7A,0x56),(0xAC,0x4E),(0x8E,0x5E),(0x84,0x66),(0x98,0x6E)]:
        count=u(owner,count_offset);assert count==int(c.get('one_entry',False) and tag==0x7A)
        chunks.append(struct.pack('<II',tag,count))
        if count:
            assert u(owner,0x5A)==probe.ARRAY
            chunks += [checks[probe.ARRAY][1:5],checks[probe.ARRAY][5:9]]
    chunks += [owner[0x1115:0x1119],owner[0x10FA:0x10FB]]
    intended=b''.join(chunks);assert len(intended)==4184+8*int(c.get('one_entry',False))
    written=[chunk[:0 if c.get('fail') else c.get('limit',len(chunk))] for chunk in chunks]
    expected=b''.join(written);actual=post[probe.DATA]
    assert actual[:len(expected)]==expected and actual[len(expected):]==bytes([165])*(probe.CAPACITY+64-len(expected))
    assert u(post[probe.STATE],0)==len(expected) and u(post[probe.STREAM],16)==len(expected)
    assert u(post[probe.STATE],4)==len(chunks)
    assert u(post[probe.STATE],12)==sum(len(a)!=len(b) for a,b in zip(chunks,written))
    position=0;events=[]
    for index,(chunk,part) in enumerate(zip(chunks,written)):
        off=index*20;log=post[probe.LOG]
        assert [u(log,off+k) for k in [0,8,12,16]]==[position,len(chunk),len(part),0 if c.get('fail') else 1]
        source=u(log,off+4)
        if index==2:assert source==probe.OWNER+0x76
        events.append(dict(position=position,source=source,request=len(chunk),written=len(part)))
        position+=len(part)
    assert post[probe.LOG][20*len(chunks):]==bytes([165])*(64*20-20*len(chunks))
    for address in [0x66D99C,0x66E3AC,0x66E3DC,0x671F80,0x672FE8,0x66C000]:assert post[address]==checks[address]
    assert post[probe.ARRAY]==checks[probe.ARRAY]
    assert u(post[probe.READSTATE],4)==u(post[probe.READSTATE],8)==u(post[probe.READSTATE],16)==0
    assert post[probe.READLOG]==checks[probe.READLOG]
    visits=lambda address:r['visited_instructions'].get(hex(address),0)
    assert visits(0x4B8450)==visits(0x4B8D40)==visits(0x506220)==1
    assert visits(0x5B3EE0)==visits(0x5FFFF0)==len(chunks)
    complete=not c.get('fail') and 'limit' not in c
    if complete:assert actual[0x1017:0x102B]==bytes.fromhex(c['counters'])
    return post,dict(return_EAX=1,output_bytes=len(expected),intended_bytes=len(intended),output_sha256=sha(expected),
                     writes=events,exact_counter20_preserved=complete,source_object_unchanged_by_writer=True,
                     IAT_and_vtables_unchanged=True,OS_write_fail_or_short=not complete,
                     native_steps=r['instruction_count'],original_Writes=len(chunks))


def main():
    rows=[]
    for c in cases():
        a,ch,s,f=execute(c);_,out=verify(c,a,ch,s['result'])
        rows.append(dict(case=c,file=str(f.relative_to(ROOT)),sha256=sha(f.read_bytes()),outcome=out))
        print(c['name'],out['output_bytes'],out['return_EAX'],flush=True)
    (ROOT/'phase190-crypt-state-writer-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf-8')


if __name__=='__main__':main()
