"""Native trap shot origin/data and post-creation state from saved evidence."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import pe_read,asm_range

ROOT=Path(__file__).resolve().parent
EXE=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe'

def main():
    data=EXE.read_bytes()
    def proof(file,needle):
        text=(ROOT/'ghidra'/file).read_text(encoding='utf8').replace('\r','')
        assert not text.startswith(('Automatic approval','The automatic permission','Error:'))
        line=next(i for i,v in enumerate(text.splitlines(),1) if needle in v)
        return dict(file=file,line=line,excerpt=text.splitlines()[line-1].strip())
    paths=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']['Traps']['joined_paths']
    def path(offset):return next(p for p in paths if p['raw_offset']==hex(offset))
    trig=[]
    for address in (0x6C1488,0x6C1C88):
        raw=pe_read(data,address,2048*4)
        trig.append(dict(address=hex(address),fraction_bits=16,count=2048,
            sha256=hashlib.sha256(raw).hexdigest(),values=list(struct.unpack('<2048i',raw))))
    assert [trig[0]['values'][i] for i in (0,512,1024,1536)]==[0,65536,0,-65536]
    assert [trig[1]['values'][i] for i in (0,512,1024,1536)]==[65536,0,-65536,0]
    result=dict(family='Traps',record_bytes=579,game_sha256=hashlib.sha256(data).hexdigest(),
        creator='0x4a80e0',normal_caller='0x4a7fd0',definition_pointer_instance_offset=0xF7,
        origin=dict(raw_offsets=[0x231,0x235,0x239],intermediate_offsets=[0x2D9,0x2DD,0x2E1],runtime_offsets=[0xC1,0xC5,0xC9],
            raw_type='3 signed DWORDs in4096 units per tile',heading_instance_offset=0xF4,
            heading_units_per_turn=2048,heading_file_field='not part of the Traps catalogue record',
            heading_lookup='The inspected creator directly indexes with a WORD heading; no local mask or bounds check. API models only0..2047.',
            arithmetic='mul16(a,b)=signed32(low32(a*b)) arithmetic-shift-right16. x=posX+mul16(offsetX,cos)-mul16(offsetY,sin); y=posY+mul16(offsetY,cos)+mul16(offsetX,sin); z=posZ+offsetZ. ADD/SUB wrap32 bits.',
            trig_views=dict(sine=trig[0],cosine=trig[1],relation='cosine view starts512 DWORDs after sine view; these overlap in the executable'),
            evidence=asm_range('phase13-trap-shot-create-assembly.txt',0x4A8125,0x4A818E)),
        creation_gate=dict(shot_type_path=path(0x20D),
            behavior='ShotTypeId0 returns0 before shot creation. Failed0x4B7CD0 also returns0 before the inspected post-creation effects.',
            evidence=[asm_range('phase13-trap-shot-create-assembly.txt',0x4A80FE,0x4A8122),
                asm_range('phase13-trap-shot-create-assembly.txt',0x4A81C1,0x4A81DC)]),
        shot_payload=dict(data1_path=path(0x1E0),data2_path=path(0x1E4),
            shot_data1_offset=0x13D,shot_data2_offset=0x141,target_tag_offset=0x147,
            target_position_offsets=[0x131,0x135,0x139],target_setter='0x4ab830',
            note='Target tag low16 and a separately supplied target-position vector are copied. Data1/2 follow creation and target-vector setup; meaning depends on the shot process/damage modes.',
            pressure_target_marker=dict(shot_runtime_offset=0x14D,value=1,
                condition='TriggerType not1 and not3, and target_tag low16!=0; writer sets1, it does not establish the initial value for other paths.'),
            normal_caller_data2_override=dict(trap_type_id=16,shot_runtime_offset=0x141,
                condition='In the nonzero-target path of0x4A7FD0, after successful creator return, Trap type16 overwrites shot Data2 with the target tag.',
                evidence=asm_range('phase13-trap-fire-assembly.txt',0x4A8043,0x4A804C)),
            evidence=[asm_range('phase13-trap-shot-create-assembly.txt',0x4A8209,0x4A825E),
                proof('phase13-shot-target-vector.txt','param_1 + 0x131'),
                proof('phase13-shot-target-vector.txt','param_1 + 0x139')]),
        post_creation=dict(charge_counter_instance_offset=0x10B,charge_counter_value=0,
            recharge_path=path(0x1C8),recharge_deadline_instance_offset=0xFF,
            recharge_rule='deadline=(current_tick+((raw_RechargeTime*ticks_per_second) modulo2**32 >>12)) modulo2**32',
            reveal=dict(raw_flags_offset=0x1EC,mask=2,instance_flags_offset=0x10C,clear_instance_mask=0x10,
                visibility_update='0x4a9f10',note='This clears current instance invisibility; it does not modify the raw catalogue INVISIBLE bit.'),
            threat=dict(threat_path=path(0x1EA),duration_path=path(0x1D0),
                instance_duration_offset=0x10E,instance_duration_bytes=1,instance_threat_offset=0x10F,instance_threat_bytes=4,
                rule='duration_ticks=((raw_ThreatDuration*ticks_per_second) modulo2**32)>>12; duration_byte=duration_ticks&255; threat_value=0 if duration_ticks==0 else unsigned16(raw Threat).',
                tick_observation='Tick decrements a nonzero duration byte. The later threat-emission gate tests only instance Threat!=0 and does not require duration_byte!=0. No clear of Threat on duration exhaustion is present in this inspected tick body.',
                limitation='This does not prove global permanent threat: other state transitions, object removal, aliases or consumers outside the inspected body are not excluded.',
                evidence=[asm_range('phase12-trap-loader-assembly.txt',0x4AAA01,0x4AAA14),
                    asm_range('phase13-trap-shot-create-assembly.txt',0x4A83D3,0x4A8401),
                    asm_range('phase12-trap-tick-assembly.txt',0x4A857F,0x4A858B),
                    asm_range('phase12-trap-tick-assembly.txt',0x4A8CD3,0x4A8D09),
                    proof('phase13-trap-flags-dump.txt','s_ThreatDuration__d'),proof('phase13-trap-flags-dump.txt','s_Threat__d')]),
            evidence=[asm_range('phase13-trap-shot-create-assembly.txt',0x4A83AF,0x4A83CE),
                asm_range('phase13-trap-shot-create-assembly.txt',0x4A8407,0x4A841D),
                proof('game-traps-runtime.txt','param_1 + 0x74) = (uint)')]),
        researched_unknown_flags=dict(masks=[4,8,0x400,0x4000],meaning='unresolved',
            search_files=['phase13-trap-definition-candidates.json','phase13-unknown-flag-candidates.json'],
            warning='Candidates from F7/88 displacements include Doors and other classes. No semantic names assigned from those coincidences; absence in a limited scan is not proof of non-use.'),
        unresolved_fields=['Traps+0x204','Terrain+0x198','Terrain+0x224','Objects+0x34C'],
        limitation='Read-only creator arithmetic and selected post-creation assignments for the exact game EXE; no successful resource/shot creation, full lifecycle, geometry/collision result or experimental gameplay is asserted.')
    (ROOT/'trap-fire-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Trap fire:exact sine/cosine views;3 rotated offsets;shot payload copies;recharge/reveal/threat assignments')

if __name__=='__main__':main()
