"""Native DX Keeper base/bonus Shot routing and bounded C8 non-effect checks."""
from pathlib import Path
from collections import Counter
import json
import dk2_formats as fmt
from build_effect_semantics import pe_read,asm_range
from build_keeper_pipeline import digest
from native_keeper_cast import selection_view,ROUTES
ROOT=Path(__file__).resolve().parent
BASELINE_CORRECTION='Phase53:upgrade state selects nonzero/lookup Shot and Data1/Data2 tuple, but full DX4BBF90/4BC290 reload primary definition42 as actual4B7CD0 ShotId argument. BonusShotId alone does not replace created type in these routes. See keeper-cast-semantics.json for120 full-wrapper emulations with provider/validator/factory fixtures.'
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def primary():
    out=[]
    for r in load('phase53-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];m=ROOT/'ghidra'/r['memory'];assert p.read_text().rstrip()==a.read_text().rstrip();v=json.loads(m.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and v['length']==r['length']==len(b) and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);out.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(b)})
    return out
def corpus():
    files=load('keeper-pipeline-semantics.json')['corpus']['files'];pairs=Counter();different=[];records=0
    for row in files:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];sec=next(s for s in fmt.parse(b) if s.signature==150)
        for i,r in enumerate(sec.records):
            records+=1;p=(r[0xF0],r[0x113]);pairs[p]+=1
            if p[0]!=p[1]:different.append(dict(file=row['file'],record_index=i,keeper_spell_id=r[0xEE],name=r[:32].split(b'\0')[0].decode('cp1252'),primary_shot_id=p[0],bonus_shot_id=p[1]))
    return dict(files=files,records=records,pairs={f'{a},{b}':n for (a,b),n in pairs.items()},different=different,different_count=len(different),different_both_nonzero=sum(bool(r['primary_shot_id'] and r['bonus_shot_id']) for r in different))
def main():
    s=dict(family='DX KeeperSpells full cast wrapper routing',executable_sha256=load('executable-hash-verification.json')['Dungeon Keeper 2/DKII-DX.exe'],routes=ROUTES,primary_program_bytes=primary(),native_emulations=load('phase53-keeper-cast-emulations.json')['cases'],corpus=corpus(),
        correction=BASELINE_CORRECTION,
        fields=[dict(name=n,raw_offset=r,runtime_offset=v,bytes=w) for n,r,v,w in [('PrimaryShotId',0xF0,0x42,1),('PrimaryData1',0xD0,0x43,4),('PrimaryData2',0xD4,0x47,4),('BonusShotId',0x113,0x4B,1),('BonusData1',0x114,0x4C,4),('BonusData2',0x118,0x50,4),('TargetRule',0xDA,0x77,1),('UnknownC8',0xC8,0x3A,4)]],
        selection='Player spell-state DWORD0D!=0 selects BonusShotId/Data1/Data2;otherwise selectsprimarytriple. SelectedShotId must be nonzero,then worldvirtual1B0 lookup usesselectedID. This isdistinct from creationID.',
        creation='A:4BC160 readsAL[definition42] before4BC19Ecall4B7CD0. B:4BC401readsAL[definition42]before4BC40Ecall. Actualfactory4B7CD0usesitsfirstargumentforvcall1ACexistencecheck,storeslowBYTEintoentity124at4B7D26,andforwardsittobaseinit4AAC90. Factoryitselfiscapturedstaticallyandstubbedinside120wrapperruns.',
        shot_fields='Afterfactorysuccess wrapperswrite selectedData1→ShotDWORD13D,Data2→DWORD141,target argumentlow16→WORD147. ForTargetRule1/2/3/6writeDWORD14D=1;otherwiseleaveprevious14Dbytes. All371-byteShotfixturecompared,otherbytesunchanged.',
        selected_zero='BonusShotId0 withupgrade!=0 exitsbeforelookup/factoryevenifprimaryIDnonzero. PrimaryID0 withnonzeroBonuspasses0tothefactoryfixture. RealfactorymayrejectID0;syntheticfixtureacceptanceisnotproofactualentity0canexist.',
        coordinates='A adds2000hex to copiedtargetZwhenprimaryID==21,independentlyofselectedBonusID. BcopiesXYZwithoutthisadjustmentinitsinspectedbody. RouteA/Blabelsareaddresses,notgeneralizedentity/landUIcommands.',
        side_effects='Successfulfixturefactory causesnativewrapper tomanagefieldsabove,callmanaadjustmentwith-1234andadd1234to playerDWORD10F2,write clock11223344tospellstateDWORD5/9,andsendsoundvirtual20. Bothwrappers returnEAX0 evenonsuccess;do notinterpretreturnascastfailure. Currencyvalidationfailurecanbebypassedbycallflagsbit1;otherfailurefixturesarestillrespected.',
        validation='120completewrappers:96casts(2routes×2upgrades×4primary/bonuspairs×6C8patterns),14TargetRulechecks,8availability/mana/validator/factoryfailurecases,2mana-bypasscases. Compareentire371Shot,4352player,17spellstate,122definition,64capturebytes. CaptureselectedlookupandactualfactoryID,XYZ,ownerWORD,delayWORD,creationflags,eventcounts,manaargumentandnativeEAXreturn.',
        fixtures='Worldavailability/definition/shot providers,affordability,targetvalidator,spellstategetter,factory,manamutator,clockandsoundareexplicitEmulatorHelpermemoryfixtures. Nativewrapperinstructionbodies,upgradechoices,fieldwrites,manaaccumulator,timestampwritesandcallargumentformationrun. ActualShotprocess,allocation,targetrules,spatialworldandmodifiedgameplaynotrun.',
        c8=dict(status='Purpose remains unresolved',mutation='Sixruntime3Avalues0/1/7FFFFFFF/80000000/FE0C0000/FFFFFFFF do notchangecapturedoutputs or wrapper-sideeffectsundertheseproviders/validators/factoryfixtures. Definitionitselfcomparedunchanged. ThisdoesnotproveC8unusedbyrealhelpersorwholegame.',search='BoundedCFGaliassearch:45candidategetter/vcall110seeds,838states,noC8overlapread,9pointerforwardleads. Virtual110 aloneisnottypeidentity;unknowncalls/indirectjumps/stackaliaslimitsreported. Originalstraight-line150instructionsearch andRC3dialog258comparisonlikewisedonotnameC8.'),
        api=dict(file='native_keeper_cast.py',operation='selection_view',scope='Diagnosticselection/dispatchviewforrawKeeper406andexactDXroutes;doesnotpredictsuccessfulgameplayorautomaticallyrewriteShotIDs.'),
        evidence=[asm_range('phase53-game-4bbf90-disassemble_function.txt',0x4BC076,0x4BC0A6),asm_range('phase53-game-4bbf90-disassemble_function.txt',0x4BC155,0x4BC19E),asm_range('phase53-game-4bc290-disassemble_function.txt',0x4BC370,0x4BC3B1),asm_range('phase53-game-4bc290-disassemble_function.txt',0x4BC3F7,0x4BC437),asm_range('phase53-game-4b7cd0-disassemble_function.txt',0x4B7CD0,0x4B7D2C)],
        limitations=['ExactDXonly;oldorRC3castimplementationsnotclaimed. ThisconcernsKeeperSpellsBonusShot,notCreatureSpellsAlternativeShotE8/E9.',
            'SyntheticspellID5avoidsCreateImp-specificcostmultiplierandShotprocess3healingbranch;thosebrancheshavenotbeenexecutedhere. Shotproviderprocess0isafixedfixture.',
            '120fullwrapperrunsarecontrollednativeexecution,notfullgamecasting. Validators/factorycanreadotherdataintheactualgame;C8globalnon-useisnotproved.',
            'Initialfixtureoverlapwasrejectedbeforeemulation. First120runsused337-byteShotfixture;finalv2runscomparecomplete371-bytefixturematchingnativefactoryallocation173hex. Onlyv2evidenceispackaged.',
            'PreserveallrawIDs/unknownbits/bytes;do notsilentlycopyBonusShotIdintoprimaryasarepair. OriginalgamefilesandEXEsuntouched.'])
    assert len(s['native_emulations'])==120
    if (ROOT/'sight-event-semantics.json').exists():s['phase77_sight_event']=load('sight-event-semantics.json')
    (ROOT/'keeper-cast-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Keeper cast:120nativecases;',s['corpus']['different_count'],'records with differing IDs')
if __name__=='__main__':main()
