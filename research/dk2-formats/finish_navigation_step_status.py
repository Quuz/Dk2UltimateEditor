"""Checkpoint validated phase98 and concrete remaining research scope."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent

def main():
    validation=json.loads((ROOT/'phase98-validation.json').read_text(encoding='utf8'));assert validation['package_components']==77
    names=['navigation-step-semantics.json','NAVIGATION_STEP_RU.md','phase98-validation.json','phase98-navigation-step-emulations.json',
      'phase98-primary-manifest.json','phase98-traversal-api-validation.json','READINESS_RU.md','INTEGRATION_RU.md','editor-format-package.json']
    refs=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in names]
    status=dict(phase=98,date='2026-10-28',status='Published and validated: whole navigation step with original callbacks, four actor/map application cases, two native permission preflights',
      published_phase=98,previous_goal_turn_classification='progress',native_cases=32,package_components=77,evidence=refs,
      findings=['Whole4D5A40→4D4760 executed under explicit empty force array; original callback initializer and PE collision pointer retained.',
        'Original4B35C0/4B5560/4B5020 applied successful XY and moved actor tag between map lists.',
        'Native48EBF0 exposes definition686/10 permission for navigation category1; rawEFC→mid12EC→runtime686 statically bound.',
        '14936 exact container API checks on3734 Creature records/82 files preserve all other flag bits and bytes.',
        'User readiness examples: temple type43 recipes editable; portal post-animation XY rawE94/E98 verified separately from initial room+1.5 point; possession speed/recharge/damage partly established,remainingstats unresolved.'],
      readiness=dict(overall_estimate_percent=75,overall_range=[70,80],container_and_lossless_edit_range=[90,95],gameplay_semantics_range=[60,70],
        meaning='Expert estimate,not measured field coverage or correctness probability; unchanged by narrow new movement checks.'),
      next_action='Trace original force registration/navigation-map construction and concrete remaining unknown consumers. Prioritize scalar98/113/114 and unassigned possession stats,Objects34C,Traps204,Doors236,Terrain198/224,CreaturePriority/Jump. Reuse accepted native caches;do not count new span/hash checks as new gameplay coverage. Keep initial portal creation versus post-animationoffset and hardcoded damage reduction versus editable HP bonus distinct.',
      pending_native_jobs=[],goal_achieved=False,significant_unknowns_remain=True,limitations=validation['limitations'])
    (ROOT/'phase98-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print('Phase98 checkpoint saved; goal active, no pending native jobs')

if __name__=='__main__':main()
