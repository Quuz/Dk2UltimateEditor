from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def main():
    p=ROOT/'INTEGRATION_RU.md';text=p.read_text(encoding='utf8')
    note=('Актуальный этап 92: `POSSESSION_SPEED_RU.md` / `possession-speed-semantics.json`, компонент `possession_speed_semantics`. '
      'Фактический speed-бонус вселения — GlobalVariables type224/StatId21: P200 даёт+200%,тоесть×3 в проверенном full48ABB0. '
      'Raw callback51DC70→native setter48DEA0→full speed/getter495080:21 сценарий,включая неизменённую текущую запись, '
      'signed16,low32 overflow,walk/run/shuffle общий21,flags priority иwriter/getter index asymmetry. '
      'Scalar98/113/114 не влияют на этот speed-path; здоровье/другиепотребители остаются открыты. '
      '125 native spans/6PE blocks;код/vtable не заменены;wholeactor+guard проверены,использован native actor image этапа90. '
      '557 Variables checked,32type224/128exact-container API patches. Компонент71,прежние70 побайтово сохранены.')
    if 'Актуальный этап 92:' not in text:
        text=text.replace('Актуальный этап 91:','Предыдущий этап 91:',1);title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    p=ROOT/'READINESS_RU.md';text=p.read_text(encoding='utf8').replace('после этапа 91','после этапа 92').replace('актуальный опубликованный этап — 91, пакет содержит 70 компонентов','актуальный опубликованный этап — 92, пакет содержит 71 компонент').replace('71 компонентов','71 компонент')
    note=('**Теперь найден и выполнен реальный бонус скорости:** запись GlobalVariables type224,StatId21,значение200. '
      'В полном нативном getter скорости это+200%,тоесть×3;walk/run/shuffle используют общий21. '
      'В текущем файле запись2658h,значение2660h;искать по типу/ID,не по абсолютному адресу. '
      'См. `POSSESSION_SPEED_RU.md`. Проверены21 native-сценарий и128 точечных API-пересборок. '
      'Бонус здоровья и семантика остальных stat-индексов этим не закрыты.\n\n')
    if '**Теперь найден и выполнен реальный бонус скорости:**' not in text:text=text.replace('## Бонусы вселения\n\n','## Бонусы вселения\n\n'+note,1)
    p.write_text(text,encoding='utf8');print('Current integration/readiness92 saved; full objective remains open')
if __name__=='__main__':main()
