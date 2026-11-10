"""Primary-backed KLD Map cells, substrate flags and native writer fidelity."""
from pathlib import Path
import json
from build_effect_semantics import asm_range
from build_phase17_semantics import proof
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def main():
    result=dict(family='Map',section_signature=100,header_bytes=36,cell_bytes=4,cell_order='x fastest; then y; byte index4*(y*width+x)',
        width_offset=20,height_offset=24,data_tag=102,data_size_offset=32,whole_size_offset=8,executable_sha256=load('executable-hash-verification.json'),
        physical_fields=[dict(offset=0,bytes=1,name='TerrainId'),dict(offset=1,bytes=1,name='PlayerId'),dict(offset=2,bytes=1,name='NativeMapFlags',legacy_name='bridge'),dict(offset=3,bytes=1,name='OpaqueFourthByte',meaning='Preserve raw; inspected parsers ignore it')],
        parser=dict(game=0x56D2D0,editor=0x42D1F0,editor_converter=0x42D1B0,
            intermediate='16 bytes: three presence DWORDs1 at0/4/8; terrain byteC, player byteD, flags byteE. ByteF initialized0 and not loaded from cell byte3.',
            game_rule='Parser reads raw byte0/1/2, initializes intermediate DWORD+C to0, ORs raw flags into byteE. Fourth raw byte is skipped by source increment4.',
            editor_rule='Init42D1A0 zeroes intermediate DWORD+C. Converter sets presence0/4/8 to1, assigns terrain/player and ORs flags into byteE. When called without init, old flag bits persist.',
            chunks='Game/editor reader process up to0x4000 cells per I/O chunk, keeping x/y across chunks.',
            evidence=[asm_range('phase28-map-game-parser-assembly.txt',0x56D428,0x56D49D),asm_range('phase28-map-editor-cell-converter-assembly.txt',0x42D1B0,0x42D1DE),
                proof('phase28-map-editor-cell-init.txt','*(undefined4 *)(param_1 + 0xc) = 0;')]),
        game_flag=dict(function=0x51EC60,intermediate_mask=2,runtime_word_offset=8,runtime_mask=0x1000,map_runtime_cell_bytes=16,
            rule='After terrain/room placement, only raw flags bit02 controls runtime cell WORD+8 bit1000. New=(old &EFFF)|(1000 if flags&2 else0); low12 owner bits and other high bits preserved in this fragment.',
            address='grid.data +16*(y*grid.width+x); CMap fields width+C and data+14.',
            interpretation='Native editor writer associates02 with a bridge adjacent to Lava terrain5;01 with Water terrain4 or an ordinary tile default. This establishes substrate encoding for that writer; not every bridge/liquid gameplay path is modeled.',
            scope='Other map callback branches construct terrain and rooms using definitions and ownership; only final bit assignment is emulated here.',
            evidence=asm_range('phase28-map-game-cell-assembly.txt',0x51EDE4,0x51EE30)),
        native_writer=dict(function=0x494470,terrain_array_offset=0x950,owner_array_offset=0x954,width_offset=0x990,height_offset=0x994,count_offset=0x998,
            outer_header='Writes100/4/(4*storedCount+36);101/8/width/height;102/(4*storedCount); then width*height cell DWORDs. Separate storedCount must agree with dimensions for consistent headers.',
            special_bridge_ids=[17,26,36],water_terrain_id=4,lava_terrain_id=5,
            bridge_rule='For bridge IDs17/26/36: examine source indices i+1,i-1,i+width,i-width. Any neighbor4 assigns flag01; afterwards any neighbor5 assigns02, so Lava wins when both occur. No qualifying neighbor performs no flag assignment: previous scratch flag survives, including previous cells/rows or initial stack byte.',
            ordinary_rule='Every terrain ID outside17/26/36 sets flags01; this is not a generic bridge-category enum for all cells.',
            boundary_rule='Reads linear ±1 and ±width without x/y bounds tests in this writer. ±1 crosses row boundaries in packed arrays. Exterior bytes depend on actual surrounding memory; projection accepts explicit guard bytes, not an invented clamp.',
            fourth_byte='Writer writes terrain/player/flags bytes at canonical frameESP+10/+11/+12, but not frameESP+13. Vcall38 passes four bytes from frame+10. Fourth emitted byte therefore retains stack contents in tested ordinary read-only output path.',
            raw_preservation='Native load/save does not preserve original flag byte or fourth byte. Independent editor should retain all4 physical bytes unless a specific edit explicitly changes one; provide native generation as a separate explicit projection.',
            evidence=[asm_range('phase28-map-editor-writer-assembly.txt',0x494470,0x49450E),asm_range('phase28-map-editor-writer-assembly.txt',0x494514,0x494620)]),
        legacy_aliases=dict(bridge='byte2 is flags, not a universal enum. Legacy bridge_values summary remains for compatibility.',unknown='physical fourth byte remains opaque even when ignored by inspected loaders.'),
        native_emulations=load('phase28-map-emulations.json')['cases'],
        limitations=['Exact supplied EXEs. No live-game terrain/room construction, GUI or rendering test.',
            'Eight complete editor-writer runs use explicit packed arrays/exterior guard bytes, seeds for scratch flags/fourth byte and a read-only file capture callback. Entire output including36-byte header is checked with FNV/length and first/last cell DWORDs.',
            'Four complete game-parser runs use a controlled read stub and callback capture; dimensions/chunk/cell code execute, header callback is absent. No world objects are constructed.',
            'Four editor converter runs start with explicit old flag bits; six game flag-fragment runs skip terrain/room setup and read out only after original store.',
            'FNV is a fixture checksum only. Ignored fourth byte and stack-backed native output do not authorize zeroing original raw files or claim no consumer exists in every version.'])
    assert len(result['native_emulations'])==22
    (ROOT/'map-cell-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('Map4 bytes, bridge substrate flags, opaque fourth and full writer/parser;22 native runs')
if __name__=='__main__':main()
