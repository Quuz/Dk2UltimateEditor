"""Checksums of supplied native intermediate buffers, never raw KWD records.

No raw-to-intermediate conversion or initialization/padding assumptions here.
Gate values must be taken from the inspected loader's actual caller context.
"""
FAMILIES = {
    'Terrain': (0x2E0, 0x84), 'Rooms': (0x4F3, 0xA2),
    'Creatures': (0x1939, 0x5AF), 'Doors': (0x2DC, 0x87),
    'Traps': (0x2EB, 0x6C), 'Objects': (0x3FE, 0xF0),
    'KeeperSpells': (0x20E, 0x36), 'CreatureSpells': (0x15A, 0x1B),
    'Shots': (0x177, 0x33),
}

def _u32(value):
    if type(value) is not int or not 0 <= value <= 0xFFFFFFFF:
        raise ValueError('Expected unsigned32 integer')
    return value

def _buffer(value):
    if not isinstance(value, (bytes, bytearray, memoryview)):
        raise ValueError('Expected byte buffer')
    return bytes(value)

def definition_checksum(family, intermediate, *, gate_value=0, previous=0):
    """Gate0 resets and sums u8; nonzero preserves the previous DWORD."""
    if not isinstance(family, str) or family not in FAMILIES:
        raise ValueError('Unsupported inspected family')
    data = _buffer(intermediate)
    if len(data) != FAMILIES[family][0]:
        raise ValueError('Expected the exact native intermediate buffer length')
    _u32(gate_value); _u32(previous)
    return previous if gate_value else sum(data) & 0xFFFFFFFF

def signed_world_accumulate(intermediate, previous=0):
    """0x5108C0: add each signed8 byte to the existing world+0x72E7 DWORD."""
    data = _buffer(intermediate); _u32(previous)
    return (previous + sum(b if b < 128 else b-256 for b in data)) & 0xFFFFFFFF

def add_definition_to_family_accumulator(previous, checksum):
    """Arithmetic used by normal-insertion callbacks, not their lookup/capacity logic."""
    return (_u32(previous) + _u32(checksum)) & 0xFFFFFFFF
