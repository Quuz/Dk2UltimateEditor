"""Synthetic post-return readout through eight128-bit XMM registers.

This uses existing emulate_function return_registers without enabling scripts
or changing any original code. Native return must be saved before this code.
Only observer code loads XMM; this assigns no SIMD behavior to the game.
"""
import struct
REGISTERS=','.join(f'XMM{i}' for i in range(8))
def code(address):
    if type(address) is not int or not 0<=address<=0xFFFFFF80:raise ValueError('Expected readable128-byte range')
    return b''.join(bytes([0xF3,0x0F,0x6F,0x05|(i<<3)])+struct.pack('<I',address+16*i) for i in range(8))
def decode(registers):return b''.join(int(registers[f'XMM{i}'],16).to_bytes(16,'little') for i in range(8))
