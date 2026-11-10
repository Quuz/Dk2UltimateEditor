"""Read-only readiness return codes; no target selection or firing simulation."""
from native_trap_loading import DX, OLD, sized, u32

PROFILES = {
    DX: dict(program='DKII-DX.exe', entry=0x4A7D10, clock=0x517490, mana=0x4C1740,
             instance_flags=0x10C, deadline=0xFF, definition=0xF7, state_word=0x115,
             clock_global=0x6E5050, exempt_owner=0x6F254C),
    OLD: dict(program='DKII-local13.exe', entry=0x49EB20, clock=0x50B3C0, mana=0x4B79F0,
              instance_flags=0x10B, deadline=0xFE, definition=0xF6, state_word=0x114,
              clock_global=0x6CFEE0, exempt_owner=0x6DD5D4),
}


def s32(v):
    return v if v < 0x80000000 else v - 0x100000000


def return_code(*, instance_flags, deadline, tick, definition_flags, owner, exempt_owner, mana_cost, mana, state_word):
    for v in [instance_flags, deadline, tick, definition_flags, owner, exempt_owner, mana_cost, mana, state_word]:
        u32(v)
    if instance_flags > 255 or owner > 65535 or exempt_owner > 65535 or state_word > 65535:
        raise ValueError('Expected BYTE flags and WORD owner/state values')
    if instance_flags & 0x20:
        return 1
    if deadline > tick:
        return 0
    if definition_flags & 0x100 and owner != exempt_owner and mana_cost != 0 and s32(mana_cost) > s32(mana):
        return 2
    return 3 if state_word else 4

