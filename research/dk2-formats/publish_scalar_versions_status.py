from pathlib import Path
ROOT=Path(__file__).resolve().parent
def main():
    p=ROOT/'INTEGRATION_RU.md';t=p.read_text(encoding='utf8')
    if 'Актуальный этап 101:' not in t:
        t=t.replace('Актуальный этап 100:','Предыдущий этап 100:',1)
        lead=('Актуальный этап 101: `POSSESSION_SCALARS_RU.md` / `possession-scalar-version-semantics.json`, компонент `possession_scalar_version_semantics`. '
          '32 новых native-сценария/38 callback-выполнений сравнивают scalar112/113/114 в точных old/DX профилях. '
          'Подтверждено low16-хранение, независимость соседних WORD и повторная замена; игровые бонусы113/114 не установлены. '
          'Old DWORD-чтение6DD978 отбрасывает113 черезAND FFFFh;110 neighbourhood-leads не дают глобального non-use доказательства. '
          '557Variables/1422selectedrecords/7110точечных пересборок сохраняют остальные байты;3codeextents/4blocks/4EXEсверены. '
          'Компонент80,прежние79схем побайтово сохранены;полныйpossession/формулы113114 исущественныевопросы15семействоткрыты.\n\n')
        t=t.replace('# Комплект для разработчика редактора\n\n','# Комплект для разработчика редактора\n\n'+lead,1);p.write_text(t,encoding='utf8')
    p=ROOT/'READINESS_RU.md';t=p.read_text(encoding='utf8').replace('после этапа 100','после этапа 101',1)
    t=t.replace('актуальный опубликованный этап — 100, пакет содержит 79 компонентов','актуальный опубликованный этап — 101, пакет содержит 80 компонентов',1)
    if '**Этап101 сравнил' not in t:
        t=t.replace('## Бонусы вселения\n\n','## Бонусы вселения\n\n'
          '**Этап101 сравнил загрузку scalar113/114 в старом и текущем EXE:**32 сценария подтвердили одинаковое сохранениеlow16 и независимость соседних ячеек. '
          'Перекрывающее DWORD-чтение старого профиля отбрасывает113 маскойFFFFh. Их бонусы и формулы всё ещё неизвестны;это не подтверждение+6%. '
          'См. `POSSESSION_SCALARS_RU.md`;7110точечных пересборок на557Variables сохраняют исходные неизвестные байты.\n\n',1)
    p.write_text(t,encoding='utf8');print('Phase101 readerstatus updated')
if __name__=='__main__':main()
