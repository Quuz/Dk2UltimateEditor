"""CC Room overlay evidence: terrain selectors, per-type health and writer."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from build_placed_creatures import whole
from native_placed_rooms import record_view,editor_kind
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def primary():
    out=[]
    for r in load('phase39-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==r['address'] and len(b)==m['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return out

def native_data():
    game=(DATA.parent.parent/'DKII-DX.exe').read_bytes();editor=(DATA/'DK2 Editor pro.exe').read_bytes();tables=[]
    for p,va,n in [(game,0x5215CC,28),(editor,0x49217C,16),(editor,0x49218C,29)]:
        b=pe_read(p,va,n);tables.append(dict(address=hex(va),hex=b.hex(),values=list(struct.unpack('<'+str(n//4)+'I',b)) if n%4==0 else list(b),sha256=digest(b)))
    assert tables[0]['values']==[0x5214DE,0x52154E,0x521500,0x52154E,0x521522,0x52154E,0x521544]
    branches=tables[2]['values'];assert {i+12 for i,c in enumerate(branches) if c!=3}=={12,14,33,37,40}
    sources=[]
    for name,sig in [('Terrain.kwd',110),('Rooms.kwd',120)]:
        p=DATA/name;b=p.read_bytes();rows=[]
        for s in fmt.parse(b):
            if s.signature!=sig:continue
            for r in s.records:
                tid=r[0x1D6] if sig==110 else r[0x394]
                if tid in [12,14,33,34,37,40]:rows.append(dict(name=r[:32].split(b'\0',1)[0].decode('ascii'),terrain_id=tid,room_id=None if sig==110 else r[0x392]))
        sources.append(dict(file=str(p),sha256=digest(b),records=rows))
    return dict(dispatch_tables=tables,catalogue_links=sources)

def corpus():
    index={f['file']:f for f in load('corpus-scan.json')['files']};paths=[(DATA/'maps'/f['file'],f['sha256']) for f in index.values() if 'sections' in f and any(s['family']=='Things' for s in f['sections'])];p=DATA/'AutoSaveThings.kld';paths.append((p,digest(p.read_bytes())))
    files=[];sizes=Counter();types=Counter();owners=Counter();health=Counter();directions=Counter();opaque=Counter();flags=Counter();z=Counter();details=[];mismatches=[];outside=[];portalzero=[];total=0
    for p,sha in paths:
        b=p.read_bytes();assert digest(b)==sha;mp=p.with_name(p.name.replace('Things.kld','Map.kld'));ms=None;mb=None;count=0
        if mp.exists():
            mb=mp.read_bytes()
            if mp.name in index:assert digest(mb)==index[mp.name]['sha256']
            ms=next((s for s in fmt.parse(mb) if s.signature==100),None)
        for s in fmt.parse(b):
            if s.signature!=190:continue
            for i,r in enumerate(s.records):
                if fmt.u32(r)!=204:continue
                v=record_view(r);count+=1;total+=1;sizes[v['payload_bytes']]+=1;types[v['terrain_id']]+=1;owners[v['player_id']]+=1;health[(v['terrain_id'],v['health'])]+=1;directions[(v['terrain_id'],v['direction'])]+=1;opaque[v['opaque_byte']]+=1;flags[v['opaque_flags']]+=1;z[v['z_bits']]+=1;row=dict(file=str(p),record_index=i,**v)
                if v['terrain_id'] in [12,40] and v['health']==0:portalzero.append(row)
                if ms:
                    w,h=fmt.u32(ms.header,20),fmt.u32(ms.header,24)
                    if v['x']>=w or v['y']>=h:outside.append(row)
                    else:
                        tile=ms.records[0][4*(v['y']*w+v['x'])];row['map_terrain_id']=tile
                        if tile!=v['terrain_id']:mismatches.append(row)
                details.append(row)
        files.append(dict(file=str(p),sha256=sha,rooms=count,map_file=str(mp) if ms else None,map_sha256=digest(mb) if ms else None))
    return json.loads(json.dumps(dict(thing_files=len(files),room_records=total,payload_sizes=dict(sizes),terrain_ids=dict(types),player_ids=dict(owners),health_by_type={str(t):{str(h):n for (tt,h),n in health.items() if tt==t} for t in types},direction_by_type={str(t):{str(d):n for (tt,d),n in directions.items() if tt==t} for t in types},opaque_bytes=dict(opaque),opaque_flags=dict(flags),raw_z=dict(z),player_zero_records=sum(n for k,n in owners.items() if k==0),records_ignored_by_game_type=sum(n for k,n in types.items() if k not in [12,14,37]),records_ignored_by_editor_type=sum(n for k,n in types.items() if editor_kind(k) is None),editor_terrain_mismatches=mismatches,outside_map=outside,portal_zero_writer_mutations=portalzero,files=files,records=details)))

def main():
    fields=[dict(offset=o,bytes=n,name=name,meaning=meaning) for o,n,name,meaning in [
        (0,4,'TileX','DWORD bits; centered Q12 by game; editor compares original tile coordinates.'),(4,4,'TileY','Same as X.'),(8,4,'OpaqueZ','Copied into intermediate; game uses Z0; new editor heart/portal/gate serializers initialize Z0.'),
        (12,2,'OpaqueFlags','WORD ORed into intermediate24; neither inspected callback consumes it after conversion/checksum. Preserve raw.'),(14,1,'Direction','BYTE: game only applies it for terrain37;0/2/4/6 set orientation0/1/2/3. Others leave existing orientation unchanged but still call refresh. Editor gates33/37 copy full byte.'),
        (15,1,'OpaqueByte','Not copied by converters. New editor heart/portal/gate serializers do not assign it; existing raw model records retain it. Unknown historical purpose.'),(16,2,'InitialHealth','Unsigned WORD, per-type rules below; no universal byte width or100 clamp.'),(18,1,'TerrainIdSelector','Baseline RoomType enum uses Terrain IDs, not catalogue RoomId. Portal12 corresponds to standalone RoomId3; heart14 toRoomId5.'),(19,1,'PlayerId','Game only tests nonzero; it does not assign owner in this callback. Editor looks up a player with this byte, then may override stored gate player to1.')]]
    result=dict(family='Things Room overlays',tag=204,payload_bytes=20,envelope_bytes=8,fields=fields,executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=primary(),native_data=native_data(),evidence=[whole(r['before']) for r in load('phase39-primary-manifest.json')],native_emulations=load('phase39-room-emulations.json')['cases'],corpus=corpus(),
        parser=dict(game='0x5708A0',editor='0x430FA0',intermediate_bytes=44,read='If declared payloadBytes!=20, zero20-byte raw buffer then read declared length; callback path does not cap the length. Bounded API accepts0..20 and zero pads, preserving physical raw length for patches. Oversized reads are not executed.',conversion='Six presenceDWORDs0..14 ->1. XYZ18..20, flagsWORD24 OR prior, directionBYTE26, healthWORD28, terrainBYTE2A, playerBYTE2B. IntermediateBYTE27 remains from seed; rawBYTE0F never copied. Parsers zero-initialize intermediate.'),
        game=dict(callback='0x521420',existing_room_resolver='0x4559B0',meaning='Overlay for an already existing tile/room; callback contains no room creation. Raw PlayerId0 returns1 before checksum/position. Otherwise center XY/Z0, call world virtual200; nonzero proceeds to tile lookup1D4, checksum44, then require tileWORD2&0FFF nonzero and resolve entity via6EE540.',type_branches=dict(portal12='If healthWORD!=100 compute health delta and call4E5300; exactly100 skips helper.',heart14='Pass full healthWORD to4DB700. This helper creates/updates heart objects, not a simple room health field; whole helper side effects are outside fixtures.',gate37='For directions0/2/4/6 set runtimeDWORD9B=0/1/2/3; every otherBYTE preserves old value. All directions then invoke4EF6C0 refresh. Other selectors, including33/34/40, return without applying CC properties.'),health_delta='p=signed32(low32(currentRoomHealthDWORD35*(100-rawHealthWORD))); high=signed64(p * -1374389535)>>32; q=high>>5; delta=low32(q+(q<0)). For ordinary positive values this is the negative truncated percentage reduction; rawWORD>100 can produce positive delta and multiplication can overflow32 bits.',ownership='Player byte is only a gate in this game callback. Do not infer ownership reassignment or a comparison against actual room owner. Health helper receives existing roomWORD1D, not rawPlayer.',ignored='Raw Z/flags affect the44-byte checksum input only after position passes; rawBYTE0F is absent from intermediate. No complete checksum uniqueness or original opaque purpose inferred.'),
        editor=dict(callback='0x492030',registration='0x48B5F0 at48B790',terrain_lookup='0x487270',player_lookup='0x488850',guard='Require position presence then compare rawTerrainId with byte at mapTerrainPlane950[X+Y*mapWidth990]. On mismatch return1 without applying CC data. Raw coordinates/PlayerId must resolve valid storage; no missing-player safety check established.',types=dict(portal=[12,40],heart=[14],gate=[33,37],ignored=[34]),portal='4226B0 creates/finds preallocated20-byte record with XY,Z0,flagsWORD0,direction0,health100,terrain selector,modelPlayerId. If health present: major60==1 keeps100, otherwise copy low8(rawHealthWORD) intoWORD10 without100 clamp.48B380 resolves by XY across all players.',heart='Copy full X/Y to player model2C/30. If health present copy full healthWORD to player34, for every major version.',gate='422C20 adds/finds20-byte record with default health100.48B4D0 resolves by XY; callback copies full directionBYTE0E, forces modelBYTE13=1. Raw healthWORD is ignored.',opaque='Portal/gate add routines do not assign local scratchBYTE0F. Editor callback comparisons exclude precisely this one byte of freshly constructed records; all256 player bytes and other19 record bytes are compared. Preallocated arrays avoid heap growth; no full allocation or map-load/GUI process claimed.'),
        writer=dict(function='0x4947C0 at4951BB..4952DF',heart_serializer='0x4225A0',heart='Emit main heart record only for player modelBYTE1C>=3. Copy XY and healthWORD34; terrain14,player=model1C,Z0,flagsWORD0,direction0. BYTE0F untouched; full writer seeds00/A5/FF persist in that byte in controlled fixtures.',portal='Emit every portal array record as20 raw bytes. If model healthWORD10==0, mutate it to100 before writing. Other fields, including Z,flags,opaqueBYTE0F, pass through unchanged.',gate='Emit every gate array record as20 raw bytes without portal zero-health correction. Raw Z/flags/opaque bytes pass through unchanged.',fixtures='Ten full writer fixtures capture2/3 CC payloads plus verify portal record mutation; other family counts0. Count/date/file helpers are explicit substitutions, real488830/4225A0/422B60/423170 execute. Whole header counts are not validated.'),
        limitations=['136 emulations:18 parser,3 converter,67 full game callback,35 full editor callback with native lookup/add/resolve and preallocated arrays,3 heart serializer,10 full Things writer. Game validity/tile/checksum/rebuild/health helpers are explicit fixtures; actual final health, geometry, room ownership and heart object creation are not simulated.','Direction cardinal names NORTH/EAST/SOUTH/WEST remain baseline labels; native control flow establishes the four raw values and runtime orientation mapping. It does not independently calibrate world north.','Native editor terrain-match is not a game callback check. Game does not compare selector to actual existing room type, so diagnostic APIs must not turn that into an invented native guarantee.','Six corpus selector34 records have no applying branch in inspected editor/game callbacks. This does not prove complete map load/save removes them: other loading phases can populate models from map cells. Independent codec retains every record.','Raw Z/WORD0C/BYTE0F retain unknown original purpose; observed ignored/discarded paths do not justify automatic zeroing. All patch/rebuild work is in memory; original files unchanged.','Phase39 corrected phase38 external provider fixture ABI:488850 consumes4 bytes and4889D0 consumes16, previously net-balanced12/8. All29 body editor cases rerun and package component refreshed; field results unchanged. Supporting functions are included in primary manifest.'])
    (ROOT/'placed-room-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('Placed rooms:',len(result['native_emulations']),'native cases;',result['corpus']['room_records'],'CC records;',result['corpus']['thing_files'],'Things files')
if __name__=='__main__':main()
