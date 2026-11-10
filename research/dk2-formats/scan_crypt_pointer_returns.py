"""Record Owner-pointer return hints missed by the forward-only search.

Return offsets are conditional static hints, not verified function summaries.
The frozen search's transfer functions and bounded CFG are otherwise kept.
"""
from pathlib import Path
import inspect
import json
import scan_deep_crypt_pointers as old
from scan_edx_crypt_pointers import ROOT, EXPECTED, sha


def make_walker(ins):
    source = inspect.getsource(old.make_walker)
    changes = {
        "registers={k:v for k,v in aliases.items() if k=='ecx'}":
            "registers={k:v for k,v in aliases.items() if k in ['ecx','edx']}",
        "trace+[f'{pc:x}: {x.mnemonic} {x.op_str}']":
            "(trace+[f'{pc:x}: {x.mnemonic} {x.op_str}'])[-64:]",
        'reads=[];indexed=[];calls=[];states=0':
            'reads=[];indexed=[];calls=[];returns=[];states=0',
        "   if x.mnemonic.startswith('ret'):continue":
            "   if x.mnemonic.startswith('ret'):\n    if 'eax' in aliases:returns.append(dict(address=hex(pc),offset=aliases['eax'],instruction=x.mnemonic+' '+x.op_str,trace=trace[-12:]))\n    continue",
        'calls=calls,states=states,limited=bool(todo)':
            'calls=calls,states=states,limited=bool(todo),returns=returns',
    }
    for needle, replacement in changes.items():
        assert source.count(needle) == 1, needle
        source = source.replace(needle, replacement)
    namespace = dict(vars(old))
    exec(compile(source, str(ROOT/Path(__file__).name)+':frozen-transfer', 'exec'), namespace)
    return namespace['make_walker'](ins)


def main():
    reports=[]
    def recording_factory(ins):
        walker, stats = make_walker(ins)
        def record(entry,initial,limit=5000,span=None,initial_stack=None):
            result=walker(entry,initial,limit=limit,span=span,initial_stack=initial_stack)
            returns=result.pop('returns')
            if returns:
                unique={json.dumps({k:v for k,v in e.items() if k!='trace'},sort_keys=True):e for e in returns}
                reports.append(dict(entry=hex(entry),registers=initial,stack=initial_stack or {},span=span,
                                    states=result['states'],limited=result['limited'],returns=list(unique.values())))
            return result
        return record,stats
    source=inspect.getsource(old.main)
    source=source.replace('phase178-deep-owner-crypt-leads.json','phase191-return-search-reproduction.json')
    namespace=dict(vars(old),make_walker=recording_factory)
    exec(compile(source,str(ROOT/Path(__file__).name)+':frozen-search','exec'),namespace)
    namespace['main']()
    current=json.loads((ROOT/'phase191-return-search-reproduction.json').read_text())
    previous=json.loads((ROOT/'phase191-edx-owner-crypt-leads.json').read_text())
    assert current==previous
    # Exact duplicate used only for a reproduction check; do not accumulate it.
    temporary=ROOT/'phase191-return-search-reproduction.json'
    assert temporary.resolve().parent==ROOT
    temporary.unlink()
    seeds={r['next']:r for r in current['producer_roots']}
    for report in reports:
        if report['entry'] in seeds:report['producer']=seeds[report['entry']]
    result=dict(phase=191,kind='bounded_static_pointer_return_hints',program_sha256=EXPECTED,
                EDX_search_exactly_reproduced=True,return_contexts=len(reports),contexts=reports,
                helper=dict(file=Path(__file__).name,sha256=sha(Path(__file__).read_bytes())),
                scope='A known pointer alias reaches EAX at a syntactic RET. Root may start midfunction; type, caller, feasibility and complete function contracts require separate primary proof.')
    (ROOT/'phase191-pointer-return-leads.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    print('RETURN HINTS',len(reports),flush=True)
    for row in reports:print(row['entry'],'root',row.get('producer',{}).get('address'),'returns',[(r['address'],r['offset']) for r in row['returns']],flush=True)


if __name__=='__main__':main()
