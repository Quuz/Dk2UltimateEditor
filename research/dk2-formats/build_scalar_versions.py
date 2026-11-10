from pathlib import Path
import json,hashlib,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
import dk2_formats as fmt
import emulate_possession_scalar_versions as native
import native_possession_scalars as api
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    out={}
    for key,rows in load('phase101-primary-manifest.json').items():
        out[key]=[]
        for r in rows:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text(encoding='utf8'));b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    return out
def results():
    m=load('phase101-scalar-version-emulations.json');rows=m['replays'];by={c['name']:c for c in native.cases()};out=[];assert len(rows)==len(by)==32 and m['scope']==native.__doc__
    for row in rows:
        p=ROOT/'ghidra'/row['file'];v=json.loads(p.read_text(encoding='utf8'));c=by[row['case']];args,e=native.build(c);r=v['result']
        assert v['case']==c and v['input']==args and v['expected']==e and digest(p.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==x for k,x in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
        out.append(dict(case=c,expected=e,evidence=row))
    return rows,out
def corpus():
    dependencies=[link(n) for n in ['native_possession_scalars.py','dk2_formats.py']];file=ROOT/'phase101-scalar-api-validation.json';sources=load('phase88-sacrifice-corpus.json')['files']
    if file.exists():
        v=load(file.name);assert v['dependencies']==dependencies
        assert [(r['file'],r['sha256']) for r in v['files']]==[(r['file'],r['sha256']) for r in sources]
        for r in v['files']:assert digest(Path(r['file']).read_bytes())==r['sha256']
        return v
    files=[];counts={str(k):0 for k in [112,113,114]};patches=0;ag=hashlib.sha256()
    for row in sources:
        data=Path(row['file']).read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data
        local=0
        for si,s in enumerate(parts):
            if s.signature!=0xe6:continue
            for ri,raw in enumerate(s.records):
                kind=struct.unpack_from('<I',raw)[0]
                if kind not in [112,113,114]:continue
                counts[str(kind)]+=1;local+=1;assert api.view(raw)['type']==kind
                for value in [0,6,0xffff,0xabcd0006,0xffffffff]:
                    changed=api.patch_value_bits(raw,value);assert changed[:4]==raw[:4] and changed[8:]==raw[8:] and api.view(changed)['loaded_word']==value&65535
                    rr=list(s.records);rr[ri]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
                    packed=b''.join(clone.pack() if i==si else a.pack() for i,a in enumerate(parts));want=bytearray(data);struct.pack_into('<I',want,s.offset+len(s.header)+16*ri+4,value)
                    assert packed==want and fmt.parse(packed)[si].records==rr;ag.update(hashlib.sha256(packed).digest());patches+=1
        files.append(dict(file=row['file'],sha256=row['sha256'],selected_records=local))
    assert len(files)==557 and patches==5*sum(counts.values())
    failures=0
    for call in [lambda:api.view(bytes(15)),lambda:api.view(bytes(16)),lambda:api.patch_value_bits(struct.pack('<4I',113,6,1,2),-1),lambda:api.patch_value_bits(struct.pack('<4I',114,6,1,2),1<<32),lambda:api.patch_value_bits(struct.pack('<4I',112,6,1,2),True)]:
        try:call()
        except ValueError:failures+=1
        else:raise AssertionError('Expected refusal')
    v=dict(dependencies=dependencies,files=files,file_count=len(files),records_by_type=counts,selected_records=sum(counts.values()),api_exact_container_patches=patches,refusals=failures,ordered_patch_digest=ag.hexdigest(),
      scope='All557 previouslyacceptedVariables files hash/roundtrip checked. Five DWORD4 patches perselected record preserve completeheaders/order/opaquebytes. No source writes;earlier24 strictrefusals retained.')
    file.write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');return v
def bindings():
    out=[];md=Cs(CS_ARCH_X86,CS_MODE_32)
    for profile,p in native.PROFILES.items():
        exe=ROOT/'source-binaries/DKII-local13.exe' if profile=='old' else ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';data=exe.read_bytes();table=0x511478 if profile=='old' else 0x51d740
        entries=[]
        for k in [112,113,114]:
            target=struct.unpack('<I',pe_read(data,table+4*(k-3),4))[0];ins=list(md.disasm(pe_read(data,target,14),target))
            assert ins[0].mnemonic=='mov' and ins[0].op_str==f'word ptr [0x{p["variables"]+0x3a0+2*(k-112):x}], ax'
            entries.append(dict(type=k,jump_target=target,word_address=p['variables']+0x3a0+2*(k-112),instruction=ins[0].mnemonic+' '+ins[0].op_str))
        out.append(dict(profile=profile,exe=str(exe),sha256=digest(data),callback=p['callback'],switch_table=table,entries=entries))
    return out
def main():
    p=primary();rows,out=results();raw=corpus();bind=bindings();prior=ROOT/'phase101-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==79;prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    scan=load('phase101-old-possession-scalar-leads.json');assert len(scan['candidates'])==110
    data=Path(bind[0]['exe']).read_bytes();fragment=pe_read(data,0x4767de,17);assert fragment.hex()=='8b0d78d96d00b81f85eb5181e1ffff0000' # throughAND ECX,FFFFh
    s=dict(phase=101,date='2026-10-28',profiles=bind,primary=p,native_replays=rows,outcomes=out,corpus=raw,
      helpers=[link(n) for n in ['native_possession_scalars.py','emulate_possession_scalar_versions.py']],
      physical_records='Sixteenbytes:<type:u32,value:u32,opaque:u32,opaque:u32>. Value isDWORD4,not type224 statvalueDWORD8;fullphysicalvalue/opaque preserved.',
      runtime_storage='Both exactprofiles store low16 atVariablebase+3A0/+3A2/+3A4 for112/113/114 respectively. Full32bits ofinput value remain in source file;highbits are discarded onlybythese consumers.',
      native_scope='32 cases,16perprofile:30singlecalls plus2fourcallback sequences=38callbackexecutions. Whole4096byteVariables block,input16prefix+rawrecord+64guard,andoutput+64guard checked. No originalcode/vtable patches,providers,gameactors or gameplaytick.',
      overlap=dict(old_address=0x4767de,old_read=0x6dd978,DX_address=0x47bb4e,DX_read=0x6f28f0,
        explanation='DWORD read spans112and113 storage,but subsequentAND ECX,FFFFh discards all113 bits. Bothfragments byteattested;fullold476600 static. Doesnotshowusage of113 bonus.',
        old_fragment_hex=fragment.hex()),
      search=dict(evidence=link('phase101-old-possession-scalar-leads.json'),candidates=110,
        conclusion='Newlinearold-profile neighbourhood inventoryfoundwrites to113/114 andoverlapreadabove,no standaloneabsoluteconsumer assigned. Indexed/adjusted/copied/indirect paths not exhausted. GhidraNoReferences result foruncreatedswitcharms wasnotaccepted asabsenceproof.'),
      baseline_names={str(k):load('enums.json')['Variable.MiscVariable.MiscType'][str(k)] for k in [112,113,114]},
      counts=dict(native_cases=32,callback_executions=38,whole_code_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),variable_source_files=raw['file_count'],selected_records=raw['selected_records'],new_raw_API_patches=raw['api_exact_container_patches']),
      limitations=['Exactold/DXhashes only;folder13 notcertifiedretail.',
        'No actual113/114 gameplaybonusformula,affectedstats,HPchange,radius orpossessioncycle established. No+6percent claim.',
        'Sourceparser notexecuted;prefix+physical16record passedtorealcallback directly.',
        'Neighbour112 represented asstoragecontrol;newphase doesnotclaimits completeconsumer/gameplaysemantics.',
        'Fullold476600 staticonly;onlytwo scalarcallbacks executed. No fullswitchbranchcoverage claim from spanread.',
        'Other15-familyunknowns remainopen;prior79schemas preserved byteexact.'])
    (ROOT/'possession-scalar-version-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']));print('types',raw['records_by_type'])
if __name__=='__main__':main()
