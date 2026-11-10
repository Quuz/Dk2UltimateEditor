"""Phase191: extend the frozen phase178 search by EDX argument forwarding.

This is a bounded static hint search, not an ABI or creature-field proof.
Only CALL argument collection changes; diagnostic history is capped at64.
Original bytes, root selection, cleanup rules and six-level bounds are kept.
"""
from pathlib import Path
import inspect
import json
import hashlib
import scan_deep_crypt_pointers as old

ROOT = Path(__file__).resolve().parent
EXPECTED = 'd7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
sha = lambda b: hashlib.sha256(b).hexdigest()


def make_walker(ins, edx=True):
    source = inspect.getsource(old.make_walker)
    needle = "registers={k:v for k,v in aliases.items() if k=='ecx'}"
    assert source.count(needle) == 1
    if edx:
        source = source.replace(needle, "registers={k:v for k,v in aliases.items() if k in ['ecx','edx']}")
    trace = "trace+[f'{pc:x}: {x.mnemonic} {x.op_str}']"
    assert source.count(trace) == 1
    source = source.replace(trace, '('+trace+')[-64:]')
    namespace = dict(vars(old))
    exec(compile(source, str(ROOT/Path(__file__).name)+':frozen-transfer', 'exec'), namespace)
    return namespace['make_walker'](ins)


def identity(hit):
    r = hit['read']
    return (hit['root']['address'], hit['field'], r['address'], r['offset'],
            r['bytes'], r['callee'], r['call'])


def indexed_identity(hit):
    e = hit['event']
    return (hit['root']['address'], e['address'], e['constant'], e['index'],
            e['scale'], e['callee'], e['call'], e['access'])


def main():
    prior = json.loads((ROOT/'phase178-deep-owner-crypt-leads.json').read_text())
    assert prior['program_sha256'] == EXPECTED
    source = inspect.getsource(old.main)
    output = 'phase178-deep-owner-crypt-leads.json'
    assert source.count(output) == 1
    source = source.replace(output, 'phase191-edx-owner-crypt-leads.json')
    namespace = dict(vars(old), make_walker=make_walker)
    exec(compile(source, str(ROOT/Path(__file__).name)+':frozen-search', 'exec'), namespace)
    namespace['main']()
    result = json.loads((ROOT/'phase191-edx-owner-crypt-leads.json').read_text())
    assert result['program_sha256'] == EXPECTED
    assert len(prior['producer_roots']) == len(result['producer_roots']) == 539
    old_hits = {identity(h) for h in prior['candidates']}
    new_hits = {identity(h) for h in result['candidates']}
    old_indexed = {indexed_identity(h) for h in prior['indexed_events']}
    new_indexed = {indexed_identity(h) for h in result['indexed_events']}
    added = [h for h in result['candidates'] if identity(h) not in old_hits]
    delta = dict(phase=191, kind='bounded_static_EDX_forwarding_search',
        previous=dict(file=output, sha256=sha((ROOT/output).read_bytes())),
        current=dict(file='phase191-edx-owner-crypt-leads.json', sha256=sha((ROOT/'phase191-edx-owner-crypt-leads.json').read_bytes())),
        helper=dict(file=Path(__file__).name, sha256=sha(Path(__file__).read_bytes())),
        frozen_helper=dict(file='scan_deep_crypt_pointers.py', sha256=sha((ROOT/'scan_deep_crypt_pointers.py').read_bytes())),
        root_count=539, previous_candidates=len(old_hits), current_candidates=len(new_hits),
        old_candidates_retained=len(old_hits & new_hits), lost_candidates=sorted(old_hits-new_hits),
        new_candidates=added, new_indexed_events=[h for h in result['indexed_events'] if indexed_identity(h) not in old_indexed],
        lost_indexed_events=sorted(old_indexed-new_indexed),
        summary_counts={k:result[k] for k in ['root_states','limited_roots','callee_summaries','limited_callees']},
        limits=dict(states_per_context=5000, direct_call_levels=6, contexts_per_root=512, callee_span=16384),
        limitations=['EDX is an additional possible argument, not a proved calling convention.',
                    'entity+2A seeds are type hints; only room owner lookup has an exact source.',
                    'Indirect calls, pointer returns, memory-stored aliases and escaped stack writes remain outside the model.',
                    'Limits and unknown types forbid inferring global nonuse from missing candidates.'])
    (ROOT/'phase191-edx-search-delta.json').write_text(json.dumps(delta, ensure_ascii=False, indent=2)+'\n', encoding='utf8')
    print('EDX DELTA', json.dumps({k:delta[k] for k in ['root_count','previous_candidates','current_candidates','old_candidates_retained','summary_counts']}, ensure_ascii=False), flush=True)
    print('added',len(added),'indexed',len(delta['new_indexed_events']),'lost',len(delta['lost_candidates']),flush=True)


if __name__ == '__main__':
    main()
