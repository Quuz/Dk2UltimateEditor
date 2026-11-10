"""Phase 102: SGMap names are leads; original DX functions are the evidence."""
import json
from pathlib import Path

root = Path(__file__).resolve().parent
queries = []
for address in (0x59BB80, 0x59C140, 0x59C360, 0x59C720, 0x59C880,
                0x59CCF0, 0x59D010, 0x59D420, 0x59D4B0):
    for tool in ('create_function', 'disassemble_function', 'force_decompile', 'get_xrefs_to'):
        args = {'program': 'DKII-DX.exe', 'address': hex(address)}
        if tool == 'create_function':
            args['disassemble_first'] = True
        queries.append({'id': f'phase102-graphics-{address:x}-{tool}',
                        'tool': tool, 'args': args, 'print_limit': 5})
(root / 'phase102-graphics-queries.json').write_text(
    json.dumps(queries, indent=2), encoding='utf8')
