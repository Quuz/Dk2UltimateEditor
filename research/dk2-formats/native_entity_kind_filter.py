"""Explicit runtime-list projection; not a KWD/KLD record parser."""
from native_trap_loading import DX, OLD, u32

PROFILES = {
    DX: dict(program='DKII-DX.exe', entry=0x453700, table=0x6EE540),
    OLD: dict(program='DKII-local13.exe', entry=0x44C250, table=0x6D95C8),
}


def first_match(head, entries, kind, owner, previous_output):
    """Return (native Boolean, pointer), preserving output on no match.

    Entries describe runtime slots, not file records. A cycle raises an API
    diagnostic; the native function has no corresponding cycle check.
    """
    u32(head);u32(kind);u32(owner);u32(previous_output)
    if head > 65535 or not isinstance(entries,dict):
        raise ValueError('Expected WORD head and explicit slot dictionary')
    seen = set()
    while True:
        if head in seen:
            raise ValueError('Cyclic fixture: native function has no cycle guard')
        seen.add(head)
        row = entries.get(head)
        if row is None:
            return 0,previous_output
        if not isinstance(row,dict) or not {'pointer','kind','owner','next'} <= set(row):
            raise ValueError('Expected explicit pointer/kind/owner/next runtime slot')
        for field in ['pointer','kind','owner','next']:
            u32(row[field])
        if row['owner'] > 65535 or row['next'] > 65535:
            raise ValueError('Runtime owner/next must be WORDs')
        if row['pointer'] == 0:
            return 0,previous_output
        if row['kind'] == kind and row['owner'] == (owner & 65535):
            return 1,row['pointer']
        head = row['next']

