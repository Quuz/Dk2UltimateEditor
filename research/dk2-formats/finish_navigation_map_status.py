"""Persist verified phase99 and prioritize remaining unknown field consumers."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent

def main():
    v=json.loads((ROOT/'phase99-validation.json').read_text(encoding='utf8'));assert v['package_components']==78
    names=['navigation-map-semantics.json','NAVIGATION_MAP_RU.md','phase99-validation.json','phase99-navigation-map-emulations.json',
      'phase99-primary-manifest.json','phase99-terrain-flags-api-validation.json','READINESS_RU.md','INTEGRATION_RU.md','editor-format-package.json']
    refs=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in names]
    s=dict(phase=99,date='2026-10-28',status='Published and validated: Terrain flags→original cache/CMap NAV producer→whole native movement',
      published_phase=99,previous_goal_turn_classification='progress',native_cases=20,raw_terrain_loader_compositions=8,package_components=78,evidence=refs,
      findings=['Fulloriginal44FC40 loading-mode placement writes NAV through4D1970 and native movement reads it throughoriginalcallback.',
        'Terrain rawDWORD190→mid248→runtime17B; priority SOLID1→8,WATER80h→1,LAVA100h→3,default0; wholebyteassignment.',
        'Water/lava permission Creature686/10/20 changes the observed controller step after realTerrain placement.',
        'Original455D40 rebuilds1024-byte cache using realWorld enumeration/count/getter;no vtable or code patch.',
        '19836 exactcontainer API checks on3306 records/82Terrain sources preserve all otherbits/bytes.',
        'Two measuring-fixture diagnostics excluded: clobberedcallbackEAX and missing expectedWorld9F6 Sessiontick assignment.'],
      next_action='Return to remaining unknown KWD consumers instead of extending known movement fixture permutations. Inspect original pickup/ability getter virtual interfaces and older-profile dataflow for CreaturePriority6EB/rawF55 andJump75C/rawEE0; review unclosed summarylimits fromphase73/85. If no typed evidence,retainunknown and pursue Traps98/raw204,Doors99/raw236,Objects106/raw34C,Terrain183/1B5 and scalar113/114 via actual structures/callers. Normal4D1930/4CE770graph rebuild remains a documented separate navigation gap,not a prerequisite to editing the proven flags.',
      readiness=dict(overall_estimate_percent=75,range=[70,80],meaning='Expert estimate,not field count or correctnessprobability'),
      pending_native_jobs=[],goal_achieved=False,significant_unknowns_remain=True,limitations=v['limitations'])
    (ROOT/'phase99-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Phase99 checkpoint saved; goal active; no pending native jobs')

if __name__=='__main__':main()
