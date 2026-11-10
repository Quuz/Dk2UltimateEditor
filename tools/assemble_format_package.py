"""Assemble the complete research package without duplicating its components in Git."""
from pathlib import Path
import argparse,hashlib,json

REPO=Path(__file__).resolve().parents[1]
RESEARCH=REPO/'research/dk2-formats'


def assemble():
    index=json.loads((RESEARCH/'format-package.index.json').read_text(encoding='utf-8'))
    meta=index['package_metadata'];components=meta['components'];result={}
    for key in index['key_order']:
        if key in components:
            ref=components[key];p=RESEARCH/ref['file'];data=p.read_bytes()
            if hashlib.sha256(data).hexdigest()!=ref['sha256']:raise ValueError('Component hash mismatch: '+ref['file'])
            result[key]=json.loads(data.decode('utf-8'))
        else:result[key]=meta[key]
    text=json.dumps(result,ensure_ascii=False,indent=2)+'\n'
    data=text.replace('\n',index['serialization']['newline']).encode('utf-8')
    if hashlib.sha256(data).hexdigest()!=index['original_package_sha256']:raise ValueError('Assembled package differs from the research snapshot')
    return data


def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--output',type=Path,default=REPO/'artifacts/formats/editor-format-package.json')
    args=ap.parse_args();data=assemble();args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_bytes(data)
    print(str(args.output),len(data),'bytes; SHA-256',hashlib.sha256(data).hexdigest())


if __name__=='__main__':main()
