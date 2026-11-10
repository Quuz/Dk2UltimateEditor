"""Scaled creature properties from saved native editor evidence and PE constants."""
from pathlib import Path
from fractions import Fraction
import hashlib,json,re,struct
from build_storage_paths import converter,NUMBER
from build_creature_editor_scalars import memory,evidence,text,FILES
from build_effect_semantics import asm_range,pe_read

ROOT=Path(__file__).resolve().parent

def main():
    exe=(ROOT.parent.parent/'Dungeon Keeper 2/Data/editor/DK2 Editor pro.exe').read_bytes()
    source=text('converter');lines=source.splitlines()
    alias_lines=[i+1 for i,line in enumerate(lines) if re.search(r'\bpcVar9 =',line)]
    assert len(alias_lines)==3 and lines[alias_lines[0]-1].strip()=='pcVar9 = param_1;'
    cutoff=alias_lines[1]-1
    # This alias is valid only before its first mutation; do not propagate it through loops.
    prefix='\n'.join(lines[:cutoff]).replace('param_1','param_2').replace('this','param_1').replace('pcVar9','param_2')
    paths=converter(prefix);models={}
    model_text=text('model')
    pattern=r'local_14 = (.*);\s*uStack_10 = 0;\s*\*\(double \*\)\(this \+ ('+NUMBER+r')\) = \(double\)local_14 \* _DAT_([0-9a-f]+);'
    for m in re.finditer(pattern,model_text):
        source_memory=m[1].replace('(int *)','').replace('*(int **)','*(undefined4 *)')
        source_field=memory(source_memory,'piVar1',4)
        if not source_field:continue
        assert source_field[1]==4
        line=model_text[:m.start()].count('\n')+1;end=line+m[0].count('\n')
        models[int(m[2],0)*4]=dict(intermediate=source_field[0],constant=int(m[3],16),
            evidence=[evidence('model',line),evidence('model',line+1),evidence('model',end)])
    fills={};alias=None;alias_proof=None
    fill_re=r'\*\(undefined8 \*\)\(\(int\)this \+ ('+NUMBER+r')\) = \*\(undefined8 \*\)\((.*) \+ ('+NUMBER+r')\);'
    for i,line in enumerate(text('fill').splitlines(),1):
        stripped=line.strip()
        if stripped.startswith('iVar3 ='):
            alias=stripped=='iVar3 = *(int *)((int)this + 0x134);';alias_proof=evidence('fill',i) if alias else None
        m=re.fullmatch(r'\s*'+fill_re,line)
        if not m:continue
        proofs=[evidence('fill',i)]
        if m[2]=='*(int *)((int)this + 0x134)':pass
        elif m[2]=='iVar3' and alias:proofs.insert(0,alias_proof)
        else:continue
        fills[int(m[1],0)]=dict(model=int(m[3],0),evidence=proofs)
    commits={}
    pat=r'\*\(undefined8 \*\)\(\*\(int \*\)\(param_1 \+ 0x134\) \+ ('+NUMBER+r')\) = \*\(undefined8 \*\)\(param_1 \+ ('+NUMBER+r')\);'
    for i,line in enumerate(text('commit').splitlines(),1):
        m=re.fullmatch(r'\s*'+pat,line)
        if m:commits[int(m[2],0)]=dict(model=int(m[1],0),evidence=evidence('commit',i))
    strings={s['id']:s for s in json.loads((ROOT/'editor-strings.json').read_text(encoding='utf8'))['strings']}
    layouts=json.loads((ROOT/'layouts.json').read_text(encoding='utf8'))
    fields=[];constants={};read=text('extract');update=text('update');writer=text('writer').splitlines()
    def constant(address):
        b=pe_read(exe,address,8);value=struct.unpack('<d',b)[0];f=Fraction(value)
        constants[hex(address)]=dict(address=address,bytes_hex=b.hex(),sha256=hashlib.sha256(b).hexdigest(),
            binary64_value=value,numerator=f.numerator,denominator=f.denominator)
        return f
    read_pat=r'(?:case (0x[0-9a-f]+):|if \(uVar1 == (0x[0-9a-f]+)\) \{)\s*\*\(undefined4 \*\)\(\(int\)param_1 \+ 0x60\) = \*\(undefined4 \*\)\(\(int\)param_1 \+ ('+NUMBER+r')\);\s*\*\(undefined4 \*\)\(\(int\)param_1 \+ 100\) = \*\(undefined4 \*\)\(\(int\)param_1 \+ ('+NUMBER+r')\);'
    for m in re.finditer(read_pat,read):
        prop=int(m[1] or m[2],16);panel=int(m[3],0)
        if panel not in fills:continue
        model=fills[panel]['model']
        if model not in models:continue
        model_path=models[model];mid=model_path['intermediate'];matched=[p for p in paths if p['intermediate_offset']==mid and p['raw_bytes']==4]
        if len(matched)!=1:continue
        path=matched[0];offset=path['raw_offset'];assert int(m[4],0)==panel+4 and commits[panel]['model']==model
        scale=constant(model_path['constant']);assert scale in [Fraction(1,4096),Fraction(1,65536),Fraction(100,4096)]
        # Exclude unrelated callback normalizations from the direct UI property set.
        assert not re.search(r'if \([^\n]*< \*\(double \*\)\(this \+ '+hex(model//4)+r'\)\)',model_text)
        block=re.search(r'(?:case '+hex(prop)+r':|if \(uVar1 == '+hex(prop)+r'\) \{)(?P<body>.*?)(?=\n\s*(?:case |default:|\}))',update,re.S)
        assert block
        update_kind='copy_double'
        if prop==0xF856:
            assert 'lVar2 = __ftol();' in block['body']
            expected=f'*(double *)(param_1 + {hex(panel)}) = (double)((uint)lVar2 & 0xff);'
            assert expected in block['body'];update_kind='truncate_to_integer_then_low8_then_double'
        else:
            copies=[(int(q[1],0),int(q[2],0)) for q in re.finditer(r'\*\(undefined4 \*\)\(param_1 \+ ('+NUMBER+r')\) = \*\(undefined4 \*\)\(param_1 \+ ('+NUMBER+r')\);',block['body'])]
            assert copies==[(panel,0x60),(panel+4,100)],(hex(prop),copies)
        update_first=update[:block.start()].count('\n')+1
        update_lines=[update_first]+[update[:block.start('body')+q.start()].count('\n')+1 for q in re.finditer(r'[^\n]*\S[^\n]*',block['body']) if '=' in q[0]]
        writer_proofs=[];write_constant=None
        for wi,line in enumerate(writer):
            load=re.fullmatch(r'([0-9a-f]{8}): FLD double ptr \[EBX \+ (0x[0-9a-f]+)\] *',line)
            if not load or int(load[2],16)!=model:continue
            multiply=re.fullmatch(r'[0-9a-f]{8}: FMUL double ptr \[(0x[0-9a-f]+)\] *',writer[wi+1]);assert multiply
            assert 'CALL 0x004acda8' in writer[wi+2]
            store=None
            for next_line in writer[wi+3:wi+6]:
                store=re.fullmatch(r'([0-9a-f]{8}): MOV dword ptr \[ESP \+ (0x[0-9a-f]+)\],EAX *',next_line)
                if store:break
                assert re.fullmatch(r'[0-9a-f]{8}: (?:XOR ECX,ECX|LEA ECX,\[ESP \+ 0x[0-9a-f]+\]) *',next_line),(hex(offset),next_line)
            assert store
            assert int(store[2],16)-0x94==offset
            write_constant=int(multiply[1],16);constant(write_constant)
            writer_proofs.append(asm_range(FILES['writer'],int(load[1],16),int(store[1],16)))
        assert len(writer_proofs)==1,(hex(offset),model)
        native_label=strings[prop];baseline=next(f for f in layouts['Creatures']['fields'] if f['offset']==offset)
        assert baseline['size']==4 and next(f for f in layouts['Creatures_5537']['fields'] if f['offset']==offset)['size']==4
        source_proofs=[evidence('converter',p['line']) for p in path['evidence']]
        if any('pcVar9' in p['excerpt'] for p in source_proofs):source_proofs.insert(0,evidence('converter',alias_lines[0]))
        read_first=read[:m.start()].count('\n')+1
        read_last=read_first+m[0].count('\n')
        fields.append(dict(raw_offset=offset,raw_offset_hex=hex(offset),raw_bytes=4,raw_kind='u32',baseline_name=baseline['name'],
            native_label=native_label['text'],label_resource=native_label,property_id=prop,
            editor_intermediate_offset=mid,editor_model_offset=model,editor_panel_offset=panel,
            raw_to_display=dict(constant_address=model_path['constant'],numerator=scale.numerator,denominator=scale.denominator),
            display_to_writer=dict(constant_address=write_constant,conversion_helper=0x4ACDA8,store='low DWORD of converted result'),
            property_update=update_kind,gui_validation_range=None,
            evidence=source_proofs+model_path['evidence']+fills[panel]['evidence']+
                [evidence('extract',read_first),evidence('extract',read_last-1),evidence('extract',read_last)]+
                [evidence('update',n) for n in sorted(set(update_lines))]+[commits[panel]['evidence']]+writer_proofs))
    fields.sort(key=lambda f:f['raw_offset']);assert len(fields)==22
    torture=next(f for f in fields if f['raw_offset']==0xEF4)
    assert torture['property_update']=='truncate_to_integer_then_low8_then_double'
    result=dict(family='Creatures',record_variants=[5449,5537],native_writer_record_bytes=5537,
        executable_sha256=json.loads((ROOT/'executable-hash-verification.json').read_text()),constants=constants,fields=fields,
        zero_extension_evidence='The saved callback builds local_14/uStack_10 as raw low DWORD/high DWORD0 before the double conversion.',
        scope=__doc__,torture_time_rule=dict(raw_offset=0xEF4,property_id=0xF856,
            formula='If the property update receives the original loaded value: raw_after = (((raw >>12) &255) <<12).',
            trigger='Execution of property-update case0xF856, including when it is the selected property during panel commit.',
            not_a_blanket_save_rule='Panel commit calls the update dispatcher for the selected property. Merely loading/writing this field does not apply the byte mask.',
            examples=[dict(seconds_before=300,seconds_after=44),dict(seconds_before=360,seconds_after=104),dict(seconds_before=32000,seconds_after=0)],
            evidence=[evidence('commit',next(i+1 for i,line in enumerate(text('commit').splitlines()) if 'FUN_0043fd60(param_1)' in line)),
                evidence('update',next(i+1 for i,line in enumerate(text('update').splitlines()) if 'uVar1 = *(uint *)(param_1 + 0x1aa8)' in line)),
                evidence('extract',next(i+1 for i,line in enumerate(text('extract').splitlines()) if '+ 0x1aa8) = uVar1' in line))]),
        limitations=['Native UI conversions and saved control flow only; no modified-map gameplay or new native emulations.',
            'The Torture Time projection assumes __ftol truncation toward zero and original loaded input. Formatting/parser effects are not modeled.',
            'Numeric labels are not independent proof of game units or an allowed GUI range.',
            'Do not apply native property side effects automatically in a lossless serializer.',
            'Floating-point writer fidelity for arbitrary edited doubles, infinities, NaNs and x87 rounding modes is outside the projection.'])
    (ROOT/'creature-editor-scaled.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    print('Scaled creature editor properties:',len(fields))
    for f in fields:print(f['raw_offset_hex'],f['native_label'],f['raw_to_display']['numerator'],f['raw_to_display']['denominator'],f['property_update'])

if __name__=='__main__':main()
