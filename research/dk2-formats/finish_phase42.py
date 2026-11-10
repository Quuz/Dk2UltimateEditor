"""Refresh current pointers and explicitly retain the phase41 prefix correction."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent
def main():
    updates={'INTEGRATION_RU.md':[('51 компонент, SHA-256 каждой исходной схемы','53 компонента, SHA-256 каждой исходной схемы'),('Актуальное дополнение, этап41: пакет содержит52 компонента.','Предыдущий этап41 расширил пакет до52 компонентов.')],
             'REPORT_RU.md':[('Пакет для редактора содержит52 компонента.','Пакет для редактора содержит53 компонента.'),('Пакет для редактора содержит51 компонент.','Пакет для редактора содержит53 компонента.')],
             'SPELL_WRITER_RU.md':[('или все320 байт intermediate.','или320-байтные префиксы intermediate.')]
            }
    for file,pairs in updates.items():
        p=ROOT/file;t=p.read_text(encoding='utf8')
        for before,after in pairs:
            if before in t:t=t.replace(before,after)
        p.write_text(t,encoding='utf8')
    p=ROOT/'phase41-validation.json';r=json.loads(p.read_text(encoding='utf8'));r['scope_correction_phase42']=dict(native_intermediate_bytes=346,phase41_compared_prefix_bytes=320,phase42_complete_buffer_validation='phase42-validation.json',note='Original61 native inputs/results retained. Phase41 converter cases compared a320-byte prefix, not the whole346-byte native intermediate. Writer/model/flags results unchanged.');p.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf8')
    candidates=ROOT/'phase42-editor-alternative-candidates.json'
    status=dict(phase=42,status='native_alternative_reference_paths_and_complete_intermediate_pass_complete; gameplay_replacement_rule_open',candidate_search=dict(file=candidates.name,sha256=hashlib.sha256(candidates.read_bytes()).hexdigest(),warning='16 displacement candidates were leads; only verified native bodies used for conclusions.'),positive_result='spell-alternative-semantics.json',validation='phase42-validation.json',correction='phase41 prefix320; full native object346. Raw tail remains ignored; earlier writer cases retained.',unresolved=['Alternative Shot/Room gameplay activation or replacement outside inspected loader','Creatures AbilityJump/PickUpPriority gameplay consumers','Terrain198/224,Traps204,Objects34C consumer meaning','KeeperSpellsC8 purpose'],next_action='Return to remaining catalogue unknowns via native pointer aliasing or old-version readers. Creature extended80-byte region writer/converter provenance is another uncompleted candidate; do not mark it scratch without native proof.')
    (ROOT/'phase42-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    assert json.loads((ROOT/'editor-format-package.json').read_text(encoding='utf8'))['spell_writer_semantics']['converter']['intermediate_bytes']==346
    print('Phase42 current documentation pointers and explicit phase41 prefix correction saved')
if __name__=='__main__':main()
