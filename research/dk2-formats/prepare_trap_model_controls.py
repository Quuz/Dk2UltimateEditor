"""Derive the established independent editor CPU comparison for full Trap chains."""
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parent
def main():
    p=ROOT/'crosscheck_terrain_model_chain.py';s=p.read_text(encoding='utf8')
    replacements=[('import emulate_terrain_model_chain as native','import emulate_trap_model_chain as native'),
      ("CONTROLS = ['catalogue41', 'boundary-ffffffff']","CONTROLS = ['catalogue16', 'word-ffff']"),
      ('0x49F5E0','0x4A1770'),('phase128-Terrain-model-Ghidra-','phase146-Traps-model-Ghidra-'),
      ('phase128-terrain-model-crosschecks.json','phase146-Traps-model-crosschecks.json')]
    for a,b in replacements:assert s.count(a)==1;(s:=s.replace(a,b))
    q=ROOT/'crosscheck_trap_model_chain.py';q.write_text(s,encoding='utf8')
    sha=lambda b:hashlib.sha256(b).hexdigest()
    (ROOT/'phase146-controls-derivation.json').write_text(json.dumps(dict(source=p.name,source_sha256=sha(p.read_bytes()),output=q.name,output_sha256=sha(q.read_bytes()),changes=replacements),indent=2),encoding='utf8')
    print(q.name)
if __name__=='__main__':main()
