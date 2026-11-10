"""Execute original constructor loops that narrow each group's LOD counts.

Isolated loop boundaries only: allocation, file IO and complete rendering excluded.
"""
from pathlib import Path
import hashlib, json, struct, sys
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'dependencies/unicorn-2.1.4'))
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32
from unicorn import x86_const as xc
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
import pefile
sha=lambda x:hashlib.sha256(x).hexdigest()
EXE=ROOT.parents[1]/'Dungeon Keeper 2/DKII-DX.exe'
RESOURCE,BUFFER,COUNTS,POLYS,STACK=0x01000000,0x02000000,0x03000000,0x04000000,0x05000000


def main():
    raw=EXE.read_bytes();pe=pefile.PE(data=raw);base=pe.OPTIONAL_HEADER.ImageBase
    assert sha(raw)=='d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a'
    cases=[];segments=[]
    for kind,start,stop,resoff,countoff,ptroff,stack_counts,stack_lods,stack_pointer,stack_poly in [
        ('mesh',0x57E9CA,0x57EA27,8,0x44,4,0x30,0x28,0x38,0x2C),
        ('anim',0x57E469,0x57E4C6,0x20,4,0x14,0x54,0x24,0x28,0x38)]:
        blob=pe.get_data(start-base,stop-start)
        segments.append(dict(kind=kind,start=hex(start),stop_exclusive=hex(stop),hex=blob.hex(),sha256=sha(blob),
            instructions=[dict(address=hex(x.address),hex=x.bytes.hex(),instruction=x.mnemonic+' '+x.op_str) for x in Cs(CS_ARCH_X86,CS_MODE_32).disasm(blob,start)]))
        u=Uc(UC_ARCH_X86,UC_MODE_32);page=start&~4095;u.mem_map(page,0x1000);u.mem_write(page,pe.get_data(page-base,0x1000))
        for p in [RESOURCE,BUFFER,COUNTS,POLYS,STACK]:u.mem_map(p,0x1000)
        for counts in [[n] for n in [0,1,127,254,255,256,257,511,65535]]+[[255]*16,[1]*17]:
            u.mem_write(BUFFER,bytes([0xA5])*512);u.mem_write(RESOURCE+resoff,struct.pack('<I',BUFFER))
            u.mem_write(COUNTS,b''.join(struct.pack('<I',n) for n in counts))
            sp=STACK+0x800
            for off,val in [(stack_counts,COUNTS),(stack_lods,len(counts)),(stack_pointer,ptroff),(stack_poly,POLYS)]:u.mem_write(sp+off,struct.pack('<I',val))
            for name,val in [('ESI',RESOURCE),('EDI',0),('EAX',0),('ECX',0),('ESP',sp)]:u.reg_write(getattr(xc,'UC_X86_REG_'+name),val)
            u.emu_start(start,stop,count=2000);assert u.reg_read(xc.UC_X86_REG_EIP)==stop
            output=bytes(u.mem_read(BUFFER,0x5C));stored=list(output[countoff:countoff+min(16,len(counts))])
            if len(counts)<=16:
                assert stored==[n&255 for n in counts]
                expected_offsets=[];offset=0
                for n in counts:expected_offsets.append(POLYS+offset);offset+=(n&255)*3
                assert [struct.unpack_from('<I',output,ptroff+4*i)[0] for i in range(len(counts))]==expected_offsets
            else:
                # The17th entry overwrites another field of the0x5C header.
                assert output[0x54:0x58]!=bytes([0xA5])*4
                if kind=='mesh':assert stored[:4]==list(struct.pack('<I',POLYS+48))
                else:assert struct.unpack_from('<I',output,0x54)[0]==POLYS+48
            cases.append(dict(kind=kind,input_counts=counts,stored_first_counts=stored,post_header_hex=output.hex(),
                narrow_to_u8_verified=len(counts)<=16,LOD17_overwrites_header_fields=len(counts)==17))
        assert bytes(u.mem_read(start,len(blob)))==blob
    report=dict(schema_version=1,date='2026-11-10',passed=True,exe_sha256=sha(raw),probe_sha256=sha(Path(__file__).read_bytes()),
        segments=segments,cases=cases,controls=len(cases),
        conclusions=['Both original loops store low8bits of each u32 triangle count;256 becomes0,257 becomes1.',
                     'LOD16 loop fits header arrays;LOD17 overwrites other header fields in both static and animated layouts.'],
        limits=['Original constructor segments executed with prepared registers/memory; full constructors, allocation, loaders, draw calls and live game not executed.',
                'No full-model polygon/FPS/crash safety ceiling established. No game file writes.'])
    (ROOT/'model-count-limits-verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8',newline='\n')
    assert EXE.read_bytes()==raw
    print('PASS:',len(cases),'original constructor-loop controls; both256->0 and LOD17 field corruption reproduced')


if __name__=='__main__':main()
