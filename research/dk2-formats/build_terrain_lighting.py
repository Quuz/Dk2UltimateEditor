"""Publish Terrain RGB bit storage and separately scoped native projections."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from inventory import DATA
from build_keeper_pipeline import digest
from build_effect_semantics import pe_read,asm_range
from build_placed_creatures import whole
from native_terrain_lighting import GROUPS,DIRECT_MASK,ALL_ENCODABLE_MASK,view
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def primary():
    rows=[]
    for r in load('phase48-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];m=ROOT/'ghidra'/r['memory'];assert p.read_text().rstrip()==a.read_text().rstrip()
        v=json.loads(m.read_text());b=bytes(v['data']);assert len(b)==v['length']==r['length'] and int(v['address'],16)==r['address'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        rows.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(b)})
    return rows

def corpus():
    rows=[];records=0;colour_records=0;different=0;projection_changes=0;flag_counts=Counter();examples=[];channel_counts=Counter()
    for p in [DATA/'Terrain.kwd',DATA/'AutoSaveGlobals.kld',*sorted((DATA/'maps').glob('*.kld'))]:
        if p.name in {'LinchpinVariables.kld','XXLevel16Variables.kld','XXlevel4Variables.kld','XXLevel6bVariables.kld'}:continue
        b=p.read_bytes();rs=[r for s in fmt.parse(b) if s.signature==110 for r in s.records]
        if not rs:continue
        assert all(len(r)==552 for r in rs);records+=len(rs)
        for r in rs:
            v=view(r);f=v['flags'];flag_counts[f]+=1;diff=False;has=False
            for name,g in GROUPS.items():
                for i,mask in enumerate(g['masks']):
                    if f&mask:channel_counts[f'{name}.{"RGB"[i]}']+=1;has=True
                diff |= v[name]['editor_values']!=v[name]['game_loaded_words']
            colour_records+=int(has);different+=int(diff);projection_changes+=int(f!=v['native_decode_encode_flags'])
            if p.name=='Terrain.kwd' and (has or f!=v['native_decode_encode_flags']):examples.append(dict(name=fmt.string(r[:32]),**v))
        rows.append(dict(file=str(p),sha256=digest(b),records=len(rs)))
    return dict(files=rows,file_count=len(rows),records=records,records_with_extension_bits=colour_records,records_with_editor_vs_game_difference=different,records_changed_by_decode_encode_flag_projection=projection_changes,extension_bits_by_channel=dict(channel_counts),flags_counts={hex(k):v for k,v in sorted(flag_counts.items())},standalone_examples=examples,scope='Standalone Terrain, AutoSaveGlobals and parseable map KLD Terrain sections. Duplicate catalogues counted per source. Observed values do not establish renderer output or historical writer.')

def main():
    ps=primary();native=load('phase48-terrain-lighting-emulations.json')['cases'];assert len(native)==99
    evidence=[whole(r['before']) for r in ps if r['address'] in [0x420320,0x420370,0x4203C0,0x420350,0x4203A0,0x4203F0,0x420470,0x420490,0x4204B0,0x425640,0x4254C0]]
    evidence.append(asm_range('phase48-game-terrain-disassemble_function.txt',0x4F69A0,0x4F6AB0))
    s=dict(family='Terrain.lighting',record_bytes=552,flags_raw_offset=0x190,flags_bytes=4,byte_order='little',executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=ps,evidence=evidence,native_emulations=native,corpus=corpus(),groups=GROUPS,
        physical_storage='Six RGB BYTEs, not WORDs. Each has one independent extension bit in raw Flags DWORD190. Overall group enable is a different flag. All offsets here are hexadecimal in prose, integer offsets in JSON.',
        editor=dict(range=[0,511],rule='GUI value=rawByte+256*extensionBit. Setter420320/420370/4203C0 takes low16 of argument, stores low8, sets internal800/1000/2000 iff unsigned low16>255. Getter420350/4203A0/4203F0 returns meaningful low16; upper EAX bits unspecified.',
            primitive_boundaries='Input512 gives byte0/extension1/getter256;65535 gives byte255/extension1/getter511;DWORD65536 wraps to low16=0/extension0/getter0. UI DDV rejects values above511; primitive behavior is a separate scope.',
            ui=dict(dialogs=[143,320],terrain_checkbox=2040,terrain_rgb=[1054,1055,1056],ambient_checkbox=2041,ambient_rgb=[2043,2046,2049],ddx='0x46D3B0:all6 DDV limits0..1FF',fill='0x46DEB0:model2E0/2FC getters to panel8C/90/94 andA4/A8/AC',commit='0x46E410:panel to both color models through6 setters; overall enables88/A0 to model3C8/3CC',scope='UI fill/DDX/commit statically traced, not emulated or run in live GUI. File editor-phase8-terrain-panel-save.txt contains destructor46E350, not commit.'),
            intermediate_to_model='49F5E0:presence-gated mid RGB BYTEs call setters first; later presenceB0 calls425640 on mid248 to restore six extension bits and enable BOOLs.',
            model_to_raw='4A7690 writer statically copies model2F8..2FA to raw1DB..1DD, model314..316 to raw201..203 and calls4254C0 for rawFlags190. Entire writer not emulated in this stage.'),
        flag_projection=dict(decoder='0x425640',encoder='0x4254C0',direct_mask=DIRECT_MASK,all_encodable_mask=ALL_ENCODABLE_MASK,always_discarded_mask=(~ALL_ENCODABLE_MASK)&0xFFFFFFFF,
            rule='Decode then encode:keep direct mask3187FFFF, add terrain extension bits00380000 only if10000000 enabled, ambient0E000000 only if20000000 enabled. Bits00400000/40000000/80000000 are not represented.',
            scope='This projection is exactly decoder followed by encoder without GUI edits. It is not a complete native GUI save roundtrip:commit46E410 also unconditionally clears model384 (raw flag10) and performs other field edits.',
            independent_editor='Keep original DWORD. Patch only chosen channel byte and chosen extension bit; never normalize all flags automatically.'),
        game_loader=dict(function='0x4F66E0',tested_fragment=['0x4F69A0','0x4F6AB6'],intermediate_bytes=736,runtime_definition_bytes=460,intermediate_flags=0x248,intermediate_flags_presence=0xB0,runtime_flags=0x17B,
            rule='When channel presence DWORD!=0, zero-extend mid BYTE into runtime WORD. Then independently of channel presence and overall enable add255 modulo65536 iff its extension bit is present in runtimeFlags. Flag presenceB0 optionally replaces runtimeFlags before all channels.',
            adjacent_copy='Between triples presence98 copies mid2DC DWORD to runtime1B5. Meaning remains unresolved; tests hold this presence0.',
            examples=[dict(raw_byte=0,extension=True,editor=256,game=255),dict(raw_byte=255,extension=True,editor=511,game=510),dict(raw_byte=255,extension=False,editor=255,game=255)],
            repeated_partial_update='With channel absent and extension set, each pass adds255 to previous WORD, wrapping65536. Full updates with all channel presences first reset to rawByte, so do not accumulate.',
            scope='Game values are loaded definition WORDs, not final rendered brightness. No automatic byte adjustment to compensate256 vs255.'),
        static_consumers=dict(terrain='441CD0 gets Terrain via452DC0, reads WORD1AF/1B1/1B3 and submits values into light object; conditionally overrides with globals6F29F0/F4. Static only.',ambient='43E440 gates a path on20000000 and narrows WORD1B9/1BB/1BD to output bytes. Static only; renderer, all branches and extension-bit interpretation downstream remain open.',excluded='4FF5C0 similar offset1B9 references creature-level percent table, not Terrain. Model3D0 matches in45A400/45A870/4A1D80/42D570 belong to other classes.'),
        api=dict(file='native_terrain_lighting.py',operations=['view','patch_channel','flags_projection','game_colour_fragment'],preservation='patch_channel accepts UI0..511, writes exactly one color byte and its corresponding flag bit. Leaves enable and all other raw bits/bytes intact.'),
        limitations=['99 native cases:21 complete setter+getter,38 complete decoder+encoder,40 game-loader fragment cases. Complete28-byte color and3D4-byte model compared, captured getter/encoder output checked; all460 game-definition bytes compared.',
            'Memory adapters bypass GUI, model construction, file I/O and resource callbacks. Native full Terrain writer, complete intermediate-to-model callback, full game loader and renderer not executed.',
            'Flag projection is scoped to decode+encode only; full GUI commit has additional mutations including clearing model384/rawflag10.',
            'Terrain raw198/runtime183 and raw224/runtime1B5 purpose remain unknown. Nearby class-offset coincidences cannot supply names.',
            'No game data or EXE writes. Corpus uses unchanged Terrain sources; historical17-input audit uses phase47 snapshots for3 AutoSave drifts rather than claiming all historical inputs unchanged.'])
    if (ROOT/'terrain-catalogue-validation.json').exists():s['phase58_complete_loading']=load('terrain-catalogue-validation.json')
    if (ROOT/'terrain-cache-semantics.json').exists():s['phase74_cache']=load('terrain-cache-semantics.json')
    (ROOT/'terrain-lighting-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Terrain lighting:',len(native),'native;',s['corpus']['file_count'],'files;',s['corpus']['records'],'records;',len(ps),'primary ranges')
if __name__=='__main__':main()
