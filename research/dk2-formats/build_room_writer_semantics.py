"""Publish complete Room writer and corrected native TorchIntensity byte width."""
from pathlib import Path
import json
import emulate_room_writer as native
import native_room_writer as api
import crosscheck_room_writer as cross
import prepare_room_writer_primary as primary
from build_possession_damage import source_executables

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def link(n):return dict(file=n,sha256=native.sha((ROOT/n).read_bytes()))

def verify_native():
    manifest=load('phase122-room-writer-emulations.json');assert manifest['scope']==native.__doc__ and len(manifest['replays'])==17
    for c,row in zip(native.cases(),manifest['replays']):
        saved=load(row['file']);assert saved['input']==c and row['case']==c['case']
        assert row['sha256']==native.sha((ROOT/row['file']).read_bytes())
        r=saved['result'];assert r['success'] and r['regions_match'] and r['actual']==c['expected']
        assert r['original_code_substitutions']==0 and r['Win32_IAT_data_bindings']==5 and not r['protected_failures']
        assert all(hex(a) in r['visited_instructions'] for a in [0x4AB320,0x4AEB67,0x4AED4D,0x4AEC43,0x4B08D3])
        if any(c['case']['slots']):assert all(hex(a) in r['visited_instructions'] for a in [0x4AB3B0,0x424B40,0x4ACDA8])
    controls=load('phase122-room-writer-crosschecks.json');assert len(controls['replays'])==2
    for case,row in zip(cross.CONTROLS,controls['replays']):
        args,count,size=cross.build(case);saved=load(row['file'])
        assert saved['input']==args and row['sha256']==native.sha((ROOT/row['file']).read_bytes())
        assert row['checked_regions']==count and row['checked_bytes']==size
        r=saved['result'];assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    return manifest,controls

def build():
    manifest,controls=verify_native();attest=primary.build();assert load('phase122-primary-manifest.json')==attest
    corpus=load('phase122-room-api-validation.json');assert len(corpus['files'])==82 and corpus['exact_container_rebuilds']==246
    for r in corpus['files']:assert native.sha(Path(r['file']).read_bytes())==r['sha256']
    sources={r['file']:r['sha256'] for r in source_executables()+load('editor-variant-semantics.json')['profiles']}
    for p,h in sources.items():assert native.sha(Path(p).read_bytes())==h
    previous=ROOT/'phase122-prior-components.json'
    if not previous.exists():
        parts=load('editor-format-package.json')['components'];assert len(parts)==100
        previous.write_text(json.dumps(parts,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(previous.name))==100
    layout=json.loads(json.dumps(load('layouts.json')['Rooms']));fields=[]
    for f in layout['fields']:
        if f['offset']==0x36E:
            assert f['name']=='TorchIntensity' and f['size']==2
            fields.append(dict(f,size=1,kind='u8',semantics='Native byte width confirmed in supplied Pro andDX converter/loader/writer;replaces historicalu16 interpretation.'))
            fields.append(dict(name='TorchIntensityOpaqueByte',offset=0x36F,offset_hex='0x36F',size=1,kind='opaque8',
                semantics='Preserve. Not read by selected native field-load paths and not assigned by original Room writer. No global non-use claim.'))
        else:fields.append(f)
    layout['fields']=fields
    return dict(phase=122,date='2026-10-29',family='Rooms complete original serialization and native byte-width correction',
        original_executables_unchanged=[dict(file=p,sha256=h) for p,h in sources.items()],
        primary=link('phase122-primary-manifest.json'),native_executions=manifest['replays'],
        independent_Ghidra_controls=controls['replays'],API_validation=link('phase122-room-api-validation.json'),source_corpus=corpus,
        writer=dict(address=0x4A7AC0,array=0x448,count=0x44C,record_bytes=1055,header_bytes=56,
            header_signature=0x78,header_tags=[0x79,0x7A],record_write_virtual_slot=0x38,
            direct_model_fields=[list(r) for r in api.DIRECT],scaled_model_fields=[list(r) for r in api.SCALED],resource_copies=[list(r) for r in api.RESOURCES],
            string_fields=[dict(raw=0,model=8,max_checked_bytes=32),dict(raw=0x3A2,model=0x154,max_checked_bytes=32)],
            float_conversion='Original __ftol4ACDA8 runs;selected stores keep low32 orlow16 after x4096. API preserves explicit raw bits instead of automatically normalizing unrelated fields.',
            flags=dict(helper=0x424B40,boolean_members=[list(r) for r in api.FLAG_FIELDS],
                other_bits='((modelDWORD4BC &3800h)>>2) supplies raw flagsE00h. Other model4BC bits ignored by this helper.',
                caution='Decompiler incorrectly renders helper as empty void return;original assembly and executed output prove nonempty flag composition.'),
            unassigned_record_bytes=[0x36F],unassigned_byte_stack_origin='EntryESP-0xB1 (raw starts EntryESP-420h;raw36F).',
            slot_holes='Counts/headerS/headerD use vector length includingNULL slots;body skips NULL. Verified diagnostics []/[0]/[1,0,2]/[2,1],not evidence normalloadedcataloguecontainsNULL.'),
        torch_intensity=dict(raw_offset=0x36E,width=1,intermediate=0x442,presence=0x74,definition=0x45E,editor_model=0x4D0,
            original_game_converter=0x56DFD0,original_game_loader=0x4EACC0,original_editor_converter=0x42DF90,
            original_editor_model_callback=0x49FC30,legacy_width=2,
            opaque_neighbor=0x36F,rule='Patch only36E asu8;preserve36F separately. It is unassigned by the selected writer,not a confirmed high intensity byte.'),
        effective_room_layout=layout,
        native_date=dict(helper=0x4AB320,relative_bytes=10,
            assigned_fields=[dict(offset=0,width=2,meaning='year'),dict(offset=2,width=1,meaning='day'),
                dict(offset=3,width=1,meaning='month'),dict(offset=6,width=1,meaning='hour'),
                dict(offset=7,width=1,meaning='minute'),dict(offset=8,width=1,meaning='second')],
            unassigned_relative_bytes=[4,5,9],Room_header_date_offset=38,Room_header_unassigned_bytes=[42,43,47],
            fixture='2026-10-29 12:34:56,UTC,noDST. Original time/localtime/gmtime/TLS-wrapper/ftol bodies run;CRT timezone cache flag,TLS andtm buffer prepared.',
            external_Win32_IAT_providers=['GetLocalTime','GetSystemTime','GetLastError','TlsGetValue','SetLastError'],
            code_substitutions=0,scope='Offset meanings are stores performed by shared helper;Roomheader placement is verified here. No allfamily/version timestamp-reader proof.',
            historical_fixture_scope='Earlier Trap/Door writer date-provider substitutions had different assigned-byte coverage. Their46/47unassigned statements were explicitly underthoseproviders,not this original helper;do not combine them into one native timestamp rule.'),
        unresolved_resource=dict(raw=0x1C4,width=84,model_resource=0x280,model_serialized_bytes=0x284,
            meaning='unresolved',confirmed='All84bytes copied by wholewriter under zero/pattern/FF payloads and threestackseeds.',
            search='24exactmodel280operand leads saved inphase122-room-model280-leads.json;freshowner/decomp evidence yielded no additional typed renderer. This finite literal scan is not global exhaustiveness.'),
        Trap_Mana_Gen_review=dict(pro_dialog=255,legacy_dialog=326,hidden_edit=1788,DDX=0x470740,dialog_member=0x5C,
            raw204_model=0x238,observation='DDX_Text1788 binds dialog5C. Selected471930load/471E00commit do not transfer that member to/from model238. Hidden caption by itself does not establish Traps204 meaning.',
            scope='Static selected panel paths;no full GUI event lifecycle orallaliases claim.'),
        helpers=[link(n) for n in ['native_room_writer.py','emulate_room_writer.py','crosscheck_room_writer.py',
            'prepare_room_writer_primary.py','validate_room_writer_api.py']],
        limitations=['Model fixtures prepared;no complete raw→MFC construction→GUI→save lifecycle orliveWindowsCFile execution.',
            'Five Win32 IAT data pointers rebound in isolatedCPU and explicit memory-output stream supplied;no original instruction/date helper/native vtable substitution.',
            'Only initialized UTC/TLS calendar path executed;not CRT bootstrap,DST,timezone transitions orallWindows versions.',
            'TorchIntensity graphics/light behavior not newly executed;new result is physical byte width and selected loaders/writer.',
            'Unassigned bytes follow seeded stack in fixtures,not proof of a live-game failure or all real saves producing nonzero bytes.',
            'Rooms1C4,Traps204 and other significant15family questions remain;no new gameplay name assigned.'])

if __name__=='__main__':
    value=build();(ROOT/'room-writer-semantics.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Room completewriter:17nativecases,2Ghidrachecks;TorchIntensity u8 correction;opaque36Fpreserved.')
