"""Conservative scalar-copy traces from saved Ghidra decompilations.

Not a general C interpreter. Only literal-address scalar assignments are traced;
indexed loops, getter calls, block/resource copies and control flow are excluded.
An absent path is NOT evidence that a field is unused. Output retains exact
assignment text/line and records transforms rather than silently treating them
as plain copies.
"""
from pathlib import Path
import json,re

ROOT=Path(__file__).resolve().parent
NUMBER=r'(?:0x[0-9a-fA-F]+|\d+)'
WIDTH={'char':1,'byte':1,'undefined1':1,'short':2,'ushort':2,'undefined2':2,
    'int':4,'uint':4,'undefined4':4,'long':4,'ulong':4,'float':4}

def memory(expression,bases):
    expression=expression.strip()
    indexed=re.fullmatch(r'(param_[12])\[('+NUMBER+r')\]',expression)
    if indexed:return dict(base=indexed[1],offset=int(indexed[2],0)*bases[indexed[1]],bytes=bases[indexed[1]])
    typed=re.fullmatch(r'\*\((\w+) \*\)\((.*)\)',expression)
    if typed:
        kind,inside=typed.groups()
        if kind not in WIDTH:return None
        m=re.fullmatch(r'(\(int\))?(param_[12]) \+ ('+NUMBER+r')',inside)
        if not m:return None
        factor=1 if m[1] else bases[m[2]]
        return dict(base=m[2],offset=int(m[3],0)*factor,bytes=WIDTH[kind])
    direct=re.fullmatch(r'\*\((\w+) \*\)(param_[12])',expression)
    if direct and direct[1] in WIDTH:return dict(base=direct[2],offset=0,bytes=WIDTH[direct[1]])
    return None

def assignments(text):
    for line_number,line in enumerate(text.replace('\r','').splitlines(),1):
        m=re.fullmatch(r'\s*(.*?) = (.*);',line)
        if m:yield line_number,m[1],m[2],line.strip()

def converter(text):
    bindings={};paths=[];bases={'param_1':4,'param_2':1}
    for line,lhs,rhs,evidence in assignments(text):
        source=memory(rhs,bases);dest=memory(lhs,bases)
        if re.fullmatch(r'\w+',lhs):
            if source and source['base']=='param_2':bindings[lhs]=dict(source=source,line=line,evidence=evidence)
            else:bindings.pop(lhs,None)
        elif dest and dest['base']=='param_1':
            binding=bindings.get(rhs)
            if source and source['base']=='param_2':binding=dict(source=source,line=line,evidence=evidence)
            if binding:
                paths.append(dict(raw_offset=binding['source']['offset'],raw_bytes=binding['source']['bytes'],
                    intermediate_offset=dest['offset'],intermediate_bytes=dest['bytes'],
                    evidence=[dict(line=binding['line'],assignment=binding['evidence']),dict(line=line,assignment=evidence)]))
    return paths

def runtime(text):
    result=[];bases={'param_1':1,'param_2':1}
    pattern=r'\*\((\w+) \*\)\(param_2 \+ ('+NUMBER+r')\)'
    for line,lhs,rhs,evidence in assignments(text):
        dest=memory(lhs,bases)
        if not dest or dest['base']!='param_1':continue
        reads=list(re.finditer(pattern,rhs))
        if len(reads)!=1:continue
        m=reads[0]
        if m[1] not in WIDTH:continue
        result.append(dict(intermediate_offset=int(m[2],0),intermediate_bytes=WIDTH[m[1]],
            runtime_offset=dest['offset'],runtime_bytes=dest['bytes'],expression=rhs,
            transform='copy' if rhs==m[0] else 'expression',evidence=dict(line=line,assignment=evidence)))
    return result

def main():
    # All these converters have (undefined4 *destination, char *raw).
    specs=[('Creatures','game-creature-convert.txt','game-creature-type.txt'),
        ('Doors','game-doors-convert.txt','game-doors-runtime.txt'),
        ('Objects','game-objects-convert.txt','game-objects-runtime.txt'),
        ('Rooms','game-rooms-convert.txt','game-rooms-runtime.txt'),
        ('Traps','game-traps-convert.txt','game-traps-runtime.txt'),
        ('Terrain','game-terrain-convert.txt','game-terrain-runtime.txt'),
        ('KeeperSpells','game-keeperspells-convert.txt','game-keeperspells-runtime.txt'),
        ('CreatureSpells','game-creaturespells-convert.txt','game-creaturespells-runtime.txt'),
        ('Effects','game-effects-convert.txt','game-effect-definition-load.txt'),
        ('EffectElements','game-effectelements-convert.txt','game-element-definition-load.txt')]
    layouts=json.loads((ROOT/'layouts.json').read_text(encoding='utf8'));families={}
    for family,conv,load in specs:
        a=converter((ROOT/'ghidra'/conv).read_text(encoding='utf8'))
        b=runtime((ROOT/'ghidra'/load).read_text(encoding='utf8'));joined=[]
        for p in a:
            field=next((f for f in layouts[family]['fields'] if f['offset']<=p['raw_offset']<f['offset']+f['size']),None)
            for q in b:
                if p['intermediate_offset']!=q['intermediate_offset']:continue
                joined.append(dict(raw_offset=hex(p['raw_offset']),raw_bytes=p['raw_bytes'],
                    baseline_field=field['name'] if field else None,intermediate_offset=hex(p['intermediate_offset']),
                    runtime_offset=hex(q['runtime_offset']),runtime_bytes=q['runtime_bytes'],
                    transform=q['transform'],runtime_expression=q['expression'],
                    converter_evidence=p['evidence'],runtime_evidence=q['evidence']))
        families[family]=dict(converter_file=conv,runtime_file=load,
            literal_scalar_copies=len(a),literal_runtime_assignments=len(b),joined_paths=joined)
    result=dict(warning=__doc__,families=families)
    (ROOT/'scalar-storage-paths.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
    # Regression against the actual F55 chain; 788 is a separate attraction count.
    f55=next(p for p in families['Creatures']['joined_paths'] if p['raw_offset']=='0xf55')
    assert f55['intermediate_offset']=='0x1345' and f55['runtime_offset']=='0x6eb'
    print({family:len(value['joined_paths']) for family,value in families.items()})

if __name__=='__main__':main()
