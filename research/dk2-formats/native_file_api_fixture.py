"""Bounded external Windows imports for original MyFile/Disc read/seek.

No game instruction, PE IAT entry or native vtable is replaced. API targets
are the exact outside-image addresses stored in the original PE IAT. Longer
adapters live at separate addresses; five-byte jumps are external-only thunks.
ReadFile is synchronous for one supplied memory-backed handle, with true EOF
short reads and bounded destinations. No real Windows file or network access.
"""
import struct
from build_effect_semantics import pe_read

READ_TARGET,SEEK_TARGET,REF_TARGET=0x28CF2A,0x28CF42,0x28C5D4
READ_IAT,SEEK_IAT,REF_IAT=0x66C1D4,0x66C1D8,0x66C0A0
HANDLE=0xABCDEF01

class Asm:
    def __init__(self):self.code=bytearray();self.labels={};self.fixups=[]
    def emit(self,b):self.code+=b
    def label(self,name):self.labels[name]=len(self.code)
    def branch(self,op,name):self.emit(bytes.fromhex(op));self.fixups.append((len(self.code),name));self.emit(bytes(4))
    def finish(self):
        for off,name in self.fixups:struct.pack_into('<i',self.code,off,self.labels[name]-off-4)
        return bytes(self.code)

def d(v):return struct.pack('<I',v&0xFFFFFFFF)
def jump(src,dest):return b'\xE9'+d(dest-src-5)

def build(exe,*,source,size,state,log,read_code,seek_code,ref_address):
    assert size<0x80000000
    for iat,target in [(READ_IAT,READ_TARGET),(SEEK_IAT,SEEK_TARGET),(REF_IAT,REF_TARGET)]:assert pe_read(exe,iat,4)==d(target)
    a=Asm();a.emit(bytes.fromhex('53565755')) # preserve EBX/ESI/EDI/EBP
    a.emit(bytes.fromhex('817c2414')+d(HANDLE));a.branch('0f85','bad')
    a.emit(bytes.fromhex('837c242400'));a.branch('0f85','bad') # synchronous only
    a.emit(bytes.fromhex('8b54242081fa')+d(0x7FFEE000));a.branch('0f82','bad')
    a.emit(bytes.fromhex('81fa')+d(0x7FFF2000-4));a.branch('0f87','bad')
    a.emit(bytes.fromhex('c70200000000'))
    a.emit(bytes.fromhex('8b6c241c81fd')+d(65536));a.branch('0f87','bad')
    a.emit(bytes.fromhex('8b7c241889f801e8'));a.branch('0f82','bad') # dest+request
    a.emit(bytes.fromhex('81ff')+d(0x746EC0));a.branch('0f82','stackdest')
    a.emit(bytes.fromhex('3d')+d(0x756EC0));a.branch('0f86','destok')
    a.label('stackdest');a.emit(bytes.fromhex('81ff')+d(0x7FFEE000));a.branch('0f82','bad')
    a.emit(bytes.fromhex('3d')+d(0x7FFF2000));a.branch('0f87','bad')
    a.label('destok');a.emit(b'\xA1'+d(state)+b'\xB9'+d(size)+bytes.fromhex('29c1'));a.branch('0f82','bad')
    a.emit(bytes.fromhex('39cd'));a.branch('0f86','requested')
    a.branch('e9','copy');a.label('requested');a.emit(bytes.fromhex('89e9'))
    a.label('copy');a.emit(bytes.fromhex('89cb8db0')+d(source)+bytes.fromhex('f3a4'))
    a.emit(bytes.fromhex('011d')+d(state)+bytes.fromhex('8b542420891a'))
    a.emit(bytes.fromhex('8b15')+d(state+4)+bytes.fromhex('83fa40'));a.branch('0f83','bad')
    a.emit(bytes.fromhex('6bd21481c2')+d(log)+bytes.fromhex('89028b4c2418894a048b4c241c894a08895a0cc7421001000000'))
    a.emit(bytes.fromhex('ff05')+d(state+4)+bytes.fromhex('395c241c'));a.branch('0f84','good')
    a.emit(bytes.fromhex('ff05')+d(state+8))
    a.label('good');a.emit(b'\xB8'+d(1));a.branch('e9','done')
    a.label('bad');a.emit(bytes.fromhex('ff05')+d(state+12)+bytes.fromhex('31c0'))
    a.label('done');a.emit(bytes.fromhex('5d5f5e5bc21400'));read=a.finish()
    a=Asm();a.emit(bytes.fromhex('817c2404')+d(HANDLE));a.branch('0f85','bad')
    a.emit(bytes.fromhex('837c240c00'));a.branch('0f85','bad')
    a.emit(bytes.fromhex('8b4424088b54241083fa00'));a.branch('0f84','store')
    a.emit(bytes.fromhex('83fa01'));a.branch('0f84','relative')
    a.emit(bytes.fromhex('83fa02'));a.branch('0f85','bad')
    a.emit(b'\x05'+d(size));a.branch('e9','store')
    a.label('relative');a.emit(bytes.fromhex('0305')+d(state))
    a.label('store');a.emit(bytes.fromhex('85c0'));a.branch('0f88','bad')
    a.emit(b'\xA3'+d(state)+bytes.fromhex('ff05')+d(state+16));a.branch('e9','done')
    a.label('bad');a.emit(bytes.fromhex('ff05')+d(state+12)+b'\xB8'+d(0xFFFFFFFF))
    a.label('done');a.emit(bytes.fromhex('c21000'));seek=a.finish()
    ref=bytes.fromhex('8b4424043d')+d(ref_address)+bytes.fromhex('7507ff008b00c2040031c0c20400')
    return {READ_TARGET:jump(READ_TARGET,read_code),SEEK_TARGET:jump(SEEK_TARGET,seek_code),
        REF_TARGET:ref,read_code:read,seek_code:seek}
