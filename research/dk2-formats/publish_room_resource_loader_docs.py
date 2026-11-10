"""Current integration/readiness notes for actual activeRoom resource requests."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    note=('Актуальный этап 150: пакет содержит 129 компонентов, прежние 128 схем сохранены побайтно. '
        '`room_resource_loader_semantics` выполняет оригинальные Terrain/Room raw-конвертеры и загрузчики с успешным asset-request IO и настоящей504D80. '
        'В13 выбранных TileConstruction raw11C/170/1C4/218 читаются только циклом суммы1267MIDбайт; '
        '19 полных пар51область/240039байт сохраняют запросы и прочие данные, кроме raw/MID/CRC. '
        'Положительный CompleteMesh-контроль меняет имена запросов. Все4 блока нулевые в2075 исходныхRoom-записях; заполненные случаи диагностические. '
        '62 сценария, все26 standaloneRooms;6Ghidra/312областей/1440258байт;21диапазон/2616PC/5блоков; '
        '328 точных пересборок/decode→rebuild в82контейнерах. RuntimeCRC A2 сам вычисляется загрузчиком; update сохраняет прежнее значение. '
        'Добавлен effective_room_resource_consumption; API/физические поля и прежние UI-подписи сохранены. '
        'Реальных ассетов/GPU/геометрии/Worldtick нет; глобальная неиспользуемость и смысл1C4 не доказаны. '
        'SHA310 источников/7EXE сохранены. См. `ROOM_RESOURCE_LOADER_RU.md`; общая оценка около75%.\n\n')
    p=ROOT/'INTEGRATION_RU.md';s=p.read_text(encoding='utf8')
    if not s.startswith('Актуальный этап 150:'):p.write_text(note+s,encoding='utf8')
    p=ROOT/'READINESS_RU.md';s=p.read_text(encoding='utf8');_,rest=s.split('\n',1)
    entry=('\nЭтап150: пакет содержит 129 компонентов. Уточнено поведение4 ресурсов Rooms: '
        'при активных запросах во всех13 выбранных режимах они входят только вMIDchecksum; запросы моделей не меняются. '
        'CompleteMesh-контроль меняет имена запросов. 62 сценария/19полныхпар/6Ghidra/21диапазон/2616PC, '
        '328 точных правок82контейнеров;4 блока целиком нулевые в2075 исходныхRoom-записях. '
        'Прежние128 схем и SHA310 источников/7EXE сохранены. Ресурс1C4 остаётсяunknown, '
        'реальных ассетов/GPU/Worldtick и доказательства глобальной неиспользуемости нет. '
        'Оценка около75% сохраняется. См. `ROOM_RESOURCE_LOADER_RU.md`.\n')
    if not rest.lstrip().startswith('Этап150:'):rest=entry+rest
    p.write_text('# Оценка готовности на 31 октября 2026 — после этапа 150\n'+rest,encoding='utf8');print('Published phase150 integration/readiness entries')

if __name__=='__main__':main()
