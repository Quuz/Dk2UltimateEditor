"""Read-only Effects graph checks and bounded creation-loop projection.

The budget and duplicate/missing/cycle stops belong to this diagnostic tool.
They must not be presented as behavior implemented by the native game.
"""
from collections import Counter
import dk2_formats as fmt

def u16(value):
    if type(value) is not int or not 0<=value<=65535:raise ValueError('Expected unsigned16 integer')
    return value

def effect_record(record):
    if not isinstance(record,(bytes,bytearray)) or len(record)!=246:raise ValueError('Expected246-byte Effects record')
    return dict(id=fmt.u16(record,0xB8),next_id=fmt.u16(record,0xC0),name=fmt.string(record[:32]))

def catalogue(records):
    rows=[effect_record(r) for r in records];counts=Counter(r['id'] for r in rows)
    duplicates=sorted(k for k,v in counts.items() if v>1)
    # Ambiguous keys are deliberately excluded, even if their links agree.
    return rows,{r['id']:r for r in rows if counts[r['id']]==1},duplicates

def project_chain(records,start_id,*,step_budget,creation_enabled=True,allocation_successes=None):
    u16(start_id)
    if type(step_budget) is not int or step_budget<1:raise ValueError('Positive diagnostic step budget required')
    if type(creation_enabled) is not bool:raise ValueError('Explicit boolean creation state required')
    if allocation_successes is not None and (type(allocation_successes) is not int or allocation_successes<0):
        raise ValueError('Allocation scenario must be a nonnegative integer or None')
    rows,index,duplicates=catalogue(records);current=start_id;created=[];visited=set()
    while True:
        if current==0:stop='zero_id';break
        if not creation_enabled:stop='creation_disabled';break
        if allocation_successes is not None and len(created)>=allocation_successes:stop='allocation_failed_scenario';break
        if any(r['id']==0 for r in rows):stop='zero_definition_tool_stop';break
        if current in duplicates:stop='duplicate_id_tool_stop';break
        if current not in index:stop='missing_definition_tool_stop';break
        if current in visited:stop='cycle_tool_stop';break
        if len(created)>=step_budget:stop='step_budget_tool_stop';break
        visited.add(current);created.append(current);current=index[current]['next_id']
    return dict(created_ids=created,stop_reason=stop,pending_id=current,
        native_return_if_modeled_exit=(int(bool(created)) if stop in ['zero_id','creation_disabled','allocation_failed_scenario'] else None),
        output_first_id=created[0] if created else None,output_pointer_written=bool(created),
        global_head_last_id=created[-1] if created else None,new_global_list_ids=list(reversed(created)),
        diagnostic_only=True,step_budget=step_budget)

def graph_audit(records):
    rows,index,duplicates=catalogue(records);all_ids={r['id'] for r in rows}
    missing=[dict(source_id=r['id'],target_id=r['next_id']) for r in rows if r['next_id'] and r['next_id'] not in all_ids]
    # Walk a functional graph once per node, keeping canonical directed cycles.
    done=set();cycles=[]
    for start in index:
        if start==0 or start in done:continue
        path=[];positions={};current=start
        while current and current in index and current not in done and current not in positions:
            positions[current]=len(path);path.append(current);current=index[current]['next_id']
        if current in positions:
            cycle=path[positions[current]:];pivot=cycle.index(min(cycle));cycles.append(cycle[pivot:]+cycle[:pivot])
        done.update(path)
    longest=[]
    if not duplicates and not missing and not cycles:
        lengths={0:0};nexts={r['id']:r['next_id'] for r in rows if r['id']}
        for start in nexts:
            path=[];current=start
            while current not in lengths:path.append(current);current=nexts[current]
            for node in reversed(path):lengths[node]=1+lengths[nexts[node]]
        if nexts:
            current=max(nexts,key=lambda key:lengths[key])
            while current:longest.append(current);current=nexts[current]
    return dict(records=len(rows),nonzero_links=sum(bool(r['next_id']) for r in rows),zero_id_records=sum(r['id']==0 for r in rows),
        duplicate_ids=duplicates,missing_links=missing,cycles=cycles,longest_chain_ids=longest)
