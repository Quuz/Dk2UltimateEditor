"""Derive a read-only observer, preserving the phase144 executor verbatim otherwise."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent
def build():
    p=ROOT/'native_unicorn_wandertrace.py';s=p.read_text(encoding='utf8')
    replacements=[
      ('    wander_rng_calls = []','    wander_rng_calls = []\n    room_target_calls = []'),
      ('        if address == 0x480A00:', '        if address == 0x4E8430:\n            room_target_calls.append(hex(uc.reg_read(xc.UC_X86_REG_ECX)))\n        if address == 0x480A00:'),
      ('if address in [0x480E92,0x480E9E]:','if address in [0x480B6E,0x480E92,0x480E9E]:'),
      ('wander_rng_calls=wander_rng_calls,','wander_rng_calls=wander_rng_calls, room_target_calls=room_target_calls,')]
    for a,b in replacements:
        assert s.count(a)==1,(a,s.count(a));s=s.replace(a,b)
    q=ROOT/'native_unicorn_roomwandertrace.py';q.write_text(s,encoding='utf8')
    sha=lambda b:hashlib.sha256(b).hexdigest()
    (ROOT/'phase145-observer-derivation.json').write_text(json.dumps(dict(source=p.name,source_sha256=sha(p.read_bytes()),output=q.name,output_sha256=sha(q.read_bytes()),changes=replacements),indent=2),encoding='utf8')
    print(q.name)
if __name__=='__main__':build()
