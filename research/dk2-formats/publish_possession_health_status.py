from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
v=json.loads((ROOT/'phase93-validation.json').read_text(encoding='utf8'));assert v['native_replays']==31 and v['previous71_components_preserved_byte_exact'] and not v['goal_achieved']
files=['possession-health-semantics.json','phase93-validation.json','phase93-primary-manifest.json','phase93-possession-health-emulations.json','native_possession_health.py']
s=dict(phase=93,date='2026-10-28',status='Ordinary possession damage reduction,healing ceiling and original event path published',
  previous_goal_turn_classification='progress:phase92 actualspeed published;phase93 adds raw224/65/239→full HP consumer and native event dispatch',
  findings=['Ordinary possessed damage scale128−64=64;−100→−50,−101→−50,−1→0;WORD443 shares OR subtraction,not extra stacking',
    'Healing ceiling baseCreatureHP+raw65/HealthLevel;raw224/Health andscalar98/113/114 do not change tested damage/heal-cap paths',
    'Raw23950→WORD64 throughnative51CD90 interactswithflag8000;withpossessed flag scale0 in fixture',
    'Over-ceiling preparedHP1500/request+200→1250 possessed versus1000 normal is conditional arithmetic,not proof of normal-game/editorbug',
    '31newnativecases15events,132whole native spans6blocks;557acceptedVariables9healthlevel36exactAPIpatches;package72/allprevious71 unchanged;4EXEs unchanged'],
  evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in files],
  next_action='Trace real writers/receivers of ownerBYTE1095 (used as signed positive count reducing damage by8 perunit) using phase94-owner-health-modifier-leads.json. Also trace actor3EA/8 writer and full possession/upgraded/friendly control path; seek actual scalar98/113/114 and otherstat224 consumers without declaring them unused from negative direct scans.',
  goal_achieved=False,significant_unknowns_remain=True,
  preserved_scope='All15 requested format families. Fullpossession cycle,death/special states,Objects34C,Traps204,Doors236,Terrain198/224,Creature Priority/Jump and remaining consumers stay open.')
(ROOT/'phase93-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Verified progress93 saved; full15-format objective active')
