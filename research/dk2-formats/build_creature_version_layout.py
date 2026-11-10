"""Compare exact decoder bytes, native strings and3 version-specific offsets."""
from pathlib import Path
import json
from build_effect_semantics import pe_read,asm_range
from build_placed_creatures import whole
from build_keeper_pipeline import digest
from native_creature_version_layout import CURRENT,OLD,PROFILES
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def primary():
    out=[]
    for r in load('phase47-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];m=ROOT/'ghidra'/r['memory'];assert p.read_text().rstrip()==a.read_text().rstrip();v=json.loads(m.read_text());b=bytes(v['data']);assert len(b)==v['length']==r['length'] and int(v['address'],16)==r['address'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);out.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(b)})
    return out

def native_labels():
    out=[]
    for r in load('phase47-label-manifest.json'):
        p=ROOT/'ghidra'/r['memory'];m=json.loads(p.read_text());b=bytes(m['data']);assert b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']) and b==r['text'].encode()+b'\0';out.append({**r,'sha256':digest(b),'memory_sha256':digest(p.read_bytes())})
    assert sorted(r['text'].strip() for r in out)==sorted(['PartyId %d','WanderRadius %d','TriggerRoot %d']*2)
    return out

def converter_pairs():
    old=(ROOT/'source-binaries/DKII-local13.exe').read_bytes();current=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes();assert digest(old)==OLD and digest(current)==CURRENT
    manifest=load('phase47-primary-manifest.json');out=[]
    for tag,a,b,n,mid in [(198,0x565630,0x56F330,24,56),(199,0x565730,0x56F430,32,84),(200,0x565840,0x56F540,28,64)]:
        ra=next(r for r in manifest if r['program']=='DKII-local13.exe' and r['address']==a);rb=next(r for r in manifest if r['program']=='DKII-DX.exe' and r['address']==b);assert ra['length']==rb['length'];x=pe_read(old,a,ra['length']);assert x==pe_read(current,b,rb['length']);out.append(dict(tag=tag,raw_payload_bytes=n,intermediate_bytes=mid,old_converter=a,current_converter=b,instruction_bytes=ra['length'],identical_sha256=digest(x)))
    return out

def main():
    ps=primary();s=dict(family='Things.PlacedCreatures.executable_profiles',executable_profiles=[dict(sha256=k,instance_fields=v) for k,v in PROFILES.items()],old_source=dict(path=str(ROOT/'source-binaries/DKII-local13.exe'),original_path='D:/Games/DK2/DKII 13/Dungeon Keeper 2/DKII.exe',sha256=OLD,version_scope='User folder label13 only; no independent numeric version certification. Use exact hash, not guessed version-number detection.'),primary_program_bytes=ps,native_labels=native_labels(),converter_pairs=converter_pairs(),native_emulations=load('phase47-creature-version-emulations.json')['cases'],
        identity='Current dump490240 and old488270 print3 native labels. Current offsets:PartyId BYTE417, WanderRadius BYTE418, TriggerRoot WORD419. Old:BYTE416/BYTE417/WORD418. Diagnostic TriggerRoot uses sign extension, soFFFF prints-1; this is not a raw-file signed-ID constraint.',
        converter_rule='All3 complete raw-to-intermediate decoder bodies are byte-for-byte identical between inspected EXEs, includingRET. C6payload24->mid56,C7payload32->mid84,C8payload28->mid64. Raw scalar widths, all intermediate bytes, and OR flags behavior verified for seeds0/A5 and raw00/FF/pattern.',
        separation='These are3 offsets inside runtime creature instances, not file offsets or Creature definitions. Do not shift rawC6/C7/C8, definition6EB/75C or every other runtime field by1.',
        unknowns='Old exact-displacement scans retained in phase47-old-creature-candidates.json and phase47-old-wander-candidates.json. Old418 WORD dump read was TriggerRoot, not WanderRadius. They establish no new gameplay consumer/unit for AbilityJump/PickUpPriority/WanderRadius, nor proof of non-use.',
        api=dict(file='native_creature_version_layout.py',operation='read_instance_fields',profile_key='Exact executable SHA-256 only',raw_codec='native_placed_creatures.py unchanged; original bytes authoritative'),
        evidence=[*[whole(r['before']) for r in ps if r['address'] not in [0x488270,0x490240]],asm_range('phase47-primary-DKII-local13.exe-488270-disassemble_function.txt',0x488CAB,0x488CF1),asm_range('phase47-primary-DKII-DX.exe-490240-disassemble_function.txt',0x490CD8,0x490D1E)],
        input_capture=dict(file='phase47-input-manifest.json',sha256=digest((ROOT/'phase47-input-manifest.json').read_bytes()),scope='Three AutoSave files differ from historical hashes during concurrent editors. Changed bytes preserved as research snapshots; original files untouched. Each snapshot independently captured, no atomic bundle claimed.'),
        limitations=['48 native cases:36 complete converters (18 per EXE),12 diagnostic fragments (6 per EXE). All56/84/64 intermediate bytes and3 captured (native format-string pointer, value) records compared.',
            'Diagnostic fragment enters after prior unrelated print cleanup and exits before next print. Logger fixture captures arguments, not real textual rendering. Entire dump functions/constructors/gameplay not executed.',
            'Equal converter bodies do not prove every parser, loader, game behavior or editor-save path is compatible across the two executables. Do not infer full version compatibility or original1.3 certification.',
            'Definition25/6EB and WanderRadius gameplay meaning remain unresolved. Bounded candidate scans are evidence of searched paths, not global non-use. No new gameplay field name/unit assigned.',
            'No research writes to original game data/EXEs. Three AutoSave inputs changed relative to baseline while both editors were observed running; exact writer not identified. They were snapshotted under research directory and not restored or repaired.14 other main inputs and3 EXEs match historical hashes.'])
    assert len(s['native_emulations'])==48
    previous=ROOT/'creature-version-layout-semantics.json'
    if previous.exists():
        saved=json.loads(previous.read_text(encoding='utf8'))
        if 'phase64_search_review' in saved:s['phase64_search_review']=saved['phase64_search_review']
    (ROOT/'creature-version-layout-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Version layout:48 native cases;3 identical full converters;3 shifted runtime fields')
if __name__=='__main__':main()
