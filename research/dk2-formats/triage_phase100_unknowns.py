"""Compact existing search gaps; does not rerun or promote old scans."""
from pathlib import Path
import json,hashlib
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))

def main():
    s=load('phase57-this-read-summaries.json');limited={a:dict(name=r.get('name'),states=r.get('states'),stops=r.get('stops')) for a,r in s.items() if r.get('unexplored') and 0x400000<=int(a,16)<0x530000}
    print('Old bounded summaries',limited)
    c=load('phase73-DX-creature-argument-leads.json');print('Creaturecontrol callees',sorted(set(r['read']['callee'] for r in c['candidates'] if r['read'].get('callee'))))
    names=['phase70-old-object-field-candidates-v2.json','phase70-indexed-origin-review.json','phase67-DX-door-word236-leads.json','phase75-wander-exposure.json']
    out=dict(previous_goal_turn_classification='progress:phase99navproductionpublished',limited_summaries=limited,inputs=[])
    for n in names:
        d=load(n);print(n,'keys',list(d)[:18])
        for k in ['new_addresses','new_candidates','other_candidates','unclassified','unresolved','candidates','reviews','functions']:
            if k in d:
                v=d[k];print(k,'length',len(v));print(json.dumps(v[:5] if isinstance(v,list) else v)[:1700])
        out['inputs'].append(dict(file=n,sha256=hashlib.sha256((ROOT/n).read_bytes()).hexdigest(),keys=list(d)))
    (ROOT/'phase100-existing-gap-triage.json').write_text(json.dumps(out,indent=2),encoding='utf8')

if __name__=='__main__':main()
