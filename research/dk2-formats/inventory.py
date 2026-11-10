import hashlib
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parent
DATA = ROOT.parent.parent / 'Dungeon Keeper 2/Data/editor'
FILES = ['Creatures.kwd', 'Objects.kwd', 'EffectElements.kwd', 'Effects.kwd', 'Rooms.kwd',
         'AutoSave.kwd', 'Terrain.kwd', 'AutoSaveMap.kld', 'Shots.kwd', 'GlobalVariables.kwd',
         'CreatureSpells.kwd', 'Traps.kwd', 'KeeperSpells.kwd', 'AutoSaveThings.kld', 'Doors.kwd']

def u32(b, o):
    return struct.unpack_from('<I', b, o)[0]

def inspect(path):
    b = path.read_bytes()
    sig = u32(b, 0)
    r = dict(file=path.name, physical_size=len(b), sha256=hashlib.sha256(b).hexdigest(),
             signature=f'0x{sig:X}', size_field_bytes=u32(b, 4), declared_size=u32(b, 8),
             info_tag=f'0x{u32(b,12):X}', info_size=u32(b,16), first_64_hex=b[:64].hex(' '))
    if sig == 0x64:
        r.update(width=u32(b,20), height=u32(b,24), data_tag=f'0x{u32(b,28):X}', data_size=u32(b,32), data_offset=36)
        r['cells'] = r['width'] * r['height']
        r['terrain_ids'] = sorted(set(b[36::4]))
        r['owners'] = sorted(set(b[37::4]))
        r['bridge_bytes'] = sorted(set(b[38::4]))
        r['fourth_bytes'] = sorted(set(b[39::4]))
    else:
        r.update(count=u32(b,20), unknown24=u32(b,24), data_tag=f'0x{u32(b,48):X}', data_size=u32(b,52), data_offset=56)
        if sig == 0xDC:
            end=56+u32(b,52)
            r['count']=struct.unpack_from('<H',b,20)[0]
            r['extra_word_count']=struct.unpack_from('<H',b,22)[0]
            r['name'] = b[56:184].decode('utf-16-le').split('\0')[0]
            r['reference_tag'] = f'0x{u32(b,end):X}'
            r['reference_data_size'] = u32(b,end+4)
            r['reference_offset'] = end+8
            r['references'] = [dict(type=f'0x{u32(b,end+8+i*72):X}',unknown=u32(b,end+12+i*72),path=b[end+16+i*72:end+80+i*72].split(b'\0')[0].decode('cp1252')) for i in range(struct.unpack_from('<H',b,20)[0])]
            r['reference_end'] = end+8+72*len(r['references'])
            r['extra_words_end']=r['reference_end']+4*r['extra_word_count']
        elif sig == 0xBE:
            p=56
            rec=[]
            for i in range(r['count']):
                tag, size=struct.unpack_from('<II',b,p)
                rec.append(dict(index=i, offset=p, tag=f'0x{tag:X}', payload_size=size))
                p+=8+size
            r['records']=rec
            r['records_end']=p
        elif sig == 0xE6:
            r['record_size']=16
            r['records']=[dict(offset=56+i*16,type=u32(b,56+i*16),words=list(struct.unpack_from('<4I',b,56+i*16))) for i in range(r['count'])]
        elif r['count']:
            r['record_size'],r['division_remainder']=divmod(r['data_size'],r['count'])
            r['names']=[b[56+i*r['record_size']:88+i*r['record_size']].split(b'\0')[0].decode('cp1252') for i in range(r['count'])]
        r['data_end']=r['extra_words_end'] if sig==0xDC else 56+r['data_size']
        r['trailing_size']=len(b)-r['data_end']
        if r['trailing_size']>=12:r['following_section_signature']=f"0x{u32(b,r['data_end']):X}"
    return r

def main():
    rows=[inspect(DATA/name) for name in FILES]
    (ROOT/'inventory.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    for r in rows:
        print(r['file'],r['signature'],'physical',r['physical_size'],'declared',r['declared_size'],'count',r.get('count'), 'stride',r.get('record_size'),'tail',r.get('trailing_size'))
    return rows

if __name__=='__main__': main()
