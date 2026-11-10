"""Publish virtual instance cap and its nav max_speed/vector consumer binding."""
from pathlib import Path
import json,hashlib,struct
from build_effect_semantics import pe_read
import emulate_navigation_speed as native
import native_navigation_speed as api
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    out={}
    for key,items in load('phase97-primary-manifest.json').items():
        out[key]=[]
        for r in items:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text(encoding='utf8'));b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    exe=Path(load('possession-health-semantics.json')['profile']['executable']).read_bytes()
    assert struct.unpack('<2I',pe_read(exe,0x66d524+0x8c,8))==(0x48d010,0x48d000)
    assert pe_read(exe,0x6a178c,21).split(b'\0')[0]==b'      max_speed %d\n'
    return out

def results():
    rows=load('phase97-navigation-speed-emulations.json')['replays'];by={c['name']:c for c in native.cases()};out=[];assert len(rows)==len(by)==27
    for row in rows:
        path=ROOT/'ghidra'/row['file'];v=json.loads(path.read_text(encoding='utf8'));c=by[row['case']];args,e=native.build(c);r=v['result']
        assert v['case']==c and v['input']==args and v['expected']==e and digest(path.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==x for k,x in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['word11c']).items())
        assert api.instance_cap(e['getter'])=={k:e[k] for k in ['word11c','controller_max_speed']}
        assert api.truncate_vector(c['vector'],e['controller_max_speed'])==e['limited_vector']
        out.append(dict(case=c,expected=e,evidence=row))
    return rows,out

def source():
    d=load('decoded/GlobalVariables.kwd.json');data=Path(d['source']).read_bytes();assert digest(data)==d['sha256']
    row=next(r for r in d['sections'][0]['records'] if r['words_u32'][:2]==[224,21]);raw=bytes.fromhex(row['raw_hex']);assert data[row['offset']:row['offset']+16]==raw
    return dict(file=d['source'],sha256=d['sha256'],record_offset=row['offset'],raw_hex=raw.hex(),words_u32=row['words_u32'])

def main():
    p=primary();rows,out=results();current=source();prior=ROOT/'phase97-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==75;prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    species=load('room-species-bonus-semantics.json')['source_catalogues']['Creatures'];assert species['names']['20']=='Fairy'
    assert digest(Path(species['file']).read_bytes())==species['sha256']
    s=dict(phase=97,date='2026-10-28',profile=load('possession-health-semantics.json')['profile'],primary=p,current_source=current,
      native_replays=rows,outcomes=out,api_helpers=[link(n) for n in ['native_navigation_speed.py','emulate_navigation_speed.py','native_possession_stats.py']],
      instance_storage=dict(virtual_setter='Creature vslot8C→48D010',virtual_getter='vslot90→48D000',word=0x11c,
        rule='Unsigned DWORD request>FFFh becomesFFFh; store low16 in actorWORD11C. Getter zero-extends this WORD.',
        cache='Setter then invokes actual vslot90 and stores result<<4 into actorDWORD206.',
        controller_base=0x124,controller_max_speed_dword=0xe2,alias='actor124h+E2h=actor206h',
        debug_binding='Original4D5C00 reads controllerDWORD_E2 and prints original format string at6A178C: max_speed %d.',
        new_examples=['Getter4098→instance4095→controller65520;Getter4096 also4095.',
          'AtP200/base1365 possessed getter4095 stays4095;1366→4098 is capped.',
          'AtP200/base2730 secondary getter4095 stays4095;2731→4096 is capped.',
          'P8000/base1000 getter42622992 is capped4095;forensic input,not recommended content.']),
      vector_limiter=dict(function='4D4F60',arguments='cdecl(outVector*,inVector*,signed32limit); no callee cleanup',
        length='Take component absolute bits. For magnitudes<4096,integer sqrt(x*x+y*y);else if<400000h,downshift8 then sqrt then upshift8;otherwise shifts18. Squared sum and returned length wrap32.',
        square_root='Original65FAE0 with original32WORD seed table65FB14; integer Newton iteration,not floating sqrt.',
        rule='If signed length<signed limit,copy input unchanged. Else signed ratio=(signed limit<<16)/signed length,truncated towardzero;result components=low32(signed component*ratio>>16).',
        native_examples=['Input(65536,0),instance3000→max_speed48000→output(48000,0).',
          'Input(0,65536)→(0,48000);(-65536,0)→(-48000,0).',
          'Input(65536,65536)→(33944,33944),showing integer/downshift length quantization.',
          'Small(100,100) andzero(0,0) pass unchanged atlimit48000;nonzero vector withlimit0 becomeszero.'],
        exception='Zero vector together withlimit0 would divide0/0 in this helper;not executed. Public model raises,not proof of live navigation failure.'),
      original_call_context=dict(
        movement='Original4D4760 reads controllerDWORD_E2 at4D4B38 and passes it to4D4F60,then stores returnedXY atcontroller8C/90. Complete4D4760 is statically reconstructed,not executed in the new driver.',
        wrapper='4D5A40 gates on controllerE2!=0,prepares movement globals and calls4D4760;static only in this phase.',
        tick='Full488930 reads instance throughvslot90. At489903/489934 it querieshex15=decimal21 and applies(100+P)/100 possessed or/200 secondary beforevslot8C setter.',
        restore='Tick saves the pre-modifier value in local at489623 and at48A45C..48A47A restores it whenactor3BF==0;otherwise retains current value. This temporary lifecycle is static,not a full tick replay.',
        crypt='At4898B0..4898EC a positive signed ownerBYTE108C scales current value by(10+count)*10/100 before the possession modifier. Priorphase94 binds Creature20/Fairy to this counter. This combined modifier path is static,not newly composed natively.'),
      interpretation=dict(
        scope='x3 possessed andx1.5 secondary are the verified P200 getter multipliers. Instance/navigation uses an additional max4095 stage;do not claim an unrestricted x3/x1.5 actual travelled distance.',
        boundary='New driver explicitly composes original getter→real virtualsetter/getter→full native vector limiter with cached limit. It is not a replay of the full Creature tick or4D4760,does not establish that these calls are naturally consecutive in every game path.',
        serialization='No newly found raw maximum-speed field. Clamp4095 is hardcoded in this DX setter. Type224/21 raw patching remains the previously verified DWORD8 API;not a new source change.'),
      counts=dict(native_cases=len(out),native_replays=len(rows),whole_native_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),
        current_raw_records=1,new_raw_API_patches=0,new_actor_images=0,original_game_function_or_vtable_patches=0),
      scope=dict(source_actor=load('possession-health-semantics.json')['scope']['source_actor'],
        fixture='Prepared phase90 target and second/source actor fromphase95;neutral health/Terrain;new definition base speed explicitly set for selected mode.',
        chain='Actualraw224 callback→actual possession setter and neighbour producer→complete48ABB0→original vslots8C/90→complete4D4F60/65FAE0. Existing external providers retained,no original code/vtable/table patches.',
        comparisons='Whole actors+guard,world/map/owner/vars,definition/rawrecord,vector buffers+64guards andother prepared data checked.'),
      limitations=['Exact supplied DX profile only;no new other-version execution.',
        'Explicit native-function composition;full488930tick,4D5A40/4D4760step,collision/steering,position update and renderer not newly executed.',
        'Statictemporaryrestore andCryptcountermodifier not promoted to new live/native-composition coverage.',
        'Quantized vector limiter tested with axes,negative axis,diagonal,small/zero vectors;no all-coordinate/overflow/zero-zero native coverage.',
        'No new raw editable maxspeed setting or source serialization rule derived from the hardcoded setter cap.',
        'Existing getter multipliers remain valid within their original scope;this phase narrows claims about actual movement distance.',
        'Fullpossession/scalar98/113/114,Objects34C,Traps204,Doors236,Terrain198/224,CreaturePriority/Jump and remaining15-format questions keepgoalopen.'])
    (ROOT/'navigation-speed-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']))

if __name__=='__main__':main()
