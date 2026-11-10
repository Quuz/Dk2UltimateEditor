from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def main():
    v=json.loads((ROOT/'phase100-validation.json').read_text(encoding='utf8'));assert v['package_components']==79
    names=['unknown-instruction-boundary-review.json','UNKNOWN_BOUNDARIES_RU.md','phase100-validation.json','phase100-object-boundary-review.json','phase100-primary-manifest.json','INTEGRATION_RU.md','READINESS_RU.md','editor-format-package.json']
    refs=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in names]
    s=dict(phase=100,date='2026-10-28',status='Publishedboundedstaticreview: six remaining phase70 Object anchored candidates rejected; five omittedswitch arms recovered',
      published_phase=100,previous_goal_turn_classification='progress',package_components=79,native_cases=0,new_raw_API_patches=0,evidence=refs,
      findings=['Four falseDWORD104 decodes omit66prefix of realWORD104;no byte106 read.',
        'One candidate starts inJMP displacement;one inside immediatezero. ExactPE/Ghidra bounds verified.',
        'Fiveway4979B0switch recovers317 instructions;metadata omission not absenceofcode.',
        'NeighbourrealDWORD107 follows localthis spill/reload;647CFGstates,explicitfixedframe/ABI assumptions. Genericwalker stacklimit retained.',
        'Sixcodeextents/one20bytetable,84sourceSHA/4EXEs;prior78schemas exact. No newunknownfield meaning.'],
      recovery='GhidraJava/listener actually absent after environmentgap;verified beforelaunching savedGUIproject. Oldprofile imported fromresearchcopy and saved;currentDX/oldbothopen on8089. No restartbasedonlyontimeout.',
      next_action='Do not repeat sixphase70anchored candidates or historicaltypedCTrapmana paths. Seek a newactualconsumer path: cross-version originalscalar113/114 loaders andtheir loaded addresses,unassignedCreaturestats,or remainingopaqueAP/EffectElement fields. UnknownObjects34C/Traps204/Doors236/PriorityJump/Terrain198224 persist;no broadernonuse claim. Reuse accepted native caches;phase100haszero newnative/patchcounts.',
      pending_native_jobs=[],goal_achieved=False,significant_unknowns_remain=True,readiness_estimate_percent=75,limitations=v['limitations'])
    (ROOT/'phase100-search-status.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print('Phase100 checkpoint saved;goal active')
if __name__=='__main__':main()
