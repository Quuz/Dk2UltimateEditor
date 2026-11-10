"""Resolve this installation's recipe IDs to hashed local catalogue names."""
from pathlib import Path
import json,hashlib,struct
from collections import Counter
import dk2_formats as fmt
from native_sacrifice import runtime_recipe
ROOT=Path(__file__).resolve().parent
def digest(b):return hashlib.sha256(b).hexdigest()
def main():
    names={};catalogues=[]
    for kind,file,id_field in [(1,'Creatures.kwd','CreatureId'),(2,'Objects.kwd','ObjectId'),(3,'KeeperSpells.kwd','KeeperSpellId')]:
        decoded=json.loads((ROOT/'decoded'/(file+'.json')).read_text(encoding='utf8'));p=Path(decoded['source']);assert digest(p.read_bytes())==decoded['sha256']
        names[kind]={r['fields'][id_field]:r['fields']['Name'] for s in decoded['sections'] for r in s['records'] if id_field in r.get('fields',{})}
        catalogues.append(dict(file=str(p),sha256=decoded['sha256'],identity_field=id_field))
    source=Path('D:/Games/DK2/Dungeon Keeper 2/Data/editor/GlobalVariables.kwd');data=source.read_bytes();rows=[]
    for section in fmt.parse(data):
        if section.signature!=230:continue
        for i,raw in enumerate(section.records):
            if int.from_bytes(raw[:4],'little')!=43:continue
            runtime=runtime_recipe(raw);count,kind,value,speech=struct.unpack_from('<IIII',runtime,18);inputs=[]
            for j in range(count):
                typ,ident,_=struct.unpack_from('<IBB',runtime,6*j)
                inputs.append(dict(type=typ,id=ident,name=names.get(typ,{}).get(ident)))
            reward=dict(type=kind,value_bits=value,speech_id=speech)
            if kind in [1,2]:reward.update(id=value&255,name=names[kind].get(value&255))
            rows.append(dict(record=i,file_offset=section.offset+len(section.header)+16*i,raw_hex=raw.hex(),inputs=inputs,reward=reward))
    assert len(rows)==44 and all(v['name'] is not None for r in rows for v in r['inputs'])
    report=dict(source=str(source),sha256=digest(data),catalogues=catalogues,recipes=rows,
                order='Input array is chronological: first sacrifice to last. Record list order controls first-match priority.',
                scope='Current local files only. Name lookup does not execute reward factories or establish official release defaults.')
    (ROOT/'temple-recipes-current.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
    lines=['# Храмовые рецепты текущей установки','',f'Источник: `{source}`, SHA-256 `{digest(data)}`.',
      '','Это 44 записи именно предоставленного файла. Порядок ингредиентов — от первой жертвы к последней; порядок строк совпадает с файлом и определяет приоритет. Имена ID взяты из проверенных по SHA локальных каталогов.',
      '','Для награды-существa matcher добавляет средний уровень жертв-существ в старшую часть значения. Успешное создание наград creature/object/gold этим этапом не исполнялось; награда маной подтверждена полной native-цепочкой.',
      '','| Запись | Смещение файла | Жертвы по порядку | Награда по записи | Речь |','|---:|---:|---|---|---:|']
    kind_names={1:'существо',2:'объект',3:'заклинание'}
    for r in rows:
        inputs=' → '.join(f"{kind_names.get(v['type'],str(v['type']))} {v['name'] or v['id']}" for v in r['inputs'])
        rw=r['reward'];kind=rw['type'];value=rw['value_bits']
        result=(f"{'существо' if kind==1 else 'объект'} {rw['name'] or rw['id']}" if kind in [1,2] else f"{'золото' if kind==3 else 'мана' if kind==4 else 'тип '+str(kind)}: {value}")
        if kind in [1,2] and value>>8:result+=f' (raw {value:08X}h)'
        lines.append(f"| {r['record']} | {r['file_offset']:X}h | {inputs} | {result} | {rw['speech_id']} |")
    lines += ['','Подтверждённые правила и ограничения: `SACRIFICE_RU.md`; машинная схема: `sacrifice-semantics.json`.','']
    (ROOT/'TEMPLE_RECIPES_RU.md').write_text('\n'.join(lines),encoding='utf8')
    print('Exported',len(rows),'recipes; reward kinds:',dict(Counter(r['reward']['type'] for r in rows)))
if __name__=='__main__':main()
