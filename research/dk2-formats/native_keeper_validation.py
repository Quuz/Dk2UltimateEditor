"""Evidence descriptions for Keeper pre-cast rules; not a general gameplay simulator."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent
def describe_rule(kind,value):
    if kind not in ['cast','target']:raise ValueError('Expected cast or target')
    if type(value) is not int or not 0<=value<=255:raise ValueError('Expected byte selector')
    schema=json.loads((ROOT/'keeper-validation-semantics.json').read_text(encoding='utf8'))
    table=schema['cast_rules' if kind=='cast' else 'target_rules']
    return table.get(str(value),dict(selector=value,status='Unsupported by the exact inspected native dispatcher; raw byte must still be preserved',native_dispatch_return=0))
