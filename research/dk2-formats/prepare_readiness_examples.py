"""Read-only evidence for the user's three readiness examples, 2026-10-28."""
from pathlib import Path
import json, hashlib, struct

ROOT = Path(__file__).resolve().parent

def main():
    tasks = []
    for address in [0x6f28f2, 0x6f28f4, 0x50f7e0, 0x494650]:
        tasks.append(dict(id=f'readiness-DX-{address:x}-xrefs', tool='get_xrefs_to',
                          args=dict(program='DKII-DX.exe', address=hex(address)), print_limit=20))
    for address in [0x50f7e0, 0x4596d0, 0x459c20]:
        for tool, suffix in [('create_function', 'create'), ('disassemble_function', 'assembly'), ('force_decompile', 'decompile')]:
            args = dict(program='DKII-DX.exe', address=hex(address))
            if tool == 'create_function': args['disassemble_first'] = True
            tasks.append(dict(id=f'readiness-DX-{address:x}-{suffix}', tool=tool, args=args, print_limit=20))
    (ROOT/'readiness-examples-queries.json').write_text(json.dumps(tasks, indent=2), encoding='utf8')
    doc = json.loads((ROOT/'decoded/GlobalVariables.kwd.json').read_text(encoding='utf8'))
    path = Path(doc['source'])
    data = path.read_bytes()
    assert hashlib.sha256(data).hexdigest() == doc['sha256'], 'Re-decode changed source before claiming current values'
    selected = []
    for record in doc['sections'][0]['records']:
        if record['words_u32'][0] in (43, 113, 114):
            offset = record['offset']
            assert bytes.fromhex(record['raw_hex']) == data[offset:offset+16]
            selected.append(record)
    result = dict(source=str(path), sha256=doc['sha256'], records=selected,
                  status='Current source bytes checked. Names/units require separate consumer evidence.')
    (ROOT/'readiness-examples-current-records.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
    print('Sacrifice records:', sum(r['words_u32'][0] == 43 for r in selected))
    for r in selected:
        if r['words_u32'][0] in (113,114):
            print('Scalar:', hex(r['offset']), r['words_u32'])
    print('Queries:', len(tasks))
    creatures = json.loads((ROOT/'decoded/Creatures.kwd.json').read_text(encoding='utf8'))
    creature_path = Path(creatures['source'])
    assert hashlib.sha256(creature_path.read_bytes()).hexdigest() == creatures['sha256']
    names = {r['fields']['CreatureId']: r['fields']['Name'] for r in creatures['sections'][0]['records']}
    for r in [r for r in selected if r['words_u32'][0] == 43][:3]:
        f = r['fields']
        inputs = [names.get(f[f'Input[{i}].Id'], f[f'Input[{i}].Id']) for i in range(3) if f[f'Input[{i}].Type'] == 1]
        print('Recipe by baseline enum names:', inputs, '->', names.get(f['RewardValue']), 'type', f['RewardType'])

if __name__ == '__main__': main()
