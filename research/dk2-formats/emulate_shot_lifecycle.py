"""Accepted original Shot first-tick/terminal damage scheduling and signed Data1."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_shot_lifecycle as p
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=p.area.sha
def cases():
    rows=[('start-factory',5,'factory',{}),('start-first-tick',5,'tick',{}),('start-second-tick',5,'tick',dict(pre_ticks=1)),
      ('start-creation-arg1',5,'tick',dict(creation_arg=1)),('start-end-before-tick',5,'end',{}),('start-tick-then-end',5,'end',dict(pre_ticks=1)),
      ('end-factory',6,'factory',{}),('end-first-tick',6,'tick',{}),('end-second-tick',6,'tick',dict(pre_ticks=1)),
      ('end-handler',6,'end',{}),('end-gate1',6,'end',dict(world_gate=1)),('end-gate2',6,'end',dict(world_gate=2)),
      ('end-tick-then-handler',6,'end',dict(pre_ticks=1)),('end-expire-tick',6,'tick',dict(health_delta=-31)),
      ('end-expire-then-handler',6,'end',dict(health_delta=-31,pre_ticks=1)),
      ('continuous-second-tick',1,'tick',dict(pre_ticks=1)),('start-no-decay-second-tick',5,'tick',dict(pre_ticks=1,flags=0)),
      ('start-negative-data1',5,'tick',dict(damage=0xFFFFFFB0)),('end-negative-data1',6,'end',dict(damage=0xFFFFFFB0)),
      ('start-radius-zero',5,'tick',dict(radius=0)),('start-data1-zero',5,'tick',dict(damage=0)),('start-gate1',5,'tick',dict(world_gate=1))]
    return [dict(name=n,mode=mode,root=root,**extra) for n,mode,root,extra in rows]
def execute(c):
    args,checks,saved,path=p.execute(c,False);r=saved['result']
    assert r['success'] and r['hit_return'] and not r['emulation_error'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    return args,checks,saved,path
def expected(c):
    ticks=c.get('pre_ticks',0)+int(c['root']=='tick');mode=c['mode']
    packets=(int(ticks>0) if mode==5 else (int(c['root']=='end' and c.get('world_gate',0)==0) if mode==6 else ticks))
    hp=[1000,1000];requests=[];health=32+c.get('health_delta',0)
    if c.get('health_delta'):requests.append((p.base.NODE,c['health_delta'],32))
    damage=c.get('damage',80);damage=damage if damage<0x80000000 else damage-0x100000000
    if c.get('radius',2)>0:
        for _ in range(packets):
            for i,a in enumerate(p.area.TARGETS[:2]):requests.append((a,-damage,hp[i]));hp[i]-=damage
    if c.get('flags',4)&4:health-=ticks
    return dict(HP=hp,requests=[list(x) for x in requests],ticks=ticks,packets=packets,shot_health=health,first_tick_flag=int(ticks>0),state=2 if c['root']=='end' else 0)
def verify(c,args,checks,r):
    e=expected(c);post=post_context(checks,r);node=post[p.f.BLOCK][p.base.NODE-p.f.BLOCK:p.base.NODE-p.f.BLOCK+371]
    u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    assert len(r['before_calls'])==len(args['before_calls']) and all(x['returned'] for x in r['before_calls'])
    assert [[x['target'],x['delta'],x['HP_before']] for x in r['hp_requests']]==e['requests'],(c['name'],r['hp_requests'],e)
    assert [u(post[a],0x40) for a in p.area.TARGETS[:2]]==e['HP']
    assert u(node,0x12D)==e['ticks'] and u(node,0x149)==e['first_tick_flag'] and u(node,0x40)==e['shot_health'] and u(node,0x16)==e['state'],(c['name'],node.hex(),e)
    assert u(node,0x125)==p.base.DEFINITION and u(post[p.f.WORLD],0x723B)==c.get('world_gate',0)
    assert post[p.base.DEFINITION][0x6B]==c['mode'] and post[p.base.DEFINITION][0x69]==0
    assert u(post[p.base.DEFINITION],0x37)==c.get('damage',80) and u(node,0x13D)==c.get('damage',80)
    assert post[p.base.RAW]==checks[p.base.RAW] and post[p.base.target.RAW]==checks[p.base.target.RAW]
    assert post[p.base.SHOT_VTABLE]==checks[p.base.SHOT_VTABLE]
    assert r['visited_instructions'].get('0x4ab180',0)==e['ticks'] and r['visited_instructions'].get('0x4aaf70',0)==int(c['root']=='end')
    assert r['visited_instructions'].get('0x4ac5a0',0)==e['packets']
    # Native tick has separate nonzero handles at2E and32 in this scene;
    # each receives its own slot30 positional update (4AB7C3/4AB7DD).
    assert u(post[p.SOUND_LOG],0)==2*e['ticks'],(c['name'],post[p.SOUND_LOG].hex())
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=p.area.EXE.read_bytes()
    for row in r['observed_reads']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(p.area.pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],row)
    return post,dict(**e,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])
def main():
    rows=[]
    for c in cases():
        args,checks,saved,path=execute(c);post,out=verify(c,args,checks,saved['result'])
        rows.append(dict(case=c,file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=out));print(c['name'],{k:out[k] for k in ['HP','packets','ticks','shot_health','state']},flush=True)
    (ROOT/'phase148-shot-lifecycle-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')
if __name__=='__main__':main()
