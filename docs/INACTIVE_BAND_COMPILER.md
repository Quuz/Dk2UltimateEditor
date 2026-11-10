Неиспользуемые места отрядов при компиляции, 11 октября 2026

Source/Sources/Compiler/Compiler_Generate.c, Compiler_GenerateHeros, обнуляет
буфер GameHeros и заполняет только непрерывный active prefix Hero->isvalid.
Native GAMEBAND записывается длиной563 bytes: header51 и16 slots по32 bytes.
Неиспользуемые slots остаются целиком нулевыми. В source это явно ZeroMemory,
не условная интерпретация назначения полей.

Наш RecordTemplate/NativeMapDocument.AddRecord проставляет номер отряда во все
16 slots для native editing. При компиляции эти editing defaults попадали
в неиспользуемые места. LegacyProjectCompiler теперь обнуляет только массив
слотов нового generated band до заполнения active prefix. Direct-native
template, DuplicateRecord, пользовательские native файлы и история исходного
DKM не менялись. Активные band/member identities и scenario roots сохранены.

tests/InactiveBandCompilerChecks проверяет literal whole563-byte records для
0/1/15/16 участников, двух разных band IDs, band/member roots, byte cast gold,
level/enemy/health/options, stale inactive fields и все reserved bytes.
Expected wire bytes задаются отдельно без использования NativeSchemas offsets.
SaveCopy/reopen проверяет весь record и ровно семь игровых файлов. Исходный
проект и его revision/history остаются точными. Native-template defaults
проверены отдельно. На предыдущей DLL test воспроизводил failure active0/ID7.

Delivery: Test/editor-inactive-band-compiler-20261011-01. Сборка app.
Фокусные probes используют именно её Core.dll. Actual game bundle delta:
в исходящем пользовательском level1 bundle изменены только97 bytes в Things
по адресам inactive bHeroBandID; остальные previous compiled bytes совпадают.
Новая manual/DK2Preview_6a88240eaf814f78 папка и ZIP содержат семь файлов.
Установленный тест НЕ заменён; game и official editor в этом проходе НЕ запускались.

Аудит known native entity/player fields нормализует original trigger IDs через
validated archive mapping. После fix все16 групп BandHero, GAMEBAND и прочие
проверенные object/player categories совпадают как multisets. Четыре GAMEGATE
ещё отличаются: это отдельный следующий fidelity scope, их смысл не угадывать.
Multiset equality известных полей не доказывает exact opaque/native/game fidelity.
Полный свежий regression report и его terminal exit/comparison проверять отдельно.

Этот regression завершён exit1:1767 pass/98 failures. Полные упорядоченные
списки1767 pass и98 failures совпадают с previous official-reopen run. Все94
historical failures совпадают с baseline без нормализации; оставшиеся4 относятся
к старому установленному preview _Global.kwd, а не к новой правке. Raw report
не фильтровался. Full corpus не зелёный; все115 Core checks прошли в его начале.
DLL этого прогона совпадает с DLL delivered app и focused probes. Source1006 и
installed maps4454 проверены по hash после terminal и остались неизменными.

Сборка App exit0 с одной MSB3026 copy-retry warning из-за одновременной Core
сборки; retry succeeded и hashes совпали. Validation build exit0 без warnings.
Это не изменение логики или непроверенная другая DLL. В последующих сборках
Core-dependent projects с общим obj собирать последовательно; независимые
probes через DLL references можно собирать параллельно.

Точный gate audit сопоставляет четыре GAMEGATE по coordinates/type/owner:
(15,6,type33,owner1):ID2→1,orientation4→0;
(27,39,type37,owner1):ID2→1;
(25,55,type33,owner1):ID2→1;
(14,28,type14,owner3):ID117→0.
Не присваивать этим IDs предположительное значение. Следующий шаг — сохранить
source-only bytes неизменённых imported gates из validated source archive с
baseline/derived-terrain/owner guards; новые, перемещённые или retargeted gates
должны сохранять свежие compiler values. Native fixture matching здесь не
доказывает общую identity policy; ambiguity и Undo требуют отдельного теста.
