"""Derive a new interprocedural Keeper search from the bounded argument engine.

Extends previous45-root mapped-span intraprocedural pass to all linear PE
instructions and up to3 direct-call levels with ECX/stack arguments. Virtual110
seeds remain untyped leads. Does not overwrite any historical search outputs.
"""
from pathlib import Path

ROOT=Path(__file__).resolve().parent


def main():
    source=(ROOT/'scan_current_door_word236_arguments.py').read_text(encoding='utf8')
    first=source.index('from pathlib import Path')
    source=__doc__.__repr__()+'\n'+source[first:]
    old="TARGETS={'Doors':[(0x99,2,'unknownWord236'),(0x97,2,'controlHeight'),(0x91,2,'controlHealth'),(0x95,2,'controlManufacture')]}"
    new="TARGETS={'KeeperSpells':[(0x3A,4,'unknownC8'),(0x3E,4,'controlRecharge'),(0x58,4,'controlManaCost'),(0x42,1,'controlShotId')]}"
    assert old in source
    source=source.replace(old,new)
    source=source.replace("getters={0x50D650:'Doors'};slots={0x138:'Doors'}","getters={0x50DD60:'KeeperSpells'};slots={0x110:'KeeperSpells'}")
    start=source.index("  if x.mnemonic=='mov' and len(op)==2")
    end=source.index("   if op[0].type==X86_OP_IMM and op[0].imm in getters",start)
    source=source[:start]+"  if x.mnemonic=='call':\n"+source[end:]
    source=source.replace('phase67-DX-door-word236-leads.json','phase72-DX-keeper-pointer-argument-leads.json')
    source=source.replace("if not h['field'].startswith('control'):print", "if not h['field'].startswith('control'):print")
    assert 'entity-field-hint' not in source and "'Doors'" not in source
    (ROOT/'scan_keeper_pointer_arguments.py').write_text(source,encoding='utf8')
    old=source.replace("data=(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe').read_bytes()","data=(ROOT/'source-binaries/DKII-local13.exe').read_bytes()")
    old=old.replace("getters={0x50DD60:'KeeperSpells'}","getters={0x5027A0:'KeeperSpells'}")
    old=old.replace('phase72-DX-keeper-pointer-argument-leads.json','phase72-old-keeper-pointer-argument-leads.json')
    assert old!=source
    (ROOT/'scan_old_keeper_pointer_arguments.py').write_text(old,encoding='utf8')
    print('Generated DX/old explicit Keeper-only all-PE/3-callee-level searches;no entity-offset seeds')


if __name__=='__main__':main()
