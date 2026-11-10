"""Trace the flat eight-byte trigger parameter switches in pinned OpenKeeper."""
from pathlib import Path
import json,re
from extract_layouts import TEXT,block,KINDS
from native_ap_conditions import apply_layout_overrides
from native_creature_anger import apply_action_override

ROOT=Path(__file__).resolve().parent

def main():
    enums=json.loads((ROOT/'enums.json').read_text(encoding='utf8'));result={};lines=['# Параметры узлов Triggers','',
        'Контейнер подтверждён загрузчиком DKII-DX.exe 0x571290. Имена восьми байт параметров ниже получены из закреплённого OpenKeeper KwdFile.java; индивидуальные игровые эффекты остальных действий здесь не проверены; семь AP-условий уточнены по DX в этапе56, SET_CREATURE_MOODS — в этапе57. Padding сохраняется даже при ненулевых байтах. Неизвестные типы остаются raw.','',
        'Заголовок 60 байт: +20 число условий D5, +24 число действий D6; узел — tag u32, payloadBytes u32, payload 16 байт. В payload: parameters[8], Id u16, NextId u16, ChildId u16, Type u8, RepeatTimes u8. D по +56 часто равно только 16 × число условий. Физический размер секции включает все узлы и их восьмибайтовые оболочки.','']
    for family,tag in [('TriggerGeneric','0xD5'),('TriggerAction','0xD6')]:
        marker='switch ((('+family+') trigger).getType())'
        pos=TEXT.index(marker);start=TEXT.index('{',pos);body,_=block(TEXT,start)
        enum=enums[family+('.TargetType' if family=='TriggerGeneric' else '.ActionType')]
        reverse={v:int(k) for k,v in enum.items()};rows={}
        # No nested switches/branches in these pinned parameter cases.
        for group in re.finditer(r'((?:\s*case\s+\w+\s*:\s*)+)([\s\S]*?)break\s*;',body):
            names=re.findall(r'case\s+(\w+)\s*:',group[1]);code=group[2];offset=0;fields=[]
            for statement in code.split(';'):
                read=re.search(r'reader\.(\w+)\(([^()]*)\)',statement)
                if not read:continue
                method,arg=read.groups()
                if method in ('readAndCheckNull','skipBytes'):
                    kind,size='bytes',int(arg);name='reserved_'+str(offset)
                else:
                    kind,size=KINDS[method]
                    key=re.search(r'setUserData\("([^"]+)"',statement)
                    name=key[1] if key else 'comparison' if 'setTargetValueComparison' in statement else None
                    assert name,statement
                field=dict(name=name,offset=offset,offset_hex=hex(offset),size=size,kind=kind)
                if kind.startswith(('enum','flags')):field['enum']=arg.removesuffix('.class')
                fields.append(field);offset+=size
            assert offset==8,(family,names,offset)
            for name in names:
                assert name in reverse,name
                rows[str(reverse[name])]=dict(name=name,parameter_bytes=8,fields=fields,
                    source_line=TEXT[:start+1].count('\n')+body[:group.start(2)].count('\n')+1,
                    confidence='pinned_source_layout_not_individual_gameplay_verification')
        result[tag]=rows
        if tag=='0xD5':apply_layout_overrides(result)
        if tag=='0xD6':apply_action_override(result)
        lines+=['## '+family,'','| Type | Имя | Поля первых 8 байт payload |','|---:|---|---|']
        for ident,row in sorted(rows.items(),key=lambda item:int(item[0])):
            lines.append(f"| {ident} | {row['name']} | "+'; '.join(f"+{f['offset']:02X} {f['name']} {f['kind']}" for f in row['fields'])+' |')
        lines.append('')
    lines += ['## Native-уточнение этап56','','DX4B0D70 и61 native проверка уточняют семь AP условий. У AP_CLAIM_PART_OF/ALL_OF байт2 — живой TerrainId, а не reserved. AP_CLAIM_ALL_OF принудительно сравнивает count16 с полной32-битной площадью на равенство; AP_TAG_ALL_OF сравнивает >= с числом Terrain17B&08, игнорируя comparison/value. Прежние поля сохранены в baseline_fields; исходные байты не нормализуются. Подробности — AP_TRIGGER_CONDITIONS_RU.md и action-point-condition-semantics.json.','']
    lines += ['## Native-уточнение этап57','','D6 type44 SET_CREATURE_MOODS: только availableBYTE==1 включает bit01 world72A1. Двенадцать полных native action→anger-add цепочек подтвердили gate и сохранение остальных битов. Подробности — CREATURE_ANGER_RU.md.','']
    (ROOT/'trigger-layouts.json').write_text(json.dumps(result,indent=2),encoding='utf8')
    (ROOT/'TRIGGERS_LAYOUT.md').write_text('\n'.join(lines),encoding='utf8')
    print('Trigger parameter layouts:',{k:len(v) for k,v in result.items()})

if __name__=='__main__':main()
