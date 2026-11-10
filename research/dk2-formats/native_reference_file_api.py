"""Declared external Win32 services for original multi-file Level traversal.

Memory-backed ASCII filenames, bounded handles/heap and synchronous file reads.
Original instructions, import table and game vtables remain unchanged.
"""
import struct
import pefile
from native_file_api_fixture import Asm,d,jump
from build_effect_semantics import pe_read
HEAP,HEAP_BYTES,HSTATE,TLS,CRIT=0x23000000,0x80000,0x23090000,0x230A0000,0x230B0000
FILES,NAMES,HANDLES,STATE,OPENLOG,READLOG=0x23100000,0x23110000,0x23120000,0x23130000,0x23140000,0x23150000
CODE=0x23200000
HANDLE_BASE=0xABCD0000
MAX_OPEN=64


def build(exe,files):
    files=[(name,address,size,128) for name,address,size in files]+[('C:\\DK2\\',0,0,16),('C:\\DK2',0,0,16)]
    assert len(files)<=16
    imports={}
    for dll in pefile.PE(data=exe).DIRECTORY_ENTRY_IMPORT:
        for i in dll.imports:
            if i.name:imports[i.name.decode()]=(i.address,int.from_bytes(pe_read(exe,i.address,4),'little'))
    memory={HEAP:bytes([165])*HEAP_BYTES,HSTATE:bytes(0x1000),TLS:bytes(0x100),CRIT:bytes(24),
        FILES:bytes(16*16),NAMES:bytes(16*256),HANDLES:bytes(64*24),STATE:bytes(64),OPENLOG:bytes([165])*(64*272),READLOG:bytes([165])*(1024*24)}
    memory[TLS]=bytes(20)+d(1)+bytes(0x100-24)
    desc=bytearray(memory[FILES]);names=bytearray(memory[NAMES])
    for i,(name,address,size,attributes) in enumerate(files):
        encoded=name.encode('ascii')+b'\0';assert len(encoded)<=256
        names[256*i:256*i+len(encoded)]=encoded;struct.pack_into('<4I',desc,16*i,NAMES+256*i,address,size,attributes)
    memory[FILES],memory[NAMES]=bytes(desc),bytes(names)
    memory[0x23170000]=b'C:\\DK2\\DKII-DX.exe\0'
    memory[0x23180000]=bytes([165])*(64*256)
    bodies={}
    a=Asm();a.emit(bytes.fromhex('53565755817c2414')+d(0x1234));a.branch('0f85','bad')
    a.emit(bytes.fromhex('8b44241c83c00f83e0f08b0d')+d(HSTATE)+bytes.fromhex('8d140181fa')+d(HEAP_BYTES));a.branch('0f87','bad')
    a.emit(bytes.fromhex('8915')+d(HSTATE)+bytes.fromhex('81c1')+d(HEAP)+bytes.fromhex('89cd8b15')+d(HSTATE+4)+bytes.fromhex('81fa00010000'));a.branch('0f83','bad')
    a.emit(bytes.fromhex('8904d5')+d(HSTATE+0x100)+bytes.fromhex('890cd5')+d(HSTATE+0x104)+bytes.fromhex('ff05')+d(HSTATE+4))
    a.emit(bytes.fromhex('f744241808000000'));a.branch('0f84','done')
    a.emit(bytes.fromhex('89cf89c131c0f3aa'));a.label('done');a.emit(bytes.fromhex('89e85d5f5e5bc20c00'))
    a.label('bad');a.emit(bytes.fromhex('ff05')+d(HSTATE+8)+bytes.fromhex('31c05d5f5e5bc20c00'));bodies['HeapAlloc']=a.finish()
    bodies['HeapFree']=bytes.fromhex('ff05')+d(HSTATE+12)+bytes.fromhex('b801000000c20c00')
    bodies['EnterCriticalSection']=bytes.fromhex('c20400');bodies['LeaveCriticalSection']=bytes.fromhex('c20400')
    bodies['TlsGetValue']=b'\xB8'+d(TLS)+bytes.fromhex('c20400')
    bodies['GetLastError']=b'\xA1'+d(STATE+20)+b'\xC3'
    bodies['SetLastError']=bytes.fromhex('8b442404a3')+d(STATE+20)+bytes.fromhex('c20400')
    bodies['IsDBCSLeadByte']=bytes.fromhex('31c0c20400')
    bodies['GetCommandLineA']=b'\xB8'+d(0x23170000)+b'\xC3'
    bodies['InterlockedIncrement']=bytes.fromhex('8b542404ff028b02c20400')
    bodies['InterlockedDecrement']=bytes.fromhex('8b542404ff0a8b02c20400')
    bodies['CreateSemaphoreA']=b'\xA1'+d(STATE+24)+b'\x05'+d(0xBEEF0000)+bytes.fromhex('ff05')+d(STATE+24)+bytes.fromhex('c21000')
    bodies['WaitForSingleObject']=bytes.fromhex('ff05')+d(STATE+28)+bytes.fromhex('31c0c20800')
    bodies['ReleaseSemaphore']=bytes.fromhex('ff05')+d(STATE+32)+bytes.fromhex('b801000000c20c00')
    a=Asm();a.emit(bytes.fromhex('535657558b1d')+d(STATE+40)+bytes.fromhex('83fb40'));a.branch('0f83','missing')
    a.emit(bytes.fromhex('89dfc1e70881c7')+d(0x23180000)+bytes.fromhex('8b742414b9ff000000'))
    a.label('copy');a.emit(bytes.fromhex('acaa84c0'));a.branch('0f84','copied');a.emit(bytes.fromhex('49'));a.branch('0f85','copy')
    a.label('copied');a.emit(bytes.fromhex('ff05')+d(STATE+40)+bytes.fromhex('31ed'))
    a.label('match');a.emit(bytes.fromhex('81fd')+d(len(files)));a.branch('0f83','missing');a.emit(bytes.fromhex('89eac1e2048bb2')+d(FILES)+bytes.fromhex('8b7c2414'))
    a.label('chars');a.emit(bytes.fromhex('8a068a0f3c41'));a.branch('0f82','leftlower');a.emit(bytes.fromhex('3c5a'));a.branch('0f87','leftlower');a.emit(bytes.fromhex('0c20'))
    a.label('leftlower');a.emit(bytes.fromhex('80f941'));a.branch('0f82','rightlower');a.emit(bytes.fromhex('80f95a'));a.branch('0f87','rightlower');a.emit(bytes.fromhex('80c920'))
    a.label('rightlower');a.emit(bytes.fromhex('38c8'));a.branch('0f85','next');a.emit(bytes.fromhex('84c0'));a.branch('0f84','found');a.emit(bytes.fromhex('4647'));a.branch('e9','chars')
    a.label('next');a.emit(bytes.fromhex('45'));a.branch('e9','match')
    a.label('found');a.emit(bytes.fromhex('8b82')+d(FILES+12)+bytes.fromhex('5d5f5e5bc20400'))
    a.label('missing');a.emit(bytes.fromhex('c705')+d(STATE+20)+d(2)+bytes.fromhex('b8ffffffff5d5f5e5bc20400'));bodies['GetFileAttributesA']=a.finish()
    # CreateFileA logs every attempted name, then opens only a declared file.
    a=Asm();a.emit(bytes.fromhex('535657558b1d')+d(STATE)+bytes.fromhex('83fb40'));a.branch('0f83','bad')
    a.emit(bytes.fromhex('69fb1001000081c7')+d(OPENLOG)+bytes.fromhex('c707ffffffff8b742414b9ff00000083c708'))
    a.label('copy');a.emit(bytes.fromhex('acaa84c0'));a.branch('0f84','copied');a.emit(bytes.fromhex('49'));a.branch('0f85','copy')
    a.label('copied');a.emit(bytes.fromhex('31ed'))
    a.label('match');a.emit(bytes.fromhex('81fd')+d(len(files)));a.branch('0f83','missing')
    a.emit(bytes.fromhex('89eac1e2048bb2')+d(FILES)+bytes.fromhex('8b7c2414'))
    a.label('chars');a.emit(bytes.fromhex('8a068a0f3c41'));a.branch('0f82','leftlower');a.emit(bytes.fromhex('3c5a'));a.branch('0f87','leftlower');a.emit(bytes.fromhex('0c20'))
    a.label('leftlower');a.emit(bytes.fromhex('80f941'));a.branch('0f82','rightlower');a.emit(bytes.fromhex('80f95a'));a.branch('0f87','rightlower');a.emit(bytes.fromhex('80c920'))
    a.label('rightlower');a.emit(bytes.fromhex('38c8'));a.branch('0f85','next');a.emit(bytes.fromhex('84c0'));a.branch('0f84','found');a.emit(bytes.fromhex('4647'));a.branch('e9','chars')
    a.label('next');a.emit(bytes.fromhex('45'));a.branch('e9','match')
    a.label('found');a.emit(bytes.fromhex('f782')+d(FILES+12)+d(16));a.branch('0f85','missing')
    a.emit(bytes.fromhex('8b0d')+d(STATE+4)+bytes.fromhex('83f940'));a.branch('0f83','bad')
    a.emit(bytes.fromhex('6bf91881c7')+d(HANDLES)+bytes.fromhex('8b82')+d(FILES+4)+bytes.fromhex('89078b82')+d(FILES+8)+bytes.fromhex('894704c7470800000000c7470c01000000896f10'))
    a.emit(bytes.fromhex('8d048d')+d(HANDLE_BASE)+bytes.fromhex('69fb1001000081c7')+d(OPENLOG)+bytes.fromhex('892f894704ff05')+d(STATE+4));a.branch('e9','return')
    a.label('missing');a.emit(bytes.fromhex('c705')+d(STATE+20)+d(2)+bytes.fromhex('b8ffffffff'));a.branch('e9','return')
    a.label('bad');a.emit(bytes.fromhex('ff05')+d(STATE+16)+bytes.fromhex('b8ffffffff'))
    a.label('return');a.emit(bytes.fromhex('ff05')+d(STATE)+bytes.fromhex('5d5f5e5bc21c00'));bodies['CreateFileA']=a.finish()
    # Shared handle normalization leaves EDX -> an open24-byte descriptor.
    def handle(a,stackoff):
        a.emit(bytes.fromhex('8b54')+bytes([0x24,stackoff])+bytes.fromhex('81ea')+d(HANDLE_BASE)+bytes.fromhex('f7c203000000'));a.branch('0f85','bad')
        a.emit(bytes.fromhex('c1ea0283fa40'));a.branch('0f83','bad')
        a.emit(bytes.fromhex('6bd21881c2')+d(HANDLES)+bytes.fromhex('837a0c01'));a.branch('0f85','bad')
    a=Asm();a.emit(bytes.fromhex('53565755'));handle(a,0x14)
    a.emit(bytes.fromhex('8b74242085f6'));a.branch('0f84','bad');a.emit(bytes.fromhex('c70600000000837c242400'));a.branch('0f85','bad')
    a.emit(bytes.fromhex('8b4c241c81f900000100'));a.branch('0f87','bad')
    a.emit(bytes.fromhex('8b7c241889f801c8'));a.branch('0f82','bad')
    a.emit(bytes.fromhex('81ff')+d(0x746EC0));a.branch('0f82','heapdest');a.emit(b'\x3D'+d(0x756EC0));a.branch('0f86','destok')
    a.label('heapdest');a.emit(bytes.fromhex('81ff')+d(HEAP));a.branch('0f82','stackdest');a.emit(b'\x3D'+d(HEAP+HEAP_BYTES));a.branch('0f86','destok')
    a.label('stackdest');a.emit(bytes.fromhex('81ff')+d(0x7FFEE000));a.branch('0f82','bad');a.emit(b'\x3D'+d(0x7FFF2000));a.branch('0f87','bad')
    a.label('destok');a.emit(bytes.fromhex('8b42042b4208'));a.branch('0f83','available');a.emit(bytes.fromhex('31c0'))
    a.label('available');a.emit(bytes.fromhex('39c1'));a.branch('0f86','sizeok');a.emit(bytes.fromhex('89c1'))
    a.label('sizeok');a.emit(bytes.fromhex('89cb8b328b6a0801ee01dd896a0829ddf3a48b742420891e'))
    a.emit(bytes.fromhex('8b0d')+d(STATE+8)+bytes.fromhex('81f900040000'));a.branch('0f83','bad')
    a.emit(bytes.fromhex('6bc91881c1')+d(READLOG)+bytes.fromhex('8b44241489018969048959088b44241c89410c8b442418894110c7411401000000ff05')+d(STATE+8)+bytes.fromhex('b8010000005d5f5e5bc21400'))
    a.label('bad');a.emit(bytes.fromhex('ff05')+d(STATE+16)+bytes.fromhex('31c05d5f5e5bc21400'));bodies['ReadFile']=a.finish()
    a=Asm();handle(a,4);a.emit(bytes.fromhex('837c240c00'));a.branch('0f85','bad')
    a.emit(bytes.fromhex('8b4424088b4c241083f900'));a.branch('0f84','store');a.emit(bytes.fromhex('83f901'));a.branch('0f84','relative');a.emit(bytes.fromhex('83f902'));a.branch('0f85','bad')
    a.emit(bytes.fromhex('034204'));a.branch('e9','store');a.label('relative');a.emit(bytes.fromhex('034208'))
    a.label('store');a.emit(bytes.fromhex('85c0'));a.branch('0f88','bad');a.emit(bytes.fromhex('894208c21000'))
    a.label('bad');a.emit(bytes.fromhex('ff05')+d(STATE+16)+bytes.fromhex('b8ffffffffc21000'));bodies['SetFilePointer']=a.finish()
    a=Asm();handle(a,4);a.emit(bytes.fromhex('8b4c240885c9'));a.branch('0f84','ret');a.emit(bytes.fromhex('c70100000000'));a.label('ret');a.emit(bytes.fromhex('8b4204c20800'))
    a.label('bad');a.emit(bytes.fromhex('ff05')+d(STATE+16)+bytes.fromhex('b8ffffffffc20800'));bodies['GetFileSize']=a.finish()
    a=Asm();a.emit(bytes.fromhex('8b4424043d0000efbe'));a.branch('0f82','file');a.emit(bytes.fromhex('3d4000efbe'));a.branch('0f83','file')
    a.emit(bytes.fromhex('ff05')+d(STATE+36)+bytes.fromhex('b801000000c20400'));a.label('file');handle(a,4);a.emit(bytes.fromhex('c7420c00000000ff05')+d(STATE+12)+bytes.fromhex('b801000000c20400'))
    a.label('bad');a.emit(bytes.fromhex('ff05')+d(STATE+44)+bytes.fromhex('c705')+d(STATE+20)+d(6)+bytes.fromhex('31c0c20400'));bodies['CloseHandle']=a.finish()
    proof=[]
    for i,(name,body) in enumerate(bodies.items()):
        iat,target=imports[name];address=CODE+0x1000*i
        memory[address]=body;memory[target]=jump(target,address)
        proof.append(dict(name=name,IAT=iat,original_target=target,provider=address,bytes=len(body)))
    allowed=[[a,len(memory[a])] for row in proof for a in [row['original_target'],row['provider']]]
    return memory,allowed,proof
