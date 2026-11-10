"""Publish complete native Shot creation evidence with explicit fixture boundaries."""
from pathlib import Path
from collections import Counter
import json
import dk2_formats as fmt
from build_keeper_cast import load,digest,ROOT
from build_effect_semantics import pe_read

def primary():
    rows=[]
    for r in load('phase54-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data'])
        assert before.read_text().rstrip()==after.read_text().rstrip()
        assert int(v['address'],16)==r['address'] and v['length']==r['length']==len(b) and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        rows.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b)})
    return rows

def corpus():
    rows=[];counts=Counter();records=0
    for old in load('keeper-pipeline-semantics.json')['corpus']['files']:
        p=Path(old['file']);standalone=p.name=='KeeperSpells.kwd'
        if standalone:p=p.with_name('Shots.kwd')
        b=p.read_bytes()
        if not standalone:assert digest(b)==old['sha256']
        sec=next(s for s in fmt.parse(b) if s.signature==260);assert sec.stride==239
        rows.append(dict(file=str(p),sha256=digest(b),records=len(sec.records)))
        records+=len(sec.records)
        counts.update(r[0xC7] for r in sec.records)
    return dict(files=rows,records=records,process_type_counts={str(k):v for k,v in sorted(counts.items())})

def main():
    s=dict(family='DX native Shot factory, constructor, initializer and Keeper cast composition',executable_sha256=load('executable-hash-verification.json')['Dungeon Keeper 2/DKII-DX.exe'],
        primary_program_bytes=primary(),native_emulations=load('phase54-shot-create-emulations.json')['cases'],corpus=corpus(),
        entry_points=dict(factory='0x4B7CD0',constructor='0x4B7D60',initializer='0x4AAC90',keeper_routes=['0x4BBF90','0x4BC290']),
        allocation_bytes=371,final_instance_vtable='0x66D86C',
        creation='Factory checks ID through world virtual1AC, nonzero global6A4818 and allocation554B30(371). On success runs complete constructor4B7D60 and initializer4AAC90, stores low8 ID at instance124, increments creatorDWORD2E and writes caller output pointer. Failure returns0 without assigning output.',
        definition_binding='Initializer reads instanceBYTE124, obtains definition via global6E5050 worldvirtual1B0 and stores it at instanceDWORD125. The composed Keeper tests use different primary and bonus definition addresses; the final pointer is primary even when upgrade selects bonus lookup. Primary fixture definitionProcess6C=9,bonus=0. No process callback executes here.',
        initial_fields=[dict(definition_offset=0x37,instance_offset=0x13D,source_bytes=4,destination_bytes=4,name='Data1'),dict(definition_offset=0x3B,instance_offset=0x141,source_bytes=4,destination_bytes=4,name='Data2'),dict(definition_offset=0x47,instance_offset=0x40,source_bytes=2,destination_bytes=4,name='Health unsigned zero extension'),dict(definition_offset=0x4D,instance_offset=0x11E,source_bytes=2,destination_bytes=2,name='Copied WORD; no new semantic name assigned')],
        other_assignments='Position copied to F6/FA/FE, orientationWORD copied to102/F4. Native setter4B49D0 writes resourceWORD93 from entityWORD4 and clears bit40 of BYTE92. Range/speed virtual8C receives definitionWORD4B; its implementation and units are outside this test. Several local fields are zeroed; full byte oracle is native_shot_creation.projected_node.',
        keeper_composition='16 complete route→factory→constructor→initializer cases:2routes×2upgrade states×4IDpairs(20/40,21/40,0/40,20/0). The actual entity ID and definition pointer use primary ID. Afterwards native wrapper overwrites Data1/Data2 from selected Keeper tuple and targetWORD147/DWORD14D. With primary21 only routeA adds2000hex to target Z. Both native wrappers still return0 after success.',
        zero_cases='SelectedID0 prevents entering factory. With selected bonus nonzero and primary0 the native factory reaches the explicit existence provider, which rejects0. This proves rejection propagation under that provider; actual-world ID0 lookup behavior is not inferred.',
        native_validation='32 finalv3 cases:12 success factory cases with IDs1/21/255,seeds00/A5,sound0/1;4 failure cases(existence/globalgate/allocation/zero rejection);16 composed Keeper cases. Standalone compares full371Shot,80world,48events,4out-pointer;composed compares371Shot,4352player,17spellstate,122Keeperdefinition,64cast-events,48factory-events,80world. All source-body captures re-read after emulations and matched complete PE ranges.',
        fixtures=[dict(address=a,contract=c) for a,c in [
            ('0x554B30','Allocator:record requested371,return fixture node or0'),('0x507850','Base entity constructor:set WORD4=1234,returnthis'),('0x4B1DF0','Base initialization:RET4'),('0x4B3610','Render constructor:RET'),('0x4B4C30','Ownership:copy owner low16 to28;record zero-extended WORD'),('0x4B5020','Movement:copy12 position bytes to1A'),('0x4B4E70','Base helper:RET4'),('0x4B37D0','Render attachment:RET16'),('0x66D86C+0x8C','Instance virtual target override in emulator memory:record argument,RET4'),('world virtual1AC/1B0','Explicit existence and definition providers'),('graphics virtualA0','Return0,so successful resource branch not run'),('sound virtual20','Record opcode,return13572468;actual sound system not run'),('Keeper helper set','Retain phase53 availability,validation,mana,clock,state provider contracts;remove factory replacement so actual4B7CD0 executes')]],
        api=dict(file='native_shot_creation.py',operation='projected_node',scope='Test oracle for this explicit fixture contract;not a raw-file codec and not a general runtime object builder'),
        raw_editor_rule='Preserve Shots rawData1(98)/Data2(9C)/ProcessType(C7),Keeper primary/bonus IDs and both data tuples independently. Do not silently replace primary ID with bonus ID. Runtime offsets125/13D/141 are not file offsets. Existing shot_parameters describes callback meanings separately.',
        limitations=['Exact DX binary only;no old/RC3 runtime equivalence asserted.','Full native factory/ctor/init execute, but entity base,ownership,movement,render,range,sound and providers are explicit fixtures. This is not a live-game casting test.','Graphics success branch and extra initializer branch for definitionWORD6D!=0 are not exercised. All fixture definitions use6D=0.','Synthetic KeeperID5 avoids CreateImp cost and healing branches. ProcessType9 versus0 distinguishes definitions;no Shot ProcessType callback or tick executes.','C8 purpose,CreatureSpells alternative replacement and other existing unresolved fields remain open.','Debug v1/v2 failures exposed fixture address relocation and upper16 owner-slot capture issues;only corrected v3 results are included. Native owner argument is a WORD;upper slot bits are not an owner value.','Research log recovery and rotation are maintenance only,not format-decoding progress. Original game data and all four EXEs are unchanged.'])
    assert len(s['native_emulations'])==32 and len(s['primary_program_bytes'])==11
    if (ROOT/'native-shot-success-semantics.json').exists():s['phase80_native_pool_and_cast']=load('native-shot-success-semantics.json')
    if (ROOT/'targeted-spell-semantics.json').exists():s['phase82_targeted_cast_reference']=dict(file='targeted-spell-semantics.json',sha256=digest((ROOT/'targeted-spell-semantics.json').read_bytes()))
    (ROOT/'shot-creation-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Shot creation:32cases,11PE ranges,',s['corpus']['records'],'raw Shot records')
if __name__=='__main__':main()
