"""Build native placed Objects rules and exact executable dispatch tables."""
from pathlib import Path
import hashlib,json,struct
from build_effect_semantics import asm_range,pe_read,EXE
from native_placed_objects import *
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def tables():
    b=EXE.read_bytes();rows=[]
    targets=struct.unpack('<17I',pe_read(b,0x520D64,68))
    for spell,target in enumerate(targets,2):
        if target==0x520C90:created=7
        else:
            ins=pe_read(b,target,5);assert ins[:4]==bytes.fromhex('c6442418');created=ins[4]
        assert created==SPELL_OBJECT_IDS.get(spell,7)
        rows.append(dict(spell_dword=spell,target=hex(target),created_id=created))
    jump=struct.unpack('<6I',pe_read(b,0x520DA8,24));selectors=pe_read(b,0x520DC0,134);crates=[]
    for actual in range(256):
        target=jump[selectors[actual-5]] if 5<=actual<=138 else 0x520D57
        expected=0x520D47 if actual in TRAP_CRATE_OBJECT_IDS else 0x520D2C if actual in DOOR_CRATE_OBJECT_IDS else 0x520D57
        assert target==expected
        if target!=0x520D57:crates.append(dict(actual_object_id=actual,target=hex(target),payload_type=1 if target==0x520D47 else 0))
    physical=[]
    for va,n in [(0x520D64,68),(0x520DA8,24),(0x520DC0,134)]:
        raw=pe_read(b,va,n);physical.append(dict(address=hex(va),size=n,hex=raw.hex(),sha256=hashlib.sha256(raw).hexdigest()))
    return dict(spell_remap=rows,crate_cases=crates,physical_tables=physical)

def main():
    result=dict(family='Things',tag=194,payload_bytes=24,executable_sha256=load('executable-hash-verification.json'),
        physical_fields=[dict(name=n,offset=o,bytes=w) for n,o,w in [('x_bits',0,4),('y_bits',4,4),('opaque_position_word',8,4),('spell_bits',12,4),('money_bits',16,4),('trigger_id',20,2),('object_id',22,1),('player_id',23,1)]],
        converter=dict(address=hex(0x56F730),intermediate_bytes=48,
            rule='Copies XYZ DWORDs to18/1C/20, spell DWORD to24, money DWORD to28, trigger WORD to2C, object/player bytes to2E/2F. All six presence DWORDs0..14 are1. Raw widths are preserved here.',
            evidence=[asm_range('phase30-object-converter-before.txt',0x56F730,0x56F781)]),
        game=dict(callback=hex(0x520BB0),checksum_first='Calls world checksum5108C0(mid,48) before skip/remap/create. Stubbed in these emulations; algorithm established separately in catalogue_checksums.',
            skip='Raw ObjectId28 returns1 after checksum without invoking creation. This is not a missing-record error.',
            remap='Only raw ObjectId7 is replaced by hardcoded selector mapping. Lookup compares the full32-bit raw spell value; values outside2..18 and holes keep7. Standalone Objects.kwd names target117..129 as Special powerups, not spell books; baseline KeeperSpellId is an overloaded field. No dynamic lookup by KeeperSpell name.',
            position='Created X/Y=low32((tileBits<<12)+800), Z=0. Raw+8 copied and checksummed but not used for placement position in this callback.',
            player='Creator receives low16 owner identity from [world84+(u8player-1)*1125+4]. Valid-player ranges are not established by this expression.',
            trigger='Raw TriggerId is copied/checksummed, but no read of intermediate2C or forwarding of it occurs in this placement callback. No broader claim about trigger processing elsewhere.',
            creation='4B7A90(world+8,createdIdByte,ownerWord,&position,&outNode). Failure returns0; success returns1 after payload branch. Actual returned instance ID12C and its definition12D determine the crate/flag behavior, rather than blindly reusing raw ObjectId.',
            priority=['flags40 gold','else flags08 spellbook','else flags10 recognized crate','else keep instance payload'],
            flags_source='Raw Objects catalogue344 DWORD -> definition runtimeF4; names Gold/Spell Book/Crate corroborated by earlier native editor type labels.',
            payload='Instance DWORD14A and BYTE14E are a type/value union for book/crate, and amount/value for gold. Book:14A=4,14E=low8(raw spell). Trap crate:14A=1,14E=actualObjectId. Door crate:14A=0,14E=actualObjectId. Unrecognized crate ID leaves preexisting payload unchanged.',
            gold=dict(helper=hex(0x49C760),rule='t=low32(initial instance14A+raw MoneyAmount); instance14A=0 if sign bit(t) else t. Negative mathematical additions can wrap positive; positive sums can wrap negative and be zeroed. No numeric saturation at INT_MAX.',
                initial_scope='Initial instance amount comes from actual creation; fixtures explicitly seed it. This schema does not assume all creators initialize0.',
                visual_scope='For actual IDs1/2 helper additionally updates instance48/4C/58/5C using amount threshold5000 and x87 calculation. Static assembly retained, no visual units inferred or floating branch emulation claimed.',
                evidence=[asm_range('phase30-object-money-setter-assembly.txt',0x49C760,0x49C7F5)]),
            tables=tables(),evidence=[asm_range('phase30-object-callback-before.txt',0x520BB0,0x520D61)]),
        native_editor=dict(load_callback=hex(0x4916B0),writer=hex(0x4947C0),
            definition_lookup='Original4205D0 linearly searches model definitions for BYTE model18 ==definition168. Missing-definition behavior is not made safe by this schema; helper dereferences the result.',
            flags_models={'special_mask4':'3D8','book_mask8':'3DC','crate_mask10':'3E4','gold_mask40':'3EC'},
            spell_load='4206A0: if notGold and (Book or Crate), store low8(input DWORD) at model19; otherwise retain old model19.',
            money_load='420730: if Gold or Special, store input DWORD at model1C; otherwise retain old model1C.',
            remap='4916B0 then checks definition ID7 and calls420610. This returns model19 only if notGold and Book, otherwise AL0. Full EAX is maskedFF before table lookup. Thus raw258 with book flag8 becomes model object117; game leaves7.',
            spell_write='420670 returns zero-extended model19 only if notGold and Book, otherwise0. Assembly explicitly XOR EAX before MOV AL; undefined upper-byte decompiler annotation is not the real writer behavior.',
            money_write='4206F0 returns complete model1C DWORD if Gold or Special, otherwise0. A standalone Crate flag does not retain raw spell DWORD in the writer getter.',
            position_write='Writer explicitly writes raw+8 DWORD0; X/Y come from virtual getters, trigger from low16 getter, object ID from definition168, owner from owner model1C byte.',
            projection_scope='Load projection API assumes all presence flags1 as set by converter and accepts explicit initial model spell/money. Writer getter API uses the definition corresponding to the current model ID. Remapping changes that ID; a combined full load/save needs the target definition flags and catalogue.',
            evidence=[asm_range('phase30-object-spell-setter-assembly.txt',0x4206A0,0x4206E7),asm_range('phase30-object-money-model-setter-assembly.txt',0x420730,0x420766),
                asm_range('phase30-object-spell-crate-getter-assembly.txt',0x420610,0x42063B),asm_range('phase30-object-spell-getter-assembly.txt',0x420670,0x42069D),asm_range('phase30-object-money-getter-assembly.txt',0x4206F0,0x420720),
                asm_range('phase30-object-definition-getter-assembly.txt',0x4205D0,0x420603),asm_range('phase30-object-editor-load-before.txt',0x4916B0,0x491866),asm_range('phase30-things-writer-before.txt',0x494DEA,0x494E73),
                asm_range('phase30-object-editor-flags-before.txt',0x420E9C,0x420EEA)]),
        native_emulations=load('phase30-object-emulations.json')['cases'],
        limitations=['Exact inspected EXEs. No complete level load, real editor GUI save or modified gameplay run.',
            'Full game placement callbacks use explicit checksum/create fixtures. Original49C760 runs for IDs other than1/2, avoiding unverified x87 visual branches. Creation output IDs/flags/initial union fields are controlled inputs.',
            'Full editor load callbacks use explicit creation and catalogue-array fixtures; original vector constructor, definition lookup, setters and selector execute. No model constructor side effects or missing-definition recovery are asserted.',
            'Ten native writer runs execute payload assignment fragment only, after envelope/header. All24 payload bytes are captured and asserted via six DWORDs; actual getter and definition lookup bodies run, owner/XY/trigger providers and file I/O are fixtures.',
            'Converter runs assert physical copy fields; checksum, world construction, triggering and book research are separate paths. Opaque bytes, high spell bytes and signed bit patterns remain raw-preserved by independent patch API.'])
    (ROOT/'placed-object-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Placed Object C2:',len(result['native_emulations']),'native cases; full DWORD game selector vs byte editor selector')
if __name__=='__main__':main()
