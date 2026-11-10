"""Accepted new raw bit29/melee and separate whole animation-selection contexts."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_melee_animation_flag as melee
import probe_instance_animation as animation
from portal_animation_tables import BASE,initialized_animations
from emulate_resistance import post_context
ROOT=Path(__file__).resolve().parent
sha=melee.cpu.sha
def cases():
    out=[]
    for enabled in [False,True]:
        for initial in [False,True]:out.append(dict(name=f'melee-{int(enabled)}-initial{int(initial)}',kind='melee',enabled=enabled,initial_runtime=initial))
        for key in ['missing_target','cooldown']:out.append(dict(name=f'melee-{int(enabled)}-{key}',kind='melee',enabled=enabled,initial_runtime=not enabled,**{key:True}))
        for moving in [False,True]:out.append(dict(name=f'animation-{int(enabled)}-moving{int(moving)}',kind='animation',enabled=enabled,moving=moving))
        out.append(dict(name=f'animation-{int(enabled)}-no-action',kind='animation',enabled=enabled,moving=False,action=0))
    return out
def execute(c):
    module=melee if c['kind']=='melee' else animation
    args,checks,saved,p=module.execute(c,False);r=saved['result']
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    return args,checks,saved,p
def verify(c,args,checks,r):
    post=post_context(checks,r);actor=post[melee.cf.ACTOR];definition=post[melee.cf.DEFINITION]
    u=lambda a,o,n=4:int.from_bytes(post[a][o:o+n],'little')
    assert bool(u(melee.cf.ACTOR,0x3EA)&0x1000)==c['enabled']
    assert bool(u(melee.cf.DEFINITION,0x686)&0x20000000)==c['enabled']
    assert post[BASE]==initialized_animations()
    assert struct.unpack_from('<I',post[BASE],17*29)[0]==0x498E30 and post[BASE][17*29+20]==5
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=melee.prior.EXE.read_bytes()
    for row in r['observed_reads']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(melee.prior.pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str,(c['name'],row)
    if c['kind']=='melee':
        assert len(r['before_calls'])==7 and all(x['returned'] for x in r['before_calls'])
        assert post[melee.prior.RAW]==checks[melee.prior.RAW]
        assert u(melee.cf.DEFINITION,0x686)==u(melee.prior.RAW,0xEFC)
        assert u(melee.prior.MID,0x12EC)==u(melee.prior.RAW,0xEFC)
        early=c.get('missing_target') or c.get('cooldown')
        hp=u(melee.attack.TARGET,0x40);assert hp==(1000 if early else 334)
        assert int(r['registers']['EAX'],16)==int(bool(c.get('cooldown')))
        assert r['visited_instructions'].get('0x498e30')==1
        out=dict(runtime_PlayInstanceAnimation=c['enabled'],target_HP=hp,damage_HP=1000-hp,root_return=int(r['registers']['EAX'],16),early=bool(early))
    else:
        assert len(r['before_calls'])==1 and r['before_calls'][0]['returned']
        chosen=[x for x in r['observed_reads'] if x['pc']=='0x48c5a8'];assert len(chosen)==1
        slot=(int(chosen[0]['address'],16)-melee.cf.DEFINITION-15)//27
        wanted=39 if not c.get('action',17) or (not c['enabled'] and not c['moving']) else 5
        assert slot==wanted,(c,slot,wanted)
        assert u(melee.attack.TARGET,0x40)==334
        assert post[melee.prior.RAW]==checks[melee.prior.RAW] and post[melee.cf.DEFINITION]==checks[melee.cf.DEFINITION]
        assert r['visited_instructions'].get('0x48c392',0)==int(not c['moving'] and c.get('action',17)!=0)
        out=dict(runtime_PlayInstanceAnimation=c['enabled'],chosen_resource_slot=slot,resource_name='MELEE_ATTACK' if slot==5 else 'STAND_STILL',moving=c['moving'],action=c.get('action',17),separate_prepared_render_context=True)
    return post,dict(**out,compared_regions=len(post),compared_bytes=sum(map(len,post.values())),native_steps=r['instruction_count'])
def main():
    rows=[];states={}
    for c in cases():
        args,checks,saved,p=execute(c);post,out=verify(c,args,checks,saved['result']);states[c['name']]=post
        rows.append(dict(case=c,file=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes()),outcome=out));print(c['name'],out,flush=True)
    off=states['melee-0-initial0'];on=states['melee-1-initial0'];diff=[]
    assert off.keys()==on.keys()
    for a,b in off.items():
        changes=[i for i,(x,y) in enumerate(zip(b,on[a])) if x!=y]
        if changes:diff.append(dict(region=hex(a),offsets=changes))
    (ROOT/'phase147-melee-animation-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,whole_raw_melee_pair_differences=diff),indent=2),encoding='utf8')
if __name__=='__main__':main()
