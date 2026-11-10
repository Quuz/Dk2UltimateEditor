"""Validate a plain-language explanation without promoting static paths to execution."""
import hashlib
import json
import struct
from pathlib import Path
import dk2_formats as fmt
from build_effect_semantics import EXE, pe_read
from spell_state_tables import initialized_states

ROOT = Path(__file__).resolve().parent


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    data = EXE.read_bytes()
    assert digest(data) == 'd7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
    spans = json.loads((ROOT/'crypt-gameplay-primary-ranges.json').read_text(encoding='utf8'))
    for row in spans:
        actual = json.loads((ROOT/row['ghidra_bytes']).read_text(encoding='utf8'))
        expected = pe_read(data, row['address'], row['length'])
        assert bytes.fromhex(actual['hex']) == expected
        row['sha256'] = digest(expected)
        row['coverage'] = 'Static reading only; no new execution of these enclosing paths.'
    table, rows = initialized_states()
    states = [r for r in rows if r['state'] in [274, 275, 276]]
    assert [(r['state'], r['callback'], r['category']) for r in states] == [
        (274, 0x483790, 11), (275, 0x483870, 11), (276, 0x4839E0, 11)]
    sources = []
    records = {}
    for filename, signature, offset in [('Rooms.kwd', 0x78, 0x392), ('Objects.kwd', 0xF0, 0x35A)]:
        path = ROOT.parent.parent/'Dungeon Keeper 2/Data/editor'/filename
        raw = path.read_bytes()
        records[filename] = {r[offset]: r[:32].split(b'\0')[0].decode('ascii')
                             for section in fmt.parse(raw) if section.signature == signature
                             for r in section.records}
        sources.append(dict(path=str(path), sha256=digest(raw)))
    assert records['Rooms.kwd'][26] == 'Crypt'
    assert records['Rooms.kwd'][14] == 'Graveyard'
    assert records['Objects.kwd'][142] == 'Crypt Bell'
    actor=(ROOT/'ghidra/phase90-creature-factory-base.bin').read_bytes()
    assert len(actor)==1343 and actor[0x36F]==8
    result = dict(status='passed', purpose='Explanation of existing phase94/107 findings and explicit static context.',
                  executable=dict(path=str(EXE), sha256=digest(data)), primary_code=spans,
                  initialized_state_rows=states, source_catalogues=sources,
                  source_names=dict(Crypt=26, Graveyard=14, CryptBell=142),
                  cross_species_recipient=dict(CreatureID=8,source='ghidra/phase90-creature-factory-base.bin',
                                               sha256=digest(actor),checks='Phase107 Fairy/Wizard registration paths update this Vampire recipient.'),
                  assigned_Crypt_groups=8,total_Crypt_groups=18,
                  native_execution_dependencies=['phase94-validation.json', 'phase107-validation.json'],
                  limitations=['New caller paths are static only.', 'No automatic AI entry or live visible animation claimed.',
                               'No exact job output or actual distance with Fairy claimed.',
                               'No new KWD fields or changes to frozen package schemas.'])
    (ROOT/'crypt-gameplay-evidence.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf8')
    print('passed: 4 original PE ranges, 3 callback bindings, Crypt/Graveyard/Bell names; static scope retained')


if __name__ == '__main__':
    main()
