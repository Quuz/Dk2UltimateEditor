"""Extract actual storage widths from the two scalar-variable callbacks."""
from pathlib import Path
from collections import Counter
import json,re

ROOT=Path(__file__).resolve().parent
def save(name,value):(ROOT/name).write_text(json.dumps(value,ensure_ascii=False,indent=2),encoding='utf-8')
def read(name):return (ROOT/'ghidra'/name).read_text().replace('\r','')

def main():
    names=json.loads((ROOT/'enums.json').read_text())['Variable.MiscVariable.MiscType']
    rows={}
    for source,handler,scaled in [('game-common-variable-handler.txt','0x51CD90',False),('game-variable-health-action.txt','0x51CB80',True)]:
        s=read(source)
        for m in re.finditer(r'case (0x[0-9a-f]+|\d+):([\s\S]*?)(?=\n\s*case |\n\s*}\n)',s):
            kind=int(m[1],0);body=m[2]
            a=re.search(r'(_?DAT_|ram0x)([0-9a-f]+)(?:\._(\d+)_(\d+)_)?\s*=\s*([\s\S]+?);',body)
            if not a:continue
            rhs=a[5].strip();width=int(a[4]) if a[4] else {'uVar1':1,'uVar2':2,'iVar3':4,'iVar1':4}.get(rhs)
            assert width in (1,2,4),(kind,a[0])
            row=dict(type=kind,baseline_name=names.get(str(kind),'UNKNOWN'),handler=handler,
                runtime_address=hex(int(a[2],16)+int(a[3] or 0)),runtime_bytes=width,
                transform='signed_i32_divide_by_TPS' if scaled else 'copy_low_bits',
                evidence='ghidra/'+source,assignment=a[0].strip())
            if kind==239:row['transform']='(raw_i32 << 7) / 100; signed rounding; low16 stored; exact expression in evidence'
            rows[str(kind)]=row
        if scaled:
            # A pre-switch branch bypasses the division specifically for type 174.
            rows['174']=dict(type=174,baseline_name=names.get('174','UNKNOWN'),handler=handler,
                runtime_address='0x6f294b',runtime_bytes=4,transform='copy_low_bits',evidence='ghidra/'+source,
                assignment='DAT_006f294b = *(int *)(param_1 + 0x14)')
    assert rows['19']['runtime_address']=='0x6f2590'
    assert rows['71']['runtime_bytes']==1 and rows['239']['runtime_bytes']==2
    entries=[];warnings=[]
    doc=json.loads((ROOT/'decoded/GlobalVariables.kwd.json').read_text())
    for rec in doc['sections'][0]['records']:
        tag,raw,_,_=rec['words_u32'];r=rows.get(str(tag))
        if r is None:continue
        bits=r['runtime_bytes']*8
        value=raw & ((1<<bits)-1)
        item=dict(file_offset=rec['offset'],type=tag,name=r['baseline_name'],file_raw_u32=raw,
            file_value_i32=rec['words_i32'][1],runtime_bytes=r['runtime_bytes'],transform=r['transform'])
        if r['transform']=='copy_low_bits':
            item['effective_unsigned']=value
            item['effective_signed']=value-(1<<bits) if value>>(bits-1) else value
            if bits<32 and raw not in (value, (value|(((1<<32)-1)^((1<<bits)-1))) if value>>(bits-1) else value):
                warnings.append(item)
        entries.append(item)
    save('variable-runtime-map.json',dict(program='DKII-DX.exe',rows=rows,
        no_op_types=[61,62,63,64],no_op_evidence=['ghidra/game-special-variable-default.txt','ghidra/game-variable-61-64.txt'],
        note='Addresses are runtime globals, not KWD offsets. Width does not establish signed use at every consumer. File payload remains 3 DWORDs.'))
    save('variable-runtime-audit.json',dict(records_considered=len(entries),truncation_diagnostics=warnings,records=entries))
    lines=['# Глобальные параметры: сколько бит реально использует игра','',
      'Variables хранит значение в 32 битах по record+4, но scalar callback часто записывает только 8/16 бит во внутреннее поле. Это не ошибка длины файла: дальнейшее применение ограничено загрузчиком. Названия из OpenKeeper, адреса и ширины получены из обработчиков оригинального образа DKII-DX.exe.',
      '0x51CD90 обычно копирует младшие биты. 0x51CB80 делит знаковое значение на TPS (целочисленно, к нулю), затем пишет i32; type174 исключён из деления. Type239 переводит процент в внутреннюю шкалу через сдвиг <<7 и деление на 100, затем сохраняет 16 бит. Полная формула компилятора сохранена как evidence, не заменена приближённым float.',
      f"Механически извлечено {len(rows)} scalar вариантов. Распределение ширин: {dict(Counter(r['runtime_bytes'] for r in rows.values()))}. В текущем GlobalVariables проверено {len(entries)} записей этих типов; неоднозначных обрезаний старших битов: {len(warnings)}.",
      'Для типов 1/2/17/43/65/66/77/224 используются отдельные union-схемы. Типы 61/62/63/64 идут через 0x51DC20 в **пустую функцию 0x401200**; в исследованном EXE они не меняют игровых настроек. Наличие 32-битного хранилища на диске не означает, что все 32 бита действуют в игре.', '',
      '| Тип | Имя базовой схемы | Байт runtime | Преобразование | Runtime адрес |','|---:|---|---:|---|---|']
    for tag,row in sorted(rows.items(),key=lambda p:int(p[0])):
        lines.append(f"| {tag} | {row['baseline_name']} | {row['runtime_bytes']} | {row['transform']} | {row['runtime_address']} |")
    lines+=['','Сохранять исходный DWORD целиком. Не нормализовать его до ширины runtime при обычном пересохранении. Проверка диапазона UI должна различать физический диапазон поля, эффективную ширину конкретного движка и допустимые игровые значения. Трактовка всех потребителей как signed/unsigned отдельно не восстановлена.', '']
    (ROOT/'VARIABLE_RUNTIME.md').write_text('\n'.join(lines),encoding='utf-8')
    print('Extracted scalar mappings:',len(rows),'effective-width diagnostics:',len(warnings))

if __name__=='__main__':main()
