"""Accepted original active Room resource-loading paths and four checksum-only spans."""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_room_resource_loader as p
from emulate_resistance import post_context
from native_room_resources import room_floor_resource_plan
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
sha=p.sha
SPANS=[0x11C,0x170,0x1C4,0x218]
MID_SPANS=[0x1F0,0x244,0x298,0x2EC]

def cases():
    out=[]
    for mode in range(1,14):
        out += [dict(name=f'mode{mode}-source',room_id=13,mode=mode,opaque='source'),
                dict(name=f'mode{mode}-four-filled',room_id=13,mode=mode,opaque='filled',opaque_offsets=SPANS)]
    out += [dict(name=f'only-{off:x}-filled',room_id=13,opaque='filled',opaque_offsets=[off]) for off in SPANS]
    for name,opts in [('no-service',dict(service=False)),('update',dict(update_mode=1))]:
        out += [dict(name=name+'-source',room_id=13,opaque='source',**opts),dict(name=name+'-filled',room_id=13,opaque='filled',opaque_offsets=SPANS,**opts)]
    out += [dict(name='complete-control',room_id=13,opaque='source',complete_control=True)]
    out += [dict(name=f'opaque-kind{kind}',room_id=13,opaque='filled',opaque_kind=kind) for kind in [0,255]]
    out += [dict(name=f'source-room{raw[0x392]}',room_id=raw[0x392],opaque='source') for raw in p.source_records('Rooms.kwd','Rooms') if raw[0x392]!=13]
    return out

def requests(post):
    log=post[p.LOG];n=int.from_bytes(log[:4],'little');assert n<=p.CAPACITY
    out=[]
    for i in range(n):
        row=log[0x100+p.ROW_SIZE*i:0x100+p.ROW_SIZE*(i+1)];name=row[:64].split(b'\0',1)[0]
        assert len(name)<64
        out.append(dict(name=name.decode('cp1252'),kind=row[64],flags=int.from_bytes(row[68:72],'little'),handle=int.from_bytes(row[72:76],'little'),return_pc=int.from_bytes(row[76:80],'little')))
    return out

def execute(c):
    args,checks,saved,path=p.execute(c);r=saved['result']
    assert r['success'] and r['hit_return'] and not r['emulation_error'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    return args,checks,saved,path

def verify(c,args,checks,r):
    assert len(r['before_calls'])==7 and all(x['returned'] for x in r['before_calls'])
    post=post_context(checks,r);raw=post[p.prior.ROOMRAW][:1055];mid=post[p.prior.ROOMMID][:1267];definition=post[p.prior.prior.ROOMDEF]
    assert post[p.prior.ROOMRAW]==checks[p.prior.ROOMRAW]
    assert all(mid[mo:mo+84]==raw[ro:ro+84] for ro,mo in zip(SPANS,MID_SPANS))
    checksum=int.from_bytes(definition[0xA2:0xA6],'little')
    assert checksum==(0x12345678 if c.get('update_mode') else sum(mid)),(c['name'],checksum,sum(mid))
    for ro,mo in zip(SPANS,MID_SPANS):
        reads=[x for x in r['observed_reads'] if int(x['address'],16)<p.prior.ROOMMID+mo+84 and int(x['address'],16)+x['size']>p.prior.ROOMMID+mo]
        assert len(reads)==(0 if c.get('update_mode') else 84) and all(x['pc']=='0x4eacfc' and x['size']==1 for x in reads),(c['name'],hex(ro),reads[:3])
    req=requests(post)
    assert all('P150_Unknown' not in x['name'] for x in req)
    assert [x['handle'] for x in req]==list(range(1,len(req)+1))
    if not c.get('update_mode') and c.get('service',True):
        plan=room_floor_resource_plan(raw)['requests']
        floor=[x for x in req if x['return_pc']<0x4EB810 and x['return_pc'] not in [0x4EAF09,0x4EAF55]]
        assert [(x['name'],x['kind'],x['flags']) for x in floor]==[(x['name'],x['resource_type'],x['service_flags']) for x in plan],(c['name'],floor,plan)
        # SourceHeroPortal19 has no requests in this loader; successful
        # service IO implies504D80 only when a request actually exists.
        assert bool(req)==bool(r['visited_instructions'].get('0x504d80',0)),(c['name'],req)
    else:assert not req
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=p.EXE.read_bytes()
    for x in r['observed_reads']:
        if int(x['address'],16)>=4096:continue
        ins=next(md.disasm(pe_read(blob,int(x['pc'],16),15),int(x['pc'],16)))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    return post,dict(checksum=checksum,requests=req,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])

def pair_differences(left,right):
    result=[]
    for a,b in left.items():
        offsets=[i for i,(x,y) in enumerate(zip(b,right[a])) if x!=y]
        if offsets:result.append(dict(region=hex(a),offsets=offsets))
    return result

def validate_pair(a,b,changed,update=False):
    diff=pair_differences(a,b);allowed={p.prior.ROOMRAW:{i for off in changed for i in range(off,off+84)},
        p.prior.ROOMMID:{i for ro,mo in zip(SPANS,MID_SPANS) if ro in changed for i in range(mo,mo+84)},p.prior.prior.ROOMDEF:set(range(0xA2,0xA6)) if not update else set()}
    assert diff
    for row in diff:assert set(row['offsets'])<=allowed.get(int(row['region'],16),set()),row
    assert a[p.LOG]==b[p.LOG]
    return diff

def main():
    rows=[];states={}
    for c in cases():
        args,checks,saved,path=execute(c);post,out=verify(c,args,checks,saved['result']);states[c['name']]=post
        rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out));print(c['name'],out['checksum'],len(out['requests']),'requests',flush=True)
    pairs=[]
    for mode in range(1,14):pairs.append(dict(name=f'mode{mode}',differences=validate_pair(states[f'mode{mode}-source'],states[f'mode{mode}-four-filled'],SPANS)))
    for off in SPANS:pairs.append(dict(name=f'only-{off:x}',differences=validate_pair(states['mode7-source'],states[f'only-{off:x}-filled'],[off])))
    for name in ['no-service','update']:pairs.append(dict(name=name,differences=validate_pair(states[name+'-source'],states[name+'-filled'],SPANS,update=name=='update')))
    assert requests(states['mode7-source'])!=requests(states['complete-control'])
    (ROOT/'phase150-room-resource-loader-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,whole_memory_pairs=pairs),indent=2),encoding='utf8')

if __name__=='__main__':main()
