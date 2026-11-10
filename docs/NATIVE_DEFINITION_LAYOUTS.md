# Текущие форматы семи семейств native-определений

Дополнение: [общий inspector parser](NATIVE_PACKED_INSPECTION.md) использует эти же guards. Неизвестный stride теперь сохраняется одним opaque frame, вместо предположительных per-row records. Поддержанные raw fields и более строгие named catalogue constraints сохранены.

25 октября 2026. Авторитетная таблица текущих guards для terrain, rooms,
doors, traps, keeper spells, creatures и creature spells. Это часть I04;
весь импорт коллекций и неизвестных layouts этим документом не закрыт.

| Семейство | Signature | Размер raw-записи | Полный source catalogue для именного редактора |
| --- | --- | --- | --- |
| Terrains | 0x6E | 552 | 41 |
| Rooms | 0x78 | 1055 | 26 |
| Doors | 0x8C | 616 | 8 |
| Traps | 0x82 | 579 | 16 |
| Keeper spells | 0x96 | 406 | 13 |
| Creatures | 0xAA | 5449 или5537 | 46; подтверждённый prefix44 поддержан |
| Creature spells | 0xA0 | 266 | 41 |

Raw typed fields и именной редактор — разные уровни. Для известных размеров
сырой записи теперь всегда нужен подтверждённый заголовок самой секции,
включая секции внутри packed Globals:

- version4, полная длина `56 + count×stride` в пределах фактического файла;
- info signature=`family+1`, info size28 и согласованный положительный count;
- definition signature=`family+2`, payload size=`count×stride`;
- поддержанный stride; положительный count при нулевом payload отклоняется
  до создания записей.

Raw записи остальных шести семейств могут иметь другое число элементов:
размер/заголовок подтверждают поля записи, но не позиционное соответствие
source catalogue. Именные средства дополнительно требуют полный source
count и единый непрерывный каталог. Они не подписывают неизвестные позиции
предполагаемыми именами. Числа41/26/8/16/13/46/41 получены из исходных
GTOrder/GROrder/GDOrder/GAOrder/GSOrder/GCOrder/GOOrder.

Для creatures дополнительно проверяются count44/46 и ID=`ordinal+1` по
offset0xF4B. Все исходные поля находятся в общем prefix5449. Добавленные88
байтов у stride5537 остаются непрозрачными и не заменяются defaults. При44
записях отсутствующие новые виды не создаются и недоступны для редактирования.

Неизвестный inner version, info header или define marker теперь даёт записи
без guessed fields. Именной редактор недоступен, generic SetField отвергается;
native→DKM сообщает `UnverifiedDefinitions` вместо переноса предположительных
значений. Исходные bytes остаются в snapshot. Обычный raw Save/reopen сохраняет
такую секцию без исправления её header, что проверено для всех семи семейств.
Повреждённые counts/payload bounds по-прежнему вызывают явный отказ загрузки.

Найденная ошибка: ранее не-creature секции могли получить typed fields только
по stride. Для packed inner секции не проверялись version/info/define markers;
при известном count именной editor и converter также могли принять её.
Общий `VerifiedDefinitionHeader` теперь используется raw reader, catalogue
guard и creature verifier. Положительный count с zero stride также больше
не создаёт фиктивные записи.

Read-only inventory93 установленных файлов дал567 секций и22 формы заголовка.
Все фактически найденные секции имеют version4/info size28 и известный stride.
Встречаются, например,39…84 creature spells и38/41/42 terrains: это не основание
автоматически считать их полными source catalogues. Инвентаризация не является
новым прогоном совместимости или утверждением о расшифровке всех их bytes.

Проверки: шесть новых групп успешны, плюс три связанные группы (все поля/reset
четырёх creature variants, global defaults и source placement). Отдельная
добавленная проверка подтвердила raw Save/reopen future inner version.
Все41 проекта собраны без ошибок/предупреждений. После следующего tail-stage
EN/RU2882: добавлен явный OpaqueDefinitionData warning, прежние2881 сохранены.
Журналы, source hashes и inventory:
`artifacts/Evidence/DefinitionLayoutGuards-20261025`.

Граница инспектора пока прежняя: Dk2FileInspector проверяет контейнер и
reference headers, а не все inner payloads. Нельзя трактовать IsValid как
разрешение редактировать каждую вложенную запись. Это различие должно войти
в конечную матрицу I04 вместе с прочими коллекциями и conversion callers.

Packed/copy boundaries и tails1/55/56/96/4096 теперь подтверждены
[общим walker](NATIVE_DEFINITION_TAILS.md); magic-only/embedded headers не
вызывают resynchronisation. Unknown headers сохраняются целым opaque block
без интерпретации count. Остаток I04: остальные native collections/исторические
sections и общая inspector/editable/conversion matrix.
Source/, Binaries/, установленные карты и fixtures не менялись; GUI/WPF/игра/
Computer Use не запускались. [Управляющий список](OPEN_FUNCTIONS_RU.md).
