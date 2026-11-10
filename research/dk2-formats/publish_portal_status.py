"""Persist audited phase91 progress without declaring the full objective done."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
v=json.loads((ROOT/'phase91-validation.json').read_text(encoding='utf8'))
assert v['native_replays']==84 and v['previous69_components_preserved_byte_exact'] and not v['goal_achieved']
files=['portal-exit-semantics.json','phase91-validation.json','phase91-primary-manifest.json','phase91-portal-emulations.json','native_portal_offsets.py']
s=dict(phase=91,date='2026-10-28',status='Portal spawn, animation gate and actual XYZ consumer published',previous_goal_turn_classification='progress',
  findings=['Native spawn point from room anchor+1.5 cells; raw E94/E98 affect post-animation exit, not initial spawn',
    'Units4096/cell; signed/fractional XY shifts and actual map-list movement confirmed; finalZ overwritten by floor',
    'Native animation/action initializers, waiting gate, actual animation tick/null-GFX branch and AI dispatcher tested',
    '14cases84replays14complete1343byte images;117 native spans/6PE blocks; API738 exact container patches82files3734views8refusals',
    'Package70, allprevious69 components unchanged;4sourceEXEs unchanged;0game function/vtable patches'],
  evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in files],
  next_action='Trace actual consumers of loaded Variables types113/114. Review phase92-possession-copy-leads.json for immediate aggregate pointers and overlapping/indexed reads; then follow possession/stat/health paths and compare actual native outcomes under controlled variable changes. Do not infer bonus meaning from OpenKeeper names or direct-xref absence.',
  goal_achieved=False,significant_unknowns_remain=True,
  preserved_scope='All15 requested file families; possession bonus semantics,Objects34C,Traps204,Doors236,Terrain198/224,Creature Priority/Jump and remaining consumers stay open.')
(ROOT/'phase91-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
print('Verified progress91 recorded; complete15-format objective remains active')
