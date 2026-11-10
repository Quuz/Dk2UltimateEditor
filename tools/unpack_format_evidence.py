"""Restore compact primary evidence for the historical research helpers."""
from pathlib import Path
import argparse,zipfile
from assemble_format_package import RESEARCH


def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--output',type=Path,default=RESEARCH/'ghidra')
    args=ap.parse_args();target=args.output.resolve();target.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(RESEARCH/'primary-evidence.zip') as z:
        for info in z.infolist():
            p=(target/info.filename).resolve()
            if not p.is_relative_to(target):raise ValueError('Archive path escapes output directory')
            p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(z.read(info))
    print('Primary evidence restored to',target)


if __name__=='__main__':main()
