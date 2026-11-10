"""Offline audit of native evidence, exact source bytes and package integration."""
from pathlib import Path
import json,hashlib,struct
from build_sacrifice_semantics import ROOT,load,digest,primary,recipe_warnings,permutation_groups
from audit_targeted_spell import executable_ranges
import emulate_sacrifice_matching as matching
import emulate_sacrifice_dispatch as dispatch
import native_sacrifice as api
import dk2_formats as fmt

def normalize(v):return json.loads(json.dumps(v))
def main():
    s=load('sacrifice-semantics.json');assert primary()==s['primary_program_bytes']
    code=Path(s['profile']['executable']).read_bytes();assert digest(code)==s['profile']['sha256']==api.DX_SHA256
    protected=executable_ranges(code)+[(0x66e3ec,0x400)]
    successes=faults=0;entries=set();native_counts=dict(raw_callbacks=0,history_appends=0,match_calls=0,spell_handlers=0)
    for module,key in [(matching,'native_matching'),(dispatch,'native_dispatch')]:
        cases={c['name']:c for c in module.cases()}
        for row in s[key]['cases']:
            c=cases.pop(row['case']);p=ROOT/'ghidra'/row['file'];saved=json.loads(p.read_text());args=module.build(c)[0];r=saved['result']
            assert digest(p.read_bytes())==row['sha256'] and saved['case']==normalize(c) and saved['input']==args
            if module is matching:
                _,fault,details=module.build(c);assert saved['expected_fault']==fault and saved['expected']==details
                if fault:
                    assert not r['success'] and not r['hit_return'] and r['final_pc']==fault['pc'] and r['emulation_error']=='Divide by 0';faults+=1
                else:
                    assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,ECX=0).items());successes+=1
                if c['kind']=='load':native_counts['raw_callbacks']+=len(c['raws'])
                else:native_counts['history_appends']+=len(c['events']);native_counts['match_calls']+=1
            else:
                _,details=module.build(c);assert saved['expected']==details
                assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,ECX=0).items());successes+=1
                native_counts['raw_callbacks']+=len(c['raws']);native_counts['spell_handlers']+=1
                native_counts['history_appends']+=len(c['prior'])+int(c['room'])
                if c['room']:native_counts['match_calls']+=(details['selected_recipe']+1 if details['selected_recipe'] is not None else len(c['raws']))
            regions=[(int(v['address'],16),bytes.fromhex(v['hex'])) for v in json.loads(args['memory'])['regions']]
            assert all(a+len(b)<=target or a>=target+n for a,b in regions for target,n in protected)
            spans=sorted((a,len(b)) for a,b in regions);assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:]))
        assert not cases
    assert successes==57 and faults==2
    # Independently inspect exact instruction sites establishing order, averaging, reset and four-case dispatch.
    matcher=(ROOT/'ghidra/phase88-sacrifice-DX-4f1660-assembly.txt').read_text()
    for text in ['004f16ad: SUB EAX,ECX','004f16af: DEC EAX','004f16f3: ADD DL,AL','004f1749: IDIV ESI','004f1756: ADD EDX,EAX']:assert text in matcher
    executor=(ROOT/'ghidra/phase88-sacrifice-DX-4f1780-assembly.txt').read_text()
    assert executor.index('004f1792: CALL 0x004e4d90')<executor.index('004f179b: MOV byte ptr [ESI + 0x5b],0xff')<executor.index('004f17ac: CMP EAX,0x3')
    caller=(ROOT/'ghidra/phase88-sacrifice-DX-5133a0-assembly.txt').read_text();assert '0051342d: JNZ 0x0051343f' in caller and '00513436: ADD ESI,0x22' in caller
    source_count=records=0;refused=len(s['corpus']['excluded_noncanonical_files']);patches=s['corpus']['exact_container_patch_checks']
    for row in s['corpus']['files']:
        data=Path(row['file']).read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data);assert b''.join(v.pack() for v in parts)==data
        actual=[];warnings=[];permutations=[]
        for i,section in enumerate(parts):
            if section.signature==230:
                rr=[(j,r) for j,r in enumerate(section.records) if struct.unpack_from('<I',r)[0]==43];actual+=rr
                warnings.extend(dict(section=i,**w) for w in recipe_warnings(rr));permutations.extend(dict(section=i,**g) for g in permutation_groups(rr))
        assert len(actual)==row['recipes'] and warnings==row['warnings'] and normalize(permutations)==row['permutation_groups'];records+=len(actual);source_count+=1
    for row in s['corpus']['excluded_noncanonical_files']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    assert source_count==557 and records==44 and patches==records*len(api.FIELDS)*3==1188 and refused==24
    for f,sha in load('executable-hash-verification.json').items():assert digest((ROOT.parent.parent/f).read_bytes())==sha
    rc=load('rc3-job-overlap-semantics.json')['profile'];assert digest(Path(rc['original_file']).read_bytes())==rc['sha256']
    old=load('creature-version-layout-semantics.json')['old_source'];assert digest(Path(old['original_path']).read_bytes())==old['sha256']
    package=load('editor-format-package.json');assert len(package['components'])==68 and package['sacrifice_semantics']==s
    prior=load('phase88-prior-components.json');assert len(prior)==67
    for key,row in package['components'].items():
        data=(ROOT/row['file']).read_bytes();assert digest(data)==row['sha256'] and json.loads(data)==package[key]
        if key!='sacrifice_semantics':assert prior[key]==row
    assert s['api']['sha256']==digest((ROOT/'native_sacrifice.py').read_bytes())
    for key in ['current_recipe_export','current_recipe_table']:
        assert digest((ROOT/s[key]['file']).read_bytes())==s[key]['sha256']
    export=load(s['current_recipe_export']['file']);assert digest(Path(export['source']).read_bytes())==export['sha256'] and len(export['recipes'])==44
    for row in export['catalogues']:assert digest(Path(row['file']).read_bytes())==row['sha256']
    source=Path(export['source']).read_bytes()
    for row in export['recipes']:assert source[row['file_offset']:row['file_offset']+16]==bytes.fromhex(row['raw_hex'])
    assert (ROOT/'SACRIFICE_RU.md').exists() and 'Актуальный этап 88:' in (ROOT/'INTEGRATION_RU.md').read_text(encoding='utf8')
    v=dict(phase=88,date='2026-10-28',native_cases=59,successful_cases=successes,expected_native_division_faults=faults,
      full_spell_dispatch_cases=22,**native_counts,whole_native_functions_attested=16,original_code_or_vtable_replacements=0,
      source_files_hashchecked=source_count+refused,strict_parse_and_roundtrip_files=source_count,strict_parser_refusals_retained=refused,
      recipe_records=records,exact_container_patch_checks=patches,source_executables_unchanged=4,original_files_written=0,
      package_components=68,previous_67_components_preserved=True,live_game_runs=0,goal_achieved=False,significant_unknowns_remain=True,
      limitations=s['limitations'],codec_scope='1188 builder checks; source hashes and native caches independently revalidated here, no native replay repeated')
    (ROOT/'phase88-validation.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({k:x for k,x in v.items() if k not in ['limitations','codec_scope']},ensure_ascii=True))
if __name__=='__main__':main()
