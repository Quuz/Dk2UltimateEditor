"""Positive Owner-return witnesses from frozen phase178 hint roots.

Not complete function summaries: forward search can stop after losing all
aliases, so missing return paths must never certify a universal return type.
"""
from pathlib import Path
from functools import lru_cache
import inspect,json,hashlib
import scan_deep_crypt_pointers as old
ROOT=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()

def walker(ins):
    code=inspect.getsource(old.make_walker)
    changes=[('calls=[];states=0','calls=[];returns=[];states=0'),
        ("if x.mnemonic.startswith('ret'):continue", "if x.mnemonic.startswith('ret'):\n    returns.append(dict(address=hex(pc),owner_offset=aliases.get('eax'),eax_constant=constants.get('eax'),trace=trace[-12:]))\n    continue"),
        ("trace+[f'{pc:x}: {x.mnemonic} {x.op_str}']","(trace+[f'{pc:x}: {x.mnemonic} {x.op_str}'])[-64:]"),
        ('calls=calls,states=states,limited=bool(todo)','calls=calls,states=states,limited=bool(todo),returned=returns')]
    for before,after in changes:
        assert code.count(before)==1,before;code=code.replace(before,after)
    namespace=dict(vars(old));exec(compile(code,str(ROOT/'scan_crypt_owner_returns.py')+':frozen-transfer','exec'),namespace)
    return namespace['make_walker'](ins)

def main():
    previous=json.loads((ROOT/'phase178-deep-owner-crypt-leads.json').read_text());data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    assert sha(data)==previous['program_sha256'];base,code=old.code_region(data)
    md=old.Cs(old.CS_ARCH_X86,old.CS_MODE_32);md.detail=True
    @lru_cache(maxsize=60000)
    def ins(a):
        if not base<=a<base+len(code):return None
        x=next(md.disasm(code[a-base:a-base+15],a,count=1),None)
        return x if x and x.id else None
    walk,stats=walker(ins);out=[];states=limited=0;queries=[]
    for seed in previous['producer_roots']:
        result=walk(int(seed['next'],16),{seed['register']:0},limit=500)
        states+=result['states'];limited+=int(result['limited'])
        positive=[r for r in result['returned'] if r['owner_offset'] is not None]
        if positive:
            out.append(dict(seed=seed,states=result['states'],limited=result['limited'],possible_returns=positive))
            queries.append(dict(id='phase189-return-owner-'+seed['address'][2:],tool='get_function_by_address',
                args=dict(program='DKII-DX.exe',address=seed['address']),print_limit=0))
    report=dict(phase=189,status='positive_untyped_return_witnesses_only',program_sha256=sha(data),roots=len(previous['producer_roots']),
        states=states,limited_roots=limited,root_limit=500,positive_roots=out,call_stack_statistics=dict(stats),
        helpers=[dict(file=n,sha256=sha((ROOT/n).read_bytes())) for n in ['scan_crypt_owner_returns.py','scan_deep_crypt_pointers.py','constant_branch_domain.py']],
        scope='FrozenpointertransferswithboundedtraceandRETobservations. ExactRoomgetterseedshaveOwnerprovenance; entity+2Aseedsarehints. Noall-pathreturntyping/consumersemanticsproved.')
    (ROOT/'phase189-owner-return-witnesses.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    (ROOT/'phase189-owner-return-owner-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf-8')
    print('roots',report['roots'],'states',states,'limited',limited,'positive',len(out))
    print([(r['seed']['address'],r['seed']['producer'],[(x['address'],x['owner_offset']) for x in r['possible_returns']]) for r in out])

if __name__=='__main__':main()
