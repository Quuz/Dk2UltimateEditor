"""Publish successful native Object/gold rewards without changing phase88 facts."""
from pathlib import Path
import json,hashlib,struct
import emulate_object_factory as factory
import emulate_sacrifice_objects as temple
import memory_readout_ymm as readout
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def canonical(v):return digest(json.dumps(v,ensure_ascii=False,sort_keys=True).encode())
def primary():
    m=load('phase89-primary-manifest.json');out={}
    for key in ['functions','blocks']:
        rows=[]
        for r in m[key]:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==r['length']==v['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            rows.append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
        out[key]=rows
    out['object_rtti']=m['object_rtti'];assert len(out['functions'])==44
    return out
def native_results(module,file,temple_mode):
    rows=load(file)['replays'];by={c['name']:c for c in module.cases()};images={};outcomes=[];all_pages={}
    for row in rows:
        p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c=by[row['case']];page=row['page'];built=module.build(c,page);args,e=built[:2];r=saved['result']
        assert saved['case']==c and saved['input']==args and saved['expected']==e and digest(p.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1 if temple_mode else 0).items())
        if 'EBX' in args['return_registers'].split(','):
            assert int(r['registers']['EBX'],16)==int(e['success']), (c['name'],r['registers']['EBX'])
        all_pages.setdefault(c['name'],{})[page]=readout.decode(r['registers'])
    for name,c in by.items():
        assert set(all_pages[name])=={0,1};b=all_pages[name][0]+all_pages[name][1];b=b[:393];built=module.build(c,0);e=built[1];fc=built[2] if temple_mode else c
        factory.verify_node(fc,b,e);path='ghidra/phase89-'+('sacrifice-object-' if temple_mode else 'object-factory-')+name+'.bin';assert (ROOT/path).read_bytes()==b
        if temple_mode and c['kind']==3 and e['success']:
            u=lambda o:int.from_bytes(b[o:o+4],'little');scale=4548 if c['value']>=5000 else 2048+c['value']//2
            assert u(0x14a)==c['value'] and u(0x48)==u(0x4c)==scale and u(0x58)==u(0x5c)==0
        images[name]=b;outcomes.append(dict(case=name,expected=e,capture=link(path),capture_is_object=e['success']))
    assert len(rows)==2*len(by)
    return rows,outcomes,images
def main():
    p=primary();fr,fo,fi=native_results(factory,'phase89-object-factory-emulations.json',False);tr,to,ti=native_results(temple,'phase89-sacrifice-object-emulations.json',True)
    for n in ['object-high-bits','raw-object','raw-object-high-bits']:assert ti[n]==ti['object']
    assert int.from_bytes(ti['object-gold-bag'][0x14a:0x14e],'little')==0 and int.from_bytes(ti['gold5000'][0x14a:0x14e],'little')==5000
    before=load('sacrifice-semantics.json');before.pop('phase89_object_rewards',None)
    prior=ROOT/'phase89-prior-components.json'
    if not prior.exists():prior.write_text(json.dumps(load('editor-format-package.json')['components'],ensure_ascii=False,indent=2),encoding='utf8')
    corpus=load('phase88-sacrifice-corpus.json')
    source=next(r for r in corpus['files'] if r['recipes']==44);assert digest(Path(source['file']).read_bytes())==source['sha256']
    s=dict(phase=89,date='2026-10-28',profile=before['profile'],phase88_base_canonical_sha256=canonical(before),primary=p,
      native_factory_replays=fr,native_reward_replays=tr,factory_outcomes=fo,reward_outcomes=to,
      api_helpers=[link(n) for n in ['emulate_object_factory.py','emulate_sacrifice_objects.py','fixture_read_probe.py']],
      storage=dict(object_definition_bytes=285,object_instance_bytes=393,object_native_vtable='66D654',object_id_instance=0x12c,definition_pointer_instance=0x12d,
        pool_payload_request=393,pool_alignment=4,pool_header_bytes=12,accounted_allocation_bytes=408),
      object_reward=dict(raw_value='Original DWORD preserved; factory ID uses low8 only',owner='Global WORD6F2548, independently of room ownerWORD1D',
        position='4E4D90 chooses room tile; Object placement4B5020 clamps Z below floor after native CThing→CMovingThing RTTI',
        fixture='Room owner8, global object-owner7; argument point9.5/10.5 ignored; chosen tile5/6; requestedZ0 becomes4096',
        failure='ID0, pool too small/empty or global tag gate0 create no Object. Temple history remains cleared; no retry proved.'),
      raw_pipeline=dict(cases=['raw-object','raw-object-high-bits'],functions=['51DB30','50F7E0','4F1600','4F1660','4F1780','4B7A90'],
        equality='All393 bytes equal to the direct runtime-reward fixture, including high raw valueFEDCBA11 versus17'),
      gold_reward=dict(function='4BA420',storage_field=0x14a,low_branch='No Treasury rooms: positive ordinary-coordinate reward creates Object ID1 via global6E5054 and writes requested gold',
        examples=[1,1000,5000],scale='For gold Object IDs1/2: 2048+floor(amount/2) below5000; 4548 at/above5000; native double−0.5 and __ftol634F30',
        balance_branch='Positive request with either cell coordinate0 directly adds to ownerDWORD7E; no Object allocation in the tested y0 case',
        zero_negative='Amount0 creates nothing. AmountFFFFFFFF follows withdrawal path and yields nothing in the empty-Treasury fixture.',
        allocation_failure='Tag gate0 yields no gold object and no direct balance addition; history already cleared',
        unverified='Depositing/withdrawing against real Treasury contents, notifications, live pickup/graphics and gameplay ticks'),
      external_providers=dict(gfx='Null provider, five arguments, RET20',sound='Fixed external provider, four arguments, RET16',
        memory_probe=dict(dll='KERNEL32.dll',name='IsBadReadPtr',iat='66C1B8',bound_target='28CFA4',
          policy='Size0 readable; reject wraparound; otherwise require complete probe interval inside merged readable PE or prepared fixture ranges. No native RTTI replacement.',
          successful_below_floor_cases='Exactly one accepted probe per native replay; invalid-probe counter remains0')),
      source_recipe_file=dict(file=source['file'],sha256=source['sha256'],records=44,prior_exact_patch_checks=1188,new_raw_patch_checks=0),
      counts=dict(factory_cases=len(fo),temple_cases=len(to),distinct_cases=len(fo)+len(to),native_replays=len(fr)+len(tr),
        successful_object_images=sum(r['expected']['success'] for r in fo+to),complete_object_image_bytes=393,whole_native_functions=44,file_backed_blocks=len(p['blocks']),
        original_game_function_or_vtable_adapters=0),
      limitations=['Prepared runtime Object definitions; full raw Objects catalogue→definition→creation composition not executed here.',
        'Complete393-byte captures combine two identical-input deterministic replays; not an atomic live-process snapshot. Unnamed bytes are retained opaque.',
        'Exact DX SHA profile only; editor/old game implementations not compared in this phase.',
        'Creature reward factory/level application remains unverified; native creation path was inspected statically only.',
        'Treasury contents/deposit/withdrawal, generic visual room effects, live speech and later gameplay ticks remain open.',
        'Earlier failed fixture diagnostics and disabled inline-script attempt are preserved and excluded from passing replay counts.',
        'Objects34C and the other substantial unknown fields remain open; whole goal is not achieved.'])
    (ROOT/'object-reward-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    before['phase89_object_rewards']=s;(ROOT/'sacrifice-semantics.json').write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(s['counts']))
if __name__=='__main__':main()
