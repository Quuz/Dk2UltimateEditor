"""Publish explicitly bounded negative evidence for remaining Object candidates."""
from pathlib import Path
import json,hashlib,struct
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))

def primary():
    out={}
    for key,rows in load('phase100-primary-manifest.json').items():
        out[key]=[]
        for r in rows:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text(encoding='utf8'));b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length']
            assert b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    return out

def review():
    v=load('phase100-object-boundary-review.json');data=Path(v['executable']).read_bytes();assert digest(data)==v['sha256']
    assert len(v['reviews'])==6
    counts={}
    for r in v['reviews']:
        a=int(r['candidate']['address'],16);c=r['canonical_address'];b=bytes.fromhex(r['canonical_bytes'])
        assert pe_read(data,c,len(b))==b and c<a<c+len(b);counts[r['classification']]=counts.get(r['classification'],0)+1
    assert counts=={'prefixed-WORD104-excludes106':4,'starts-in-JMP-displacement':1,'starts-in-immediate-zero':1}
    switch=v['switch_recovery'];assert tuple(switch['targets'])==struct.unpack('<5I',pe_read(data,switch['table'],20))
    assert len(switch['recovered_instructions'])==317 and not switch['unresolved_stops']
    for r in switch['recovered_instructions']:assert pe_read(data,r['address'],len(bytes.fromhex(r['hex'])))==bytes.fromhex(r['hex'])
    p=v['neighbouring_actual_read'];assert p['restore_dominates_read'] and p['cfg_states']==647 and not p['unresolved_stops']
    # Preserve the limitation: the generic walker did NOT solve its stack reload.
    assert all(not r['aliases'] for r in p['original_walker_could_not_resolve_stack'])
    return v,counts

def sources():
    rows=load('object-indexed-consumer-review.json')['corpus']['files']
    for r in rows:assert digest(Path(r['file']).read_bytes())==r['sha256']
    return dict(files=rows,file_count=len(rows),scope='Historical84 related source files hash checked;no parse/patch/nativecases recounted as new.')

def main():
    p=primary();v,counts=review();raw=sources();prior=ROOT/'phase100-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==78;prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    old=load('creature-version-layout-semantics.json')['old_source']
    s=dict(phase=100,date='2026-10-28',profile=dict(executable=v['executable'],sha256=v['sha256'],original_file=old['original_path'],
        version_scope='Exact supplied olderprofile;folder13 is not certifiedretailversion.'),primary=p,review=v,corpus=raw,
      raw_field=dict(family='Objects',offset=0x34c,width=2,intermediate=0x3cc,runtime=0x106,meaning='unresolved'),
      classification_counts=counts,evidence=[link(n) for n in ['phase100-object-boundary-review.json','phase100-old-object-boundary-contexts.json',
        'ghidra/phase100-old107-backward.txt','phase70-old-object-field-candidates-v2.json','review_old_object_boundaries.py']],
      conclusion='All six remaining nonindexed novelanchored candidates fromphase70 are rejected as originalinstruction starts. This closes that finite list,not all potential consumers of rawObjects34C.',
      actual_neighbour_read='The neighbouring realDWORD107 read uses restoredthis fromlocalstack. Evidence is explicitinstruction/localCFG+decompilerinput;fixedstackframe/ABI andunaliasing assumptions stated. It is not promoted to catalogueObjectdefinition usage.',
      switch='Five targets at497E58 recover317 instructions omittedbyGhidrabody. BoundsCMP EAX,4/JA andfullPEbytes checked;this adds missing branchcoverage to staticinspection,not nativeexecution.',
      counts=dict(reviewed_anchored_candidates=6,prefixed_word104_candidates=4,recovered_switch_arms=5,recovered_instructions=317,
        local_cfg_states=647,whole_native_code_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),native_cases=0,new_raw_API_patches=0),
      limitations=['Staticanalysis only;no new emulation,rawpatch,gameplay,GUI orrenderer proof.',
        'Sixfinitephase70anchored candidates only;otherexistingwide,byte,adjusted,indexed,indirect andotherprofile candidates not exhausted.',
        'Genericregisterwalker could notresolve this afterstackreload;explicitlocalspill/restore proof carriesfixedframe/ABI andunaliasingassumptions. PCodeHighVariable selection byitself doesnotprove everyregisteruse.',
        'No newObjects34C,ManaGen,Traps204,Doors236,CreaturePriority/Jump,Terrain198/224 or scalar113/114 meaning assigned.',
        'Prior78schemas preserved byteexact;unknownrawbytes remain authoritative. Full15-familygoal incomplete.'])
    (ROOT/'unknown-instruction-boundary-review.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']));print('Source files',raw['file_count'])

if __name__=='__main__':main()
