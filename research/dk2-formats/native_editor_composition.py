"""Exact Lean byte edits and read-only projection of traced Room GUI effects."""
from pathlib import Path
import json

ROOT=Path(__file__).resolve().parent
SCHEMA=json.loads((ROOT/'editor-composition.json').read_text(encoding='utf8'))

def terrain_lean(record):
    if len(record)<0x1D6:raise ValueError('Terrain record too short')
    return {axis:{f['position']:record[f['raw_offset']] for f in SCHEMA['terrain_lean']['bindings'] if f['axis']==axis}
        for axis in ('horizontal','vertical')}

def set_terrain_lean(record,axis,values):
    """Change only the three bytes of one axis. All other bytes remain intact."""
    if axis not in ('horizontal','vertical'):raise ValueError('Axis must be horizontal or vertical')
    values=list(values)
    if len(values)!=3 or any(type(v) is not int or not 0<=v<=255 for v in values):
        raise ValueError('Exactly three integers in 0..255 are required')
    terrain_lean(record) # Length validation before any write.
    out=bytearray(record)
    fields=[f for f in SCHEMA['terrain_lean']['bindings'] if f['axis']==axis]
    for field,value in zip(fields,values):out[field['raw_offset']]=value
    return bytes(out)

def room_terrain_projection(room_record,terrain_records):
    """Resolve a supplied explicit scope; no automatic repair or input writes.

    Ambiguous IDs are reported instead of silently choosing a candidate. This
    function projects only known unconditional Room commit assignments, not the
    entire native UI save or game acceptance.
    """
    if len(room_record)<=0x394:raise ValueError('Rooms record too short')
    ident=room_record[0x394];matches=[]
    for index,record in enumerate(terrain_records):
        if len(record)<552:raise ValueError('Terrain record too short')
        if record[0x1D6]==ident:matches.append((index,record))
    result=dict(terrain_id=ident,match_count=len(matches),findings=[])
    if len(matches)!=1:
        result['findings'].append(dict(code='missing_terrain' if not matches else 'duplicate_terrain_id',id=ident))
        return result
    index,terrain=matches[0];result.update(terrain_record_index=index,lean=terrain_lean(terrain))
    resets=[]
    for field in SCHEMA['room_commit_side_effects']['scalar_resets']:
        o,n=field['raw_offset'],field['raw_bytes'];before=int.from_bytes(terrain[o:o+n],'little')
        resets.append(dict(field=field['field'],raw_offset=hex(o),before=before,projected_after=0,would_change=before!=0))
    flags=SCHEMA['room_commit_side_effects']['forced_terrain_flags'];o=flags['raw_offset']
    before=int.from_bytes(terrain[o:o+4],'little')
    after=(before&~flags['affected_mask'])|flags['forced_value']
    result.update(unconditional_scalar_resets=resets,
        forced_flags_projection=dict(before=hex(before),after=hex(after),mask=hex(flags['affected_mask']),would_change=before!=after),
        scope='Known Room GUI assignments on linked Terrain only; no bytes changed and no full save emulation.')
    return result
