"""Publish native Door WORD separation and correct the public research codec."""
from pathlib import Path
import json
import emulate_door_manufacture_split as native
import crosscheck_door_manufacture_split as controls
import prepare_door_manufacture_split_primary as primary
from native_door_field_layout import effective_door_layout
from validate_door_manufacture_split import validate

ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def sha(b):return native.sha(b)
def link(n):return dict(file=n,sha256=sha((ROOT/n).read_bytes()))

def build():
    attested=primary.build();assert attested==load('phase142-door-split-primary.json')
    manifest=load('phase142-door-split-emulations.json');rows=manifest['replays'];assert len(rows)==5
    posts={}
    for name,row in zip(native.cases(),rows):
        args,checks,saved,path=native.execute(name);post,out=native.verify(name,args,checks,saved['result']);posts[name]=post
        assert row['case']==name and row['outcome']==out and row['file']==str(path.relative_to(ROOT)) and row['sha256']==sha(path.read_bytes())
        assert saved['result']['source_exe_sha256']==attested['profile']['sha256']
    for name,value in [('fixed-one',1),('fixed-max',65535),('fixed-upper',0xE7A9)]:
        expected={a:bytearray(b) for a,b in posts['baseline'].items()}
        offsets=[(native.RAW,0x236),(native.MID,0x2AA),(native.DEF,0x99)] if name=='fixed-upper' else [(native.RAW,0x234),(native.MID,0x2A8),(native.DEF,0x95)]
        for a,o in offsets:expected[a][o:o+2]=value.to_bytes(2,'little')
        assert {a:bytes(b) for a,b in expected.items()}==posts[name]
    cross=load('phase142-door-split-crosschecks.json')['replays'];assert len(cross)==3
    for row in cross:
        args,n,size=controls.build(row['case']);saved=load(row['file']);r=saved['result']
        assert saved['input']==args and sha((ROOT/row['file']).read_bytes())==row['sha256']
        assert row['checked_regions']==n and row['checked_bytes']==size
        assert not saved['tool_is_error'] and r['success'] and r['hit_return']
        assert all(int(r['registers'][k],16)==v for k,v in dict(EAX=1,EBP=1,EBX=0,ECX=0).items())
    edits=validate();assert edits==load('phase142-door-split-api-validation.json')
    old=load('effect-generation-scale-semantics.json')['preservation']
    sources={str(Path(r['file'])).lower():r for r in old['source_files']}
    for r in edits['files']:sources[str(Path(r['file'])).lower()]={k:r[k] for k in ['file','sha256']}
    preservation=dict(source_files=list(sources.values()),executable_paths=old['executable_paths'])
    for r in preservation['source_files']+preservation['executable_paths']:assert sha(Path(r['file']).read_bytes())==r['sha256']
    prior=ROOT/'phase142-prior-components.json'
    if not prior.exists():
        components=load('editor-format-package.json')['components'];assert len(components)==120
        prior.write_text(json.dumps(components,ensure_ascii=False,indent=2),encoding='utf8')
    assert len(load(prior.name))==120
    # Native storage/edit separation is earlier evidence, reused explicitly.
    historical=load('door-semantics.json')['manufacture_split']
    assert historical['native_low_word']['raw_offset']==0x234 and historical['native_low_word']['bytes']==2
    assert historical['native_high_word']['raw_offset']==0x236 and historical['native_high_word']['bytes']==2
    assert sha((ROOT/'dk2_formats_phase141.py').read_bytes())=='21ad42c7263fe25f149eddd8052605310aa54297d160913e75c6fd8c87534c01'
    return dict(phase=142,date='2026-10-31',families=['Doors'],profile=attested['profile'],primary=attested,
        corrected_public_field=dict(name='ManufToBuild',raw_offset=0x234,old_bytes=4,current_bytes=2,raw_kind='u16',
            intermediate_offset=0x2A8,definition_offset=0x95,range=[0,65535],
            adjacent=dict(name='Unknown236',raw_offset=0x236,bytes=2,intermediate_offset=0x2AA,definition_offset=0x99,meaning='Unresolved')),
        defect=dict(location='Research dk2_formats.set_scalar and baseline-derived effective Door layout, not the original game/editor or the parallel editor.',
            trigger='Set ManufToBuild on a record with nonzeroUnknown236, or pass a DWORD value above65535.',
            old_behavior='Four-byte encoding overwrites adjacent unknownWORD; input65536 becomes lowWORD0 andupperWORD1.',
            new_behavior='Public decode/edit uses nativeWORD234 andseparateUnknown236. Manufacture edit preserves236; out-of-range values rejected.',
            frozen_reproducer=link('dk2_formats_phase141.py'),source_nonzero_upper_words=edits['source_nonzero_upper_words'],
            source_scope='All656 sourceDoor records have upperWORD0. Clobber proof uses explicit diagnostic copies withA55A; no claim supplied game files were corrupted.'),
        API=dict(current_codec=link('dk2_formats.py'),field_layout_helper=link('native_door_field_layout.py'),
            public_decode='decoded(Section) exposes ManufToBuild:u16 andUnknown236:u16.',
            public_edit="set_scalar(raw,'Doors','ManufToBuild',value) changesonly234..235; explicitUnknown236edit changesonly236..237.",
            standalone_patch='native_door_field_layout.patch_manufacture',
            baseline='layouts.json andfmt.LAYOUTS retain historicalu32 for audit; use currentpublicAPI/effective_catalogue_layouts.Doors for edits.'),
        reused_native_evidence=dict(storage_and_editor_split=historical,links=[link(n) for n in ['door-semantics.json','door-loading-semantics.json','door-writer-semantics.json','manufacture-semantics.json']],
            scope='Earlier editor Pro/RC3 fullwriter and bothgame-profile loader results reused, not counted as new executions. New five currentDX chains validate publicedit outputs.'),
        native_executions=manifest,independent_Ghidra_controls=cross,API_validation=edits,preservation=preservation,effective_door_layout=effective_door_layout(),
        initial_misc_review=link('phase142-effect-misc-initial-review.json'),
        counts=dict(native_raw_chains=5,paired_whole_memory_controls=3,paired_compared_bytes=sum(r['outcome']['compared_bytes'] for r in rows[:3]),
            independent_Ghidra_CPU_controls=3,Ghidra_compared_regions=sum(r['checked_regions'] for r in cross),Ghidra_compared_bytes=sum(r['checked_bytes'] for r in cross),
            executed_function_spans=len(attested['functions']),unique_executed_original_PCs=attested['unique_executed_original_PCs'],
            new_exact_container_edits=82,public_decode_rebuild_controls=82,legacy_clobber_reproductions=82,
            preserved_source_files=len(sources),preserved_executable_paths=len(preservation['executable_paths']),
            newly_corrected_public_field_widths=1,newly_named_opaque_fields=0,source_writes=0),
        helpers=[link(n) for n in ['native_door_field_layout.py','emulate_door_manufacture_split.py','crosscheck_door_manufacture_split.py','prepare_door_manufacture_split_primary.py','validate_door_manufacture_split.py','scan_effect_misc_literal_reads.py']],
        limitations=['Exact suppliedDX only for new five chains; oldergame/Pro/RC3 evidence remains historical, not fresh execution.',
            'Null visualprovider andnonzero audio/checksum gate1. No resource resolution, checksum0 branch or World/workshop queue executed here.',
            'UpperWORD semanticpurpose remainsunknown. Range0..65535 is physicalstorage; it doesnotcertify sensible gameplay at allvalues.',
            'InitialEffectsMisc review checks onlyliteral displacements in18mode entries; adjusted/indexed/helper accesses remainopen. No globalunused claim.',
            'Fix is confined to researchcodec andintegration package. Source game/editor binaries/data andparallel editor unmodified; full15-familygoal remainsincomplete.'])

if __name__=='__main__':
    v=build();(ROOT/'door-manufacture-split-semantics.json').write_text(json.dumps(v,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(v['counts']))
