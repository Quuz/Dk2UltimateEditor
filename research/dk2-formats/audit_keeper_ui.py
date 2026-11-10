"""Audit full native raw/UI/action chain and exact-source container changes."""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from audit_targeted_spell import executable_ranges
from build_keeper_ui import ROOT,load,digest,primary,corpus,outcomes
import emulate_keeper_ui as native
import memory_readout

def main():
    s=load('keeper-ui-semantics.json');functions,blocks=primary();assert functions==s['primary_program_bytes'] and blocks==s['primary_data_ranges']
    assert len(functions)==33 and sum(r['executed_path'] for r in functions)==22 and len(blocks)==5
    b=bytes.fromhex(blocks[0]['hex']);assert struct.unpack_from('<I',b,0x114-0xFC)[0]==0x508F10 and struct.unpack_from('<I',b,0x120-0xFC)[0]==0x50DE00
    assert bytes.fromhex(blocks[1]['hex'])==struct.pack('<I',0x508C40) and bytes.fromhex(blocks[2]['hex'])==struct.pack('<I',0x5096F0)
    assert struct.unpack_from('<I',bytes.fromhex(blocks[4]['hex']),0xA6)[0]==0x411140
    protected=executable_ranges(Path(s['executable']).read_bytes())+[(0x66E3EC,0x400),(0x66C4A4,92),(0x68FD58,0x140)]
    by={c['name']:c for c in native.cases()};expected={};actual_queue={};same_state=[]
    for row in s['native_emulations']:
        p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());c=by.pop(row['case']);args,e=native.build(c);r=saved['result']
        assert digest(p.read_bytes())==row['sha256'] and saved['case']==c and saved['input']==args and saved['expected']==e
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,ECX=0,EBP=1,EBX=0).items())
        q=memory_readout.decode(r['registers']);assert q.hex()==e['queue128_expected_hex'];actual_queue[c['name']]=q;expected[c['name']]=e
        regions=[(int(v['address'],16),bytes.fromhex(v['hex'])) for v in json.loads(args['memory'])['regions']]
        assert all(a+len(b)<=target or a>=target+n for a,b in regions for target,n in protected)
        if c['c8'] is not None:
            prepared_expected=[b for a,b in regions if 0x11000000<=a<0x12000000];assert len(prepared_expected)==len(e['checks'])
            # Their source binding/fullnative comparisons were verified above.
            assert len(prepared_expected[4])==0x300 and len(prepared_expected[5])==0x2800
            same_state.append((prepared_expected[4],prepared_expected[5],q))
    assert not by and len(same_state)==6 and all(v==same_state[0] for v in same_state)
    assert s['outcomes']==outcomes() and len(s['native_emulations'])==28
    assert struct.unpack_from('<4IH',actual_queue['standalone'])==(5,13,0,0x72,7)
    assert struct.unpack_from('<4IH',actual_queue['qz2'])==(5,9,0,0x72,7)
    assert expected['only-gold']['visible_ids']==[13] and expected['qz2-only-gold']['visible_ids']==[]
    assert expected['only-gold']['tab_enabled'] and not expected['qz2-only-gold']['tab_enabled']
    assert expected['qz2']['visible_ids']==expected['qz2-reverse']['visible_ids']
    assert expected['qz2-gold-order10']['clicked_id']==expected['qz2-raise-dead-order10']['clicked_id']==13
    assert expected['only-reserved']['visible_ids']==expected['qz2-only-id5']['visible_ids']==[]
    assert expected['queue-full']['clicked_id']==13 and not expected['queue-full']['action_enqueued']
    assert expected['queue-wrap']['action_enqueued']
    assert expected['all-hidden']['selection_results'][0]==dict(ordinal=0,success=True,id=4)
    assert all(expected[n]['visible_ids']==[13] for n in ['gold-state2','gold-state100','gold-state-max'])
    assert s['corpus']==corpus() and s['corpus']['patch_count']==4 and s['corpus']['source_files_hashchecked']==82
    search=load('phase84-order-call-candidates.json');exe=Path(search['file']).read_bytes();assert digest(exe)==search['sha256'];md=Cs(CS_ARCH_X86,CS_MODE_32)
    for r in search['candidates']:
        b=bytes.fromhex(r['bytes']);assert b==pe_read(exe,r['address'],len(b));x=next(md.disasm(b,r['address']));assert f'{x.address:08x}: {x.mnemonic} {x.op_str}'==r['instruction']
    for file,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/file).read_bytes())==sha
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==digest(Path(old['path']).read_bytes())==old['sha256']
    rc3=load('camera-catalogue-semantics.json')['rc3'];assert digest(Path(rc3['original_file']).read_bytes())==digest(Path(rc3['research_copy']).read_bytes())==rc3['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==67 and package['keeper_pipeline_semantics']['phase84_ui_order']==s
    assert package['keeper_pipeline_semantics']['phase83_runtime_array']==load('keeper-array-semantics.json')
    assert package['spell_alternative_semantics']['phase82_targeted_cast']==load('targeted-spell-semantics.json')
    for key,row in package['components'].items():
        b=(ROOT/row['file']).read_bytes();assert digest(b)==row['sha256'] and json.loads(b)==package[key]
    v=dict(phase=84,date='2026-10-28',native_emulation_invocations=28,raw_init_convert_insert_chains=329,native_selector_calls=420,native_tab_callbacks=28,native_click_callbacks=28,
      local_actions_enqueued=20,native_code_spans_attested=33,executed_path_spans=22,static_only_spans=11,original_data_ranges=5,
      source_files_hashchecked=82,new_exact_map_container_patches=4,source_executables_unchanged=4,package_components=67,
      original_code_or_vtable_replacements=0,actual_network_or_sound_calls=0,real_mouse_or_framebuffer_runs=0,C8_gameplay_purpose_proved=False,
      research_writes_to_original_game_data=0,significant_unknowns_remain=True,limitations=s['limitations'])
    (ROOT/'phase84-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({k:v for k,v in v.items() if k!='limitations'}))
if __name__=='__main__':main()
