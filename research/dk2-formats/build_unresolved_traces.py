"""Native storage facts for unresolved fields; no invented gameplay labels."""
from pathlib import Path
from collections import Counter
import json
import dk2_formats as fmt
from inventory import DATA
from build_storage_paths import converter

ROOT=Path(__file__).resolve().parent

def main():
    paths=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']
    fields=[]
    for family,offset,width,intermediate,runtime in [
        ('Terrain',0x198,2,0x250,0x183),('Terrain',0x224,4,0x2DC,0x1B5),
        ('Traps',0x204,2,0x2AC,0x98),('Objects',0x34C,2,0x3CC,0x106)]:
        p=next((p for p in paths[family]['joined_paths'] if p['raw_offset']==hex(offset)),None)
        if family=='Objects' and p is None:
            # The conservative bulk extractor excludes aliases. Trace this one
            # explicitly and retain the assignment proving the alias identity.
            conv=(ROOT/'ghidra/game-objects-convert.txt').read_text()
            source=next(p for p in converter(conv) if p['raw_offset']==offset)
            text=(ROOT/'ghidra/game-objects-runtime.txt').read_text().replace('\r','')
            lines=text.splitlines()
            assert text.count('local_10c =')==1 and 'local_10c = param_1;' in text
            assignment='*(undefined2 *)(local_10c + 0x106) = *(undefined2 *)(param_2 + 0x3cc);'
            alias_line=next(i for i,line in enumerate(lines,1) if line.strip()=='local_10c = param_1;')
            write_line=next(i for i,line in enumerate(lines,1) if line.strip()==assignment)
            assert source['intermediate_offset']==intermediate
            p=dict(raw_offset=hex(offset),raw_bytes=source['raw_bytes'],runtime_bytes=2,
                intermediate_offset=hex(intermediate),runtime_offset=hex(runtime),
                converter_evidence=source['evidence'],runtime_file='game-objects-runtime.txt',
                runtime_alias_evidence=dict(line=alias_line,assignment='local_10c = param_1;'),
                runtime_evidence=dict(line=write_line,assignment=assignment),
                note='Manual alias trace; outside the literal-only scalar-storage-paths extractor.')
        assert p is not None,(family,hex(offset))
        assert p['raw_bytes']==width and p['runtime_bytes']==width
        assert p['intermediate_offset']==hex(intermediate) and p['runtime_offset']==hex(runtime)
        section=next(s for s in fmt.parse((DATA/(family+'.kwd')).read_bytes()) if fmt.FAMILIES.get(s.signature)==family)
        values=Counter(int.from_bytes(r[offset:offset+width],'little') for r in section.records)
        row=dict(family=family,raw_offset=hex(offset),raw_bytes=width,intermediate_offset=hex(intermediate),
            runtime_offset=hex(runtime),runtime_bytes=width,meaning='unresolved',
            signedness='Copy width is verified; no signedness inferred without a semantic consumer.',
            storage_evidence=p,standalone_unsigned_value_counts={str(k):v for k,v in sorted(values.items())})
        if family=='Traps':row.update(
            baseline_aliases=['unknown4[0].unknown4','unknown4[1].unknown4'],
            native_view='One word, not two independent native properties.',
            observed_bit_pattern='0xfff6 can be displayed as unsigned 65526 / signed -10; neither display proves meaning.',
            hypothesis='ManaGen in a third-party template remains unconfirmed by native consumption.')
        fields.append(row)
    editor_path=ROOT/'editor-field-bindings.json'
    if editor_path.exists():
        editor=json.loads(editor_path.read_text(encoding='utf8'))
        for row in fields:
            match=next((f for f in editor['unknown_field_paths'] if f['family']==row['family'] and f['raw_offset']==row['raw_offset']),None)
            if match:row['native_editor_trace']=match
            if row['family']=='Traps':
                row['hypothesis']='ManaGen is unconfirmed. The native editor contains a hidden orphan Mana Gen. static caption; no binding to raw+0x204 has been established.'
    result=dict(program='DKII-DX.exe',fields=fields,
        search=dict(tool='find_native_memory_candidates.py',mapped_spans_scanned=8729,
            evidence_files=['phase7-terrain-trap-candidates.json','phase7-object-and-trap-candidates.json'],
            conclusion='No semantic consumer established for these four fields in this pass.',
            limitation='Exact memory displacements in mapped .text spans only. Pointer adjustment, aliases, bulk copies, unmapped code or other binaries can evade this search. Absence is not proof of non-use.',
            rejected_candidate=dict(function='0x47b970',instruction='0x47bafc',
                reason='0x183 is a displacement in the creature level-stat table, not the Terrain definition.')),
        preserve='Keep complete original bytes; do not zero fields or rename a template hypothesis as verified semantics.')
    trap_path=ROOT/'trap-semantics.json'
    if trap_path.exists():
        trap=json.loads(trap_path.read_text(encoding='utf8'))
        result['search']['additional_rejected_candidate']=trap['unresolved']['rejected_candidate']
    object_path=ROOT/'object-property-semantics.json'
    if object_path.exists():
        obj=json.loads(object_path.read_text(encoding='utf8'))
        row=next(f for f in fields if f['family']=='Objects' and f['raw_offset']=='0x34c')
        row['meaning']='Native editor label Mana Gen established; gameplay effect remains unresolved.'
        row['signedness']='Signed16 native editor display established; runtime consumer signedness remains unresolved.'
        row['native_editor_trace']['meaning']=row['meaning']
        row['native_editor_ui_binding']=obj['mana_gen']
        result['search']['additional_full_text_pass']=dict(tool='find_full_text_memory_candidates.py',
            evidence_files=obj['full_text_searches'],
            conclusion='Objects+0x34C now has a verified native Mana Gen UI binding. No new gameplay meaning is assigned to Terrain+0x198/+0x224 or Objects+0x34C.',
            limitation=obj['search_limitation'])
    previous=ROOT/'unresolved-field-traces.json'
    if previous.exists():
        saved=json.loads(previous.read_text(encoding='utf8'))
        if 'phase64_search_review' in saved:result['phase64_search_review']=saved['phase64_search_review']
    (ROOT/'unresolved-field-traces.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Unresolved native storage traces:',len(fields),'fields; semantics remain unresolved')

if __name__=='__main__':main()
