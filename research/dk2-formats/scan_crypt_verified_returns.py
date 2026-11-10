"""Phase191: follow two original, byte-attested pointer-return contracts.

4C5F50 returns ECX+B43. 4DC7E0 returns ECX+EA1 after46 iterations.
These are arithmetic contracts, not a proof of the receiver's Owner type.
Other observed EAX aliases at RET are deliberately not return summaries.
"""
from pathlib import Path
import inspect
import json
import scan_deep_crypt_pointers as old
from scan_edx_crypt_pointers import ROOT, EXPECTED, sha, identity, indexed_identity
from build_effect_semantics import pe_read

CONTRACTS={0x4C5F50:0xB43,0x4DC7E0:0xEA1}
HEX={0x4C5F50:'8d81430b0000c3',
     0x4DC7E0:'8a5424048d81650b00008891470b0000b92e0000003850f27506c70000a0000083c0124975efc20400'}


def make_walker(ins, contracts=None):
    contracts=CONTRACTS if contracts is None else contracts
    source=inspect.getsource(old.make_walker)
    changes={
        "registers={k:v for k,v in aliases.items() if k=='ecx'}":
            "registers={k:v for k,v in aliases.items() if k in ['ecx','edx']}",
        "trace+[f'{pc:x}: {x.mnemonic} {x.op_str}']":
            "(trace+[f'{pc:x}: {x.mnemonic} {x.op_str}'])[-64:]",
        "    for r in ['eax','ecx','edx']:aliases.pop(r,None);frames.pop(r,None)":
            "    returned=aliases.get('ecx') if op[0].type==X86_OP_IMM and op[0].imm in RETURN_CONTRACTS else None\n    if returned is not None:returned+=RETURN_CONTRACTS[op[0].imm]\n    for r in ['eax','ecx','edx']:aliases.pop(r,None);frames.pop(r,None)\n    if returned is not None:aliases['eax']=returned",
    }
    for needle,replacement in changes.items():
        assert source.count(needle)==1,needle
        source=source.replace(needle,replacement)
    namespace=dict(vars(old),RETURN_CONTRACTS=contracts)
    exec(compile(source,str(ROOT/Path(__file__).name)+':frozen-transfer','exec'),namespace)
    return namespace['make_walker'](ins)


def main():
    data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    assert sha(data)==EXPECTED
    for address,hexbytes in HEX.items():
        assert pe_read(data,address,len(bytes.fromhex(hexbytes)))==bytes.fromhex(hexbytes),hex(address)
    source=inspect.getsource(old.main).replace('phase178-deep-owner-crypt-leads.json','phase191-verified-return-owner-leads.json')
    namespace=dict(vars(old),make_walker=make_walker)
    exec(compile(source,str(ROOT/Path(__file__).name)+':frozen-search','exec'),namespace)
    namespace['main']()
    current=json.loads((ROOT/'phase191-verified-return-owner-leads.json').read_text())
    previous=json.loads((ROOT/'phase191-edx-owner-crypt-leads.json').read_text())
    oh={identity(h) for h in previous['candidates']};nh={identity(h) for h in current['candidates']}
    oi={indexed_identity(h) for h in previous['indexed_events']};ni={indexed_identity(h) for h in current['indexed_events']}
    result=dict(phase=191,status='bounded_static_search_with_verified_arithmetic_returns',
        contracts=[dict(entry=hex(a),receiver='ECX',returned_offset=offset,original_hex=HEX[a]) for a,offset in CONTRACTS.items()],
        current=dict(file='phase191-verified-return-owner-leads.json',sha256=sha((ROOT/'phase191-verified-return-owner-leads.json').read_bytes())),
        prior=dict(file='phase191-edx-owner-crypt-leads.json',sha256=sha((ROOT/'phase191-edx-owner-crypt-leads.json').read_bytes())),
        prior_candidates=len(oh),current_candidates=len(nh),retained_candidates=len(oh&nh),lost_candidates=sorted(oh-nh),
        new_candidates=[h for h in current['candidates'] if identity(h) not in oh],
        new_indexed_events=[h for h in current['indexed_events'] if indexed_identity(h) not in oi],
        lost_indexed_events=sorted(oi-ni),
        counts={k:current[k] for k in ['root_states','limited_roots','callee_summaries','limited_callees']},
        limitations=['Only two exact arithmetic contracts; no general return-value analysis.',
                     'A return pointer into a receiver does not establish its runtime type.',
                     'Conditional/void-return EAX survivors are not accepted contracts.',
                     'Original static search limits and indirect/pointee gaps remain.'])
    (ROOT/'phase191-verified-return-delta.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    print('VERIFIED RETURN DELTA',len(result['new_candidates']),len(result['new_indexed_events']),result['counts'],flush=True)


if __name__=='__main__':main()
