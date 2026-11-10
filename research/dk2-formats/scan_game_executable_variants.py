"""Exhaustive byte comparison of supplied normal/DX EXEs; no runtime DLL claim."""
from pathlib import Path
import collections,hashlib,json
import pefile

ROOT=Path(__file__).resolve().parent
GAME=ROOT.parent.parent/'Dungeon Keeper 2'
PROFILES={
    'normal':('DKII.EXE','e99818fbe9ad337ab4a24fa76901d5fe5a8c0a0dd306172d24b9547762c534cc'),
    'DX':('DKII-DX.exe','d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a')}
def sha(b):return hashlib.sha256(b).hexdigest()

def build():
    images={};profiles=[]
    for key,(name,h) in PROFILES.items():
        data=(GAME/name).read_bytes();assert sha(data)==h;images[key]=data
        p=pefile.PE(data=data)
        profiles.append(dict(key=key,file=str(GAME/name),sha256=h,bytes=len(data),
            image_base=p.OPTIONAL_HEADER.ImageBase,entrypoint_RVA=p.OPTIONAL_HEADER.AddressOfEntryPoint,
            memory_image_bytes=p.OPTIONAL_HEADER.SizeOfImage))
    a,b=images['normal'],images['DX'];assert len(a)==len(b)
    differences=[i for i,(x,y) in enumerate(zip(a,b)) if x!=y]
    assert differences==list(range(2668760,2668765))
    pa,pb=pefile.PE(data=a),pefile.PE(data=b);off=differences[0];rva=pa.get_rva_from_offset(off);va=pa.OPTIONAL_HEADER.ImageBase+rva
    assert va==0x68C8D8 and a[off:off+10]==b'DDRAW.dll\0' and b[off:off+10]==b'PATCH.dll\0'
    sections=[]
    for x,y in zip(pa.sections,pb.sections):
        assert x.__pack__()==y.__pack__()
        ba,bb=x.get_data(),y.get_data();equal=ba==bb
        sections.append(dict(name=x.Name.rstrip(b'\0').decode(),RVA=x.VirtualAddress,
            file_offset=x.PointerToRawData,raw_bytes=x.SizeOfRawData,virtual_bytes=x.Misc_VirtualSize,
            executable=bool(x.Characteristics&0x20000000),identical=equal,
            normal_sha256=sha(ba),DX_sha256=sha(bb),different_bytes=sum(u!=v for u,v in zip(ba,bb))))
        if x.Characteristics&0x20000000:assert equal
    assert sum(s['different_bytes'] for s in sections)==5
    imports=[]
    assert len(pa.DIRECTORY_ENTRY_IMPORT)==len(pb.DIRECTORY_ENTRY_IMPORT)
    for x,y in zip(pa.DIRECTORY_ENTRY_IMPORT,pb.DIRECTORY_ENTRY_IMPORT):
        assert x.struct.__pack__()==y.struct.__pack__()
        xs=[(i.name,i.ordinal,i.address) for i in x.imports];ys=[(i.name,i.ordinal,i.address) for i in y.imports]
        assert xs==ys
        if x.dll!=y.dll:
            assert x.dll==b'DDRAW.dll' and y.dll==b'PATCH.dll'
            imports.append(dict(normal_library=x.dll.decode(),DX_library=y.dll.decode(),name_VA=va,
                symbols=[dict(name=i.name.decode(),IAT_VA=i.address) for i in x.imports]))
    assert len(imports)==1
    patch=GAME/'patch.dll';blob=patch.read_bytes();p=pefile.PE(data=blob)
    exports=[dict(name=e.name.decode() if e.name else None,ordinal=e.ordinal,RVA=e.address) for e in p.DIRECTORY_ENTRY_EXPORT.symbols]
    assert [e['name'] for e in exports]==['DirectDrawCreate','DirectDrawEnumerateA']
    return dict(phase=124,profiles=profiles,exact_file_differences=dict(count=5,file_offset=off,RVA=rva,VA=va,
        normal_bytes=a[off:off+10].hex(),DX_bytes=b[off:off+10].hex(),role='Name string ofonePEimportdescriptor'),
        all_other_file_bytes_identical=True,sections=sections,changed_import_descriptors=imports,
        patch_DLL=dict(file=str(patch),sha256=sha(blob),bytes=len(blob),exports=exports,
            scope='Presence and export table read only. No DLL execution orallinternalhook audit.'),
        rule='Normal SHA may reuse suppliedDX static definition-layout/code projections atidenticalVA. Keepactualsourcehash. WholeWindowsbehaviornot declared equivalent:imports targetdifferentDLLs.',
        limitations=['SourcePEbyteidentity isnot independent code implementation orallbranch execution.',
            'WindowsPEloader,IATbinding,patch.dll/Flame/dynamicpatching/graphics/OScompatibility not covered by isolatedcodeprojection.',
            'No claim thatsoftwarelauncherDKII_SOFT.EXE hasthislayout;not amongthematchedpair.',
            'KeeperC8 andremainingunknownfields stayunresolved;no consumer inferredfrombinaryequality.'])

if __name__=='__main__':
    value=build();(ROOT/'phase124-game-executable-variants.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
    print('Two2977280byteEXEs differby5importnamebytes;allexecutablebytes andallotherdatamatch.')
