"""Bounded DX decision for an already invoked Creature sacrifice callback."""
def decision(*,room_present,room_category,surface_height,cell_owner,creature_owner):
    if not room_present or room_category!=13:return 'none'
    if surface_height==0:return 'sacrifice'
    if cell_owner&0xFFF==creature_owner&0xFFFF:return 'request_state67'
    return 'none'
