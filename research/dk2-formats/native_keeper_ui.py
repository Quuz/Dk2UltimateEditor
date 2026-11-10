"""Keeper UI ordinal/status oracle;numeric states,not invented availability enums."""
from native_keeper_lookup import lookup_index
def state(ids,values,key):
    i=lookup_index(ids,key);return 1 if i is None else values[i]
def sequence(definitions,state_ids,values):
    rows=[]
    for order in range(1,len(definitions)+1):
        key=next((r[0x75] for r in definitions if r[0x6C]==order),0)
        eligible=state(state_ids,values,key)!=1 and key not in [5,14]
        rows.append(dict(order=order,id=key,eligible=eligible))
    return rows
def select(rows,ordinal):
    count=0
    for row in rows:
        if row['eligible']:count+=1
        if count==ordinal:return row['id']
    return None
