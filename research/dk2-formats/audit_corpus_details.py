"""Preserve noncanonical Variables and audit trigger links without repairing them."""
from collections import Counter
from pathlib import Path
import hashlib,json,struct
import dk2_formats as fmt
from inventory import DATA

ROOT=Path(__file__).resolve().parent

def main():
    survey=json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'))
    validation=json.loads((ROOT/'corpus-validation.json').read_text(encoding='utf8'))
    forensic=[];trigger_rows=[];unknown=Counter();node_count=0;noncanonical=[]
    for row in survey['files']:
        path=DATA/'maps'/row['file'];data=path.read_bytes()
        if 'error' in row:
            assert fmt.u32(data)==0xE6
            physical_count,partial=divmod(len(data)-56,16)
            records=[dict(offset=56+16*i,words_u32=list(struct.unpack_from('<4I',data,56+16*i)),
                raw_hex=data[56+16*i:72+16*i].hex()) for i in range(physical_count)]
            forensic.append(dict(file=path.name,sha256=row['sha256'],header_hex=data[:56].hex(),
                physical_size=len(data),declared_size=fmt.u32(data,8),declared_count=fmt.u32(data,20),declared_data_size=fmt.u32(data,52),
                physical_complete_slots=physical_count,partial_tail_hex=data[56+physical_count*16:].hex(),
                types=dict(Counter(r['words_u32'][0] for r in records)),records=records,
                warning='Forensic 16-byte interpretation through physical EOF, NOT a validated section or automatic header repair. Which records the game accepts requires separate analysis.'))
            assert data==bytes.fromhex(forensic[-1]['header_hex'])+b''.join(bytes.fromhex(r['raw_hex']) for r in records)+bytes.fromhex(forensic[-1]['partial_tail_hex'])
            continue
        for s in fmt.parse(data):
            if s.signature in (0xBE,0xE6) and fmt.u32(s.header,len(s.header)-4)!=len(s.data):
                noncanonical.append(dict(file=path.name,family=fmt.NAMES[s.signature],declared_data_size=fmt.u32(s.header,len(s.header)-4),actual_data_size=len(s.data)))
            if s.signature!=0xD2:continue
            nodes=fmt.decoded(s)['records'];node_count+=len(nodes)
            ids=Counter(n['fields']['Id'] for n in nodes);links=[]
            for n in nodes:
                f=n['fields']
                for key in ('NextId','ChildId'):
                    if f[key] and f[key] not in ids:links.append(dict(id=f['Id'],field=key,target=f[key]))
                if 'parameters' not in f:unknown[n['tag']+':'+str(f['Type'])]+=1
            trigger_rows.append(dict(file=path.name,node_count=len(nodes),duplicates={str(k):v for k,v in ids.items() if v>1},dangling_links=links))
    result=dict(trigger_files=len(trigger_rows),trigger_nodes=node_count,
        source_unmapped_parameter_types=dict(unknown),trigger_graphs=trigger_rows,noncanonical_data_sizes=noncanonical)
    (ROOT/'trigger-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    (ROOT/'noncanonical-variables-forensic.json').write_text(json.dumps(forensic,ensure_ascii=False,indent=2),encoding='utf8')
    lines=['# Проверка на всём наборе карт','',
        f"Проверены {survey['total_files']} файлов .kwd/.kld из `Data/editor/maps`, {survey['total_bytes']:,} байт. {validation['exact_json_roundtrip_files']} файлов прошли разбор, JSON-экспорт и точную обратную сборку. Проверка не запускает игру и не доказывает работоспособность каждой карты.",'',
        'У 80 наборов Globals подтвердились известные размеры всех 11 каталогов. Creatures: 76 наборов × 5537 байт и 4 × 5449 байт на запись. Во всех 112 descriptor блок DF имеет 25683 байта. У размещений обнаружены 11 типов с уже известными размерами; тип Camera CD дополнительно есть в исходном GlobalVariables.','',
        '## Triggers: отдельные правила длины','',
        f"Найдено {len(trigger_rows)} файлов, {node_count} узлов. У 107 файлов D = 16 × количество условий; у одного D содержит все байты узлов, у одного сохраняется другое старое значение. Загрузчик 0x571290 проходит сумму двух счётчиков и не использует D для границы активного чтения узлов. Схемы параметров 67 условий и 47 действий: `trigger-layouts.json`. Неразмеченные варианты: `{dict(unknown)}`.",'',
        'Добавление и удаление узлов проверено с сохранением исходного соглашения о D. Повторение/дублирование узла в этих тестах проверяет механику контейнера, а не создаёт корректный игровой граф с новым ID. Диагностика ссылок и дубликатов: `trigger-audit.json`.','',
        '## Нестандартные длины Things/Variables','',
        '| Файл | Семейство | D в заголовке | Фактические байты данных |','|---|---|---:|---:|']
    for n in noncanonical:lines.append(f"| {n['file']} | {n['family']} | {n['declared_data_size']} | {n['actual_data_size']} |")
    lines+=['','У Things функция 0x56FA00 читает N tagged-записей. В BattlesThings 20 полных записей укладываются в 744 байта файла; D=1680 устарело, реальные данные занимают 688 байт. Исходное значение сохраняется при обратной сборке, диагностика выводится отдельно.','',
        '## Четыре файла с противоречивыми заголовками Variables','',
        '| Файл | Размер физически / заявлен | Полных 16-байтовых слотов / заявлено N | D |','|---|---|---|---:|']
    for f in forensic:lines.append(f"| {f['file']} | {f['physical_size']} / {f['declared_size']} | {f['physical_complete_slots']} / {f['declared_count']} | {f['declared_data_size']} |")
    lines+=['','Linchpin содержит 394 выровненных слота с типами Variables, включая 344 Availability, но N=174, S=2840 и D=2782. После заявленного конца продолжаются такие же записи, а не заголовок следующей секции. У трёх XX-файлов физически на одну запись меньше, чем обещают N/S/D. Определить по этим фактам, какой заголовок задумывался автором и какие записи должны участвовать, нельзя.','',
        'Строгий parser эти четыре файла отвергает. Отдельный `noncanonical-variables-forensic.json` сохраняет их заголовки и все физические слоты без исправления входов. Его обратная сборка в проверке побайтно совпала с оригиналами; это не признание повреждённых заголовков корректными.','',
        'Повторить: `scan_map_corpus.py`, затем `verify_corpus.py`, затем `audit_corpus_details.py`. SHA-256 каждого файла находятся в `corpus-scan.json` и `corpus-validation.json`.','']
    (ROOT/'CORPUS_VALIDATION.md').write_text('\n'.join(lines),encoding='utf8')
    print('Triggers:',len(trigger_rows),'files',node_count,'nodes; unmapped:',dict(unknown))
    print('Graph diagnostics:',sum(bool(r['duplicates']) for r in trigger_rows),'files with duplicate IDs;',sum(len(r['dangling_links']) for r in trigger_rows),'dangling links')
    print('Noncanonical lengths:',noncanonical)

if __name__=='__main__':main()
