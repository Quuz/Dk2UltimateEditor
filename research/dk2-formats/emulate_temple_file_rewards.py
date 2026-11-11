"""Actual Variables reader -> complete spell sacrifice -> native gold/Object reward.

Extends separate phases88/89 at their missing file/entry composition boundary.
Factory runtime definitions, room/grid, heap and external sound/GFX are prepared
inputs. This is not a whole level load or a live physical sacrifice simulation.
"""
from pathlib import Path
import json,struct,sys,hashlib
import emulate_sacrifice_objects as prior
import emulate_variables_reader as files
import native_unicorn_mapreader as cpu
from native_sacrifice import raw_recipe,runtime_recipe,history_append,match
from emulate_element_colors import region
from emulate_resistance import post_context
from build_effect_semantics import EXE
ROOT=Path(__file__).resolve().parent
f=prior.f
ACTION,STOP,VARS=0x19000000,0x1F900000,0x6F2550
load=lambda n:json.loads((ROOT/n).read_text(encoding='utf8'))
sha=lambda b:hashlib.sha256(b).hexdigest()
def dump(n,d):(ROOT/n).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def put(b,o,v,n=4):b[o:o+n]=(v&((1<<(8*n))-1)).to_bytes(n,'little')

def cases():
 base=dict(value=5000,kind=3,x=5,y=6,gate=4094,empty=False,append=True,room=True)
 return [dict(base,**v,name=n) for n,v in [
 ('gold5000',{}),('gold1',dict(value=1)),('gold0',dict(value=0)),
 ('gold-negative',dict(value=0xffffffff)),('gold-x0-credit',dict(x=0)),('gold-y0-credit',dict(y=0)),
 ('gold-gate-zero',dict(gate=0)),('gold-pool-empty',dict(empty=True)),
 ('source44-no-spell-recipe',dict(append=False)),('missing-room',dict(room=False)),
 ('object17',dict(kind=2,value=17)),('object-high-bits',dict(kind=2,value=0xFEDCBA11)),
 ('first-recipe-wins',dict(override=1)),('last-nonmatching-kept',dict(trailing_unmatched=True)),('whole-source-first-section',dict(whole_source=True,append=False)),('whole-source-plus-gold',dict(whole_source=True))]]

def build(c):
 old=next(x for x in prior.cases() if x['name']=='gold5000').copy()
 old.update(value=c['value'],kind=c['kind'],ident=1 if c['kind']==3 else c['value']&255,
            room_x=c['x'],room_y=c['y'],gate=c['gate'],empty=c['empty'])
 args,_,_=prior.build(old,0)
 mem={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(args['memory'])['regions']
      if int(r['address'],16)<0x11000000 and int(r['address'],16)!=f.OBS}
 mem.pop(0x7FFEF000,None)
 # Expand the explicit World object without overlapping old provider buffers.
 relocation={f.SESSION:0x1A000000,f.GFX:0x1A010000,f.GV:0x1A020000,f.SOUND:0x1A030000,f.SV:0x1A040000}
 for old,new in relocation.items():mem[new]=mem.pop(old)
 put(mem[f.WORLD],4,relocation[f.SESSION]);put(mem[relocation[f.SESSION]],0x10,relocation[f.GFX])
 put(mem[relocation[f.GFX]],0,relocation[f.GV]);put(mem[relocation[f.SOUND]],0,relocation[f.SV])
 put(mem[0x75B468],0,relocation[f.SOUND]);mem[f.WORLD].extend(bytes(0xA3D0-len(mem[f.WORLD])))
 mem[0x7FFEE000]=bytearray([165])*0x4000
 # Register a real room in the existing factory World's prepared map.
 put(mem[0x6EE540],200*4,prior.ROOM);mem[0x6ED540][200]=1
 off=(c['y']*32+c['x'])*16;pool=off+16
 for cell in [off,pool]:put(mem[f.MAP],cell+2,200 if c['room'] else 0,2)
 mem[f.MAP][off+4:off+6]=bytes([c['x']+1,c['y']]);mem[f.MAP][off+12]=0
 mem[f.MAP][pool+4:pool+6]=bytes(2);mem[f.MAP][pool+12]=255
 put(mem[prior.ROOM],0xA7,2,2)
 mem[prior.ROOM][0x5B:0x5E]=bytes([255,0,0]);put(mem[prior.ROOM],4,200,2)
 mem[ACTION]=bytearray(struct.pack('<4I',9,((c['x']+1)<<12)+0x800,(c['y']<<12)+0x800,0))
 mem[VARS]=bytearray(0x1000);put(mem[VARS],0x40,20);mem[0x6E4154]=bytearray(struct.pack('<I',VARS))
 table,registration=files.registration();mem[f.WORLD][0x1F24:0x1F24+len(table)]=table
 source=load('temple-recipes-current.json');raws=[bytes.fromhex(r['raw_hex']) for r in source['recipes']]
 if 'override' in c:raws.insert(0,raw_recipe([(3,9)],3,c['override']))
 if c['append']:raws.append(raw_recipe([(3,9)],c['kind'],c['value']))
 if c.get('trailing_unmatched'):raws.append(raw_recipe([(3,10)],3,0x12345678))
 records=[list(struct.unpack('<4I',r)) for r in raws]
 if c.get('whole_source'):
  common=files.GLOBAL.read_bytes();n=int.from_bytes(common[20:24],'little')
  original=[common[56+16*i:72+16*i] for i in range(n)]
  assert [r for r in original if int.from_bytes(r[:4],'little')==43]==[bytes.fromhex(r['raw_hex']) for r in source['recipes']]
  allrows=original+([raw_recipe([(3,9)],c['kind'],c['value'])] if c['append'] else [])
  records=[list(struct.unpack('<4I',r)) for r in allrows]
  assert not any(c.get(k) for k in ['override','trailing_unmatched'])
 fc=dict(files.cases()[0],name=c['name'],source=None,records=records,profile='capture')
 if c.get('whole_source'):
  import append_temple_recipe as append
  expected=append.append(files.GLOBAL.read_bytes(),raw_recipe([(3,9)],c['kind'],c['value'])) if c['append'] else files.GLOBAL.read_bytes()
  assert files.raw_file(fc)==expected[:int.from_bytes(expected[8:12],'little')]
 fa,_=files.build(fc);layer={int(r['address'],16):bytearray.fromhex(r['hex']) for r in json.loads(fa['memory'])['regions']}
 layer.pop(0x7FFEE000);assert not set(mem)&set(layer)
 mem.update(layer);put(mem[files.CFG],0xD8,0x521790);put(mem[files.CFG],0xDC,f.WORLD)
 # Preserve the original PE import target for IsBadReadPtr; this is the existing
 # declared external provider, not a substitute for a game function.
 ext=[[a,len(mem[a])] for a in [f.GFX_CB,f.SOUND_CB,prior.probe.ADDRESS] if a in mem]+fa['allowed_external_IO_code']
 args.update(address='0x5133a0',registers=json.dumps(dict(ECX=hex(f.WORLD),DF='0')),stack_args=[ACTION],
  before_calls=fa['before_calls']+[dict(address='0x56cbd0',registers=dict(ECX='0'),stack_args=[files.REF,files.CFG])],
  memory=json.dumps(dict(regions=[region(a,b) for a,b in mem.items()])),max_steps=2000000,
  allowed_external_IO_code=ext,observe_reads=[[ACTION,16],[f.WORLD+0x2E70,34*len(raws)],[prior.ROOM,0xC0]],
  observe_writes=[[prior.ROOM,0xC0],[prior.ROOMOWNER,0x1200],[f.BLOCK,768],[f.WORLD,0x8000]])
 excluded={0x7FFEE000}|{a for a,_ in ext}
 checks={a:bytes(b) for a,b in mem.items() if a not in excluded}
 spans=sorted((a,len(b)) for a,b in mem.items());assert all(a+n<=b for (a,n),(b,_) in zip(spans,spans[1:])),[(hex(a),n,hex(b)) for (a,n),(b,_) in zip(spans,spans[1:]) if a+n>b]
 return args,checks,dict(raws=[r.hex() for r in raws],records=records,raw_file=files.raw_file(fc).hex(),registration=registration,
  source_recipe_sha256=source['sha256'],factory_definition_scope='Prepared285-byte runtime definition from phase89; not a fresh raw Object load.')

def execute(c):
 args,ch,meta=build(c);tag=sha((json.dumps(args,sort_keys=True)+sha(Path(cpu.__file__).read_bytes())).encode())[:12]
 file=ROOT/'ghidra'/f'phase202-temple-file-native-{c["name"]}-{tag}.json'
 if file.exists():saved=load(str(file.relative_to(ROOT)));assert saved['input']==args
 else:saved=dict(case=c,input=args,result=cpu.run(EXE,args,list(ch.items()),STOP,f.BLOCK,768));dump(str(file.relative_to(ROOT)),saved)
 return args,ch,meta,saved,file

def verify(c,args,ch,meta,r):
 assert r['success'] and r['hit_return'] and not r['emulation_error'],(c['name'],r['emulation_error'],r['trace_tail'])
 assert not r['protected_write_or_execution_failures'] and r['function_or_vtable_substitutions']==0
 assert len(r['before_calls'])==3 and all(x['returned'] for x in r['before_calls'])
 assert int(r['before_calls'][-1]['return_EAX'],0)==1
 post=post_context(ch,r);u=lambda a,o=0,n=4:int.from_bytes(post[a][o:o+n],'little')
 raws=[bytes.fromhex(x) for x in meta['raws']];history=history_append(ch[prior.ROOM],(3,9,0)) if c['room'] else ch[prior.ROOM]
 selected=next((i for i,raw in enumerate(raws) if match(history,runtime_recipe(raw))['matched']),None) if c['room'] else None
 reward=None if selected is None else struct.unpack('<3I',match(history,runtime_recipe(raws[selected]))['output'])
 created=reward is not None and (reward[0]==2 or (reward[0]==3 and 0<reward[1]<0x80000000 and c['x'] and c['y'])) and bool(c['gate']) and not c['empty']
 assert u(0x6EE540,400)==(f.BLOCK+116 if created else 0),(c['name'],hex(u(0x6EE540,400)),created)
 assert bool(post[0x6ED540][100])==created
 assert u(f.WORLD,0x731B,1)==len(raws)
 for i,raw in enumerate(raws):assert post[f.WORLD][0x2E70+34*i:0x2E70+34*(i+1)]==runtime_recipe(raw)
 assert post[f.WORLD][0x1F24:0x1F24+files.TABLE_BYTES]==ch[f.WORLD][0x1F24:0x1F24+files.TABLE_BYTES]
 checksum=sum((b if b<128 else b-256) for row in meta['records'] for b in struct.pack('<4I',1,1,1,1)+struct.pack('<4I',*row))
 assert u(f.WORLD,0x72E7)==checksum&0xffffffff
 if selected is not None:history=bytearray(history);history[0x5B:0x5E]=bytes([255,0,0])
 assert post[prior.ROOM]==bytes(history),(c['name'],'history')
 credit=reward[1] if reward is not None and reward[0]==3 and 0<reward[1]<0x80000000 and (not c['x'] or not c['y']) else 0
 assert u(prior.ROOMOWNER,0x7E)==credit
 assert post[files.DATA]==ch[files.DATA] and post[ACTION]==ch[ACTION]
 assert struct.unpack('<5I',post[files.STATE])[0:4]==(len(bytes.fromhex(meta['raw_file'])),3+len(meta['records']),0,0)
 if created:
  node=post[f.BLOCK][116:116+393];assert int.from_bytes(node[:4],'little')==0x66D654
  assert int.from_bytes(node[0x28:0x2A],'little')==7
  assert node[0x12C]==(1 if reward[0]==3 else reward[1]&255)
  if reward[0]==3:assert int.from_bytes(node[0x14A:0x14E],'little')==reward[1]
  assert list(struct.unpack_from('<3I',node,0x1A))==[(c['x']<<12)+0x800,(c['y']<<12)+0x800,0x1000]
 pcs=r['visited_instructions'];assert pcs.get('0x5133a0')==1 and pcs.get('0x51db30')==len(raws)
 assert bool(pcs.get('0x4f1780'))==(selected is not None)
 return post,dict(selected_recipe=selected,reward=None if reward is None else list(reward),object_created=bool(created),
   object_owner=7 if created else None,room_owner=8,direct_gold_credit=credit,source_recipes=44,loaded_recipes=len(raws),file_records=len(meta['records']),
   history_cleared=selected is not None,native_steps=r['instruction_count'])

def main():
 rows=[]
 for c in cases():
  if len(sys.argv)>1 and c['name'] not in sys.argv[1:]:continue
  args,ch,meta,saved,file=execute(c);_,out=verify(c,args,ch,meta,saved['result'])
  rows.append(dict(case=c,file=str(file.relative_to(ROOT)),sha256=sha(file.read_bytes()),outcome=out));print(c['name'],out,flush=True)
 if len(rows)==len(cases()):dump('phase202-temple-file-emulations.json',dict(scope=__doc__,replays=rows))
if __name__=='__main__':main()
