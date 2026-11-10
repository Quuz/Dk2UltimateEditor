"""CreatureSpells native reference/UI/checksum evidence, exact inspected EXEs."""
from pathlib import Path
from collections import Counter
import hashlib,json,re
import dk2_formats as fmt
from inventory import DATA
from build_effect_semantics import pe_read
from build_placed_creatures import whole
from extract_editor_dialogs import extract
from native_spell_writer import FLAG_MODEL
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def primary():
    rows=[]
    for r in load('phase42-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==r['address'] and len(b)==m['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        rows.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return rows

def resources():
    data=(DATA/'DK2 Editor pro.exe').read_bytes();dialogs=extract(data);assert dialogs==load('editor-dialogs.json')['dialogs'];d=next(r for r in dialogs if r['resource_path']==[5,257,2057]);controls={c['id']:c for c in d['controls'] if c['id']!=0xFFFFFFFF}
    config=[(1,0x184,0x8C,0x95),(2,0x188,0x90,0x98),(4,0x18C,0x94,0x9A),(8,0x190,0xA8,0x9B),(32,0x194,0x98,0x96),(64,0x198,0x9C,0x97),(128,0x19C,0xA0,0x99),(256,0x1A0,0xA4,0x9C)]
    flags=[dict(mask=bit,model_offset=model,panel_offset=panel,control_id=ident,native_caption=controls[ident]['title'],native_label=controls[ident]['title'].replace('&',''),control=controls[ident]) for bit,model,panel,ident in config]
    assert [(x['mask'],x['model_offset']) for x in flags]==list(FLAG_MODEL)
    return dict(program_sha256=digest(data),dialog=d,dialog_id=257,constructor='0x442C40 pushes101',ddx='0x443010',flags=flags,combos=[dict(raw_offset=232,model_offset=367,panel_index_offset=120,id_array_pointer_offset=212,id_array_count_offset=216,control=controls[0x703],native_label='Alternative Shot',caption_association='Static caption adjacent in dialog257; same shot array as primary Shot control6FE.'),dict(raw_offset=233,model_offset=366,panel_index_offset=124,id_array_pointer_offset=232,id_array_count_offset=236,control=controls[0x704],native_label='Alternative Room',caption_association='Static caption adjacent in dialog257; room selector has distinct arrayE8/EC.')],scope='Dialog257 is selected by actual constructor. Duplicate template328 has hidden/disabled alternatives, but is not used to claim these controls hidden in this constructor.')

def corpus():
    rows=[];pairs=Counter();nonzero=[];missing=[]
    for row in load('spell-writer-semantics.json')['corpus']['files']:
        p=Path(row['file']);b=p.read_bytes();assert digest(b)==row['sha256'];parts=fmt.parse(b);sec=next(s for s in parts if s.signature==160)
        if p.name=='CreatureSpells.kwd':cats={name:next(s for s in fmt.parse((DATA/(name+'.kwd')).read_bytes()) if s.signature==sig) for name,sig in [('Shots',260),('Rooms',120)]}
        else:cats={name:next((s for s in parts if s.signature==sig),None) for name,sig in [('Shots',260),('Rooms',120)]}
        ids={name:{r[190 if name=='Shots' else 914] for r in s.records} for name,s in cats.items() if s};local=0
        for i,raw in enumerate(sec.records):
            pair=(raw[232],raw[233]);pairs[pair]+=1
            if any(pair):
                local+=1;nonzero.append(dict(file=str(p),record_index=i,creature_spell_id=raw[230],name=raw[:32].split(b'\0')[0].decode('cp1252'),alternative_shot_id=pair[0],alternative_room_id=pair[1]))
                for name,value in zip(['Shots','Rooms'],pair):
                    if value and name in ids and value not in ids[name]:missing.append(dict(file=str(p),record_index=i,target_catalogue=name,target_id=value))
        rows.append(dict(file=str(p),sha256=row['sha256'],records=len(sec.records),nonzero_reference_records=local,paired_catalogue_names=list(ids)))
    return dict(files=rows,file_count=len(rows),records=sum(pairs.values()),nonzero_reference_records=len(nonzero),reference_pairs=[dict(alternative_shot_id=k[0],alternative_room_id=k[1],records=v) for k,v in sorted(pairs.items())],nonzero_references=nonzero,missing_targets_in_available_paired_catalogues=missing,scope='Reference diagnostics are metadata only; a resolved ID does not prove gameplay use. Map bundles use their own catalogues; standalone uses neighbor standalone catalogues. No fallback to a different Globals bundle.')

def main():
    s=dict(family='CreatureSpells',record_bytes=266,executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=primary(),evidence=[whole(r['before']) for r in load('phase42-primary-manifest.json')],native_emulations=load('phase42-spell-alternative-emulations.json')['cases'],native_ui=resources(),corpus=corpus(),
        intermediate=dict(bytes=346,converter_written_extent_bytes=319,untouched_tail_offset=319,untouched_tail_bytes=27,game_initializer='0x56A490',editor_initializer='0x42A000',initialization='Both zero20 presenceDWORDs plus266-byte data block:80+264+2=346. Full initializers, direct converters and init->convert chains compared across entire346-byte buffer.',phase41_correction='Phase41 native converter fixtures compared a320-byte prefix; its intermediate_bytes320 description was incomplete. Actual346-byte structure was already the checksum length in phase22. All61 prior native cases stay retained and valid within their prefix scope; phase42 checks the entire object. Raw-file27-byte tailEF remains ignored; this is separate from intermediate27-byte tail13F.'),
        references=[dict(raw_offset=232,native_label='Alternative Shot',converter_presence_offset=40,intermediate_offset=312,model_offset=367,model_presence_test_offset=44,writer_raw_offset=232),dict(raw_offset=233,native_label='Alternative Room',converter_presence_offset=44,intermediate_offset=313,model_offset=366,model_presence_test_offset=40,writer_raw_offset=233)],
        editor=dict(model_callback='0x4A2260',fragment='0x4A23E4..0x4A2405',presence='Tests are crossed:28 gates Room139→model16E,2C gates Shot138→model16F. Complete binary converters set both so normal complete-record references preserve their own values. No normal-load value swap claimed.',writer='Full writer4A9730 from phase41 copies model16F→rawE8 and16E→rawE9; evidence included.',lookup='443C20 shot and443C50 room scan byte arrays, return first matching index or signed-1; no signed-byte special treatment for IDs128..255.',fill='4439A0 fragment443A69..443AA6 calls actual lookup helpers and stores shot index78/room index7C, including-1.',commit='443CF0 fragment443D72..443DA1 dereferences arrays with stored indices and assigns model16E/16F without a bounds check. Controlled index-1 reads provided guard byte at array-1; real preceding memory, GUI validation and resulting whole save not predicted.',builders='443370 builds byte-ID arrayD4 with leading0 none then globals54FB20/24 in physical order;443550 does same for arrayE8 from54F998/9C. Builder bodies reviewed statically, not fully emulated.',ui='Constructor442C40 selects dialog257. DDX443010 full native body captured12 combo/checkbox bindings with explicit external DDX fixtures. Dialog captions/resources re-extracted from exact PE. Full MFC runtime/UI not run.'),
        game=dict(loader='0x4F7860',definition_bytes=70,checksum_offset=27,checksum_bytes=4,checksum_gate='DWORD6FCDCF',rule='gate0: sum all346 intermediate unsigned bytes. gate!=0: retain previous checksum. Alternative bytes138/139 are not transferred to semantic definition fields; with gate0 changing them changes only checksum1B..1E in inspected loader.',scope='Complete native loader with null resource provider compared all70 bytes for four reference pairs, both gate values, varied unused intermediate tail and TPS. No claim that all other game code or builds ignore the raw fields.',distinction='RawEE AlternativeShot rule is separately transferred to runtime45 and dispatches eligibility predicates. It is not the same as rawE8 Alternative Shot ID.'),
        limitations=['63 native cases:6 full initializers,12 full converters/init-convert chains,12 full game loaders,12 editor model reference fragments,10 full byte-array lookups,4 panel fill fragments,6 commit fragments and1 complete DDX binding capture.',
            'Game loader resource provider is null; texture/icon loading branches not run. Checksums and70 definition bytes are original computation. Full intermediate comparison covers346 bytes; model/panel comparisons cover full supplied state.',
            'Panel/model fragments bypass prologue, construction, MFC validation, selection events and final array registration. Index-1 cases prove unchecked native read with an explicit preceding guard byte, not a universal crash or value in real UI.',
            'Native labels are verified editor semantics. Alternative ID gameplay activation/replacement remains unresolved. Metadata links and reference validity alone do not establish a game mechanic.',
            'No game process, GUI open/save or original file edits. Exact inspected hashes only; no assertions about Flame runtime patches or other executable versions.'])
    assert len(s['native_emulations'])==63
    s['phase55_pipeline']=load('spell-catalogue-validation.json')
    if (ROOT/'spell-predicate-semantics.json').exists():s['phase78_predicate_runtime']=load('spell-predicate-semantics.json')
    if (ROOT/'creature-shot-request-semantics.json').exists():s['phase79_cast_request_reference']=dict(file='creature-shot-request-semantics.json',sha256=digest((ROOT/'creature-shot-request-semantics.json').read_bytes()))
    if (ROOT/'native-shot-success-semantics.json').exists():s['phase80_native_success_reference']=dict(file='native-shot-success-semantics.json',sha256=digest((ROOT/'native-shot-success-semantics.json').read_bytes()))
    if (ROOT/'targeted-spell-semantics.json').exists():s['phase82_targeted_cast']=load('targeted-spell-semantics.json')
    (ROOT/'spell-alternative-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('CreatureSpells alternatives:',len(s['native_emulations']),'native cases;',s['corpus']['records'],'records;',s['corpus']['nonzero_reference_records'],'nonzero references;',len(s['corpus']['missing_targets_in_available_paired_catalogues']),'missing targets')
if __name__=='__main__':main()
