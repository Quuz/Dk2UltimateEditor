"""Publish scoped native readiness evidence and retain all unresolved work."""
from pathlib import Path
import json
import hashlib
from finish_phase44 import insert_after_title

ROOT = Path(__file__).resolve().parent


def load(name):
    return json.loads((ROOT/name).read_text(encoding='utf8'))


def main():
    v = load('phase68-validation.json')
    assert v['native_full_readiness_calls'] == 42 and v['DX_candidate_reads_type_classified'] == 2 and v['source_executables_unchanged'] == 4
    p = ROOT/'INTEGRATION_RU.md'
    t = p.read_text(encoding='utf8').replace('Актуальный этап67: пакет сохраняет67 компонентов.','Предыдущий этап67: пакет сохраняет67 компонентов.')
    p.write_text(t,encoding='utf8')
    insert_after_title('INTEGRATION_RU.md',
        'Актуальный этап68: пакет сохраняет67 компонентов. `TRAP_READINESS_RU.md` / `trap-semantics.json.phase68_readiness`:42 полных native-проверки допуска в двух EXE, без подмен кода/providers. Порядок bit20 → unsigned deadline → signed mana → WORD-state подтверждён; unknown Traps204 не влияет на этот helper. Native CTrap RTTI/slot14 и slot4C типизируют два DX DWORD9A чтения, ошибочно подходивших по пересечению DoorWORD99; остальные5 DX и7 old кандидатов требуют отдельного review.6 исполнявшихся/10 полных PE функций,3 RTTI диапазона;82 исходных файла1293 Traps и4 EXE неизменны. Полный tick, выстрел и глобальное назначение204/236 остаются открытыми.')
    insert_after_title('REPORT_RU.md',
        'Этап68,27 октября: `TRAP_READINESS_RU.md` —42 native readiness cases двух EXE с настоящими clock/mana helpers. Отрицательная signed стоимость допускается сравнением; фактическое списание/выстрел не проверены. Unknown Traps204 не меняет результат helper. Два DX DWORD9A совпадения поиска Door236 принадлежат native CTrap vtable paths; другие кандидаты не переименованы.10 целых PE-функций,6 исполнялись,3 RTTI диапазона;82 источника1293 Traps и4 EXE неизменны, новых raw-патчей0. Общая цель остаётся открытой.')
    insert_after_title('TRAPS_RUNTIME_RU.md',
        'Этап68 — `TRAP_READINESS_RU.md`: полный4A7D10/old49EB20 проверен в42 случаях с native clock/mana helpers. Bit20 даёт ранний код1, unsigned deadline код0, недостаточная signed mana код2; WORD115/old114 различает3/4. Не объявлять это полным предиктором выстрела. Unknown204→def98 не влияет в тестах; чтение9A относится к ManaCostToFire. Native CTrap slot14/4C устанавливают тип двух DX чтений, не игровой смысл Door236.')
    insert_after_title('DOORS_RUNTIME_RU.md',
        'Этап68 — `TRAP_READINESS_RU.md`: native RTTI CTrap и его vtable slot14/4C связывают DX4A7D58/4A9C99 с TrapDWORD9A, а не DoorWORD99. Только эти два из семи DX-кандидатов типизированы в новом этапе; остальные требуют отдельной проверки происхождения указателей. Door236 остаётся unknown; сохранение всех16 бит и независимость от234 не меняются.')
    insert_after_title('SOURCES.md',
        '## Этап68: native Trap readiness и типизация кандидатов\n\n`TRAP_READINESS_RU.md`,`trap-readiness-semantics.json`,`phase68-validation.json`,`phase68-primary-manifest.json`,`phase68-trap-readiness-emulations.json`;`native_trap_readiness.py`,`emulate_trap_readiness.py`,`prepare_trap_gate_queries.py`,`prepare_trap_readiness_primary.py`,`build_trap_readiness.py`,`audit_trap_readiness.py`,`finish_phase68.py`.42 whole native readiness calls with6 executed original functions;10 complete PE bodies and3 RTTI data ranges attested. Two DX reads4A7D58/4A9C99 typed CTrap,other candidates remain separate.82 source hashes/1293 Trap records,4 EXEs unchanged;no new raw patches.')
    s = load('phase67-search-status.json')
    s.update(phase=68,status='native_Trap_readiness_and_two_DX_type_rejections_published;unknown_gameplay_purposes_open',
        previous_goal_turn_classification='progress:phase67fullDoorloading58cases/656patchespublished;phase68adds42actualreadinesscasesandtwoCTraptype-provedconsumerpaths',
        schema='trap-readiness-semantics.json;embedded trap-semantics.phase68_readiness;Doors phase68_readiness_review SHA link',
        validation='phase68-validation.json',supplement='trap-semantics.json.phase68_readiness',
        findings=[
            '42 complete native gates,21 per exactEXE;nativeclock andsignedmana helpers execute without providers',
            'Gate order bit20/unsigneddeadline/signedcost/smallstateWORD verified;earlybit20 acceptsnullworld/definition/owner pointers',
            'UnknownTraps204 runtimeWORD98 variations0/1/8000/FFFF do not change this gate;globalpurpose open',
            'NativeCTrap RTTI/vslots14/4C classify DX4A7D58/4A9C99 as TrapDWORD9A reads,not DoorWORD99',
            'Only2of7DXcandidates classified;remaining5and7oldcandidates require their own pointer-origin proofs',
            '10fullPEfunctions,6executed,3RTTIdataranges;82sources1293Traps/4EXEsunchanged;67components',
        ],
        scope='Full readiness functions and their true clock/mana helpers;prebuilt memory world/actors. Static caller/RTTI type roots are separately identified. No complete tick/firing/debit/gameplay.',
        evidence=[dict(file=name,sha256=hashlib.sha256((ROOT/name).read_bytes()).hexdigest()) for name in ['phase68-trap-readiness-emulations.json','phase68-primary-manifest.json','phase68-validation.json']],
        next_action='Do not repeat42readiness/58Doorloading or unchanged offset scans. Resolve remaining candidate types via iterator/provider outputs rather than method membership alone,or pursue KeeperC8/CreatureJumpPriority/Terrain198224/Objects34C/AP0408/Spellalternatives/WanderRadius and other unresolved gameplay paths through indexed/indirect origins. Door236 andTraps204 remain unknown;new raw-preserving codec must retain them.')
    (ROOT/'phase68-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
    pack = load('editor-format-package.json')
    for key,row in pack['components'].items():
        b = (ROOT/row['file']).read_bytes()
        assert hashlib.sha256(b).hexdigest() == row['sha256'] and json.loads(b) == pack[key]
    assert len(pack['components']) == 67
    print('Phase68 published:42 native calls/2 type rejections;67 components;full goal remains active')


if __name__=='__main__':
    main()
