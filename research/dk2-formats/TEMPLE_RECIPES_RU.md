# Храмовые рецепты текущей установки

Источник: `D:\Games\DK2\Dungeon Keeper 2\Data\editor\GlobalVariables.kwd`, SHA-256 `d14b6a28694254dad722071267268d4f36c8535cd3a9c610f9ee0447fe9b941a`.

Это 44 записи именно предоставленного файла. Порядок ингредиентов — от первой жертвы к последней; порядок строк совпадает с файлом и определяет приоритет. Имена ID взяты из проверенных по SHA локальных каталогов.

Для награды-существa matcher добавляет средний уровень жертв-существ в старшую часть значения. Успешное создание наград creature/object/gold этим этапом не исполнялось; награда маной подтверждена полной native-цепочкой.

| Запись | Смещение файла | Жертвы по порядку | Награда по записи | Речь |
|---:|---:|---|---|---:|
| 254 | 1018h | существо Vampire → существо Vampire | существо Bile Demon | 20 |
| 255 | 1028h | существо Troll → существо Troll | существо Warlock | 22 |
| 256 | 1038h | существо Salamander → существо Salamander | существо Mistress | 21 |
| 257 | 1048h | существо Dark Elf → существо Dark Elf | существо Troll | 24 |
| 258 | 1058h | существо Warlock → существо Warlock | существо Goblin | 27 |
| 259 | 1068h | существо Rogue → существо Rogue | существо Salamander | 28 |
| 260 | 1078h | существо Skeleton → существо Skeleton → существо Firefly | существо Dark Elf | 23 |
| 261 | 1088h | существо Black Knight → существо Black Knight | существо Vampire | 25 |
| 262 | 1098h | существо Bile Demon → существо Bile Demon | существо Rogue | 32 |
| 263 | 10A8h | существо Mistress → существо Mistress | существо Skeleton | 6 |
| 264 | 10B8h | существо Guard → существо Mistress | существо Black Knight | 31 |
| 265 | 10C8h | существо Black Knight → существо Warlock → существо Firefly | существо Imp | 5 |
| 266 | 10D8h | существо Black Knight → заклинание Turncoat | существо Knight | 33 |
| 267 | 10E8h | существо Goblin → заклинание Create Imp | существо Dwarf | 34 |
| 268 | 10F8h | существо Bile Demon → существо Salamander → заклинание Tremor | существо Giant | 35 |
| 269 | 1108h | существо Warlock → заклинание Inferno | существо Wizard | 36 |
| 270 | 1118h | существо Dark Elf → заклинание Sight Of Evil | существо Elven Archer | 37 |
| 271 | 1128h | существо Mistress → заклинание Create Gold | существо Thief | 38 |
| 272 | 1138h | существо Vampire → существо Guard → заклинание Heal | существо Monk | 39 |
| 273 | 1148h | существо Mistress → существо Firefly → заклинание Thunderbolt | существо Fairy | 40 |
| 274 | 1158h | существо Rogue → существо Dark Elf → заклинание Call To Arms | существо Guard | 42 |
| 275 | 1168h | существо Imp → существо Bile Demon | объект Crate Gas | 1 |
| 276 | 1178h | существо Imp → существо Giant | объект Crate Boulder | 1 |
| 277 | 1188h | существо Imp → существо Warlock | объект Crate Sentry | 1 |
| 278 | 1198h | существо Imp → существо Dark Elf | объект Crate Alarm | 1 |
| 279 | 11A8h | существо Imp → существо Salamander | объект Crate Fireburst | 1 |
| 280 | 11B8h | существо Imp → существо Mistress | объект Crate Lightning | 1 |
| 281 | 11C8h | существо Imp → существо Fairy | объект Crate Freeze | 1 |
| 282 | 11D8h | существо Imp → существо Skeleton | объект Crate Fear | 1 |
| 283 | 11E8h | существо Imp → существо Troll | объект Crate Trigger | 1 |
| 284 | 11F8h | существо Imp → существо Black Knight | объект Crate Spike | 1 |
| 285 | 1208h | существо Imp → существо Thief | объект Crate Wooden Door | 1 |
| 286 | 1218h | существо Imp → существо Guard | объект Crate Braced Door | 1 |
| 287 | 1228h | существо Imp → существо Knight | объект Crate Steel Door | 1 |
| 288 | 1238h | существо Imp → существо Wizard | объект Crate Magic Door | 1 |
| 289 | 1248h | существо Imp → существо Rogue | объект Crate Secret Door | 1 |
| 290 | 1258h | существо Imp → существо Elven Archer | объект Crate Barricade | 1 |
| 291 | 1268h | существо Monk → существо Monk → существо Monk | объект Special [ Mana Boost ] | 9 |
| 292 | 1278h | существо Thief → существо Thief → существо Thief | объект Special [ Increase Gold ] | 9 |
| 293 | 1288h | существо Dwarf → существо Dwarf → существо Mistress | объект Special [ Make Safe ] | 9 |
| 294 | 1298h | существо Mistress → существо Mistress → существо Fairy | объект Special [ Make Happy ] | 9 |
| 295 | 12A8h | существо Black Knight → существо Black Knight → существо Dark Elf | объект Special [ Make Unhappy ] | 9 |
| 296 | 12B8h | существо Bile Demon → существо Dark Elf → существо Mistress | объект Special [ Receive Imps ] | 9 |
| 297 | 12C8h | существо Bile Demon → существо Giant → существо Guard | объект Special [ Stun Imps ] | 9 |

Подтверждённые правила и ограничения: `SACRIFICE_RU.md`; машинная схема: `sacrifice-semantics.json`.
