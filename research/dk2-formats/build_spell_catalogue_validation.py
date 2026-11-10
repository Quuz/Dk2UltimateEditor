"""Evidence supplement to existing CreatureSpells alternative/storage schema."""
from pathlib import Path
import json
from build_keeper_cast import ROOT,load,digest
from build_effect_semantics import pe_read
from build_spell_alternatives import corpus

def primary():
    out=[]
    for r in load('phase55-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert p.read_text().rstrip()==after.read_text().rstrip() and int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return out

def main():
    names=[f'phase55-xrefs-{a:x}.txt' for a in [0x56A560,0x4F7860,0x51FE00,0x56A6D0,0x746FCA,0x747102,0x747103]]
    s=dict(phase=55,program='DKII-DX.exe',program_sha256=load('executable-hash-verification.json')['Dungeon Keeper 2/DKII-DX.exe'],
        primary_program_bytes=primary(),native_emulations=load('phase55-spell-catalogue-emulations.json')['cases'],corpus=corpus(),
        supplement_to='spell-alternative-semantics.json',
        native_chain=['0x56A6D0 parser','0x56A490 initializer','0x56A560 converter','0x51FE00 world record callback','0x4F7860 definition loader','0x50DFA0 lookup'],
        scratch=dict(raw_buffer='0x746EC0',raw_bytes=266,intermediate='0x746FCA',intermediate_bytes=346,alternative_shot='0x747102',alternative_room='0x747103',reuse='Traps parser571040 loads747103 and calls initializer570B60. It zeros747 bytes there. Two composed tests first complete CreatureSpells parser then deliberately call this actual initializer and compare all747 zeros,346 CreatureSpell intermediate bytes and unchanged210-byte definition array/world. This establishes scratch overwrite possibility,not actual unconditional loading order.'),
        source_lifetime='Parser resets the same346-byte global intermediate for each record, converts raw bytes and invokes51FE00 synchronously. The callback sorts/copies only70-byte definitions or updates an existing70-byte definition. In checked paths it does not retain raw/intermediate pointers in CWorld. After return the global scratch contains the final record,not a persistent per-spell alternate-reference array.',
        alternative_result='Across complete parser→callback→loader tests,changing rawE8/E9 changes only each definition checksumDWORD1B and the insertion accumulator when checksum gate6FCDCF=0. When gate!=0 even those stay unchanged. Update callback does not change world accumulator/count. This strengthens phase42 to complete record loading;it does not identify a gameplay replacement rule.',
        distinct_rule='RawEE still copies to definitionBYTE45 and controls existing target-eligibility predicates. It must not be conflated with rawE8/E9.',
        previously_static_now_executed=['Insertion shifts full70-byte definitions and inserts before equal IDs','Lookup starts at min(ID-1,count-1);ID0 returnsnull','Capacity full silently skips insertion while parser stillreturns1','Update branch uses actual virtualE8→50DFA0 getter then actual4F7860,does not incrementcount or addchecksum','Raw EF tail does not enter global346-byte intermediate'],
        registration='51E0A0(ordinary branch) and541AA0 assign51FE00 as record callback. Twelve main tests use the actual registration shape with null header callback and preallocated capacity3. Five supplementary tests combine native header counting51FDE0 with record callback as an explicit artificial harness;these do not claim actual world allocation/count passes executed together.',
        validation='18 finalv3 parser executions,3 input records each,126 original getter calls. Compare entire3×70 definitions,41932-byte world,346-byte intermediate,input stream unchanged and12 event/cursor bytes. Two cases additionally run complete747-byte Traps initializer on overlapping scratch. Twelve main cases cover2checksumgates×2insertion/update states×3alternativepairs. Four edge cases cover duplicateIDs,ID0,capacitydrop,opaquetail. Full funcs executed except low-level memory stream read;resourceprovider null.',
        fixtures='World and definition arrays preallocated;70-byte unused slots seededA5;world graphics/resource pointer null. Only native5B3EB0 read function replaced: bounded memory copy,maximum266bytes and no read past supplied stream end,setsstatus/bytesRead/returnsstatuspointer,cdeclRET. Global checksum gate and TPS explicit. Callbacks/getter/converter/loader instruction bodies are native.',
        xrefs=[dict(file=n,sha256=digest((ROOT/'ghidra'/n).read_bytes())) for n in names],
        api=dict(file='native_spell_catalogue.py',scope='Diagnostic null-resource loader/callback projection with A5unused-slot fixture;not a raw codec,allocator or automatic ID repair'),
        limitations=['Only exact DX image;old/RC3 lifecycle equivalence not established.','Actual filesystem,descriptor resolution,count/allocation passes,graphics success branches and gameplay execution are outside scope.','No general proof of unused alternate IDs in every code path/build;rawE8/E9 remain authoritative editor fields and are preserved.','Scratch initializer invoked deliberately after parser in two tests;actual Traps loading order depends on descriptor/runtime flow. Traps parser body reviewed statically,not executed in these cases.','Update-modeactivation remains unresolved.10548 mapped-range displacement scan has18leads and one directwritecandidate in5099A0;no global non-use inferred.','No new component added:existing alternative schema receives this evidence supplement;package stays65components. Original files and all4EXEs unchanged.'])
    assert len(s['native_emulations'])==18 and len(s['primary_program_bytes'])==12
    (ROOT/'spell-catalogue-validation.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('CreatureSpells pipeline supplement:18native,12completePEranges')
if __name__=='__main__':main()
