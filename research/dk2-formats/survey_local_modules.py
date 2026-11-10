"""Read local DLL PE metadata; never load or execute a Windows module."""
import json
from pathlib import Path
import pefile
from scan_editor_variants import ROOT, sha, instructions


def build():
    game = ROOT.parent.parent/'Dungeon Keeper 2'
    rows = []
    for relative in ['flame/DKII.dll','flame/Flame.dll','patch.dll','WEANETR.dll','Aweman32.dll']:
        path = game/relative
        data = path.read_bytes()
        pe = pefile.PE(data=data)
        exports = getattr(pe, 'DIRECTORY_ENTRY_EXPORT', None)
        symbols = exports.symbols if exports else []
        targets = sorted(set(s.address for s in symbols))
        row = dict(file=str(path), bytes=len(data), sha256=sha(data),
            machine=pe.FILE_HEADER.Machine, timestamp=pe.FILE_HEADER.TimeDateStamp,
            image_base=pe.OPTIONAL_HEADER.ImageBase,
            sections=[dict(name=s.Name.rstrip(b'\0').decode('ascii'), rva=s.VirtualAddress,
                virtual_bytes=s.Misc_VirtualSize, raw_bytes=s.SizeOfRawData) for s in pe.sections],
            exports=len(symbols), unique_export_RVAs=targets,
            imports=[e.dll.decode('ascii') for e in getattr(pe,'DIRECTORY_ENTRY_IMPORT',[])])
        if relative == 'flame/DKII.dll':
            code = next(s for s in pe.sections if s.Name.rstrip(b'\0') == b'.text')
            raw = pe.get_data(code.VirtualAddress, code.Misc_VirtualSize)
            assert len(raw) == 11 and targets == [0x1008]
            assert raw.hex() == 'b801000000c20c0033c0c3'
            row.update(all_exports_share_return_zero_stub=True,
                original_text_hex=raw.hex(), instructions=instructions(raw, pe.OPTIONAL_HEADER.ImageBase+code.VirtualAddress),
                conclusion='This exact file provides exported-name stubs, not additional original game consumer code. Names remain hints only.')
        rows.append(row)
    return dict(phase=117, modules=rows,
        scope=__doc__, limits='Only DKII.dll tiny text is semantically classified. Flame.dll/patch.dll implementations and runtime hooks are not analysed; imports/names do not prove their gameplay semantics.')


if __name__ == '__main__':
    result=build()
    (ROOT/'phase117-local-module-survey.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('5 read-only module profiles; DKII.dll exports all share one return-zero stub.')
