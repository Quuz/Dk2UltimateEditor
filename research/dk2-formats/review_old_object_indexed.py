"""Prove global-table origins for indexed leads using verified function CFGs.

Calls invalidate EAX/ECX/EDX; other registers use the x86 callee-saved ABI.
Only direct branch destinations inside each complete Ghidra body are followed.
Unresolved indirect branches are reported, never treated as absence evidence.
"""
from pathlib import Path
from collections import deque
import json
import re
import struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32,CS_AC_WRITE
from capstone.x86 import X86_OP_REG,X86_OP_MEM,X86_OP_IMM
from build_effect_semantics import pe_read
from scan_interprocedural_creature_fields import GROUP

ROOT = Path(__file__).resolve().parent


def main():
    contexts = json.loads((ROOT/'phase70-old-indexed-contexts.json').read_text())
    data = (ROOT/'source-binaries/DKII-local13.exe').read_bytes()
    md = Cs(CS_ARCH_X86,CS_MODE_32)
    md.detail = True
    result,functions = [],[]
    switch_specs = {0x4F007B:(0x4F0A08,4),0x4F01D5:(0x4F0A18,4)}
    for start in sorted({r['function_hint'] for r in contexts}):
        file = ROOT/'ghidra'/f'phase70-old-{start:x}-assembly.txt'
        addresses = [int(line[:8],16) for line in file.read_text().splitlines() if re.match(r'^[0-9a-fA-F]{8}:',line)]
        assert addresses[0] == start
        nodes = {a:next(md.disasm(pe_read(data,a,15),a,count=1)) for a in addresses}
        last = nodes[addresses[-1]]
        end = last.address+last.size
        recovered = set()
        switches = []
        targets = {r['read'] for r in contexts if r['function_hint']==start}
        assert targets <= set(nodes),(hex(start),[hex(a) for a in targets-set(nodes)])
        checks = {a:set() for a in targets}
        todo = deque([(start,frozenset())])
        visited,indirect = set(),set()
        while todo:
            pc,known = todo.popleft()
            state = (pc,known)
            if state in visited:
                continue
            if pc not in nodes:
                if not start<=pc<end:
                    continue
                x = next(md.disasm(pe_read(data,pc,15),pc,count=1),None)
                assert x is not None and x.id,hex(pc)
                nodes[pc] = x
                recovered.add(pc)
            visited.add(state)
            x = nodes[pc]
            op = x.operands
            aliases = set(known)
            if pc in targets:
                mem = next(o for o in op if o.type==X86_OP_MEM)
                checks[pc].add(x.reg_name(mem.mem.base) in aliases)
            def reg(o):
                return GROUP.get(x.reg_name(o.reg),x.reg_name(o.reg))
            if x.mnemonic=='call':
                aliases -= {'eax','ecx','edx'}
            elif x.mnemonic=='mov' and op[0].type==X86_OP_REG:
                dst = reg(op[0])
                source = op[1]
                is_alias = source.type==X86_OP_REG and source.size==4 and reg(source) in aliases
                is_root = source.type==X86_OP_MEM and source.size==4 and not source.mem.base and not source.mem.index and source.mem.disp==0x6CF048
                aliases.discard(dst)
                if op[0].size==4 and (is_alias or is_root):
                    aliases.add(dst)
            else:
                for o in op:
                    if o.type==X86_OP_REG and o.access&CS_AC_WRITE:
                        aliases.discard(reg(o))
            nxt = pc+x.size
            if x.mnemonic.startswith('ret'):
                continue
            if x.mnemonic.startswith('j'):
                if op[0].type != X86_OP_IMM:
                    if pc in switch_specs:
                        table,count = switch_specs[pc]
                        destinations = list(struct.unpack('<'+'I'*count,pe_read(data,table,4*count)))
                        assert all(start<=a<end for a in destinations)
                        if not any(r['jump']==pc for r in switches):
                            switches.append(dict(jump=pc,table=table,count=count,targets=destinations,
                                bound='Native AND EAX,3 and CMP EAX,3 precede this jump;all four possible indices covered.'))
                        for dest in destinations:
                            todo.append((dest,frozenset(aliases)))
                    else:
                        indirect.add(pc)
                    continue
                destinations = [op[0].imm] if x.mnemonic=='jmp' else [op[0].imm,nxt]
            else:
                destinations = [nxt]
            for dest in destinations:
                todo.append((dest,frozenset(aliases)))
        for a,status in checks.items():
            result.append(dict(read=a,function=start,origin_verified_on_reached_states=status=={True},reached_alias_states=sorted(status)))
        recovered_file = f'phase70-old-{start:x}-recovered-switch-CFG.txt'
        if recovered:
            text = '\n'.join(f'{a:08x}: {nodes[a].mnemonic} {nodes[a].op_str}' for a in sorted(recovered))
            (ROOT/'ghidra'/recovered_file).write_text(text,encoding='utf8')
        functions.append(dict(function=start,assembly=file.name,states=len(visited),indirect_branch_stops=sorted(indirect),
                              recovered_instruction_count=len(recovered),recovered_assembly=recovered_file if recovered else None,switches=switches))
    out = dict(scope=__doc__,global_pointer=0x6CF048,initialized_target=0x6DD5D8,reads=sorted(result,key=lambda r:r['read']),functions=functions,
               caveat='Only reported reached states are proved. Empty or mixed state sets remain unresolved. Global target identity refers to the attested initializer contract,not an arbitrary corrupted runtime.')
    (ROOT/'phase70-indexed-origin-review.json').write_text(json.dumps(out,indent=2),encoding='utf8')
    print('Verified global-table origins',sum(r['origin_verified_on_reached_states'] for r in result),'of',len(result),
          'CFG states',sum(r['states'] for r in functions),'indirect stops',sum(len(r['indirect_branch_stops']) for r in functions))
    for r in result:
        if not r['origin_verified_on_reached_states']:
            print('Unresolved',hex(r['read']),r['reached_alias_states'])


if __name__=='__main__':
    main()
