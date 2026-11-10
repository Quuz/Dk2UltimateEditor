"""Bounded PE/RTTI comparison of DX and the user's older CPlayer layout.

This maps the serializer/class-size anchors only. No offset translation or
old Crypt array identity is inferred from the state-block end.
"""
from pathlib import Path
import json,struct
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from native_shot_pair_trace import pe_image
import probe_crypt_state_writer as native
ROOT=native.ROOT


def inspect(file,expected_sha,*,name,col,table,writer,destructor,stride,byte_offset,block_size):
    blob=file.read_bytes();assert native.sha(blob)==expected_sha
    base,data=pe_image(blob)
    def read(a,n):
        assert base<=a and a+n<=base+len(data)
        return bytes(data[a-base:a-base+n])
    assert read(name,len(b'.?AVCPlayer@@\0'))==b'.?AVCPlayer@@\0'
    locator=list(struct.unpack('<5I',read(col,20)))
    assert locator[0]==locator[1]==locator[2]==0 and locator[3]==name-8
    assert struct.unpack('<3I',read(table-4,12))==(col,destructor,writer)
    md=Cs(CS_ARCH_X86,CS_MODE_32)
    prefix=list(md.disasm(read(writer,128),writer))
    assert any(i.mnemonic=='lea' and i.op_str==f'ecx, [esi + {hex(byte_offset)}]' for i in prefix)
    assert any(i.mnemonic=='lea' and i.op_str=='eax, [esi + 0x76]' for i in prefix)
    assert any(i.mnemonic=='push' and i.op_str==hex(block_size) for i in prefix)
    dtor=list(md.disasm(read(destructor,128),destructor))
    assert any(i.mnemonic=='push' and i.op_str==hex(stride) for i in dtor)
    return dict(file=str(file),sha256=expected_sha,RTTI=dict(name=name,type_descriptor=name-8,COL=col,COL_fields=locator,
                vtable=table,destructor=destructor,writer=writer,raw_vtable=read(table-4,16).hex()),
                serializer=dict(leading_BYTE_offset=byte_offset,fixed_block_offset=0x76,fixed_block_bytes=block_size,
                                fixed_block_end_exclusive=0x76+block_size),array_destructor_stride=stride,
                primary=dict(writer_prefix=dict(address=writer,length=128,hex=read(writer,128).hex(),
                            instructions=[dict(address=i.address,text=i.mnemonic+' '+i.op_str) for i in prefix]),
                             destructor_prefix=dict(address=destructor,length=128,hex=read(destructor,128).hex(),
                            instructions=[dict(address=i.address,text=i.mnemonic+' '+i.op_str) for i in dtor])),
                limitation='StaticRTTIandboundedprefixanchorsonly;nooldCryptmappingorfulloldCPUexecution.')


def build():
    dx=inspect(native.EXE,'d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a',
               name=0x69F338,col=0x67D900,table=0x66D99C,writer=0x4B8D40,destructor=0x4C1FB0,stride=0x1125,byte_offset=0x10FC,block_size=0x1028)
    old=inspect(ROOT/'source-binaries/DKII-local13.exe','93e826a142f677ef5120699feddb0572799d93399167ef1d05ec4b48a8b2a342',
                name=0x6918E8,col=0x66FE50,table=0x65E91C,writer=0x4AF8A0,destructor=0x4B8280,stride=0x1029,byte_offset=0x1000,block_size=0xF34)
    return dict(phase=190,DX=dx,older_user_copy=old,old_version_scope='User-suppliedcopylabelled13;notcertifiedretail1.3.',
                conclusion='CPlayerclassandserializercorrespond,butlayoutdiffers.DonotcarryDXcounteroffsetsorotherfieldsintotheoldercopywithoutindependentmapping.',
                old_tail20='Unassigned;neitherlast20identitynoroldCryptcountersemanticsisproved.')


def main():
    out=build();(ROOT/'phase190-player-runtime-layout.json').write_text(json.dumps(out,indent=2)+'\n',encoding='utf-8')
    print('DX',out['DX']['serializer'],'stride',hex(out['DX']['array_destructor_stride']))
    print('older',out['older_user_copy']['serializer'],'stride',hex(out['older_user_copy']['array_destructor_stride']))


if __name__=='__main__':main()
