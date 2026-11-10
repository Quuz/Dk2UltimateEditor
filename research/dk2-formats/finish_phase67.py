"""Publish checked Door loading evidence; keep the complete research goal open."""
from pathlib import Path
import json
import hashlib
from finish_phase44 import insert_after_title

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT / name).read_text(encoding='utf8'))


def main():
    v = load('phase67-validation.json')
    assert v['native_emulations'] == 58 and v['new_exact_in_memory_patches'] == 656 and v['source_executables_unchanged'] == 4
    p = ROOT / 'INTEGRATION_RU.md'
    t = p.read_text(encoding='utf8').replace('Актуальный этап66: пакет сохраняет67 компонентов.', 'Предыдущий этап66: пакет сохраняет67 компонентов.')
    p.write_text(t, encoding='utf8')
    insert_after_title('INTEGRATION_RU.md',
        'Актуальный этап67: пакет сохраняет67 компонентов. `DOOR_LOADING_RU.md` / `door-semantics.json.phase67_loading`:58 полных native-сценариев initializer/converter/loader двух EXE; все616/732/181 байт сравниваются. Raw21E/220/230 не меняют результат этой цепочки. WORD236→mid2AA→def99 сохраняет16 бит; игровой смысл открыт. Baseline ResearchTime224 в null-resource loader влияет только на вычисляемую сумму, не на другие поля definition.8 полных PE-функций/2 диапазона данных;656 новых точечных пересборок/82 файла;4 EXE неизменны. Визуальные ресурсы, полный parser/world insertion и игровая сессия вне новой проверки.')
    insert_after_title('REPORT_RU.md',
        'Этап67,27 октября: `DOOR_LOADING_RU.md` —58 native cases полной загрузки Doors в двух игровых EXE. Отдельные8 raw holes не переносятся, другие8 mid padding сохраняются converter и очищаются initializer. WORD236 хранится без потерь; ResearchTime224 при null visual provider меняет только checksum, если тот рассчитывается.8 полных функций повторно сверены с PE;656 новых224-пересборок/82 файла656 Doors;4 исходных EXE неизменны. Ложные Traps204 displacement leads не доказывают ошибку записи или сбой игры. Неизвестные игровые назначения остаются открытыми.')
    insert_after_title('DOORS_RUNTIME_RU.md',
        'Этап67 — `DOOR_LOADING_RU.md`: полный initializer→converter→loader проверен при null visual provider. WORD236→mid2AA/pres38→definition99; raw holes21E/220/230 не меняют intermediate/definition. Baseline ResearchTime224 в этом пути отдельно не переносится и влияет лишь на вычисляемую сумму. Не трактовать как глобальное отсутствие потребителя или как доказанный дефект редактора.58 native cases,8 PE bodies,656 новых raw-пересборок.')
    insert_after_title('ENGINE_FINDINGS_RU.md',
        '## Загрузка Doors и границы вывода об ошибках, этап67\n\n`DOOR_LOADING_RU.md`:58 полных native-сценариев сравнивают raw616/mid732/def181 при null visual provider. WORD236 сохраняется целиком в def99; raw21E/220/230 не доходят до mid. ResearchTime224 меняет только сумму при её расчёте. Старый loader считает сумму/звук всегда, DX пропускает при global6FCDCF≠0.8 полных PE-функций/2 data ranges,656 новых patches/82 файла,4 EXE неизменны. Игровой смысл unknowns и последствия writer anomalies не установлены.')
    insert_after_title('SOURCES.md',
        '## Этап67: полные Door initializer/converter/loader\n\n`DOOR_LOADING_RU.md`,`door-loading-semantics.json`,`phase67-validation.json`,`phase67-primary-manifest.json`,`phase67-door-loading-emulations.json`;`native_door_loading.py`,`emulate_door_loading.py`,`prepare_door_loading_primary.py`,`build_door_loading_semantics.py`,`audit_door_loading.py`,`finish_phase67.py`.58 native cases/8 complete executed PE functions/2 native data ranges;full616/732/181-byte comparisons.656 new224-WORD container patches/82 sources656 Doors. Raw holes ignored only in this chain;unknown gameplay consumers open. Search outputs`phase67-old-door-word236-leads.json`,`phase67-DX-door-word236-leads.json` retain untyped candidates without assigning a Door meaning.')
    s = load('phase66-search-status.json')
    s.update(phase=67,
        status='full_Door_loading_published_under_null_visual_provider_contract;unknown_gameplay_purposes_open',
        previous_goal_turn_classification='progress:58 complete native loading scenarios,8 post-emulation PE bodies and2 native data ranges captured;new schema and raw patch API published',
        schema='door-loading-semantics.json;embedded door-semantics.phase67_loading',
        validation='phase67-validation.json', supplement='door-semantics.json.phase67_loading',
        findings=[
            '58 full native cases:6 initializer,12 converter,16 loader,24 chains;all raw616/mid732/def181 bytes compared',
            'WORD236/pres38 reaches definition99 with all16 bits;gameplay meaning unresolved',
            'Raw8 bytes21E/220/230 do not affect complete mid/definition buffers in12 native chains',
            'Converter retains8 different intermediate padding bytes292..297/2A4..2A5;initializer clears all732 bytes',
            'Baseline ResearchTime224 affects only calculated checksum in tested null-provider loader;not global non-use proof',
            'Checksum sum732bytes at87 collides for WORD236=00FF/FF00;DXgate skips checksum/sound',
            '656 new exact224 patches/82 sources656 Doors;4 EXEs unchanged;67 package components',
        ],
        scope='Whole original loading functions with null visual provider and explicit sound capture/token adapter. No GUI/filesystem parser/world insertion/gameplay claim.',
        evidence=[dict(file=name, sha256=hashlib.sha256((ROOT / name).read_bytes()).hexdigest()) for name in ['phase67-door-loading-emulations.json','phase67-primary-manifest.json','phase67-validation.json']],
        next_action='Do not repeat completed58 Door-loading cases or unchanged offset searches. Pursue typed consumers of unknown Door236,Traps204,Objects34C,Terrain198/224,KeeperC8 and other open fields through verified object/class origins and indexed/indirect accesses; or resource branches when they resolve these questions. Classify untyped DWORD9A overlaps without assuming Door type. Preserve raw unknowns and distinguish editor defects from demonstrated gameplay effects.')
    (ROOT / 'phase67-search-status.json').write_text(json.dumps(s, ensure_ascii=False, indent=2), encoding='utf8')
    pack = load('editor-format-package.json')
    assert len(pack['components']) == 67
    for key, row in pack['components'].items():
        b = (ROOT / row['file']).read_bytes()
        assert hashlib.sha256(b).hexdigest() == row['sha256'] and json.loads(b) == pack[key]
    print('Phase67 published:58 native cases/656 new patches;67 components;goal remains active')


if __name__ == '__main__':
    main()
