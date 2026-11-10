"""Publish verified Object loading and false anchored instruction boundaries."""
from pathlib import Path
import json
import struct
from build_effect_semantics import pe_read
from build_trap_readiness import digest
from native_object_loading import PROFILES,CONVERT,RESOURCES,LOAD

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def primary():
    spec=load('phase71-primary-manifest.json')
    functions,blocks=[],[]
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
    boundaries=load('phase71-canonical-boundary-review.json')
    old=(ROOT/'source-binaries/DKII-local13.exe').read_bytes()
    for r in boundaries:
        assert pe_read(old,r['canonical_guess'],len(bytes.fromhex(r['canonical_bytes']))).hex()==r['canonical_bytes']
        assert r['canonical_guess']<r['anchor']<r['canonical_guess']+len(bytes.fromhex(r['canonical_bytes']))
        if 'recovered_switch' in r:
            switch=r['recovered_switch']
            assert struct.unpack('<5I',pe_read(old,switch['table'],20))==tuple(switch['targets'])
            assert pe_read(old,0x497A15,3)==bytes.fromhex('83f804')
    return functions,blocks,boundaries


def main():
    functions,blocks,boundaries=primary()
    emu=load('phase71-object-loading-emulations.json')
    assert len(functions)==15 and sum(r['executed'] for r in functions)==8 and len(blocks)==3
    assert len(emu['cases'])==90 and sum(r['expected_fault'] for r in emu['cases'])==4
    s=dict(phase=71,family='Objects complete init/converter/null-visual-provider loading',profiles=PROFILES,
        sizes=dict(raw=894,intermediate=1022,definition=285),primary_code_extents=functions,primary_data_ranges=blocks,
        native_emulations=emu['cases'],native_execution_scope=emu['scope'],
        initializer='Full bodies clear all1022bytes,including32DWORDpresence slots and3intermediate padding bytes.',
        converter=dict(scalars=[dict(presence=p,raw=a,intermediate=a+128,bytes=n) for p,a,n in CONVERT],
            resources=[dict(presence=p,raw=a,intermediate=a+128,bytes=84) for p,a in RESOURCES],
            strings=[dict(presence=0,raw=0,intermediate=128,bytes=32),dict(presence=8,raw=0x35E,intermediate=0x3DE,bytes=32)],
            strings_rule='Native bounded strncpy32 then force finalNUL;earlyNUL and unterminated32-byte inputs tested.',
            flags='presence7C=1;midDWORD3C4 |= rawDWORD344. Isolated dirty-converter seed preserves earlier bits. Normal-chain initializer clears them.',
            light=dict(presence=0x30,raw=0x314,intermediate=0x394,bytes=24),
            skipped=dict(raw=0x341,intermediate_padding=0x3C1,bytes=3,
                finding='No raw341..343 transfer. Isolated converter retains prior mid3C1..3C3;full initializer clears them.12 full chains vary raw padding with identical full mid/definition per profile/gate.'),
            presence='Full converter sets all32 DWORD presence slots0..7C to1.'),
        loader=dict(scalars=[dict(presence=p,intermediate=a,definition=b,bytes=n) for p,a,b,n in LOAD],
            unknown34C='rawWORD34C→midWORD3CC/presenceDWORD50→definitionWORD106. All16bits preserved;presence0 retains,2/FFFFFFFF apply. Hidden GUI ManaGen label does not establish gameplay purpose.',
            speed=dict(raw=0x338,intermediate=0x3B8,presence=0x44,definition=0x100,bytes=2,
                expression='low=u16(unsignedRawDWORD / unsignedTPS);if low==0 AND raw!=0 then1 else low.',
                fault='Presence44!=0 andTPS0 stops at nativeDIV in both EXEs even rawSpeed0. Four expected fault cases capture exactPC;no successful-return buffer comparisons are claimed for faults.',
                absent='Presence44==0 skipsDIV and retains previousWORD100 evenTPS0.'),
            light=dict(presence=0x30,intermediate=0x394,definition=0xD8,bytes=24,flags_raw=0x324,flags_definition=0xE8,
                color_raw=0x328,color_definition=0xEC,
                expression='Copyall24bytes;then runtimeLightFlagsDWORD |= int(any(mid3A8,mid3A9,mid3AA)).',
                behavior='Any of first3 color bytes setsbit0. Black does not clear preexistingbit0. Fourth copiedbyte32B does not trigger bit0. Other flags retained.',
                gate='Light copy/bit update outside DXglobal6FCDCF visual/checksum/sound gate;not a renderer or light-emission test.')),
        checksum=dict(definition=0xF0,bytes=4,rule='Sum all1022 unsigned intermediate bytes;OLD always,DX onlyglobal6FCDCF==0. Not a raw-file checksum.'),
        sound=dict(presence=8,intermediate=0x3DE,definition=0x10E,
            rule='OLD always whenpresent;DX onlyatgate0. Native bounded-copy32,forcebyte31NUL,call audio vslot1C(prefixGLOBAL\\,name),store returnedDWORD.',
            scope='Audio capture adapter records prefixpointer/name/callcount and returnsA1B2C3D4. No actual resource lookup/playback/error validation.'),
        canonical_boundary_review=boundaries,
        rejected_remaining_anchors='All6 new non-WORD anchored leads left byphase70 start inside true instructions:four drop66prefix on WORD104 reads(no overlap106),one decodes insideJMP,one insideMOV immediate. Confirmed against original function instructions or bounded recovered switch,not linear guesses alone.',
        parser_static=dict(function=0x56D880,initializer_call=0x56D9CE,converter_call=0x56DA54,
            scope='Native parser assembly included for initializer-per-record evidence;filesystem/parser/world callbacks not newly executed.'),
        corpus=load('entity-definition-semantics.json')['corpus'],
        api=dict(file='native_object_loading.py',operations=['intermediate','definition','sound_requested','patch_light_color_bytes','patch_light_flags'],
            policy='Projections require explicit previous buffers/exactEXE profile. Preserve original894 raw bytes for serialization;runtime285-byte definition is not a saving source. Light patches do not normalize other fields.'),
        limitations=[
            '86 successful native scenarios and4 expectedDIV0 stops;no modified game process.8 distinct complete native functions execute;15 code extents and3 data ranges attested separately.',
            'Null visual-resource provider bypasses visual resource success/failure paths. Native full functions execute but this is not a complete renderer/world insertion/GUI session.',
            'Successfulcases compare all894raw/1022mid/285definition/32soundbuffer/40capture bytes. Fault cases confirm stop/error/exactDIV address only;observer never runs.',
            'Six anchored false positives rejected does not establish global non-use of Objects106. Other historical aliases,indexed paths,versions and significant unknown fields remain open.',
            'OLD denotes suppliedSHA,not certified retail1.3. Historical source drift captured inphase47 remains separate;current84-file catalogue corpus matches its own hashes.',
        ])
    (ROOT/'object-loading-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    obj=load('object-property-semantics.json');obj['phase71_loading']=s
    (ROOT/'object-property-semantics.json').write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf8')
    print('Objects loading:86native success+4expectedDIV0;6false anchors rejected;unknown34C purpose open')


if __name__=='__main__':main()
