"""Read-only native editor views, separate from the game compatibility checker."""
from pathlib import Path
import json

ROOT=Path(__file__).resolve().parent
SCHEMA=json.loads((ROOT/'editor-field-bindings.json').read_text(encoding='utf8'))

def editor_views(record,family):
    result=[]
    for field in SCHEMA['bindings']:
        if field['family']!=family:continue
        start,size=field['raw_offset'],field['raw_bytes']
        raw=record[start:start+size]
        if len(raw)!=size:raise ValueError('Record too short for '+field['baseline_field'])
        unsigned=int.from_bytes(raw,'little')
        value=int.from_bytes(raw,'little',signed=field['editor_interpretation'].startswith('signed_'))
        value*=field['editor_scale']
        lo,hi=field['editor_validation_range']
        result.append(dict(field=field['baseline_field'],raw_offset=hex(start),raw_unsigned=unsigned,
            editor_value=value,editor_label=field['label'],editor_validation_range=[lo,hi],
            within_editor_validation=lo<=value<=hi,
            control_id=field['control']['control_id'],
            note='Native editor view/range only. It does not validate game behavior or instruct a writer to change bytes.'))
    return result

def trap_commit_projection(record):
    """Project only the explicitly traced Require Mana / cost assignment.

    No byte mutation and no claim to reproduce the complete native save: the
    dialog must first pass its own validation, which can fail for other fields.
    """
    if len(record)<0x1F0:raise ValueError('Trap record too short')
    flags=int.from_bytes(record[0x1EC:0x1F0],'little')
    before=int.from_bytes(record[0x1D4:0x1D8],'little')
    after=before if flags&0x100 else 0
    return dict(field='ManaCostToFire',require_mana=bool(flags&0x100),raw_before=before,
        projected_raw_after=after,would_change=before!=after,
        scope='Cost assignment only, if the native properties commit reaches it. No input bytes changed.')
