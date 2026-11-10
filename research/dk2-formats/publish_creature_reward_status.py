"""Record verified phase90 progress and the next unresolved native lead."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def main():
    v=json.loads((ROOT/'phase90-validation.json').read_text(encoding='utf8'))
    assert v['native_replays']==66 and v['previous68_components_preserved_byte_exact'] and not v['goal_achieved']
    evidence=[]
    for n in ['creature-reward-semantics.json','phase90-validation.json','phase90-primary-manifest.json','phase90-creature-factory-emulations.json',
              'phase90-sacrifice-creature-emulations.json','CREATURE_REWARDS_RU.md','READINESS_RU.md','editor-format-package.json','INTEGRATION_RU.md']:
        evidence.append(dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()))
    s=dict(phase=90,date='2026-10-28',status='Successful native Creature temple reward and conditional room entry published',
      previous_goal_turn_classification='progress: phase89 published Object/gold; phase90 adds actual Creature ctor/factory/level/state-entry and raw recipe composition',
      findings=['11 distinct cases/66 native replays;11 complete1343-byte actor images; all checked fixture/guard bytes pass',
        'Requested levels0/1→1,3→3,10/11/255→10 in alive fixture;level2 health coefficient150→HP1500',
        'Native AI category/state initialization and actual category12 callback execute; full temple retains awarded creature but rejects state6A',
        'raw43→native loader/history/matcher→factory/levels/room-entry yields exact same actor as direct runtime reward',
        '89 whole native spans/6 PE blocks, original4EXE hashes, allprevious68 schema components verified; new component69',
        'Game function/vtable/AI callback patches0; prepared runtime definitions and explicit external GFX/sound/OS providers documented'],
      evidence=evidence,goal_achieved=False,significant_unknowns_remain=True,
      next_action='Follow portal first-spawn lifecycle and actual placement: native state193/C1 category7 callback459C20, mapped by47BBE0 writer47EE22. Determine predecessor state requests, cell selection and animation gating496D00; then run bounded original callback/placement with raw E94/E98/E9C changes. Current mapping is a lead, not proof of first-portal lifecycle.',
      preserved_scope='All15 requested KWD/KLD families. Objects34C,Traps204,Doors236,Terrain198/224,Priority/Jump and possession bonus consumers remain open; keep unknown bytes and do not edit source game/editor files.')
    (ROOT/'phase90-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Verified progress90 saved; goal remains active; native portal state193 lead retained')
if __name__=='__main__':main()
