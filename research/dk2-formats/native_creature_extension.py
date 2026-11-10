"""Preserve separate game/editor flags and opaque Creature record extension."""
import struct
GAME_FLAGS_OFFSET=0x1545
OPAQUE_OFFSET=0x1549
EDITOR_FLAGS_OFFSET=0x1599
LAST_WORDS_OFFSET=0x159D

def check(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw) not in [5449,5537]:raise ValueError('Expected5449- or5537-byte Creature record')

def view(raw):
    check(raw);long=len(raw)==5537
    return dict(record_bytes=len(raw),game_flags=struct.unpack_from('<I',raw,GAME_FLAGS_OFFSET)[0],
                opaque80_hex=raw[OPAQUE_OFFSET:EDITOR_FLAGS_OFFSET].hex() if long else None,
                editor_flags=struct.unpack_from('<I',raw,EDITOR_FLAGS_OFFSET)[0] if long else None,
                last_words_hex=raw[LAST_WORDS_OFFSET:].hex() if long else None)

def patch_flags(raw,slot,mask,enabled):
    check(raw)
    if slot not in ['game','editor'] or type(mask) is not int or not 0<=mask<=0xFFFFFFFF or type(enabled) is not bool:raise ValueError('Expected game/editor slot, DWORD mask and bool')
    if slot=='editor' and len(raw)!=5537:raise ValueError('Short record has no editor flag slot; implicit extension forbidden')
    off=GAME_FLAGS_OFFSET if slot=='game' else EDITOR_FLAGS_OFFSET;old=struct.unpack_from('<I',raw,off)[0];new=old|mask if enabled else old&(~mask&0xFFFFFFFF);out=bytearray(raw);struct.pack_into('<I',out,off,new);return bytes(out)

def patch_opaque80(raw,value):
    check(raw)
    if len(raw)!=5537 or not isinstance(value,(bytes,bytearray)) or len(value)!=80:raise ValueError('Explicit80-byte replacement requires long record')
    return bytes(raw[:OPAQUE_OFFSET])+bytes(value)+bytes(raw[EDITOR_FLAGS_OFFSET:])

def native_stack_tail(context_address,probe_return_address=0x4A7F10):
    """Four emitted bytes for inspected writer/probe stack layout, not file data."""
    if any(type(n) is not int or not 0<=n<=0xFFFFFFFF for n in [context_address,probe_return_address]):raise ValueError('Expected DWORD addresses')
    return struct.pack('<I',context_address)[1:]+bytes([probe_return_address&255])

def writer_suffix(stack84,editor_flags,context_address,main_name=b''):
    """Diagnostic last92-byte projection; never use this as a lossless writer."""
    if not isinstance(stack84,(bytes,bytearray)) or len(stack84)!=84:raise ValueError('Expected84 preexisting stack bytes')
    if type(editor_flags) is not int or not 0<=editor_flags<=0xFFFFFFFF:raise ValueError('Expected DWORD flags')
    if not isinstance(main_name,bytes) or len(main_name)>5537:raise ValueError('Name must fit controlled diagnostic record')
    out=bytearray(stack84)+struct.pack('<I',editor_flags&15)+native_stack_tail(context_address)
    if len(main_name)>GAME_FLAGS_OFFSET:out[:min(len(main_name)-GAME_FLAGS_OFFSET,len(out))]=main_name[GAME_FLAGS_OFFSET:GAME_FLAGS_OFFSET+len(out)]
    return bytes(out)
