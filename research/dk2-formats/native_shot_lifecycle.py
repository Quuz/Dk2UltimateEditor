"""Current bounded Shot damage5/6 semantics; raw scalar storage stays unchanged."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent

def effective_parameters():
    d=json.loads((ROOT/'area-damage-semantics.json').read_text(encoding='utf8'))['effective_shot_parameters']
    for mode in [5,6]:
        row=d['damage_types'][str(mode)]
        row.update(data1='Full32-bit Data1 is negated by the native handler before the HP request. Tested+80 -> -80HP,0 ->0HP,-80(two-complementFFFFFFB0) ->+80HP. Raw API storage remainsu32.',
            data2='Integer cell extentR: filled half-open square[cx-R,cx+R) x[cy-R,cy+R), not Q12 and not a radial test.',
            phase148_evidence='shot-lifecycle-semantics.json',
            corpus_scope='No mode5 or6 among5551 source Shot records in82containers; these are executed diagnostic modes, not a discovered shipped spell.',
            execution_scope='Original raw converters/loaders/factory, complete selected ticks and explicit terminal callback, with preparedWorld/targets and explicit resource/audio IO. No normalWorldscheduler or physical poolfree.')
    d['damage_types']['5'].update(trigger='Once inside4AB180 first-tick branch while instanceDWORD149==0; branch sets149=1 afterdamage/process40. Factory alone does not damage.',
        terminal_behavior='Explicit4AAF70 before or after firsttick does not apply mode5 damage.',
        world_gate='PreparedWorld723B=1 does not suppress the selected firsttick path; the mode6 terminalgate is not checked here.')
    d['damage_types']['6'].update(trigger='Inside4AAF70 only when originalWorldvslot4C/getter509850 returnsWorldDWORD723B==0.',
        terminal_behavior='Can apply with ShotHP32 before anytick. HP reaching0 duringtick only requests generic deletion; explicit laterterminal applies damage. Prepared terminalsetsstate16=2.',
        world_gate='Native setter509860 tested0/1/2. Both1and2 suppressdamage while terminal cleanup stillruns. Meaning ofWorld723B is not yet named.')
    d['damage_types']['1']['phase148_selected_tick_control']='Two complete4AB180 ticks execute two constant-damage packets in the selected mode1/ProcessNONE scene; othermode1 lifecycle paths retain phase131 boundaries.'
    return d
