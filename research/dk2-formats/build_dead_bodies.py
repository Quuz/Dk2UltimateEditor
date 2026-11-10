"""CA native overrides: Health WORD, Level byte, checksum/placement split."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from build_placed_creatures import whole
from native_dead_bodies import record_view,editor_health
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def primary():
    out=[]
    for r in load('phase38-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==r['address'] and len(b)==m['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return out

def native_data():
    game=(DATA.parent.parent/'DKII-DX.exe').read_bytes();editor=(DATA/'DK2 Editor pro.exe').read_bytes();labels=[]
    for va,want in [(0x69D008,'Level'),(0x69D798,'TypeId'),(0x69D714,'RoomId'),(0x69EBDC,'Relocating Mode'),(0x69EBC4,'InfernoEffects')]:
        b=pe_read(game,va,64).split(b'\0',1)[0];assert want in b.decode('ascii');labels.append(dict(address=hex(va),text=b.decode('ascii'),hex=b.hex(),sha256=digest(b)))
    b=pe_read(editor,0x50EAB0,8);assert struct.unpack('<d',b)[0]==100.0;v=pe_read(game,0x66D904+0x70,4);assert struct.unpack('<I',v)[0]==0x4B8100
    variables=load('variable-runtime-map.json');assert variables['rows']['23']['runtime_address']=='0x6f25a0';assert variables['rows']['19']['runtime_address']=='0x6f2590'
    return dict(labels=labels,editor_legacy_multiplier=dict(address='0x50EAB0',value=100.0,hex=b.hex()),body_vtable_reset=dict(address=hex(0x66D904+0x70),target='0x4B8100',hex=v.hex()),variable_dependency=dict(file='variable-runtime-map.json',sha256=digest((ROOT/'variable-runtime-map.json').read_bytes()),decay_variable=variables['rows']['23'],tick_rate_variable=variables['rows']['19']))

def corpus():
    paths=[]
    for f in load('corpus-scan.json')['files']:
        if 'sections' in f and any(s['family']=='Things' for s in f['sections']):paths.append((DATA/'maps'/f['file'],f['sha256']))
    p=DATA/'AutoSaveThings.kld'
    if p not in [q for q,_ in paths]:paths.append((p,digest(p.read_bytes())))
    sizes=Counter();values=Counter();levels=Counter();types=Counter();z=Counter();files=[];total=0;details=[]
    for p,sha in paths:
        b=p.read_bytes();assert digest(b)==sha;count=0
        for s in fmt.parse(b):
            if s.signature!=190:continue
            for i,r in enumerate(s.records):
                if fmt.u32(r)!=202:continue
                v=record_view(r);count+=1;total+=1;sizes[len(r)-8]+=1;values[v['editor_health_word']]+=1;levels[v['level']]+=1;types[v['creature_id']]+=1;z[v['z_bits']]+=1;details.append(dict(file=str(p),record_index=i,**v,modern_editor_health=editor_health(v['editor_health_word'])))
        files.append(dict(file=str(p),sha256=sha,dead_bodies=count))
    return json.loads(json.dumps(dict(thing_files=len(files),dead_bodies=total,payload_sizes=dict(sizes),raw_health_words=dict(values),raw_levels=dict(levels),creature_ids=dict(types),raw_z_bits=dict(z),files=files,records=details)))

def main():
    schema=dict(family='Things DeadBody',tag=202,payload_bytes=16,envelope_bytes=8,executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=primary(),native_data=native_data(),evidence=[whole(r['before']) for r in load('phase38-primary-manifest.json')],native_emulations=load('phase38-body-emulations.json')['cases'],corpus=corpus(),
        fields=[dict(offset=o,bytes=n,name=name,baseline_alias=alias,meaning=meaning) for o,n,name,alias,meaning in [
            (0,4,'TileX','PosX','DWORD bits; centered Q12 X=(raw<<12)+2048 modulo2^32.'),(4,4,'TileY','PosY','Same transform as X.'),(8,4,'OpaqueZ','PosZ','Copied into32-byte checksum input; game placement and editor writer use Z0.'),
            (12,2,'EditorHealthValue','GoldHeld','Raw unsigned WORD. Editor major!=1 uses min(low8(value),100); major1 divides absolute health by creature-definition Health WORD15EA and multiplies100 before truncation/low8/clamp. Inspected game callback does not read this value after checksum.'),
            (14,1,'CreatureId','CreatureId','BYTE -> runtime128; creator validates through native provider before allocation.'),(15,1,'Level','PlayerId','BYTE -> runtime12D and editor model19; native debug label Level and rendering-scale consumer agree. This byte is not the ownership/link argument.')]],
        parser=dict(game='0x570500',editor='0x430CD0',read_bytes=16,rule='Registered-callback path always reads16 bytes, ignoring envelope length. Tests declared15/17 have full16 backing bytes; no truncated real I/O executed. Negative read status returns0.',intermediate_bytes=32,flags='Four presenceDWORDs0..0C become1. Payload16 copied to intermediate10..1F; full editor converter42FAA0 overwrites every byte regardless of previous seed.'),
        runtime=dict(callback='0x5212D0',creator='0x4B7FC0',constructor='0x4B8050',initializer='0x4B0F20',allocation_bytes=320,offsets=dict(position=26,angle=244,creature_id=296,definition_pointer=297,level=301,decay_counter=64),placement='Checksum receives whole32-byte intermediate first. Then centered XY/Z0, RNG(2048) angle&2047, creatureBYTE and levelBYTE are passed to creator. RawWORD0C is never loaded by this callback. Creator requires nonnull type provider, enabled global6A4818 and allocation; increments world+3E only on success.',initialization='4B0F20 copies level into12D without0->1 or10 clamp. Counter40=u32(global6F25A0 * TPS6F2590). Position and prior position fields are initialized, angle copied toF4/102, runtime state fields cleared. Ownership/link helper4B4C30 receives globalWORD6F2548, independently of rawLevel.',unused_creation_word='World variables pointer84 supplies WORD1129 to creator third init argument. Inspected4B0F20 does not consume that argument; this is not rawWORD0C. Its broader purpose remains unassigned.',scope='18 full callback/create/constructor/init runs compare all320 bytes under declared base/world/render/provider fixtures. No complete corpse physics, ownership lists, visual drawing, expiry/deletion lifecycle or game process is simulated.'),
        level_consumer=dict(function='0x4B12D0',fragment='0x4B1540..0x4B15E3',multiply_helper='0x4363F0',rule='Read runtimeLevel12D. Scale factor=(level+15)*256. For level1 use unsigned definitionWORD626; otherwise signed growthWORD[global6E4154+2D9+2*level] * unsigned definitionWORD626, signed division by100 truncating toward0. Multiply signed32 factor by signed32 adjusted size; arithmetic shift12, keep low32. Result assigned to both renderer DWORD48/4C, DWORD58/5C cleared.',raw_definition_link='Earlier scalar_storage_paths maps Creatures rawFCD DWORD -> mid13BD -> definition626 low16. StatId29 source name UNKNOWN_29; this proves a dead-body rendering consumer, not every creature-scale use.'),
        editor=dict(callback='0x491DE0',definition_lookup='0x41CE20',model_offsets=dict(creature_id=24,level=25,health_percent=26,party=33,dead=88),behavior='Requires position and CreatureId presence flags; searches definitions by ID168, looks up player ID2 through488850 RET4; coordinate/option arguments remain for creature creator4889D0 RET16, sets partyFF and dead58=1. Copies levelBYTE directly, including0/255. When health presence is set: map major60==1 uses legacy conversion; every other major uses low8(rawWORD), then unsigned clamp100.',legacy='Original x87 sequence: unsigned WORD -> exact double stack store; divide by unsigned definition Health WORD15EA; multiply double100.0; ftol4ACDA8 sets rounding mode towardzero and returns signed64. Only lowbyte of result is compared with100 and stored. Definition Health0 / exact extended precision edge cases are not simulated.',writer='Full4947C0 CA loop49501A..4950C3 iterates shared creature model array, writes records with modelBYTE58!=0. Emits X/Y, Z0, zero-extended healthBYTE1A as WORD0C, CreatureId from definition168, levelBYTE19 as raw0F. All16 payload bytes assigned; previous scratch seed00/A5/FF produces identical output.',ownership='Raw0F has no player/owner enum. Model creation uses editor player-position provider kind2; actual world owner lookup is outside this fixture. Do not expose raw0F as an owner control.'),
        limitations=['Legacy fractional conversion: original4ACDA8 in Ghidra emulator produced10 for9.9 despite setting truncation mode. Failed snapshot and debug capture retained; this result is not treated as CPU behavior. Final9 legacy callback cases capture actual ST0 narrowed to double and use explicit integer-return fixture. Modern18 cases and2 missing-presence cases need no floating fixture.','native_dead_bodies.editor_health is exact for modern byte/clamp; major1 requires explicit signed64 legacy_integer. It does not silently approximate the x87 conversion or normalize raw loading.','Game full creation substitutes base entity initialization, world lookup/link/position registration, allocation, checksum, RNG and render helpers. Actual constructor/initializer/reset/vector-zero code runs; all320 bytes compared within that fixture contract.','Full Things writer substitutes unrelated count/date/file getters and virtual XY/enabled providers. Provider ABI was corrected in phase39:488850 RET4 and4889D0 RET16 replace the earlier net-balanced RET12/RET8 fixtures; all29 editor cases rerun with identical field results. Actual48B30/48D30 and41CE20 execute; payload16 is validated, not whole-header counts or full GUI saving.','Seven scale runs execute only the arithmetic/render assignment fragment and real4363F0; the complete body tick physics/effects/timer/deletion branches are not executed.','Initial function-address guess4B1460 was inside another function and failed creation; retained as discovery error, not part of final primary manifest. Writer initial fixture missed virtual enabled getter34; its failed snapshot is retained outside final manifest.','RawWORD0C and rawZ remain authoritative even when this placement ignores them. Do not strip or regenerate these fields automatically. All patches/roundtrips are in memory; no original game files modified.'])
    previous=ROOT/'dead-body-semantics.json'
    if previous.exists():
        saved=json.loads(previous.read_text(encoding='utf8'))
        if 'phase65_camera_consumer' in saved:schema['phase65_camera_consumer']=saved['phase65_camera_consumer']
    (ROOT/'dead-body-semantics.json').write_text(json.dumps(schema,ensure_ascii=False,indent=2),encoding='utf8');print('DeadBody:',len(schema['native_emulations']),'native cases;',schema['corpus']['dead_bodies'],'records;',schema['corpus']['thing_files'],'Things files')
if __name__=='__main__':main()
