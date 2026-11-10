"""Bounded current mode8 semantics and a lossless Terrain destroyed-type patch."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parent

def effective_parameters():
    d=json.loads((ROOT/'shot-lifecycle-semantics.json').read_text(encoding='utf8'))['effective_shot_parameters']
    d['damage_types']['8'].update(
        data1='Native32-bit NEG forms the HPdelta. Its sign selectsdamage/healbranch; TerrainHPapplication uses low16magnitude/addend. Tested80->80damage,65536->0,65537->1,-80->80healing,INT_MIN->0inselectedcells.',
        data2='SignedinstanceData2 arithmetic>>12 givescell extentR. Filledhalf-open square[cx-R,cx+R)x[cy-R,cy+R), Xouter/Yinner; Worldboundschecked percell; no radialsampling.',
        trigger='Complete4AB180tick calls4AC3C0 ->4ACCA0; onepacket perselectedtick. Directdispatcher paths alsotested.',
        target_filter='Terrainflags definition17B musthaveSOLID1 andnotIMPENETRABLE2. NoCreature/attackcategory/resistances inthisdamagepath.',
        owner='452F60 receives cellWORD8 &FFF asowner; not theShotcreatorowner. Unclaimedowner0 usedforcoherentdestructioncases.',
        terrain_transition='WhenHPbecomes0, TerrainDestroyedType differsfromcurrenttype -> originalCMap44FC40 setsnewtype. SourceRock2 becomesDirtPath3 withStartingHealth1 andnavbyte0; selfDestroyedType2 leavesHP0/SOLIDtype2/nav8.',
        corpus_scope='No mode8 among5551 sourceShotrecords; executeddiagnosticmode, not discoveredshippedTremorspell.',
        phase151_evidence='tremor-damage-semantics.json',
        execution_scope='OriginalrawShot/rawTerrain loaders/factory, realNAV initialization, complete selectedticks, realcellHP/CMap/typechange/NAVupdate. PreparedWorld/actors, explicitresourcefailure/audioIO, noGPU/liveWorldscheduler.')
    return d

def patch_destroyed_type(raw,terrain_id):
    if type(raw) is not bytes or len(raw) not in [548,552] or type(terrain_id) is not int or not 0<=terrain_id<=255:
        raise ValueError('Expected548/552-byte Terrain andu8 destroyedtype ID')
    return raw[:0x1DA]+bytes([terrain_id])+raw[0x1DB:]
