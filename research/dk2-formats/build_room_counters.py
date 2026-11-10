"""Publish the real producer of ownerBYTE1095 and its native HP consumer."""
from pathlib import Path
import json,hashlib,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from capstone.x86 import X86_OP_MEM,X86_OP_IMM,X86_REG_EAX
from build_effect_semantics import pe_read
import dk2_formats as fmt
import emulate_room_counters as native
import native_room_counters as api
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    out={}
    for key,items in load('phase94-primary-manifest.json').items():
        out[key]=[]
        for r in items:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    return out

def switch_mapping():
    data=Path(load('possession-health-semantics.json')['profile']['executable']).read_bytes();md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True
    old=struct.unpack('<45I',pe_read(data,0x4d9274,180));new=struct.unpack('<45I',pe_read(data,0x4d9328,180))
    def instructions(at):return list(md.disasm(pe_read(data,at,64),at))
    def mem_slot(at):
        for ins in instructions(at):
            for op in ins.operands:
                if op.type==X86_OP_MEM and op.size==1 and op.mem.base==X86_REG_EAX and 0x108a<=op.mem.disp<=0x109b:return op.mem.disp
            if ins.mnemonic.startswith('j') or ins.mnemonic=='ret':break
        return None
    rows=[]
    for ident,(a,b) in enumerate(zip(old,new),1):
        left=mem_slot(a);right=mem_slot(b);ii=instructions(b)
        if len(ii)>1 and ii[0].mnemonic==ii[1].mnemonic=='push':
            assert ii[0].operands[0].type==ii[1].operands[0].type==X86_OP_IMM and ii[0].operands[0].imm==1
            right=0x108a+ii[1].operands[0].imm
        assert left==right==api.counter_slot(ident),(ident,hex(a),hex(b),left,right,api.counter_slot(ident))
        rows.append(dict(creature_id=ident,remove_branch=hex(a),add_branch=hex(b),owner_byte_offset=left))
    assert len(api.GROUPS)==18 and sum(r['owner_byte_offset'] is not None for r in rows)==41
    return rows

def catalogues():
    result={};layouts=load('layouts.json')
    for family,field in [('Creatures','CreatureId'),('Rooms','RoomId')]:
        doc=load('decoded/'+family+'.kwd.json');path=Path(doc['source']);data=path.read_bytes();assert digest(data)==doc['sha256']
        parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data
        offset=next(f['offset'] for f in layouts[family]['fields'] if f['name']==field);names={}
        for row in doc['sections'][0]['records']:
            raw=bytes.fromhex(row['raw_hex']);assert data[row['offset']:row['offset']+len(raw)]==raw
            ident=row['fields'][field];name=row['fields']['Name'];assert raw[offset]==ident and raw[:32].split(b'\0')[0].decode('ascii')==name
            assert ident not in names;names[ident]=name
        result[family]=dict(file=str(path),sha256=doc['sha256'],id_raw_offset=offset,id_width=1,names={str(k):v for k,v in names.items()},roundtrip_byte_exact=True)
    assert result['Creatures']['names']['19']=='Monk' and result['Rooms']['names']['26']=='Crypt'
    return result

def results():
    manifest=load('phase94-room-counter-emulations.json');rows=manifest['replays'];by={c['name']:c for c in native.cases()};out=[];assert len(rows)==len(by)==35
    for row in rows:
        p=ROOT/'ghidra'/row['file'];v=json.loads(p.read_text());c=by[row['case']];args,e=native.build(c);r=v['result']
        assert v['case']==c and v['input']==args and v['expected']==e and digest(p.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==x for k,x in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['health']['hp']).items())
        out.append(dict(case=c,expected=e,evidence=row))
    return rows,out

def main():
    p=primary();mapping=switch_mapping();sources=catalogues();rows,out=results();prior=ROOT/'phase94-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==72;prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    groups=[dict(owner_byte_offset=slot,creature_ids=ids,current_names=[sources['Creatures']['names'].get(str(i)) for i in ids],effect='ordinary damage factor subtract8 per positive signed count' if slot==0x1095 else 'not assigned by this phase') for slot,ids in sorted(api.GROUPS.items())]
    s=dict(phase=94,date='2026-10-28',profile=load('possession-health-semantics.json')['profile'],primary=p,source_catalogues=sources,
      native_replays=rows,outcomes=out,api_helpers=[link(n) for n in ['native_room_counters.py','emulate_room_counters.py']],
      crypt_binding=dict(runtime_room_type=26,current_raw_room_name='Crypt',current_raw_id_offset=0x392,
        native_source_path='D:\\Dev\\DK2\\Projects\\Source\\Game\\Crypt.cpp',native_source_path_address=0x6a1904,
        static_entry_paths=['483790 checks current tile for room type1Ah and actor owner,roomWORD49!=0,WORD53==0;successful471890 request leads to4D8E60(actorTag)',
          '4839E0 repeats room type1Ah/current owner and the same successful registration path'],
        static_exit_paths=['47B920','47AF20','47B120','47B4A0'],
        exit_condition='After unregistering room member,roomBYTE18==1Ah and roomWORD53==actorWORD4 lead to4D8E60(0)',
        scope='Entry/exit action paths are statically reconstructed; new native executions begin at4D8E60 with prepared room/member data. No live Crypt scene, room construction, animation or automatic AI action executed.'),
      room_setter=dict(function='4D8E60',arguments='ECX=room,low16 stack argument=new entity tag,RET4',room_old_tag_word=0x53,
        removal='If old tag nonzero,call4D9570;then if old tag alive and entityDWORD12==0,decrement mapped ownerBYTE with signed post-BYTE clamp.',
        addition='If new low16 tag alive and entityDWORD12==0,select by creatureBYTE36F. IDs5/8/42 increment their ownerBYTE directly;other mapped IDs call4D93E0(code,1).',
        owner='4E52F0 reads roomWORD1D and resolves that owner in6EE540. It does not read the registered creature owner.',
        generic_adjustment='4D93E0 uses owner+108Ah+(argumentIndex&FFFFh),adds low8 delta modulo256,then resets result to0 when sign bit is1.',
        cleanup='4D9570 processes object list roomWORD0E and occupant list roomWORD0A,then clears roomDWORD55. New fixtures supply both lists empty; destruction/AI branches not newly executed.',
        always='Writes new low16 tag to roomWORD53 even for an inactive/non-creature/unmapped entity. Does not validate room type locally.',
        arithmetic='value=(old+delta)&255;value=0 if value&80h else value.127+1→0,128−1→127;this is not saturation at127.'),
      species_groups=groups,switch_mapping=mapping,
      monk_damage=dict(creature_id=19,current_name='Monk',owner_byte=0x1095,
        producer='Monk19→4D8E60 generic index11→4D93E0→roomOwnerBYTE1095',
        consumer='4B2CE0 reads damaged creature ownerBYTE1095 as signed;positive count subtracts8*count from ordinary damage factor before clamp0.',
        coexistence='Base128;possession OR WORD443 subtract64 once;then subtract8*positive owner count. Other modifiers and special states retain phase93 limitations.',
        new_native_examples=['Prepared owner counter0→registerMonk→1;Vampire8 oldHP1000/request−100→907 normal,957 possessed',
          'Prepared count1→registerMonk→2;possessedHP963',
          'Clear oldMonk tag withcount1→0;possessedHP950',
          'Roomowner8 gets count1;damaged Creature owner7 stillHP950',
          'Prepared count7→registerMonk→8 withpossession yields no HP loss;prepared15→16 normal also no HP loss'],
        raw_editing='The species→counter map and8/128 contribution are hardcoded in this DX. They are not newly found KWD percentages and must not be offered as ordinary editable KWD fields.'),
      counts=dict(native_cases=len(out),native_replays=len(rows),native_groups_tested=18,static_switch_indices=45,mapped_creature_ids=41,
        whole_native_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),current_source_catalogues=2,
        new_actor_images=0,new_raw_API_patches=0,original_game_function_or_vtable_patches=0),
      scope=dict(source_actor=load('possession-health-semantics.json')['scope']['source_actor'],
        setup='Prepared type26 room,empty cleanup lists,minimal tagged member record and phase90 native-created damaged actorID8;raw224/65 loaders and actual possession setter retained from phase93 composition.',
        no_game_patches='Code,PE vtables,game callbacks and species switch tables unchanged. External session provider records the original HP event as previously documented.'),
      limitations=['Exact supplied DX profile;other EXEs/versions not executed in this phase.',
        'Crypt entry/exit source binding is static;full real-game AI/placement/room building/rendering not executed.',
        'Only empty room cleanup lists executed;other4D9570 list/destruction/state branches remain open.',
        '18 distinct counter groups execute one representative each;all45 switch indices independently checked against both original PE tables,not all aliases newly executed.',
        'Ordinary alive/nonlethal HP handler conditions inherited from phase93. Prepared high counters are forensic inputs,not proof of normal-game reachability or editor bugs.',
        'Game meaning of the other17 counters not assigned;raw editable percentage for Crypt resistance not found.',
        'Fullpossession cycle and scalar98/113/114,Objects34C,Traps204,Doors236,Terrain198/224,Creature Priority/Jump and other15-format questions remain open.'])
    (ROOT/'room-species-bonus-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']))

if __name__=='__main__':main()
