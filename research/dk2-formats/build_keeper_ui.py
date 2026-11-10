"""Publish native UI order filtering,real click and local action queue evidence."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
from native_keeper_pipeline import patch_raw
from build_effect_semantics import pe_read
import emulate_keeper_ui as native
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    spec=load('phase84-primary-manifest.json');out=[]
    for group in ['functions','blocks']:
        rows=[]
        for r in spec[group]:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            row={**r,'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b)}
            if group=='functions':row['assembly_sha256']=digest((ROOT/'ghidra'/r['assembly']).read_bytes())
            else:row['hex']=b.hex()
            rows.append(row)
        out.append(rows)
    return tuple(out)

def corpus():
    sources=load('keeper-array-semantics.json')['corpus']['files'];files=[];patches=[]
    for r in sources:
        p=Path(r['file']);data=p.read_bytes();assert digest(data)==r['sha256'];files.append(dict(file=str(p),sha256=r['sha256']))
    for source in ['qz2','qz']:
        p=native.SOURCES[source];original=p.read_bytes();sections=fmt.parse(original);assert b''.join(s.pack() for s in sections)==original
        for si,s in enumerate(sections):
            if s.signature!=150:continue
            for ident in [9,13]:
                ri=next(i for i,r in enumerate(s.records) if r[0xEE]==ident);raw=s.records[ri];assert raw[0xDB]==9
                records=list(s.records);records[ri]=patch_raw(raw,0xDB,10);clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(records)
                packed=b''.join(clone.pack() if j==si else part.pack() for j,part in enumerate(sections));expected=bytearray(original);at=s.offset+len(s.header)+406*ri+0xDB;expected[at]=10
                assert packed==expected and fmt.parse(packed)[si].records==records
                patches.append(dict(file=str(p),id=ident,raw_offset=0xDB,absolute_offset=at,before=9,after=10,scope='In-memory exact container patch only;original file untouched'))
    return dict(files=files,source_files_hashchecked=len(files),new_exact_map_container_patches=patches,patch_count=len(patches),
      scope='Prior82 Keeper source hashes verified.4new exact DB9→10 patches:2actualQz maps×2alternative rows. Only selected byte changes;unknowns,headers,recordorder andothersections preserved. Prior246patches/82permutations not rerun or counted as new.')

def outcomes():
    rows=[]
    for c in native.cases():
        _,e=native.build(c);rows.append(dict(case=c['name'],source=e['source'],raw_records=e['raw_records'],runtime_ids=e['runtime_ids'],visible_ids=e['visible_ids'],
          selection_results=e['selection_results'],clicked_id=e['clicked_id'],tab_enabled=e['tab_enabled'],action_enqueued=e['action_enqueued'],action_record_hex=e['action_record_hex']))
    return rows

def main():
    functions,blocks=primary();rows=outcomes();cases=load('phase84-keeper-ui-emulations.json')['cases'];assert len(cases)==len(rows)==28
    indexed={r['case']:r for r in rows};rawcount=sum(r['raw_records'] for r in rows);assert rawcount==329
    assert indexed['standalone']['clicked_id']==13 and indexed['qz2']['clicked_id']==indexed['qz']['clicked_id']==9
    assert indexed['only-gold']['visible_ids']==[13] and indexed['qz2-only-gold']['visible_ids']==[]
    assert len(functions)==33 and sum(r['executed_path'] for r in functions)==22 and len(blocks)==5
    d=load('phase84-order-call-candidates.json');classified=[]
    for h in d['candidates']:
        classified.append(dict(address=h['address'],function=h['function_hint']['address'],instruction=h['instruction'],
          status='Different/unproven receiver;3arguments,not treated as World.v120' if h['function_hint']['address']==0x5992B0 else 'UI+53 World ordinal lookup;native consumer or static rendering/callback context'))
    s=dict(phase=84,date='2026-10-28',family='KeeperSpells',executable=functions[0]['exe'],executable_sha256=digest(Path(functions[0]['exe']).read_bytes()),
      primary_program_bytes=functions,primary_data_ranges=blocks,native_emulations=cases,outcomes=rows,corpus=corpus(),
      counts=dict(logical_scenarios=28,native_emulation_invocations=28,actual_raw_init_convert_insert_chains=329,native_selector_invocations=420,native_tab_callbacks=28,native_click_callbacks=28,
        local_actions_enqueued=sum(r['action_enqueued'] for r in rows),code_spans_attested=33,executed_path_spans=22,static_only_spans=11,file_backed_data_ranges=5,original_code_or_vtable_replacements=0),
      field=dict(raw_offset=0xDB,raw_bytes=1,intermediate_offset=0x153,definition_offset=0x6C,baseline_name='OrderInEditor',
        verified_consumer='Main client spell-grid ordering/selection and tab enabling. Its use is not confined to original editor UI.',
        serialization='Keep physical BYTE and file record order. Diagnose duplicates/zero/out-of-scan orders;do not silently normalize existing maps.'),
      receiver=dict(ui_world='Native402D00/4039A0 storeUI53 from *(UI4 Session +14);fixture Session14 points to original-vtable World.',
        world_player='OriginalWorld.v260=508C40 returns6EE540[low16(playerTag)] without an alive check. Valid prepared player required.',
        data='World Keeper count160C,array1604,stride122;player spell-state count6E,array72,stride17;ID byte0 and stateDWORD1.'),
      filtering=dict(selector='0x4115E0',scan='For order1..Worldcount inclusive:actualWorld.v120 finds first matching rawDB/runtime6C. No match gives0.',
        status='Actual4BAFA0 by-ID lookup returnsstoredDWORD state,or1 whenmissing. Eligible iff state!=1 andID!=5 andID!=14.',
        states='0,2,100hex,FFFFFFFF tested as accepted storedvalues. This does not define a complete availability enum or guarantee spell casting.',
        reserved='IDs5/14 excluded numerically,even when catalogue names differ. StandaloneID5 isSummonHorny;QzID5 isTurncoat. Other activation sources are outside this proof.',
        ordinal='Normal positions are1-based amongeligible scan results. Outputbyte unchanged on failure. Native equality comparison is outside eligibility-if:ordinal0 can return the first filtered-outID whenrunningcount is0;no normalization performed.',
        order_range='RawDB0 or>Worldcount is never queried by this scan. Sparse IDs are allowed,whilethe separate order byte must occur within scanned1..count toappear.'),
      tab=dict(entry='0x416E20',prepared_button_kind=2,write='ButtonDWORD61=1 iff at leastone eligible ordinaryspell;otherwise0.',scope='Other button kinds/door/trap tabs are not executed.'),
      click=dict(entry='0x411140',position='ordinal + UIE5*UIB5;fixturesE5=0,B5=4. No summoned-object button chain in prepared player.',
        valid='Native4094C0 clears priorselection path(initialmode0);thenUImode13DB=8,selectedBYTE13E8=ID,native40C180(cursorstate12F6=C),sound24,422600→4346E0→527130→525F00.',
        outputs='UI action-sequenceDWORD A increments;UI sound handle2E set to external55667788. Source definition sound handle71 isloaded11223344;native UI sound opcode104hex.',
        local_action='Actual Session queue receives18bytes:DWORD5,DWORD selectedSpellID,DWORD0,DWORD72hex,WORDplayerTag7.',
        scope='Real handler/internal localqueue executed;physical mouse dispatch,framebuffer,actual sound/network and world application of action not executed. Descriptor region contains original411140 pointer,not a full event-registration proof.'),
      queue=dict(entry='0x525F00 via527130',location='Session+1C',entry_bytes=18,capacity=32,count_offset=0x240,read_index_offset=0x244,write_index_offset=0x248,
        accepted='For tested valid states:copy18bytes to write index;incrementwrite;resetto0 iff it becomes32;incrementcount.',
        full='Ifcount==32:return0 without copy. UI callback ignores acceptance:selection/sound/actioncounter change althoughno newqueueentry. Count31/write31 wrap case passes.',
        malformed='No general >=32 guard or modulo rule inferred for malformed count/writeindex;onlyvalid0/31/32 controls.'),
      actual_maps=dict(files=[str(native.SOURCES['qz2']),str(native.SOURCES['qz'])],collision='RaiseDeadID9 andCreateGoldID13 bothrawDB9;order10 absent.',
        result='Native UI scan omitsCreateGold. At ordinaryordinal9 click selectsID9 and queues(5,9,0,72hex,7). If onlyID13 hasstate0 andothers1,tabisdisabled andnoaction isqueued.',
        controls='Standaloneordinal9 selects13. Qz2 source physical reversal leavescollisioneffect. In-emulator-only DB13→10 exposesGold atordinal10;DB9→10 restoresGold atordinal9.',
        safety='Originalmaps were not edited;results do not identify who produced duplicateorder orprove anoriginal-editor defect.'),
      C8=dict(purpose='unresolved',boundary_values=[0,1,0x7FFFFFFF,0x80000000,0xFE0C0000,0xFFFFFFFF],
        result='AppliedtoGoldrawC8 infullraw-load→UI→localqueue. UI selection/tab/action tuple unchanged across6values;World/definition checksumschange aspreviouslyknown.',global_nonuse_proved=False),
      call_search=dict(source=link('phase84-order-call-candidates.json'),decoded=d['decoded_instructions'],candidates=classified,
        limits='Literalv120/direct50DE00 candidates;10UIcalls classified,2different/unproven receivers excluded. No whole-program call-resolution claim.'),
      verification=dict(full_state='Every replay compares complete World,array+guards,lastintermediate,allrawrows,Sessionincludingqueue,UI,button,player,states,tagtable,outputs,soundcounters,globals,FS andscratchconstructor.',
        readout='128nativebytes fromSessionqueue start viaXMM. Remaining state verified by post-return whole-buffer equality;not advertised as fullraw snapshots.',
        setup='PreallocatedWorld/array,preparedUI/player/state/Session,initialhandmode0,cursorstateC,emptyobject chains,World7291=FF. Nativecode andoriginalWorld/UIvtables unmodified;nullGFX and2explicitexternal sound handlers.'),
      limitations=['ExactDX binary and supplied sources;no new old/RC3/Flame equivalence. UI/Player/Session constructors and stream parser/Worldallocation not fully executed.',
        'On-screen drawing,mouse event registration/delivery,actual sound/network and executionof queued action/spell effects are outside scope.',
        'State meanings beyond native numeric checks,other UI modes/summoned-object buttons andmalformed queues remain unspecified.',
        'UnknownC8,AP04/08,CreatureJump/Priority,Terrain198/224,Traps204,Objects34C,Doors236,ElementB1/B2,SpellE8/E9 andother substantial goals remain open.'])
    (ROOT/'keeper-ui-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    old=load('keeper-pipeline-semantics.json');old['phase84_ui_order']=s
    (ROOT/'keeper-pipeline-semantics.json').write_text(json.dumps(old,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(dict(phase=84,counts=s['counts'],corpus=dict(files=s['corpus']['source_files_hashchecked'],new_exact_patches=s['corpus']['patch_count']))))
if __name__=='__main__':main()
