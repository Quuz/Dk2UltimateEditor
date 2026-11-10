"""Raw stream oracle, complete callback captures, World invariants and pairs."""
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
import json,struct
import emulate_native_map_reader as n
import emulate_map_room_chain as old
from emulate_resistance import post_context
from native_map_cells import intermediate_cell
import native_room_adjacency as adjacency
from build_effect_semantics import EXE,pe_read

def u(b,o=0,size=4):return int.from_bytes(b[o:o+size],'little')

def expected_file(c):
    data=n.raw_file(c);pos=12;logs=[];buffer=bytearray([165])*n.BUFFER_BYTES;records=bytearray()
    def read(requested,dest):
        nonlocal pos
        actual=min(requested,max(0,len(data)-pos));logs.append((pos,dest,requested,actual,1))
        out=data[pos:pos+actual];pos+=actual;return out
    read(16,0x7FFEFFF0)
    accepted=c['header_tag']==101
    if accepted:
        read(8,0x7FFEFFD8);accepted=c['data_tag']==102
    if accepted and c['callback']:
        count=c['width']*c['height'];i=0
        while i<count:
            take=min(16384,count-i);chunk=read(4*take,n.BUFFER);buffer[:len(chunk)]=chunk
            for j in range(take):
                index=i+j;records+=struct.pack('<II',index%c['width'],index//c['width'])+intermediate_cell(buffer[4*j:4*j+4])
            i+=take
    seek=accepted and not c['callback']
    if seek:pos+=4*c['width']*c['height'] if c['declared'] is None else c['declared']
    return dict(accepted=accepted,pos=pos,logs=logs,buffer=bytes(buffer)+bytes([165])*64,records=bytes(records),seek=bool(seek))

def verify(c,args,checks,r):
    post=post_context(checks,r);e=expected_file(c)
    assert int(r['registers']['EAX'],16)==int(e['accepted'])
    assert post[n.DATA]==checks[n.DATA] and post[n.REF]==checks[n.REF] and post[n.CFG]==checks[n.CFG]
    assert post[n.FILE]==struct.pack('<II',0x671F80,n.STREAM)+bytes([165])*64
    stream=bytearray(checks[n.STREAM]);struct.pack_into('<I',stream,8,1);struct.pack_into('<I',stream,16,e['pos']);assert post[n.STREAM]==stream
    wanted_state=struct.pack('<5I',e['pos'],len(e['logs']),sum(x[2]!=x[3] for x in e['logs']),0,int(e['seek']))
    assert post[n.STATE]==wanted_state
    log=b''.join(struct.pack('<5I',*row) for row in e['logs'])+bytes([165])*(1280-20*len(e['logs']));assert post[n.LOG]==log
    assert post[n.BUFFER]==e['buffer']
    assert all(row['returned'] for row in r['before_calls']) and len(r['before_calls'])==(52 if c['profile']=='world' else 2)
    assert r['before_calls'][-2]['return_EAX']==hex(n.FILE) and r['before_calls'][-1]['return_EAX']=='0x0'
    pcs={int(a,16) for a in r['visited_instructions']}
    assert r['visited_instructions'].get('0x5b3eb0')==len(e['logs'])
    assert r['visited_instructions'].get('0x5fffb0')==len(e['logs'])
    assert (0x600030 in pcs)==e['seek']
    world_out=None
    if c['profile']=='capture':
        records=e['records'] if c['callback'] else b''
        assert post[n.CAPTURE]==records+bytes([165])*(len(checks[n.CAPTURE])-len(records))
        assert u(post[n.CAPSTATE])==len(records)//24
        assert 0x51EC60 not in pcs
    else:
        p=n.world.old.prior;points={tuple(pt) for pt in c['points']};cells=post[p.MAP];room=post[p.ROOM]
        assert post[n.CAPTURE]==checks[n.CAPTURE] and post[n.CAPSTATE]==checks[n.CAPSTATE]
        assert r['visited_instructions'].get('0x51ec60')==25
        assert u(post[p.WORLD],0x9D0)==1 and u(room,0xA7,2)==len(points)
        assert u(room,0x35)==10000*len(points) and room[0x18]==13 and u(room,0x1D,2)==2
        nav=bytearray(25)
        for y in range(5):
            for x in range(5):
                at=16*(y*5+x);terrain=1 if x in [0,4] or y in [0,4] else 22 if (x,y) in points else 3
                assert cells[at+10]==terrain and u(cells,at+8,2)&0xFFF==(2 if terrain==22 else 8)
                assert u(cells,at+2,2)&0xFFF==(1000 if terrain==22 else 0)
                raw=checks[n.WALLRAW] if terrain==1 else checks[n.world.old.TERRAINRAW] if terrain==22 else checks[n.world.TERRAW2]
                assert u(cells,at+6,2)==u(raw,0x1D7,2)
                if terrain==22:assert cells[at+12]==adjacency.construction7_mask(cells,5,5,x,y,22)
                assert bool(u(cells,at+8,2)&0x1000)==bool(terrain==22 and c['flags']&2)
                nav[5*y+x]=8 if terrain==1 else 4 if terrain==22 and cells[at+12]==255 else 0
        assert post[n.world.old.BITS]==nav
        assert u(post[p.WORLD],0x72E7)==sum(v if v<128 else v-256 for i in range(0,len(e['records']),24) for v in e['records'][i+8:i+24])&0xFFFFFFFF
        assert u(post[n.world.PLAYERS],0x1125+0xB3F)==len(points)
        assert u(post[n.world.PLAYERS],7*0x1125+0xB3F)==25-len(points)
        objects=old.pool_objects(post)
        assert objects and all(o['ObjectId']==111 for o in objects)
        assert post[n.world.BLOCK][16408:]==bytes([165])*64
        assert post[p.TRIANGLES][1454:]==bytes([165])*64
        world_out=dict(native_Map_callbacks=25,room_tiles=len(points),live_objects=len(objects),center_mask=cells[16*12+12],
            NAV=list(nav),map_sum=u(post[p.WORLD],0x72E7),native_remesh_calls=r['visited_instructions'].get('0x4ce770',0),
            raw_owner=2,nonownable_ground_and_rock_runtime_owner=8,arena_fully_accounted=True)
    md=Cs(CS_ARCH_X86,CS_MODE_32);md.detail=True;blob=EXE.read_bytes()
    for row in r['observed_reads']+r['observed_writes']:
        if int(row['address'],16)>=4096:continue
        ins=next(md.disasm(pe_read(blob,int(row['pc'],16),15),int(row['pc'],16)))
        assert int(row['address'],16)==0 and row['size']==4 and 0x64 in ins.prefix and 'fs:[0]' in ins.op_str
    return post,dict(returned=int(e['accepted']),profile=c['profile'],read_calls=len(e['logs']),actual_short_reads=sum(x[2]!=x[3] for x in e['logs']),
        read_log=[dict(offset=a,destination=hex(b),requested=q,actual=z,ReadFile_BOOL=k) for a,b,q,z,k in e['logs']],
        callback_captures=u(post[n.CAPSTATE]),file_position=e['pos'],seek_performed=e['seek'],
        capture_all_records_match_oracle=c['profile']=='capture',last_capture_hex=e['records'][-24:].hex() if e['records'] else None,
        whole_regions=len(post),whole_bytes=sum(map(len,post.values())),world=world_out,steps=r['instruction_count'])

def pair(name,a,b):
    if name in ['world-fourthFF','chunk-fourthFF','source-AutoSaveMap-opaque-edit']:
        expected={x:(b[x] if x in [n.DATA,n.BUFFER] else v) for x,v in a.items()}
    elif name=='world-declared0':expected={x:(b[x] if x==n.DATA else v) for x,v in a.items()}
    elif name=='world-flags2':
        expected={x:bytearray(v) for x,v in a.items()};expected[n.DATA][:]=b[n.DATA];expected[n.BUFFER][:]=b[n.BUFFER]
        p=n.world.old.prior
        for y in range(1,4):
            for x in range(1,4):expected[p.MAP][16*(y*5+x)+9]|=0x10
        n.world.put(expected[p.WORLD],0x72E7,357);expected={x:bytes(v) for x,v in expected.items()}
    else:raise ValueError(name)
    assert b==expected,[(hex(x),[hex(i) for i,(s,t) in enumerate(zip(b[x],expected[x])) if s!=t][:12]) for x in b if b[x]!=expected[x]]
    return dict(case=name,whole_regions=len(a),whole_bytes=sum(map(len,a.values())),all_other_prepared_bytes_equal=True)

def main():
    rows=[];posts={};pairs=[]
    for c in n.cases():
        args,checks,saved,path=n.execute(c);post,out=verify(c,args,checks,saved['result']);posts[c['name']]=post
        rows.append(dict(case=c['name'],file=str(path.relative_to(n.ROOT)),sha256=n.sha(path.read_bytes()),outcome=out))
        if c['name'] in ['world-fourthFF','world-declared0','world-flags2']:pairs.append(pair(c['name'],posts['world-square'],post))
        if c['name']=='chunk-fourthFF':pairs.append(pair(c['name'],posts['chunk-crossing'],post))
        if c['name']=='source-AutoSaveMap-opaque-edit':pairs.append(pair(c['name'],posts['source-AutoSaveMap'],post))
        print(c['name'],out['returned'],out['actual_short_reads'],out['callback_captures'],flush=True)
    value=dict(phase=139,scope=__doc__,replays=rows,paired_controls=pairs)
    (n.ROOT/'phase139-native-map-reader-emulations.json').write_text(json.dumps(value,indent=2),encoding='utf8')

if __name__=='__main__':main()
