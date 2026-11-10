"""OriginalDoorwriter semantics and requiredpreservation of unassignedbytes."""
from pathlib import Path
import hashlib,json
import dk2_formats as fmt
from build_effect_semantics import pe_read
from native_door_writer import PROFILES,DIRECT,RESOURCES,GAPS
from emulate_door_writer import cases
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    fs=[];bs=[]
    for r in load('phase66-primary-manifest.json')['functions']:
        b=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];m=ROOT/'ghidra'/r['memory'];v=json.loads(m.read_text());raw=bytes(v['data']);assert b.read_text().rstrip()==a.read_text().rstrip() and int(v['address'],16)==r['address'] and len(raw)==v['length']==r['length'] and raw==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);fs.append({**r,'before_sha256':digest(b.read_bytes()),'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(raw)})
    for r in load('phase66-primary-manifest.json')['blocks']:
        m=ROOT/'ghidra'/r['memory'];v=json.loads(m.read_text());raw=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(raw)==v['length']==r['length'] and raw==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);bs.append({**r,'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(raw),'hex':raw.hex()})
    return fs,bs
def corpus():
    rows=[]
    for old in load('entity-definition-semantics.json')['corpus']['files']:
        if not old['records'].get('Doors'):continue
        b=Path(old['file']).read_bytes();assert digest(b)==old['sha256'];records=[r for sec in fmt.parse(b) if fmt.FAMILIES.get(sec.signature)=='Doors' for r in sec.records];assert len(records)==old['records']['Doors'];rows.append(dict(file=old['file'],sha256=old['sha256'],records=len(records)))
    return dict(files=rows,file_count=len(rows),records=sum(r['records'] for r in rows),scope='Door-containing subsetof84sourceentitycorpus;repeatedcataloguescountphysically.')
def main():
    fs,bs=primary();native=load('phase66-door-writer-emulations.json')['cases'];assert len(native)==len(cases())==42
    s=dict(phase=66,family='Doors original Pro/RC3 serialization',record_bytes=616,profiles=PROFILES,primary_program_bytes=fs,primary_data_and_static_ranges=bs,native_emulations=native,corpus=corpus(),direct_model_fields=DIRECT,resource_copies=RESOURCES,
        output=dict(header='56bytes:N=contextcount;S=56+616*N;D=616*N. Enumeratearrayphysicalorder;nullslots skippedwithoutcorrectingN/S/D. Fourmalformednull-slotcasesareexplicitfixtures,not proof GUI normallycreatesnullarrays.',resources='Sixreal41CE60/426210LEA gettersreturnembedded84-bytedata;all504resourcebytescompared. Originalflagsencoder,CStringhelperandftolrununchanged.',strings='RealCStringhelpercopiesentirelength;count32onlypads shortstrings. Labels<=32andsounds<=32producefixedrecords;syntheticlabels543/544/548/562overwritealreadyassignedfieldswithin616-byteoutputbuffer. GUIacceptanceoftheselengthsisnotproved.',flag_rule='NativeencoderbuildsonlymaskE0frommodelBOOL29C/2A0/2A4,noncanonicalnonzeroBOOLaccepted. Combinedwithpreviousdecoderbehavior,nativeroundtripcanloseunsupportedrawbits. Independentserializerpreservesunselectedbits.',height='Modeldouble280*binary644096→actualftol→rawDWORD218. Syntheticexactbinaryinputs1.25/2.25avoidunsupportedgeneralx87roundingclaim.'),
        unassigned_record_spans=[dict(raw_offset=o,bytes=n,origin='Retainedpriorstackbufferbyteswhenbothstringsbounded32. No scalarassignmentinitializesthese8bytes inthiswriterpath.',initial_stack_offset_from_entry=-(0x268-o)) for o,n in GAPS],
        scratch=dict(record_start='EntryESP-268hex;insidewrite-looprecordstartsESP+4Cafterlocals+fourcallee-savedpushes.',source_spans='raw21E/220/230 correspondEntryESP-4A/-48/-38. Uniform00/A5/FFandpatternseedsverified inbothEXEs.',reuse='Scratchreusedbetweenrecords. Firstsynthetic562-charlabeloverwritesall8unassignedbyteswith51hex;nextboundedlabelleavesthose51bytesinthefollowingrecord. Thisisverifiednativewriterstate,notlosslessrawpreservation.',historical_limits='StandaloneDoorshasdifferentnonzerountransferredbytes;corpuspatterniscompatiblewithgarbagebutdoesnotproveuniquehistoricalproducerorvalues. Donotgeneratearbitrarystackbytesinneweditor.',header_padding='Header46/47unassignedunderdateprovider;EntryESP-26A/-269. Full56bytescomparedwithseed. ActualWindowsdatehelpernotexecuted.'),
        word236=dict(raw_offset=0x236,bytes=2,model=0x28A,native_store='Pro4A8D56/4A8D5D;RC3homologouslocations',values=[0,1,32767,32768,65535],scope='Fullwriterspreserveall16bitsindependentlyofraw234ManufToBuild. No gameplaynameorpanelbindingnewlyassigned.'),
        comparison=dict(cases=42,writers_per_profile=21,emitted_records=50,full_header=True,full_record=True,full_models_and_context=True,source_CStrings_unchanged=True,providers='OnlymemoryfileWrite anddate helperarefixtures;noGUI/Windowsclock/filewrites. Actualresource/flags/CString/ftolhelpersandwritersrun.'),
        api=dict(file='native_door_writer.py',operations=['record','patch_word236','patch_untransferred'],policy='record projectsoriginalwriterwithexplicitpriorrawscratch;notpreferredserializer. Patcheschangeonlyselectedrawspanandpreserveallothers. Do notsilentlynormalizeholes/flags/nullcounts.'),
        limitations=['ExactPro/RC3SHAprofiles;sharedwritercodecanhavecommondefects. No universalall-editorbehaviorclaim.','42fullwritersuseexplicitprebuiltmodels,stackseed,date/fileproviders. GUIload/edit/saveandgameplaynotexecuted.','Longnamesaresynthetic,notproofGUIacceptsthem. Onlylabelcopieswithin616bytesaretested;unboundedsound/stackoverflownotexecuted.','The8rawbytesareunassignedwithboundedstringsinthispath;historicalmeaningorotherreadersnotgloballyexcluded. Existingconverteromissionandruntimeunknown236remainseparatefacts.','Nativecountsincludeempty/nullslotswhileoutputskipsnulls;suchslotstatesnotattributedtoGUIcreation.','Heightftolonlyexactbinaryvalues;nogeneralx87fractionalroundingcertification.','Thisdoesnotshowacrash,visualartifactorwronggameplayfromDoorsholes. Safeeditorpreservesoriginalrawbytes.'])
    (ROOT/'door-writer-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');p=ROOT/'door-semantics.json';base=load(p.name);base['phase66_writer']=s;p.write_text(json.dumps(base,ensure_ascii=False,indent=2),encoding='utf8')
    old=load('phase66-old-ap-pointer-leads.json');review=dict(phase=66,program_sha256=old['program_sha256'],getter=0x504140,world_vslot=0x2C8,tag_table=0x6D95C8,search=link('phase66-old-ap-pointer-leads.json'),roots=len(old['producer_roots']),states=old['root_states'],candidates=len(old['candidates']),limited_roots=old['limited_roots'],limited_callees=old['limited_callees'],purpose='NoAP04/08semanticconsumerestablished;native01/10/20leadsandunrelatedclassoffsetcoincidencesdo notdecode04/08.',scope='Boundedstaticforwardingfromuntypedtagtable/vslotseedswithread-policyfix;no newoldAPnativegameplayexecution. Missingpathsandlimitshitremainexplicit.')
    (ROOT/'phase66-old-ap-search-review.json').write_text(json.dumps(review,ensure_ascii=False,indent=2),encoding='utf8')
    print('Doorwriter42native/50records;10fullPE/4ranges;Doorcorpus',s['corpus']['file_count'],s['corpus']['records'],'oldAP',review['roots'],review['states'])
if __name__=='__main__':main()
