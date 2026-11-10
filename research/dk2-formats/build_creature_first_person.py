"""Build primary-backed Creatures first-person field semantics."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import pe_read,asm_range
from build_phase17_semantics import proof

ROOT=Path(__file__).resolve().parent
def load(name):return json.loads((ROOT/name).read_text(encoding='utf8'))
def main():
    storage=load('scalar-storage-paths.json')['families']['Creatures'];fields=[]
    names={0x107B:'Sniper Zoom Multiplier',0xDBC:'Waddle Scale',0xDC0:'Oscillate Scale',0xD3D:'Walk Cycle Scale'}
    for offset,label in names.items():
        path=next(p for p in storage['joined_paths'] if int(p['raw_offset'],16)==offset)
        evidence=[proof(storage['converter_file'],p['assignment']) for p in path['converter_evidence']]
        evidence.append(proof(storage['runtime_file'],path['runtime_evidence']['assignment']))
        fields.append(dict(raw_offset=offset,raw_bytes=path['raw_bytes'],baseline_name=path['baseline_field'],name=label,
            raw_divisor=1 if offset==0xD3D else 4096,intermediate_offset=int(path['intermediate_offset'],16),runtime_offset=int(path['runtime_offset'],16),runtime_bytes=path['runtime_bytes'],storage_evidence=evidence))
    table=load('phase25-native-sine-table.json');game=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()
    tablebytes=pe_read(game,table['address'],8192);assert struct.unpack('<2048i',tablebytes)==tuple(table['values'])
    vtables=[]
    for address,target in [(0x66CC94+0xD0,0x43A980),(0x66E3EC+0xE8,0x50DFA0)]:
        raw=pe_read(game,address,4);assert int.from_bytes(raw,'little')==target
        vtables.append(dict(address=address,bytes=raw.hex(),target=target))
    schema=dict(family='Creatures',record_variants=[5449,5537],fields=fields,executable_sha256=load('executable-hash-verification.json'),
        phase_table=table,vtables=vtables,
        motion=dict(function=0x40E910,phase_address=0x6CE968,previous_phase_address=0x6CE96C,time_origin_address=0x6CE970,
            phase_rule='If movement input flag is zero and instance vcall88 returns0: phase=0, origin=now. Otherwise phase=unsigned32(now-origin)//50. Unit of the supplied timestamp is not independently established here.',
            table_rule='Signed DWORD table of2048 entries from original PE; min=-65536,max=65536. Use exact table values, not recomputed floating sine.',
            index_rule='p=low32(runtimeCycle*phase); W=((low32(p<<7)>>2)&2047); O=((low32(p<<8)>>2)&2047). All SHR logical.',
            input_waddle='trunc_zero(trunc_zero(s32(low32(table[W]*rawWaddle))/512)/4096); add modulo2^32 to local input DWORD.',
            action80_waddle='trunc_zero(trunc_zero(s32(low32(table[W]*rawWaddle))/4096)/4096); first DWORD of action80, next two DWORDs0, followed by action type and player tag.',
            oscillate='trunc_zero(trunc_zero(s32(low32(table[O]*rawOscillate))/128)/4096). If local half flag!=0: trunc_zero(result/2). Add modulo2^32 to local DWORD.',
            overflow='Signed32 low product is used before division. No game clamp to native editor0..1 or cycle1..12 in these fragments.',
            gates='The input-waddle addition requires changed phase, vcall88==0 and nonzero Waddle. The complete function has additional creature status, input and terrain conditions. These are not modeled as a complete input controller.',
            scope='Exact arithmetic and action80 construction only. Local coordinate interpretation and camera action80 handler/rendering are not newly established.',
            phase_evidence=asm_range('phase25-first-person-input-assembly.txt',0x40EBD9,0x40EC44),
            input_evidence=asm_range('phase25-first-person-input-assembly.txt',0x40EC46,0x40EC95),
            oscillate_evidence=[asm_range('phase25-first-person-input-assembly.txt',0x40F12D,0x40F187),asm_range('phase25-first-person-input-assembly.txt',0x40F1B1,0x40F206)],
            action80_evidence=asm_range('phase25-first-person-input-assembly.txt',0x40F25F,0x40F2CF)),
        sniper=dict(function=0x40FDF0,raw_offset=0x107B,runtime_offset=0x79B,
            integer_factor='unsigned32(raw)>>12; fractional Q12 bits discarded.',
            multiplier_gate='integerFactor>1, equivalent to raw>=8192. Applies before both activate AND deactivate branches.',
            slots='Examine indices0,1,2 in order. Getter returns runtime+5E1+19*i only if i<runtimeByte789, otherwise initialized zero slot. First availableLevel<=instanceByte403 and nonzero spellId wins. No eligible slot -> no commands, even if SniperMode was already set.',
            raw_slot_layout=load('creature-subrecords.json')['spell_array'],
            lookup='Calls world vtableE8 with selected spellId; return value is ignored. This body does not require a successful spell-definition lookup.',
            camera_base='Native bridge vtableD0 returns bridge+3E5 (camera); base DWORD is camera+1D8, i.e. bridge+5BD. It is inside camera configuration mode2 at subrecord+80 according to the source layout. Its physical unit is not established by this result.',
            active_bit=dict(instance_offset=0x3E6,mask=0x80000000,debug_label='SniperMode',evidence=proof('phase24-creature-state-dump.txt','s_SniperMode_')),
            activate='ActionE=(low32(cameraBase*integerFactor),300,0,tag); then Action27=(1,0,0,tag).',
            deactivate='ActionE=(cameraBase,300,0,tag); then Action27=(0,0,0,tag).',
            packet='Three payload DWORDs at0/4/8; type DWORD+C; player tag WORD+10. Actions are queued; no camera-rendering effect or time unit for300 is claimed here.',
            flag_handler=dict(function=0x5171D0,rule='If player+134 possessed tag !=0: state=(state &7FFFFFFF)|((actionValue &1)<<31). Otherwise unchanged. All other bits preserved.',evidence=asm_range('phase25-sniper-action-assembly.txt',0x5171D0,0x517217),
                registration=dict(initializer=0x511A80,record_address=0x6A5A1C,type_offset=0,handler_offset=4,
                    evidence=[asm_range('phase25-action-registration-assembly.txt',0x51248D,0x512492),asm_range('phase25-action-registration-assembly.txt',0x51252E,0x51252E)],
                    scope='Original initializer associates type27 with handler5171D0 in one action record; complete dispatch lifecycle is outside this projection.')),
            gate_evidence=asm_range('phase25-sniper-zoom-assembly.txt',0x40FE38,0x40FE81),
            camera_evidence=[asm_range('phase25-sniper-zoom-assembly.txt',0x40FE96,0x40FED7),asm_range('phase25-camera-getter-assembly.txt',0x43A980,0x43A986)],
            command_evidence=asm_range('phase25-sniper-zoom-assembly.txt',0x40FED9,0x40FF93),
            getter_evidence=asm_range('phase25-creature-slot-getter-assembly.txt',0x4943F0,0x494461)),
        native_editor='Previously documented Q12 display/writer for SniperZoomMultiplier; Waddle/Oscillate values>1 reset0 on native model callback, WalkCycle>12 resets4. Independent serialization should preserve original raw values, not silently run the editor projection.',
        native_emulations=load('phase25-first-person-emulations.json')['cases'],
        limitations=['Exact inspected executable hashes; no live-game input, camera rendering or complete GUI run.',
            'Motion emulations enter arithmetic fragments after caller gates. Sniper emulations skip caller/player resolution and SEH prologue, then run original threshold/slot selection, getter, empty world lookup and camera getter; queued packets are captured in emulator memory.',
            'The supplied native slot-getter initialization flag is1 and its fallback is zero, matching the initialized state; first-time initialization/atexit registration is outside the emulation.',
            'No pixel zoom ratio, FOV units, actionE duration unit or full first-person control behavior is inferred from the packet producer alone.'])
    assert len(schema['native_emulations'])==65
    (ROOT/'creature-first-person-semantics.json').write_text(json.dumps(schema,ensure_ascii=False,indent=2),encoding='utf8')
    print('First-person arithmetic and Sniper command producer;65 original-code runs')
if __name__=='__main__':main()
