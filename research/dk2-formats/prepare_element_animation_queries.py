"""Continue phase 102 with previously unqueried native render helpers."""
import json
from pathlib import Path

root = Path(__file__).resolve().parent
queries = []
for address in (0x4B3B40, 0x4B3C10, 0x4B4710, 0x4B47C0, 0x4B3610):
    for tool in ('create_function', 'disassemble_function', 'force_decompile', 'get_xrefs_to'):
        args = {'program': 'DKII-DX.exe', 'address': hex(address)}
        if tool == 'create_function':
            args['disassemble_first'] = True
        queries.append({'id': f'phase102-animation-{address:x}-{tool}',
                        'tool': tool, 'args': args, 'print_limit': 5})
(root / 'phase102-animation-queries.json').write_text(
    json.dumps(queries, indent=2), encoding='utf8')
