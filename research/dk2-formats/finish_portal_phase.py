"""Current docs update for phase91; historical machine schemas stay unchanged."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    p=ROOT/'INTEGRATION_RU.md';text=p.read_text(encoding='utf8')
    note=('Актуальный этап 91: `PORTAL_EXIT_RU.md` / `portal-exit-semantics.json`, компонент `portal_exit_semantics`. '
      'Нативная цепочка ctor→portal4DC9A0→action49→state193/callback459C20 подтверждена 14 сценариями/84 выполнениями '
      'с полными1343-байтовыми экземплярами; два сценария используют настоящий AI dispatcher476FC0, два — animation tick496AA0 '
      '(null-GFX fallback). Начальная точка — room anchorXY+1.5 клетки; rawE94/E98 применяются позже как XY-offset, '
      '4096 единиц/клетку; Z прочитан, затем заменён полом. Проверены ожидание, signed/fractional переносы и списки клеток. '
      '117 полных диапазонов функций/6 блоков PE; нативные initializers AI/animation выполнены, код/vtable/callbacks не заменены. '
      '`native_portal_offsets.py`: 3734 views,738 exact-container API правок/82 источника,8 отказов. '
      'Компонент70, прежние69 побайтово сохранены. Полный rawCreatures→spawn, реальные кадры/physics и остальные существенные вопросы открыты.')
    if 'Актуальный этап 91:' not in text:
        text=text.replace('Актуальный этап 90:','Предыдущий этап 90:',1);title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    p=ROOT/'READINESS_RU.md';text=p.read_text(encoding='utf8').replace('после этапа 90','после этапа 91').replace('актуальный опубликованный этап — 90, пакет содержит 69 компонентов','актуальный опубликованный этап — 91, пакет содержит 70 компонентов')
    begin=text.index('## Первое появление из портала');end=text.index('## Бонусы вселения')
    section='''## Первое появление из портала

**Да, теперь подтверждено, как изменить XY-точку выхода после анимации:** DWORD raw Creatures `E94/E98`, signed fixed-point с 4096 единицами на клетку. Z по `E9C` сохраняется физически; в проверенном `459C20` он читается и складывается, но перед перемещением заменяется высотой пола новой клетки. Это правило нельзя переносить на другие потребители тройки.

Начальное создание — отдельный шаг: `4DC9A0` берёт `(roomX+1.5, roomY+1.5)`, вызывает настоящую factory, включает action49 и состояние193. После завершения action нативный AI dispatcher вызывает `459C20`, тот прибавляет XY, перемещает существо между списками клеток и возвращает state1. Выполнены14 сценариев/84 повтора, включая настоящий animation tick с null-GFX fallback и AI dispatcher; не только статическое предположение по имени PORTAL_ENTRANCE. См. `PORTAL_EXIT_RU.md`, `portal-exit-semantics.json`, API `native_portal_offsets.py`.

Настоящая длительность отрисовываемой анимации, physics, автоматический выбор типа существа/портала и полный новый raw-Creatures→creation запуск остаются за границей этого доказательства. Raw→definition/getter подтверждены отдельным этапом85; этап91 проверяет реального потребителя с подготовленной runtime definition. Начальную абсолютную клетку нельзя представлять как поле этой XYZ-тройки.

'''
    p.write_text(text[:begin]+section+text[end:],encoding='utf8')
    p=ROOT/'CREATURE_DEFINITION_RU.md';text=p.read_text(encoding='utf8')
    note='Обновление этапа91: игровой потребитель XYZ-индекса0 теперь проверен в цепочке появления из портала; X/Y — смещение после анимации в единицах1/4096 клетки, Z в этом пути заменяется полом. См. `PORTAL_EXIT_RU.md`. Ниже сохранён исходный объём доказательств этапа85; остальные XYZ-потребители отдельно не закрыты.'
    if 'Обновление этапа91:' not in text:
        title,rest=text.split('\n',1);p.write_text(title+'\n\n'+note+'\n'+rest,encoding='utf8')
    print('Current integration/readiness91 saved; previous69 machine components untouched')
if __name__=='__main__':main()
