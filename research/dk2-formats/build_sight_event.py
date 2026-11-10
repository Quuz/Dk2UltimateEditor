"""Publish the newly executed Sight event consumer and exact raw preservation."""
from pathlib import Path
from collections import Counter
import json,hashlib,struct
import dk2_formats as fmt
from native_keeper_pipeline import patch_raw
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def primary():
    spec=load('phase77-primary-manifest.json');functions=[];blocks=[]
    for r in spec['functions']:
        before,after,memory=[ROOT/'ghidra'/r[k] for k in ['before','after','memory']];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert before.read_text().rstrip()==after.read_text().rstrip()
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        functions.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    for r in spec['blocks']:
        memory=ROOT/'ghidra'/r['memory'];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        blocks.append({**r,'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b),'hex':b.hex()})
    pairs=list(struct.iter_unpack('<II',bytes.fromhex(blocks[0]['hex'])));assert pairs[-1]==(0,0) and dict(pairs)[13]==0x448580
    assert bytes.fromhex(blocks[1]['hex'])==bytes(17) # Zero table image;registry initializer supplies runtime callback.
    registry=(ROOT/'ghidra/phase77-final-DX-4ad1b0-assembly.txt').read_text()
    assert all(line in registry for line in ['004ad3ec: MOV EAX,0x4af0e0','004ad3f1: MOV [0x0069e9f9],EAX','004ad511: MOV byte ptr [0x0069e9f8],0x19'])
    return functions,blocks

def corpus():
    files=load('keeper-pipeline-semantics.json')['corpus']['files'];selected=[];counts=Counter();patches=0;records=0
    for row in files:
        original=Path(row['file']).read_bytes();assert digest(original)==row['sha256'];parts=fmt.parse(original);assert b''.join(p.pack() for p in parts)==original
        for si,s in enumerate(parts):
            if s.signature!=150:continue
            records+=len(s.records)
            for ri,raw in enumerate(s.records):
                if raw[0xEE]!=1:continue
                counts[(fmt.u32(raw,0xC8),fmt.u32(raw,0xD0),fmt.u32(raw,0x114))]+=1
                selected.append(dict(file=row['file'],record=ri,c8=fmt.u32(raw,0xC8),primary_data1=fmt.u32(raw,0xD0),bonus_data1=fmt.u32(raw,0x114),primary_shot=raw[0xF0],bonus_shot=raw[0x113]))
                for offset in [0xD0,0x114]:
                    for value in [0,7,0x10000,0xFFFFFFFF]:
                        replacement=patch_raw(raw,offset,value);assert replacement[0xC8:0xCC]==raw[0xC8:0xCC]
                        rr=list(s.records);rr[ri]=replacement;section=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);section.replace_records(rr)
                        packed=b''.join(section.pack() if j==si else p.pack() for j,p in enumerate(parts));expected=bytearray(original);at=s.offset+len(s.header)+sum(map(len,s.records[:ri]))+offset;expected[at:at+4]=value.to_bytes(4,'little')
                        assert packed==expected;patches+=1
    return dict(files=files,file_count=len(files),records=records,selected_Keeper_ID1_records=selected,
        ID1_field_counts=[dict(c8=a,primary_data1=b,bonus_data1=c,records=n) for (a,b,c),n in sorted(counts.items())],
        exact_container_patch_cases=patches,scope='PhysicalID1 data observation and explicitData1patches. Map/reference resolution and per-map Shot33ProcessType are not inferred from ID alone.')

def main():
    functions,blocks=primary();native=load('phase77-sight-event-emulations.json');assert len(native['cases'])==30
    shots=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/Shots.kwd';keepers=ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/KeeperSpells.kwd'
    example=dict(files=[dict(file=str(p),sha256=digest(p.read_bytes())) for p in [keepers,shots]],keeper_id=1,keeper_primary_shot=33,keeper_bonus_shot=33,shot_id=33,process_type=25)
    kr=next(r for s in fmt.parse(keepers.read_bytes()) if s.signature==150 for r in s.records if r[0xEE]==1);sr=next(r for s in fmt.parse(shots.read_bytes()) if s.signature==260 for r in s.records if r[0xBE]==33)
    assert kr[0xF0]==kr[0x113]==33 and sr[0xC7]==25
    s=dict(phase=77,family='DX Sight-of-Evil process25 through actual Bridge event0D consumer',executable_sha256=load('executable-hash-verification.json')['Dungeon Keeper 2/DKII-DX.exe'],
        primary_program_bytes=functions,primary_data_ranges=blocks,native_emulations=native['cases'],native_execution_scope=native['scope'],source_example=example,
        source_paths=dict(ShotData1=dict(raw=0x98,definition=0x37,instance=0x13D),KeeperPrimaryData1=dict(raw=0xD0,definition=0x43,instance=0x13D),KeeperBonusData1=dict(raw=0x114,definition=0x4C,instance=0x13D),
            evidence='Earlierraw/converter/loader andphase53/54 cast-routingproofs. NewchainstartsfromprebuiltShot,notnewrawload/fullcast.'),
        process_registration=dict(initializer=0x4AD1B0,runtime_row=0x69E9F8,type_byte=25,callback_pointer_offset=1,callback=0x4AF0E0,
            warning='Runtime row is zero in savedPE;nativeinitializerwrites type25 andcallbackpointer. Staticinitializerproof,notnewinitializationexecution.'),
        process=dict(entry=0x4AF0E0,processed_field=dict(offset=0x169,bytes=4),
            first='IfDWORD169==0,setto1beforeowner/Data1checks. Onlymatching ownerWORD28 andWorldWORD7243 andData1DWORD13D!=0send event0D.',
            event='16bytes:XDWORD1A,YDWORD1E,low16(Shot40)|(low16(Data1)<<16),command0D. Data2notreadinthiscallback.',
            repeat='WhenDWORD169!=0,noinitialevent. Matchingowner andFULLDWORD40==20 callssoundprovider(oldhandle2E,definitionDWORD72,E8,XYZpointer);returnedsoundhandle replaces2E.',
            return_value=1,scope='Return1evenwithoutpacket/effect. Nonzero/highprocessedDWORDs and40highbits tested;notbyte-onlyconditions.'),
        event_path=[0x4AF0E0,0x517480,0x527150,0x43B1E0,0x448580,0x441510],
        bridge_binding=dict(constructor=0x43A740,vtable=0x66CC94,connect_engine=0x43AAD0,handler_pairs=0x66D0D8,event13_handler=0x448580,
            session='World4→Session;Session10→Bridge;nonnullBridge incrementsSession26C modulo32 beforevslot28dispatch. NullBridge drops eventwithoutcounterincrement.',
            dispatcher='Native43B1E0calls Bridge[2CA+4*code]thenforwards sameeventtoEngine.vslot28. Nestedredraw events6 also traverse thisdispatcher;handler6itselfisfixture.'),
        data1=dict(full_dword_gate='Data1!=0enablesinitialevent;zero disables it. This happensbefore16-bitpacking.',event_word='Data1&FFFF',receiver_radius='min(event_word,7)',
            units='Cell-coordinate half-extent:Shot signedQ12X/Y arithmetic-shift12;receiverusescenter±radius.',
            boundary='00010000and80000000are nonzeroDWORDswith radiusWORD0:theysendinitialeventwithradius0. Literal00000000sendsnone. FFFFFFFFhasradius7.',
            editor='PreservefullDWORD;do notnormalizeData1to0..7. Do notlabelthisvalueasdurationseconds. MeaningappliesonlywhenactualProcessType25executes.'),
        region=dict(entry=0x441510,layout=dict(start_tick=0x139F,end_tick=0x13A3,inner_bounds=0x13A7,outer_bounds=0x13B7,radius=0x13C7,width=0x32E,height=0x332),
            update='StorecurrentSessiontick;end=low32(tick+unsignedlow16Shot40). Clampcenter±radius separatelyto0..dimension-1;expandboundsby1andclampagainforouterredraw.',
            events='Ifpreviousstarttick!=0sendoldouterrectangle event6;alwayssendnewouterrectangle event6. Payloaddimensionsarelow16(end-start),without+1.',
            limit='Theseareverifiedstatecoordinatesandredrawevents,notfinalvisiblemask/circle/pixelbrightness. Nativeworld/renderframe loopnotexecuted.'),
        graphics=dict(effect_request='Handler13requestsvirtual90 ID45 withtokenfromvirtualBC;parameterpositions=floorcellXY<<12,Z2000hex,rotationWORD0.',
            actual_forwarder='Native440F50tail-forwardsBridgeBCtoEngineBC. EngineBC/Bridge90areexplicitcapture/providers;actual440420/engineallocation/renderernotexecuted.',
            renderer_fixture='NativeBridgehandler6replacedbydatapointertoadapter;Engineeventcallbacklogsall16bytes. Nooriginalcodeoverwrite.'),
        c8=dict(raw=0xC8,definition=0x3A,status='Purpose remains unresolved',
            new_check='SixKeeperdefinition3Avalues0/1/7FFFFFFF/80000000/FE0C0000/FFFFFFFFleavecapturednativeShot/Bridge/Session/events/effectandsoundrequestsidenticalunderexplicitgraphicsproviders. Definitioncomparedunchanged.',
            boundary='RealKeeperlookup50DD60isofferedinWorldvslot110withvalidID1definition,butexecutedSightchainhasnouseofthatlookup. Thisdoesnotruleoutotherstates,castchecks,otherbuildsorrealgraphicsproviders. NoC8unitassigned.'),
        corpus=corpus(),api=dict(file='native_sight_event.py',operation='describe_data1',scope='Read-only32-bitinterpretationforactualProcess25eventpath;region_stateisfixtureoracle. Existingnative_keeper_pipeline.patch_rawpreservesotherbytes.'),
        limitations=['30completeDXprocess-to-regioncompositions;8originalfunctions execute,3additionalstaticregistration/typefunctions;11wholePE/7dataattestedafterexecution.',
            'Full371Shot,29440World,672Session,5120Bridge,122Keeperdefinition,144Shotdefinition,interfaces/vtablesand1280outputcompared. EAXcapturebeforeobserver;allprocessreturns1.',
            'No fullrawload/cast,actualrenderer/effectallocation,audioplaybackorworldtickloop. Explicitadaptersforredraw6/engineevents/effectparams/effectrequest/sound. Nativecodeintact.',
            'Finalvisibilityshapeandcleanup/expiryframeprocessingarenotprovedbyregionstatealone. Type25Data1isnottheunknownKeeperC8.',
            '441510clampsitsstoredradiusfieldbutusesoriginalradiusargumentforbounds. Inthetestedpath448580alreadycapsargumentat7;otherdirectcallersnotcovered.',
            'OnlyDXnewgameplaychain;oldEXE/RC3behaviornotinferred. Examplecataloguesarecurrentstandalonefiles;othermapreferencescanoverrideShotIDs/processes.',
            '656newin-memorycontainerpatches targetKeeperID1D0/114andpreserveallotherbytesincludingC8;originalgamefiles/4EXEsunchanged.'])
    assert s['corpus']['exact_container_patch_cases']==656
    (ROOT/'sight-event-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    component=load('keeper-cast-semantics.json');component['phase77_sight_event']=s
    (ROOT/'keeper-cast-semantics.json').write_text(json.dumps(component,ensure_ascii=False,indent=2),encoding='utf8')
    print('Sight event:30native/11wholePE/7data;',s['corpus']['exact_container_patch_cases'],'exactpatches;C8stillopen')

if __name__=='__main__':main()
