"""Add indexed-address/LEA and limited-context diagnostics to prior bounded search.

Same root enumeration/constant-alias engine;new outputs never overwrite phase73.
An indexed event is a lead,not a solved index range or a proven definition type.
"""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent
def main():
    manifests=[]
    for role,name in [('creature','scan_current_creature_arguments.py'),('object','scan_object_priority_arguments.py')]:
        source=(ROOT/name).read_text();sha=hashlib.sha256(source.encode()).hexdigest()
        source=source.replace('reads=[];calls=[];states=0','reads=[];indexed=[];calls=[];states=0')
        anchor="   if x.mnemonic.startswith('ret'):continue"
        new="""   for o in op:
    if o.type!=X86_OP_MEM or not o.mem.index:continue
    br=x.reg_name(o.mem.base);ir=x.reg_name(o.mem.index)
    access='address' if x.mnemonic=='lea' else ('read' if reads_memory(x,o) else ('call/jump' if x.mnemonic in ['call','jmp'] else 'other'))
    if br in aliases:
     indexed.append(dict(address=hex(pc),constant=aliases[br]+o.mem.disp,index=ir,scale=o.mem.scale,bytes=o.size,access=access,instruction=x.mnemonic+' '+x.op_str,trace=trace[-12:]))
    elif ir in aliases and o.mem.scale==1:
     indexed.append(dict(address=hex(pc),constant=aliases[ir]+o.mem.disp,index=br,scale=1,bytes=o.size,access=access,instruction=x.mnemonic+' '+x.op_str,trace=trace[-12:],pointer_in_index=True))
   if x.mnemonic.startswith('ret'):continue"""
        assert source.count(anchor)==1;source=source.replace(anchor,new)
        source=source.replace('return dict(reads=reads,calls=calls,states=states,limited=bool(todo))','return dict(reads=reads,indexed=indexed,calls=calls,states=states,limited=bool(todo))')
        source=source.replace('summaries={};hits=[];statecount=limited=0;roots=[]','summaries={};hits=[];indexed_hits=[];statecount=limited=0;roots=[]')
        anchor="  reads=[dict(r,callee=None,call=None) for r in result['reads']]"
        assert anchor in source;source=source.replace(anchor,anchor+"\n  events=[dict(r,callee=None,call=None) for r in result['indexed']]")
        anchor="   summary=summaries[key]"
        assert anchor in source;source=source.replace(anchor,anchor+"\n   for r in summary['indexed']:events.append(dict(r,callee=hex(target),call=call['address'],argument_chain=chain))")
        anchor="  seen=set()\n  for r in reads:"
        assert anchor in source;source=source.replace(anchor,"  unique_events=set()\n  for ev in events:\n   evkey=(ev['address'],ev['constant'],ev['index'],ev['scale'],ev['callee'],ev['call'],ev['access'])\n   if evkey in unique_events:continue\n   unique_events.add(evkey);indexed_hits.append(dict(root=root,event=ev))\n"+anchor)
        anchor=" out=dict(scope=__doc__,"
        assert anchor in source
        source=source.replace(anchor," limited_contexts=[dict(target=hex(k[0]),registers=dict(k[1]),arguments=dict(k[2]),summary=v) for k,v in summaries.items() if v['limited']]\n"+anchor)
        source=source.replace('candidates=hits)','candidates=hits,indexed_events=indexed_hits,limited_contexts=limited_contexts)')
        output=f'phase85-{role}-index-diagnostics.json';source=source.replace('phase73-DX-creature-argument-leads.json',output).replace('phase73-object-priority-pointer-leads.json',output)
        source=source.replace(";print('roots'", ";print('newindexed',len(indexed_hits),'limitedcontexts',[(c['target'],c['summary']['states']) for c in limited_contexts]);print('roots'")
        dest=f'scan_priority_{role}_indices.py';(ROOT/dest).write_text(source,encoding='utf8');manifests.append(dict(base=name,base_sha256=sha,derived=dest,derived_sha256=hashlib.sha256(source.encode()).hexdigest(),output=output))
    (ROOT/'phase85-index-diagnostic-generation.json').write_text(json.dumps(dict(scope=__doc__,scripts=manifests),indent=2),encoding='utf8')
if __name__=='__main__':main()
