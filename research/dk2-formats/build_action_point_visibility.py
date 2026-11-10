"""Native consumers of AP reveal10 and toolbox20; keep unknown04/08 open."""
from pathlib import Path
import json,struct
import dk2_formats as fmt
from inventory import DATA
from build_keeper_pipeline import digest
from build_effect_semantics import pe_read,asm_range
from build_placed_creatures import whole
from native_action_points import record_view
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def primary():
    out=[]
    for r in load('phase45-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        v=json.loads(memory.read_text());b=bytes(v['data']);assert len(b)==r['length']==v['length'] and int(v['address'],16)==r['address'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return out

def corpus():
    rows=[];total=reveal=toolbox=reverse=zero=0
    for p in [DATA/'AutoSaveThings.kld',*sorted((DATA/'maps').glob('*Things.kld'))]:
        b=p.read_bytes();sections=fmt.parse(b);rs=[r for s in sections if s.signature==190 for r in s.records if fmt.u32(r,0)==197]
        if not rs:continue
        vs=[record_view(r) for r in rs];total+=len(vs);r=sum(bool(v['flags']&16) for v in vs);t=sum(bool(v['flags']&32) for v in vs);reveal+=r;toolbox+=t
        for v in vs:
            a,y,c,d=[n&65535 for n in v['bounds_bits']];reverse+=int(c<a or d<y);zero+=int(v['id']==0)
        rows.append(dict(file=str(p),sha256=digest(b),records=len(vs),reveal_records=r,toolbox_records=t))
    return dict(files=rows,file_count=len(rows),records=total,reveal_records=reveal,toolbox_records=toolbox,reversed_runtime_bounds=reverse,zero_ids=zero,scope='Counts raw masks of C5 in AutoSaveThings and map Things. No inference of world linked-list order, coordinate validity against an unreferenced map, or executed reveal events.')

def main():
    s=dict(family='Things.ActionPoint',tag=197,raw_flags_offset=18,raw_flags_bytes=2,runtime_flags_offset=50,runtime_bounds_offset=56,executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=primary(),native_emulations=load('phase45-ap-visibility-emulations.json')['cases'],evidence=[asm_range('phase45-ap-50b5b0-disassemble_function.txt',0x50C05D,0x50C16B),whole('phase45-helper-454b80-disassemble_function.txt'),whole('phase45-helper-517480-disassemble_function.txt'),whole('phase45-helper-510e20-disassemble_function.txt')],corpus=corpus(),
        reveal=dict(mask=16,fragment='0x50C05D..0x50C170',world_gate='If worldBYTE7239 &40, skip whole AP reveal loop. Exact meaning of this world flag is not assigned.',
            traversal='Owner AP head worldWORD12, tag table6EE540, AP next tagWORD08. Every node with runtimeBYTE32 &10 is processed. AP ID37 is not used for traversal.',
            bounds='Unsigned WORDx0/y0/x1/y1. Integer comparisons after zero-extension, inclusive x0..x1 and y0..y1. No coordinate/map boundary clipping here. Reversed axes skip tile calls; area event still emitted with width/height low16(end-start+1).',
            tile='Runtime map tiles16 bytes at world9EE, width world9E6. For each tile call real454B80; this is a runtime tile layout, not the4-byte KLD map cell.',
            helper='If tileWORD08 &2000 already set, no change/event/update. Otherwise tileWORD02 low12 indexes room tag; if its definitionBYTE452 is3,4,8, delegate4E8B80. This helper delegation is fixture only, so whole-room propagation not certified.',
            ordinary='Other tiles: OR tileWORD08 with2000 preserving all other bits; submit16-byte event(positionPackedXY,1,0,3) through actual517480; then call451C60(x,y). Position uses global6DA49C tile base and6DA490 width; fixtures set them equal to world map, not a guarantee of all runtime states.',
            area_event='After every matching AP, submit(positionPackedStart,packedWidthHeight,0,6), even for empty/reversed loops and when all cells already revealed. Overlapping AP areas therefore produce per-area events, while tile events happen only on the first successful bit transition.',
            no_unknown_assignment='Tests vary04/08 alone and with10; they do not gate this particular fragment. This does not name04/08 or prove global non-use.'),
        toolbox=dict(mask=32,function='0x510E20',rule='Walk the same world head12/AP nextWORD08/tag table. Return low8 ID37 of LAST matching mask20 node; return0 for none. A later matching ID0 replaces an earlier nonzero ID. Physical file order is not proven equal to this runtime linked-list order.',scope='Consumer ID selection only; no toolbox placement, ownership or rendering simulation.'),
        api=dict(file='native_action_point_visibility.py',runtime_only=True,raw_codec='native_action_points.py patch_raw preserves full FlagsWORD and all other spans'),
        limitations=['36 native cases:30 original reveal-loop fragments with complete per-tile454B80/helper517480 plus6 full toolbox selections. Combined16384-byte result buffer compares all tile bytes and event/update captures; counters checked separately.',
            'World initialisation before50C05D and later code after50C170 excluded. Entry/exit memory-only jumps bypass them; no complete50B5B0 execution claimed.',
            'Event session527150, renderer update451C60 and delegated room reveal4E8B80 are fixtures. Real event forwarding517480 runs. Room fixture records calls and does not simulate how a real room changes visibility.',
            'No renderer, visible fog result, GUI or modified-gameplay run. No list cycles/out-of-map accesses executed; safe projection rejects ordinary ordered out-of-map rectangles.',
            'Unknown AP04/08, C8 gameplay purpose and other remaining catalogue semantics remain unresolved. Candidate getter/overlap scans are leads only and are not decoded fields.',
            'First event capture fixture copied4 instead of16 bytes; its failed snapshot retained as superseded and excluded. Corrected36 cases passed. Original game bytes unaffected.'])
    assert len(s['native_emulations'])==36
    (ROOT/'action-point-visibility-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('AP visibility:36 native;',s['corpus']['records'],'AP; reveal',s['corpus']['reveal_records'])
if __name__=='__main__':main()
