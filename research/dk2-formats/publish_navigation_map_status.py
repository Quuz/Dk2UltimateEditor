"""Update phase99 reader status; never mutate earlier machine schemas."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    p=ROOT/'INTEGRATION_RU.md';t=p.read_text(encoding='utf8')
    if 'Актуальный этап 99:' not in t:
        t=t.replace('Актуальный этап 98:','Предыдущий этап 98:',1)
        lead=('Актуальный этап 99: `NAVIGATION_MAP_RU.md` / `navigation-map-semantics.json`, компонент `navigation_map_semantics`. '
          '20 native-композиций связывают Terrain с навигацией: настоящий cache455D40→целаяCMap44FC40→loading-mode4D1970→preflight/полныйmovement step. '
          'Восемь включают raw552→initializer56EB60→converter56ED20→loader4F66E0. RawDWORD190→mid248→def17B: '
          'бит1 выбираетNAV8,иначе80h→1,иначе100h→3,иначе0. Присваивается весь байт;SOLID приоритетнееWATER,та приоритетнееLAVA. '
          'Разрешения Creature686/10 и/20 реально меняют проходимость воды/лавы. 82Terrain sources/3306records/19836точечных пересборок сохраняют остальные биты/байты. '
          '217полных диапазонов функций/13PE-блоков,0gamepatches. Компонент78,прежние77схем побайтово сохранены. '
          'Нормальныйgraph-rebuild,мосты/двери,полныйKLD→world,силы/A* и существенные неизвестные15семейств открыты.\n\n')
        t=t.replace('# Комплект для разработчика редактора\n\n','# Комплект для разработчика редактора\n\n'+lead,1);p.write_text(t,encoding='utf8')
    p=ROOT/'READINESS_RU.md';t=p.read_text(encoding='utf8').replace('после этапа 98','после этапа 99',1)
    t=t.replace('актуальный опубликованный этап — 98, пакет содержит 77 компонентов','актуальный опубликованный этап — 99, пакет содержит 78 компонентов',1)
    if '**Этап99 связал' not in t:
        t=t.replace('## Бонусы вселения\n\n','## Бонусы вселения\n\n'
          '**Этап99 связал Terrain с реальной навигацией:**20 сценариев,включая8 rawTerrain→loader→CMap→NAV→movement, '
          'подтвердили приоритет SOLID/WATER/LAVA и разрешения проходимости Creature. Это loading-mode путь,не полныйKLD/world/tick. '
          'См. `NAVIGATION_MAP_RU.md`. Оценка всей цели остаётся около75%;Priority/Jump и другие существенные неизвестные ещё открыты.\n\n',1)
    p.write_text(t,encoding='utf8')
    for n in ['NAVIGATION_STEP_RU.md','TERRAIN_LOADING_RU.md','MAP_CELLS_RU.md']:
        p=ROOT/n;t=p.read_text(encoding='utf8')
        if 'Обновление этапа99:' not in t:
            at=t.index('\n')+1;t=t[:at]+'\nОбновление этапа99: Terrain rawDWORD190→definition17B→навигационный байт теперь проверен20 native-композициями,включая8raw-loader цепочек. См. `NAVIGATION_MAP_RU.md`. Это loading-mode CMap иограниченныйшаг;мосты/двери,нормальныйgraph-rebuild иполныйrawKLD→world остаютсяотдельнымивопросами. Ограничения ниже относятся кисходномуэтапу.\n'+t[at:];p.write_text(t,encoding='utf8')
    print('Phase99 reader documentation updated')

if __name__=='__main__':main()
