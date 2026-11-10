"""Confirm six canonical-instruction guesses against actual native bodies."""
from pathlib import Path
import json
from build_effect_semantics import pe_read

ROOT = Path(__file__).resolve().parent


def main():
    rows = json.loads((ROOT/'phase71-anchored-boundary-preparation.json').read_text())
    data = (ROOT/'source-binaries/DKII-local13.exe').read_bytes()
    queries,boundaries = [],[]
    for r in rows:
        a = r['function_hint']
        previous = pe_read(data,a-16,16)
        assert previous[-1] in [0x90,0xC3] or previous[-3]==0xC2,(hex(a),previous.hex())
        boundaries.append(dict(address=a,previous16=previous.hex(),first16=pe_read(data,a,16).hex(),canonical_read=r['canonical_guess']))
        key = f'phase71-boundary-old-{a:x}'
        queries.extend([
            dict(id=key+'-create',tool='create_function',args=dict(program='DKII-local13.exe',address=hex(a),disassemble_first=True),print_limit=5),
            dict(id=key+'-assembly',tool='disassemble_function',args=dict(program='DKII-local13.exe',address=hex(a)),print_limit=5),
        ])
    (ROOT/'phase71-boundary-entries.json').write_text(json.dumps(boundaries,indent=2),encoding='utf8')
    (ROOT/'phase71-boundary-queries.json').write_text(json.dumps(queries,indent=2),encoding='utf8')


if __name__=='__main__':
    main()
