"""Candidate scan only: no inference of structure identity or global non-use."""
from pathlib import Path
import json
from find_full_text_memory_candidates import scan
ROOT=Path(__file__).resolve().parent
SPANS=[('Terrain198',0x183,2),('Terrain224',0x1B5,4),('Objects34c',0x106,2),('Traps204',0x98,2)]

def main():
    offsets={i for _,offset,width in SPANS for i in range(offset-3,offset+width)}
    result=scan(ROOT.parent.parent/'Dungeon Keeper 2/DKII-DX.exe',offsets)
    result['target_spans']=[dict(name=n,offset=o,bytes=w) for n,o,w in SPANS]
    for row in result['candidates']:
        offset=int(row['displacement'],16)
        row['overlaps']=[n for n,o,w in SPANS if offset<o+w and offset+row['memory_bytes']>o]
        row['actual_dereference_candidate']=bool(row['access']&1 and row['overlaps'] and row['instruction'].split()[0] not in ['lea','call','jmp'])
    (ROOT/'phase22-overlapping-field-candidates.json').write_text(json.dumps(result,indent=2),encoding='utf8')
    print('Candidates:',len(result['candidates']),'; overlapping read candidates:',sum(r['actual_dereference_candidate'] for r in result['candidates']))

if __name__=='__main__':main()
