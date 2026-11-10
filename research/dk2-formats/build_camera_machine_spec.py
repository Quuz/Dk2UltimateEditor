"""Identify camera global759AEC from native Machine Spec Level setter/string."""
from pathlib import Path
import json,struct,pefile
from build_effect_semantics import pe_read,asm_range
from build_keeper_pipeline import digest
from build_placed_creatures import whole
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def primary():
    out=[]
    for r in load('phase46-primary-manifest.json'):
        p=ROOT/'ghidra'/r['before'];a=ROOT/'ghidra'/r['after'];m=ROOT/'ghidra'/r['memory'];assert p.read_text().rstrip()==a.read_text().rstrip();v=json.loads(m.read_text());b=bytes(v['data']);assert len(b)==v['length']==r['length'] and int(v['address'],16)==r['address'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length']);out.append({**r,'before_sha256':digest(p.read_bytes()),'memory_sha256':digest(m.read_bytes()),'program_bytes_sha256':digest(b)})
    return out

def native_data():
    exe=ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe';b=exe.read_bytes();rows=[]
    for a,n in [(0x66F560,4),(0x6BDA30,19),(0x66F5A0,396)]:
        p=ROOT/'ghidra'/('phase46-data-'+hex(a)[2:]+'.txt');m=json.loads(p.read_text());data=bytes(m['data']);assert int(m['address'],16)==a and m['length']==n and len(data)==n and data==pe_read(b,a,n);rows.append(dict(address=a,length=n,bytes_hex=data.hex(),sha256=digest(data),live_file=p.name))
    assert struct.unpack('<I',bytes.fromhex(rows[0]['bytes_hex']))[0]==0x6BDA30 and bytes.fromhex(rows[1]['bytes_hex'])==b'Machine Spec Level\0'
    imports=[]
    for dll in pefile.PE(data=b).DIRECTORY_ENTRY_IMPORT:
        for i in dll.imports:
            if i.address in [0x66C010,0x66C018]:imports.append(dict(address=i.address,dll=dll.dll.decode(),name=i.name.decode()))
    assert {r['name'] for r in imports}=={'RegSetValueExA','RegCloseKey'}
    return dict(ranges=rows,imports=imports,profile_rows=[dict(index=i,raw_dwords=list(struct.unpack_from('<9I',bytes.fromhex(rows[2]['bytes_hex']),i*36))) for i in range(11)])

def main():
    s=dict(family='Things.Camera.runtime_dependency',tag=205,setting_name='Machine Spec Level',setting_address=0x759AEC,settings_object_address=0x759A78,settings_field_offset=0x74,setting_bytes=4,executable_sha256=load('executable-hash-verification.json'),primary_program_bytes=primary(),native_data=native_data(),native_emulations=load('phase46-camera-machine-spec-emulations.json')['cases'],
        evidence=[whole('phase46-spec-566520-disassemble_function.txt'),whole('phase46-spec-565cf0-disassemble_function.txt'),whole('phase46-link-449a30-disassemble_function.txt'),whole('phase46-link-562670-disassemble_function.txt'),whole('phase46-link-554a60-disassemble_function.txt'),whole('phase46-link-5621e0-disassemble_function.txt'),asm_range('phase46-setup-5a63b0-disassemble_function.txt',0x5A6751,0x5A677F)],
        identity='Native566520 stores inputDWORD at this+74 and writes native string Machine Spec Level via562670. With this=759A78 the actual address is759AEC, which full449A30 reads. Verified end-to-end41 native cases include28 setter->camera chains and13 distinct profile updates.',
        setter=dict(function='0x566520',store='Write thisDWORD74 before attempting to open registry; no range clamp in this setter. Emulated open fail and RegSetValueExA error do not undo memory value.',persistence='If563E30 succeeds, real562670 passes name Machine Spec Level, REG_DWORD type4, data size4 and exact32 input bits to imported RegSetValueExA. Real5621E0 calls RegCloseKey on the opened handle. All imports/open are fixtures, never real registry calls.',scope='This is one setter, not proof that every arbitrary value is valid across the whole game or can be entered through GUI. Real registry load/init fallback not executed.'),
        camera_rule=dict(function='0x449A30',unsigned_threshold=5,id_range=[1,18],expression='For accepted camera ID and unsigned Machine Spec Level<5: ZoomMax=low32(ZoomMin+trunc0(signed32(ZoomMax-ZoomMin)/3)); intermediate presenceDWORD20=1. For level>=5 copy original152-byte intermediate.',example=dict(zoom_min=3000,zoom_max=12000,level4_loaded_max=6000,level5_loaded_max=12000),non_destructive='Raw payload is unchanged by the projection. Preserve original maxZoom and all opaque bytes on file save; machine-dependent loaded bank is a separate view.',no_automatic_refresh='Store reads setting when449A30 runs. Already-filled preset slots are not proved to change merely because566520 changes memory.'),
        command_line_profile=dict(function='0x565CF0',option='-SPEC',range='unsigned0..10 only;11/FFFFFFFF are no-ops',table_address=0x66F5A0,stride=36,changed_settings_offsets=[0x44,0x4C,0x50,0x54,0x60,0x58,0x5C,0x68],rule='The -SPEC option in5A63B0 calls565CF0. It selects8 settings from a9-DWORD table row and does not assign Machine Spec Level at74. All11 table profiles and2 invalid indices compared over entireB4-byte settings object. Registry open was fixture-failed in these13 cases.',scope='Do not assume command-line -SPEC4 changes global759AEC to4. Native interface labels of the8 settings retained in source disassembly; no performance/device recommendation implied.'),
        api=dict(file='native_cameras.py',function='preset_for_machine_spec',legacy='preset_projection(raw,global_setting=...) retained; parameter now identified as Machine Spec Level',authoritative_raw=True),
        limitations=['41 native cases:28 complete566520 setter -> full449A30 store chains and13 complete565CF0 profiles. SettingsB4 and entire camera bankC00 plus32 captured registry bytes compared for chains; profiles compare entireB4.',
            'Memory-only adapters connect functions.563E30 registry open and imported RegSetValueExA/RegCloseKey are fixtures. Real wrappers554A60/562670/5621E0 execute. No real registry, game settings or files changed.',
            'Constructor563C50 and registry-open563E30 sources inspected and retained; not executed by these fixtures. Auto-detection/reset563FB0 and full command-line parser5A63B0 inspected only, not emulated.',
            'Only the camera dependency identity is newly resolved. Complete camera activation, rendered view, all Machine Spec Level effects, and remaining catalogue unknown meanings remain open.'])
    assert len(s['native_emulations'])==41
    (ROOT/'camera-machine-spec-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Machine Spec Level identity;41 native cases; camera dependency resolved')
if __name__=='__main__':main()
