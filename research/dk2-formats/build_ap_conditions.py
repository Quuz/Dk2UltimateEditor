"""Native AP trigger-parameter routing, actual counters and signed comparisons."""
from pathlib import Path
from collections import Counter
import json,struct
import dk2_formats as fmt
from build_keeper_cast import ROOT,load,digest
from build_effect_semantics import pe_read
from native_ap_conditions import OPS
KINDS=[39,40,41,42,62,69,70]
def primary():
    spec=load('phase56-primary-manifest.json');out=[]
    for r in spec['functions']:
        p=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];v=json.loads(memory.read_text());b=bytes(v['data'])
        assert p.read_text().rstrip()==after.read_text().rstrip() and int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    blocks=[]
    for r in spec['blocks']:
        p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data']);assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);blocks.append({**r,'memory_sha256':digest(p.read_bytes()),'program_bytes_sha256':digest(b)})
    return dict(functions=out,blocks=blocks)
def corpus():
    src=load('corpus-scan.json');paths=[]
    for row in src['files']:
        if any(s['family']=='Triggers' for s in row.get('sections',[])):paths.append(dict(file=str(Path(src['directory'])/row['file']),sha256=row['sha256']))
    snap=next(r for r in load('phase49-survey-updates.json')['changed_sources'] if Path(r['file']).name=='AutoSaveTriggers.kld');paths.append(dict(file=snap['snapshot'],original_file=snap['file'],sha256=snap['snapshot_sha256'],snapshot=True))
    files=[];kinds=Counter();filters=Counter();nonzero=[];total=0
    for row in paths:
        b=Path(row['file']).read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);sec=next(s for s in parts if s.signature==210);n=0
        for i,r in enumerate(sec.records):
            total+=1
            if fmt.u32(r)!=213 or r[22] not in KINDS:continue
            n+=1;kind=r[22];kinds[kind]+=1
            if kind in [40,41]:
                filters[r[10]]+=1
                if r[10]:nonzero.append(dict(file=row['file'],record_index=i,condition_kind=kind,terrain_filter=r[10],parameters_hex=r[8:16].hex()))
        files.append({**row,'records':len(sec.records),'ap_condition_records':n})
    return dict(files=files,records=total,ap_condition_records=sum(kinds.values()),type_counts={str(k):n for k,n in sorted(kinds.items())},claim_terrain_filter_counts={str(k):n for k,n in sorted(filters.items())},nonzero_previously_reserved_filters=nonzero,scope='109 map trigger files with baseline hashes plus independently captured AutoSaveTriggers snapshot from phase49;not an atomic map bundle.')
def main():
    spec=load('trigger-layouts.json')['0xD5'];p=primary();s=dict(family='ActionPoint TriggerGeneric predicates',program='DKII-DX.exe',executable_sha256=load('executable-hash-verification.json')['Dungeon Keeper 2/DKII-DX.exe'],primary_program_bytes=p['functions'],primary_data_ranges=p['blocks'],native_emulations=load('phase56-ap-condition-emulations.json')['cases'],corpus=corpus(),
        dispatch='AP tick4B05E0 forwards triggerWORD34/byte36 to4B0CC0;positive condition nodes call4B0D70 with ECX=AP. Full4B0D70 and eight counter/filter/comparator helpers execute in61tests. AP tick/recursive trigger walker reviewed statically,not included in61execution paths.',
        comparisons=dict(function='0x519980',table='0x519A04',operators={str(k):v for k,v in OPS.items()},operands='Signed32 comparison;AP counters zero-extended low16 before call.0,7..FFFFFFFF returnfalse,not out-of-table call.'),
        conditions=[dict(type=k,name=spec[str(k)]['name'],parameter_fields=spec[str(k)]['fields'],native_note=spec[str(k)]['native_note']) for k in KINDS],
        count_entities='4B0620 traverses inclusive tile bounds then each tile headWORD0/entitynextWORD0C via6EE540. Requires entityDWORD12==0 unless targetTypeBYTE3==6,which selects2.4B0A80 uses PlayerBYTE10FC if player!=0 and creatureBYTE36F/objectBYTE12C if target!=0. Return low16 count. No further possession test for type27.',
        count_owned='4B07E0:player0/terrain0 returns low16 inclusive area without scanning. Terrain0/player!=0 scans TerrainBYTE17B&04 and tileWORD8&0FFF==player ownerWORD4. Nonzero terrain filters tileBYTE0A and optionally owner;does not additionally requireTerrain04 in that branch. Baseline previously called byte2 reserved for types40/41;now corrected.',
        player_index='For tile ownership native obtains playersArray+(playerId-1)*4389+4. For marker lists it reads pointer at playersArray+(playerId-1)*4389+1105. Player bounds not checked in these bodies;tests use1/2 with allocated arrays. Creature owner filtering uses entityplayerptr+10FC,which is distinct fromownerWORD4.',
        count_marked='4B0960 iterates inclusive bounds and actual508290 searches player marking-list categories1..15;AP counts only returned categories1or2. Lists use counts4678+2category,head4656+2category,36-byte nodes with XY0/2 andnextWORD1F. Types3and15 do not contribute to this AP count. No map tile visibility assumption replaces these lists.',
        tag_all='Type69/45hex forcescomparison5(>=),threshold=low16 count ofTerrainBYTE17B&08 cells computedby4B0A00,notfullAParea;rawcomparison/value ignored.0eligible+0marked returns true. Baseline Terrain flag08 nameTAGGABLE remains separately attributable;bit use verified natively.',
        claim_all='Type41/29hex forcescomparison3(==),threshold=full32 inclusive area;rawcomparison/value ignored. Measuredcount is low16,so area65536 compares0 to65536 and fails even in unfiltered fast-area case. Byte2 terrain filter is used,byte3 ignored.',
        possessed='Type70/46hex returns existence,ignoring rawcomparison/value. Actual4B06D0 requires matching kind/player/target and creatureBYTE3E6&80 or objectBYTE146&08. It stops on firstmatch;no519980call.',
        ap_flags='AP runtime32 values0/04/08/0C produce identical condition results and unchangedfullAP in7four-case groups. This only excludes an effect in these AP condition bodies;purpose04/08 globally unresolved. Eight candidateAPgettercall sites saved separately,notalltype-proved.',
        fixtures='Only world virtual1D8 coordinate→tile fixture. Native dispatch,counts,entity filter,mark lookup and signedcomparator run. World/player/mark/entity/terrain/tile buffers are explicit synthetic input;entire supplied state compared unchanged. Ordered3x2coordinates;65536area test uses fast-area branch without scanning. No dynamic allocation,renderer,fileI/O,fullgameortriggeractions.',
        raw_editor_rule='Keep complete D5/D6 envelopes,payload16,first8parameters,unusedbits and sourceorder. NewTerrainId is byte2 ofparameters=byte10 of24-byte enveloped node. Preserve ignored comparison/value;do not normalize them from runtime semantics. Patches change selected parameterbytes only.',
        baseline_corrections='build_trigger_layouts.py now calls native_ap_conditions.apply_layout_overrides. Type40/41reserved_2 split into terrainIdBYTE2/reserved_3BYTE3. All7native notes added;baseline_fieldsretain original source-derived layout. Catalogue runtime offsets do not enterrawTriggerpayload.',
        api=dict(file='native_ap_conditions.py',operations=['compare','evaluate'],scope='Dispatch/width projection from already measuredcounts;does not model a full world scanner or navigation'),
        limitations=['ExactDX only,not old/RC3 runtime equivalence.','No fullAP tick/walker/actions chain;static receiver path links examinedpredicate toAP object. Native output result saved separately before observer comparisons.','PlayerIDs must identify allocated native player slots;unknown/out-of-range references not emulated. Orderedsmallareas only except one fast65536areacase. Reversed bound loops can be unsafe and are not run.','Originalcreation ofmarklists,ownership changes,entity-list registration and possessionflag producers not executed. No higher-level names for all marker categories assigned.','AP04/08 gameplay remains open;this confirms specifictrigger consumers and newparameterbytes,not allAPbehavior.','Terrain08/04 consumer bitsverified;baseline flag names are retained with attribution,not automatically promoted nativeGUI labels.','All original game files and4EXEsunchanged;onlyresearch artifactswritten.'])
    assert len(s['native_emulations'])==61 and len(s['primary_program_bytes'])==12 and len(s['primary_data_ranges'])==4
    (ROOT/'action-point-condition-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('AP conditions:61native,7conditions,',s['corpus']['ap_condition_records'],'realnodes;',len(s['corpus']['nonzero_previously_reserved_filters']),'nonzero old-reservedfilters')
if __name__=='__main__':main()
