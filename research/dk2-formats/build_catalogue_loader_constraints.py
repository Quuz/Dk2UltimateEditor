"""Record each native catalogue parser's division-before-zero-test evidence."""
from pathlib import Path
import json,re

ROOT=Path(__file__).resolve().parent
SPECS=[('Terrain',0x6E,0x56F090,'game-catalogue-terrain-parser',552),
 ('Rooms',0x78,0x56E300,'game-catalogue-rooms-parser',1055),
 ('Traps',0x82,0x571040,'game-catalogue-traps-parser',579),
 ('Doors',0x8C,0x56AC80,'game-catalogue-doors-parser',616),
 ('KeeperSpells',0x96,0x56BCF0,'game-catalogue-keeperspells-parser',406),
 ('CreatureSpells',0xA0,0x56A6D0,'game-catalogue-creaturespells-parser',266),
 ('Creatures',0xAA,0x56A250,'game-creature-parser',5449),
 ('Objects',0xF0,0x56D880,'game-catalogue-objects-parser',894),
 ('EffectElements',0xFA,0x56B740,'game-catalogue-effectelements-parser',182),
 ('Shots',0x104,0x56E900,'game-catalogue-shots-parser',239),
 ('Effects',0x10E,0x56B210,'game-catalogue-effects-parser',246)]

def main():
    result={};lines=['# Ограничения чтения каталогов движком','',
        'Для всех11 семейств в исследованном DKII-DX.exe подтверждён один порядок: активный record callback → вычисление stride=D/N → проверка N. В assembly DIV ECX предшествует TEST ECX,ECX. При N=0 активная ветка делит на ноль. При отсутствии callback отдельная ветка пропускает D байт, поэтому вывод относится именно к активному чтению определений.','',
        '| Семейство | Parser | DIV | Следующий TEST | Native stride |','|---|---|---|---|---:|']
    for family,sig,address,label,stride in SPECS:
        asm_name=label+'-assembly.txt'
        if family=='Shots':asm_name='game-catalogue-shots-parser-assembly.txt'
        asm=(ROOT/'ghidra'/asm_name).read_text(encoding='utf8').splitlines()
        i=next(i for i,s in enumerate(asm) if ': DIV ECX' in s)
        assert ': TEST ECX,ECX' in asm[i+1],family
        division=asm[i].split(':')[0];test=asm[i+1].split(':')[0]
        decomp_name='game-shots-parser.txt' if family=='Shots' else label+'.txt'
        c=(ROOT/'ghidra'/decomp_name).read_text(encoding='utf8')
        assert 'uStack_44 / local_1c' in c and 'if (local_1c != 0)' in c,family
        assert c.index('uStack_44 / local_1c')<c.index('if (local_1c != 0)'),family
        result[hex(sig)]=dict(family=family,parser=hex(address),native_record_bytes=stride,
            observed_record_bytes=[5449,5537] if family=='Creatures' else [stride],
            zero_count_division=hex(int(division,16)),subsequent_zero_test=hex(int(test,16)),
            condition='Record callback registered; the skip-data branch has no such division',
            stride_expression='unsigned D/N before count guard',assembly_evidence=asm_name,decompiler_evidence=decomp_name,
            assembly_context=asm[max(0,i-5):i+5])
        lines.append(f'| {family} | {address:#x} | {int(division,16):#x} | {int(test,16):#x} | {stride} |')
    lines+=['','У Creatures native stride5449; длинные записи5537 измерены в пользовательских Globals и читаются через ветку другого размера. Это не означает, что произвольная длина новой записи или произвольное расширение массива поддержаны. Не уменьшать record stride до размера runtime-структуры.','',
        'Нулевой каталог может быть структурно корректным контейнером. Codec сохраняет возможность его читать/собирать, а compatibility_findings отдельно сообщает риск движка. Не вставлять фиктивную запись автоматически: ID и содержимое требуют осмысленного выбора, а отсутствие секции/ссылки имеет другие последствия.','']
    (ROOT/'catalogue-loader-constraints.json').write_text(json.dumps(result,indent=2),encoding='utf8')
    (ROOT/'CATALOGUE_LOADER_LIMITS.md').write_text('\n'.join(lines),encoding='utf8')
    print('Division-before-zero-guard confirmed for',len(result),'catalogue parsers')

if __name__=='__main__':main()
