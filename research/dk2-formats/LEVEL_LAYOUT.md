# Параметры уровня AutoSave.kwd

Native-уточнение этап35: `LEVEL_DESCRIPTOR_RUNTIME_RU.md` / `level-descriptor-semantics.json`. UnknownHistory520 и Unknown63C1 не переносятся converters обоих EXE, а writer при строках в пределах полей оставляет там scratch. Raw63CA переносится converter, но writer также оставляет его неприсвоенным. Верхние3 байта DWORD списка существ в native writer зависят от scratch. Исходные байты всех этих областей сохранять; baseline-имена ниже оставлены для аудита.38 native-прогонов проверяют modern DF25683.

Таблица описывает блок `0xDF`, смещения от начала его полезной части (в этом AutoSave — файл+0x38). Длина 25683 байта. Старый вариант 25603 заканчивается перед OneShotHornyLevel. Источники: закреплённый level_struct.bt и игровой конвертер 0x56C100.
Игровой конвертер подтверждает **20 сообщений по 512 UTF-16 символов**, а не 512 сообщений по 20 символов в OpenKeeper. Поле SpecialLevelNumber — u8; следующий u8 является отдельным полем. Нулевые терминаторы и байты после них сохраняются в raw.
Названия смыслов преимущественно из шаблона; чтение смещения и размера движком не доказывает всех игровых последствий. UnknownHistory и Unknown63C1 остаются нерасшифрованными.

| Смещение DF | Смещение файла | Байт | Тип | Поле |
|---|---|---:|---|---|
| `0x0` | `0x38` | 128 | utf16le | Name |
| `0x80` | `0xb8` | 2048 | utf16le | Description |
| `0x880` | `0x8b8` | 128 | utf16le | Author |
| `0x900` | `0x938` | 128 | utf16le | Email |
| `0x980` | `0x9b8` | 2048 | utf16le | Information |
| `0x1180` | `0x11b8` | 2 | u16 | TriggerId |
| `0x1182` | `0x11ba` | 2 | u16 | TicksPerSecond |
| `0x1184` | `0x11bc` | 520 | bytes | UnknownHistory |
| `0x138c` | `0x13c4` | 1024 | utf16le | Messages[0] |
| `0x178c` | `0x17c4` | 1024 | utf16le | Messages[1] |
| `0x1b8c` | `0x1bc4` | 1024 | utf16le | Messages[2] |
| `0x1f8c` | `0x1fc4` | 1024 | utf16le | Messages[3] |
| `0x238c` | `0x23c4` | 1024 | utf16le | Messages[4] |
| `0x278c` | `0x27c4` | 1024 | utf16le | Messages[5] |
| `0x2b8c` | `0x2bc4` | 1024 | utf16le | Messages[6] |
| `0x2f8c` | `0x2fc4` | 1024 | utf16le | Messages[7] |
| `0x338c` | `0x33c4` | 1024 | utf16le | Messages[8] |
| `0x378c` | `0x37c4` | 1024 | utf16le | Messages[9] |
| `0x3b8c` | `0x3bc4` | 1024 | utf16le | Messages[10] |
| `0x3f8c` | `0x3fc4` | 1024 | utf16le | Messages[11] |
| `0x438c` | `0x43c4` | 1024 | utf16le | Messages[12] |
| `0x478c` | `0x47c4` | 1024 | utf16le | Messages[13] |
| `0x4b8c` | `0x4bc4` | 1024 | utf16le | Messages[14] |
| `0x4f8c` | `0x4fc4` | 1024 | utf16le | Messages[15] |
| `0x538c` | `0x53c4` | 1024 | utf16le | Messages[16] |
| `0x578c` | `0x57c4` | 1024 | utf16le | Messages[17] |
| `0x5b8c` | `0x5bc4` | 1024 | utf16le | Messages[18] |
| `0x5f8c` | `0x5fc4` | 1024 | utf16le | Messages[19] |
| `0x638c` | `0x63c4` | 2 | flags16 | Flags |
| `0x638e` | `0x63c6` | 32 | string | SoundCategory |
| `0x63ae` | `0x63e6` | 1 | u8 | TalismanPieces |
| `0x63af` | `0x63e7` | 1 | enum8 | RewardPrevious[0] |
| `0x63b0` | `0x63e8` | 1 | enum8 | RewardPrevious[1] |
| `0x63b1` | `0x63e9` | 1 | enum8 | RewardPrevious[2] |
| `0x63b2` | `0x63ea` | 1 | enum8 | RewardPrevious[3] |
| `0x63b3` | `0x63eb` | 1 | enum8 | RewardNext[0] |
| `0x63b4` | `0x63ec` | 1 | enum8 | RewardNext[1] |
| `0x63b5` | `0x63ed` | 1 | enum8 | RewardNext[2] |
| `0x63b6` | `0x63ee` | 1 | enum8 | RewardNext[3] |
| `0x63b7` | `0x63ef` | 1 | u8 | SoundTrack |
| `0x63b8` | `0x63f0` | 1 | u8 | TextTableId |
| `0x63b9` | `0x63f1` | 2 | u16 | TextTitleId |
| `0x63bb` | `0x63f3` | 2 | u16 | TextPlotId |
| `0x63bd` | `0x63f5` | 2 | u16 | TextDebriefId |
| `0x63bf` | `0x63f7` | 2 | u16 | TextObjectiveId |
| `0x63c1` | `0x63f9` | 2 | bytes | Unknown63C1 |
| `0x63c3` | `0x63fb` | 2 | u16 | TextSubObjectiveId1 |
| `0x63c5` | `0x63fd` | 2 | u16 | TextSubObjectiveId2 |
| `0x63c7` | `0x63ff` | 2 | u16 | TextSubObjectiveId3 |
| `0x63c9` | `0x6401` | 1 | u8 | SpecialLevelNumber |
| `0x63ca` | `0x6402` | 1 | u8 | UniqueCreatureTypeId |
| `0x63cb` | `0x6403` | 1 | u8 | IntroductionCreatureId[0] |
| `0x63cc` | `0x6404` | 1 | u8 | IntroductionCreatureId[1] |
| `0x63cd` | `0x6405` | 1 | u8 | IntroductionCreatureId[2] |
| `0x63ce` | `0x6406` | 1 | u8 | IntroductionCreatureId[3] |
| `0x63cf` | `0x6407` | 1 | u8 | IntroductionCreatureId[4] |
| `0x63d0` | `0x6408` | 1 | u8 | IntroductionCreatureId[5] |
| `0x63d1` | `0x6409` | 1 | u8 | IntroductionCreatureId[6] |
| `0x63d2` | `0x640a` | 1 | u8 | IntroductionCreatureId[7] |
| `0x63d3` | `0x640b` | 2 | u16 | IntroductionTextId[0] |
| `0x63d5` | `0x640d` | 2 | u16 | IntroductionTextId[1] |
| `0x63d7` | `0x640f` | 2 | u16 | IntroductionTextId[2] |
| `0x63d9` | `0x6411` | 2 | u16 | IntroductionTextId[3] |
| `0x63db` | `0x6413` | 2 | u16 | IntroductionTextId[4] |
| `0x63dd` | `0x6415` | 2 | u16 | IntroductionTextId[5] |
| `0x63df` | `0x6417` | 2 | u16 | IntroductionTextId[6] |
| `0x63e1` | `0x6419` | 2 | u16 | IntroductionTextId[7] |
| `0x63e3` | `0x641b` | 32 | string | TerrainPath |
| `0x6403` | `0x643b` | 1 | u8 | OneShotHornyLevel |
| `0x6404` | `0x643c` | 1 | u8 | PlayerCount |
| `0x6405` | `0x643d` | 1 | enum8 | RewardPreviousExtra |
| `0x6406` | `0x643e` | 1 | enum8 | RewardNextExtra |
| `0x6407` | `0x643f` | 2 | u16 | SpeechHornyId |
| `0x6409` | `0x6441` | 2 | u16 | SpeechPrelevelId |
| `0x640b` | `0x6443` | 2 | u16 | SpeechPostlevelWinId |
| `0x640d` | `0x6445` | 2 | u16 | SpeechPostlevelLostId |
| `0x640f` | `0x6447` | 2 | u16 | SpeechPostlevelNewsId |
| `0x6411` | `0x6449` | 2 | u16 | SpeechPrelevelGeneralId |
| `0x6413` | `0x644b` | 64 | utf16le | HeroName |

После DF: `u32 0xDE`, `u32 referenceBlockBytes`, N ссылок по 72 байта, K DWORD списка существ. N и K — два u16 по +0x14 и +0x16 заголовка. Игра 0x56C4E0 читает DWORD каждого элемента списка, затем передаёт **младший байт как creature ID**. Сохранять все 4 байта; это не случайная дополнительная область.
Ссылка: u32 тип каталога, u32 служебное поле, char path[64]. Длина блока = 72*N+4*K для этого файла. Порядок ссылок, неизвестное служебное поле и старшие байты creature ID сохранять.
