"""Survey all local KWD/KLD sections, including cameras after Variables."""
from pathlib import Path
from collections import Counter
import json,hashlib
import dk2_formats as fmt
from inventory import DATA
from native_cameras import record_view
ROOT=Path(__file__).resolve().parent
def digest(b):return hashlib.sha256(b).hexdigest()
def main():
    capture=json.loads((ROOT/'phase47-input-manifest.json').read_text(encoding='utf8'));old={str(Path(r['file']).resolve()).lower():r for r in capture['main_files']+capture['files']};rows=[];cameras=[];errors=[];records=0
    for p in sorted(DATA.rglob('*')):
        if not p.is_file() or p.suffix.lower() not in ['.kwd','.kld']:continue
        cap=old.get(str(p.resolve()).lower());b=Path(cap['snapshot']).read_bytes() if cap and 'snapshot' in cap else p.read_bytes();sha=digest(b)
        if cap:assert sha==cap['sha256'],str(p)
        row=dict(file=str(p),sha256=sha,bytes=len(b))
        if cap and 'snapshot' in cap:row['snapshot']=cap['snapshot']
        try:sections=fmt.parse(b)
        except ValueError as e:
            row.update(status='strict_parse_rejected',error=str(e));errors.append(dict(file=str(p),error=str(e),sha256=sha));rows.append(row);continue
        row.update(status='parsed',signatures=[s.signature for s in sections],camera_records=0)
        for si,s in enumerate(sections):
            if s.signature!=190:continue
            rs=[]
            for ri,r in enumerate(s.records):
                if fmt.u32(r)!=205:continue
                row['camera_records']+=1;records+=1;rs.append(dict(index=ri,record_sha256=digest(r),**record_view(r)))
            if rs:cameras.append(dict(file=str(p),sha256=sha,section_index=si,section_offset=s.offset,preceding_signatures=[v.signature for v in sections[:si]],records=rs,id_counts=dict(Counter(str(r['id']) for r in rs))))
        rows.append(row)
    out=dict(scope='All recursively found local Data/editor KWD/KLD sources. Every strict parse failure listed; no camera absence assertion for rejected files. Phase47 snapshots used for three recorded AutoSave drifts. Duplicated directories/files remain separate sources, not distinct semantic presets.',files=rows,file_count=len(rows),strict_parse_rejected=errors,camera_sections=cameras,camera_records=records,camera_files=len({r['file'] for r in cameras}))
    (ROOT/'phase49-camera-corpus-manifest.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8');print('Sources',len(rows),'rejected',len(errors),'camera_files',out['camera_files'],'records',records)
    for r in cameras:print(r['file'],r['preceding_signatures'],len(r['records']),r['id_counts'])
if __name__=='__main__':main()
