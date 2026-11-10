"""Checkpoint unpublished bounded searches;do not promote unknown semantics."""
from pathlib import Path
from collections import Counter
import hashlib,json
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def main():
    manifest=load('phase65-read-replay-manifest.json')
    for r in manifest['outputs']:assert link(r['file'])==r
    for r in manifest['replays']:assert digest((ROOT/r['script']).read_bytes())==r['source_sha256']
    a=load('phase57-interprocedural-creature-leads.json');b=load('phase65-interprocedural-creature-leads-v2.json');assert a['candidates']==b['candidates'] and b['mapped_functions']==8605 and b['states']==647664 and b['limited_functions']==115
    a=load('phase59-typed-definition-leads.json');b=load('phase65-typed-definition-leads-v2.json');assert a['candidates']==b['candidates'] and len(b['producer_roots'])==413 and b['states']==6126 and b['limited_roots']==0
    copies=load('phase65-old-creature-bulk-copy-leads-v2.json');assert len(copies['producer_roots'])==822 and copies['root_states']==14605 and copies['limited_roots']==copies['limited_callees']==0 and len(copies['candidates'])==356 and len(copies['bulk_copies'])==25
    assert not any(r['field'] in {'AbilityJump','PickUpPriority'} for r in copies['candidates'])
    unique=Counter((r['copy']['address'],r['copy']['bytes']) for r in copies['bulk_copies']);assert unique=={('0x443eb2',152):21,('0x421ee0',24):1,('0x421ee2',2):1,('0x421ee4',1):1,('0x58e35a',36):1}
    assert all(r['root']['producer']=='vslot-return-hint' for r in copies['bulk_copies'])
    s={**load('phase64-search-status.json'),'phase':65,'status':'in_progress:corrected_DX_searches_and_bounded_REP_copy_leads_captured;phase64_remains_published_semantic_package','previous_goal_turn_classification':'progress:phase64publishednativeHeroLairconsumer/readpolicyreview;phase65DXmetadatareplaysandREPcopytrackingcaptured','schema':'No phase65 semantic schema yet;editor-format-package67 retains phase64','validation':'Checkpoint only:source/output hashes and bounded search statistics checked;not a nativevalidation ofphase65gameplay','findings':['Corrected DX generic-this summaries8605functions647664states115limited;threecomposedCreaturecandidates unchanged fromhistorical pass','Corrected DX typedObjectTrap413roots6126states10candidates0limited;onlyunknown204overlap5955E5ispreviouslyrejectedDoorHeight','Old822producerbulk search addsconstant-countREP/MOVSsourceintervalsandpossiblecopiedstackreads;14605states356controlreads,noJump/Prioritypurpose','25bulk observationsalloriginateuntypedvslotC0hints:21atoldcamera443EB2/152bytes,plus24/2/1bytesat421EE0/2/4and36at58E35A. NoCreatureidentityprovedbythesebulkcopies','FirstbulkdiagnosticomittedMOVSWpoinadvance;v2includesMOVSWandretainsv1diagnostic. No v1 result promoted','Publishedphase64has23nativecalls/10503rawpatches/67components;phase65searchesdonotchangepurposeunknowns'],
       'scope':'Staticcandidatecodeonly. SamevslotC0isnotCWorldtypeproof;DFforward assumption,constantcountrecoveryandpossiblecopiedstackaliasesrequireprimaryverification. EBPframebasebeforeproducer,heapdestinations,indirectcallreturns,indexedreadsandruntime-builtmetadataremainincomplete.',
       'evidence':[link('phase65-read-replay-manifest.json'),link('phase65-old-creature-bulk-copy-leads-v2.json'),link('scan_creature_bulk_copies.py')],
       'next_action':'Do notrepeat phase64native23/phase63camera68 orunchangedphase65metadata/REPpasses. IfpursuingCreaturebulkleads,firstproveobjecttypeandcopysourceidentity;21old443EB2copiesarecamera-shapedvslotfalseleads,notnewCreaturesemantics. Exploreindexeddataflow/indirectreturn/fullruntimeprototypepaths forremainingunknowns,orcompleteknownpartialgameconsumerwithnativeevidence. Keepallunknownsopen.'}
    (ROOT/'phase65-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    print('Phase65checkpoint:DXcorrectedpassesand25untypedbulkleads;no new semanticclaims;packagephase64')
if __name__=='__main__':main()
