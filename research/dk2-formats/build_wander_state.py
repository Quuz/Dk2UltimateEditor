"""Publish scoped native wandering behavior without naming placement-byte units."""
from pathlib import Path
from collections import Counter
import json,hashlib,struct
from build_effect_semantics import pe_read
from native_wander_state import DIRS,JITTER
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    spec=load('phase75-primary-manifest.json');functions=[];blocks=[]
    for r in spec['functions']:
        before,after,memory=[ROOT/'ghidra'/r[k] for k in ['before','after','memory']];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert before.read_text().rstrip()==after.read_text().rstrip()
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        memory=ROOT/'ghidra'/r['memory'];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        blocks.append({**r,'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    assert list(struct.iter_unpack('<ii',bytes.fromhex(blocks[0]['hex'])))==DIRS
    assert list(struct.iter_unpack('<hh',bytes.fromhex(blocks[1]['hex'])))==JITTER
    assert bytes.fromhex(blocks[2]['hex']).endswith(b'Game\\StateUtils.cpp\0')
    return functions,blocks

def main():
    functions,blocks=primary();emulations=load('phase75-wander-state-emulations.json');assert len(emulations['cases'])==18
    exposure=load('phase75-wander-exposure.json');corpus=load('phase75-wander-corpus.json')
    s=dict(phase=75,family='Placed-creature WanderRadius and separate DX wandering-state limits',
        primary_program_bytes=functions,primary_data_ranges=blocks,native_emulations=emulations['cases'],native_execution_scope=emulations['scope'],
        executable_sha256=exposure['profiles'][0]['sha256'],
        placement_byte=dict(label='Native diagnostic WanderRadius',raw_neutral=0x17,raw_good=0x1D,raw_good_in_C9='PerGoodslot+1D;canonicalpayload2B+32*slot+1D',current_instance=0x418,old_instance=0x417,
            evidence='Earlier phase31/47 converter,placement and diagnostic proofs;not newly executed raw-to-instance chain.',
            units='Unresolved',gameplay_effect='Unresolved globally;paired0/255 cases leave480750 candidate trace,seed,internal vector and return unchanged under blocked-navigation fixtures.'),
        state_helper=dict(entry=0x480750,scope='Direct invocation of real StateUtils helper,not full state-machine dispatch or world simulation.',
            context_fields=dict(creature_pointer=8,world_pointer=12,center_x=0xA7,center_y=0xAB,distance_limit=0xAF,deadline=0xB3),
            timer='Unsigned deadline<=nativeTick returns0 before PRNG or queries. Absolute DWORD comparison,not wrap-aware elapsed-time arithmetic.',
            candidates='Creature signed fixed-point X/Y at1A/1E arithmetic-shift12 to cells;one realPRNG(bound4) chooses start among N/E/S/W;try all4 in cyclic order.',
            distance='For ordinary coordinates,max(abs(candidateX-centerX),abs(centerY-candidateY)) <= unsignedDWORD contextAF. At extreme DWORDs abs/sub wrap;native signed max then unsigned limit compare is retained by oracle.',
            query='Only candidates passing distance call world.vslot208(x,y);any nonzero DWORD rejects candidate. Adapter captures args and returns supplied DWORD.',
            subcell_offsets=JITTER,directions=DIRS,
            move='After zero query result,realPRNG(bound9) chooses one pair. Vector is cell<<12 OR pair;Z0. Actual471890 receives vector,continuation state56hex(decimal86),flags80hex.',
            navigation_fixture='Actual4B35C0 checks unsigned map bounds after arithmetic shift12,then bitmap bit8.32x32 fixture has bit8 set in every cell;all movement requests fail before4D0F60/state changes.',
            observable='Native return0;full context/creature/nav data unchanged;world PRNG state advances. Last candidate vector is captured from expired stack locals after return as internal evidence,not a return parameter.'),
        separation=dict(shared_context='DWORD AF is a limit only in480750. Other real485AB0/485B80 paths copy creature X toAF and Y toB3 after requesting stateBA;do not globally rename this storage asradius/deadline.',
            other_wandering='Static4808E0 chooses a4..7 byte radius for helper480A00 and decreases it on failures. Full4808E0/480A00 behavior not natively executed here. No link toplacement418 established.',
            open='Source/entry paths populating480750 contextAF remain unproved. Nothing here transfers contextAF units to placementWanderRadius.'),
        exposure={**link('phase75-wander-exposure.json'),'profiles':[dict(profile=p['profile'],sha256=p['sha256'],overlap_or_address_candidates=len(p['candidates']),context_AF_write_candidates=len(p['context_AF_writers'])) for p in exposure['profiles']],
            'scope':'New all-linear-PE overlapping-width reads/writes,exactaddressLEA,selected offsetimmediates andStateUtilsAFwriteinventory.24DX/23old candidates are untyped and include stack/pixel/MMX coincidences;function-map names are hints,not structureidentity. No globalnonuse proof.'},
        corpus={**link('phase75-wander-corpus.json'),**corpus},
        api=dict(file='native_wander_state.py',operation='candidates',scope='Read-only fixture projection for candidategeneration under failednavigation. No rawserializer or modifiedgameplay;earlier rawplacement/C9 APIs remain authoritative.'),
        limitations=['18completeDXhelper compositions,5originalexecutedfunctions;4additionalstaticfunctions.9wholePEranges/3datablocks attested after execution.',
            'Native clock,PRNG,471890 and4B35C0 execute. Onlyworld208andmemoryconstructionarefixtures. Navigationbit8rejectsallmoves;successmovement/4D0F60/pathfinder/statechange not tested.',
            'Direct helper entry with prebuilt context/creature/world. No constructor,actual state dispatch,world tick loop or rawparser/load chain newly executed.',
            'Pair0/255 establishes unchanged observables only in testedbranch. Unknown uses throughotherstates,indexed/adjusted/out-parameter/otherbinary paths remain possible.',
            'OldEXE has exposureinventory and priorlayoutproof only;newnative wandering cases areDXonly. Not proofretail13/currentbehavioridentity.',
            'Static480A00 highest-address block ends in a backward shortJMP,notRET. Its complete1217-byte extent is verified using actual final instruction size;not by requiring the last-address instruction to be RET.',
            'No new rawpatchcases;phase31/32 preservation remains separate evidence.109corpusfiles and4EXEs hashchecked;originals not modified.'])
    (ROOT/'wander-state-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    component=load('placed-creature-semantics.json');component['phase75_wander_state']=s
    (ROOT/'placed-creature-semantics.json').write_text(json.dumps(component,ensure_ascii=False,indent=2),encoding='utf8')
    print('Wander state:18nativeDX/9wholePE/3data;',corpus['file_count'],'files;placementbyte meaningremainsopen')

if __name__=='__main__':main()
