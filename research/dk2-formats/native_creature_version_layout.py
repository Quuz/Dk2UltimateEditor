"""Read only3 natively identified fields for exact executable SHA-256 profiles."""
import struct
CURRENT='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
OLD='93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342'
PROFILES={CURRENT:dict(party=0x417,wander=0x418,trigger=0x419),OLD:dict(party=0x416,wander=0x417,trigger=0x418)}

def read_instance_fields(buffer,executable_sha256):
    if executable_sha256 not in PROFILES:raise ValueError('Unknown executable profile; do not guess offsets from a version label')
    if not isinstance(buffer,(bytes,bytearray)):raise ValueError('Expected byte buffer')
    p=PROFILES[executable_sha256]
    if len(buffer)<p['trigger']+2:raise ValueError('Buffer lacks verified field span')
    t=struct.unpack_from('<H',buffer,p['trigger'])[0]
    return dict(party_id=buffer[p['party']],wander_radius_byte=buffer[p['wander']],trigger_root_bits=t,diagnostic_trigger_signed=t if t<32768 else t-65536)
