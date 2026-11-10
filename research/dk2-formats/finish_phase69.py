"""Publish entity-filter provenance without equating typed routes to exclusivity."""
from pathlib import Path
import json
import hashlib
from finish_phase44 import insert_after_title

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def main():
    v = load('phase69-validation.json')
    assert v['native_full_filter_calls'] == 32 and v['native_filter_whole_bytes'] == 91 and v['total_type_rooted_DX_reads'] == 6
    p = ROOT/'INTEGRATION_RU.md'
    t = p.read_text(encoding='utf8').replace('Актуальный этап68: пакет сохраняет67 компонентов.','Предыдущий этап68: пакет сохраняет67 компонентов.')
    p.write_text(t,encoding='utf8')
    insert_after_title('INTEGRATION_RU.md',
        'Актуальный этап69: пакет сохраняет67 компонентов. `ENTITY_KIND_FILTER_RU.md` / `trap-semantics.json.phase69_entity_filter`:32 полных вызова91-байтного фильтра kind/owner в двух EXE. Выбирает первое совпадение списка; owner low16, kind full32; неудача сохраняет output. Для6 из7 DX-зацепок подтверждены пути от ловушек к DWORD9A. Handler513730 передаёт сущность по tag без локальной проверки типа; upstream контракт открыт, глобальная исключительность CTrap не заявляется.9 полных PE-функций,2 исполнялись;1 jump-table диапазон.82 источника и4 EXE неизменны;новых raw-патчей0. Unknown204/236 и прочие игровые назначения открыты.')
    insert_after_title('REPORT_RU.md',
        'Этап69,27 октября: `ENTITY_KIND_FILTER_RU.md` —32 native cases фильтра453700/old44C250,полные91 байт отличаются лишь двумя tag-table операндами. Kind3/owner filtering и jump-case3 связывают ещё4 DX-чтения с TrapDWORD9A; вместе с этапом68 подтверждены6 путей. Отдельный handler513730 локально не проверяет тип; это не доказанный штатный Door consumer и не ошибка записи редактора.9 полных PE/1 data range;82 hashchecked sources и4 EXE неизменны,67 компонентов. Новых raw-пересборок нет;общая цель открыта.')
    insert_after_title('DOORS_RUNTIME_RU.md',
        'Этап69 — `ENTITY_KIND_FILTER_RU.md`: ещё4 DX-кандидата получают указатели через проверку kind3 или полноценный фильтр kind/owner. Всего6 инструкций имеют подтверждённые Trap paths. Handler513730 — отдельный вход без локальной проверки kind; upstream контракт не установлен, глобальное отсутствие Door paths не доказано. Смысл WORD236 остаётся неизвестным, его нельзя назначать из числового пересечения99/9A.')
    insert_after_title('TRAP_READINESS_RU.md',
        'Продолжение этап69 — `ENTITY_KIND_FILTER_RU.md`: отдельные32 native-прогона kind/owner filter подтвердили происхождение ещё4 DX-кандидатов. Всего6 типизированных Trap paths; один командный вход513730 локально не проверяет kind. Исторические42 readiness cases этого документа не повторялись как новая работа. Полный игровой смысл Traps204/Doors236 остаётся открытым.')
    insert_after_title('SOURCES.md',
        '## Этап69: kind/owner filter и происхождение candidate pointers\n\n`ENTITY_KIND_FILTER_RU.md`,`entity-kind-filter-semantics.json`,`phase69-validation.json`,`phase69-primary-manifest.json`,`phase69-entity-kind-filter-emulations.json`;`native_entity_kind_filter.py`,`emulate_entity_kind_filter.py`,`prepare_entity_kind_filter.py`,`prepare_entity_kind_filter_primary.py`,`build_entity_kind_filter.py`,`audit_entity_kind_filter.py`,`finish_phase69.py`.32 native filter calls;2 executed/9 complete PE functions,1 jump-table data range. Complete91-byte match includes finalRET operand;initial89-byte prefix corrected with diagnostic retained.6 type-rooted DX routes,1 untyped command route;not exclusive global type proof.82 source hashes/4 EXEs unchanged;no new raw patches.')
    s = load('phase68-search-status.json')
    s.update(phase=69,status='native_kind_owner_filter_and_scoped_DX_Trap_pointer_routes_published;unknown_gameplay_purposes_open',
        previous_goal_turn_classification='progress:phase68published42native readiness cases;phase69adds32nativefiltercases,4additionaltype-rootedDXroutes,anduntypedcommandboundary',
        schema='entity-kind-filter-semantics.json;embedded trap-semantics.phase69_entity_filter;Doors phase69_consumer_review SHA link',
        validation='phase69-validation.json',supplement='trap-semantics.json.phase69_entity_filter',
        findings=[
            '32 full native kind/owner filters,16 per EXE;two complete91-byte bodies match outside two tag-table operands',
            'First linked match/full32kind/low16owner/output unchanged on no match;no native cycle/validity check in this helper',
            '4newplus2priorDXreads have real Trap-origin paths;not a global exclusivity claim',
            '513730 passes table[tag] to4A7E70 without local kind/NULL/validity check;upstream command contract unresolved',
            '9completePEfunctions,2executed,1jumptablerange;82sources/4EXEsunchanged,67components,no newrawpatches',
        ],
        scope='Full low-level runtime-list filter only;caller/command/static type chains attested separately. No complete tick,shot creation,command producer or gameplay run.',
        evidence=[dict(file=name,sha256=hashlib.sha256((ROOT/name).read_bytes()).hexdigest()) for name in ['phase69-entity-kind-filter-emulations.json','phase69-primary-manifest.json','phase69-validation.json']],
        next_action='Do not repeat32filters,42readiness,58Doorloaders or unchanged offset searches. Preserve completed candidate provenance and the unchecked-command boundary. Prioritize other unresolved actual gameplay consumers:KeeperC8,CreatureJump/PickUpPriority,Terrain198/224,Objects34C,Traps204,AP04/08,CreatureSpell alternatives,WanderRadius;use verified indexed/indirect pointer origins or branch contracts. Do not infer Door236 purpose from malformed command inputs or reinterpret editor writer defects as format normalization.')
    (ROOT/'phase69-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack = load('editor-format-package.json')
    assert len(pack['components']) == 67
    for key,row in pack['components'].items():
        b = (ROOT/row['file']).read_bytes()
        assert hashlib.sha256(b).hexdigest() == row['sha256'] and json.loads(b) == pack[key]
    print('Phase69 published:32 native filters;6 typed routes/1 untyped boundary;goal remains active')


if __name__=='__main__':
    main()
