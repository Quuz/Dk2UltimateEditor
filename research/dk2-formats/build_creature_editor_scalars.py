"""Join primitive creature properties through saved native editor functions.

Intentionally excludes scaled doubles, indexed tables, inferred pointer aliases,
and labels that do not have a complete direct fill/commit/writer chain.
"""
from pathlib import Path
import hashlib,json,re
from build_storage_paths import converter,WIDTH,NUMBER
from build_effect_semantics import asm_range

ROOT=Path(__file__).resolve().parent
FILES=dict(converter='creature-record-decoder.txt',model='phase17-creature-editor-model.txt',
    fill='phase17-creature-main-panel-fill.txt',extract='phase17-creature-f55-panel-extract.txt',
    update='phase17-creature-f55-panel-update.txt',commit='phase17-creature-main-panel-commit.txt',
    writer='phase17-creature-writer-assembly.txt')

def text(key):return (ROOT/'ghidra'/FILES[key]).read_text(encoding='utf8')
def evidence(key,line):
    return dict(file=FILES[key],line=line,excerpt=text(key).splitlines()[line-1].strip())

def memory(expression,variable,base):
    # Only a single scalar cast, literal index or literal displacement is allowed.
    expression=re.sub(r'^\((?:short|char|int|uint|ushort|byte)\)','',expression)
    m=re.fullmatch(variable+r'\[('+NUMBER+r')\]',expression)
    if m:return int(m[1],0)*base,base
    m=re.fullmatch(r'\*\((\w+) \*\)\((\(int\))?'+variable+r' \+ ('+NUMBER+r')\)',expression)
    if m and m[1] in WIDTH:return int(m[3],0)*(1 if m[2] else base),WIDTH[m[1]]
    return None

def pairs(key):
    for i,line in enumerate(text(key).splitlines(),1):
        m=re.fullmatch(r'\s*(.*?) = (.*);',line)
        if m:yield i,m[1],m[2]

def main():
    # Rename this/raw parameters without losing line numbers or changing arithmetic.
    paths=converter(text('converter').replace('param_1','param_2').replace('this','param_1'))
    models={}
    for line,lhs,rhs in pairs('model'):
        dst=memory(lhs,'this',4);src=memory(rhs,'piVar1',4)
        if dst and src and dst[1]<=src[1]:
            assert dst[0] not in models
            models[dst[0]]=dict(intermediate=src[0],width=dst[1],evidence=evidence('model',line))
    fill_re=r'\*\((\w+) \*\)\(\(int\)this \+ ('+NUMBER+r')\) = \*\((\w+) \*\)\(\*\(int \*\)\(\(int\)this \+ 0x134\) \+ ('+NUMBER+r')\);'
    fills={}
    for i,line in enumerate(text('fill').splitlines(),1):
        m=re.fullmatch(r'\s*'+fill_re,line)
        if m and m[1] in WIDTH and m[3] in WIDTH:
            assert WIDTH[m[1]]==WIDTH[m[3]]
            fills[int(m[2],0)]=dict(model=int(m[4],0),width=WIDTH[m[1]],evidence=evidence('fill',i))
    commits={}
    commit_re=r'\*\((\w+) \*\)\(\*\(int \*\)\(param_1 \+ 0x134\) \+ ('+NUMBER+r')\) = \*\((\w+) \*\)\(param_1 \+ ('+NUMBER+r')\);'
    for i,line in enumerate(text('commit').splitlines(),1):
        m=re.fullmatch(r'\s*'+commit_re,line)
        if m and m[1] in WIDTH and m[3] in WIDTH:
            assert WIDTH[m[1]]==WIDTH[m[3]]
            commits[int(m[4],0)]=dict(model=int(m[2],0),width=WIDTH[m[1]],evidence=evidence('commit',i))
    labels={s['id']:s for s in json.loads((ROOT/'editor-strings.json').read_text(encoding='utf8'))['strings']}
    layouts=json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))
    runtime=json.loads((ROOT/'scalar-storage-paths.json').read_text(encoding='utf8'))['families']['Creatures']['joined_paths']
    overrides=json.loads((ROOT/'verified-overrides.json').read_text(encoding='utf8'))
    # Export existing verified game interpretations by exact raw offset, not by similar labels.
    game_fields=overrides['creature_fields']
    fields=[];extract_text=text('extract');update_text=text('update');writer_lines=text('writer').splitlines()
    extract_re=r'(?:case (0x[0-9a-f]+):|if \(uVar1 == (0x[0-9a-f]+)\) \{)\s*\*\(double \*\).*?= \(double\)(?:\(int\))?\*\((byte|ushort|short|uint) \*\)\(\(int\)param_1 \+ ('+NUMBER+r')\);'
    for m in re.finditer(extract_re,extract_text):
        prop=int(m[1] or m[2],16);panel=int(m[4],0);kind=m[3]
        if panel not in fills:continue
        fill=fills[panel];model=fill['model'];width=fill['width']
        if model not in models:continue
        model_path=models[model];mid=model_path['intermediate']
        matches=[p for p in paths if p['intermediate_offset']==mid and p['raw_bytes']==width]
        if len(matches)!=1:continue
        path=matches[0];offset=path['raw_offset']
        assert width==model_path['width']==WIDTH[kind]
        assert commits[panel]['model']==model and commits[panel]['width']==width
        block=re.search(r'(?:case '+hex(prop)+r':|if \(uVar1 == '+hex(prop)+r'\) \{)(?P<body>.*?)(?=\n\s*(?:case |default:|\}))',update_text,re.S)
        assert block and 'lVar2 = __ftol();' in block['body']
        store=re.search(r'\*\((char|short|int|uint) \*\)\(param_1 \+ ('+NUMBER+r')\) = \(\1\)lVar2;',block['body'])
        assert store and int(store[2],0)==panel and WIDTH[store[1]]==width
        writer_proof=[]
        for wi,line in enumerate(writer_lines):
            load=re.fullmatch(r'([0-9a-f]{8}): MOV (\w+),(byte|word|dword) ptr \[EBX \+ (0x[0-9a-f]+)\] *',line)
            if not load or int(load[4],16)!=model:continue
            assert {'byte':1,'word':2,'dword':4}[load[3]]==width
            for next_line in writer_lines[wi+1:wi+4]:
                if re.search(r': (?:PUSH|POP|CALL|RET|J\w+) ',next_line):break
                dest=re.fullmatch(r'([0-9a-f]{8}): MOV (byte|word|dword) ptr \[ESP \+ (0x[0-9a-f]+)\],'+load[2]+r' *',next_line)
                if dest:
                    assert int(dest[3],16)-0x94==offset and {'byte':1,'word':2,'dword':4}[dest[2]]==width
                    writer_proof.append(asm_range(FILES['writer'],int(load[1],16),int(dest[1],16)));break
        assert len(writer_proof)==1,(hex(offset),'ambiguous writer')
        label=labels[prop];baseline=next(f for f in layouts['Creatures']['fields'] if f['offset']==offset)
        long_field=next(f for f in layouts['Creatures_5537']['fields'] if f['offset']==offset)
        assert baseline['size']==long_field['size']==width
        source_proofs=[]
        for row in path['evidence']:
            source_proofs.append(evidence('converter',row['line']))
        extract_line=extract_text[:m.start()].count('\n')+1
        extract_assignment_line=extract_line+m[0].count('\n')
        update_line=update_text[:block.start()].count('\n')+1
        store_line=update_text[:block.start('body')+store.start()].count('\n')+1
        ftol_line=update_text[:block.start('body')+block['body'].index('lVar2 = __ftol();')].count('\n')+1
        joined=[r for r in runtime if int(r['raw_offset'],16)==offset]
        existing=next((r for r in game_fields if r.get('offset')==offset),None)
        fields.append(dict(raw_offset=offset,raw_offset_hex=hex(offset),raw_bytes=width,baseline_name=baseline['name'],
            native_label=label['text'],property_id=prop,editor_intermediate_offset=mid,editor_model_offset=model,
            editor_panel_offset=panel,editor_display_kind=dict(byte='u8',ushort='u16',short='s16',uint='u32')[kind],
            gui_validation_range=None,scale=1,label_resource=label,
            runtime_storage_paths=joined,existing_engine_override=existing,
            evidence=source_proofs+[model_path['evidence'],fill['evidence'],evidence('extract',extract_line),
                evidence('extract',extract_assignment_line),evidence('update',update_line),evidence('update',ftol_line),
                evidence('update',store_line),commits[panel]['evidence']]+writer_proof))
    fields.sort(key=lambda f:f['raw_offset']);assert len(fields)==35
    assert next(f for f in fields if f['raw_offset']==0xF53)['native_label']=='Fly Height Percentage'
    assert not next(f for f in fields if f['raw_offset']==0xF53)['runtime_storage_paths']
    assert next(f for f in fields if f['raw_offset']==0xF20)['editor_display_kind']=='u16'
    result=dict(family='Creatures',record_variants=[5449,5537],native_writer_record_bytes=5537,
        executable_sha256=json.loads((ROOT/'executable-hash-verification.json').read_text()),
        functions=dict(converter=0x4287C0,model=0x4A01B0,fill=0x43F2A0,extract=0x4405E0,
            update=0x43FD60,commit=0x441160,writer=0x4A7F00),
        scope=__doc__,fields=fields,writer_buffer_evidence=asm_range(FILES['writer'],0x4A8B7D,0x4A8B84),
        limitations=['Native labels and numeric display confirmed; labels do not independently prove gameplay units or consumers.',
            'No0..100 GUI limit inferred from Percentage labels; full native GUI validation was not reconstructed.',
            'Update narrowing models the integer result after __ftol, not arbitrary floating-point input.',
            'Raw values and unknown bytes remain authoritative. No automatic fixed-point conversion for Max Angle Change.',
            'Runtime paths are pre-existing literal-copy traces. An empty path list is not proof of non-use by all game code.',
            'No new Ghidra calls, native emulation or modified-map gameplay validation were performed in this pass.'])
    (ROOT/'creature-editor-scalars.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Native creature scalar properties:',len(fields))
    for f in fields:
        if f['baseline_name'].lower().startswith(('unk','x')):print(f['raw_offset_hex'],f['native_label'],f['editor_display_kind'])

if __name__=='__main__':main()
