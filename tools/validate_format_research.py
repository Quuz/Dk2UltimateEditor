"""Validate the portable research snapshot; no game installation or Ghidra required."""
from pathlib import Path
import ast,gzip,hashlib,json,re,zipfile
from assemble_format_package import assemble,REPO,RESEARCH


def main():
    manifest=json.loads((RESEARCH/'snapshot-manifest.json').read_text(encoding='utf-8'))
    total=0
    for row in manifest['files']:
        p=RESEARCH/row['file'];data=p.read_bytes()
        assert p.resolve().is_relative_to(RESEARCH.resolve()),row['file']
        assert len(data)==row['bytes'] and hashlib.sha256(data).hexdigest()==row['sha256'],row['file']
        if p.suffix=='.json':json.loads(data.decode('utf-8-sig'))
        if p.suffix=='.py':ast.parse(data.decode('utf-8-sig'),filename=row['file'])
        if p.suffix=='.gz':json.loads(gzip.decompress(data).decode('utf-8-sig'))
        total+=len(data)
    bundle=assemble();package=json.loads(bundle)
    assert len(package['components'])==manifest['component_count'] and manifest['component_count']>=171
    assert package['gold_capacity_margin_semantics']['field']['confirmed_alias']=='GOLD_CAPACITY_MARGIN'
    assert package['gold_capacity_margin_semantics']['field']['historical_alias']=='GAME_TICKS'
    assert package['crypt_pointer_forwarding_semantics']['counts']['Crypt_counter_meanings_remaining']==10
    assert package['gold_capacity_margin_semantics']['priority']['block_complete'] is False
    evidence=RESEARCH/'primary-evidence.zip'
    assert hashlib.sha256(evidence.read_bytes()).hexdigest()==manifest['primary_evidence']['sha256']
    with zipfile.ZipFile(evidence) as z:
        assert z.testzip() is None
        assert len(z.infolist())==manifest['primary_evidence']['members']
        for info in z.infolist():
            p=Path(info.filename)
            assert not p.is_absolute() and '..' not in p.parts and p.suffix in ['.txt','.bin']
    if 'editor_variables_writer_semantics' in package:
        writer=package['editor_variables_writer_semantics']
        assert writer['counts']['native_scenes']==22 and writer['counts']['expected_division_faults']==2
        assert writer['counts']['Ghidra_known_FISTP_DWORD_differences']==7
        assert writer['counts']['whole_native_memory_equal_Ghidra_controls']==0
        assert writer['numeric_model']['fixed_point_types']==[67,155,158,185,220,221]
        assert writer['derived74']['Terrain_MaxHealth']['raw_offset']==0x1FF
        assert writer['derived74']['direct_Map_double74_ignored'] is True
        assert writer['physical_preservation']['opaque_record_append_remove_controls']==2
        assert writer['physical_preservation']['exact_scalar_value_edits']==10
        with zipfile.ZipFile(evidence) as z:
            for row in writer['primary']['functions']+writer['primary']['blocks']:
                for key in ['memory','assembly']:
                    if row.get(key):assert hashlib.sha256(z.read(Path(row[key]).name)).hexdigest()==row[key+'_sha256']
            ref=writer['Ghidra_FPU_limitation']['primary']
            assert hashlib.sha256(z.read(Path(ref['file']).name)).hexdigest()==ref['sha256']
    for p in RESEARCH.iterdir():assert p.suffix.lower() not in ['.exe','.dll','.kwd','.kld','.xlsx','.log'],p.name
    # Check token-shaped strings, without printing any matching value.
    secret=re.compile(rb'(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,}|sk-proj-[A-Za-z0-9_-]{40,}|AKIA[A-Z0-9]{16})')
    for p in RESEARCH.iterdir():
        if p.is_file() and p.suffix in ['.py','.json','.md','.txt']:assert not secret.search(p.read_bytes()),'Credential-shaped string in '+p.name
    print('PASS:',len(manifest['files']),'source files;',manifest['component_count'],'components;',total,'source bytes;',len(bundle),'assembled bytes;',manifest['primary_evidence']['members'],'primary evidence members')


if __name__=='__main__':main()
