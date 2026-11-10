"""Nine native intermediate checksums; exact instruction ranges and gate distinctions."""
from pathlib import Path
import json
from build_effect_semantics import asm_range
from build_phase17_semantics import proof
from native_catalogue_checksums import FAMILIES

ROOT=Path(__file__).resolve().parent
# name, entry, guard start, exit, destination register, source register, assembly file
ROWS=[
 ('Terrain',0x4F66E0,0x4F66F8,0x4F6721,'EBX','EBP','phase22-terrain-checksum-assembly.txt'),
 ('Rooms',0x4EACC0,0x4EACE4,0x4EAD0F,'EDI','EBP','game-phase11-room-loader-assembly.txt'),
 ('Creatures',0x492320,0x49234E,0x492375,'EBX','EBP','game-creature-type-assembly.txt'),
 ('Doors',0x4A7100,0x4A711A,0x4A7143,'EBP','ESI','phase22-doors-checksum-assembly.txt'),
 ('Traps',0x4AA440,0x4AA463,0x4AA483,'EBP','ESI','phase22-traps-checksum-assembly.txt'),
 ('Objects',0x49DD60,0x49DD84,0x49DDAD,'EBX','EBP','phase22-objects-checksum-assembly.txt'),
 ('KeeperSpells',0x4F7CE0,0x4F7D00,0x4F7D20,'EDI','ESI','phase22-keeperspells-checksum-assembly.txt'),
 ('CreatureSpells',0x4F7860,0x4F7880,0x4F78A0,'EDI','ESI','phase22-creaturespells-checksum-assembly.txt'),
 ('Shots',0x4ACDF0,0x4ACE0A,0x4ACE2A,'EDI','ESI','phase22-shots-checksum-assembly.txt'),
]

def main():
    loading=json.loads((ROOT/'catalogue-loading.json').read_text(encoding='utf8'))['families']
    rows=[]
    for family,entry,start,end,dest,source,file in ROWS:
        length,offset=FAMILIES[family];body=asm_range(file,entry,end-1)
        # asm_range end need not be an instruction; inclusive range remains inside checksum branch.
        instructions=body['instructions']
        assert any(f'CMP EAX,{hex(length)} ' in line for line in instructions)
        assert any(f' + {hex(offset)}]' in line for line in instructions)
        assert any('byte ptr' in line for line in instructions)
        accumulator=int(loading[family]['world_loaded_count_offset'],16)+4
        callback_file=f'game-phase7-{family.lower()}-record.txt'
        callback_proof=proof(callback_file,f'param_2[{hex(accumulator//4)}] = param_2[{hex(accumulator//4)}] +')
        rows.append(dict(family=family,loader=entry,intermediate_bytes=length,
            runtime_checksum_offset=offset,runtime_checksum_bytes=4,
            gate=dict(kind='world DWORD',offset=0xA3C7) if family in ['Rooms','Creatures'] else dict(kind='global DWORD',address=0x6FCDCF),
            rule='gate == 0: checksum = sum(unsigned8(intermediate[i])) for every i in [0,length); gate !=0: retain previous checksum',
            family_world_accumulator_offset=accumulator,callback_evidence=callback_proof,
            accumulator_scope='Normal insertion callback adds the resulting per-definition DWORD modulo2^32. Update branch returns before that addition.',
            evidence=body,emulator=dict(entry=entry,start=start,end_exclusive=end,destination_register=dest,source_register=source)))
    data=dict(program='DKII-DX.exe',program_sha256=json.loads((ROOT/'catalogue-loading.json').read_text())['program_sha256'],
        scope='Nine CWorld catalogue definition loaders. Effects and EffectElements are outside this schema.',
        byte_order='little',families=rows,
        input_contract='Exact bytes of the populated native intermediate structure, including presence DWORDs and all bytes in the scanned extent. Not raw KWD bytes and not the final runtime definition.',
        reset='The per-definition accumulator is reset when its gate is zero; supplied previous does not participate in that branch.',
        collision_warning='Byte sums are order-independent: swapping any two bytes leaves the checksum unchanged. No claim of cryptographic hashing or checksum rejection on file load.',
        signed_world_accumulator=dict(function=0x5108C0,world_offset=0x72E7,rule='existing DWORD + sum(signed8(payload bytes)), modulo2^32; zero length leaves existing value unchanged',
            evidence=asm_range('phase22-world-byte-accumulator-assembly.txt',0x5108C0,0x5108EB)),
        terrain_array=dict(raw_offset=0x1AE,item_bytes=2,count=16,intermediate_offset=0x266,editor_model_offset=0x328,
            meaning='unresolved',preservation='Keep all32 raw bytes. The unsigned checksum includes the intermediate array because 0x266..0x285 is inside0x2E0.',
            inspected_xrefs='Direct setter xref is editor loader0x49F5E0; direct getter xref is writer0x4A7690. No UI or gameplay meaning follows from this.',
            evidence=[proof('game-terrain-convert.txt','puVar7 = (undefined2 *)((int)param_1 + 0x266);'),
                proof('game-terrain-convert.txt','pcVar6 = param_2 + 0x1ae;'),
                proof('phase22-terrain-array-intermediate-get.txt','return *(undefined2 *)((int)this + param_1 * 2 + 0x266);'),
                proof('phase22-terrain-array-editor-get.txt','return *(undefined2 *)((int)this + param_1 * 2 + 0x328);'),
                proof('phase22-terrain-array-editor-get-refs.txt','From 004a7972'),
                proof('phase22-terrain-array-editor-refs.txt','From 0049f883'),
                asm_range('editor-phase8-terrain-writer-assembly.txt',0x4A7966,0x4A7981),
                asm_range('editor-phase8-terrain-writer-assembly.txt',0x4A7A68,0x4A7A7B)]),
        rejected_unknown_field_candidate=dict(field='Objects raw+0x34C / runtime+0x106 WORD',candidate=0x4D5C00,
            reason='Candidate uses a navigation DWORD at the same numerical offset, labeled path_last_progress; structure identity and width differ.',
            evidence=[proof('phase22-object106-path-dump.txt','s_path_last_progress__d_'),
                proof('phase22-object106-path-dump.txt','s_Pilot_Navigation__')]),
        native_emulations=json.loads((ROOT/'phase22-checksum-emulations.json').read_text(encoding='utf8'))['cases'],
        limitations=['No complete raw-to-intermediate checksum reconstruction is supplied: constructors, resource expansion, merge flags and byte initialization must be reproduced first.',
            'Unknown field semantics are not inferred from being included in a checksum.',
            'KWD header N/D/S are separately serialized counts and lengths; these native checksums are not extra file-header fields.',
            'The global0x6FCDCF gate is distinct from world+0xA3C7; activation of modes is not inferred.',
            'No modified-game execution or networking/desync behavior was tested.'])
    (ROOT/'catalogue-checksums.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf8')
    print('Catalogue checksum schema:9 families,2 gate locations,1 signed world accumulator,16-word Terrain preservation')

if __name__=='__main__':main()
