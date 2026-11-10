"""Bounded primary-byte check of Pro/RC3 storage access and local writer loop.

RC3 local loop alignment is proved by identical instruction bytes except CALL
displacement. No RC3 function-wide CPU execution or complete UI-use claim.
"""
from pathlib import Path
import json
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from emulate_terrain_opaque_table import sha

ROOT=Path(__file__).resolve().parent
PRO=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'
RC3=ROOT/'source-binaries/DK2-Editor-rc3.exe'

def validate():
    pro,rc3=PRO.read_bytes(),RC3.read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32)
    getter=pe_read(pro,0x425400,26);assert getter==pe_read(rc3,0x42F770,26)
    assert getter.hex()=='8b44240483f81072066633c0c20400668b844128030000c20400',getter.hex()
    # Entire loop ends after JC to PUSH index. Last branch bytes are included.
    starts=[0x4A7966,0x4AD186];loops=[]
    for blob,a,target in [(pro,starts[0],0x425400),(rc3,starts[1],0x42F770)]:
        raw=pe_read(blob,a,29);ins=list(md.disasm(raw,a));assert sum(i.size for i in ins)==len(raw)
        calls=[i for i in ins if i.mnemonic=='call'];assert len(calls)==1 and int(calls[0].op_str,16)==target
        assert calls[0].address==a+12
        masked=bytearray(raw);masked[13:17]=bytes(4)
        loops.append(dict(address=a,length=29,bytes=raw.hex(),normalized_call_bytes=masked.hex(),
            instructions=[dict(address=i.address,bytes=i.bytes.hex(),mnemonic=i.mnemonic,operands=i.op_str) for i in ins]))
    assert loops[0]['normalized_call_bytes']==loops[1]['normalized_call_bytes']
    inherited=json.loads((ROOT/'phase128-terrain-model-primary.json').read_text())
    rows=[r for r in inherited['fresh_functions'] if r['address'] in [0x425400,0x425420,0x49F5E0,0x4A7690]]
    assert len(rows)==4
    for row in rows:
        assert sha((ROOT/row['assembly']).read_bytes())==row['assembly_sha256']
        data=bytes(json.loads((ROOT/row['memory']).read_text())['data'])
        assert sha((ROOT/row['memory']).read_bytes())==row['memory_sha256']
        assert sha(data)==row['original_bytes_sha256'] and data==pe_read(pro,row['address'],row['length'])
    xrefs=[]
    for getter_name,pc in [('425400',0x4A7972),('425420',0x49F883)]:
        name=f'ghidra/phase134-Terrain-{getter_name}-xrefs.txt'
        text=(ROOT/name).read_text();assert text.strip().count('\n')==0 and f'{pc:08x}' in text.lower()
        xrefs.append(dict(file=name,sha256=sha((ROOT/name).read_bytes()),direct_xref=pc))
    return dict(phase=134,scope=__doc__,profiles=[dict(file=str(p),sha256=sha(p.read_bytes())) for p in [PRO,RC3]],
        inherited_full_Pro_function_spans=rows,Pro_getter=0x425400,Pro_setter=0x425420,RC3_getter=0x42F770,
        getter_bytes=26,getter_unsigned_index_limit=16,getter_out_of_range='AX=0, upperEAX unspecified',
        model_offset=0x328,raw_offset=0x1AE,words=16,element_bytes=2,
        native_direct_xrefs=xrefs,RC3_local_writer_loops=loops,RC3_local_writer_equivalent_except_call_displacement=True,
        RC3_CPU_executions=0,semantic_names_found=False,complete_UI_use_search=False)

if __name__=='__main__':
    v=validate();(ROOT/'phase134-Terrain-table-editor-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print('Pro4whole-function spans; RC3getter26bytes and local writer loop29bytes verified')
