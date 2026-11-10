"""Attest Pro icon instructions/tables to original PE; keep static scope explicit."""
from pathlib import Path
import hashlib,json,re,sys
import capstone
from build_effect_semantics import pe_read
from emulate_editor_object_icons import ROOT,EXE

FUNCTIONS=[0x42D570,0x4A1D80,0x41BB90,0x41C0B0,0x41CE60,0x41CB30,
    0x41C8E0,0x49E960,0x497AC0,0x497E20,0x484720,0x45A400,0x45AC40,0x4A9390]

def sha(b):return hashlib.sha256(b).hexdigest()
def seed():
    image=EXE.read_bytes();md=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32);rows=[]
    for a in FUNCTIONS:
        asm=f'ghidra/phase121-icon-{a:06x}-disassemble_function.txt'
        text=(ROOT/asm).read_text(encoding='utf8')
        addresses=[int(m[1],16) for m in re.finditer(r'^([0-9a-fA-F]{8}):',text,re.M)]
        assert addresses and addresses[0]==a
        last=next(md.disasm(pe_read(image,addresses[-1],15),addresses[-1]))
        n=addresses[-1]+last.size-a
        native=[i.address for i in md.disasm(pe_read(image,a,n),a)]
        assert native==addresses,(hex(a),len(native),len(addresses))
        rows.append(dict(address=a,length=n,assembly=asm,assembly_sha256=sha((ROOT/asm).read_bytes()),instruction_count=len(native)))
    return rows

def queries():
    q=[]
    for r in seed():
        a=r['address'];q.append(dict(id=f'phase121-icon-{a:06x}-read_memory',tool='read_memory',
            args=dict(address=hex(a),length=r['length'],program='DK2 Editor pro.exe'),print_limit=0))
    q.append(dict(id='phase121-icon-string-xrefs',tool='get_xrefs_to',args=dict(address='0x539b30',program='DK2 Editor pro.exe'),print_limit=0))
    q.append(dict(id='phase121-editor-save',tool='save_program',args=dict(program='DK2 Editor pro.exe'),print_limit=0))
    (ROOT/'phase121-editor-primary-queries.json').write_text(json.dumps(q,indent=2),encoding='utf8')

def build():
    image=EXE.read_bytes();stock=(EXE.parent/'DK2 Editor.exe').read_bytes();rows=[]
    for r in seed():
        a,n=r['address'],r['length'];p=ROOT/f'ghidra/phase121-icon-{a:06x}-read_memory.txt'
        saved=json.loads(p.read_text(encoding='utf8'));b=bytes(saved['data'])
        assert int(saved['address'],16)==a and len(b)==n and b==pe_read(image,a,n)
        rows.append(dict(r,memory=str(p.relative_to(ROOT)),memory_sha256=sha(p.read_bytes()),
            original_bytes_sha256=sha(b),stock_bytes_identical=b==pe_read(stock,a,n)))
    tables=[]
    for a in [0x50AC9C,0x50AFFC]:
        p=ROOT/f'ghidra/phase121-icon-table-{a:06x}.txt';j=json.loads(p.read_text(encoding='utf8'));b=bytes(j['data'])
        assert b==pe_read(image,a,104)
        assert int.from_bytes(b[0x24:0x28],'little')==0x41CB30
        assert int.from_bytes(b[0x28:0x2C],'little')==0x41C8E0
        tables.append(dict(address=a,length=104,memory=str(p.relative_to(ROOT)),sha256=sha(p.read_bytes())))
    message=ROOT/'ghidra/phase121-icon-spin-message-map.txt'
    j=json.loads(message.read_text(encoding='utf8'));entry=bytes(j['data'])
    import struct
    assert entry==pe_read(image,0x511710,24)
    assert struct.unpack('<6I',entry)==(0x4E,0xFD2E,1309,1309,38,0x45AC40)
    dialogs=json.loads((ROOT/'editor-dialogs.json').read_text(encoding='utf8'))
    assert dialogs['sha256']==sha(image)
    for identifier in [256,327]:
        dialog=next(d for d in dialogs['dialogs'] if d['resource_path'][1]==identifier)
        selected={c['id']:c for c in dialog['controls'] if c['id'] in [1308,1309,1310]}
        assert len(selected)==3 and selected[1310]['title']=='Object Icon'
        assert all(c['template_visible'] for c in selected.values())
    bitmap=EXE.parent/'Graphics/ObjectIcons.bmp';b=bitmap.read_bytes()
    assert b[:2]==b'BM' and struct.unpack_from('<ii',b,18)==(256,256)
    return dict(phase=121,functions=rows,tables=tables,
        UI_message_map=dict(address=0x511710,length=24,words=list(struct.unpack('<6I',entry)),
            memory=str(message.relative_to(ROOT)),sha256=sha(message.read_bytes()),
            dialog_metadata_file='editor-dialogs.json',dialog_metadata_sha256=sha((ROOT/'editor-dialogs.json').read_bytes())),
        bitmap=dict(file=str(bitmap),sha256=sha(b),
        width=256,height=256,cell_width=64,cell_height=64,columns=4,rows=4,slots=16),
        source_exe_sha256=sha(image),stock_code_equal_ranges=sum(r['stock_bytes_identical'] for r in rows),
        scope='14wholeoriginalfunctionranges,2vtableblocks,1MFCmessage entry. Static nativeMFC/GUI/render/save paths; only converter/atoi/atlas getter receive bounded CPU executions.')

if __name__=='__main__':
    if '--queries' in sys.argv:queries();print('Prepared14primarymemoryqueries.')
    else:
        value=build();(ROOT/'phase121-primary-manifest.json').write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf8')
        print('14functionranges/2vtables checked;stock equal',value['stock_code_equal_ranges'])
