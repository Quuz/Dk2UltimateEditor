"""Pinned-source review of Dk2maptool; does not execute or change that tool."""
import hashlib
import json
import re
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent
REF = ROOT/'source-references'/'dk2maptool-d38fd0dc333d'
COMMIT = 'd38fd0dc333d70ef4edc5412d7ffc98179fee6a4'


def strip_comments(s):
    return re.sub(r'/\*.*?\*/|//[^\n]*', '', s, flags=re.S)


def function_body(source, name):
    m = re.search(r'^short\s+'+re.escape(name)+r'\([^;]*?\)\s*\{',source,flags=re.M)
    assert m, name
    start = source.index('{',m.start())
    depth = 1
    end = start+1
    while depth:
        if source[end] == '{':depth += 1
        if source[end] == '}':depth -= 1
        end += 1
    return source.count('\n',0,m.start())+1,source[m.start():end]


def main():
    provenance = json.loads((REF/'provenance.json').read_text(encoding='utf-8'))
    assert provenance['commit_from_zip_comment'] == COMMIT
    archive = (REF/'download.zip').read_bytes()
    assert hashlib.sha256(archive).hexdigest() == provenance['zip_sha256']
    with zipfile.ZipFile(REF/'download.zip') as z:
        assert z.comment.decode('ascii') == COMMIT
        for x in provenance['manifest']:
            b = (REF/'source'/x['path']).read_bytes()
            assert hashlib.sha256(b).hexdigest() == x['sha256']
            assert z.read('Dk2maptool-master/'+x['path']) == b
    assert len(provenance['manifest']) == 9
    source = (REF/'source'/'dk2mfile.c').read_bytes().decode('cp1251')
    header = (REF/'source'/'dk2mfile.h').read_bytes().decode('ascii')
    package_bytes = (ROOT/'editor-format-package.json').read_bytes()
    package = json.loads(package_bytes)
    handlers = {}
    for family, suffix in {
        'Creatures':'creatures', 'Rooms':'rooms', 'Traps':'traps', 'Doors':'doors',
        'Shots':'shots', 'Objects':'objects', 'CreatureSpells':'ctspells',
        'KeeperSpells':'kpspells', 'Terrain':'terrain', 'Variables':'variables',
        'Globals':'globals', 'Things':'things', 'Players':'players', 'Triggers':'triggers',
    }.items():
        name = 'dk2m_read_'+suffix+'chunk'
        line,body = function_body(source,name)
        live = strip_comments(body)
        calls = re.findall(r'\b(dk2m_\w+|read_int\w+|memcpy)\s*\(',live)
        assert not any(x.startswith('read_int') or x == 'memcpy' for x in calls), name
        assert not any(x.startswith('dk2m_read_') and x != name for x in calls), name
        assert not re.search(r'lvl\s*->|chunk\s*->\s*data',live), name
        handlers[family] = {'function':name,'line':line,
                            'body_sha256':hashlib.sha256(body.encode('cp1251')).hexdigest(),
                            'decodes_record_fields':False}
    assert 'FTKWD_EFFECTS' not in header and 'FTKWD_EFFECTELEMENTS' not in header
    assert 'tile->u1=read_int8_buf(chunk->data+offs+3);' in source
    assert 'unsigned char u1;' in header
    assert 'This block contains random trash' in header
    assert 'lvl->props.u11=read_int8_buf(chunk->data+offs);' in source
    regions=[]
    offset=0
    def add(name,size,line):
        nonlocal offset
        current=[{'name':f['name'],'offset':f['offset'],'size':f['size']}
                 for f in package['level_layouts']['Level_25683']['fields']
                 if offset <= f['offset'] < offset+size]
        regions.append({'source_name':name,'offset':offset,'offset_hex':hex(offset),
                        'bytes':size,'source_line':line,'current_fields':current})
        offset += size
    for name,size,line in [
        ('name',128,807),('desc',2048,811),('author',128,815),('email',128,819),
        ('info',2048,823),('u3',2,827),('ticks_per_sec',2,829),('randblock',520,832),
        ('msgs[20]',20480,834),('lvflags',2,840),('speechstr',32,842),
        ('talisman_pieces',1,846),('reward_prev[0..3]',4,848),('reward_next[0..3]',4,851),
        ('sound_track',1,854),('text_table_id',1,856),('text_title_id',2,858),
        ('text_plot_id',2,860),('text_debrief_id',2,862),('text_objectv_id',2,864),
        ('u10',2,866),('text_subobjctv_id[3]',6,868),('speclvl_idx',1,871),
        ('u11',1,873),('text_intrdc_overrd_obj[8]',8,875),('text_intrdc_overrd_id[8]',16,878),
        ('terrain_path',32,881),('one_shot_horny_lev',1,885),('u20',1,887),
        ('reward_prev[4]',1,889),('reward_next[4]',1,891),('speech_horny_id',2,893),
        ('speech_prelvl_id',2,895),('speech_postlvl_win',2,897),('speech_postlvl_lost',2,899),
        ('speech_postlvl_news',2,901),('speech_prelvl_genr',2,903),('hero_name',64,905),
    ]:add(name,size,line)
    assert offset == 25683
    lookup={x['source_name']:x for x in regions}
    assert lookup['randblock']['offset'] == 4484
    assert lookup['u10']['offset'] == 25537
    assert lookup['u11']['offset'] == 25546
    assert lookup['u20']['offset'] == 25604
    assert lookup['u20']['current_fields'][0]['name'] == 'PlayerCount'
    assert lookup['u11']['current_fields'][0]['name'] == 'UniqueCreatureTypeId'
    assert 'read_chunk(lvl,&chunk,flags);' in source
    assert 'snprintf(fname, sizeof(fname), "%s.kwd", name);' in source
    assert 'lvl->unkarr01[k]=read_int32_le_buf(chunk->data+offs+(k<<1));' in source
    assert len(package['components']) == 143
    result = {
        'date':'2026-11-01','repository':provenance['repository'],'commit':COMMIT,
        'provenance':'source-references/dk2maptool-d38fd0dc333d/provenance.json',
        'validation':{'passed':True,'verified_text_files':9,
                      'catalogue_and_other_stub_handlers':len(handlers),
                      'descriptor_regions':len(regions),'modern_payload_bytes':offset,
                      'new_gameplay_field_assignments':0,'new_native_executions':0},
        'stub_handlers':handlers,'level_descriptor_comparison':regions,
        'map_cell':{'source_lines':[1012,1013,1014,1015],
                    'fields':['slab','owner','wlb','u1'],
                    'fourth_byte_semantics':'Still u1; no new interpretation.'},
        'open_blocks':{'temple':'No recipe or gameplay decoder',
                       'crypt':'No bonus implementation',
                       'resistances':'No resistance decoder or consumer',
                       'level_opaque_520':'randblock comment; TODO remains, no independent runtime proof',
                       'level_opaque_2':'u10 with speculative comment; not semantically decoded',
                       'level_unique_byte':'u11 remains unnamed'},
        'source_limitations':[{'line':1395,'issue':'read_chunk return value is ignored; top-level success does not prove fields decoded.'},
                              {'line':1429,'issue':'sizeof(fname) is pointer width, not allocated buffer capacity.'},
                              {'line':794,'issue':'Reads32bits at16bit stride intoWORD array; last element can overread nominal chunk.'},
                              {'line':910,'issue':'Descriptor reader requires exactly25683; does not support old25603 here.'}],
        'package_preserved':{'sha256':hashlib.sha256(package_bytes).hexdigest(),'components':143},
        'scope':'Source review and comparison only. No game, original EXE, tool binary, or parallel editor executed or modified.'}
    (ROOT/'dk2maptool-source-review.json').write_text(
        json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    assert hashlib.sha256((ROOT/'editor-format-package.json').read_bytes()).hexdigest() == result['package_preserved']['sha256']
    print(json.dumps(result['validation'],ensure_ascii=False))


if __name__ == '__main__':
    main()
