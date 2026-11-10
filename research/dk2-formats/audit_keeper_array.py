"""Audit full native Keeper arrays,source-backed lookup and all original bytes."""
from pathlib import Path
import json,struct
from build_keeper_array import ROOT,load,digest,primary,corpus,outcomes
from audit_targeted_spell import executable_ranges
from build_effect_semantics import pe_read
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_keeper_array as native
import memory_readout

def main():
    s=load('keeper-array-semantics.json');functions,blocks=primary();assert functions==s['primary_program_bytes'] and blocks==s['primary_vtable_data']
    assert len(functions)==20 and sum(r['executed_path'] for r in functions)==13 and len(blocks)==1
    vv=bytes.fromhex(blocks[0]['hex'])
    for off,a in [(0xFC,0x50DC80),(0x100,0x50DCA0),(0x104,0x50DD00),(0x108,0x50DDD0),(0x10C,0x50DE50),(0x110,0x50DD60),(0x120,0x50DE00)]:
        assert struct.unpack_from('<I',vv,off-0xFC)[0]==a
    protected=executable_ranges(Path(s['executable']).read_bytes())+[(0x66E3EC,0x400)];by={c['name']:c for c in native.cases()};expected={}
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c=by.pop(row['case']);args,e=native.build(c);r=saved['result']
        assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['input']==args and saved['expected']==e
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items())
        assert memory_readout.decode(r['registers']).hex()==e['first128_expected_hex']
        regions=[(int(v['address'],16),bytes.fromhex(v['hex'])) for v in json.loads(args['memory'])['regions']]
        assert all(a+len(b)<=target or a>=target+n for a,b in regions for target,n in protected);expected[c['name']]=e
    assert not by and outcomes()==s['outcomes'] and len(s['native_emulations'])==41
    assert expected['physical-unsorted']['runtime_ids']==[1,3,7]
    assert expected['capacity-one-overflow']['runtime_ids']==[7] and len(expected['capacity-one-overflow']['steps'])==3
    assert not expected['capacity-full']['steps'][0]['loader_called']
    assert expected['duplicate3']['probes'][0]['index']==2 and expected['duplicate1']['probes'][0]['index']==0
    assert int.from_bytes(bytes.fromhex(expected['duplicate3']['output_expected_hex'])[0x14:0x18],'little')==0xFE0C0000
    assert int.from_bytes(bytes.fromhex(expected['duplicate1']['output_expected_hex'])[0x14:0x18],'little')==0xFE0C0001
    assert int.from_bytes(bytes.fromhex(expected['partial-c8-absent']['array_expected_hex'])[122+0x3A:122+0x3E],'little')==0x12340001
    assert expected['overlay-missing']['steps'][0]['null_write'] and expected['overlay-zero']['steps'][0]['null_write']
    source_out=bytes.fromhex(expected['source-order-duplicate']['output_expected_hex']);assert struct.unpack_from('<II',source_out,0x50)==(9,0)
    assert corpus()==s['corpus'] and s['corpus']['file_count']==82 and s['corpus']['records']==1066
    assert s['corpus']['new_order_byte_exact_patches']==246 and s['corpus']['whole_container_record_permutations']==82
    example=load('phase83-order-duplicate-source.json');assert digest(Path(example['file']).read_bytes())==example['sha256']
    search=load('phase83-array-root-candidates.json');md=Cs(CS_ARCH_X86,CS_MODE_32)
    for profile in search['profiles']:
        data=Path(profile['file']).read_bytes();assert digest(data)==profile['sha256'] and len(profile['hits'])==18
        for hit in profile['hits']:
            b=bytes.fromhex(hit['bytes']);assert pe_read(data,hit['address'],len(b))==b
            x=next(md.disasm(b,hit['address']));assert x.mnemonic+' '+x.op_str==hit['instruction']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==67 and package['keeper_pipeline_semantics']['phase83_runtime_array']==s
    assert package['spell_alternative_semantics']['phase82_targeted_cast']==load('targeted-spell-semantics.json')
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    v=dict(phase=83,date='2026-10-28',logical_scenarios=41,native_emulation_invocations=41,load_scenarios=29,accessor_scenarios=12,
      raw_converter_calls=33,load_callback_calls=38,controlled_low_address_write_scenarios=3,native_code_spans_attested=20,executed_path_spans=13,static_only_spans=7,
      original_vtable_data_ranges=1,source_files_hashchecked=82,source_records=1066,exact_DB_byte_patches=246,full_container_record_permutations=82,
      DX_new_array_root_candidates=18,old_untyped_secondary_candidates=18,old_native_invocations=0,source_executables_unchanged=4,package_components=67,
      original_code_or_vtable_replacements=0,real_game_crashes_triggered=0,C8_gameplay_purpose_proved=False,research_writes_to_original_game_data=0,
      significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase83-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))
if __name__=='__main__':main()
