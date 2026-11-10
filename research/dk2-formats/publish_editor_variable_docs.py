"""Append new evidence and prepend current stage; preserve dated older notes."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent

def main():
    note=('Этап166: оригинальныйPro47FFB0/49EC90 подтверждает248начальныхdouble/193числовыхID, signed32 и6масштабируемыхтипов. '
          'Неизвестный233 принимается/начальное40;19/29/186/242 пропускаются ввыбранныхсценах. Настоящийрецепт43неинтерпретируетсяcallback, '
          'потерявсегофайланедоказана. Повторнаяинициализациясохраняет100=42. '
          '16native/4полныхGhidra,249областей/1175180байт;8точныхправокреальногоGlobalVariables. '
          '7дополнительныхдиапазонов(2статических)/4унаследованных/1261PC/2блока. '
          'Зацепка48131F/320 относитсякглобальномумассиву[100],неTerrain. '
          'Прежние144компонента/codec/всеeffectiveкаталоги сохранены;420текущихsourceSHA/7EXE проверены. '
          'Игровойсмысл233/113/114 ипрочиезначимые15семейныевопросы открыты. '
          'См.`EDITOR_VARIABLE_LOADING_RU.md`; экспертнаяоценкаоколо75%, цельактивна.')
    for name,prefix,oldprefix in [('INTEGRATION_RU.md','Актуальный этап 166: пакет содержит 145 компонентов. ','Актуальный этап 165:'),
        ('READINESS_RU.md','# Оценка готовности на 2 ноября 2026 — после этапа 166\n\nпакет содержит 145 компонентов. ','# Оценка готовности на 2 ноября 2026 — после этапа 165')]:
        p=ROOT/name;s=p.read_text(encoding='utf-8')
        if not s.startswith(prefix):
            assert s.startswith(oldprefix);p.write_text(prefix+note+'\n\n'+s,encoding='utf-8')
    p=ROOT/'SOURCES.md';s=p.read_text(encoding='utf-8')
    line='Дополнение2026-11-02,этап166: оригинальные47FFB0/49EC90 иточнаярегистрация47D6E4/47D6F1;7дополнительных/4унаследованныхдиапазона,2блока,1261PC. Независимое восстановление248double/193ID,16native/4Ghidra/8точныхправокGlobalVariables. `editor-variable-loading-semantics.json`, `EDITOR_VARIABLE_LOADING_RU.md`, `phase166-validation.json`. Новыхигровыхимёнopaque-полей0;оригинальныефайлынеизменены.\n'
    if line not in s:p.write_text(s+'\n'+line,encoding='utf-8')
    print('Published phase166 documentation.')

if __name__=='__main__':main()
