"""King lightning immunity: byte evidence, both EXE chains, scoped native runs."""
from pathlib import Path
import hashlib,json,struct
import dk2_formats as fmt
from build_effect_semantics import pe_read
from build_placed_creatures import whole
from native_lightning_immunity import flag_value
ROOT=Path(__file__).resolve().parent
OLD=ROOT.parent.parent/'DKII 13/Dungeon Keeper 2'
CURRENT=ROOT.parent.parent/'Dungeon Keeper 2'
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()

def primary():
    out=[]
    for r in load('phase36-primary-manifest.json'):
        before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];memory=ROOT/'ghidra'/r['memory'];assert before.read_text().rstrip()==after.read_text().rstrip()
        m=json.loads(memory.read_text());b=bytes(m['data']);assert int(m['address'],16)==r['address'] and m['length']==r['length']==len(b) and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
        out.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(memory.read_bytes()),'program_bytes_sha256':digest(b)})
    return out

def sources():
    rows=[]
    for base in [OLD,CURRENT]:
        for relative in ['Data/editor/Creatures.kwd','Data/editor/AutoSaveGlobals.kld','Data/editor/KeeperSpells.kwd','Data/editor/Shots.kwd']:
            p=base/relative;b=p.read_bytes();row=dict(file=str(p),sha256=digest(b),bytes=len(b))
            for s in fmt.parse(b):
                if s.signature==170:
                    for i,raw in enumerate(s.records):
                        if raw[0xF4B]==21:
                            row['king']=dict(name=raw[:32].split(b'\0',1)[0].decode('ascii'),creature_id=21,record_index=i,stride=len(raw),record_file_offset=s.offset+len(s.header)+i*len(raw),game_flag_file_offset=s.offset+len(s.header)+i*len(raw)+0x1545,game_flags=flag_value(raw),game_flags_hex=raw[0x1545:0x1549].hex(),immune_bit=bool(flag_value(raw)&1),editor_flags=flag_value(raw,'editor') if len(raw)>=0x159D else None)
                elif s.signature==150:
                    for raw in s.records:
                        if raw[:32].split(b'\0',1)[0]==b'Thunderbolt':row['thunderbolt']=dict(keeper_spell_id=raw[0xEE],shot=raw[0xF0],bonus_shot=raw[0x113])
                elif s.signature==260:
                    row['process9_shots']=[dict(id=raw[0xBE],name=raw[:32].split(b'\0',1)[0].decode('ascii'),process=raw[0xC7]) for raw in s.records if raw[0xC7]==9]
            rows.append(row)
    p=CURRENT/'Data/editor/maps/kop4Globals.kld';b=p.read_bytes();row=dict(file=str(p),sha256=digest(b),bytes=len(b))
    for s in fmt.parse(b):
        if s.signature==170:
            raw=next(raw for raw in s.records if raw[0xF4B]==21);row['king']=dict(stride=len(raw),game_flags=flag_value(raw),editor_flags=flag_value(raw,'editor'))
    rows.append(row)
    p=OLD/'Data/editor/maps/Level20.kwd';b=p.read_bytes();s=fmt.parse(b)[0];data=s.records[0];df=fmt.u32(s.header,len(s.header)-4);refs=[]
    for i in range(fmt.u16(s.header,20)):
        rec=data[df+8+72*i:df+8+72*(i+1)];refs.append(dict(type=fmt.u32(rec),path=rec[8:72].split(b'\0',1)[0].decode('ascii')))
    rows.append(dict(file=str(p),sha256=digest(b),bytes=len(b),references=refs))
    return rows

def main():
    rows=sources();versions=[]
    for label,path,converter,loader,handler in [('local13',OLD/'DKII.exe',0x55F170,0x48A150,0x4A60E0),('current',CURRENT/'DKII-DX.exe',0x568E30,0x492320,0x4AF1E0)]:
        versions.append(dict(label=label,file=str(path),sha256=digest(path.read_bytes()),converter=hex(converter),type_loader=hex(loader),lightning_handler=hex(handler)))
    result=dict(family='Creatures lightning immunity',raw_game_flags_offset=0x1545,raw_game_flags_bytes=4,mask=1,intermediate_offset=0x1935,runtime_definition_offset=0x68A,king_creature_id=21,versions=versions,
        version_provenance='local13 means the actual EXE provided in D:/Games/DK2/DKII 13 directory, copied without modification for Ghidra. PE has no version-resource number; folder name alone is not independent certification of every historical1.3 build.',
        chain='Both converters read raw1545 and OR into intermediate1935. Corresponding type loader copies DWORD to definition68A. Full Process9 handler tests lowbyte mask1 of the target creature definition; when set, bypasses the health-application call and its later shock/state branch.',
        assignment='King bit is already stored in the data record, not a hardcoded CreatureId21 special case in either inspected lightning handler. Other creatures receive the same gate when their definition bit1 is set; separate ID7 cases confirm this in native fixtures.',
        spell_link='In both standalone catalogue sets, KeeperSpells Thunderbolt id6 normal/bonusShot21 -> Shots Thunderbolt id21 ProcessType9. Shots30 Lightning Shot and59 Lightning Trap Shot also select Process9. This does not claim immunity to every attack whose name contains lightning or any separate general DamageType path.',
        editor_slot='Long5537 records have a separate editor slot1599. Provided Pro writer uses that slot; game converter for these two EXEs uses1545. Never mirror automatically. UI resources246/322 contain Immune To Lightning control1653; this phase does not re-run full UI apply/save.',
        observed_data=rows,primary_program_bytes=primary(),evidence=[whole(r['before']) for r in load('phase36-primary-manifest.json')],native_emulations=load('phase36-lightning-emulations.json')['cases'],
        evidence_strength=dict(storage='Exact bytes, IDs, record strides and paths checked directly; valid for the hashed sources.',control_flow='22 full native-handler emulations over both hashed executables, with real lookup/flag gate/arithmetic and explicit event/health fixtures.',gameplay='Real game not run with modified data; health helper/state/world integration and runtime patches not simulated.',probability='No statistically calibrated99.9% claim. Confidence is scoped by evidence type and exact executable/data versions.'),
        limitations=['Both supplied EXEs retain the immunity check; do not infer it was removed from all later releases.',
            'King standalone flags1 vs inspected Globals flags0 is an actual data difference. A given running map must be traced to its chosen catalogue before claiming the cause of observed vulnerability.',
            'Event helper runs before immunity check even when immune; native fixtures capture the call count, not its complete visual/audio/game effects.',
            'Full health application is a captured fixture; nonimmune fixture has nonzero existing shock/state field to avoid unrelated aggro branches. Actual shock-timer assignment is checked.',
            'local13 exact version provenance qualified above; original/copy SHA checked. Only research Ghidra database functions created, no binary edits.',
            'All raw patch checks are in memory; original EXEs and data never modified.'])
    (ROOT/'creature-lightning-immunity.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print('Lightning immunity:',len(result['native_emulations']),'native runs;',len(rows),'data sources;',len(result['primary_program_bytes']),'PE ranges')
if __name__=='__main__':main()
