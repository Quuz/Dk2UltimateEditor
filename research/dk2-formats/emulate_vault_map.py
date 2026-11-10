"""Original loaders -> optional old Vault factory -> complete CMap replacement -> full Player mana.

One CPU/memory per case. Prepared ground navigation, owners7/8, UI disabled,
external null graphics/sound and bounded IsBadReadPtr import. No game-code,
IAT or vtable replacements. Old Vault HP0 is not final scheduler removal.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import probe_vault_map as probe
import native_unicorn_declared_imports as cpu
import native_object_loading as objects
import native_terrain_catalogue as terrains
import emulate_factory_mana as mana
from object_state_tables import BASE, initialized_table
from emulate_element_colors import region
from build_effect_semantics import EXE,pe_read

ROOT=Path(__file__).resolve().parent
f=probe.f
TABLE,OTHER=0x10800000,0x100A0000


def cases():
    base=dict(old_terrain=31,new_terrain=32,old_owner=8,new_owner=7,old_object=True,
              gain=None,ceiling=None,mana_gen=None,income_ceiling=500)
    return [dict(base,**v,name=n) for n,v in [
        ('place-claimed',dict(old_terrain=3,old_object=False)),('claim-existing',{}),
        ('claim-edited',dict(gain=125,ceiling=200)),('claim-signed',dict(gain=65535,ceiling=65526)),
        ('revert-to-unclaimed',dict(old_terrain=32,new_terrain=31)),
        ('same-owner-cap80',dict(old_owner=7,income_ceiling=80)),
        ('claim-object-manaffff',dict(mana_gen=65535))]]


def s16(v):return v if v<32768 else v-65536


def build(c):
    args,placement,_=probe.build_map({k:c[k] for k in ['old_terrain','new_terrain','old_owner','new_owner','old_object']})
    memory={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    # Same explicit mana context used in phase111; both recipient records exist.
    memory[0x6E5054]=bytearray(struct.pack('<I',f.WORLD))
    memory[0x75AD4B]=bytearray(struct.pack('<I',1));memory[0x75AEA7]=bytearray(struct.pack('<H',1))
    f.put(memory[f.SESSION],4,100)
    for off,v in [(0x82,1000),(0x86,1),(0x9A,0),(0xAE,0xFFFFFFFF),(0x19A,1),
                  (0x10E2,20),(0x10E6,100),(0x10DE,c['income_ceiling']),(0x10EA,10000)]:
        f.put(memory[f.PLAYER],off,v)
    for off,v in [(0x19A,1),(0x10E2,20),(0x10DE,500)]:f.put(memory[OTHER],off,v)
    f.put(memory[OTHER],0xB3F,int(c['old_owner']==8));f.put(memory[f.PLAYER],0xB3F,int(c['old_owner']==7))
    for off,name in [(0x19C,'gain'),(0x19E,'ceiling')]:
        if c[name] is not None:f.put(memory[0x10812000],off,c[name],2)
    if c['mana_gen'] is not None:
        for a in [probe.source.RAW]+([probe.source.RAW+0x2000] if c['old_object'] else []):
            f.put(memory[a],0x34C,c['mana_gen'],2)
    args['before_calls'].append(dict(address='0x4c10e0',registers=dict(ECX=hex(f.PLAYER)),stack_args=[]))
    args['observe_reads']=[[f.DEFINITION,len(memory[f.DEFINITION])],[TABLE,33*460],[0,4096]]
    args['observe_writes']=[[f.BLOCK,len(memory[f.BLOCK])],[f.PLAYER,0x1200],[OTHER,0x1200],[f.WORLD,0x8000],[f.MAP,16384]]
    probe.add_import(args,memory)
    checks={a:bytes(b) for a,b in memory.items() if a not in [0x7FFEF000,f.GFX_CB,f.SOUND_CB,probe.imported.ADDRESS]}
    wanted={a:bytearray(b) for a,b in checks.items()}
    wanted[BASE]=bytearray(initialized_table())
    for index,ident in enumerate([3,31,32]):
        ra,mi=0x10810000+index*0x1000,0x10820000+index*0x1000
        mid=terrains.intermediate(memory[ra]);off=ident*460
        definition,_=terrains.definition(mid,bytes(memory[TABLE][off:off+460]),1)
        wanted[mi]=bytearray(mid);wanted[TABLE][off:off+460]=definition
    object_defs={}
    for i,ident in enumerate([101,108] if c['old_object'] else [{31:101,32:108}[c['new_terrain']]]):
        ra,mi=probe.source.RAW+i*0x2000,probe.source.MID+i*0x2000
        mid=objects.intermediate(memory[ra],bytes(1022));off=i*285
        definition=objects.definition(mid,bytes(memory[f.DEFINITION][off:off+285]),objects.DX,20,1)
        wanted[mi]=bytearray(mid);wanted[f.DEFINITION][off:off+285]=definition
        object_defs[ident]=(f.DEFINITION+off,definition)
    # Independent allocator/list projection. Fresh creations append to the used pool list.
    count=1+int(c['old_object']);remaining=1024 if c['old_object'] else 512
    previous_header=None;nodes=[];tags=[]
    final_owner=c['new_owner'] if c['new_terrain']==32 else 8
    identities=([{31:101,32:108}[c['old_terrain']]] if c['old_object'] else [])+[{31:101,32:108}[c['new_terrain']]]
    owners=([c['old_owner']] if c['old_object'] else [])+[final_owner]
    owner_previous={}
    for i,(ident,owner) in enumerate(zip(identities,owners)):
        remaining-=408;header=f.BLOCK+remaining;node=header+12;tag=100+i
        nodes.append(node);tags.append(tag)
        f.put(wanted[f.BLOCK],0,remaining)
        struct.pack_into('<III',wanted[f.BLOCK],remaining,408,previous_header or f.UH,f.UT)
        if previous_header is not None:f.put(wanted[f.BLOCK],previous_header-f.BLOCK+8,header)
        if i==0:f.put(wanted[f.UH],8,header)
        f.put(wanted[f.UT],4,header)
        previous_header=header
        definition_address,definition=object_defs[ident]
        obj=bytearray((ROOT/'ghidra/phase89-object-factory-base.bin').read_bytes())
        for off,value,n in [(4,tag,2),(8,tags[i-1] if i else 0,2),(0xC,tags[i-1] if i else 0,2),
                            (0x28,owner,2),(0x2A,f.PLAYER if owner==7 else OTHER,4),
                            (0x2E,0,4),(0x32,0,4),(0x22,0x1000,4),(0xFE,0,4),
                            (0x40,0 if i<count-1 else int.from_bytes(definition[0x104:0x106],'little'),4),
                            (0x6C,definition_address+0x51,4),(0x93,tag,2),
                            (0x11C,int.from_bytes(definition[0x100:0x102],'little'),2),
                            (0x11E,int.from_bytes(definition[0xFE:0x100],'little'),2),
                            (0x12C,ident,1),(0x12D,definition_address,4),(0x13A,42,4),(0x13E,0,4),(0x14A,40,4)]:
            f.put(obj,off,value,n)
        if i:
            for off in [6,0xA]:f.put(wanted[f.BLOCK],nodes[i-1]-f.BLOCK+off,tag,2)
        if owner in owner_previous:
            previous_node,previous_tag=owner_previous[owner]
            f.put(obj,0x10,previous_tag,2);f.put(wanted[f.BLOCK],previous_node-f.BLOCK+0xE,tag,2)
        owner_previous[owner]=(node,tag)
        wanted[f.BLOCK][node-f.BLOCK:node-f.BLOCK+393]=obj
        target=f.PLAYER if owner==7 else OTHER
        f.put(wanted[target],0xA,tag,2)
        old_count=int.from_bytes(wanted[target][0x1E:0x22],'little');f.put(wanted[target],0x1E,old_count+1)
        f.put(wanted[0x6EE540],4*tag,node);wanted[0x6ED540][tag]=1
    f.put(wanted[f.WORLD],0x2A,count);f.put(wanted[f.WORLD],0x76,408*count)
    f.put(wanted[f.WORLD],0x9F6,100)
    f.put(wanted[0x6A4818],0,4094-count);f.put(wanted[0x6A4818],4,100+count)
    f.put(wanted[f.KINDS],4,tags[-1],2)
    f.put(wanted[f.OUT],8,2*count);f.put(wanted[f.OUT],12,count)
    if c['old_object']:f.put(wanted[f.OUT],4,nodes[0])
    f.put(wanted[probe.imported.COUNTER],0,count)
    cell=(6*32+5)*16;newrow=wanted[TABLE][c['new_terrain']*460:(c['new_terrain']+1)*460]
    f.put(wanted[f.MAP],cell,tags[-1],2);f.put(wanted[f.MAP],cell+6,int.from_bytes(newrow[0x1AB:0x1AD],'little'),2)
    f.put(wanted[f.MAP],cell+8,final_owner,2);f.put(wanted[f.MAP],cell+10,c['new_terrain'],1)
    oldraw=memory[0x10810000+[3,31,32].index(c['old_terrain'])*0x1000]
    newraw=memory[0x10810000+[3,31,32].index(c['new_terrain'])*0x1000]
    old_gain,old_cap=struct.unpack_from('<hh',oldraw,0x19C);gain,cap=struct.unpack_from('<hh',newraw,0x19C)
    oldplayer=f.PLAYER if c['old_owner']==7 else OTHER
    def add(address,offset,amount):
        v=int.from_bytes(wanted[address][offset:offset+4],'little');f.put(wanted[address],offset,v+amount)
    # 455B90 adjusts the old owner's income for a terrain-type change, but not its ceiling.
    add(oldplayer,0x10E2,gain-old_gain)
    if final_owner!=c['old_owner']:
        for off,v in [(0xB3F,1),(0x10E2,gain),(0x10DE,cap)]:
            add(oldplayer,off,-v);add(f.PLAYER if final_owner==7 else OTHER,off,v)
    extra=mana.s32(int.from_bytes(wanted[f.PLAYER][0x10E2:0x10E6],'little'))
    ceiling=mana.s32(int.from_bytes(wanted[f.PLAYER][0x10DE:0x10E2],'little'))
    delta=min(extra+100,ceiling);current=max(0,1000+delta) if delta<0 else min(1000+delta,10000)
    for off,v in [(0x82,current),(0x9A,100),(0x76,0x100)]:f.put(wanted[f.PLAYER],off,v)
    expected=dict(old_object=c['old_object'],old_node=nodes[0] if c['old_object'] else None,new_node=nodes[-1],
                  actual_owner=final_owner,factories=count,old_gain=old_gain,old_ceiling=old_cap,
                  gain=gain,ceiling=cap,player_income=extra,player_ceiling=ceiling,mana=current,
                  object_tags=tags,old_HP_after_replacement=0 if c['old_object'] else None,
                  old_object_still_registered=c['old_object'])
    return args,expected,checks,wanted


def verify(c,args,e,checks,wanted,r):
    assert r['success'] and r['hit_return'] and not r['protected_write_or_execution_failures']
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==len(args['before_calls'])
    post={a:bytearray(b) for a,b in checks.items()}
    for row in r['context_mismatches']:
        a=int(row['address'],16);assert len(post[a])==row['length']
        for d in row['differences']:
            assert post[a][d['offset']]==d['expected'];post[a][d['offset']]=d['actual']
    assert post==wanted,(c['name'],[(hex(a),[(hex(i),x,y) for i,(x,y) in enumerate(zip(post[a],wanted[a])) if x!=y][:20]) for a in post if post[a]!=wanted[a]])
    node=e['new_node'];assert post[f.BLOCK][node-f.BLOCK:node-f.BLOCK+393].hex()==r['captured_hex']
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;data=EXE.read_bytes()
    for x in r['observed_reads']:
        if int(x['address'],16)<4096:
            assert int(x['address'],16)==0 and x['size']==4
            ins=next(md.disasm(pe_read(data,int(x['pc'],16),15),int(x['pc'],16)))
            assert 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    visits=r['visited_instructions']
    for a in [0x44FC40,0x455B90,0x4559D0,0x4D1930,0x4C10E0,0x4C17A0]:assert hex(a) in visits
    assert visits['0x4b7a90']==e['factories'] and visits.get('0x4b2ad0',0)==int(c['old_object'])
    if c['old_object']:assert '0x4537e0' in visits
    for x in r['observed_reads']:
        a=int(x['address'],16)
        assert not any(a<d+0x108 and a+x['size']>d+0x106 for d in [f.DEFINITION]+([f.DEFINITION+285] if c['old_object'] else []))
        assert not any(a<TABLE+i*460+o+n and a+x['size']>TABLE+i*460+o
                       for i in [3,31,32] for o,n in [(0x183,4),(0x1B5,1)])


def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    backend=''.join(hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in ['native_unicorn_declared_imports.py','native_unicorn_factorytrace.py'])
    rows=[]
    for c in selected:
        args,e,checks,wanted=build(c);tag=hashlib.sha256((json.dumps(args,sort_keys=True)+backend).encode()).hexdigest()[:10]
        p=ROOT/'ghidra'/f'phase112-vault-map-{c["name"]}-{tag}.json'
        if p.exists():
            v=json.loads(p.read_text(encoding='utf8'));assert v['input']==args and v['expected']==e and v['case']==c
        else:
            r=cpu.run(EXE,args,list(checks.items()),f.OBS,e['new_node'],393)
            v=dict(case=c,input=args,expected=e,result=r);p.write_text(json.dumps(v,indent=2),encoding='utf8')
        verify(c,args,e,checks,wanted,v['result'])
        rows.append(dict(case=c['name'],file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
        print(c['name'],e,'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase112-vault-map-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':main()
