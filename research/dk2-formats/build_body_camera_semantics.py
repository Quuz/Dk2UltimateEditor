"""Publish native CDeadBody camera consumer and exact bounded PRNG rule."""
from pathlib import Path
import json,hashlib
import dk2_formats as fmt
from build_effect_semantics import pe_read
from emulate_body_camera import cases,table
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    spec=load('phase65-primary-manifest.json');fs=[];bs=[]
    for r in spec['functions']:
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data']);assert before.read_text().rstrip()==after.read_text().rstrip() and int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);fs.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);bs.append({**r,'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    return fs,bs

def corpus():
    files=[];snap=ROOT/'phase65-input-snapshots'
    for old in load('creature-extension-semantics.json')['corpus']['files']:
        p=Path(old['file']);b=p.read_bytes();sha=digest(b);source=p
        if sha!=old['sha256']:
            snap.mkdir(exist_ok=True);source=snap/(p.stem+'.'+sha[:16]+p.suffix)
            if source.exists():assert source.read_bytes()==b
            else:source.write_bytes(b)
        records=[r for sec in fmt.parse(b) if fmt.FAMILIES.get(sec.signature)=='Creatures' for r in sec.records]
        files.append(dict(file=str(p),input_path=str(source),sha256=sha,historical_sha256=old['sha256'],records=len(records),record_bytes=sorted({len(r) for r in records})))
    return dict(files=files,file_count=len(files),records=sum(r['records'] for r in files),scope='82 Creature catalogue sources;changed historicalsources are independentlysnapshotted,not overwritten/restored. Repeatedcataloguecopies countedphysically.')

def main():
    fs,bs=primary();native=load('phase65-body-camera-emulations.json')['cases'];assert len(native)==len(cases())==44
    s=dict(phase=65,family='Things.CDeadBody camera mode11 / Creatures.EyeHeight',program='DKII-DX.exe',executable_sha256='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a',primary_program_bytes=fs,primary_data_and_fragments=bs,native_emulations=native,corpus=corpus(),
        body_identity=dict(native_class='CDeadBody',rtti=load('phase65-body-rtti.json'),constructor='4B8050 finalvtable66D904',kind_path='4B0F20 PUSH6→4B1DF0→4B28D0→4B4A10 storesargumentintoentityDWORD12.',definition_path='4B0F20 worldvirtualC0 Creaturelookup→storeentity129 at4B0F5D. RuntimebodyID128,level12D aredistinctfromordinaryCreatureID36F/definition370.',inherited_CA_storage=link('thing-layouts.json'),scope='NativeRTTI/baseinstructionsbindtypes statically;phase65doesnotexecutecreationagain. PreviouslyverifiedCApayload16 hasnoanglefield;callback5212D0draws angle0..2047 fromRNG.'),
        eye_height=dict(raw_offset=0xEB0,raw_bytes=4,intermediate=0x12A0,presence=0xF4,definition=0x65C,definition_bytes=2,native_gui_label='Eye Height (Tiles)',label_evidence=link('creature-editor-scaled.json'),storage='RawDWORD copiedtomidDWORD;gatedloaderstoreslow16. Thecamera zeroextendsWORD65C;rawhigher16bits remainphysicaldataandarenotdiscardedbysafeeditor.',new_consumer='Mode11kind6usesthisEyeHeightasangularXYoffsetfactor,notZaddition. ItreadsCDeadBodydefinition129,notordinaryCreaturedefinition370.'),
        selection=dict(function='44ACE0',conditions='Requestedmode11,validityBYTE6ED540[low16node]!=0,entityDWORD12==6;otherwisephase63fallback.',native_angle='entityWORD F4 indexes both tables directlywithoutAND7FF. Onlyvalid0..2047indicesprojected/emulated. EarlierCAcreatorproducesangle&2047.',point='u16height=definition65C;dx=s32(low32(tableX[angle]*height))>>16;dy=s32(low32(tableY[angle]*height))>>16;X=u32(entityX-dx),Y=u32(entityY+dy). Terrainvslot204 receives(X,Y,entityZ). TargetZ=u32(returnedterrainheight+256).',table=dict(base_x='0x6C1488',base_y='0x6C1C88',overlapping_bytes=0x2800,sha256=digest(table()),rule='NativeinitializedPEbytes;10240-byteunion,not two independent duplicated8192-bytearrrays.'),sideeffect='entityBYTEF3&=FE occurs inthisbranchbeforetransitioncompletion. Fullspecified1024-bytebodyfixtureotherwiseunchanged;nativeobjectallocationis320bytes,not1024.',noninputs='RuntimeLevel12D andCArawhealth0C/rawZ are notreadbythisnewbranch;CArawZplacementis independentlyspecifiedinphase38.'),
        rng=dict(function='5558D0',nonzero_bound_rule='updated=u32(seed*0x19660D+0x3C6EF35F);*seed=updated;returnROR32(updated,13)%unsignedbound.',tests='20 complete nativecalls:4seeds×5nonzerobounds;all16seed/backingbytesandnativereturncompared. NoPRNGinstructionreplacement.',diagnostic_scope='global75B2F4=0innewfixtures;debuglogging pathrequiresglobalnonzeroandspecialseedpointer6F3FA8. Zeroboundlogging/debugwindowbranchesnotnewlytested.',body_sequence='a=R(0x77758,&localcopyentityX);b=R(0x2278,&localcopyentityY);seed=a XOR b;rollRand=R(256,&seed);pitchRand=R(512,&seed);yawRand=R(256,&seed). entityX/YarenotupdatedbylocalPRNGseedwrites.',angles='DC6=(activeYawD84+yawRand-128)&2047;DCA=(rollRand-128)&2047;DC8=(pitchRand+768)&2047. Samecoordinates/activeyaw reproducethesameanglesunderthisarithmetic;doesnotconsumeaworldrandomseed.'),
        activation=dict(tests='24 full44ACE0 calls,23kind6paths+1invalidnodefallback. Full0xF00manager,entireexplicitbody/definition,andterraininputpositioncompared. Threecasesprecedecamerawithreal21-byteEyeHeightassignmentfragment4923C1..4923D5:high/lowDWORDpairandpresence0retention.',transition='Commonmode11eventrequests(9,now,1500),(7,1500),targetLensfrommid84andduration3000remainasphase63. E3Cbit40doesnotforceinstantcompletionhere. Targetsarewritten;activecamera/renderingnotcertified.',providers='Clock,worlddimensions,groundheight,globalmodehook,timer/matrix/eventprovidersremainexplicitfixtures. NativePRNGandnativePEtrigtablearenowreal. ActualCDeadBodycreation/fullterrain/eventqueue/renderer/perframeinterpolationnotexecuted.'),
        api=dict(file='native_body_camera.py',operations=['random_step','project'],raw_patch='native_creature_editor_scaled.patch_raw(record,0xEB0,value) retains4physicalbytes;do notsavecamera-derivedruntimeWORDbackoverrawDWORD.'),
        bounded_search_followup=dict(read_replay=link('phase65-read-replay-manifest.json'),bulk_leads=link('phase65-old-creature-bulk-copy-leads-v2.json'),rule='CorrectedDXgeneric8605functions/647664states115limitedandtyped413roots/6126states10leads:nochangedunknownconsumers. OldREP25observationsalluntypedvslotC0hints;noJump/Prioritypurpose. Searchresultsnotgameplayclaims.'),
        limitations=['ExactDXSHAonly;notoldgame/RC3equivalence.','Newnativebodycamera tests useprebuilt1024-bytebackingbodyfixture;nativeCAobjectsizeis320. StaticCDeadBodyRTTI/base/initpathsnotnewlyemulated.','Runtimeangleoutside0..2047notexecuted;nativeindexlacksmaskbutconsequenceisnotpredicted.','PRNGzerobound/logging/debugwindowbranchesnotnewlyexecuted;nonzeroarithmeticverifiedwithdebugflag0.','Raw→midconverterlinkstaticplusinheritedschema. ThreeEyeHeightassignmentfragment→camera casesusetemporaryemulatorentry/exitjumps;notcompleteCreatureloader,rawparserorbodycreationchains. Bothfullnativefunctionsandexplicitfragmentsattestedseparately.','Ground/world/time/eventsandotherexternalsremainproviders;nogameplay/renderercallorvisibleartifactfinding.','MajorremainingunknownpurposesKeeperC8/CreatureJumpPriority/Terrain198224/Traps204/Objects34C/AP0408/CreatureSpellalternatives/WanderRadiusremainopen.'])
    (ROOT/'body-camera-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    p=ROOT/'camera-semantics.json';c=load(p.name);c['phase65_body_camera']=s;p.write_text(json.dumps(c,ensure_ascii=False,indent=2),encoding='utf8')
    p=ROOT/'dead-body-semantics.json';c=load(p.name);c['phase65_camera_consumer']=link('body-camera-semantics.json');p.write_text(json.dumps(c,ensure_ascii=False,indent=2),encoding='utf8')
    print('Bodycamera44native/5executed10PE/7ranges;corpus',s['corpus']['file_count'],s['corpus']['records'])
if __name__=='__main__':main()
