"""Publish full Terrain load validation inside the existing Terrain component."""
from pathlib import Path
from collections import Counter
import json
import dk2_formats as fmt
from build_keeper_cast import ROOT,load,digest
from build_effect_semantics import pe_read
from native_terrain_catalogue import SCALARS,COLOURS

def primary():
 out=[]
 for r in load('phase58-primary-manifest.json'):
  before=ROOT/'ghidra'/r['before'];after=ROOT/'ghidra'/r['after'];mem=ROOT/'ghidra'/r['memory'];v=json.loads(mem.read_text());b=bytes(v['data'])
  assert before.read_text().rstrip()==after.read_text().rstrip() and int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
  out.append({**r,'before_sha256':digest(before.read_bytes()),'memory_sha256':digest(mem.read_bytes()),'program_bytes_sha256':digest(b)})
 return out

def corpus():
 files=load('terrain-lighting-semantics.json')['corpus']['files'];word=Counter();dword=Counter();records=0;ids=[]
 for row in files:
  b=Path(row['file']).read_bytes();assert digest(b)==row['sha256']
  for sec in fmt.parse(b):
   if sec.signature!=110:continue
   ii=[r[0x1D6] for r in sec.records]
   ids.append(dict(file=row['file'],count=len(ii),maximum_id=max(ii),distinct_ids=len(set(ii)),zero_ids=ii.count(0),header_count=fmt.u32(sec.header,20)))
   for raw in sec.records:word[int.from_bytes(raw[0x198:0x19A],'little')]+=1;dword[int.from_bytes(raw[0x224:0x228],'little')]+=1;records+=1
 return dict(files=files,file_count=len(files),records=records,unknown198_unsigned_counts={str(k):v for k,v in sorted(word.items())},unknown224_unsigned_counts={str(k):v for k,v in sorted(dword.items())},id_profiles=ids)

def main():
 s=dict(phase=58,program='DKII-DX.exe',program_sha256=load('executable-hash-verification.json')['Dungeon Keeper 2/DKII-DX.exe'],primary_program_bytes=primary(),native_emulations=load('phase58-terrain-emulations.json')['cases'],corpus=corpus(),supplement_to='terrain-lighting-semantics.json',
 native_chain=['56F090 parser','56EB60 intermediate initializer','56ED20 converter','51EB30 header callback','51EB50 record callback','4F66E0 complete definition loader','508DD0 lookup'],
 unknown_fields=[dict(raw_offset=0x198,raw_bytes=2,intermediate=0x250,presence=0x30,runtime=0x183),dict(raw_offset=0x224,raw_bytes=4,intermediate=0x2DC,presence=0x98,runtime=0x1B5)],
 exact_bits='Both unknown fields copy all raw bits through complete native parser to definition. No clamp,sign conversion or /4096 in the game loader. This is storage validation,not gameplay signedness/meaning. Complete loader tests accept nonzero presence1/2/FFFFFFFF equally;presence0 retains prior bytes.',
 short_record='548-byte stride:parser zeros552-byte raw buffer before read;missing raw224 DWORD becomes zero and converter marks it present. Thus complete short-record loading overwrites previous224 with zero;it does not behave like an absent intermediate presence flag.',
 placement='Direct array slot460*rawID. Full tests include IDs0/22/255 and duplicate7. Lookup computes base+460*(DWORDargument&255),without checking loaded_count or slot ID. Nine lookups per32 cases;288 native calls. Tables preallocated256slots;not proof native allocator supplies256slots.',
 counts='Header callback addsN to CWorld15A8. Normal record callback increments15AC for every record,including duplicates,and adds the resulting slot checksumDWORD84 to15B0 after each load. Duplicate ID overwrites its slot but both contributions remain in insertion accumulator. Update modeA3C7!=0 uses same direct slot and changes neither15AC nor15B0.',
 special_ids='Normal callback assigns water9BAE when loaded flags80 and lava9BAF when100;last flagged record wins. A subsequent overwrite without these flags does not clear earlier assignments. Update callback returns before these assignments,so definition flags can change while both world selector bytes retain previous values.',
 checksum='If6FCDCF==0:definitionDWORD84=sum of all736 intermediate unsigned bytes. Otherwise previous DWORD84 survives. Not hash of552-byte raw record. Native collisionpair508/01000000 versus763/00010000 changes both unknown runtime values while producing equal checksum and world accumulator. Checksum equality cannot attest content equality.',
 constructor='Full4F6570→13real504D60 resource initializers tested separately in all32 compositions. OnA5 input,constructor clears unknown183/1B5 tozero,but leaves checksum84DWORD untouched;1CB preserves upper6bits and clears low2. This does not assert native allocation initially containsA5.',
 projections=dict(scalars=[dict(raw=a,intermediate=b,presence=p,runtime=d,bytes=n,runtime_bytes=2 if d==0x17F else n) for a,b,p,d,n in SCALARS],colours=[dict(raw=a,intermediate=b,presence=p,runtime=d,extension_mask=m) for a,b,p,d,m in COLOURS],api='native_terrain_catalogue.py',operations=['intermediate','definition','lookup_address','patch_unknown']),
 search=dict(evidence='phase58-adjusted-unknown-leads.json',sha256=digest((ROOT/'phase58-adjusted-unknown-leads.json').read_bytes()),mapped_spans=8729,seeds=24722,local_leads=38,forward_leads=303,status='Unclassified lead inventory only;no gameplay name assigned from displacement matches. Includes unrelated GUI/mesh/list/anger loop matches. Summaries overapproximate numeric loops and omit indirect/dynamic/stack-pointee/return-alias cases.'),
 limitations=['ExactDX only;not proofRC3 or local13 gameplay equality.','24complete parser→callback→loader cases plus8complete direct partial loader cases. All32alsoexecute native constructor and9getter calls.','Native graphics provider null,resource descriptors absent,special flags1000/2000 excluded. Real renderer/model loading not executed.','Only5B3EB0 reads and virtual sound handle provider are fixtures. Sound provider returnsA100+callindex;actual audio engine not executed. ID22 extra provider path exercised.','Compare entire256×460 definition array,A3CC world,736intermediate,16control,460constructor buffer,input stream and288lookup addresses. FinalEAX/EBP are observer equality results,not loader returns.','Original first diagnostic failed because an inherited read fixture used an old cursor address. Onlyexplicit finalv2 files listed in manifest count as validation;diagnostic/failing artifacts excluded.','Terrain198/224,Traps204,Objects34C gameplay purposes remain unresolved. Negative lead searches do not prove unused fields.','No game/EXE/parallel-editor writes or changed-data game runs.'])
 assert len(s['native_emulations'])==32 and len(s['primary_program_bytes'])==11
 (ROOT/'terrain-catalogue-validation.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8')
 t=load('terrain-lighting-semantics.json');t['phase58_complete_loading']=s;(ROOT/'terrain-lighting-semantics.json').write_text(json.dumps(t,ensure_ascii=False,indent=2),encoding='utf8')
 print('Terrain full loading:32native;',s['corpus']['file_count'],'files;',s['corpus']['records'],'records;11PEbodies')
if __name__=='__main__':main()
