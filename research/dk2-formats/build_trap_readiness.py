"""Publish native readiness codes and two type-proved displacement rejections."""
from pathlib import Path
import json
import hashlib
import struct
from build_effect_semantics import pe_read
from native_trap_readiness import PROFILES

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def primary():
    spec = load('phase68-primary-manifest.json')
    functions, blocks = [], []
    for r in spec['functions']:
        before,after,memory = [ROOT/'ghidra'/r[k] for k in ['before','after','memory']]
        v = json.loads(memory.read_text())
        b = bytes(v['data'])
        assert before.read_text().rstrip() == after.read_text().rstrip()
        assert int(v['address'],16) == r['address'] and len(b) == v['length'] == r['length']
        assert b == pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        memory = ROOT/'ghidra'/r['memory']
        v = json.loads(memory.read_text())
        b = bytes(v['data'])
        assert int(v['address'],16) == r['address'] and len(b) == v['length'] == r['length']
        assert b == pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        blocks.append({**r,'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    bylabel = {r['label']:bytes.fromhex(r['hex']) for r in blocks}
    assert int.from_bytes(bylabel['CTrap-vtable'][:4],'little') == 0x67D760
    assert struct.unpack_from('<I',bylabel['CTrap-vtable'],4+0x14)[0] == 0x4A84E0
    assert struct.unpack_from('<I',bylabel['CTrap-vtable'],4+0x4C)[0] == 0x4A9B70
    assert struct.unpack_from('<I',bylabel['CTrap-locator'],12)[0] == 0x69F130
    assert bylabel['CTrap-type'][8:].startswith(b'.?AVCTrap@@\0')
    return functions,blocks


def main():
    functions,blocks = primary()
    emu = load('phase68-trap-readiness-emulations.json')
    assert len(functions) == 10 and sum(r['executed'] for r in functions) == 6 and len(emu['cases']) == 42
    s = dict(phase=68,family='Traps native readiness return codes',profiles=PROFILES,
        primary_program_bytes=functions,primary_data_ranges=blocks,native_emulations=emu['cases'],native_execution_scope=emu['scope'],
        rule_order=[
            'instance BYTE flags has20: return1 before world/definition/owner pointer reads',
            'unsigned instance deadlineDWORD > current tickDWORD: return0',
            'definition FlagsDWORD88 bit100 AND ownerWORD28 != native exempt-ownerWORD AND costDWORD9A!=0 AND signed32(cost)>signed32(ownerDWORD82): return2',
            'Otherwise return3 when instance stateWORD is nonzero;return4 when zero',
        ],
        raw_paths=dict(flags=dict(raw=0x1EC,definition=0x88,mask=0x100),mana_cost=dict(raw=0x1D4,definition=0x9A,bytes=4),
                       unknown=dict(raw=0x204,definition=0x98,bytes=2,scope='Tested runtime values0/1/8000/FFFF do not change this function. Global purpose unresolved.')),
        signed_mana='Cost0 always allowed by helper,including negative mana. Nonzero cost and mana compared signed32;negative costs can pass. No assertion about GUI acceptance or subsequent mana debit/firing.',
        type_proof=dict(native_class='CTrap',vtable=0x66D754,locator=0x67D760,type_descriptor=0x69F130,
            readiness_caller=dict(slot=0x14,function=0x4A84E0,this_to_edi=0x4A84FE,call_this_from_edi=0x4A8C3B,call=0x4A8C3D,target=0x4A7D10,read=0x4A7D58),
            diagnostic_consumer=dict(slot=0x4C,function=0x4A9B70,this_to_esi=0x4A9B8C,definition_load=0x4A9C8C,read=0x4A9C99),
            conclusion='These two DX reads are in native CTrap member paths,not proven DoorWORD99 consumers. They read full DWORD9A,not WORD99.',
            remaining='Other5 DX DWORD9A candidate reads and7 old counterparts retain separate type-review requirements. A CTrap method can query another entity;its local pointer is not automatically this.'),
        corpus=load('trap-writer-semantics.json')['corpus'],
        api=dict(file='native_trap_readiness.py',operation='return_code',policy='Read-only integer readiness code projection. Not a complete firing predictor and not a serializer.'),
        limitations=[
            'Six whole native functions execute without providers or instruction replacements:two readiness gates,two clock getters,two mana helpers. Experimental world/owner data are explicit memory fixtures.',
            'Four more whole DX functions are statically attested caller/diagnostic contexts;they are not counted as executed. Native CTrap RTTI attested in three data ranges.',
            'Native EAX saved before the observer;all512 actor,209 definition,512 owner,8 clock-root,8 clock-frame bytes compared unchanged. Final EAX/EBP belong to observer.',
            'Runtime layout selected by exact SHA. OLD directory name not an independently certified retail version. No gameplay session,full tick,target selection,shot allocation or debit is certified.',
            'No new raw patch API;previous phase60/61 Trap exact-patch proofs retained rather than repeated as new tests.',
        ])
    (ROOT/'trap-readiness-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    trap = load('trap-semantics.json')
    trap['phase68_readiness'] = s
    (ROOT/'trap-semantics.json').write_text(json.dumps(trap,ensure_ascii=False,indent=2),encoding='utf8')
    door = load('door-semantics.json')
    door['phase68_readiness_review'] = dict(file='trap-readiness-semantics.json',sha256=digest((ROOT/'trap-readiness-semantics.json').read_bytes()),
        scope=s['type_proof'],warning='This review does not assign a gameplay meaning to DoorWORD236.')
    (ROOT/'door-semantics.json').write_text(json.dumps(door,ensure_ascii=False,indent=2),encoding='utf8')
    print('Trap readiness:42 native cases,6 executed/10 full PE functions;2 DX displacement leads type-classified')


if __name__=='__main__':
    main()
