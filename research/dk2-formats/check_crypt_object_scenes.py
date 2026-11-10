"""Bounded phase179 checks; the full phase179 package audit is still pending.

Actual original Object create/unlink/destruct/free in an isolated CPU context.
This does not execute automatic Crypt job selection or visible animations.
"""
import json
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
import probe_crypt_objects as probe
from emulate_resistance import post_context

ROOT=probe.ROOT
p=probe.p
sha=probe.sha
EXE=probe.EXE
u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')

CASES=[
    dict(name='create-zero',count=0,spare_blocks=2,expected_ids=[]),
    dict(name='create-two',count=2,spare_blocks=2,expected_ids=[143,143]),
    dict(name='remove-two',count=2,spare_blocks=2,cleanup=True,expected_ids=[]),
    dict(name='reuse-freed-block',count=2,spare_blocks=2,cleanup=True,recreate=1,expected_ids=[143]),
    dict(name='preserve-other-room-objects',count=2,spare_blocks=4,extras_before=[142,144],cleanup=True,expected_ids=[142,144]),
    dict(name='no-old-registration',count=2,spare_blocks=2,cleanup=True,old_registered=False,expected_ids=[143,143]),
    dict(name='create-one',count=1,spare_blocks=1,expected_ids=[143]),
    dict(name='create-ten',count=10,spare_blocks=10,expected_ids=[143]*10),
    dict(name='create-twenty',count=20,spare_blocks=20,expected_ids=[143]*20),
    dict(name='create-forty',count=40,spare_blocks=40,expected_ids=[143]*40),
    dict(name='remove-forty',count=40,spare_blocks=40,cleanup=True,expected_ids=[]),
    dict(name='zero-with-prior-count',count=0,initial55=3,spare_blocks=1,expected_ids=[]),
    dict(name='negative-diagnostic',count=-1,initial55=3,spare_blocks=1,expected_ids=[]),
    dict(name='no-free-object-space',count=2,spare_blocks=0,expected_ids=[]),
    dict(name='one-free-object-space',count=2,spare_blocks=1,expected_ids=[143]),
    dict(name='remove-partial-create',count=2,spare_blocks=1,cleanup=True,expected_ids=[]),
    dict(name='skip-nondancers-before-tail',count=1,spare_blocks=3,extras_after=[142,144],cleanup=True,expected_ids=[142,144]),
    dict(name='remove-factory-default-dancer',count=0,spare_blocks=1,extras_before=[143],cleanup=True,expected_ids=[]),
    dict(name='objects-and-actual-occupants',count=2,spare_blocks=2,occupants=2,cleanup=True,expected_ids=[]),
]

def cases():return CASES

def execute(c):
    args,checks,saved,file,post,actors,owner=probe.execute(c)
    return args,checks,saved,file

def verify(c,args,checks,r):
    post=post_context(checks,r)
    actors=[p.target.attack.TARGET] if c.get('occupants',0) else []
    if c.get('occupants',0)==2:actors.append(p.cf.ACTOR)
    assert r['success'] and r['hit_return'] and not r['emulation_error'],(c,r['emulation_error'])
    assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=probe.EXE.read_bytes()
    for x in r['observed_reads']+r['observed_writes']:
        if int(x['address'],16)>=4096:continue
        pc=int(x['pc'],16);ins=next(md.disasm(probe.p.pe_read(blob,pc,15),pc))
        assert int(x['address'],16)==0 and x['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],x)
    assert u(post[probe.readable.COUNTER],4)==0
    def at(a,n):
        base=max(b for b,v in post.items() if b<=a and a+n<=b+len(v))
        return post[base][a-base:a-base+n]
    nodes={}
    for tag in range(103,u(post[0x6A4818],4)):
        address=u(post[0x6EE540],tag*4)
        if not address:
            assert post[0x6ED540][tag]==0
            continue
        b=at(address,393)
        assert u(b,0x12)==2 and post[0x6ED540][tag]==1
        nodes[tag]=dict(address=address,ID=b[0x12C],previous=u(b,0x128,2),next=u(b,0x12A,2),room=u(b,0x138,2),state=u(b,0x13A),marker=u(b,0x133))
    assert sorted(x['ID'] for x in nodes.values())==c['expected_ids'],(c['name'],nodes)
    tag=u(post[probe.old.ROOM],0xE,2);previous=0;seen=[]
    while tag:
        assert tag not in seen and tag in nodes
        node=nodes[tag];assert node['previous']==previous and node['room']==300
        seen.append(tag);previous=tag;tag=node['next']
    assert set(seen)==set(nodes)
    cleanup=c.get('cleanup') and c.get('old_registered',True)
    assert r['visited_instructions'].get('0x4d9570',0)==int(bool(cleanup))
    before=c.get('extras_before',[]);after=c.get('extras_after',[])
    created=min(max(c['count'],0),c['spare_blocks']-len(before))
    deleted=(created+before.count(143)+after.count(143)) if cleanup else 0
    for pc in ['0x4e62d0','0x4b7b10','0x554bd0']:
        assert r['visited_instructions'].get(pc,0)==deleted,(c['name'],pc)
    expected55=c.get('recreate',0) if cleanup else (c.get('initial55',0)+c['count'])&0xFFFFFFFF
    assert u(post[probe.old.ROOM],0x55)==expected55
    world=post[probe.p.f.WORLD]
    assert u(world,0x2A)==len(nodes)
    assert u(world,0x76)==384+408*len(nodes)
    # Both doubly linked allocator lists must remain traversable. Count the
    # original Shot allocation too; do not equate decreasing usage with free.
    pool_counts={}
    for name,head,tail in [('free',probe.FREE_HEAD,probe.FREE_TAIL),('used',probe.p.f.UH,probe.p.f.UT)]:
        previous=head;cursor=u(at(head,12),8);visited=[];total=0
        while cursor!=tail:
            assert cursor and cursor not in visited,(c['name'],name,hex(cursor))
            if name=='free':assert previous>cursor>tail
            h=at(cursor,12);assert u(h,4)==previous
            visited.append(cursor);total+=u(h,0);previous=cursor;cursor=u(h,8)
        assert u(at(tail,12),4)==previous
        pool_counts[name]=dict(blocks=len(visited),bytes=total)
    assert pool_counts['used']['bytes']==u(world,0x76)
    assert sum(x['bytes'] for x in pool_counts.values())==512*(1+c['spare_blocks'])
    assert r['visited_instructions'].get('0x4b7a90',0)==max(c['count'],0)+len(before)+len(after)+c.get('recreate',0)
    assert r['visited_instructions'].get('0x4e6220',0)==created+len(before)+len(after)+c.get('recreate',0)
    for node in nodes.values():
        if node['ID']==143:
            assert node['state']==57 and node['marker']==1
    for a in actors:
        b=post[a];assert u(b,0x40)==u(checks[a],0x40) and post[0x6ED540][u(b,4,2)]==1
        if cleanup:
            assert u(b,0x25E)==1 and all(u(b,o,2)==0 for o in [0x236,0x238,0x3B9])
    assert r['visited_instructions'].get('0x48ad30',0)==len(actors)*int(bool(cleanup))
    if c.get('recreate'):
        addresses=[probe.ARENAS+4096*i+0x74 for i in range(2)]
        assert len(nodes)==1 and next(iter(nodes.values()))['address'] in addresses
    for a,b in checks.items():
        if probe.ARENAS<=a<probe.ARENAS+64*4096:assert post[a][512:]==b[512:]
    for a,n in [(probe.DEFS,3*285),(probe.RAWS,3*894),(probe.MID,1022)]:assert post[a][n:]==checks[a][n:]
    for a in [probe.ROOMDEF,probe.TERRAIN_TABLE,0x66D524,0x66D654,0x66D754,0x66D86C,0x66E3EC,0x670574]:assert post[a]==checks[a]
    return post,dict(case=c,nodes=nodes,room55=expected55,pool=pool_counts,
                low_page_accesses_only_FS0=True,invalid_readability_probes=0,original_object_deletions=deleted,native_steps=r['instruction_count'])

def run(c):
    args,checks,saved,file=execute(c)
    _,out=verify(c,args,checks,saved['result'])
    return dict(out,trace=str(file.relative_to(ROOT)),trace_sha256=sha(file.read_bytes()))

def main():
    before={str(p):probe.sha(p.read_bytes()) for p in [probe.EXE,probe.SOURCE,probe.SOURCE.parent/'Rooms.kwd',probe.SOURCE.parent/'Terrain.kwd']}
    rows=[]
    for c in CASES:
        row=run(c);rows.append(row);print(c['name'],len(row['nodes']),row['pool'],flush=True)
    assert before=={p:probe.sha(probe.Path(p).read_bytes()) for p in before}
    room_trace=probe.room_context()[2]
    out=dict(status='bounded_native_checks_passed',publication='Phase179 full primary/Ghidra/package audit pending; not a completed goal.',
             room_terrain_loader_replay=dict(file=str(room_trace.relative_to(ROOT)),sha256=probe.sha(room_trace.read_bytes())),
             scope=__doc__,source_hashes=before,probe_sha256=probe.sha(probe.Path(probe.__file__).read_bytes()),scenes=rows)
    (ROOT/'phase179-crypt-object-scene-checks.json').write_text(json.dumps(out,indent=2),encoding='utf-8')

if __name__=='__main__':main()
