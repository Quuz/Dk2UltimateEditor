"""Exercise exported JSON and tagged-record edits against the map corpus."""
from pathlib import Path
from collections import Counter
import hashlib,json
import dk2_formats as fmt
from inventory import DATA

ROOT=Path(__file__).resolve().parent

def main():
    passed=[];rejected=[];edits=Counter();styles=Counter();diagnostics=[]
    expected={r['file']:r['sha256'] for r in json.loads((ROOT/'corpus-scan.json').read_text(encoding='utf8'))['files']}
    for path in sorted((DATA/'maps').iterdir()):
        if path.suffix.lower() not in ('.kwd','.kld'):continue
        data=path.read_bytes()
        assert hashlib.sha256(data).hexdigest()==expected[path.name],('Input changed since survey',path.name)
        try:sections=fmt.parse(data)
        except ValueError as e:
            rejected.append(dict(file=path.name,error=str(e)));continue
        document=dict(sections=[fmt.decoded(s) for s in sections])
        findings=fmt.compatibility_findings(document)
        if findings:diagnostics.append(dict(file=path.name,findings=findings))
        exported=json.loads(json.dumps(document,ensure_ascii=False))
        assert fmt.rebuild_decoded(exported)==data,path.name
        for s in sections:
            if s.signature==0xD2:
                style=fmt.decoded(s)['declared_size_convention'];styles[style]+=1
                assert Counter(fmt.u32(r) for r in s.records)==Counter({0xD5:fmt.u32(s.header,20),0xD6:fmt.u32(s.header,24)}),path.name
                for tag in (0xD5,0xD6):
                    r=next((r for r in s.records if fmt.u32(r)==tag),None)
                    if r is None:continue
                    clone=fmt.Section(0,s.signature,s.header,list(s.records),s.suffix)
                    clone.replace_records(clone.records+[r])
                    reread=fmt.parse(clone.pack())[0]
                    assert reread.records==s.records+[r] and reread.suffix==s.suffix
                    assert fmt.u32(reread.header,20+(0 if tag==0xD5 else 4))==fmt.u32(s.header,20+(0 if tag==0xD5 else 4))+1
                    if style=='condition_payloads_only':assert fmt.u32(reread.header,56)==fmt.u32(s.header,56)+(16 if tag==0xD5 else 0)
                    clone.replace_records(s.records)
                    assert clone.pack()==s.pack()
                    edits[style+':'+hex(tag)]+=1
        passed.append(dict(file=path.name,sha256=hashlib.sha256(data).hexdigest()))
    # This shipped Things file has stale D but consistent N and complete size.
    path=DATA/'maps/BattlesThings.kld';s=fmt.parse(path.read_bytes())[0]
    original=s.pack();s.replace_records(s.records+[s.records[0]])
    assert len(fmt.parse(s.pack())[0].records)==21
    s.replace_records(s.records[:-1]);assert s.pack()==original
    result=dict(exact_json_roundtrip_files=len(passed),rejected_files=rejected,
        trigger_size_conventions=dict(styles),trigger_append_remove_tests=dict(edits),
        stale_things_length_append_remove=True,files=passed,
        compatibility_diagnostics=diagnostics,inputs_unchanged_since_survey=True,
        scope='Structural and byte-preservation verification; no execution/gameplay acceptance claim')
    (ROOT/'corpus-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print({k:v for k,v in result.items() if k!='files'})

if __name__=='__main__':main()
