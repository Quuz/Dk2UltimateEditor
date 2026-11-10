"""Post-return256-byte capture with8native VMOVDQU loads;no AVX512 userops."""
import struct
REGISTERS=','.join('YMM'+str(i) for i in range(8))
def code(address):
    return b''.join(bytes.fromhex('c5fe6f')+bytes([5+8*i])+struct.pack('<I',address+32*i) for i in range(8))
def decode(registers):return b''.join(int(registers['YMM'+str(i)],16).to_bytes(32,'little') for i in range(8))
