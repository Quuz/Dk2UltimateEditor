"""Attest executed original PCs and original Crypt return epilogues."""
from pathlib import Path
from functools import cache
import json, re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
import emulate_crypt_action_completion as native
import prepare_crypt_room_assignment_primary as old
from audit_targeted_spell import executable_ranges

ROOT = Path(__file__).resolve().parent
sha = native.sha
load = lambda name: json.loads((ROOT/name).read_text(encoding='utf-8'))
dump = lambda name, value: (ROOT/name).write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


@cache
def build():
    blob = native.EXE.read_bytes(); ranges = executable_ranges(blob)
    visited, roots = set(), set()
    for row in load('phase189-crypt-completion-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes()) == row['sha256']
        trace = load(row['file'])['result']
        visited |= {int(pc, 16) for pc in trace['visited_instructions'] if any(a <= int(pc,16) < a+n for a,n in ranges)}
        roots |= {target for source, target in trace['observed_call_edges']}
    candidates = {}
    for manifest in ['phase188-crypt-job-primary.json', 'phase187-crypt-waiting-primary.json', 'phase180-crypt-action-primary.json']:
        if not (ROOT/manifest).exists():
            continue
        d = load(manifest)
        for row in d.get('functions', []) + d.get('inherited', []):
            if row.get('program', 'DKII-DX.exe') == 'DKII-DX.exe':
                candidates.setdefault(row['address'], dict(row, source_manifest=manifest))
    for row in old.inherited_rows():
        candidates.setdefault(row['address'], row)
    fresh_addresses = [0x4633D0, 0x46F440, 0x4DED00]
    for address in fresh_addresses:
        assert address in roots, hex(address)
        prefix = f'ghidra/phase189-completion-primary-{address:x}-'
        raw = bytes(load(prefix+'read_memory.txt')['data'])
        assert raw == native.p.pe_read(blob, address, len(raw))
        candidates[address] = dict(address=address, length=len(raw), memory=prefix+'read_memory.txt',
                                   assembly=prefix+'disassemble_function.txt',
                                   assembly_sha256=sha((ROOT/(prefix+'disassemble_function.txt')).read_bytes()),
                                   boundary_evidence='ExistingGhidrafunctionbody;entryisactualcapturedoriginalCALLtarget.',
                                   info=dict(file=prefix+'get_function_by_address.txt', sha256=sha((ROOT/(prefix+'get_function_by_address.txt')).read_bytes())))
    covered, used = set(), []
    for row in candidates.values():
        pcs = old.instructions(row)
        if not pcs & visited:
            continue
        file = old.path(row['memory']); raw = bytes(load(file)['data'])
        assert len(raw) == row['length'] and raw == native.p.pe_read(blob, row['address'], len(raw))
        used.append(dict(row, memory=file, memory_sha256=sha((ROOT/file).read_bytes()), bytes_sha256=sha(raw)))
        covered |= pcs
    missing = visited - covered
    if missing:
        dump('phase189-completion-primary-gaps.json', dict(missing=[hex(x) for x in sorted(missing)],
             actual_CALL_entries_not_covered=[hex(x) for x in sorted((roots & visited)-covered)]))
    assert not missing, [hex(x) for x in sorted(missing)[:60]]
    # A static claim about original listed return epilogues, separately from
    # the controlled executions and separately from all World/AI lifetimes.
    file = ROOT/'ghidra/phase180-primary-499ca0-assembly.txt'
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    pcs = [int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):', file.read_text(encoding='utf-8'), re.M)]
    instructions = [next(md.disasm(native.p.pe_read(blob, pc,15), pc)) for pc in pcs]
    epilogues = []
    for index, ins in enumerate(instructions):
        if ins.mnemonic != 'ret':
            continue
        trail = []
        for previous in reversed(instructions[max(0,index-8):index]):
            trail.append(dict(address=previous.address, text=previous.mnemonic+' '+previous.op_str))
            if previous.mnemonic == 'mov' and previous.op_str == 'eax, 1':
                break
            assert previous.mnemonic in ['pop', 'add'], (hex(ins.address), trail)
            assert not previous.op_str.startswith('eax'), (hex(ins.address), trail)
        else:
            raise AssertionError((hex(ins.address), trail))
        epilogues.append(dict(return_address=ins.address, reversed_epilogue=trail, EAX=1))
    assert epilogues
    return dict(profile=dict(file=str(native.EXE), sha256=sha(blob)),
                functions=[row for row in used if row['address'] in fresh_addresses],
                inherited=[row for row in used if row['address'] not in fresh_addresses],
                unique_executed_original_PCs=len(visited), all_executed_original_PCs_covered=True,
                callback_return_epilogues=epilogues,
                callback_scope='AllRETinstructionsintheoriginalGhidralistinghaveEAX1epilogues;notproofaboutallindirectcalleeoutcomes,modifiedcodeorWorldscheduler.',
                original_listing=dict(file=str(file.relative_to(ROOT)), sha256=sha(file.read_bytes())),
                scope='ExactPE/Ghidrainstructioncoverageof11acceptedscenes;notthewholegame/allversions.')


def main():
    value = build(); dump('phase189-crypt-completion-primary.json', value)
    print(len(value['inherited']), 'spans', value['unique_executed_original_PCs'], 'PC', len(value['callback_return_epilogues']), 'return epilogues')


if __name__ == '__main__':
    main()
