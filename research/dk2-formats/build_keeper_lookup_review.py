"""Publish exact lookup behavior and bounded3-level C8 consumer-search scope."""
from pathlib import Path
import json,struct
from build_effect_semantics import pe_read
from build_trap_readiness import digest
from emulate_keeper_lookup import PROFILES

ROOT=Path(__file__).resolve().parent


def load(name):return json.loads((ROOT/name).read_text(encoding='utf8'))


def primary():
    spec=load('phase72-primary-manifest.json');functions=[];blocks=[]
    for r in spec['functions']:
        before,after,memory=[ROOT/'ghidra'/r[k] for k in ['before','after','memory']]
        v=json.loads(memory.read_text());b=bytes(v['data'])
        assert before.read_text().rstrip()==after.read_text().rstrip()
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length']
        assert b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        memory=ROOT/'ghidra'/r['memory'];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length']
        assert b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        blocks.append({**r,'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    assert struct.unpack('<I',bytes.fromhex(blocks[0]['hex'])[:4])[0]==0x5027A0
    assert bytes.fromhex(blocks[1]['hex'])==bytes.fromhex('8b461885c074098b964001000089553a')
    return functions,blocks


def link(name):return dict(file=name,sha256=digest((ROOT/name).read_bytes()))


def main():
    functions,blocks=primary();emu=load('phase72-keeper-lookup-emulations.json')
    scans=[]
    for name in ['phase72-DX-keeper-pointer-argument-leads.json','phase72-old-keeper-pointer-argument-leads.json']:
        q=load(name)
        scans.append({**link(name),'program_sha256':q['program_sha256'],'producer_roots':len(q['producer_roots']),
            'root_states':q['root_states'],'callee_summaries':q['callee_summaries'],'candidates':len(q['candidates']),
            'unknown_C8_candidates':sum(r['field']=='unknownC8' for r in q['candidates']),
            'control_candidates':sum(r['field'].startswith('control') for r in q['candidates']),
            'limited_roots':q['limited_roots'],'limited_callees':q['limited_callees'],
            'limited_argument_context_roots':sum(r['limited_argument_contexts'] for r in q['producer_roots'])})
    assert len(emu['cases'])==26 and [r['producer_roots'] for r in scans]==[45,48]
    s=dict(phase=72,family='KeeperSpells old/DX lookup contracts and argument-forward search',profiles=PROFILES,
        primary_program_bytes=functions,primary_data_and_code_fragments=blocks,native_emulations=emu['cases'],native_execution_scope=emu['scope'],
        runtime_layout=dict(record_bytes=122,array_pointer_in_world=0x1604,count_in_world=0x160C,ID_in_definition=0x75,
            old_world_vtable=0x65F31C,old_slot=0x110,old_slot_target=0x5027A0,
            old_scalar_fragment=dict(address=0x4EC919,bytes=16,presence=0x18,intermediate=0x140,definition=0x3A,
                scope='Static fragment verifies intermediate→definition copy only;oldrawconverter/full old loader not newly executed.')),
        lookup=dict(expression='key=argument&255;index=min(key-1,count-1);whileindex>=0:ifID[index]==key returnrow;ifID[index]<key returnNULL;--index;returnNULL.',
            behavior=['ID0 lookup returnsNULL evenwhenphysicalrow0exists.','[0,1,2] lookup1startsindex0andstopsatID0,so existingID1isnotfound.',
                'Unsorted[1,7,3] lookup7stopsatlastID3anddoesnotfind7.','Sparse[1,3,7] lookup7findsrow2;do not require IDs to be contiguous merely fromcount.',
                'Duplicate[1,1,3] lookup1findsrow0becauseinitialindex=key-1=0;notalwayslastduplicate.',
                'Onlylow8argumentused;12340003matches3,100maps0andreturnsNULL.'],
            preservation='Runtime lookup contract is diagnostic. Do not delete,sort,renumber or change physical file records automatically. Normal loader insertion/sorting is a separate operation;syntheticunsortedtablesarenotproofthenativeloaderleavesnormalarraysunsorted.'),
        consumer_search=scans,
        new_search_scope='All linear PE.text seeds plus direct-callee forwarding up to3levels;ECX andfirst8potentialstackslots,ESP/EBP aliases,correct TEST/CMPmemoryreadrule. Olderphase64passwasmapped-span/intraprocedural. Nativegetterrootverified;untypedvirtual110matchescanreferotherclasses.',
        search_result='No read overlapping candidateDWORD3A established. BothEXEsfind27controlleads each. Absence ofboundedcandidateisnotnewpurposeorproofglobalnon-use.',
        search_limitations=['Dynamicindices,indirectcallee/return identities,computedjumpdestinations,arbitraryheapspillsandunknownABIarenotfullyresolved.',
            'Walkermaystopwhenaliasesdieorindirectjumpsappear. Zero state/argument budget limits means no budget saturation,not whole-program coverage.',
            'Potentialstackslotsareforwardsleads,notprovenformalarguments. Onlyactualcallee loadsseedaliases;3-leveldepthnotexhaustive.',
            'Functionhintsfromnearestdirectcallarelabelsforselection,notproofactualfunctionownership. Instructionlinearpasscanmisscodeorinterpretdata.'],
        source_tools=[link(n) for n in ['prepare_keeper_pointer_search.py','scan_keeper_pointer_arguments.py','scan_old_keeper_pointer_arguments.py','scan_current_door_word236_arguments.py','native_memory_access.py']],
        corpus=load('keeper-pipeline-semantics.json')['corpus'],
        api=dict(file='native_keeper_lookup.py',operation='lookup_index',policy='Pure lookup overexplicitruntimeBYTEIDsequence. Does not emulate world sorting/file parsingorreturn nativepointer. Originalrawrecordsretained.'),
        limitations=['26 full lookup executions;two complete nativefunctions;2 separately labelleddata/codefragmentranges. No providers or originalcodepatches.',
            'EachobservercapturesexactnativeEAXbeforecomparisonandcomparesentire5888-byteworldand122-byte-rowsarrayunchanged. FinalEAX/EBPbelongtoobserver.',
            'Fulloldrawconverter/loader,worldinsertion,GUIandspecialSightOfEvilShotpathnotexecutedinthisstage.',
            'C8purpose,AP0408,Terrain198224,CreatureJump/PickUpPriority,Objects34C/Traps204/Doors236andotherfullgoalunknownsremainopen.',
            'No new raw patchcases;previous Keeper exactpatches fromphases44/53retainedseparately,not countedagain.'])
    (ROOT/'keeper-lookup-review.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    k=load('keeper-pipeline-semantics.json');k['phase72_lookup_review']=s
    (ROOT/'keeper-pipeline-semantics.json').write_text(json.dumps(k,ensure_ascii=False,indent=2),encoding='utf8')
    print('Keeper lookup26native;DX45/old48roots;C8purposeopen;67componentsembeddedsupplement')


if __name__=='__main__':main()
