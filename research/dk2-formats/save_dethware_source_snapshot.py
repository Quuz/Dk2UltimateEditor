"""Archive the pinned Dethware sources and verify every Git blob identity."""
import hashlib
from io import BytesIO
import json
from pathlib import Path, PurePosixPath
from urllib.request import Request, urlopen
import zipfile

ROOT = Path(__file__).resolve().parent
ARCHIVE = ROOT / 'dethware-DK2-28adde55-source.zip'
INDEX = json.loads((ROOT / 'dethware-source-index.json').read_text(encoding='utf-8-sig'))
EXPECTED = {row['path']: row['git_blob'] for row in INDEX['files']}


def verify(files):
    assert set(files) == set(EXPECTED)
    for name, raw in files.items():
        git_header = b'blob ' + str(len(raw)).encode() + b'\0'
        assert hashlib.sha1(git_header + raw).hexdigest() == EXPECTED[name], name


def main():
    if ARCHIVE.exists():
        with zipfile.ZipFile(ARCHIVE) as archive:
            assert len(archive.infolist()) == len(set(archive.namelist()))
            files = {name: archive.read(name) for name in archive.namelist()}
    else:
        url = f"https://api.github.com/repos/Dethware/DK2/zipball/{INDEX['revision']}"
        request = Request(url, headers={'User-Agent': 'DK2-format-research'})
        with urlopen(request, timeout=30) as response:
            payload = response.read(8 * 1024 * 1024 + 1)
        assert len(payload) <= 8 * 1024 * 1024
        with zipfile.ZipFile(BytesIO(payload)) as upstream:
            files = {}
            for member in upstream.infolist():
                if member.is_dir():
                    continue
                name = str(PurePosixPath(*PurePosixPath(member.filename).parts[1:]))
                assert name in EXPECTED, name
                files[name] = upstream.read(member)
        verify(files)
    verify(files)
    packed = BytesIO()
    with zipfile.ZipFile(packed, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name, raw in sorted(files.items()):
            entry = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            entry.create_system = 3
            entry.external_attr = 0o644 << 16
            entry.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(entry, raw, compresslevel=9)
    if not ARCHIVE.exists() or ARCHIVE.read_bytes() != packed.getvalue():
        ARCHIVE.write_bytes(packed.getvalue())
    manifest = {
        'revision': INDEX['revision'], 'source_archive': ARCHIVE.name,
        'sha256': hashlib.sha256(ARCHIVE.read_bytes()).hexdigest(),
        'files': [{'path': name, 'sha256': hashlib.sha256(raw).hexdigest(),
                   'bytes': len(raw), 'git_blob': EXPECTED[name]}
                  for name, raw in sorted(files.items())],
    }
    path = ROOT / 'dethware-source-snapshot.json'
    path.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf8')
    print('Verified and archived', len(files), 'source files')


if __name__ == '__main__':
    main()

