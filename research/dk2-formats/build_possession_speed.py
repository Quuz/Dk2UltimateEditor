"""Publish actual type224 speed consumer, keeping scalar113/114 uncertainty."""
from pathlib import Path
import json,hashlib,struct
import dk2_formats as fmt
import emulate_possession_speed as native
import native_possession_stats as api
from build_effect_semantics import pe_read
ROOT=Path(__file__).resolve().parent
def load(n):return json.loads((ROOT/n).read_text(encoding='utf8'))
def digest(b):return hashlib.sha256(b).hexdigest()
def link(n):return dict(file=n,sha256=digest((ROOT/n).read_bytes()))
def primary():
    out={}
    for key,items in load('phase92-primary-manifest.json').items():
        out[key]=[]
        for r in items:
            p=ROOT/'ghidra'/r['memory'];v=json.loads(p.read_text());b=bytes(v['data'])
            assert int(v['address'],16)==r['address'] and len(b)==v['length']==r['length'] and b==pe_read(Path(r['exe']).read_bytes(),r['address'],r['length'])
            out[key].append(dict(r,memory_sha256=digest(p.read_bytes()),program_bytes_sha256=digest(b)))
    return out
def results():
    rows=load('phase92-possession-speed-emulations.json')['replays'];by={c['name']:c for c in native.cases()};out=[];assert len(rows)==len(by)==21
    for row in rows:
        p=ROOT/'ghidra'/row['file'];v=json.loads(p.read_text());c=by[row['case']];args,e=native.build(c);r=v['result']
        assert v['input']==args and v['expected']==e and v['case']==c and digest(p.read_bytes())==row['sha256']
        assert r['success'] and r['hit_return'] and all(int(r['registers'][k],16)==x for k,x in dict(EAX=1,EBP=1,EBX=0,ECX=0,EDX=e['speed']).items())
        value=c['percent'] if c.get('raw_index',21)==21 else 0
        assert api.speed_modifier([c['base_speed'],8192,2048][c['mode']],value,c['possessed'],c['other_flag'])==e['speed']
        out.append(dict(case=c,expected=e,evidence=row))
    return rows,out
def corpus():
    files=[];records=patches=0;aggregate=hashlib.sha256()
    for row in load('phase88-sacrifice-corpus.json')['files']:
        p=Path(row['file']);data=p.read_bytes();assert digest(data)==row['sha256'];parts=fmt.parse(data);assert b''.join(s.pack() for s in parts)==data;local=0
        for si,s in enumerate(parts):
            if s.signature!=0xe6:continue
            for ri,raw in enumerate(s.records):
                if len(raw)!=16 or int.from_bytes(raw[:4],'little')!=224:continue
                info=api.view(raw);assert info['value_bits']==int.from_bytes(raw[8:12],'little');records+=1
                for value in [0,100,200,0xffffffff]:
                    changed=api.patch_value_bits(raw,value);rr=list(s.records);rr[ri]=changed;clone=fmt.Section(s.offset,s.signature,s.header,list(s.records),s.suffix);clone.replace_records(rr)
                    packed=b''.join(clone.pack() if i==si else x.pack() for i,x in enumerate(parts));expected=bytearray(data)
                    off=s.offset+len(s.header)+sum(len(x) for x in s.records[:ri])+8;struct.pack_into('<I',expected,off,value)
                    assert packed==expected and fmt.parse(packed)[si].records==rr and changed[12:]==raw[12:];aggregate.update(hashlib.sha256(packed).digest());local+=1;patches+=1
        files.append(dict(file=str(p),sha256=row['sha256'],type224_patches=local))
    assert len(files)==557 and records>0 and patches==records*4
    return dict(files=files,source_files=557,type224_records=records,API_exact_container_patches=patches,ordered_patch_digest=aggregate.hexdigest(),scope='All accepted phase88 Variables files; all type224 records patched at rawDWORD8 only; header/order/other fields and ignoredDWORD12 preserved. Previous24 strict refusals are not diagnosed/repaired here.')
def current():
    d=load('decoded/GlobalVariables.kwd.json');data=Path(d['source']).read_bytes();assert digest(data)==d['sha256'];rows=[]
    for r in d['sections'][0]['records']:
        if r['words_u32'][0] in [98,113,114,224]:
            raw=bytes.fromhex(r['raw_hex']);assert data[r['offset']:r['offset']+16]==raw
            rows.append(dict(offset=r['offset'],raw_hex=raw.hex(),words_u32=r['words_u32']))
    return dict(file=d['source'],sha256=d['sha256'],records=rows)
def main():
    p=primary();rows,out=results();c=corpus();source=current();prior=ROOT/'phase92-prior-components.json'
    if not prior.exists():
        before=load('editor-format-package.json')['components'];assert len(before)==70;prior.write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf8')
    s=dict(phase=92,date='2026-10-28',profile=load('portal-exit-semantics.json')['profile'],primary=p,native_replays=rows,outcomes=out,corpus=c,current_source=source,
      api_helpers=[link(n) for n in ['native_possession_stats.py','emulate_possession_speed.py']],
      storage=dict(record_type=224,record_bytes=16,stat_id_dword=4,value_dword=8,ignored_dword=12,loader='51DC70',table_address='6F2863',table_words=32,
        writer='Writes low16 value to6F2863+2*fullDWORDindex modulo2^32. No bounds check and no index narrowing.',
        getter='495080 takes low8 query; if<32, reads signedWORD at[global6E4154]+313h+2*query; otherwise returns100',
        ordinary_alias='global6E4154 points to6F2550, so+313h=6F2863; fixture uses this actual alias',
        asymmetry='Native writer index32 writes6F28A3 outside table; getter32 returns100. Writer277 writes6F2A8D, getter277 narrows to21 and returns untouched0.',
        persistence='Preserve the full rawDWORD value and ignoredDWORD; load does not define a level dimension even when ignoredDWORD happens to be10.'),
      actual_speed=dict(function='48ABB0',base_modes={'0':'definitionDWORD5B4','1':'definitionDWORD5B8','2':'definitionDWORD5BC'},
        stat_query=21,all_modes_same_query=True,flags=dict(possessed='actorDWORD3E6 bit80',setter='48DEA0(1) actually executed',secondary='actorDWORD3EA bit08; broader meaning unassigned',priority='Possessed branch wins if both bits set'),
        formula='P=sign_extend16(loadedWORD21); product=low32(base_after_health_and_terrain*(100+P)); possessed→floor(unsigned product/100); secondary-only→floor(unsigned product/200); neither→unchanged base',
        current='Current source type224/StatId21/value200/ignored10 at2658h was passed unchanged to native loader; base4096→12288 after real setter and complete speed getter',
        examples={'0':4096,'100':8192,'200':12288,'FFFF':4055,'8000':41611591,'7FFF':1346232},example_base=4096,
        mode_examples='Walk4096→12288,Run8192→24576,Shuffle2048→6144 underP200. Loading StatId22/value200 with21=0 leaves all tested speed at base; Run does not query22 here.',
        other='secondary-only P200 gives6144;P0 gives2048. This is half the whole result, not merely half the added percentage.',
        before_modifier='Complete native function also applies low-health and terrain modifiers before final percent. Fixtures have HP1000/baseHP1000 and terrain flags0; their branches are not newly validated.',
        actor='Full1343+256guard source from verified phase90 native factory capture; definition speeds prepared. Setter-on changes flag80 and resets488/3EE; HP remains1000 in these complete checked actor images.'),
      scalar_status=dict(types=[98,113,114],runtime_words=['6F28E0','6F28F2','6F28F4'],test='All three prepared WORDs0,6,30000 yield the same speed undertype224/21=200 in full48ABB0',
        conclusion='These scalar values do not drive this tested speed calculation. Their global gameplay meaning and health/stat effects remain unresolved; do not infer unused globally.'),
      native_scope=dict(chain=['raw16byte type224 record','51DC70','48DEA0(1)','48ABB0','original Creature vslot88→48ED40','495080'],
        query_cases='Two additional direct495080 calls verify index32/277 behavior; all supplied data checked after native execution',
        setup='Prepared already-created actor/world data from phase90 native evidence, not another ctor/factory execution',
        source_actor=link('ghidra/phase90-creature-factory-base.bin'),providers='Existing external nullGFX/fixedSound/rangeOS-probe bytes retained but no calls observed; OUT guards and probe counter checked'),
      counts=dict(native_cases=len(out),native_replays=len(rows),whole_native_spans=len(p['functions']),file_backed_blocks=len(p['blocks']),
        source_files=c['source_files'],type224_records=c['type224_records'],API_exact_container_patches=c['API_exact_container_patches'],new_full_actor_images=0,original_game_function_or_vtable_patches=0),
      limitations=['Exact supplied DX profile only.',
        'Type224 StatId21 speed proved. Other type224 stat names, especially Health1, are not newly assigned gameplay semantics.',
        'Scalar98/113/114 unchanged-result pairs only constrain this speed path, not every indirect/copy consumer.',
        'Full Shot26→player possession/control/renderer, unpossession, health/terrain modifier branches and actual movement ticks are not composed here.',
        'Actor starts from an earlier verified native creation image; this is not fresh actor allocation or a new live-game dump.',
        'Stats index32/277 scenarios are bounded forensic writer checks, not recommended valid editor output; unknown records must still be preserved.',
        'Possession health/formulas for other stats,Objects34C,Traps204,Doors236,Terrain198/224,Creature Priority/Jump and other15-format questions keep the objective open.'])
    (ROOT/'possession-speed-semantics.json').write_text(json.dumps(s,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(s['counts']))
if __name__=='__main__':main()
