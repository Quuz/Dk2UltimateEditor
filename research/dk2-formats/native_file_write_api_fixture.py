"""Bounded external WriteFile adapter for the original MyFile/Disc writer.

The PE IAT and every original stream method/vtable are retained. Only the
outside-image OS endpoint is supplied; bytes go to a research memory buffer.
"""
import struct
from native_file_api_fixture import Asm, d, jump, HANDLE

WRITE_IAT, WRITE_TARGET = 0x66C1D0, 0x28CF36


def build(exe, pe_read, *, owner, owner_size, data, capacity, state, log, code_address, limit=None, fail=False, extra_sources=()):
    assert pe_read(exe, WRITE_IAT,4) == d(WRITE_TARGET)
    a = Asm(); a.emit(bytes.fromhex('53565755'))
    a.emit(bytes.fromhex('817c2414')+d(HANDLE)); a.branch('0f85','bad')
    a.emit(bytes.fromhex('837c242400')); a.branch('0f85','bad')
    a.emit(bytes.fromhex('8b54242081fa')+d(0x7FFEE000)); a.branch('0f82','bad')
    a.emit(bytes.fromhex('81fa')+d(0x7FFF2000-4)); a.branch('0f87','bad')
    a.emit(bytes.fromhex('c702000000008b6c241c81fd')+d(0x2000)); a.branch('0f87','bad')
    a.emit(bytes.fromhex('8b74241889f001e8')); a.branch('0f82','bad')
    a.emit(bytes.fromhex('81fe')+d(owner)); a.branch('0f82','source0' if extra_sources else 'stack')
    a.emit(b'\x3d'+d(owner+owner_size)); a.branch('0f86','sourceok')
    for index,(start,size) in enumerate(extra_sources):
        a.label('source'+str(index))
        following='source'+str(index+1) if index+1<len(extra_sources) else 'stack'
        a.emit(bytes.fromhex('81fe')+d(start)); a.branch('0f82',following)
        a.emit(b'\x3d'+d(start+size)); a.branch('0f86','sourceok')
    a.label('stack'); a.emit(bytes.fromhex('81fe')+d(0x7FFEE000)); a.branch('0f82','bad')
    a.emit(b'\x3d'+d(0x7FFF2000)); a.branch('0f87','bad')
    a.label('sourceok')
    a.emit(bytes.fromhex('8b15')+d(state+4)+bytes.fromhex('83fa40')); a.branch('0f83','bad')
    if fail:
        a.emit(bytes.fromhex('31ed'))
    elif limit is not None:
        a.emit(bytes.fromhex('81fd')+d(limit)); a.branch('0f86','countok')
        a.emit(b'\xbd'+d(limit)); a.label('countok')
    a.emit(b'\xa1'+d(state)+bytes.fromhex('89c301e8')); a.branch('0f82','bad')
    a.emit(b'\x3d'+d(capacity)); a.branch('0f87','bad')
    a.emit(bytes.fromhex('6bd21481c2')+d(log)+bytes.fromhex('891a8b4c2418894a048b4c241c894a08896a0cc74210')+d(0 if fail else 1))
    a.emit(bytes.fromhex('ff05')+d(state+4)+bytes.fromhex('396c241c')); a.branch('0f84','exact')
    a.emit(bytes.fromhex('ff05')+d(state+12)); a.label('exact')
    a.emit(bytes.fromhex('8d3b81c7')+d(data)+bytes.fromhex('89e9fcf3a4012d')+d(state)+bytes.fromhex('8b542420892a'))
    a.emit(b'\xb8'+d(0 if fail else 1)); a.branch('e9','done')
    a.label('bad'); a.emit(bytes.fromhex('ff05')+d(state+8)+bytes.fromhex('31c0'))
    a.label('done'); a.emit(bytes.fromhex('5d5f5e5bc21400'))
    code = a.finish()
    return {WRITE_TARGET: jump(WRITE_TARGET,code_address), code_address: code}
