"""New indexed/return-alias leads over prior Terrain roots;do not alter old passes.

Return propagation is an overapproximate candidate source,not an ABI/type proof.
Bounded callers/callees,unknown return paths and missing stack pointees remain.
"""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def main():
    scripts=[]
    for kind in ['arguments','cache']:
        name='scan_dx_terrain_'+kind+'.py';original=(ROOT/name).read_text();text=original
        text=repr(__doc__)+'\n'+text[text.index('from pathlib import Path'):]
        text=text.replace('reads=[];calls=[];states=0','reads=[];calls=[];indexed=[];returns=[];states=0')
        anchor="   if x.mnemonic.startswith('ret'):continue"
        replace="""   for o in op:
    if o.type!=X86_OP_MEM or not o.mem.index:continue
    br=x.reg_name(o.mem.base);ir=x.reg_name(o.mem.index)
    access='address' if x.mnemonic=='lea' else ('read' if reads_memory(x,o) else 'other')
    if br in aliases:indexed.append(dict(address=hex(pc),constant=aliases[br]+o.mem.disp,index=ir,scale=o.mem.scale,bytes=o.size,access=access,instruction=x.mnemonic+' '+x.op_str,trace=trace[-12:]))
    elif ir in aliases and o.mem.scale==1:indexed.append(dict(address=hex(pc),constant=aliases[ir]+o.mem.disp,index=br,scale=1,bytes=o.size,access=access,instruction=x.mnemonic+' '+x.op_str,trace=trace[-12:],pointer_in_index=True))
   if x.mnemonic.startswith('ret'):
    returns.append(dict(address=hex(pc),eax_offset=aliases.get('eax'),instruction=x.mnemonic+' '+x.op_str,trace=trace[-12:]))
    continue"""
        assert text.count(anchor)==1;text=text.replace(anchor,replace)
        anchor='return dict(reads=reads,calls=calls,states=states,limited=bool(todo))';assert text.count(anchor)==1
        text=text.replace(anchor,'return dict(reads=reads,calls=calls,indexed=indexed,returns=returns,states=states,limited=bool(todo))')
        text=text.replace('summaries={};hits=[];statecount=limited=0;roots=[]','summaries={};hits=[];indexed_hits=[];return_hits=[];generated=set();statecount=limited=0;roots=[];original_seed_count=len(seeds)')
        anchor="result=walk(seed['next'],{seed['register']:0})";assert text.count(anchor)==1;text=text.replace(anchor,"result=walk(seed['next'],{seed['register']:seed.get('initial_offset',0)})")
        anchor="  reads=[dict(r,callee=None,call=None) for r in result['reads']]";assert text.count(anchor)==1;text=text.replace(anchor,anchor+"\n  events=[dict(r,callee=None,call=None) for r in result['indexed']]")
        anchor='   summary=summaries[key]';assert text.count(anchor)==1
        text=text.replace(anchor,anchor+"""
   for r in summary['indexed']:events.append(dict(r,callee=hex(target),call=call['address'],argument_chain=chain))
   for r in summary['returns']:
    if r['eax_offset'] is None:continue
    return_hits.append(dict(root=hex(seed['address']),call=call['address'],target=hex(target),returned=r,limited=summary['limited'],argument_chain=chain))
    if seed.get('return_depth',0)>=2 or len(seeds)>=4096:continue
    pc=int(call['address'],16);caller=ins(pc)
    if caller is None or caller.mnemonic!='call' or caller.operands[0].type!=X86_OP_IMM:continue
    rk=(pc,r['eax_offset'],r['address'],seed['family'])
    if rk in generated:continue
    generated.add(rk);seeds.append(dict(address=pc,next=pc+caller.size,family=seed['family'],register='eax',producer='returned-alias-lead',instruction=caller.mnemonic+' '+caller.op_str,initial_offset=r['eax_offset'],return_depth=seed.get('return_depth',0)+1,source_root=hex(seed['address']),returned_at=r['address'],callee_limited=summary['limited']))""")
        anchor='  seen=set()\n  for r in reads:';assert text.count(anchor)==1;text=text.replace(anchor,"  eventkeys=set()\n  for r in events:\n   ek=(r['address'],r['constant'],r['index'],r['scale'],r['callee'],r['call'])\n   if ek not in eventkeys:indexed_hits.append(dict(root=root,event=r));eventkeys.add(ek)\n"+anchor)
        anchor=' out=dict(scope=__doc__,';assert text.count(anchor)==1;text=text.replace(anchor," limited_contexts=[dict(target=hex(k[0]),registers=dict(k[1]),arguments=dict(k[2]),summary=v) for k,v in summaries.items() if v['limited']]\n"+anchor)
        text=text.replace('candidates=hits)','candidates=hits,indexed_events=indexed_hits,return_alias_events=return_hits,limited_contexts=limited_contexts,original_seed_count=original_seed_count,generated_return_seeds=len(seeds)-original_seed_count)')
        oldfile='phase74-DX-terrain-'+('argument' if kind=='arguments' else kind)+'-leads.json';output='phase88-terrain-'+kind+'-return-leads.json';assert oldfile in text;text=text.replace(oldfile,output)
        filename='scan_terrain_'+kind+'_returns.py';compile(text,filename,'exec');(ROOT/filename).write_text(text,encoding='utf8')
        scripts.append(dict(base=name,base_sha256=hashlib.sha256(original.encode()).hexdigest(),derived=filename,derived_sha256=hashlib.sha256(text.encode()).hexdigest(),output=output))
    (ROOT/'phase88-search-generation.json').write_text(json.dumps(dict(scope=__doc__,scripts=scripts),indent=2),encoding='utf8');print('Generated2newTerrain-return/index diagnostics;prioroutputsretained.')
if __name__=='__main__':main()
