"""Native CWorld catalogue placement rules, independently traced in Ghidra."""
from pathlib import Path
import json

ROOT=Path(__file__).resolve().parent

def main():
    layouts=json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))
    specs=[
        # family, raw ID, header callback, record callback, lookup, array, stride, runtime ID, intermediate ID
        ('Terrain','TerrainId',0x51EB30,0x51EB50,0x508DD0,0x15A4,0x1CC,0x1C4,0x28E),
        ('Rooms','RoomId',0x51EE40,0x51EE60,0x50D0B0,0x15B4,0x486,0x44D,0x466),
        ('Creatures','CreatureId',0x51F220,0x51F240,0x50E3A0,0x15C4,0x897,0x770,0x133B),
        ('Doors','DoorId',0x51FF10,0x51FF30,0x50D650,0x15D4,0xB5,0xAD,0x2B8),
        ('Traps','TrapId',0x520040,0x520060,0x50D8C0,0x15E4,0xD1,0xB4,0x2B4),
        ('Objects','ObjectId',0x521630,0x521650,0x50DB20,0x15F4,0x11D,0x113,0x3DA),
        ('KeeperSpells','KeeperSpellId',0x51FCB0,0x51FCD0,0x50DD60,0x1604,0x7A,0x75,0x166),
        ('CreatureSpells','CreatureSpellId',0x51FDE0,0x51FE00,0x50DFA0,0x1614,0x46,0x43,0x136),
        ('Shots','ShotId',0x5217F0,0x521810,0x50E1E0,0x1624,0x76,0x58,0x146)]
    families={}
    for family,id_name,header,callback,lookup,array,stride,runtime,intermediate in specs:
        field=next(f for f in layouts[family]['fields'] if f['name']==id_name)
        assert field['size']==1
        label=family.lower()
        if family=='Terrain':label='terrain'
        evidence=['game-phase7-'+label+'-record-assembly.txt',
                  'game-phase7-'+('trap' if family=='Traps' else label)+'-getter.txt']
        for name in evidence:assert (ROOT/'ghidra'/name).exists(),name
        families[family]=dict(id_field=id_name,raw_id_offset=field['offset'],id_bytes=1,
            header_callback=hex(header),record_callback=hex(callback),lookup=hex(lookup),
            world_array_offset=hex(array),world_capacity_offset=hex(array+4),world_loaded_count_offset=hex(array+8),
            runtime_record_bytes=stride,runtime_id_offset=hex(runtime),intermediate_id_offset=hex(intermediate),
            allocation='sum_header_counts + 1' if family=='Terrain' else 'sum_header_counts',
            placement='direct_id_index' if family=='Terrain' else 'insertion_before_first_greater_or_equal_id',
            lookup_start='id & 255' if family=='Terrain' else 'min((id & 255)-1, loaded_count-1)',
            lookup_early_exit=None if family=='Terrain' else 'walk backward; equality returns slot, smaller key returns null',
            insertion_capacity_guard=family!='Terrain',evidence=evidence)
    families['Creatures'].update(
        placement='compare existing effective key with incoming CreatureId; shift while existing_key >= incoming_id',
        unique_flag_raw_offset=0xEFC,unique_flag_mask=0x08000000,clone_id_raw_offset=0xD3B,
        clone_id_runtime_offset='0x5b3',flags_runtime_offset='0x686',effective_key_helper='0x494360',
        effective_key='CloneCreatureId if Flags & 0x08000000 else CreatureId',
        ordinary_lookup='0x50d390',ordinary_lookup_vtable_offset='0xc0',
        raw_lookup_vtable_offset='0x1c8',
        warning='Insertion compares two different keys for unique creatures. Arbitrary record permutations need not preserve lookup results.')
    # Keep the proof of the direct write independent of decompiler inference.
    alloc=(ROOT/'ghidra/game-phase7-world-allocate-assembly.txt').read_text()
    assert '0051e2e2: INC EDX' in alloc and '0051e324: PUSH 0x1cc' in alloc
    write=(ROOT/'ghidra/game-phase7-terrain-record-assembly.txt').read_text()
    assert '0051eba6: MOV CL,byte ptr [EBX + 0x28e]' in write
    assert '0051ebbf: CALL 0x004f66e0' in write
    ids=(ROOT/'ghidra/game-phase7-creature-id-getter-assembly.txt').read_text()
    assert '0x8000000' in ids and '0x5b3' in ids and '0x770' in ids
    result=dict(program='DKII-DX.exe',program_sha256='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a',
        scope='Nine CWorld catalogues in normal insertion mode. Effects/EffectElements use CBridge and are outside this schema.',
        counting_pass='0x51df90 registers header callbacks; 0x51dd40 calls descriptor/count pass 0x51de50 then allocator 0x51e2c0',
        allocation_function='0x51e2c0',record_registration='0x51e0a0, branch where (world[0x7239] & 0x04) == 0',
        capacity_scope='Sum of header N values visited during the actual count pass; not necessarily the count in one file. No inference of referenced files is made here.',
        update_branch=dict(world_flag_offset='0xa3c7',condition='nonzero',
            behavior='Lookup existing ID and call its runtime loader; no insertion/count increment. Do not infer when this mode is activated.',
            warning='For sorted families lookup can return null; inspected callbacks do not guard the result before calling loader.'),
        raw_storage='File order and complete records remain authoritative; runtime placement does not instruct a serializer to reorder them.',
        families=families)
    if (ROOT/'creature-lookup-review.json').exists():result['phase73_creature_lookup']=json.loads((ROOT/'creature-lookup-review.json').read_text(encoding='utf8'))
    if (ROOT/'creature-definition-semantics.json').exists():result['phase85_complete_definition']=json.loads((ROOT/'creature-definition-semantics.json').read_text(encoding='utf8'))
    (ROOT/'catalogue-loading.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Native catalogue loading:',len(families),'families')

if __name__=='__main__':main()
