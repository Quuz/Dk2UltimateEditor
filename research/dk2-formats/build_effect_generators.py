"""First-party CB EffectGenerator evidence and scoped data-set observations."""
from pathlib import Path
from collections import Counter
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from build_placed_creatures import whole
from native_effect_generators import record_view,editor_extent
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def primary():
    out=[]
    for row in load('phase37-primary-manifest.json'):
        before=ROOT/'ghidra'/row['before'];after=ROOT/'ghidra'/row['after'];memory=ROOT/'ghidra'/row['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==row['address'] and len(b)==m['length']==row['length'] and b==pe_read(Path(row['exe']).read_bytes(),row['address'],row['length'])
        out.append({**row,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return out

def native_data():
    b=(DATA.parent.parent/'DKII-DX.exe').read_bytes();out=[]
    for va,term in [(0x69EC8C,'TickLastEffectGenerated'),(0x69EC78,'Frequency'),(0x69EC68,'Active'),(0x69CC1C,'Flags'),(0x69EC58,'Width'),(0x69EC48,'Height'),(0x69EB6C,'Id'),(0x69EC38,'Effect')]:
        raw=pe_read(b,va,64).split(b'\0',1)[0];assert term in raw.decode('ascii');out.append(dict(address=hex(va),text=raw.decode('ascii'),hex=raw.hex(),sha256=digest(raw)))
    v=pe_read(b,0x66D814+0x3C,8);assert struct.unpack('<II',v)==(0x4B75A0,0x4B75B0)
    return dict(labels=out,vtable=dict(address=hex(0x66D814+0x3C),bytes=v.hex(),width_getter='0x4B75A0',height_getter='0x4B75B0'))

def corpus():
    paths=[]
    for f in load('corpus-scan.json')['files']:
        if 'sections' in f and any(s['family']=='Things' for s in f['sections']):paths.append((DATA/'maps'/f['file'],f['sha256']))
    p=DATA/'AutoSaveThings.kld'
    if p not in [q for q,_ in paths]:paths.append((p,digest(p.read_bytes())))
    files=[];sizes=Counter();flags=Counter();z=Counter();widths=Counter();heights=Counter();freq=Counter();tails=Counter();ids=Counter();effects=Counter();total=0;holes=0;changes=[]
    for p,sha in paths:
        b=p.read_bytes();assert digest(b)==sha;count=0;map_path=p.with_name(p.name.replace('Things.kld','Map.kld'));dimensions=None
        if map_path.exists():
            map_bytes=map_path.read_bytes();map_parts=fmt.parse(map_bytes);maps=[s for s in map_parts if s.signature==100]
            if maps:dimensions=[fmt.u32(maps[0].header,20),fmt.u32(maps[0].header,24)]
        for s in fmt.parse(b):
            if s.signature!=190:continue
            for i,raw in enumerate(s.records):
                if fmt.u32(raw)!=203:continue
                sizes[len(raw)-8]+=1;v=record_view(raw);total+=1;count+=1;flags[v['flags_bits']]+=1;z[v['z_bits']]+=1;widths[v['width']]+=1;heights[v['height']]+=1;freq[v['frequency']]+=1;tails[v['opaque_tail_hex']]+=1;ids[v['id']]+=1;effects.update(v['effects']);holes+=int(0 in v['effects'] and any(v['effects']))
                if dimensions:
                    normalized=[editor_extent(v['x'],dimensions[0],v['width']),editor_extent(v['y'],dimensions[1],v['height'])]
                    if normalized!=[v['width'],v['height']]:changes.append(dict(file=str(p),record_index=i,xy=[v['x'],v['y']],map_dimensions=dimensions,raw_extents=[v['width'],v['height']],editor_extents=normalized))
        files.append(dict(file=str(p),sha256=sha,generators=count,map_file=str(map_path) if dimensions else None,map_dimensions=dimensions))
    result=dict(thing_files=len(files),generators=total,payload_sizes=dict(sizes),raw_flags=dict(sorted(flags.items())),raw_z_bits=dict(sorted(z.items())),widths=dict(sorted(widths.items())),heights=dict(sorted(heights.items())),frequencies=dict(sorted(freq.items())),ids=dict(sorted(ids.items())),effect_slots=dict(sorted(effects.items())),distinct_opaque_tails=len(tails),opaque_tail_counts=dict(tails),generators_with_empty_and_nonempty_slots=holes,editor_extent_projection_changes=changes,files=files)
    return json.loads(json.dumps(result))

def main():
    fields=[dict(offset=o,bytes=n,name=name,meaning=meaning) for o,n,name,meaning in [
        (0,4,'TileX','Unsigned DWORD bits; game position=(raw<<12)+2048 modulo2^32.'),(4,4,'TileY','Same as TileX.'),(8,4,'OpaqueZ','Copied into checksum intermediate; placed game Z=0; editor save writes0.'),
        (12,4,'OpaqueFlags','ORed into intermediate; inspected game callback does not apply it; editor setter is RET4, getter returns0; preserve raw.'),
        (16,2,'Width','Unsigned width in cells; tick adds random(width*4096) to centered X.'),(18,2,'Height','Unsigned height in cells; positive Y extent.'),
        (20,8,'EffectIds','Four unsigned WORD slots. Uniform random slot0..3; a zero slot aborts this attempt without advancing deadline.'),(28,1,'Frequency','BYTE multiplied by ticks-per-second global6F2590 after the create call; next attempt requires unsigned now>deadline.'),
        (29,1,'Id','BYTE stored in runtime44; constructor fixture covers255. No meaning of global ID uniqueness inferred.'),(30,6,'OpaqueTail','Not copied by either converter; inspected editor writer does not assign these six bytes. Keep raw; not guaranteed zero padding.')]]
    result=dict(family='Things EffectGenerator',tag=203,payload_bytes=36,envelope_bytes=8,fields=fields,executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=primary(),native_data=native_data(),evidence=[whole(r['before']) for r in load('phase37-primary-manifest.json')],native_emulations=load('phase37-generator-emulations.json')['cases'],corpus=corpus(),
        parser=dict(game='0x570630',editor='0x430DB0',read_bytes=36,rule='When callback exists both readers request36 bytes regardless of declared payload length. Tests declaring35/37 provide36 backing bytes; no out-of-bounds or truncated real read is executed. Negative I/O status returns0.',intermediate_bytes=76,presence_dwords=10,intermediate_offsets=dict(position=40,flags=52,width=56,height=58,effects=60,frequency=68,id=69,opaque_tail=70),converter='42FB50 ORs raw flags with existing mid34 and leaves last6 intermediate bytes unchanged; actual parsers initialize the76-byte intermediate to zero before conversion.'),
        runtime=dict(callback='0x521380',creator='0x4B81E0',constructor='0x4B7560',initializer='0x4B19A0',tick='0x4B1B60',allocation_bytes=69,offsets=dict(position=26,deadline=46,effects=50,frequency=58,flags=59,active=60,width=64,height=66,id=68),initial='Checksum callback first; centered Q12 X/Y,Z0, raw width/height retained; activeDWORD=1,deadlineDWORD=0,runtime flagsBYTE=0; four effect slots and frequency assigned after successful creation.',attempt='Inactive returns1 without clock. Require unsigned now>deadline, choose slot0..3. If selected WORD==0 return1 unchanged. Add random(width<<12) and random(height<<12) to base X/Y modulo2^32. World virtual20C rejects on nonzero; rejection returns1 without updating deadline. Otherwise random angle0..2047; virtual38C receives effectWORD, ownerWORD6F2548, XYZ and angle. Regardless of creation return value, set deadline=now+frequencyBYTE*globalTPS modulo2^32.',random='5558D0 bound0 logs through634DB0, returns0 without advancing state. Normal branch advances LCG(seed*1664525+1013904223), rotates right13 and returns unsigned remainder by bound; debug logging branch excluded from nonzero fixtures.',interpretation='Width/height are extents starting at the centered placement point, not a symmetric radius. Four slots including zero are sampled, not compacted. Frequency0 still waits for a strictly later tick. First tick0 equals initial deadline0, so does not emit.'),
        editor=dict(callback='0x491F20',constructor='0x41F810',model_bytes=48,offsets=dict(x=4,y=8,id=24,opaque_flags=28,frequency=32,width=34,height=36,effects=38),width_setter='0x41F990',height_setter='0x41F9E0',bounds='Compare signed32(originDWORD+requestedWORD) against zero-extended low16(mapDimensionDWORD). If less, keep requested; otherwise store low16(mapDimension-low16(origin)-1). Ordinary coordinates yield dimension-origin-1; invalid positions can wrap, not clamp0.',flags_setter='0x401A50 RET4',flags_getter='0x41FA70 returns0',writer='0x4947C0 at4950C9..4951BB',save='Writes TileX,TileY,Z0,flags0,width/height,effects,frequency,id. Last6 bytes not assigned in CB loop;6 full writer fixtures with other family counts0 preserve stack seed00/A5/FF in all6 tail bytes. No universal inter-record initialization guarantee inferred.'),
        limitations=['World registration, base entity initialization, allocation, checksum helper and actual visual effect creation are explicit fixtures. The complete real game/rendering is not run.','Tick clock/random/rejection/create callbacks are controlled; real5558D0 is separately tested in5 cases with debug switch0. Position rejection virtual20C meaning is not given a guessed terrain label.','Editor full callback uses actual generator constructor, bounds/effect setters and no-op flags setter. Base constructor, coordinate virtual setters, parent provider and map insertion are fixtures. No GUI panel ranges/control bindings are claimed.','Full native Things writer fixtures validate the CB payload; unrelated count/date/file helpers are substituted. Header counts are not claimed correct in these isolated fixtures. The36-byte header is captured before the final36-byte CB payload.','RawZ,rawFlags and six tail bytes retain unknown original purpose even where an inspected path ignores/discards them. Runtime flagsBYTE is not assumed to be rawFlags lowbyte.','Assembly/live memory and original PE bytes compared after all emulation. Superseded fixture snapshots retained outside final manifest: RNG log store initially addressed EBP-relative rather than absolute; writer capture initially expected1 len36 call instead of header+payload2.','Independent codec preserves every raw field and opaque bit; editor projections require explicit compatibility intent. All patch/roundtrip checks operate in memory, not on game inputs.'])
    (ROOT/'effect-generator-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('Effect generators:',len(result['native_emulations']),'native cases;',result['corpus']['generators'],'records;',result['corpus']['thing_files'],'Things files')
if __name__=='__main__':main()
