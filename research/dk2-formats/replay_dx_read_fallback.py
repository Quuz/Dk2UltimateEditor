"""Replay named historical DX searches without overwriting their snapshots.

Only the data-memory-read classifier and output/input snapshot names change.
Underlying alias/CFG bounds and source maps remain historical and incomplete.
"""
from pathlib import Path
from contextlib import redirect_stdout
import io,json,hashlib
ROOT=Path(__file__).resolve().parent
def digest(b):return hashlib.sha256(b).hexdigest()
def replay(script,changes,expected_count):
    p=ROOT/script;raw=p.read_bytes();source=raw.decode('utf8');old="o.type==X86_OP_MEM and o.access&CS_AC_READ and x.reg_name(o.mem.base) in aliases and not o.mem.index and x.mnemonic not in ['lea','call','jmp']"
    assert source.count(old)==expected_count
    source=source.replace(old,"reads_memory(x,o) and x.reg_name(o.mem.base) in aliases and not o.mem.index")
    source=source.replace('from build_effect_semantics import pe_read','from build_effect_semantics import pe_read\nfrom native_memory_access import reads_memory')
    for before,after in changes.items():assert before in source;source=source.replace(before,after)
    with redirect_stdout(io.StringIO()):exec(compile(source,str(p),'exec'),{'__name__':'__main__','__file__':str(p)})
    return dict(script=script,source_sha256=digest(raw),classifier='native_memory_access.reads_memory',classifier_substitutions=expected_count,snapshot_replacements=changes,transformed_source_sha256=digest(source.encode()))

def main():
    rows=[]
    rows.append(replay('scan_interprocedural_creature_fields.py',{'phase57-this-read-summaries.json':'phase65-this-read-summaries-v2.json','phase57-interprocedural-creature-leads.json':'phase65-interprocedural-creature-leads-v2.json'},1))
    s=json.loads((ROOT/'phase65-interprocedural-creature-leads-v2.json').read_text());print('DXthissummaries',s['mapped_functions'],s['states'],'limits',s['limited_functions'],'composedleads',len(s['candidates']),flush=True)
    rows.append(replay('scan_typed_definition_consumers.py',{'phase57-this-read-summaries.json':'phase65-this-read-summaries-v2.json','phase59-typed-definition-leads.json':'phase65-typed-definition-leads-v2.json'},1))
    s=json.loads((ROOT/'phase65-typed-definition-leads-v2.json').read_text());print('DXtypedroots',len(s['producer_roots']),s['states'],'limits',s['limited_roots'],'leads',len(s['candidates']),flush=True)
    for h in s['candidates']:
        if h['field'].startswith('unknown'):print(h['family'],h['field'],h['address'],h['text'],flush=True)
    (ROOT/'phase65-read-replay-manifest.json').write_text(json.dumps(dict(scope=__doc__,replays=rows,outputs=[dict(file=n,sha256=digest((ROOT/n).read_bytes())) for n in ['phase65-this-read-summaries-v2.json','phase65-interprocedural-creature-leads-v2.json','phase65-typed-definition-leads-v2.json']]),indent=2),encoding='utf8')
if __name__=='__main__':main()
