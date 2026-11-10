"""Build native KeeperSpells battle-selection and editor flag preservation evidence."""
from pathlib import Path
import json,struct
from build_effect_semantics import asm_range,pe_read
from build_phase17_semantics import proof
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def main():
    dialogs=load('editor-dialogs.json')['dialogs'];dialog=next(r for r in dialogs if r['resource_path'][1]==258)
    labels=['Create Worker','Attacking','Defensive',"Don't Cast On Creatures"];bits=[]
    for i,(mask,label) in enumerate(zip([2,4,8,16],labels)):
        control=next(c for c in dialog['controls'] if c['id']==1796+i);assert control['title']==label
        model=0x204+4*i;panel=0xB4+4*i
        bits.append(dict(mask=mask,name=label,model_offset=model,panel_offset=panel,control=control,
            ddx_evidence=proof('phase26-keeper-ddx.txt',f'FUN_004e490a(param_1,{hex(1796+i)},(int *)((int)this + {hex(panel)}));')))
    editor=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes()
    constants=[]
    for address,value in [(0x50AC48,1/4096),(0x50AC58,4096.0)]:
        raw=pe_read(editor,address,8);assert struct.unpack('<d',raw)[0]==value
        constants.append(dict(address=address,bytes=raw.hex(),value=value))
    result=dict(family='KeeperSpells',record_variants=[406],raw_flags_offset=0xDC,raw_flags_bytes=4,game_intermediate_offset=0x154,game_presence_offset=0x74,runtime_offset=0x54,
        raw_mana_cost_offset=0x11C,runtime_mana_cost_offset=0x58,executable_sha256=load('executable-hash-verification.json'),
        flags_storage=dict(converter=0x56BAB0,loader=0x4F7CE0,rule='Converter ORs raw DWORD into intermediate+154; initialized intermediate0 gives exact raw bits. Loader copies complete DWORD when presence74 is nonzero. No mask1E in game path.',
            evidence=[asm_range('phase26-keeper-game-converter-assembly.txt',0x56BCC8,0x56BCDC),asm_range('game-keeperspell-load-assembly.txt',0x4F7EB6,0x4F7EC3)]),
        native_editor=dict(converter=0x42B720,callback=0x4A25E0,decoder=0x420270,encoder=0x420230,writer=0x4A99B0,
            model_bools=bits,dialog_resource_path=dialog['resource_path'],dialog_title=dialog['title'],constructor=0x450A10,fill=0x451CC0,ddx=0x450F80,commit=0x451FD0,
            projection='Decoder extracts only bits2/4/8/10 into four BOOL DWORDs. Encoder rebuilds only these four bits. Complete native load/model -> writer flag projection is raw &1E, even without changing a checkbox. Other raw bits need independent preservation.',
            evidence=[asm_range('phase26-keeper-flags-decode-assembly.txt',0x420270,0x4202A9),asm_range('phase26-keeper-flags-encode-assembly.txt',0x420230,0x420265),
                proof('phase26-keeper-editor-mid-init.txt','puVar2 = param_1 + 0x1e;'),proof('phase26-keeper-editor-mid-init.txt','for (iVar1 = 0x65; iVar1 != 0; iVar1 = iVar1 + -1) {'),
                proof('phase26-keeper-editor-converter.txt','uVar4 = *(uint *)(param_1 + 0xdc);'),proof('phase26-keeper-editor-converter.txt','*(uint *)((int)this + 0x154) = *(uint *)((int)this + 0x154) | uVar4;'),
                asm_range('phase26-keeper-editor-model-assembly.txt',0x4A28EE,0x4A28FC),asm_range('phase26-keeper-actual-fill-assembly.txt',0x451E7D,0x451EA7),
                asm_range('phase26-keeper-ddx-assembly.txt',0x4511AE,0x4511F1),asm_range('phase26-keeper-actual-commit-assembly.txt',0x4521C1,0x452204),
                asm_range('phase26-keeper-editor-writer-assembly.txt',0x4A9C0C,0x4A9C16),asm_range('phase26-keeper-editor-writer-assembly.txt',0x4A9CAB,0x4A9CB6)],
            writer_offset='Original writer record starts at ESP+50. Encoder result stored at ESP+12C => raw+DC; no intervening stack displacement in this span.',
            constructor_evidence=proof('phase26-keeper-actual-constructor.txt','FUN_004d9a8b(this,0x102,param_2);')),
        battle_ai=dict(function=0x503060,definition_lookup_world_vtable_offset=0x110,count_world_vtable_offset=0x114,
            caller_gates='Function traverses a creature list and checks category16. Additional owner/room/CTA branch and random/personality gates select whether and which spell-search branch runs. These are not complete AI behavior in the provided API.',
            branch_choice='At50328B a random helper is requested with bound4. Unsigned result <((AI+4B2 >>26)&7) chooses defensive; otherwise attacking. Earlier gate requires random helper result for bound4 >=(AI+4B6 &7). PRNG implementation/distribution is outside this projection.',
            enumeration='Both branches iterate integer i=1..worldCount (signed loop comparisons), query player availability helper4BAFA0(i)==3 and cooldown helper4BE390(i)==0, then lookup definition by i. This does not prove every sparse ID above count is visited.',
            flag_gates='Defensive requires flags&8, attacking flags&4. Both require !(flags&10). Bit2 alone qualifies for neither branch. Unrelated high bits do not block these tests.',
            random_bound='Cost is read as unsigned DWORD. q=(unsigned64(costBits)*D1CC931B)>>43, equivalent to floor(costBits/2499) over u32 domain. Requested bound=q+1 defensive, q+2 attacking.',
            random_source=dict(function=0x5558D0,rule='For nonzero bound: seed=low32(seed*19660D+3C6EF35F); result=ROR32(seed,13)%bound, hence0<=result<bound. AI passes world+15A0 as seed pointer. Logging branch is not modeled; distribution/uniformity is not asserted.',
                evidence=asm_range('phase26-random-helper-assembly.txt',0x555967,0x55598A)),
            threshold='power=(AI DWORD+4B6 >>23)&7; threshold=unsigned32(4-power). A random result succeeds iff unsigned result>=threshold. Power5..7 wraps threshold near FFFFFFFF, rather than clamping to0.',
            defensive='For each eligible definition, selectedId=low8(i) BEFORE random test. Stop at first random success; if none succeed, selectedId remains the LAST eligible ID. No eligible ->0.',
            attacking='Set selectedId=low8(i) ONLY after random success, then stop. No successful candidate ->0.',
            scope='Chosen ID is only a candidate. Later distance/health/status target checks and cast-rule validation can prevent a cast; API does not execute them.',
            target_notes='Defensive subsequent path examines own creatures with squared XY distance <19000 hex, health strictly below half of level-adjusted maximum, and no instance bit200000. Attacking subsequent path uses an opponent group and a distance gate. These are evidence notes, not a complete target API.',
            evidence=[asm_range('phase26-keeper-ai-assembly.txt',0x503219,0x5032DC),asm_range('phase26-keeper-ai-assembly.txt',0x5032DE,0x50335F),
                asm_range('phase26-keeper-ai-assembly.txt',0x503480,0x50351E),asm_range('phase26-keeper-ai-assembly.txt',0x5035F0,0x503687)]),
        opaque_c8=dict(raw_offset=0xC8,raw_bytes=4,meaning='still unresolved',intermediate_offset=0x140,runtime_offset=0x3A,editor_model_offset=0x178,
            editor_rule='Editor callback treats raw as unsigned DWORD and divides by4096 into model double178. Writer multiplies that double by4096, converts via4ACDA8 and stores low32 at rawC8.',
            main_panel='Verified Keeper panel451CC0 uses model180 ->panel78 for Recharge Time, not model178. Similar-looking fill4439A0 belongs to a different catalogue (array getter count+59C), so its model178 read is not proof that Keeper C8 is Recharge Time.',
            unresolved='No new semantic game consumer of runtime3A established. Do not rename C8 or assume zero/unused; preserve its four bytes.',
            scale_constants=constants,evidence=[proof('phase26-keeper-editor-converter.txt','uVar3 = *(undefined4 *)(param_1 + 200);'),
                proof('phase26-keeper-editor-converter.txt','*(undefined4 *)((int)this + 0x140) = uVar3;'),
                asm_range('phase26-keeper-editor-model-assembly.txt',0x4A273B,0x4A2758),asm_range('phase26-keeper-editor-writer-assembly.txt',0x4A9B2A,0x4A9B3B),
                proof('phase26-keeper-actual-451cc0.txt','*(undefined8 *)((int)this + 0x78) = *(undefined8 *)(iVar5 + 0x180);'),
                proof('phase26-keeper-actual-commit.txt','*(undefined8 *)(*(int *)(param_1 + 0xc4) + 0x180) = *(undefined8 *)(param_1 + 0x78);')],
            writer_offset='record baseESP+50; C8 storeESP+118. Separate CC storeESP+11C uses model180.'),
        native_emulations=load('phase26-keeper-ai-emulations.json')['cases'],
        limitations=['Exact inspected EXEs only. No complete AI, GUI or live-game cast/render test.',
            '32 original selection-loop runs use explicit availability/cooldown/catalogue/count/random stubs. Original loop comparisons, flag tests, cost arithmetic and selection stores execute. Caller gates and target search skipped.',
            'Eight game pipeline runs connect original converter OR and loader copy fragments with memory-only adapters; eight editor runs execute original decoder -> encoder. Full writer resource serialization and x87 C8 path were not emulated.',
            'C8 remains semantically unknown. Editor projection losing unknown flags is documented behavior, not recommended automatic normalization for a new editor.'])
    assert len(result['native_emulations'])==48
    (ROOT/'keeper-ai-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('Keeper AI selection, editor mask1E, unresolved C8 storage;48 native runs')
if __name__=='__main__':main()
