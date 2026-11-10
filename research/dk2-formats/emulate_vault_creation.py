"""Original raw Object loader/factory/state42 idle-effect compositions.

No gameplay code replacements. Graphics query and null sound registration are
explicit external providers; periodic requests reach original null graphics code.
"""
import hashlib
import json
import struct
import sys
from pathlib import Path
import probe_mana_vault_factory as probe
import native_unicorn_factorytrace as cpu
import native_object_loading as loading
from object_state_tables import BASE, initialized_table
from emulate_element_colors import region
from build_effect_semantics import EXE

ROOT = Path(__file__).resolve().parent
f = probe.f


def cases():
    base = dict(ident=101, ticks=0, start=None, mana=0, misc=None, tps=20, fault=False)
    variants = [
        ('vault-created', {}), ('claimed-created', dict(ident=108)), ('gate-created', dict(ident=114)),
        ('vault-start59-overridden', dict(start=59, ticks=1)),
        ('claimed-start255-overridden', dict(ident=108, start=255, ticks=1)),
        ('ordinary-start16', dict(ident=17, start=16, ticks=1)),
        ('vault-before-period', dict(ticks=39)), ('vault-at-period', dict(ticks=40)),
        ('claimed-at-period', dict(ident=108, ticks=40)), ('gate-at-period', dict(ident=114, ticks=40)),
        ('vault-two-periods', dict(ticks=80)), ('vault-tps10', dict(tps=10, ticks=20)),
        ('vault-mana100', dict(mana=100, ticks=40)), ('vault-manaffff', dict(mana=65535, ticks=40)),
        ('vault-misc123', dict(misc=123, ticks=40)), ('vault-misc0', dict(misc=0, ticks=40)),
        ('ordinary-state42-created', dict(ident=17, start=42)),
        ('ordinary-state42-divide-zero', dict(ident=17, start=42, ticks=1, fault=True)),
    ]
    return [dict(base, **v, name=n) for n, v in variants]


def build(c):
    args, factory, checks, factory_case = probe.build(**{k:c[k] for k in ['ident','ticks','start','mana','misc','tps']})
    memory = {int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']}
    wanted = {a:bytearray(b) for a,b in checks.items()}
    # Existing independently verified pool/map/owner registration expectations.
    old_args, old_expected = f.build(factory_case, 0)
    old = {int(r['address'],16):bytes.fromhex(r['hex']) for r in json.loads(old_args['memory'])['regions']}
    shadow = 0x11000000
    for row in old_expected['checks']:
        a,n = int(row['address'],16),row['bytes']
        expected = old[shadow]
        shadow += ((n+255)//256)*256
        initial = next(b[a-p:a-p+n] for p,b in old.items() if p<0x11000000 and p<=a and a+n<=p+len(b))
        target = next((p for p,b in wanted.items() if p<=a and a+n<=p+len(b)),None)
        assert target is not None
        for i,(before,after) in enumerate(zip(initial,expected)):
            if before!=after:
                wanted[target][a-target+i]=after
    raw = bytes(memory[probe.RAW])
    mid = loading.intermediate(raw,bytes(1022))
    definition = loading.definition(mid,checks[f.DEFINITION],loading.DX,c['tps'],1)
    wanted[probe.MID],wanted[f.DEFINITION],wanted[BASE] = bytearray(mid),bytearray(definition),bytearray(initialized_table())
    f.put(wanted[f.OUT],0,0)  # No synthetic factory-return observer executes.
    obj = bytearray((ROOT/'ghidra/phase89-object-factory-base.bin').read_bytes())
    active = 42 if c['ident'] in [101,108,114] else definition[0x11B]
    timer = 2*c['tps'] if c['ident'] in [101,108,114] else 0
    for off,value,n in [(0x2E,0,4),(0x32,0,4),(0x11C,int.from_bytes(definition[0x100:0x102],'little'),2),
                         (0x11E,int.from_bytes(definition[0xFE:0x100],'little'),2),
                         (0x40,int.from_bytes(definition[0x104:0x106],'little'),4),
                         (0x12C,c['ident'],1),(0x13A,active,4),(0x14A,timer,4),
                         (0x13E,c['ticks'],4),(0x6A,c['ticks']-int(c['fault']),2)]:
        f.put(obj,off,value,n)
    node=factory['node'];wanted[f.BLOCK][node-f.BLOCK:node-f.BLOCK+393]=obj
    count = c['ticks']//timer if timer else 0
    effect = int.from_bytes(definition[0x117:0x119],'little')
    e=dict(active=active,age=c['ticks'],timer=timer,effect_id=effect,effect_requests=count,
           expected_divide_zero=c['fault'],getter_return=active if c['ticks']==0 else 1)
    if count:
        args['observe_reads'].append([0x7FFEF000,8192])
    args['memory']=json.dumps(dict(regions=[region(a,b) for a,b in memory.items()]))
    return args,e,checks,wanted,node


def verify(c,args,e,checks,wanted,node,r):
    assert all(x['returned'] for x in r['before_calls']) and len(r['before_calls'])==5+max(0,c['ticks']-1)
    if c['fault']:
        assert not r['success'] and not r['hit_return'] and int(r['registers']['EIP'],16)==0x4A3568
        assert 'UC_ERR_EXCEPTION' in r['emulation_error'] and int(r['registers']['ECX'],16)==0
    else:
        assert r['success'] and r['hit_return'] and int(r['registers']['EAX'],16)==e['getter_return']
    assert not r['protected_write_or_execution_failures']
    post={a:bytearray(b) for a,b in checks.items()}
    for row in r['context_mismatches']:
        a=int(row['address'],16);assert len(post[a])==row['length']
        for d in row['differences']:
            assert post[a][d['offset']]==d['expected'];post[a][d['offset']]=d['actual']
    assert post==wanted, (c['name'],[(hex(a),[(hex(i),x,y) for i,(x,y) in enumerate(zip(post[a],wanted[a])) if x!=y][:20]) for a in post if post[a]!=wanted[a]])
    assert post[f.BLOCK][node-f.BLOCK:node-f.BLOCK+393].hex()==r['captured_hex']
    assert not any(int(x['address'],16)<f.DEFINITION+0x108 and int(x['address'],16)+x['size']>f.DEFINITION+0x106 for x in r['observed_reads'])
    visits=r['visited_instructions']
    assert visits[hex(f.GFX_CB)]==1 and visits[hex(f.SOUND_CB)]==2
    assert visits.get('0x509580',0)==visits.get('0x4430d0',0)==e['effect_requests']
    words=[int.from_bytes(bytes.fromhex(x['value_hex']),'little') for x in r['observed_reads'] if int(x['pc'],16)==0x50E4B5]
    assert len(words)==e['effect_requests'] and all(w&65535==e['effect_id'] for w in words)
    assert '0x4b7a90' in visits and '0x49b1c0' in visits
    if c['ident'] in [101,108,114]:assert '0x49b512' in visits and '0x49ef60' in visits


def main():
    selected=cases()
    if len(sys.argv)>1:selected=[c for c in selected if c['name']==sys.argv[1]];assert selected
    rows=[];backend=hashlib.sha256((ROOT/'native_unicorn_factorytrace.py').read_bytes()).hexdigest()
    for c in selected:
        args,e,checks,wanted,node=build(c)
        tag=hashlib.sha256((json.dumps(args,sort_keys=True)+backend).encode()).hexdigest()[:10]
        p=ROOT/'ghidra'/f'phase111-vault-{c["name"]}-{tag}.json'
        if p.exists():
            value=json.loads(p.read_text(encoding='utf8'));assert value['case']==c and value['input']==args and value['expected']==e
        else:
            r=cpu.run(EXE,args,list(checks.items()),f.OBS,node,393)
            value=dict(case=c,input=args,expected=e,node=node,result=r);p.write_text(json.dumps(value,indent=2),encoding='utf8')
        verify(c,args,e,checks,wanted,node,value['result'])
        rows.append(dict(case=c['name'],file=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
        print(c['name'],e,'verified',flush=True)
    if len(selected)==len(cases()):(ROOT/'phase111-vault-creation-emulations.json').write_text(json.dumps(dict(scope=__doc__,replays=rows),indent=2),encoding='utf8')


if __name__=='__main__':main()
