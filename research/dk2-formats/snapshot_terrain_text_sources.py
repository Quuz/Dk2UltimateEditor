"""Record observed pre-existing map drift without replacing historical evidence."""
from pathlib import Path
from datetime import datetime, timezone
from copy import deepcopy
from hashlib import sha256
import json

ROOT=Path(__file__).resolve().parent

def main():
    target=ROOT/'phase165-source-baseline.json'
    if target.exists():
        d=json.loads(target.read_text(encoding='utf-8'))
        for row in d['source_files']+d['executable_paths']:
            assert sha256(Path(row['file']).read_bytes()).hexdigest()==row['sha256'],row['file']
        print('Existing phase165 source baseline still matches; not replaced.')
        return
    prior=json.loads((ROOT/'creature-work-initial-semantics.json').read_text(encoding='utf-8'))['preservation']
    d=deepcopy(prior);drift=[]
    for group in ['source_files','executable_paths']:
        for row in d[group]:
            p=Path(row['file']);h=sha256(p.read_bytes()).hexdigest()
            if h!=row['sha256']:
                assert group=='source_files'
                drift.append(dict(file=str(p),phase164_sha256=row['sha256'],current_sha256=h,
                    bytes=p.stat().st_size,modified_utc=datetime.fromtimestamp(p.stat().st_mtime,timezone.utc).isoformat()))
                row['sha256']=h
    expected={r'Data\editor\AutoSaveMap.kld',
              r'Data\editor\maps\DK2Preview_6a88240eaf814f78Map.kld',
              r'Data\editor\maps\DK2Preview_6a88240eaf814f78Things.kld',
              r'Data\editor\maps\DK2Preview_6a88240eaf814f78Triggers.kld'}
    game=Path(r'D:\Games\DK2\Dungeon Keeper 2')
    assert {str(Path(row['file']).relative_to(game)) for row in drift}==expected
    assert len(d['source_files'])==420 and len(d['executable_paths'])==7
    d.update(baseline_captured_utc=datetime.now(timezone.utc).isoformat(),historical_phase164_drift=drift,
        baseline_scope='Current-source snapshot captured after native/Ghidra checks, before publication. Four files lastmodified2026-11-01 differfromphase164. No claim they stayed unchanged sincephase164; original snapshots and schemas retained.',
        current_task_source_writes=0,attribution='No attribution of external changes to a particular application; only hashes and filesystem timestamps observed.')
    target.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('Recorded420current sources/7unchangedEXEs;4historical map differences retained explicitly.')

if __name__=='__main__':main()
