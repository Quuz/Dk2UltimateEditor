"""Publish current Crypt runtime writer result without dropping prior history."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent
NOTE=('Актуальный этап190:6 native/6 Ghidra подтвердили общий IO-потребитель счётчиков: CPlayer writer4B8D40 через настоящие MyFile/Disc передаёт блок76..109D и все20 байт108A..109D. '
      'Это18 сопоставленных групп+2 неназначенных хвостовых слота, не новые поля KWD/бонусы. Короткая/неудачная внешняя запись даёт65/0 байт при возврате1; внешний save-framework не исполнен. '
      'Исправлена карта памяти стенда, полные guards прошли;12 прежних проб исключены.138 областей/986970 байт,562 PC/10 новых+7 прежних диапазонов. '
      '167 прежних компонентов/codec/420 источников/7 EXE сохранены, пакет168.10 игровых назначений и полный жизненный цикл Crypt открыты; цель активна, около75% без повышения. См. `CRYPT_STATE_WRITER_RU.md`.')


def main():
    for name in ['INTEGRATION_RU.md','READINESS_RU.md']:
        file=ROOT/name;text=file.read_text(encoding='utf-8')
        if NOTE not in text:file.write_text(NOTE+'\n\n'+text,encoding='utf-8')
    print('phase190 current docs published; old text retained')


if __name__=='__main__':main()
