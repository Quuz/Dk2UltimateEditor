"""Compare the actually referenced bundle with the adjacent autosave bundle."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA
from validate_references import ID_FIELDS,scalar

ROOT=Path(__file__).resolve().parent

def main():
    paths=[DATA/'AutoSaveGlobals.kld',DATA/'maps/kop4Globals.kld']
    docs=[fmt.inspect(p) for p in paths];changes=[];totals=Counter()
    for a,b in zip(docs[0]['sections'],docs[1]['sections']):
        assert a['signature']==b['signature'] and a['stride']==b['stride']
        family=a['family'];key=ID_FIELDS[family]
        aa={scalar(r['fields'][key]):r for r in a['records']};bb={scalar(r['fields'][key]):r for r in b['records']}
        assert len(aa)==len(a['records']) and len(bb)==len(b['records'])
        for ident in sorted(aa.keys()|bb.keys()):
            if ident not in aa or ident not in bb:
                changes.append(dict(family=family,id=ident,kind='record_added_or_removed'));totals[family]+=1;continue
            ra,rb=aa[ident],bb[ident];ba,bc=bytes.fromhex(ra['raw_hex']),bytes.fromhex(rb['raw_hex'])
            if ba==bc:continue
            layout=fmt.LAYOUTS[family+('_5537' if family=='Creatures' and len(ba)==5537 else '')]
            fields=[]
            for f in layout['fields']:
                p=f['offset'];q=p+f['size']
                if ba[p:q]!=bc[p:q]:fields.append(dict(field=f['name'],offset=hex(p),size=f['size'],adjacent=ra['fields'][f['name']],referenced=rb['fields'][f['name']]))
            changes.append(dict(family=family,id=ident,name=ra['fields'].get('Name'),
                changed_byte_offsets=[hex(i) for i,(x,y) in enumerate(zip(ba,bc)) if x!=y],fields=fields))
            totals[family]+=1
    result=dict(adjacent_source=docs[0]['source'],referenced_source=docs[1]['source'],
        hashes=[d['sha256'] for d in docs],changed_records_by_family=dict(totals),changes=changes,
        note='Direction: adjacent AutoSaveGlobals -> bundle named in AutoSave.kwd. Baseline field names are source interpretations; raw bytes remain authoritative.')
    (ROOT/'globals-comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    lines=['# Какой Globals действительно указан в AutoSave','',
        '`AutoSave.kwd` ссылается на `Data\\Editor\\Maps\\kop4Globals.kld`. Файл существует. Соседний `AutoSaveGlobals.kld` имеет тот же размер и состав каталогов, но другой SHA-256 и другое содержимое. Подмена одного другим скрывает различия свойств.','',
        '| Каталог | ID | Имя | Изменившиеся поля базовой схемы |','|---|---:|---|---|']
    for row in changes:
        lines.append(f"| {row['family']} | {row['id']} | {row.get('name','')} | {', '.join(f['field'] for f in row.get('fields',[]))} |")
    lines+=['','Всего записей с различиями: '+str(sum(totals.values()))+'. Полные значения в обоих наборах, диапазоны полей и изменившиеся байты: `globals-comparison.json`. Это сравнение содержимого, не доказательство фактического успешного открытия всей карты: несколько других ссылок descriptor не находятся по указанным путям.','']
    (ROOT/'GLOBALS_COMPARISON.md').write_text('\n'.join(lines),encoding='utf8')
    print('Changed records:',dict(totals))

if __name__=='__main__':main()
