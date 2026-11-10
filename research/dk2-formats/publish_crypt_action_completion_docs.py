"""Prepend current bounded Crypt action completion result, retaining history."""
from pathlib import Path
ROOT = Path(__file__).resolve().parent
NOTE = ('Актуальный этап189:11 native/7 Ghidra/66 снимков отделили продолжающийся ритуал80 от завершения анимации: duration0/mode0/callback1 сохраняют80, тогда как танец76/mode3 завершается. '
        'Контроли явной остановки и диагностического переполнения часов сохраняют живого участника; повторный Job28 при clock0 может снять регистрацию отдельно от действия. '
        '18686 PC/3 новых+235 прежних диапазонов;938 областей/3234847 байт.166 прежних компонентов/codec/420 источников/7 EXE сохранены, пакет167. '
        '10 счётчиков, остальные естественные прерывания/World scheduler/оплата/опыт открыты; цель активна, около75% без повышения. См. `CRYPT_ACTION_COMPLETION_RU.md`.')


def main():
    for name in ['INTEGRATION_RU.md', 'READINESS_RU.md']:
        file = ROOT/name; text = file.read_text(encoding='utf-8')
        if NOTE not in text:
            file.write_text(NOTE+'\n\n'+text, encoding='utf-8')
    print('phase189 current docs published; prior text preserved')


if __name__ == '__main__':
    main()
