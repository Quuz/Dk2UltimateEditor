"""Raw EffectElement -> constructor/converter/loader -> spawn/update/render.

Independent selected scalar/color/queue/guard checks plus whole-memory palette
pairs. The runtime definition's unknown terminal bytes remain semantically open.
"""
import json,struct
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_raw_element_palette as probe
from native_element_colors import initial_color,alpha_threshold,update_projection
from emulate_resistance import post_context
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
base=probe.base
sha=probe.cpu.sha
VTABLES=[(0x66CC94,0x164),(0x66CBDC,0x20),(0x66D0C4,0x40),(0x66FCCC,0x28),(0x66FCF4,0x28)]

def cases():
    rows=[]
    for ident,ticks,stage in [(294,0,'spawn'),(294,1,'live'),(294,126,'late'),(294,128,'zero'),(1,31,'cloud-late')]:
        for palette in [(0,0),(255,0),(0,255),(255,255)]:
            rows.append(dict(name=f'{stage}-{palette[0]}-{palette[1]}',ident=ident,ticks=ticks,palette=list(palette),spawn=True,render=True,rgb=None))
    for ident in [1,294]:rows.append(dict(name=f'loader-{ident}',ident=ident,ticks=0,palette=[0,0],spawn=False,render=False,rgb=None))
    rows.append(dict(name='RGB-control',ident=294,ticks=126,palette=[0,0],spawn=True,render=True,rgb=[40,80,120]))
    return rows

def build(c):
    args,checks=probe.build(**{k:c[k] for k in ['ident','palette','ticks','spawn','render','rgb']})
    for a,n in VTABLES:checks[a]=pe_read(EXE.read_bytes(),a,n)
    return args,checks

def execute(c):
    args,checks=build(c)
    tag=sha((json.dumps(args,sort_keys=True)+sha((ROOT/'native_unicorn_factorytrace.py').read_bytes())+json.dumps({hex(a):sha(b) for a,b in checks.items()},sort_keys=True)).encode())[:12]
    path=ROOT/'ghidra'/f'phase140-element-chain-native-{tag}.json'
    if path.exists():saved=json.loads(path.read_text(encoding='utf8'));assert saved['input']==args
    else:
        r=probe.cpu.run(EXE,args,list(checks.items()),probe.STOP,base.NODE,287)
        saved=dict(case=c,input=args,result=r);path.write_text(json.dumps(saved,indent=2),encoding='utf8')
    r=saved['result'];assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures'],(c['name'],r['emulation_error'],r['trace_tail'])
    return args,checks,saved,path

def verify(c,args,checks,r):
    post=post_context(checks,r);raw=checks[probe.RAW];mid=post[probe.MID];definition=post[base.DEF]
    for a,n in VTABLES:assert post[a]==pe_read(EXE.read_bytes(),a,n)
    u=lambda b,o,n=4:int.from_bytes(b[o:o+n],'little')
    assert post[probe.RAW]==raw and raw[0xB1:0xB3]==bytes(c['palette'])
    assert mid[0x11D:0x11F]==definition[0x4D:0x4F]==bytes(c['palette'])
    for a,size in [(probe.RAW,182),(probe.MID,290),(base.DEF,79)]:assert post[a][size:]==bytes([165])*64
    assert u(definition,0x3E,2)==u(raw,0xA0,2)==c['ident']
    assert u(definition,0x1F)==u(raw,0x9C) and definition[0x4A:0x4D]==raw[0xAE:0xB1]
    assert definition[0xA]==raw[0x70] and u(definition,0xB)==u(raw,0x60) and u(definition,0xF)==0x1234
    assert u(post[probe.IO],0)==1
    expected_calls=4+int(c['spawn'])+int(c['ticks']!=0)+int(c['spawn'] and c['render'])
    assert len(r['before_calls'])==expected_calls and all(x['returned'] for x in r['before_calls'])
    assert int(r['registers']['EAX'],16)==0
    pcs={int(a,16) for a in r['visited_instructions']}
    overlap=[]
    for row in r['observed_reads']:
        address=int(row['address'],16)
        if address<base.DEF+79 and address+row['size']>base.DEF+77:overlap.append(row)
    rgba=None;remaining=None;render_type=None
    if c['spawn']:
        node=post[base.BLOCK][base.NODE-base.BLOCK:base.NODE-base.BLOCK+287]
        assert u(post[base.OUT],4)==base.NODE and u(node,0)==0x66CBDC and u(node,0x103)==base.DEF
        assert u(node,0x101,2)==c['ident'] and u(node,0x107,2)==7
        assert u(raw,0xA2,2)==u(raw,0xA4,2)
        remaining=u(raw,0xA2,2)-c['ticks'];assert u(node,0x109)==remaining&0xFFFFFFFF
        threshold=alpha_threshold(u(raw,0xA2,2),raw[0xB3])['unsigned'];assert u(node,0x10D)==threshold
        color=initial_color(raw[0x70],u(raw,0x60),list(raw[0xAE:0xB1]))
        expected=update_projection(color['channels'],u(raw,0xA2,2),c['ticks'],threshold,u(raw,0x9C),raw[0x70])
        assert node[0x6E:0x72]==bytes(expected['channels'])
        rgba=list(expected['channels'])
        # Render wrapper uses the actual freshly initialized/updated instance.
        assert u(post[probe.graphics.QUEUE],0x30)==probe.graphics.HEAP+20
        heap=post[probe.graphics.HEAP];render_type=u(heap,20);assert render_type in [0x66FCF4,0x66FCCC]
        assert post[probe.graphics.HEAP][128:]==checks[probe.graphics.HEAP][128:]
        assert u(post[0x765B00],0xA0+4*28)==0
        assert u(post[base.PARENT],0x113)==base.NODE and u(post[0x6D6458],4)==base.NODE
        # One allocation of300bytes; creation-only list is adequate here.
        assert u(post[base.BRIDGE],0x135F)==300 and u(post[base.BLOCK],0)==724
        assert post[base.BLOCK][1023:]==checks[base.BLOCK][1023:]
    else:
        assert post[base.BLOCK]==checks[base.BLOCK]
    assert (0x4352E0 in pcs)==c['spawn'] and (0x436430 in pcs)==bool(c['ticks'])
    assert (0x599C60 in pcs)==c['spawn']
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    return post,dict(lifetime_after=remaining,RGBA=rgba,resource_type=definition[0xA],render_vtable=hex(render_type) if render_type else None,
        unknown_definition_reads=overlap,source_unknown_bytes=list(definition[0x4D:0x4F]),
        whole_regions=len(post),whole_bytes=sum(map(len,post.values())),steps=r['instruction_count'],
        full_physics_calls=r['visited_instructions'].get('0x4476e0',0),
        scope='Complete selected chain; external lookup handle/connected-engine queries anduniform coefficients explicitly prepared. Output opaque bytes retain unknown meaning.')

def pair(reference,changed,c):
    expected={a:bytearray(b) for a,b in reference.items()}
    for a,o in [(probe.RAW,0xB1),(probe.MID,0x11D),(base.DEF,0x4D)]:expected[a][o:o+2]=bytes(c['palette'])
    assert changed=={a:bytes(b) for a,b in expected.items()},(c['name'],[(hex(a),[hex(i) for i,(s,t) in enumerate(zip(changed[a],expected[a])) if s!=t][:14]) for a in changed if changed[a]!=expected[a]])
    return dict(case=c['name'],whole_regions=len(reference),whole_bytes=sum(map(len,reference.values())),
        only_differences='The same two bytes in raw record, intermediate and runtime definition. All other prepared data including instance, RNG, physics outputs andrender heap/queue is byte-identical.',
        unknown_meaning_still_unresolved=True)

def main():
    rows=[];groups={};pairs=[]
    for c in cases():
        args,checks,saved,path=execute(c);post,outcome=verify(c,args,checks,saved['result'])
        key=(c['ident'],c['ticks'])
        if c['spawn'] and c['rgb'] is None:
            if c['palette']==[0,0]:groups[key]=post
            else:pairs.append(pair(groups[key],post,c))
        rows.append(dict(case=c['name'],file=str(path.relative_to(ROOT)),sha256=sha(path.read_bytes()),outcome=outcome))
        print(c['name'],outcome['lifetime_after'],outcome['RGBA'],len(outcome['unknown_definition_reads']),flush=True)
    v=dict(phase=140,scope=__doc__,replays=rows,paired_controls=pairs)
    (ROOT/'phase140-element-palette-chain-emulations.json').write_text(json.dumps(v,indent=2),encoding='utf8')

if __name__=='__main__':main()
