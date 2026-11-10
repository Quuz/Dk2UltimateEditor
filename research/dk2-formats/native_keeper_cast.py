"""Observed DX Keeper cast selection, not a full gameplay/cast validator."""
import struct
from native_keeper_pipeline import check
ROUTES={'A':0x4BBF90,'B':0x4BC290}
def selection_view(raw,upgrade_bits,route):
    check(raw)
    if type(upgrade_bits) is not int or not 0<=upgrade_bits<=0xFFFFFFFF:raise ValueError('Expected DWORD upgrade state')
    if not isinstance(route,str) or route not in ROUTES:raise ValueError('Expected observed DX cast route A/B')
    primary=raw[0xF0];upgraded=upgrade_bits!=0;selected=raw[0x113] if upgraded else primary;offset=0x114 if upgraded else 0xD0
    d1,d2=struct.unpack_from('<II',raw,offset)
    return dict(route=route,selected_shot_id=selected,primary_shot_id=primary,factory_shot_id=primary if selected else None,data1_bits=d1,data2_bits=d2,selected_shot_nonzero=bool(selected),target_flag_one=raw[0xDA] in [1,2,3,6],position_z_add=0x2000 if route=='A' and primary==21 else 0,native_return_bits=0,
        scope='Field selection only; real availability/mana/target checks and factory can still reject. A zero factory ID passed by wrapper is not proof a real entity0 can be created.')
