"""Locate original CPlayer RTTI/COL/vtable chains, without guessing this types."""
from pathlib import Path
import json, struct
from native_shot_pair_trace import pe_image
from audit_targeted_spell import executable_ranges
import probe_crypt_action_completion as profile

ROOT = Path(__file__).resolve().parent
sha = profile.sha


def main():
    blob = profile.EXE.read_bytes(); base, data = pe_image(blob)
    assert sha(blob) == 'd7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
    ranges = executable_ranges(blob)
    read = lambda address, size: bytes(data[address-base:address-base+size])
    dword = lambda address: struct.unpack('<I', read(address,4))[0]
    refs = lambda address: [base+i for i in range(0,len(data)-3,4) if data[i:i+4] == struct.pack('<I',address)]
    names = []
    needle = b'.?AVCPlayer@@\0'; start = 0
    while (index := data.find(needle,start)) != -1:
        names.append(base+index); start = index+1
    rows = []
    for name in names:
        descriptor = name-8
        cols = []
        for reference in refs(descriptor):
            address = reference-12
            if not (base <= address and address+20 <= base+len(data)):
                continue
            fields = struct.unpack('<5I',read(address,20))
            signature, offset, cd_offset, type_desc, hierarchy = fields
            if signature != 0 or offset > 0x2000 or cd_offset > 0x2000 or not base <= hierarchy < base+len(data):
                continue
            tables = []
            for location in refs(address):
                entries = []
                for slot in range(128):
                    value = dword(location+4+4*slot)
                    if not any(a <= value < a+n for a,n in ranges):
                        break
                    entries.append(dict(slot=4*slot, target=value))
                if entries:
                    tables.append(dict(locator_pointer_address=location, vtable=location+4, entries=entries,
                                       bounded_at_128=len(entries)==128, bytes_hex=read(location,4+4*len(entries)).hex()))
            if tables:
                cols.append(dict(address=address, fields=list(fields), hex=read(address,20).hex(), tables=tables))
        rows.append(dict(name_address=name, descriptor=descriptor, name=needle[:-1].decode(), COLs=cols))
    out = dict(phase=190, status='original_CPlayer_RTTI_candidates', profile=dict(file=str(profile.EXE),sha256=sha(blob)),
               rows=rows, limitations='OnlyliteralMSVC32RTTIandcontiguouscodepointerchains;this-adjustment/livestructureandmethodsemanticsnotyetproved.')
    (ROOT/'phase190-player-rtti.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
    print([(hex(r['descriptor']),[(hex(c['address']),[(hex(t['vtable']),len(t['entries'])) for t in c['tables']]) for c in r['COLs']]) for r in rows])


if __name__ == '__main__':
    main()
