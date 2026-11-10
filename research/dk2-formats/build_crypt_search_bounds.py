"""Publish bounded Crypt search negatives without inventing field meanings."""
from copy import deepcopy
import gzip,json,re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import emulate_crypt_search_bounds as native
import rescan_crypt_limited_contexts as search
import build_recovery_step as storage
ROOT=native.ROOT
KEY='crypt_search_boundary_semantics'
SCHEMA='crypt-search-boundary-semantics.json'
sha=native.sha
load=native.load
dump=native.dump
canonical=lambda d:sha(json.dumps(d,sort_keys=True,ensure_ascii=False).encode())
link=lambda n:dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def primary():
    exe=native.EXE.read_bytes();base,image=native.native.pe_image(exe)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    proofs=[]
    for address,length,kind in [(0x5056E0,99,'complete-function'),(0x4BCAE0,60,'complete-function'),(0x4BDDA0,74,'bounded-array-loop-only')]:
        memory=f'ghidra/phase186-search-{address:x}-memory.txt';r=load(memory)
        blob=image[address-base:address-base+length]
        assert bytes.fromhex(r['hex'])==bytes(r['data'])==blob and r['length']==length
        listing=(ROOT/f'ghidra/phase186-search-{address:x}-assembly.txt').read_text()
        assert listing.startswith(f'{address:08x}:'.upper()) or listing[:8].lower()==f'{address:08x}'
        proofs.append(dict(address=hex(address),length=length,kind=kind,sha256=sha(blob),
            memory=link(memory),assembly=link(f'ghidra/phase186-search-{address:x}-assembly.txt')))
    checks={
        0x5056E4:('mov','al, byte ptr [edi + 0x103]'),0x5056F7:('and','esi, 0xff'),
        0x50570A:('cmp','edx, esi'),0x50570C:('jl','0x505701'),0x50570E:('cmp','word ptr [edi + edx*2], cx'),
        0x4BCAE0:('xor','edx, edx'),0x4BCAE4:('mov','dl, byte ptr [ecx + 0x1bf]'),
        0x4BCB03:('cmp','eax, edx'),0x4BCB05:('jb','0x4bcafa'),
        0x4BDDC5:('xor','edx, edx'),0x4BDDC9:('mov','dl, byte ptr [ebp + 0x1bf]'),
        0x4BDDE6:('cmp','eax, edx'),0x4BDDE8:('jb','0x4bddd9')}
    instructions=[]
    for a,wanted in checks.items():
        ins=next(md.disasm(image[a-base:a-base+15],a,count=1))
        assert (ins.mnemonic,ins.op_str)==wanted,(hex(a),ins.mnemonic,ins.op_str,wanted)
        instructions.append(dict(address=hex(a),hex=ins.bytes.hex(),mnemonic=ins.mnemonic,operands=ins.op_str))
    return dict(profile=link('phase185-validation.json'),program_sha256=sha(exe),extents=proofs,instruction_checks=instructions)

def build():
    rescan=load('phase186-crypt-limited-rescan.json');reports=rescan['reports'];assert len(reports)==13
    assert rescan['program_sha256']==search.EXPECTED
    for ref in rescan['helpers']+[rescan['prior']]:assert link(ref['file'])==ref
    for r in reports:
        raw=r['raw'];assert link(raw['file'])==raw
        full=json.loads(gzip.decompress((ROOT/raw['file']).read_bytes()))
        assert search.summary_rows(full)==r['events'] and (full['states'],full['limited'])==(r['states'],r['limited'])
        assert r['baseline_reproduced'] and r['previous_limit']==5000 and r['new_limit']==50000
    limits=[dict(context=r['id'],states=r['states']) for r in reports if r['limited']]
    hits=[dict(context=r['id'],**h) for r in reports for h in r['counter_candidates'] if h['new_beyond_previous_limit']]
    bounds={
        '5056e0':dict(start=0xA21,element_bytes=2,count_offset=0xB24,maximum_count=255,maximum_index=255,maximum_word_offset=0xC1F,
            witness_pcs=['0x505701','0x50570e','0x50571c'],reason='Bytecountmasked255. Searchindices0..N-1; terminalcheckalsoindexN. Shiftreadindex<=255 evenifbytecountisupdated. Owner+A21 is a search context, not a new raw KWD field.'),
        '4bcae0':dict(start=0xB2,element_bytes=2,count_offset=0x1BF,maximum_count=255,maximum_index=254,maximum_word_offset=0x2AE,
            witness_pcs=['0x4bcafa'],reason='ZeroedEDXreceivesDLbytecount; unsignedindexcomparison. AtN255onlyindices0..254.'),
        '4bdda0':dict(start=0xB2,element_bytes=2,count_offset=0x1BF,maximum_count=255,maximum_index=254,maximum_word_offset=0x2AE,
            witness_pcs=['0x4bddd9'],reason='SamezeroedEDX/DLbytecountandunsignedindexcomparison. This proofcoversfirstarraylooponly,notwholefunctionorotherpointers.')}
    assert len(hits)==54 and sum(h['unknown'] for h in hits)==30
    for h in hits:
        b=bounds[h['context']];read=h['read']
        assert read['address'] in b['witness_pcs'] and read['offset']>b['maximum_word_offset']
    manifest=load('phase186-array-emulations.json');assert len(manifest['replays'])==9
    traces=[]
    for c,row in zip(native.cases(),manifest['replays']):
        _,_,saved,rebuilt=native.execute(c);assert rebuilt==row;traces.append(saved['result'])
    controls=load('phase186-array-crosschecks.json')['replays'];assert len(controls)==3
    for row in controls:
        c=next(c for c in native.cases() if c['name']==row['case']);saved=load(row['file'])
        assert saved['input']==native.cross_build(c) and link(row['file'])['sha256']==row['sha256']
        r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return'] and int(r['registers']['EAX'],16)==1
    proof=primary();pcs={int(a,16) for r in traces for a in r['visited_instructions'] if int(a,16)!=native.DRIVER and int(a,16)<0x10000000}
    assert all(any(int(e['address'],16)<=a<int(e['address'],16)+e['length'] for e in proof['extents'][:2]) for a in pcs)
    baseline=load('phase165-source-baseline.json')
    for r in baseline['source_files']+baseline['executable_paths']:assert sha(native.Path(r['file']).read_bytes())==r['sha256'],r['file']
    preserved=load('phase186-prior-package-state.json')
    for r in preserved['components'].values():assert link(r['file'])==r
    return dict(phase=186,status='verified_three_false_search_lead_families_not_counter_meanings',
        counts=dict(revisited_contexts=13,old_callee_contexts=12,old_root_contexts=1,baseline_reproductions=13,static_states=sum(r['states'] for r in reports),
            contexts_now_finished=1,contexts_still_limited=len(limits),new_counter_overlap_leads=54,new_unknown_counter_overlap_leads=30,
            rejected_overlap_leads=54,new_counter_meanings=0,native_scenes=9,independent_Ghidra_controls=3,
            Ghidra_compared_regions=3,Ghidra_compared_bytes=3*8192,unique_executed_original_PCs=len(pcs),
            Crypt_passive_counter_meanings_assigned=8,Crypt_active_groups_assigned=10,Crypt_counter_meanings_remaining=10,
            prior_components_preserved=163,new_raw_fields=0,new_raw_edits=0,source_writes=0),
        search=dict(evidence=link('phase186-crypt-limited-rescan.json'),unchanged_transfer='Frozenphase178; diagnostictracecap64only, stateidentityunchanged. All13old5000resultsreproducedbefore50000.',
            remaining_limits=limits,completed_context='4b2ce0 stackargument+32:32986states; boundedsearchcompletiononly,notallfunctionbehavior.'),
        bounds=bounds,rejected_candidates=hits,primary=proof,native_executions=manifest,independent_Ghidra_controls=controls,
        storage=dict(new_raw_fields=0,new_raw_edits=0,rechecked=storage.storage(),reference=link('phase184-validation.json')),
        source_baseline=link('phase165-source-baseline.json'),
        helpers=[link(n) for n in ['rescan_crypt_limited_contexts.py','emulate_crypt_search_bounds.py','build_crypt_search_bounds.py']],
        priority=dict(current='Crypt groups',block_complete=False,next=['Realconsumersof10unassignedownercounters; indirectionandsearchlimitsremain.',
            'NaturalCryptselection/terminationunderactualscheduler; livingparticipationproved,notsalary/experience/mandatorysacrifice.',
            'Pretimeout255graphicscontextisaseparateopenboundary; notnewformatsemantics.']),
        limitations=['StaticpointerhintsarenotOwner-typeproof. Noabsenceofconsumersinwholeprogramproved.',
            'Native/Ghidrascenescovertwocompletefunctionsatselectedcounts,notallbranches;4BDDA0loopboundisstaticonly.',
            'Count128/255fixturesarebyte-representablestresscases,notprovedlegalgamearraycapacities.',
            'ThisstageassignsnoCryptbonus/rawfieldanddoesnotchangecodec,effectiveviewsorreadinesspercentage.'])

NOTE='crypt_search_boundary_semantics phase186:13previouslylimitedcontextsrevisited5000->50000,632986states;1finished/12stilllimited.54newcounteroverlaps(30unknown)rejectedbythreebyte-countarraybounds;9native/3Ghidra,24576B. Noadditionalcountermeaning/rawfield;8passive/10active roles distinct;163priorcomponents/source/codec preserved,goalactive.'

def main():
    p=load('editor-format-package.json');prior=ROOT/'phase186-prior-package-state.json'
    if not prior.exists():
        assert len(p['components'])==163
        dump(prior.name,dict(components=deepcopy(p['components']),codec=link('dk2_formats.py'),package_sha256=sha((ROOT/'editor-format-package.json').read_bytes()),
            preserved_top_level={k:canonical(v) for k,v in p.items() if k not in ['components','precedence',KEY]}))
    previous=load(prior.name);schema=build();dump(SCHEMA,schema)
    assert set(p['components']) in [set(previous['components']),set(previous['components'])|{KEY}]
    for k,v in previous['components'].items():assert p['components'][k]==v
    for k,h in previous['preserved_top_level'].items():assert canonical(p[k])==h
    p[KEY]=schema;p['components'][KEY]=link(SCHEMA)
    if NOTE not in p['precedence']:p['precedence'].append(NOTE)
    dump('editor-format-package.json',p)
    dump('phase186-search-status.json',dict(phase=186,goal_status='active',current_block='Crypt groups',block_complete=False,
        previous_goal_turn=dict(classification='no progress',reason='Userquestionansweredfromalready-publishedevidence; nextsafeactionwaslimited-contextrescan.'),
        current_turn=dict(classification='progress',reason='Raisedboundedsearch;classified54falseleadswithPE/Ghidrabyteproofandnativecontrols;packagepublishedwithoutnewfieldclaims.'),
        counts=schema['counts'],remaining=schema['priority']['next'],evidence=[link(SCHEMA)]))
    print(json.dumps(schema['counts']))

if __name__=='__main__':main()
