"""Lossless Object StartState byte edits; supplied-DX registry classification."""
from native_object_lifetime import check


def patch_start_state(raw, value):
    check(raw)
    if type(value) is not int or not 0 <= value <= 255:
        raise ValueError('StartState must fit an unsigned byte')
    return raw[:0x35B] + bytes([value]) + raw[0x35C:]


def supplied_dx_state_status(value):
    if type(value) is not int or not 0 <= value <= 255:
        raise ValueError('Expected an unsigned state byte')
    if value == 0:
        return 'no active callback; dispatcher may copy definition StartState'
    if value in [2, 7, 57, 58]:
        return 'initialized callback returns zero; dispatcher resets active state to zero'
    if value in [16, 22, 51, 56]:
        return 'initialized callback returns one without object writes'
    if value < 59:
        return 'initialized callback; state-specific behavior required'
    return 'outside 0..58 registry initialized by this exact unpatched DX; preserve stored byte'
