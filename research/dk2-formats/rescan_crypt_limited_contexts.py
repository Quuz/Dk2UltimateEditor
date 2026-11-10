"""Revisit only phase178 work-limit hits; never interpret absence as nonuse.

The frozen search's transfer rules stay identical. Diagnostic path history is
capped at64 (records already retain only12); it is absent from state identity.
Every prior callee result is reproduced byte-for-byte canonically at5000 before
raising its limit. Pointer types are hints, not proofs. This is static search.
"""
from pathlib import Path
from functools import lru_cache
import gzip
import hashlib
import inspect
import json
import time
import scan_deep_crypt_pointers as old

ROOT=Path(__file__).resolve().parent
EXPECTED='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
UNKNOWN={0,1,8,9,12,13,14,15,16,17}
sha=lambda b:hashlib.sha256(b).hexdigest()
canonical=lambda x:json.dumps(x,sort_keys=True,separators=(',',':')).encode()
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def make_bounded_walker(ins):
    source=inspect.getsource(old.make_walker)
    needle="trace+[f'{pc:x}: {x.mnemonic} {x.op_str}']"
    assert source.count(needle)==1
    source=source.replace(needle,'('+needle+')[-64:]')
    namespace=dict(vars(old))
    exec(compile(source,str(ROOT/'rescan_crypt_limited_contexts.py')+':frozen-transfer','exec'),namespace)
    return namespace['make_walker'](ins)

def summary_rows(result):
    # Keep an original witness trace for each distinct event; counts preserve
    # repeated visits. All raw events also remain in a deterministic gzip.
    out={}
    for family in ['reads','indexed','calls']:
        rows={}
        for event in result[family]:
            key=canonical({k:v for k,v in event.items() if k!='trace'})
            if key not in rows:rows[key]=dict(event,observations=0)
            rows[key]['observations']+=1
        out[family]=list(rows.values())
    return out

def targets(rows):
    hits=[]
    for read in rows['reads']:
        for field,width,name in old.TARGETS['Owners']:
            if read['offset']<field+width and read['offset']+read['bytes']>field:
                hits.append(dict(field=name,unknown=int(name[-2:]) in UNKNOWN,read=read))
    return hits

def main():
    previous=json.loads((ROOT/'phase178-deep-owner-crypt-leads.json').read_text())
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    assert sha(data)==previous['program_sha256']==EXPECTED
    base,code=old.code_region(data)
    md=old.Cs(old.CS_ARCH_X86,old.CS_MODE_32);md.detail=True
    @lru_cache(maxsize=60000)
    def ins(a):
        if not base<=a<base+len(code):return None
        x=next(md.disasm(code[a-base:a-base+15],a,count=1),None)
        return x if x and x.id else None
    walk,stats=make_bounded_walker(ins)
    jobs=[]
    for c in previous['limited_contexts']:
        target=int(c['target'],16)
        jobs.append(dict(id=c['target'][2:],entry=target,initial=c['registers'],
            stack={int(k):v for k,v in c['arguments'].items()},span=(target,target+16384),previous=c['summary']))
    for r in previous['producer_roots']:
        if r['limited']:
            jobs.append(dict(id='root-'+r['address'][2:],entry=int(r['next'],16),initial={r['register']:0},stack={},span=None,seed=r))
    reports=[]
    for job in jobs:
        start=time.monotonic()
        low=walk(job['entry'],job['initial'],limit=5000,span=job['span'],initial_stack=job['stack'])
        if 'previous' in job:
            assert canonical(low)==canonical(job['previous']),job['id']
        else:
            original,_=old.make_walker(ins)
            assert canonical(low)==canonical(original(job['entry'],job['initial'],limit=5000)),job['id']
        high=walk(job['entry'],job['initial'],limit=50000,span=job['span'],initial_stack=job['stack'])
        raw='phase186-rescan-'+job['id']+'.json.gz'
        (ROOT/raw).write_bytes(gzip.compress(canonical(high),mtime=0))
        before=summary_rows(low);after=summary_rows(high)
        old_keys={canonical({k:v for k,v in r.items() if k not in ['trace','observations']}) for r in targets(before)}
        hits=targets(after)
        for h in hits:
            # Compare semantic event identities, not changed observation count.
            h['new_beyond_previous_limit']=not any(h['field']==p['field'] and
                {k:v for k,v in h['read'].items() if k not in ['trace','observations']}==
                {k:v for k,v in p['read'].items() if k not in ['trace','observations']} for p in targets(before))
        report=dict(id=job['id'],entry=hex(job['entry']),registers=job['initial'],arguments=job['stack'],
            span=None if job['span'] is None else list(map(hex,job['span'])),baseline_reproduced=True,
            previous_limit=5000,new_limit=50000,previous_states=low['states'],states=high['states'],limited=high['limited'],
            seconds=round(time.monotonic()-start,3),raw=dict(file=raw,sha256=sha((ROOT/raw).read_bytes())),
            events=after,counter_candidates=hits)
        reports.append(report)
        print(job['id'],'states',high['states'],'limited',high['limited'],'reads',len(after['reads']),
            'calls',len(after['calls']),'counter_leads',len(hits),'new',sum(h['new_beyond_previous_limit'] for h in hits),flush=True)
        dump('phase186-crypt-limited-rescan-progress.json',dict(completed=len(reports),total=len(jobs),reports=reports))
    dump('phase186-crypt-limited-rescan.json',dict(phase=186,kind='bounded-static-pointer-search',program_sha256=EXPECTED,
        prior=dict(file='phase178-deep-owner-crypt-leads.json',sha256=sha((ROOT/'phase178-deep-owner-crypt-leads.json').read_bytes())),
        helpers=[dict(file=n,sha256=sha((ROOT/n).read_bytes())) for n in ['rescan_crypt_limited_contexts.py','scan_deep_crypt_pointers.py','constant_branch_domain.py']],
        previous_goal_turn='no-progress: question answered from already-published evidence; this rescan is the next safe action',
        reports=reports,call_stack_statistics=dict(stats),
        limitations=['Static hints only; entity+2A is not necessarily Owner.','Only the12old limited callees and1limited root; no claim of global coverage.',
            'Same pointer/constant/stack transfer and direct-call handling as phase178; indirect calls, memory-derived pointers and unknown ABI remain unresolved.',
            'Finishing a bounded search does not prove function semantics or absence of counter consumers.']))

if __name__=='__main__':main()
