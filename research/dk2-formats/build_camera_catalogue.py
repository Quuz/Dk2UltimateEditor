"""Camera records following Variables:separate Pro load/save path and RC3 UI."""
from pathlib import Path
import json,struct
from build_keeper_pipeline import digest
from build_effect_semantics import pe_read
from build_placed_creatures import whole
from native_camera_catalogue import bank_projection
from emulate_camera_catalogue import real_records
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
SCOPE_UPDATE='Phase49:the110 Things-only corpus did not cover GlobalVariables.kwd,whose secondBE section has39 realCD records. Pro47D5F0 optionally registers49F550 atA8;Pro4AB210 separately writes cameras after Variables via47CF90. Earlier48B5F0/4947C0 omission applies only to the map-Things path. See camera-catalogue-semantics.json;do not infer global absence or inability of Pro to write cameras.'

def primary():
    rows=[]
    for r in load('phase49-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];m=ROOT/'ghidra'/r['memory'];assert p.read_text().rstrip()==a.read_text().rstrip();v=json.loads(m.read_text());b=bytes(v['data']);assert len(b)==v['length']==r['length'] and int(v['address'],16)==r['address'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);rows.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(b)})
    return rows

def main():
    ps=primary();survey=load('phase49-camera-corpus-manifest.json');rs=real_records();assert len(rs)==39
    bank=bank_projection(rs,5,bytes([165])*0xC00);reverse=bank_projection(rs[::-1],5,bytes([165])*0xC00)
    flags=lambda b:struct.unpack_from('<I',b,0x28+13*152+0x8C)[0]
    assert flags(bank)==42 and flags(reverse)==34 and bank!=reverse
    comparison=load('phase49-editor-dialog-comparison.json');rc3=next(v for v in comparison if v['file'].endswith('rc3.exe'));assert digest(Path(rc3['file']).read_bytes())==rc3['sha256']==digest((ROOT/'source-binaries/DK2-Editor-rc3.exe').read_bytes())
    dialog=next(v for v in rc3['dialogs'] if v['resource_path'][1]==333);assert dialog['title']=='Modify Camera Properties'
    fields=[]
    for name,raw,panel,ids,limits,signed in [('position',0,0xB8,[1894,1896,1898],[0,32000],True),('min_clip',12,0xD0,[1926,1928,1930],[0,32000],True),('max_clip',24,0xE8,[1932,1934,1936],[0,32000],True),('view',36,0x60,[1886,1892,1904],[-64000,64000],True),('zoom',48,0x78,[1888,1902,1924],[-64000,64000],True),('lens',60,0x90,[1890,1900,1922],[0,4096],False)]:
        fields.append(dict(name=name,raw_offsets=[raw+i*4 for i in range(3)],model_offsets=[4+raw+i*4 for i in range(3)],panel_offsets=[panel+i*8 for i in range(3)],control_ids=ids,ddv_range=limits,editor_interpretation='signedDWORD/4096' if signed else 'unsignedDWORD/4096'))
    s=dict(family='Camera catalogue within multi-section Variables files',tag=205,payload_bytes=84,raw_authoritative=True,primary_program_bytes=ps,evidence=[whole(r['before']) for r in ps],native_emulations=load('phase49-camera-catalogue-emulations.json')['cases'],scope_update=SCOPE_UPDATE,
        corpus=dict(manifest='phase49-camera-corpus-manifest.json',manifest_sha256=digest((ROOT/'phase49-camera-corpus-manifest.json').read_bytes()),source_files_scanned=survey['file_count'],strict_parse_rejected=len(survey['strict_parse_rejected']),camera_files=survey['camera_files'],camera_records_counting_export_copies=survey['camera_records'],camera_sections=survey['camera_sections'],scope=survey['scope'],original_global_variable_records=621,original_camera_records=39,original_distinct_ids=18,duplicate_ids={str(i):4 for i in range(12,19)},export_copy_rule='Three camera files have identical whole-file SHA256. They contain39 records each;117 is not117 different presets. Strict rejected inputs were not repaired or declared camera-free.'),
        pro=dict(load='47D5F0 initializes context via42C730. It always registers Variables callbacks49EC50/49EC90 atD8/DC;only when BYTE argument!=0 writes camera callback49F550 atA8. Tested init+registration fragment6 cases;full file-opening routine static only.',
            callback='49F550 allocates88,real41CB70 constructs vtable50ACE8+84 zero bytes,then copies84 contiguous bytes from mid44 to model+4,and inserts via482750. Presence flags are not individually tested in this copy callback. Allocator and insertion are fixtures in chain tests.',
            save='47CF90 calls4AA6E0 Variables writer,then4AB210 camera Things-section writer on the same stream. Whole47CF90 is static evidence,not emulated. Camera writers execute in26 cases across Pro/RC3 and46 Pro chains.',
            separate_map_path='48B5F0 has cameraA8 zero;4947C0 writes C2..CC. These earlier findings remain correct for map Things,not a statement that Pro lacks all camera save paths.'),
        serialization=dict(pro_writer='0x4AB210',rc3_writer='0x4B0A20',header_bytes=56,section_signature=190,section_info_tag=191,data_tag=192,record_tag=205,record_payload_bytes=84,record_physical_bytes=92,pro_array=0x60C,pro_count=0x610,rc3_array=0x608,rc3_count=0x60C,
            header='N=context count;S=56+92*N;D=92*N. InfoSize28,versionDWORD24=1. Even0 records emits56-byte section. Both full writers enumerate array inphysical order and emit every entry asCD84 without ID sorting/filtering/renumbering or deduplication.',
            model_layout='Camera object88=vtableDWORD+84 contiguous raw-layout bytes. Full writer emits model[4:88],including last byte87/raw53,allFlags andBYTE ID. Null pointers are not safely filtered by this writer;not emulated.',
            native_projection='Parser431220 clears152-byte mid and converter42FE20 omits raw53.49F550 copies mid44..97. Therefore normal parser->model->writer yields0 for raw53 and normalizes every shorter payload to84 bytes. All39 observed records have raw53=0,so this loss requires synthetic nonzero-tail fixture to expose.',
            timestamp_scope='Date callbacks are9-byte fixture writes in emulations. Header37/47 retain seeded scratch bytes under thatfixture. Actual date helper is not executed;do not infer all real-clock timestamp or padding bytes from fixture output. Independent editor preserves original56-byte header unless a size/count edit is requested.'),
        ordered_game_load=dict(callback='0x5215F0',store='0x449A30',cases=4,records_per_case=39,orders=['original','reversed'],machine_spec_levels=[4,5],definition='Full39 callbacks and stores execute per case,with manager-provider/checksum fixtures. Whole0xC00 bank compared. ID13 Flags after original order=0x2A;after reverse=0x22. Only last record foraccepted ID remains in bank. This is an ordered camera-specific rule,not universal KWD duplicate handling.',limits='Only1..18 accepted by this exact preset store. Do not delete raw duplicates,ID0 or19..255 in a file-preserving editor;checksum and input-order scope remain separate.'),
        rc3=dict(original_file=rc3['file'],research_copy=str(ROOT/'source-binaries/DK2-Editor-rc3.exe'),sha256=rc3['sha256'],dialog=dialog,ddx='0x437790',fill='0x437D50',commit='0x437F80',fields=fields,angles=dict(raw_offsets=[76,78,80],model_offsets=[80,82,84],panel_offsets=[168,172,176],control_ids=[1916,1918,1920],labels=['XY','YZ','XZ'],ddv_range=[0,4096],scope='Native GUI integers,not independently measured degrees. No GUI clamp imposed on rawWORD codec.'),checkboxes=[dict(mask=1<<i,control_id=ident,label=label,setter=hex(0x427250+i*32)) for i,(ident,label) in enumerate([(1906,'Angle XY'),(1907,'Angle YZ'),(1908,'Angle XZ'),(1909,'Zoom'),(1910,'View Distance'),(1911,'Lens'),(1912,'Position')])],checkbox_rule='Fullsetter instructions test DWORD nonzero and set/clear selected bit of model4C while preservingall other bits. Seven checked controls;GUI fill/DDX/commit statically traced,not executed. Commit multiplies panel doubles by4096 and calls4B9850;arbitrary fractional ftol rounding not certified.'),
        api=dict(file='native_camera_catalogue.py',operations=['model_from_raw','editor_scaled_view','apply_checkboxes','writer_section','bank_projection'],normalization='model_from_raw andwriter_section are explicitly scoped native projections. Lossless roundtrip usesdk2_formats.Section/raw-record preservation;never silently apply model53=0.'),
        negative_search=dict(file='phase49-old-terrain-candidates.json',rule='Old exact183/1B5 candidates searched as alternate Terrain lead;no new verifiedTerrain consumer established. This doesnot prove global non-use. Rc3Terrain dialog143 identical toPro;no new198/224 label established. Wrongboundary47E550 discovery excluded fromprimary manifest.'),
        limitations=['82 native cases:26 complete writers(13perEXE,includingfull39-record catalogue),46 fullPro parser/callback/constructor/writer chains(39real+7synthetic),6 registration fragments,4 ordered game-bank sequences of39 records. All emitted file bytes orwhole bank compared.',
            'File read/write,date,allocation,pointer-array insertion andcamera-manager/checksum providers arememory-only fixtures. Native parsers/converter/constructor/callback/writers/getters execute;full GUI/file-opening/global save routines andrenderer do not.',
            'RC3 GUI scaling/ranges and7 checkbox bindings arestatic primary-instruction evidence,not native numerical UI runs. No universal angle units orscreen brightness/visibility semantics inferred.',
            '3930source files include nested parallel-editor acceptance/export artifacts. They were read only,not edited;copies counted separately andidentical camera files explicitly distinguished.',
            'Three AutoSave historical drifts usephase47 snapshots. No restoration orgame-data/EXE modification. Terrain198/224 andother unresolved gameplayfields remain open.'])
    assert len(s['native_emulations'])==82
    updates=load('phase49-survey-updates.json');s['corpus']['post_survey_updates']=dict(manifest='phase49-survey-updates.json',sha256=digest((ROOT/'phase49-survey-updates.json').read_bytes()),independent_current_snapshots=updates['changed_sources'],rule='Three extra AutoSave Players/Triggers/Variables changed after firstsurvey. Replay uses their later captured bytes;doesnotclaim originalsurvey bytes were recovered orall3930 remained unchanged.')
    (ROOT/'camera-catalogue-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    old=load('camera-semantics.json');old['later_scope_update']=SCOPE_UPDATE;(ROOT/'camera-semantics.json').write_text(json.dumps(old,ensure_ascii=False,indent=2),encoding='utf8')
    print('Camera catalogue:82 native cases;39original records;25 primary ranges;3sourcecopies')
if __name__=='__main__':main()
