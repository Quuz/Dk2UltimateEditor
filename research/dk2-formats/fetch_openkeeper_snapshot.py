"""Pin reference sources for offline analysis; never execute downloaded code."""
from pathlib import Path
import hashlib, io, json, urllib.request, zipfile

ROOT = Path(__file__).resolve().parent / 'sources'
PIN = '59c1966b80b589cf397476b421fd779527b8e2c4'

def fetch(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'DK2-format-research'})
    with urllib.request.urlopen(req, timeout=90) as response:
        return response.read()

head = json.loads(fetch('https://api.github.com/repos/tonihele/OpenKeeper/commits/master'))
(ROOT / 'openkeeper-head-check.json').write_text(json.dumps({
    'checked_date': '2026-10-22', 'head': head['sha'], 'pinned': PIN,
    'head_commit_date': head['commit']['committer']['date']}, indent=2), encoding='utf8')
archive = fetch('https://codeload.github.com/tonihele/OpenKeeper/zip/' + PIN)
dest = ROOT / ('openkeeper-' + PIN[:12])
count = 0
with zipfile.ZipFile(io.BytesIO(archive)) as z:
    for member in z.infolist():
        rel = Path(*Path(member.filename).parts[1:])
        if not member.is_dir() and (rel.parts[0] == 'src' or str(rel) in ['README.md', 'build.gradle', 'license.txt', 'LICENSE']):
            target = dest / rel
            assert target.resolve().is_relative_to(dest.resolve())
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(z.read(member))
            count += 1
(ROOT / 'openkeeper-source-snapshot.json').write_text(json.dumps({
    'commit': PIN, 'archive_sha256': hashlib.sha256(archive).hexdigest(),
    'extracted_files': count, 'directory': dest.name, 'archive_bytes': len(archive)}, indent=2), encoding='utf8')
print('head', head['sha'], 'snapshot', count, 'files;', len(archive), 'archive bytes')
