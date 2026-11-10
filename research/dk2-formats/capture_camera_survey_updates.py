"""Preserve observed source drift after first read-only broadcamera survey."""
from pathlib import Path
from datetime import datetime,timezone
import json,hashlib
ROOT=Path(__file__).resolve().parent
def digest(b):return hashlib.sha256(b).hexdigest()
def main():
    dst=ROOT/'phase49-survey-updates.json'
    if dst.exists():
        m=json.loads(dst.read_text(encoding='utf8'))
        for r in m['changed_sources']:assert digest(Path(r['snapshot']).read_bytes())==r['snapshot_sha256']
        print('Existing independent drift captures verified:',len(m['changed_sources']));return
    survey=json.loads((ROOT/'phase49-camera-corpus-manifest.json').read_text(encoding='utf8'));out=[];folder=ROOT/'phase49-input-snapshots';folder.mkdir(exist_ok=True)
    for r in survey['files']:
        p=Path(r.get('snapshot',r['file']));b=p.read_bytes();sha=digest(b)
        if sha==r['sha256']:continue
        target=folder/(Path(r['file']).stem+'-'+sha[:16]+Path(r['file']).suffix)
        if target.exists():assert target.read_bytes()==b
        else:target.write_bytes(b)
        out.append(dict(file=r['file'],survey_sha256=r['sha256'],snapshot_sha256=sha,snapshot=str(target),bytes=len(b),captured_at_utc=datetime.now(timezone.utc).isoformat(),scope='Independently captured current bytes after survey hash drift. Original survey bytes not restored or claimed recoverable. No atomic source bundle.'))
    dst.write_text(json.dumps(dict(survey_manifest_sha256=digest((ROOT/'phase49-camera-corpus-manifest.json').read_bytes()),changed_sources=out,scope='Read-only observations during concurrent editor activity. Exact writing process notidentified. Original survey manifest retained unchanged;current snapshots separate.'),ensure_ascii=False,indent=2),encoding='utf8');print('New drift snapshots:',len(out))
if __name__=='__main__':main()
