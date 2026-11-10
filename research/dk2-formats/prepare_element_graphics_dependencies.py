"""Primary DX code for the actual BridgeThing graphics dispatch and builders."""
import json
from pathlib import Path

root = Path(__file__).resolve().parent
queries = []
for address in (0x59D260, 0x59C2A0, 0x57CEB0, 0x57F1E0, 0x57FAF0,
                0x5801E0, 0x580480, 0x583120):
    for tool in ('create_function', 'disassemble_function', 'force_decompile', 'get_xrefs_to'):
        args = {'program': 'DKII-DX.exe', 'address': hex(address)}
        if tool == 'create_function':
            args['disassemble_first'] = True
        queries.append({'id': f'phase102-builder-{address:x}-{tool}',
                        'tool': tool, 'args': args, 'print_limit': 5})
(root / 'phase102-builder-queries.json').write_text(
    json.dumps(queries, indent=2), encoding='utf8')
