from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent

def main():
    validation=json.loads((ROOT/'phase95-validation.json').read_text(encoding='utf8'))
    assert validation['package_components']==74 and validation['native_cases']==34 and validation['previous73_components_preserved_byte_exact']
    names=['possession-neighbour-semantics.json','phase95-validation.json','phase95-primary-manifest.json','phase95-possession-neighbour-emulations.json','native_possession_neighbours.py','POSSESSION_NEIGHBOURS_RU.md']
    status=dict(phase=95,date='2026-10-28',status='Actual secondary speed flag producer, native XY-ray and raw Door dependency published',published_phase=95,
      previous_goal_turn_classification='progress:phase94 room counters/HP published;phase95 composes actual source setter,neighbour flag writer,ray,tile/map helpers and target speed plus rawDoor loader paths',
      findings=['4BFD00 sets or clears actor3EA/08 for other owner-list members according to actual XY-ray when source possessed;unpossessed-source branch clears listedmembers.',
        'Raw224/21P200 gives neighbour speed×1.5 versus×3 possessed;P0 secondary-only gives×0.5. Blocked ray removes neighbourflag.',
        'Native original Door init/converter/loader proved rawDWORD228/40→mid29C→def8B→ray→flag→speed in4newrawpaths;loadergate1 explicit.',
        '34newnativecases,153whole spans/9blocks;82Doorfiles/656records/1312exact bit40 patches;package74 withprior73 unchanged;4EXEs unchanged.'],
      evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in names],
      next_action='Trace remaining raw224 consumers:4F7AB0 queriesStat31 and scalar6F29B0 while reading receiverDWORD2D;identify caller/receiver from original xrefs before naming effect. Also revisit488930 Stat25 use at489903/489934. Fullscalar98/113/114 meaning and other15-formatunknowns remain objective.',
      native_cases=34,package_components=74,goal_achieved=False,significant_unknowns_remain=True,pending_native_jobs=[])
    (ROOT/'phase95-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8');print('phase95 published/validated;fullgoalactive;no pendingnativejobs')

if __name__=='__main__':main()
