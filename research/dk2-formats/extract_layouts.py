"""Generate auditable offset maps from OpenKeeper's sequential read statements.

This is a narrowly scoped source tracer, not a general Java interpreter. Every
read retains the source line. Resulting lengths must match the actual catalogues.
"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SOURCE = (ROOT/'sources/KwdFile.java').read_text(encoding='utf-8-sig')
# Preserve line numbers while suppressing comments.
TEXT = re.sub(r'/\*[\s\S]*?\*/|//[^\n]*',lambda m: '\n'*m[0].count('\n'),SOURCE)

def block(text, start, opening='{', closing='}'):
    assert text[start] == opening
    level=1
    for i in range(start+1,len(text)):
        level += (text[i]==opening)-(text[i]==closing)
        if not level:return text[start+1:i], i+1
    raise ValueError('Unclosed block')

def method(name):
    m = re.search(r'private\s+[\w.\[\]]+\s+'+name+r'\(', TEXT)
    start=TEXT.index('{',m.end())
    body,end=block(TEXT,start)
    return body, TEXT[:start+1].count('\n')+1

KINDS={
 'readUnsignedByte':('u8',1), 'readByteAsEnum':('enum8',1), 'readByteAsFlag':('flags8',1),
 'readUnsignedShort':('u16',2), 'readShort':('i16',2), 'readShortAsFloat':('i16/4096',2),
 'readShortAsEnum':('enum16',2),'readShortAsFlag':('flags16',2),
 'readUnsignedInteger':('u32',4), 'readInteger':('i32',4), 'readIntegerAsFloat':('i32/4096',4),
 'readIntegerAsDouble':('i32/65536',4), 'readIntegerAsEnum':('enum32',4), 'readIntegerAsFlag':('flags32',4),
 'readFloat':('float32',4),
}
READ=re.compile(r'readArtResource\(reader\)|readLight\(reader\)|readColor\(reader\)|readStringId\(reader\)|readJobPreferences\((\d+),\s*creature,\s*reader\)|reader\.(\w+)\(([^()]*)\)')

class Trace:
    def __init__(self,variant=5449):
        self.fields=[];self.offset=0;self.arrays={};self.variant=variant

    def walk(self,body,base_line=1,prefix=''):
        i=0
        while i<len(body):
            while i<len(body) and body[i].isspace():i+=1
            if i>=len(body):break
            begin=i
            m=re.match(r'(for|if)\s*\(',body[i:])
            if m:
                kind=m[1];p=i+m.end()-1
                cond,q=block(body,p,'(',')')
                while body[q].isspace():q+=1
                if body[q]!='{':raise ValueError('Expected braced conditional: '+body[i:i+100])
                sub,end=block(body,q)
                line=base_line+body[:q+1].count('\n')
                if kind=='for':
                    if ':' in cond:
                        length=7; label='AnimationOffsets'
                    else:
                        bound=re.search(r'<\s*(\w+)(\.length)?',cond)
                        if not bound:raise ValueError(cond)
                        length=int(bound[1]) if bound[1].isdigit() else self.arrays[bound[1]]
                        label=bound[1] if bound[2] else 'loop'
                        if label=='loop':
                            if 'Spell spell' in sub or 'var spell' in sub:label='Spells'
                            elif 'ArtResource resource' in sub:label='AdditionalResources'
                            elif 'generateIds.add' in sub:label='GenerateIds'
                            elif 'roomEffects.add' in sub:label='Effects'
                            elif 'roomObjects.add' in sub:label='Objects'
                    for x in range(length):self.walk(sub,line,prefix+label+'['+str(x)+'].')
                elif 'getItemSize() > CREATURE_SIZE' in cond:
                    if self.variant>5449:self.walk(sub,line,prefix+'Extension.')
                elif READ.search(sub):
                    raise ValueError('Unmodelled read conditional '+cond)
                i=end
                continue
            # Ordinary statement, which can contain an array initializer.
            p=i;depth=0
            while p<len(body):
                if body[p]=='{':depth+=1
                elif body[p]=='}':depth-=1
                if body[p]==';' and depth==0:break
                p+=1
            stmt=body[i:p+1];line=base_line+body[:i].count('\n');i=p+1
            arr=re.search(r'(\w+)(?:\[\])?\s*=\s*new\s+[\w.]+\[(\d+)\]',stmt)
            if arr:self.arrays[arr[1]]=int(arr[2])
            matches=list(READ.finditer(stmt))
            matches=[m for m in matches if m[2] not in ('position','readChunk')]
            if not matches:continue
            setter=re.search(r'\.set(\w+)\(',stmt)
            if setter:
                name=setter[1]
                if name in ('Animation','AnimationOffsets'):
                    extra=re.search(r'(?:AnimationType|OffsetType)\.(\w+)',stmt)
                    if extra:name+='.'+extra[1]
            else:
                lhs=re.search(r'(\w+)(?:\[\w+\])?\s*=',stmt)
                name=lhs[1] if lhs else 'Reserved'
            for j,m in enumerate(matches):
                token=m[0]
                children=None
                if token.startswith('readArtResource'):kind,size='ArtResource',84
                elif token.startswith('readLight'):kind,size='Light',24
                elif token.startswith('readColor'):kind,size='RGB8',3
                elif token.startswith('readStringId'):kind,size='StringId',24
                elif token.startswith('readJobPreferences'):
                    kind,size='JobPreference['+m[1]+']',12*int(m[1])
                else:
                    call,arg=m[2],m[3]
                    if call in KINDS:kind,size=KINDS[call]
                    elif call in ('read','readString','readAndCheckNull','skipBytes'):
                        size=int(arg);kind='string' if call=='readString' else 'bytes'
                    else:raise ValueError('Unhandled call '+token)
                enum=re.search(r'([\w.]+)\.class',token)
                f=dict(name=prefix+name+('.'+str(j) if len(matches)>1 else ''),offset=self.offset,
                       offset_hex=f'0x{self.offset:X}',size=size,kind=kind,source_line=line)
                if enum:f['enum']=enum[1]
                if re.search(r'unk|unknown|unreferenced|^X[0-9]|Reserved',name,re.I):f['semantics']='unresolved'
                else:f['semantics']='OpenKeeper interpretation'
                self.fields.append(f);self.offset+=size

METHODS={'Terrain':('readTerrain',552),'Rooms':('readRooms',1055),'Doors':('readDoors',616),
 'Traps':('readTraps',579),'KeeperSpells':('readKeeperSpells',406),'CreatureSpells':('readCreatureSpells',266),
 'Creatures':('readCreatures',5449),'Objects':('readObjects',894),'EffectElements':('readEffectElements',182),
 'Effects':('readEffects',246),'Shots':('readShots',239)}

def main():
    result={}
    for family,(name,size) in METHODS.items():
        body,line=method(name)
        a=re.search(r'\w+\.setName\(',body).start()
        b=body.index('checkOffset(header',a)
        part=body[a:b];startline=line+body[:a].count('\n')
        for variant in ([5449,5537] if family=='Creatures' else [size]):
            t=Trace(variant);t.walk(part,startline)
            assert t.offset==variant,(family,t.offset,variant)
            result[family+('_5537' if variant==5537 else '')]=dict(record_size=variant,fields=t.fields)
            print(family,variant,len(t.fields),'fields')
    (ROOT/'layouts.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    things={}
    body,line=method('readThings')
    for case in re.finditer(r'case (THING_\w+):\s*\{',body):
        start=case.end()-1;part,end=block(body,start)
        t=Trace();t.walk(part,line+body[:start+1].count('\n'))
        tag=int(re.search(r'\b'+case[1]+r'\s*=\s*(\d+)',TEXT)[1])
        things[f'0x{tag:X}']=dict(name=case[1],payload_size=t.offset,fields=t.fields)
    (ROOT/'thing-layouts.json').write_text(json.dumps(things,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Things:',[(k,x['payload_size']) for k,x in things.items()])
    enums={}
    for path in (ROOT/'sources').glob('*.java'):
        text=re.sub(r'/\*[\s\S]*?\*/|//[^\n]*','',path.read_text(encoding='utf-8-sig'))
        scopes=[]
        for m in re.finditer(r'\b(class|enum)\s+(\w+)[^{;]*\{',text):
            try:sub,end=block(text,m.end()-1)
            except ValueError:continue
            scopes.append((m.start(),end,m[2],m[1],sub))
        for start,end,name,kind,sub in scopes:
            if kind!='enum':continue
            parents=[s[2] for s in scopes if s[0]<start and s[1]>end]
            key='.'.join(parents+[name])
            values=re.findall(r'\b([A-Z][A-Z_0-9]*)\(\s*(-?(?:0x[0-9A-Fa-f]+|\d+))[Ll]?\s*\)',sub.split(';')[0])
            if values:enums[key]={str(int(v,0) if 'x' in v.lower() else int(v)):n for n,v in values}
    (ROOT/'enums.json').write_text(json.dumps(enums,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Enums:',len(enums))
    return result

if __name__=='__main__':main()
