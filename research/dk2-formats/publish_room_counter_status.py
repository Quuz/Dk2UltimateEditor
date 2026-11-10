from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent

def main():
    validation=json.loads((ROOT/'phase94-validation.json').read_text(encoding='utf8'))
    assert validation['package_components']==73 and validation['native_cases']==35 and validation['previous72_components_preserved_byte_exact']
    evidence=[dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest()) for n in [
      'room-species-bonus-semantics.json','phase94-validation.json','phase94-primary-manifest.json',
      'phase94-room-counter-emulations.json','native_room_counters.py','CRYPT_BONUSES_RU.md']]
    status=dict(phase=94,date='2026-10-28',status='Real producer of owner damage-reduction counter and its Crypt binding published',
      previous_goal_turn_classification='progress: reconstructed species counter writer and room26 callers from original code; current turn composes setter and HP consumer natively',
      findings=['Monk19 registration4D8E60→4D93E0 increments room ownerBYTE1095;unregistration reverses it.',
        'Crypt binding supported by two static room1Ah entry paths,currentRoomsID26 and original Crypt.cpp source-path string.',
        'Counter1 reduces ordinary damage factor by8/128;1000HP/request−100→907 normal,957 possessed;clear→950 possessed;other owner remains950.',
        '35native cases/18groups;both45-index switch tables statically verified,41mappedIDs;BYTE wrap/sign clamp boundaries executed.',
        '143whole native spans/9blocks;73package components withprior72 retained byte-exact;4EXEs and game data unchanged;no new raw-editable percentage found.'],
      evidence=evidence,next_action='Trace actual consumers of remaining17 owner counter slots108A..109B and actual writer of actor3EA/08. Follow resulting dataflow into raw224 and scalar98/113/114,without assigning names from arithmetic alone. Other high-priority raw unknowns retain full objective scope.',
      goal_achieved=False,significant_unknowns_remain=True,
      preserved_scope='All15 requested format families. Fullpossession cycle,Crypt gameplay/cleanup branches,Objects34C,Traps204,Doors236,Terrain198/224,CreaturePriority/Jump and remaining consumers open.')
    (ROOT/'phase94-search-status.json').write_text(json.dumps(status,ensure_ascii=False,indent=2),encoding='utf8')
    print('phase94 status saved;full goal active;no pending native run')

if __name__=='__main__':main()
