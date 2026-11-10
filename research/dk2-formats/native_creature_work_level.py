"""Narrow raw work-modifier storage API for known5449/5537-byte Creature records.

CF2 is loaded bit-exact atdefinition797 and added modulo32 toActor3AF by full
native48B120 on eligiblelevel-up. CEE remains a nativeGUIlabel/storage binding;
its gameplayinitialization is not established by the selectedlevel-up path.
"""
FIELDS={'level_increase':0xCF2,'default':0xCEE}
def check(raw):
    if not isinstance(raw,(bytes,bytearray)) or len(raw) not in [5449,5537]:raise ValueError('Expected known5449/5537-byte Creature record')
def view(raw):
    check(raw);return {k:int.from_bytes(raw[o:o+4],'little') for k,o in FIELDS.items()}
def patch(raw,field,value):
    check(raw)
    if field not in FIELDS or type(value) is not int or not 0<=value<=0xFFFFFFFF:raise ValueError('Expected field/default or level_increase and unsigned32 bitpattern')
    o=FIELDS[field];return bytes(raw[:o])+value.to_bytes(4,'little')+bytes(raw[o+4:])
def increment_modifier(current,increment,*,eligible):
    if any(type(x) is not int or not 0<=x<=0xFFFFFFFF for x in [current,increment]) or type(eligible) is not bool:raise ValueError('Expected u32 bitpatterns and eligibility bool')
    return (current+(increment if eligible else 0))&0xFFFFFFFF
