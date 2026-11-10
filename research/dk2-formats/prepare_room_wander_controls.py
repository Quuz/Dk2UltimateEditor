"""Reuse the whole-memory independent CPU comparison driver for new room cases."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent
def build():
    p=ROOT/'crosscheck_wander_target_paths.py';s=p.read_text(encoding='utf8')
    replacements=[
      ('import emulate_wander_target_paths as native','import emulate_room_wander as native'),
      ("CONTROLS=['helper-only-before-start','helper-unvisited-corner','callback-open','callback-wander255','callback-blocked','callback-foreign-fallback']", "CONTROLS=['two-eligible-last','last-empty-skips-earlier','trailing-excluded-selected','callback-room','callback-room-wander255','callback-room-blocked']"),
      ('phase144-wander-Ghidra-','phase145-room-wander-Ghidra-'),
      ('phase144-wander-target-path-crosschecks.json','phase145-room-wander-crosschecks.json')]
    for a,b in replacements:assert s.count(a)==1;s=s.replace(a,b)
    q=ROOT/'crosscheck_room_wander.py';q.write_text(s,encoding='utf8')
    sha=lambda b:hashlib.sha256(b).hexdigest()
    (ROOT/'phase145-controls-derivation.json').write_text(json.dumps(dict(source=p.name,source_sha256=sha(p.read_bytes()),output=q.name,output_sha256=sha(q.read_bytes()),changes=replacements),indent=2),encoding='utf8')
    print(q.name)
if __name__=='__main__':build()
