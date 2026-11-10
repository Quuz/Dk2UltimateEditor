"""Publish selected completed research files while preserving the historical snapshot."""
from pathlib import Path
import argparse,hashlib,json
from assemble_format_package import RESEARCH,assemble


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--source',type=Path,required=True);ap.add_argument('--phase',type=int,required=True)
    ap.add_argument('--include',action='append',default=[]);args=ap.parse_args()
    manifest_path=RESEARCH/'snapshot-manifest.json';manifest=json.loads(manifest_path.read_text(encoding='utf8'))
    package_raw=(args.source/'editor-format-package.json').read_bytes();package=json.loads(package_raw)
    files={r['file']:r for r in manifest['files']};names=set(args.include)
    names.update(r['file'] for r in package['components'].values())
    for name in sorted(names):
        if Path(name).name!=name or Path(name).suffix not in ['.json','.md','.py','.csv','.java','.txt','.gz','.bin']:raise ValueError('Expected one research basename: '+name)
        data=(args.source/name).read_bytes();(RESEARCH/name).write_bytes(data)
        files[name]=dict(file=name,bytes=len(data),sha256=hashlib.sha256(data).hexdigest())
    metadata={k:v for k,v in package.items() if k not in package['components']}
    index=dict(schema_version=1,original_package_sha256=hashlib.sha256(package_raw).hexdigest(),original_package_bytes=len(package_raw),
        key_order=list(package),package_metadata=metadata,
        serialization=dict(encoding='utf-8',indent=2,ensure_ascii=False,newline='\r\n' if b'\r\n' in package_raw else '\n'),
        use='Assemble with tools/assemble_format_package.py; complete package matches the source SHA-256.')
    (RESEARCH/'format-package.index.json').write_text(json.dumps(index,ensure_ascii=False,indent=2)+'\n',encoding='utf8',newline='\n')
    manifest.update(latest_integrated_phase=args.phase,component_count=len(package['components']),files=sorted(files.values(),key=lambda r:r['file'].lower()),
        documentation_count=sum(n.endswith('.md') for n in files),python_source_count=sum(n.endswith('.py') for n in files),
        original_package_sha256=index['original_package_sha256'],original_package_bytes=len(package_raw),in_progress=None)
    (RESEARCH/'snapshot-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf8',newline='\n')
    assert assemble()==package_raw
    print('Updated phase',args.phase,'components',len(package['components']),'source files',len(files),'exact package',len(package_raw),'bytes')


if __name__=='__main__':main()
