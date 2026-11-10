"""Primary RC3/Pro tables and exact resistance-dialog resources; no runtime ID guess."""
from pathlib import Path
import hashlib, json
from build_combat_categories import table
from extract_editor_dialogs import extract
from extract_editor_strings import resource_leaves

ROOT = Path(__file__).resolve().parent
PRO = ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe'
RC3 = ROOT / 'source-binaries/DK2-Editor-rc3.exe'


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def build():
    a, b = PRO.read_bytes(), RC3.read_bytes()
    assert sha(a) == '989c45df779bccfc9a9e509b2ad0224a9bd269629f74792adc0320fa4b99fb09'
    assert sha(b) == '7368d200cc5ac7e0cdb28c9852d9aa51b960fd718d9eea8dd927c4084add102c'
    tables = [table(b, address) for address in [0x51DB08, 0x5252C0, 0x525E50]]
    pro = table(a, 0x512D28)
    wanted = [(row['id'], row['label']) for row in pro['rows']]
    assert [row[0] for row in wanted] == list(range(1, 16))
    for native in tables:
        assert [(row['id'], row['label']) for row in native['rows']] == wanted
    path = [5, 233, 2057]
    pa = next(row for row in resource_leaves(a) if row[0] == path)
    pb = next(row for row in resource_leaves(b) if row[0] == path)
    assert pa[3] == pb[3]
    da = next(row for row in extract(a) if row['resource_path'] == path)
    db = next(row for row in extract(b) if row['resource_path'] == path)
    assert da == db
    return dict(phase=126, profiles=[dict(file=str(PRO), sha256=sha(a)), dict(file=str(RC3), sha256=sha(b))],
        RC3_tables=tables, Pro_reference_table=pro,
        identical_resistance_dialog=dict(resource_path=path, bytes=len(pa[3]), sha256=sha(pa[3]),
            Pro_RVA=pa[1], RC3_RVA=pb[1], parsed=da),
        supported_native_label_IDs=list(range(1, 16)), missing_source_IDs=[16, 30, 32],
        conclusion='The three RC3 native category tables and identical dialog233 contain no extra labels16/30/32. This doesnot identify their meaning or prove absence of every possible hidden RC3 path.',
        scope='Raw original PE tables/resource parsed directly; no new RC3 Ghidra CPU or full GUI execution claimed.')


if __name__ == '__main__':
    value = build()
    (ROOT / 'phase126-resistance-editor-version-tables.json').write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf8')
    print('RC3 three tables1..15 match Pro labels; dialog233 is byte-identical;16/30/32 remain unresolved.')
