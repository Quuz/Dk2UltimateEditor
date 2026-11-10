"""Audit native object/gold evidence, protected program bytes and integration."""
from pathlib import Path
import json,struct
from build_object_rewards import ROOT,load,digest,canonical,primary,native_results
from audit_targeted_spell import executable_ranges
import emulate_object_factory as factory
import emulate_sacrifice_objects as temple
import fixture_read_probe as probe

def main():
    s=load('object-reward-semantics.json');assert primary()==s['primary']
    fr,fo,fi=native_results(factory,'phase89-object-factory-emulations.json',False);tr,to,ti=native_results(temple,'phase89-sacrifice-object-emulations.json',True)
    assert fr==s['native_factory_replays'] and tr==s['native_reward_replays'] and fo==s['factory_outcomes'] and to==s['reward_outcomes']
    code=Path(s['profile']['executable']).read_bytes();assert digest(code)==s['profile']['sha256']
    protected=executable_ranges(code)+[(r['address'],r['length']) for r in s['primary']['blocks']]
    imports=probes=raw_chains=0
    for module,rows in [(factory,fr),(temple,tr)]:
        by={c['name']:c for c in module.cases()}
        for row in rows:
            c=by[row['case']];args=module.build(c,row['page'])[0];saved=load('ghidra/'+row['file']);r=saved['result']
            mem=[(int(v['address'],16),bytes.fromhex(v['hex'])) for v in json.loads(args['memory'])['regions']]
            assert all(a+len(b)<=p or a>=p+n for a,b in mem for p,n in protected)
            spans=sorted((a,len(b)) for a,b in mem);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
            if any(a==probe.ADDRESS for a,b in mem):
                assert 'EBX' in args['return_registers'].split(',');probes+=int(r['registers']['EBX'],16);imports+=1
            if module is temple and c.get('raw_pipeline'):assert args['address']=='0x51db30';raw_chains+=1
    assert raw_chains==4 and probes>0
    assert all(ti[n]==ti['object'] for n in ['object-high-bits','raw-object','raw-object-high-bits'])
    assert int.from_bytes(ti['object-gold-bag'][0x14a:0x14e],'little')==0
    source=s['source_recipe_file'];assert digest(Path(source['file']).read_bytes())==source['sha256'] and source['new_raw_patch_checks']==0
    assert load('phase88-validation.json')['exact_container_patch_checks']==source['prior_exact_patch_checks']==1188
    export=load('temple-recipes-current.json')
    for row in export['catalogues']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    for path,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/path).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==68 and package['sacrifice_semantics']['phase89_object_rewards']==s
    prior=load('phase89-prior-components.json');assert len(prior)==68
    base=load('sacrifice-semantics.json');assert base.pop('phase89_object_rewards')==s and canonical(base)==s['phase88_base_canonical_sha256']
    text=json.dumps(base,ensure_ascii=False,indent=2)
    assert prior['sacrifice_semantics']['sha256'] in [digest(text.encode()),digest(text.replace('\n','\r\n').encode())]
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert digest(data)==row['sha256'] and json.loads(data)==package[key]
        if key!='sacrifice_semantics':assert row==prior[key]
    for row in s['api_helpers']:assert digest((ROOT/row['file']).read_bytes())==row['sha256']
    assert (ROOT/'OBJECT_REWARDS_RU.md').exists() and 'Актуальный этап 89:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=89,date='2026-10-28',**s['counts'],raw_to_object_chains=raw_chains,external_memory_probe_provider_replays=imports,
      accepted_native_memory_probes=probes,original_game_function_or_vtable_patches=0,source_executables_unchanged=4,
      original_files_written=0,package_components=68,previous_67_other_components_preserved=True,phase88_base_preserved_byte_exact=True,
      new_raw_patch_checks=0,prior1188_checks_reused=True,live_game_runs=0,goal_achieved=False,significant_unknowns_remain=True,
      limitations=s['limitations'])
    (ROOT/'phase89-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:x for k,x in v.items() if k!='limitations'}))
if __name__=='__main__':main()
