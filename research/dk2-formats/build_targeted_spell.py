"""Publish real third CreatureSpell wrapper and target prediction evidence."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from build_effect_semantics import pe_read
from native_targeted_spell import patch_height
import emulate_targeted_spell as native
ROOT=Path(__file__).resolve().parent

def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    spec=load('phase82-primary-manifest.json');out=[]
    for name in ['functions','blocks']:
        rows=[]
        for row in spec[name]:
            p=ROOT/'ghidra'/row['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==row['address'] and v['length']==len(b)==row['length']
            assert b==pe_read(Path(row['exe']).read_bytes(),row['address'],row['length'])
            r={**row,'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b)}
            if name=='functions':r['assembly_sha256']=digest((ROOT/'ghidra'/row['assembly']).read_bytes())
            else:r['hex']=b.hex()
            rows.append(r)
        out.append(rows)
    return tuple(out)

def corpus():
    source=load('creature-shot-request-semantics.json')['corpus']['files'];files={};records={170:0,160:0};patches=creature_files=0
    for row in source:
        p=Path(row['file']);original=p.read_bytes();assert digest(original)==row['sha256'];files[str(p)]=dict(file=str(p),sha256=row['sha256'])
    for row in files.values():
        original=Path(row['file']).read_bytes();parts=fmt.parse(original);assert b''.join(s.pack() for s in parts)==original;has_creature=False
        for si,s in enumerate(parts):
            if s.signature in records:records[s.signature]+=len(s.records)
            if s.signature!=170 or not s.records:continue
            has_creature=True;raw=s.records[0]
            for value in [0,0x80000000,0xFFFFFFFF]:
                rr=list(s.records);rr[0]=patch_height(raw,value)
                section=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);section.replace_records(rr)
                packed=b''.join(section.pack() if j==si else other.pack() for j,other in enumerate(parts));expected=bytearray(original)
                at=s.offset+len(s.header)+0xEA4;expected[at:at+4]=value.to_bytes(4,'little');assert packed==expected
                assert fmt.parse(packed)[si].records[0]==rr[0];patches+=1
        creature_files+=int(has_creature);assert digest(Path(row['file']).read_bytes())==row['sha256']
    return dict(files=list(files.values()),unique_source_files=len(files),creature_files=creature_files,creature_records=records[170],creature_spell_records=records[160],new_height_exact_container_patches=patches,
      scope='Hashcheck83 prior unique Creature/CreatureSpell inputs. Patch rawEA4 fullDWORD in first Creature record of each of82 files with0/80000000/FFFFFFFF;whole-container equality outside selected4bytes. No original source writes. Counts are file records,not unique definitions.')

def snapshots():
    rows=[]
    for c in native.cases():
        n='ghidra/phase82-targeted-spell-'+c['name']+'.bin';b=(ROOT/n).read_bytes();_,e=native.build(c,0);native.verify_node(c,b,e)
        rows.append(dict(case=c['name'],allocated=e['success'],file=n,bytes=len(b),sha256=digest(b),prediction=e['prediction'] if e['success'] else None,
          origin=e['origin'] if e['success'] else None,target_height=e['target_height'] if e['success'] else None))
    return rows

def main():
    functions,blocks=primary();nodes=snapshots();replays=load('phase82-targeted-spell-emulations.json')['replays']
    assert len(functions)==50 and len(blocks)==17 and len(nodes)==27 and sum(r['allocated'] for r in nodes)==21 and len(replays)==81
    base=(ROOT/'ghidra/phase82-targeted-spell-base.bin').read_bytes()
    assert all((ROOT/'ghidra'/('phase82-targeted-spell-'+name+'.bin')).read_bytes()==base for name in ['alt-zero','alt68','alt255','moving-z'])
    height=next(r for r in load('scalar-storage-paths.json')['families']['Creatures']['joined_paths'] if int(r['raw_offset'],16)==0xEA4)
    assert height['runtime_offset']=='0x648' and height['raw_bytes']==4 and height['runtime_bytes']==2
    s=dict(phase=82,date='2026-10-28',families=['Creatures','CreatureSpells','Shots'],executable=functions[0]['exe'],executable_sha256=digest(Path(functions[0]['exe']).read_bytes()),
      primary_program_bytes=functions,primary_file_backed_data=blocks,native_replays=replays,raw_snapshots=nodes,corpus=corpus(),
      counts=dict(logical_scenarios=27,native_emulation_invocations=81,successful_allocation_scenarios=21,nonallocation_scenarios=6,successful_allocation_replays=63,
        complete371byte_snapshots=27,original_code_or_vtable_replacements=0,native_code_spans_attested=50,file_backed_data_ranges=17),
      root=dict(address='0x498870',callers='DATA entries at49548A/49586C in4950C0;full animation-event dispatch not newly executed',
        executed='Actual slot getter,cooldown clock,CreatureSpell exists/get,sprintf/output helpers,target native vslots,factory/pool/registration/resource/trajectory run;GFX/sound external providers remain explicit.',
        returns='1 during unsigned cooldown waiting;0 after attempt,including successful creation. Return0 is not a creation-failure verdict.',
        gates='After readable Spell,primary definitionBYTE44 must be nonzero. TargetWORD39D must have nonzero alive byte6ED540[tag];native targetDWORD12 must not equal9. No Creature-only restriction on this path.',
        diagnostic='Native sprintf634DB0 writes global740C20 with Creature Spell12 has no Shot Type associated even for valid nonzero primaryShot. Formatting occurs after successful Spell lookup,before primaryShot!=0 check;this text alone does not establish an error/log emission.'),
      target_getters=dict(previous_position='All3 original Creature/Object/Shot vtables use v2C=4B5D40 returning this+F6.',
        height={'Creature v40=4B5D80':'u16 definition648 at level1;otherwise trunc0(s16(global6E4154+CF+2*level)*u16height/100),returned as32bits.',
          'Object v40=402AD0':'0 on exact native Object vtable.',
          'Shot v40=4B7E10':'u16 definition4F shifted left1;the caller halves it logically before adding to aimZ.'},
        class_scope='Prepared objects with original class vtables;constructors of target entities are not run. Kind9 is rejected before any target vslot.'),
      creature_height_lineage=dict(raw_offset=0xEA4,raw_bytes=4,intermediate_offset=0x1294,definition_offset=0x648,definition_bytes=2,prior_lineage=height,
        interpretation='Target getter zero-extends low16 height;level!=1 additionally uses signed level coefficient. Full rawDWORD is preserved in serializers.',
        scope='Raw/intermediate/definition link is previously proven static lineage;new execution starts with prepared target definition,not raw Creature loader.'),
      prediction=dict(source='Current targetXYZ from1A;previousXYZ from real v2C;actor currentXYZ from1A.',
        dx_dy='s32(low32(targetXY-actorXY));each square is low32 then arithmetic>>12;sum and<<4 wrap32.',
        factor='floor_sqrt(u32(((s32(low32(dx*dx))>>12)+(s32(low32(dy*dy))>>12))<<4))>>8;actual65FAE0 executes.',
        xy='aim[i]=u32(target[i]+trunc0(s32(low32(low32(target[i]-previous[i])*factor*3))/2)),i=X/Y.',
        z='aimZ=u32(targetZ+(u32(nativeHeight)>>1));previousZ is not used in this prediction.',
        consequences=['One tile horizontal distance gives factor1,two tiles factor2,half a tile gives0 in the checked controls.',
          'MovingX delta256 at factor1 adds384;movingY delta-256 subtracts384.',
          'Creature height2048 adds1024,odd4095 adds2047;level2 coefficient150 makes height3072 and adds1536.',
          'Changing only previousZ gave identical371-byte Shot in this path. This does not prove vertical movement globally ignored.'],
        speed='Shot speed changes later native trajectory setup;it is not read in this wrapper prediction formula. No general guarantee of interception or real visual trajectory is inferred.'),
      native_shot=dict(data='After actual creation,CreatureSpell definitionDWORD1F/23 overwriteShot13D/141,as in prior wrappers.',
        creator='ActorWORD4->Shot145 and16D;targetWORD39D->Shot147. Native target copy4AB830 writes predictedXYZ131 before actual4AB8A0.',
        id30='No special ID30 shortcut in498870:the successful Shot30 case retains target147 AND executes ordinary prediction/trajectory setup. This differs from already-confirmed4984D0 ID30 branch;not a contradiction or a ProcessType9 tick test.',
        origin='Actor physical slotXYZ low32 rotation as established earlier;flag80 zerosXY offsets butstilladdsZ. New nonzero-offset controls checked.'),
      alternative_ids=dict(pairs=[[36,4],[0,0],[68,0],[255,255]],result='All371 bytes equal in new third wrapper498870 under fixed prepared inputs. Prior4982B0/4984D0 native equality remains separate evidence.',
        global_gameplay_meaning='Unresolved;no global nonuse or alternate-replacement rule inferred.'),
      capture=dict(method='3 independent fresh identical-input runs read128-byte XMM pages after actual return;assemble371bytes. Not atomic live-process snapshots.',
        checks='Every run compares full prepared pool/global/tag/kind/owner/map/actor/target/definitions/coefficients/diagnostic buffer plusoriginal input buffers andoutside-allocation memory.',
        rejected_diagnostic='phase82-emulation-targeted-spell-base-page0.json retained:old observer incorrectly expected undecremented available tag count. Native code completed;onlyv2 files are final evidence.'),
      limitations=['Prepared target/actor/Spell/Shot definitions,finite lists,empty terrain and cardinal coefficient data;not a full game/AI or renderer test.',
        'Target vtables and native sprintf/factory/resource/math functions are intact;graphics0 and fixed external sound handle remain providers.',
        'No full target constructor,raw catalogue loading,animation-event selection,subsequent shot ticks,collision,hit,damage or immunity in this phase.',
        '50 code spans/17 data ranges are post-run PE attestations,not a per-function branch-execution trace. Several CRThelpers are conservative static evidence.',
        'Unknown object bytes retained opaque. AlternativeE8/E9 and other substantial format semantics remain open;original goal not achieved.'])
    (ROOT/'targeted-spell-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    old=load('spell-alternative-semantics.json');old['phase82_targeted_cast']=s
    (ROOT/'spell-alternative-semantics.json').write_text(json.dumps(old,ensure_ascii=False,indent=2),encoding='utf8')
    for name in ['creature-subrecords.json','shot-creation-semantics.json']:
        old=load(name);old['phase82_targeted_cast_reference']=link('targeted-spell-semantics.json')
        (ROOT/name).write_text(json.dumps(old,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(dict(phase=82,counts=s['counts'],corpus={k:v for k,v in s['corpus'].items() if k!='files'})))

if __name__=='__main__':main()
