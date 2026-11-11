"""Attest original instruction ranges reached in Variables/temple composition."""
import re
from capstone import Cs,CS_ARCH_X86,CS_MODE_32
from build_effect_semantics import pe_read
from audit_targeted_spell import executable_ranges
import emulate_temple_file_rewards as n
ROOT,load,dump,sha=n.ROOT,n.load,n.dump,n.sha

def main():
 blob=n.EXE.read_bytes();ranges=executable_ranges(blob);pcs=set();known=[]
 for row in load('phase202-temple-file-emulations.json')['replays']:
  assert sha((ROOT/row['file']).read_bytes())==row['sha256'];d=load(row['file'])
  for token in d['result']['visited_instructions']:
   a=int(token,16)
   if any(p<=a<p+s for p,s in ranges):pcs.add(a)
   else:assert any(p<=a<p+s for p,s in d['input']['allowed_external_IO_code'])
 for name in ['phase201-speed-context-primary.json','phase200-global-index-primary.json','phase197-Variables-primary.json','phase198-reference-primary.json','phase135-temple-object-primary.json','phase136-temple-creature-primary.json']:
  known+=load(name)['functions']
 sacrifice=load('sacrifice-semantics.json')
 known+=sacrifice['primary_program_bytes']['functions']+sacrifice['phase89_object_rewards']['primary']['functions']
 queries=[];fresh=[]
 for info in (ROOT/'ghidra').glob('phase202-PC-*-info.txt'):
  m=re.search(r'Body: ([0-9a-fA-F]+) - ([0-9a-fA-F]+)',info.read_text());assert m,info
  a,end=[int(v,16) for v in m.groups()];fresh.append((a,end));mp=info.with_name(info.name.replace('-info','-memory'));ap=info.with_name(info.name.replace('-info','-assembly'))
  if mp.exists() and ap.exists():known.append(dict(address=a,length=end-a+1,memory=str(mp.relative_to(ROOT)),assembly=str(ap.relative_to(ROOT))))
  else:
   for tool,path in [('read_memory',mp),('disassemble_function',ap)]:
    if not path.exists():queries.append(dict(id=path.stem,tool=tool,args=dict(program='DKII-DX.exe',address=hex(a),**(dict(length=end-a+1) if tool=='read_memory' else {})),print_limit=0))
 md=Cs(CS_ARCH_X86,CS_MODE_32);covered=set();rows=[];seen=set()
 for r in known:
  a,size=r['address'],r['length']
  if (a,size) in seen or not any(a<=pc<a+size for pc in pcs):continue
  seen.add((a,size));paths={k:(ROOT/r[k] if (ROOT/r[k]).exists() else ROOT/'ghidra'/r[k]) for k in ['memory','assembly']}
  j=__import__('json').loads(paths['memory'].read_text(encoding='utf8'));raw=bytes(j['data']) if 'data' in j else bytes.fromhex(j['hex'])
  assert len(raw)==size and raw==pe_read(blob,a,size),(hex(a),paths['memory'])
  for k in paths:
   if r.get(k+'_sha256'):assert sha(paths[k].read_bytes())==r[k+'_sha256']
  starts=set(int(x,16) for x in re.findall(r'^([0-9a-fA-F]{8}):',paths['assembly'].read_text(),re.M))
  for pc in list(starts):
   ins=next(md.disasm(pe_read(blob,pc,15),pc,count=1))
   if ins.mnemonic=='wait':starts.add(pc+ins.size)
  covered.update(starts);rows.append(dict(address=a,length=size,bytes_sha256=sha(raw),**{k:str(p.relative_to(ROOT)) for k,p in paths.items()},**{k+'_sha256':sha(p.read_bytes()) for k,p in paths.items()}))
 missing=pcs-covered;clusters=[]
 for a in sorted(missing):
  if not clusters or a-clusters[-1][-1]>128:clusters.append([a])
  else:clusters[-1].append(a)
 for c in clusters:
  if not any(a<=c[0]<=b for a,b in fresh):queries.append(dict(id=f'phase202-PC-{c[0]:x}-info',tool='get_function_by_address',args=dict(program='DKII-DX.exe',address=hex(c[0])),print_limit=0))
 if queries:dump('phase202-temple-primary-queries.json',queries);print('Pending',len(queries),'missing PCs',len(missing));return
 assert not missing,[hex(x) for x in sorted(missing)[:20]]
 dump('phase202-temple-file-primary.json',dict(profile=dict(file=str(n.EXE),sha256=sha(blob)),functions=rows,unique_executed_original_PCs=len(pcs),all_executed_original_PCs_covered=True,scope='Whole byte extents for all native PCs; not complete branch coverage. Actual file reader, callbacks, spell entry, matcher, dispatcher and selected gold/Object reward paths.'))
 print('PASS primary',len(pcs),'PCs',len(rows),'function extents')
if __name__=='__main__':main()
