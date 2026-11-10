"""Attest original CPlayer writer entries, vtables and executed PE bytes."""
from functools import cache
import json,re,sys
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_IMM
import emulate_crypt_state_writer as native
import prepare_crypt_room_assignment_primary as old
from audit_targeted_spell import executable_ranges
ROOT,sha=native.ROOT,native.sha
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf-8'))
dump=lambda n,d:(ROOT/n).write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8')


@cache
def evidence():
    blob=native.EXE.read_bytes();ranges=executable_ranges(blob);visited=set();roots=set()
    for row in load('phase190-crypt-state-writer-emulations.json')['replays']:
        assert sha((ROOT/row['file']).read_bytes())==row['sha256']
        saved=load(row['file']);r=saved['result'];assert r['success'] and r['hit_return']
        visited|={int(x,16) for x in r['visited_instructions'] if any(a<=int(x,16)<a+n for a,n in ranges)}
        roots|={int(saved['input']['address'],16)}|{int(x['address'],16) for x in saved['input']['before_calls']}
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;entries=[]
    for pc in sorted(visited):
        ins=next(md.disasm(native.p.pe_read(blob,pc,15),pc))
        if ins.mnemonic=='call' and ins.operands[0].type==X86_OP_IMM and ins.operands[0].imm in visited:
            target=ins.operands[0].imm;roots.add(target);entries.append(dict(source=pc,target=target,kind='executed_original_direct_CALL'))
    for table,slot in [(0x66D99C,4),(0x66E3AC,4),(0x671F80,12),(0x672FE8,8)]:
        target=int.from_bytes(native.p.pe_read(blob,table+slot,4),'little');assert target in visited
        roots.add(target);entries.append(dict(vtable=table,slot=slot,target=target,kind='original_vtable_target_executed'))
    # The original constructor passes this original callback to the native
    # vector-construction helper; it is not an arbitrary interior address.
    assert (0x40D640).to_bytes(4,'little') in native.p.pe_read(blob,0x4B8450,0x150) and 0x40D640 in visited
    roots.add(0x40D640);entries.append(dict(source_constructor=0x4B8450,target=0x40D640,kind='original_constructor_callback_argument_executed'))
    candidates={r['address']:r for r in old.inherited_rows()}
    for name in ['phase189-crypt-completion-primary.json','phase160-native-door-reader-primary.json','phase160-door-reader-primary.json']:
        if not (ROOT/name).exists():continue
        d=load(name)
        for r in d.get('functions',[])+d.get('inherited',[]):
            if r.get('program','DKII-DX.exe')!='DKII-DX.exe':continue
            if r['address'] not in candidates or r['length']>candidates[r['address']]['length']:
                candidates[r['address']]=dict(r,source_manifest=name)
    covered=set().union(*(old.instructions(r) for r in candidates.values()))
    fresh=sorted((roots&visited)-covered)
    return blob,visited,roots,entries,candidates,fresh


def queries():
    blob,visited,roots,entries,candidates,fresh=evidence()
    dump('phase190-writer-entry-proofs.json',dict(profile_sha256=sha(blob),entries=entries,fresh_entries=fresh))
    dump('phase190-writer-function-queries.json',[dict(id=f'phase190-writer-{a:x}-info',tool='get_function_by_address',args=dict(program='DKII-DX.exe',address=hex(a)),print_limit=0) for a in fresh])
    print('actual entries missing primary:',[hex(a) for a in fresh])


def memory_queries():
    blob,visited,roots,entries,candidates,fresh=evidence();tasks=[]
    for a in fresh:
        txt=(ROOT/f'ghidra/phase190-writer-{a:x}-info.txt').read_text(encoding='utf-8')
        body=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',txt)
        assert body, (hex(a),txt)
        begin,end=map(lambda x:int(x,16),body.groups());assert begin==a,(hex(a),hex(begin))
        for tool,suffix,args in [('disassemble_function','assembly',{}),('read_memory','memory',dict(length=end-begin+1))]:
            tasks.append(dict(id=f'phase190-writer-{a:x}-{suffix}',tool=tool,args=dict(program='DKII-DX.exe',address=hex(a),**args),print_limit=0))
    dump('phase190-writer-memory-queries.json',tasks);print(len(tasks),'primary queries')


@cache
def build():
    blob,visited,roots,entries,candidates,fresh=evidence();covered=set();new=[];inherited=[]
    for a in fresh:
        memory=f'ghidra/phase190-writer-{a:x}-memory.txt';assembly=f'ghidra/phase190-writer-{a:x}-assembly.txt'
        raw=bytes(load(memory)['data']);assert raw==native.p.pe_read(blob,a,len(raw))
        row=dict(address=a,length=len(raw),memory=memory,assembly=assembly,memory_sha256=sha((ROOT/memory).read_bytes()),
                 assembly_sha256=sha((ROOT/assembly).read_bytes()),bytes_sha256=sha(raw),entry_evidence='phase190-writer-entry-proofs.json',extent_kind='original_Ghidra_function_body')
        new.append(row);covered|=old.instructions(row)
    for row in candidates.values():
        pcs=old.instructions(row)
        if not pcs&visited:continue
        memory=old.path(row['memory']);raw=bytes(load(memory)['data'])
        assert len(raw)==row['length'] and raw==native.p.pe_read(blob,row['address'],len(raw))
        inherited.append(dict(row,memory=memory,memory_sha256=sha((ROOT/memory).read_bytes()),bytes_sha256=sha(raw)))
        covered|=pcs
    gaps=visited-covered
    assert not gaps,[hex(x) for x in sorted(gaps)[:50]]
    return dict(profile=dict(file=str(native.EXE),sha256=sha(blob)),functions=new,inherited=inherited,
                unique_executed_original_PCs=len(visited),all_executed_original_PCs_covered=True,
                entries=dict(file='phase190-writer-entry-proofs.json',sha256=sha((ROOT/'phase190-writer-entry-proofs.json').read_bytes())),
                scope='Sixbare-CPlayerwriterexecutions;notfulllivesavegameorKWDformats.')


def main():
    if '--queries' in sys.argv:queries();return
    if '--memory' in sys.argv:memory_queries();return
    d=build();dump('phase190-crypt-state-writer-primary.json',d)
    print(len(d['functions']),'new',len(d['inherited']),'inherited',d['unique_executed_original_PCs'],'originalPC')


if __name__=='__main__':main()
