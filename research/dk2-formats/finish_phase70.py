"""Publish static indexed-origin evidence without claiming unknown semantics."""
from pathlib import Path
import json
import hashlib
from finish_phase44 import insert_after_title

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def main():
    v = load('phase70-validation.json')
    assert v['indexed_WORD_origins_verified']==17 and v['recovered_switch_instructions']==130 and v['source_executables_unchanged']==4
    p = ROOT/'INTEGRATION_RU.md'
    t = p.read_text(encoding='utf8').replace('Актуальный этап69: пакет сохраняет67 компонентов.','Предыдущий этап69: пакет сохраняет67 компонентов.')
    p.write_text(t,encoding='utf8')
    insert_after_title('INTEGRATION_RU.md',
        'Актуальный этап70: пакет сохраняет67 компонентов. `OBJECT_INDEXED_REVIEW_RU.md` / `object-property-semantics.json.phase70_indexed_review`:17 новых indexed WORD-кандидатов106 получают базу из отдельной global table6CF048→6DD5D8, а не Objects definition. Восстановлены2 computed switches/130 инструкций;4883 CFG states,0 неразрешённых indirect jumps в выбранных контекстах. Отвергнута потеря префикса66 в42F0F1;ещё6 новых anchored-кандидатов открыты.11 code extents/3 explicit ranges,84 source hashes и4 EXE неизменны;новых native executions/raw patches0. Скрытая подпись Mana Gen не превращена в доказанный игровой эффект.')
    insert_after_title('REPORT_RU.md',
        'Этап70,27 октября: `OBJECT_INDEXED_REVIEW_RU.md` —новый old-EXE поиск с indices/anchors дал146 кандидатов,24 новых адреса.17 WORD-чтений прослежены к отдельной глобальной таблице коэффициентов. Два вычисляемых перехода4EFB70 восстановлены по nativeAND3/CMP3 и8 targets:130 дополнительных инструкций,4883 состояния. Prefix-shift42F0F1 отвергнут;6 новых широких/byte anchors остаются открытыми.11 code extents+3 ranges PE/live совпали;84 источника/4 EXE unchanged. Это статическое уточнение поиска,не расшифровка Objects34C;новых native/raw-проверок0.')
    insert_after_title('OBJECT_PROPERTIES_RU.md',
        'Продолжение этап70 — `OBJECT_INDEXED_REVIEW_RU.md`:17 новых индексированных WORD-чтений оказались путями к другой глобальной таблице. После восстановления2 switches/130 инструкций источник подтверждён в4883 CFG-состояниях. Это не consumer definition106;Mana Gen остаётся только native GUI label. 6 новых anchored-кандидатов и глобальное назначение34C открыты;исходное WORD сохранять.')
    insert_after_title('SOURCES.md',
        '## Этап70: Objects indexed-origin review\n\n`OBJECT_INDEXED_REVIEW_RU.md`,`object-indexed-consumer-review.json`,`phase70-validation.json`,`phase70-primary-manifest.json`,`phase70-indexed-origin-review.json`,`phase70-old-object-field-candidates-v2.json`;`scan_old_object_field_v2.py`,`inspect_old_object_indexed.py`,`prepare_old_object_indexed_queries.py`,`review_old_object_indexed.py`,`prepare_object_indexed_primary.py`,`build_object_indexed_review.py`,`audit_object_indexed_review.py`,`finish_phase70.py`.Static17indexed-root proof,2switches/130recoveredinstructions/4883states.11codeextents+3explicitranges,84sourcehashes/4EXEsunchanged. No new native executions or raw patches;Objects34C purpose open.')
    s = load('phase69-search-status.json')
    s.update(phase=70,status='old_Objects_indexed_origins_and_switch_recovery_published;unknown_gameplay_purposes_open',
        previous_goal_turn_classification='progress:phase69kindfilterpublished;phase70addsnewindexed/anchoredsearch,17verifiedglobal-tableorigins,2recoveredcomputedswitchesandprefixboundaryrejection',
        schema='object-indexed-consumer-review.json;embedded object-property-semantics.phase70_indexed_review',
        validation='phase70-validation.json',supplement='object-property-semantics.json.phase70_indexed_review',
        findings=[
            '780881decoded old instructions/146 candidates/24new addresses,including17 indexed WORD reads excluded in phase61',
            'All17 indexed roots trace to6CF048;initializer sets fixed6DD5D8 coefficient table,not Objects catalogue definition',
            '2missing Ghidra switches reconstructed with8native targets/130instructions;4883states,no unresolved indirect jumps in these contexts',
            '42F0F1 drops canonical66prefix from CEEffectElement WORD getter42F0F0;6other new anchors remain unclassified',
            '11nativecodeextents/3explicitranges attested;84sources/4EXEsunchanged;67components;0newnative/patchcases',
        ],
        scope='Static local CFG/alias proof under normal x86 callee-saved ABI and native initialization contract. No whole-program non-use,full constructor/destructor or gameplay claim.',
        evidence=[dict(file=name,sha256=hashlib.sha256((ROOT/name).read_bytes()).hexdigest()) for name in ['phase70-old-object-field-candidates-v2.json','phase70-indexed-origin-review.json','phase70-primary-manifest.json','phase70-validation.json']],
        next_action='Do not repeat unchanged780881instructionsearch/17indexedpaths/previousnativecases. Preserve6unclassifiednewanchorsandotherknownunknowns. Pursue verified actual consumers ofObjects34C/Terrain198224/KeeperC8/CreatureJumpPriority/AP0408/Spellalternatives/WanderRadius,or complete less-verified Object load/save assembly when it can expose real field behavior. Future CFG searches must resolve bounded computed switches rather than infer no-use from Ghidra function spans or nearest direct-call hints. Retain unknown raw bytes and full objective.')
    (ROOT/'phase70-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack = load('editor-format-package.json')
    assert len(pack['components'])==67
    for key,row in pack['components'].items():
        b = (ROOT/row['file']).read_bytes()
        assert hashlib.sha256(b).hexdigest()==row['sha256'] and json.loads(b)==pack[key]
    print('Phase70 published:17 indexed origins/2 switches/1 prefix correction;goal remains active')


if __name__=='__main__':
    main()
