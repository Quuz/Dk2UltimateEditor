from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
v=json.loads((ROOT/'phase92-validation.json').read_text(encoding='utf8'));assert v['native_replays']==21 and v['previous70_components_preserved_byte_exact'] and not v['goal_achieved']
files=['possession-speed-semantics.json','phase92-validation.json','phase92-primary-manifest.json','phase92-possession-speed-emulations.json','native_possession_stats.py']
s=dict(phase=92,date='2026-10-28',status='Actual type224/21 possession speed consumer and index asymmetry published',
  previous_goal_turn_classification='progress:phase91 published portal offset lifecycle;phase92 adds native raw224→setter→fullspeed evidence',
  findings=['Current type224/StatId21=200→+200%=3x speed in healthy non-liquid fixture;all walk/run/shuffle query21',
    'Getter signed16 percent and low32 product with unsigned divide100;secondary3EA/8 divides whole product by200;possessed3E6/80 wins',
    'Actual untouched current[224,21,200,10] record passed;scalar98/113/114 0/6/30000 unchanged-results constrain only this speed path',
    'Native uncheckedDWORD writer index versus low8 bounded getter:32/277 isolated writes and query behavior verified',
    '21 native cases,125whole spans6blocks,557accepted Variables/32type224/128exact API patches;package71/allprevious70 components exact;4EXEs unchanged'],
  evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in files],
  next_action='Prioritize actual possession health and other statistic consumers. Native495080 has only6 known direct call sites (speed48ABB0,whole tick488930,helper4F7AB0); this does not establish other stats unused. Trace raw224/1 and scalar98/113/114 through indexed/copied data and actual damage/health/possess event paths. Do not reinterpret health100 as proved+100% or declare scalar fields globallyunused.',
  goal_achieved=False,significant_unknowns_remain=True,
  preserved_scope='All15 KWD/KLD families;possession health/otherstats,Objects34C,Traps204,Doors236,Terrain198/224,Creature Priority/Jump and other remaining consumers stay open.')
(ROOT/'phase92-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Verified progress92 saved; full objective active')
