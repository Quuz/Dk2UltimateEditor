"""Exact selected rawLevel -> runtime bindings of the original Level callback.

Aliases come from the existing physical schema; runtime transport does not
certify the later gameplay use of text/reward identifiers.
"""
import struct
from native_level_descriptor import SIZE,patch_raw

BINDINGS=[
    dict(name='TriggerId',raw=0x1180,size=2,target='World',offset=0x7310,width=2),
    dict(name='TicksPerSecond',raw=0x1182,size=2,target='global',offset=0x6F2590,width=4),
    dict(name='TalismanPieces',raw=0x63AE,size=1,target='global',offset=0x6F295B,width=1),
    dict(name='OneShotHornyLevel',raw=0x6403,size=1,target='global',offset=0x6F295C,width=1),
    dict(name='SpeechHornyId',raw=0x6407,size=2,target='global',offset=0x6F295D,width=1),
    dict(name='SoundTrack',raw=0x63B7,size=1,target='World',offset=0x720D,width=1),
    dict(name='TextTitleId',raw=0x63B9,size=2,target='World',offset=0x9B64,width=4),
    dict(name='TextPlotId',raw=0x63BB,size=2,target='World',offset=0x9B68,width=4),
    dict(name='TextObjectiveId',raw=0x63BF,size=2,target='World',offset=0x9B70,width=4),
    dict(name='TextSubObjectiveId2',raw=0x63C5,size=2,target='World',offset=0x9B74,width=4),
    dict(name='TextSubObjectiveId3',raw=0x63C7,size=2,target='World',offset=0x9B78,width=4),
    dict(name='TextSubObjectiveId1',raw=0x63C3,size=2,target='World',offset=0x9B7C,width=4),
    dict(name='SpecialLevelNumber',raw=0x63C9,size=1,target='World',offset=0x722E,width=1),
    dict(name='UniqueCreatureTypeId',raw=0x63CA,size=1,target='World',offset=0x722F,width=1),
    dict(name='RewardPrevious',raw=0x63AF,size=4,target='World',offset=0x9B80,width=4,opaque_bytes=True),
    dict(name='RewardPreviousExtra',raw=0x6405,size=1,target='World',offset=0x9B84,width=1),
    dict(name='RewardNext',raw=0x63B3,size=4,target='World',offset=0x9B86,width=4,opaque_bytes=True),
    dict(name='RewardNextExtra',raw=0x6406,size=1,target='World',offset=0x9B8A,width=1),
    dict(name='IntroductionCreatureId',raw=0x63CB,size=8,target='World',offset=0x9B8C,width=8,opaque_bytes=True),
    dict(name='IntroductionTextId',raw=0x63D3,size=16,target='World',offset=0x9B94,width=16,opaque_bytes=True),
    dict(name='SpeechPrelevelAndPostlevelIds',raw=0x6409,size=10,target='World',offset=0x9BA4,width=10,opaque_bytes=True)]


def project(payload):
    if len(payload)!=SIZE:raise ValueError('Expected modern25683-byte rawLevel payload')
    out=[]
    for row in BINDINGS:
        data=payload[row['raw']:row['raw']+row['size']]
        value=data if row.get('opaque_bytes') else (int.from_bytes(data,'little')&((1<<(8*row['width']))-1)).to_bytes(row['width'],'little')
        out.append(dict(row,data=value))
    return out


def flags(previous_runtime_bits,raw_flags):
    if type(previous_runtime_bits) is not int or not 0<=previous_runtime_bits<=0xFFFFFFFF or type(raw_flags) is not int or not 0<=raw_flags<=65535:raise ValueError('Expected u32 runtime and physicalu16 flags')
    return (previous_runtime_bits&~0x7FF)|((raw_flags>>2)&0x7FF)


def patch(payload,name,value):
    row=next((r for r in BINDINGS if r['name']==name),None)
    if row is None:raise ValueError('Unknown checked binding')
    if row.get('opaque_bytes'):
        if type(value) is not bytes or len(value)!=row['size']:raise ValueError('Expected exact physical bytes')
        data=value
    else:
        if type(value) is not int or not 0<=value<(1<<(8*row['size'])):raise ValueError('Outside physical field width')
        data=value.to_bytes(row['size'],'little')
    return patch_raw(payload,row['raw'],data)
