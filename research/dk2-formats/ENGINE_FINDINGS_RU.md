# Проверка спорных полей по оригинальному движку

## Загрузка Doors и границы вывода об ошибках, этап67

`DOOR_LOADING_RU.md`:58 полных native-сценариев сравнивают raw616/mid732/def181 при null visual provider. WORD236 сохраняется целиком в def99; raw21E/220/230 не доходят до mid. ResearchTime224 меняет только сумму при её расчёте. Старый loader считает сумму/звук всегда, DX пропускает при global6FCDCF≠0.8 полных PE-функций/2 data ranges,656 новых patches/82 файла,4 EXE неизменны. Игровой смысл unknowns и последствия writer anomalies не установлены.

## Полная запись Doors, этап66

`DOOR_WRITER_RU.md`:42writers/50records,все616bytes+56headerbytesпроверены.21E/220/230 сохраняютстарые8scratchbytes,повторноиспользуемыемеждузаписями;Word236 independent16bits. NativeCStringcopiesentirelength,synthetic562labelвлияетнаследующуюзапись;GUIнепроверен. Этоwriterbehavior,неустановленныйигровойдефект. OldAP04/08boundedsearch2553roots121718states234leads8/28limitsбезновогоpurpose.2624patches/82files/10wholePE,4EXEsunchanged.

## CDeadBody camera и исходный генератор, этап65

`BODY_CAMERA_RU.md`:kind6←nativeCDeadBodyRTTI/base/init,definition129→EyeHeight65C.44 nativeсценария:24fullactivation/20PRNG;3nativeEyeHeightassignmentfragmentchains. XYсмещается signedlow32trigproduct>>16;targetZ=ground+256. ЛокальныеseedsизbodyXY→5PRNGreturns→targetangles;bodyF3&=FE.5executedwholefunctions+5staticidentities;10240PEtrigbytes;3734DWORDpatches/82files. Nativeполекамеры неравноrawstorage;renderer/unknownpurposesнеобъявленызавершёнными.

## HeroLair consumer и проверка инструмента, этап64

`AP_RETREAT_RU.md`:full4611D0/real517490 в23сценариях;HeroLair01 впервые связывается спровереннымretreatconsumer. ПервыйдоступныйAP/startcellQ12;owner/gateпутьточноограничен. Вsuccess native52A/52C обаизentityX;необъявленоподтверждённойвидимойошибкой. `memory-read-policy.json`:CapstoneTESTmem,regaccess0;4correctedpasses,+21Creatureflagtests/+2APcontrolreads. APscan8root/28calleelimits;unknownsнепереименованы.106source3501AP/10503patches;67components.

## Применение камеры, этап63

`CAMERA_ACTIVATION_RU.md`:69 полных44ACE0в68сценариях,19raw→activationchains.13..15→slot9;44B312/44B318 обеLensграницы←mid84,обычноMin−1 послеsignedexclusiveclamp. Gate80 послеобщихsideeffects;mode7/18 толькоmode-state.44BD70zero/nonduration branches исполнены,ноmatrix/time/world/event providersявные,renderer/interpolation несертифицированы.2808rawspanpatches,4EXE unchanged. СтарыеCreature822producerroots:333/335controlreads,нетустановленногоJump/Priority consumer;negativeboundedsearch неglobalnonuse.

## Старые definition pointers и ложные unknown-потребители, этап62

`OLD_ENTITY_DEFINITIONS_RU.md`: old CTrap/CDoor definitionF6 и CObject12C подтверждены nativeRTTI, ctor/factory/init/vslots.21 old lookup calls и10 full58ACA0 cases; вся память сравнивается. Чтение58ACC5WORD97 — Door Height, не Traps98;4ED876WORD98 прослежено к158-byteComputerPlayerconfiguration. StaticObjects563920/494E80 дают raw34C→mid3CC/pres50→def106; gameplay не установлен. V3 boundedsearch408roots5966states10leads исключаетESPstackseeds, ни один bound не достигнут; это не доказательство globalnonuse.

## Полная загрузка Traps в старом и текущем EXE, этап61

`TRAP_LOADING_RU.md`:60 native cases сравнивают полные747-byte intermediate/209-byte definition при null visualprovider;30 soundlookups captured. Unknown204 WORD сохраняется целиком, absence удерживает previous98. ВекторC1..CC перезаписывается безpresence9C. Old всегда заменяет checksum6C и soundB0 приsoundpresence;DXgate6FCDCF!=0 сохраняет их. Native00FF/FF00collision опровергает использование равенстваchecksum как равенства свойств. Isolatedconverter OR-флагов ограничен: реальныйDXparser init вызывается на каждом recordloop. Getter42F0F0 связан с CEEffectElement по ctor/vtable/nativeRTTI и не расшифровывает Objects34C.

## Редактор не является безошибочной спецификацией, этап60

`TRAP_WRITER_RU.md`:38 native cases устанавливают поведение полных Pro/RC3 writers и DDX/commit chain. Control12861 пишет raw1FA ManufToBuild; подпись Mana Gen. скрыта и не доказывает назначение unknown204. Writer теряет неподдержанные биты, не исправляет N/S/D при null-слотах, оставляет два stack padding bytes. Эти наблюдения не превращены в обязательные правила нового codec. Игровое производство подтверждено отдельно; gameplay204 остаётся открытым.

## Height в графике состояния Traps/Doors, этап59

`STRUCTURE_OVERLAY_RU.md`:49 native chains,48 захваченных submissions. Полные converter передают Height DWORD, ограниченные loader-участки сохраняют low16, полный5955C0 расширяет WORD unsigned. Trap offset512; Door offset2048/4248 по native DoorOpen10D. Полный5953D0 проходит native5956E0/5958F0 и передаёт оба XYZ; renderer/resource conversion — fixtures.18 PE functions/8 RTTI/vtable/string ranges,83 файла/3898 точечных пересборок. Candidate5955E5 — DoorHeight97, не Traps204. Остальные неизвестные поля открыты.

## Terrain: полный loader и повторные ID, этап58

`TERRAIN_LOADING_RU.md`:32 native прогона и288 lookup.56F090→56EB60→56ED20→51EB50→4F66E0 сохраняет unknown198/224 без изменения битов. Getter508DD0 возвращает base+460*(arg&255), без проверки count/ID. Обычная запись увеличивает счётчик и суммирует checksum даже для повторного ID; update этого не делает и не обновляет water/lava IDs. Проверена checksum-коллизия разных unknown-значений.11 полных PE функций/82 файла/6612 точечных изменений; пакет67. Назначения неизвестных полей и renderer остаются открытыми.

## Creature mood thresholds и world action44, этап57

`CREATURE_ANGER_RU.md`:129 native cases. F4F→low16(floor(BYTE×32767/100))→WORD76A→zeroextendedDWORD286. Anger6DWORD26E:State0 ifmax<thr,else2 ifmax>32767,else1;mutatedvaluesclamp0..65533.518AD0type44→519704setsworld72A1/01iffavailable==1;4B5A10binds6DC424/6E5050tothesameworldarg. Gate0skipschannelwritesbutstillreclassifiesState. GiftFCBunsignednegationchannel5;Emotionlessgatebypassedbyprecedingdirectreset1/4inselectedfragments.20PEfunctions/7data,7477patches,67components. Jump75C/Priority6EBunknown.

## AP условия Triggers: ранее резервный байт используется, этап56

`AP_TRIGGER_CONDITIONS_RU.md`:61 complete4B0D70 dispatches выполняют4B0620/06D0/07E0/0960/0A00/0A80,508290,519980. Type40/41 parameterBYTE2→TerrainId,baseline reserved_2неверен. Type41 forces==сarea32 приlow16count;65536false. Type69 forces>=сTerrain17B&08count,неплощадью;emptyeligibletrue. Signedcompareoperators1<,2<=,3==,4>,5>=,6!=;invalidfalse.12PEfunctions+4tablesmatch,4236exactpatches,66components. AP04/08purposeunresolved.

## CreatureSpell intermediate — рабочая память, этап55

`CREATURE_SPELL_LOADING_RU.md`:18 полных native parser chains,126 getters;4 группы альтернатив подтверждают semantic equality за исключением gated checksum. Global intermediate746FCA сбрасывается для каждой записи. Его AlternativeRoom747103 одновременно base Traps intermediate:57117A→570B60. Два намеренных последующих вызова настоящего initializer обнуляют747 байт scratch, оставляя70-byte definitions/world неизменными. Полный Traps parser здесь не исполнялся;его адресная привязка доказана статически.12 PE ranges/314 exact patches,пакет65.

## Keeper и создание Shot, этап54

`SHOT_CREATION_RU.md`:32 эмуляции полного native4B7CD0→4B7D60→4AAC90, из них16 вместе с4BBF90/4BC290. Alloc371,primaryID→Shot124,worldlookup→Shot125,definition37/3B→13D/141,WORD47→unsignedDWORD40. Cast затем заменяет13D/141 выбранной Keeper-парой. При upgrade описание остаётся основным.11 complete PE ranges совпали после эмуляции;16653 lossless patches по5551 Shot. Base/render/movement/providers/sound явно заменены;Process callbacks не исполнялись.

## Keeper BonusShot: выбранный ID и ID фабрики различаются, этап53

`KEEPER_CAST_RU.md`:4BBF90выбираетBonus4B/Data4C/50,но4BC160читаетprimary42передfactory4BC19E.4BC290повторяет4BC401/40E. Factory4B7CD0firstarg→existence1AC/ShotBYTE124.120полныхwrapperfixturesпроверяют371Shot/4352player/17state/122definition/64events. Выбранный0блокирует создание;targetRule1/2/3/6→Shot14D1;return0дажеafterevents.5PEfunctions,6396patches. C8 mutation groups do not prove globalnon-use.

## Old/DX: checksum CreatureSpell и состояние AP, этап52

`VERSION_CROSSCHECKS_RU.md`: old4EC440 всегда sum346→definition1B, DX4F7860 gate6FCDCF. Old/DX converter360 отличается толькоCALL на whole-byte-identical strncpy254; initializer198 одинаков. RTTI CActionPoint→statewriter/readers в обоих EXE, oldfactory4AEA80запрашивает40hex. Localstate9→Flags32 сохраняет08. Variables61–64 registeredcallbacks→RET-stub в обоих EXE.90 native cases/26functions/6RTTIranges. Gameplayunknowns остаются открытыми.

## RC3 Action Point: Hero Gate08, этап51

`RC3_ACTION_POINTS_RU.md`: resource278/1835 → DDX432A00/panel68 → apply432D30 → setter425DB0/modelBYTE38 mask08. Parser421FD0/4210C0 → callback496FD0 → packed425E70; writer499DD0/425E50 сохраняет08 и добавляет04 всем AP при любом Next!=0. Upper rawFlags byte native model теряет; raw API сохраняет.156 native cases,45 whole PE ranges. Игровой механизм08 и PlinthCE не установлены.

## RC3: десять совпавших converters и игровой слот Creature Flags, этап50

`RC3_COMPATIBILITY_RU.md`:37 primary ranges;10 converters побайтно равны кромеrel32CALL и2целых helpers равны. Creatures исключение: RC3419FF0 raw1545→mid1935/presence260→decoder4292B0→model1880/1884/1888/1828→encoder429270→writer4AD720 raw1545. Pro использует1599.166 native cases,включая90buffer comparisons и9 scopedFlagschains. Fullmodelcallback/resources/GUI иполные5449writerfieldvalues несертифицированы;unknowngameplayfields остаютсяоткрытыми.

## Камеры в GlobalVariables и отдельный Pro writer, этап49

`CAMERA_CATALOGUE_RU.md`:621 Variables230,затем39 CD вBE. Pro47D5F0 условно ставит49F550 вA8;callback копирует84 bytes mid44 вmodel4;4AB210/RC34B0A20 пишутCD84 целиком.82 native cases,включая39 real chains и4×39 game callbacks. Порядок повторныхID13 меняетFlags2A/22;дедупликация неразрешена. Широкое толкование прежнего отсутствиякамер неверно:48B5F0/4947C0 лишьmapThings. Native parser/model теряетraw53,аотдельныйwriter копируетегоиз модели.

## Terrain RGB: BYTE+Flags, editor256 против game255, этап48

`TERRAIN_LIGHTING_RU.md`: Terrain1DB..1DD и Ambient201..203 вместе с шестью Flags190 битами дают GUI0..511. Full setters/getters420320..4204B0 и flag codec425640/4254C0 проверены. Game4F69A0..4F6AB6 прибавляет255 к runtime WORD независимо от channel presence/enable; при partial update возможно повторное накопление.99 native cases/17 PE ranges. Full GUI commit46E410 статически найден, не путать с destructor46E350. Полный writer и renderer не исполнялись; неизвестные198/224 не получили названия.

## C6/C7/C8: межверсийное совпадение converter и сдвиг instance fields, этап47

`CREATURE_VERSION_LAYOUT_RU.md`: old565630/565730/565840 и DX56F330/56F430/56F540 одинаковы по всем instruction bytes; native mid56/84/64 совпадает. Старый dump488270 читает Party416/Wander417/Trigger418, DX490240 —417/418/419. Типы BYTE/BYTE/WORD, Trigger sign-extends для печати.12 diagnostic fragments и36 full converters;48 cases. Исходный rawlayout не сдвигается; неизвестные gameplay meanings не назначены. Три AutoSave baseline drift записаны явно, original files не восстанавливались.

## Camera store: native Machine Spec Level, этап46

`CAMERA_MACHINE_SPEC_RU.md`:566520 пишет this74 и ключ Machine Spec Level, pointer66F560→6BDA30. При this759A78 это ровно759AEC, читаемый449A30. Unsigned level<5 уменьшает maxZoom,>=5 сохраняет.41 native cases:28 full setter/store chains и13 full565CF0 profiles; -SPEC0..10 меняет8 других полей,74 сохраняется. Registry callbacks — fixtures, real wrappers работают;9 function/3 data livePE ranges. Ранее неизвестное имя зависимости теперь установлено, raw projection не сохранять поверх файла.

## ActionPoint: native reveal10 и toolbox20, этап45

`ACTION_POINT_VISIBILITY_RU.md`: fragment50C05D..50C170 + full454B80/517480 подтверждает inclusive rectangle и OR runtime tileWORD08 bit2000 с сохранением прочих bits. Already-set пропускает callbacks; room definitionBYTE452=3/4/8 делегирует4E8B80 (fixture). Cell event3 один раз при переходе, area event6 на каждую AP. Full510E20 выбирает последний matching20 ID37, включая0.36 cases,60 raw patches,4 fullPE ranges. Это runtime map16, неKLD cell4. Unknown04/08 остаются открытыми.

## KeeperSpells: полный writer и два stack bytes, этап44

`KEEPER_PIPELINE_RU.md`: full4A99B0 не назначает rawE0/E1 при bounded имени, converters56BAB0/42B720 игнорируют их. Сохранены22 полных writer snapshots, включая четыре seeds и null/reordered arrays; N/S/D использует count slots, null payload пропускается. C8→mid140→runtime3A без TPS; editor zero-extendsDWORD, /4096→double178, full writer восстанавливает low32, включаяFE0C0000/FFFFFFFF. Имя C8 не установлено.68 native cases,522 patches,82files1066records;15 full livePE ranges. Raw/unknown flags сохранять.

## Creatures: происхождение расширения, этап43

`CREATURE_EXTENSION_RU.md`: full writer4A7F00 оставляет stack84 в raw1545..1598 при bounded имени, включая game DWORD/opaque80; actual41EB80 пишет flags модели1599. Tail159D..15A0 получает contextECX bytes1..3 и low-byte return4A7F10 после4AD490. Context54F550 даётF5540010, совпало3504/3504 long records.12 paired converters/8 full parsers подтвердили игнорирование80/last4 и раздельные slots. Game5537 read перекрывает первые88 mid, затем converter назначает их заново.38 native cases,2484 patches,53 livePE ranges. Corpus35 ненулевых editor flags при game0 во всехlong; slots не зеркала. Raw не обнулять.

## CreatureSpells: references, native labels и extent, этап42

`SPELL_ALTERNATIVES_RU.md`: rawE8 Shot/ E9 Room подтверждены native panel257→DDX443010→model16F/16E→writer. Presence model crossed28/2C; normal full converters задают оба, без value swap. LookupC20/C50 возвращает−1; commit-фрагмент использует его без bounds-check, проверен контролируемый guard-byte read, полный GUI не заявлен. Init56A490/42A000 очищает346 bytes, loader4F7860 суммирует346 и не переносит альтернативные ID в semantic definition fields; при gate0 они меняют только checksum. Исправлена формулировка этапа41 о320 как полном размере: прежние тесты проверяли prefix.63 native cases,1110 patches,18 livePE ranges,157 ненулевых reference pairs/3535 records, missing paired targets0. Gameplay replacement rule остаётся открытым.

## CreatureSpells: native writer и opaque tail, этап41

`SPELL_WRITER_RU.md`: converters56A560/42A0D0 не читают rawEF..109(27). Full writer4A9730 сохраняет в этом диапазоне stack seed при bounded имени; строка240 через реальный4AB3B0 перезаписывает первый tail byte, поэтому ограничение явно сохранено. Model decoder41D760/encoder41D6F0 оставляет flags1EF, game converter сохраняет все32 бита. Null array slots пропускаются без исправления N/S/D.61 native case,393 API checks,360 exact in-memory patches,82 файла3535 записей2707 ненулевых хвостов;12 livePE ranges. Исторический смысл каждого хвоста/неизвестных flags не объявлен установленным. Raw сохранять.

## Things CD: BYTE ID, камеры и Editor Pro skip, этап40

`CAMERAS_RU.md`: converters56F900/42FE20 читают rawBYTE52, rawBYTE53 не копируют. Callback5215F0→449A30 checksum152 и slotManager28+id*98 только1..18; при global759AEC<5 ZoomMax уменьшается доmin+(max-min)/3. Это банк конфигураций, не active view. Relative flags10/20 блокируют activeView/Lens; absolute setters игнорируют их. Editor registration48B5F0 оставляет cameraA8=0, parser431220 seek-skip, writer4947C0 не содержитCD.110 Things безCD;84 synthetic native cases,472 exact patches16 appends;21 livePE ranges. Полная mode activation и экранная визуализация не проверены; opaque53/flags сохранять.

## Things CC: TerrainId selector, свойства комнат и writer, этап39

`PLACED_ROOMS_RU.md`: callback521420 не создаёт комнату; требует playerBYTE!=0, valid position и существующий tile roomId. Только12Portal/14Heart/37Gate3×1 имеют applying branches. Editor492030 требует terrain-byte match, поддерживает12/40 portals,14 heart,33/37 gates. Portal health modern low8WORD, heart fullWORD, gates default100/forcedPlayer1. Writer portal0→100 меняет модель; gate raw20; heart serializer4225A0 оставляетBYTE0F scratch.858 CC совпали с map terrain;586 ненулевых raw0F.136 native cases,15444 exact in-memory patches,26 live ranges. UnknownZ/flags/opaque0F сохранять; прочие этапы map loading не исключены.

## Things CA: исправление GoldHeld/PlayerId, этап38

`DEAD_BODIES_RU.md`: rawWORD0C — health input редактора; game5212D0 не читает его после передачи intermediate32 в checksum. RawBYTE0F — Level: init4B0F20→runtime12D, native dump4B18B0 подписывает Level; body tick4B1540 использует его с definition626/growth2D9 для масштаба renderer. Это не поле владельца. Editor491DE0 major!=1 использует low8/clamp100; major1 — перевод абсолютного здоровья, где полное x87 rounding ограничено явно описанным fixture. Full writer4947C0 назначает все16 payload bytes, Z0, healthBYTE→WORD, LevelBYTE.74 native cases,12 тел,110 Things roundtrip,144 exact in-memory patches,21 live PE ranges. Community aliases не заменяют эти подтверждённые поля; baseline сохранён для аудита.

## Things CB: область, слоты эффектов и Frequency, этап37

`EFFECT_GENERATORS_RU.md` подтверждает WORD10/12 как Width/Height, Q12-появление в положительных extents и выбор всех четырёх слотов, включая0. Полный tick4B1B60 требует unsigned now>deadline; пустой слот/отклонённая позиция не сдвигают deadline, вызов создания сдвигает даже при ошибке. Editor491F20 уменьшает extents по bounds setters; writer4947C0 пишет Z/flags0 и не назначает хвост6. В80 генераторах14 ненулевых хвостов; все raw сохранять.62 native-прогона,2880 точечных in-memory пересборок,110 Things roundtrip,23 live PE ranges. Полная визуализация эффекта/GUI не проверялись; неизвестные Flags/Z/tail не объявлены расшифрованными.

## Level DF: opaque520/2 и native scratch, этап35

`LEVEL_DESCRIPTOR_RUNTIME_RU.md` подтверждает полным writer493200 и converters56C100/42BDE0: два opaque-диапазона raw1184(520)/63C1(2) пропускаются converters и сохраняют scratch writer при строках в пределах полей. Raw63CA converter копирует, writer не назначает. Верхние24 бита available-creature DWORD также остаются scratch. Реальные48CF90/4AB3B0 копируют всю CString length, count только дополняет: длинная Information может записать в первыйWORD opaque520, что отдельно подтверждено и ограничивает вывод.38 native-прогонов с полным сравнением25991-byte intermediate/целых output-файлов,14 parse→pack,1017 точечных patch;23 live ranges совпали с PE. Игровое назначение baseline UniqueCreatureTypeId и исторический смысл opaque520 не установлены.

## ActionPoint: native-панель и самоссылки, этап34

`ACTION_POINT_EDITOR_RU.md` подтверждает цепочку ресурс278→fill4323A0→DDX432530→apply432850→setters: Hero Lair01, Reveal10, Tool Box20, Ignore Solid40. Apply меняет только71, сохраняет unknown04/08. Auto-ID41B840 выбирает первый свободный1..80 и не меняет прежний ID при исчерпании; rawID остаётся byte. Wait GUI0..32767 отделён от rawWORD. Choice builder4325C0 исключает текущий ID, lookup432820 при отсутствии возвращает0:170 штатных самоссылок в31 файле при композиции панели проецируются вnone.46 native-прогонов,4603 границы,252 точечных пересборки;13 live ranges совпадают с PE. Реальный GUI save не выполнялся. Unknown04/08 gameplay effect остаётся открытым.

Исследован локальный `Dungeon Keeper 2/DKII-DX.exe`, SHA-256 `d7a8b854e4e4f02709eba271bead6fe2307dd6e332b5a96e70857e5b0e19fb8a`. Адреса ниже относятся к этому образу x86. Исходные файлы игры не изменены. Декомпиляции сохранены в `ghidra/game-*.txt`. Имена из шаблонов отделены от того, что непосредственно видно в коде. Патчи Flame, применяемые при запуске, здесь не отслеживались.

## Things ActionPoint: center/radius, mask3D и строгий deadline, этап33

`ACTION_POINTS_RU.md` уточняет C5: raw DWORD08=0 заставляет converters обоих EXE вычислять corners из center/radius; game сужает углы доWORD, WaitDelay*TPS доWORD, flags доmask3D. Raw40 сохраняется Editor Pro, runtime patrol его читает, но этот C5 loader его не устанавливает. Writer при любой ненулевой waypoint-связи OR04 всем AP моделям и пишет flagsBYTE widenedWORD, оставляя name32 scratch в проверенном пути.

Patrol471EB0 использует random(end-start), неinclusive count; отдельный count4B07E0 имеет+1. Next472150 сохраняет nextBYTE в objectiveDWORD, deadline=now+waitWORD; wait4721C0 заканчивается только unsigned now>deadline.70 native-прогонов252 точечных пересборки,26 live ranges matchPE — `phase33-validation.json`. Семантика unknown04/08 и полный UI commit ещё не установлены.

## Things HeroParty: четыре варианта и C9 member paths, этап32

`HERO_PARTIES_RU.md` устанавливает C9 payload264/268/300/555. В современном555 raw X23/X27 не копируются в intermediate, все16 слотов копируются полностью. Native writer4947C0 не назначает имя32 и reserved8; полные writer fixtures и593-state prefix stack audit отделены от общих helper contracts. Для268 name читается вне полученного payload и не восстанавливается из файла однозначно.

World50FA70 адресует slot520 по unsigned byteID без проверки24, сохраняет trigger/ID и512 bytes members, оставляет runtime gap3..7. Editor ограничивает get/count/writer24 slots; member callback C9 присваивает header PartyId, сохраняет/defaults Leader/Follower и Level0, отличается от C7 default Objective/AP. Native-проекции отделены от lossless raw patch.50 native-прогонов,1184 пересборки8 добавлений и46 диапазонов живой Ghidra памяти, совпавших с PE — `phase32-validation.json`. Полный spawn и MFC insertion не моделировались.

## Things Creatures: percentages, PartyId и native loss, этап31

`PLACED_CREATURES_RU.md` подтверждает проценты золота/здоровья в C6/C7/C8 через x870.01 и level-adjusted unsigned WORD definitions. Native diagnostic strings дают имена WanderRadius/PartyId прежним неизвестным bytes; PartyId417 используется458C70 для same-party Leader/Follower отбора, включая равныеFF. Эффект и единицы WanderRadius418 не установлены.

В modern Editor load low8 применяется доmin100:300→44,256→0,255→100. Good callback отдельно сбрасывает PartyIdFF и Leader/Follower, сужает AP DWORD доbyte; Keeper сохраняет полныйAP. Не нормализовать raw по этим правилам без явного режима. C9 party paths не проверены этим компонентом.88 native-прогонов,2064 пересборки в памяти и8717 direct creature placements — `phase31-validation.json`.

## Things Objects: селекторы, золото и original save, этап30

`PLACED_OBJECTS_RU.md` дополняет baseline C2: DWORD0C является также полным селектором старого ObjectId7 для Special117..129; game callback520BB0 сравнивает32 бита, editor4916B0 использует условный byte getter после setter. Пример258→117 в editor требует Book bit8; штатный standalone ID7 имеет Special без Book и так не преобразуется. Флаги возвращённого определения выбирают Gold40 → Book08 → Crate10. Gold helper49C760 складывает по модулю2³² и обнуляет сумму со знаковым битом. Crate использует actual instance ObjectId, не предполагаемый индекс/ID двери или ловушки.

Raw ObjectId28 пропускается после checksum, DWORD08 не задаёт высоту (Z0), TriggerId не передаётся создателю в этом callback. Все поля сохранять raw; checksum читает intermediate48 до этих ветвей. Editor writer4947C0 записывает DWORD08=0 и условно обнуляет0C/10 по флагам; этот native-путь отделён от независимого lossless codec.99 native-прогонов,96 точечных пересборок в памяти,562 инструкции и12 повторных assembly — `phase30-validation.json`.

## Скорость поворота Creatures +0xF20

Конвертер `0x568E30` переносит 16 бит из записи +0xF20 в промежуточное поле +0x1310. Загрузчик `0x492320` явно читает его как **ushort**, делит на число тиков симуляции в секунду и сохраняет результат в runtime+0x6A0. Если исходное значение ненулевое, а частное равно нулю, записывает 1.

В `0x4B2AE0` и `0x4B2C10` это значение ограничивает изменение угла за шаг. Полный оборот — **2048**, углы оборачиваются маской `0x7FF`. До целочисленного деления на TPS:

```text
degreesPerSecond = raw * 360 / 2048
perTickLimit = floor(raw / TPS)    // затем минимум 1 для положительного raw
```

Например, raw=2048 задаёт номинально 360°/с. При TPS=20 предел одного шага равен 102 угловым единицам, поэтому фактическая сумма за секунду немного отличается из-за округления. Для крайних значений учитывать дальнейшую 16-битную запись и угловые маски; это не обещание монотонного поведения при любом TPS.

OpenKeeper трактует это поле как i16/4096. Такая трактовка ошибочно меняет знак верхней половины диапазона и единицы измерения. В `layouts.json` оставлена исходная схема для аудита; использовать `verified-overrides.json`, `engine_fields` в дампах и `set_engine_scalar(...,'MaxAngleChangeRaw',raw)` для правки. Границы 0, 2048, 32768, 65535 проверены на сохранение остальных байтов.

## Два слота дополнительных флагов

Продолжение этап36 — `KING_LIGHTNING_RU.md`: бит1 теперь проверен22 полными native Process9 handlers в нынешнем DX и EXE из папкиDKII13. В обоих standalone Creatures у King Reginald/ID21 Flags1545=1; в проверенных Globals=0. Подтверждена цепочка ThunderboltKeeper6→Shot21→Process9 и аналогичные converter/loader старогоEXE. Номер версии старого файла не выведен только из названия папки; хеш закреплён. Проверка не является полным игровым боем.

| Слот записи Creatures | Что подтверждено |
|---|---|
| `0x1545`, u32 | Конвертер игры 0x568E30 читает это поле в промежуточное +0x1935; далее 0x492320 переносит в runtime+0x68A |
| `0x1599`, u32 | Декодер оригинального редактора 0x4287C0 читает этот слот длинной записи 5537 |

Загрузчик игры `0x56A250` вычисляет размер записи по D/N, читает запись и вызывает тот же конвертер. В исследованном конвертере нет чтения +0x1599. Слоты не являются автоматически взаимозаменяемыми. У короткой записи 5449 слот +0x1545 является последним DWORD; длинная добавляет 80 неизвестных байтов, редакторский DWORD и два неизвестных 16-битных поля. Это не дополнительный ArtResource.

Применение runtime+0x68A также найдено:

- `0x4AF1E0`: бит 0x1 участвует в условии, препятствующем вызову нанесения урона в проверенном пути атаки молнией.
- `0x478CE0`: бит 0x4 пропускает вызов 0x486ED0. Название Emotionless согласуется с шаблоном, но весь набор эмоциональных реакций этим наблюдением не доказан.
- `0x4DBF90`: бит 0x8 участвует в допуске существа к комнате ID 0x19 (hero portal); рядом проверяются обычный портал и уникальность существа.

Для бита 0x2 название Stone Knight взято из источников сообщества; все его игровые последствия не установлены. Не обнулять неизвестные биты. Не копировать один слот в другой автоматически: это отдельная операция совместимости, требующая проверки конкретной версии игры и редактора.

Дополнительно проверена запись оригинального редактора: helper 0x41EB80 заново собирает только биты 1/2/4/8 из четырёх UI-полей. Assembly начинает с XOR EAX,EAX, поэтому остальные биты не сохраняются этим helper. Писатель 0x4A7F00 сохраняет результат по raw+0x1599: store по 0x4A8699 в ESP+0x162D, тогда как указатель начала записи ESP+0x94 передаётся в Write по 0x4A8B7D. Разность — 0x1599. Это ещё одно основание сохранять неизвестные биты в новом редакторе самостоятельно. Прямой записи в игровой слот +0x1545 в просмотренном writer не найдено; это наблюдение не заменяет эксперимент сохранения карты оригинальным редактором. Первичные файлы: `editor-creature-extra-flags-save-assembly.txt`, `editor-creature-writer-assembly.txt`.

## Переменные

Загрузчик `0x5716E0` читает 16 байт записи и формирует промежуточную запись 32 байта с признаками присутствия. Диспетчер `0x521790` выбирает callback по первому DWORD. Таблица инициализации — `0x51AA90`; извлечённые строки — `variable-dispatch.json`. Адреса промежуточных полей не являются смещениями в файле.

| Тип | Обработчик | Подтверждённое устройство |
|---:|---|---|
| 17 | `0x51DB00 → 0x506FF0` | Три DWORD полезной части добавляются в список по порядку; запись списка 12 байт. Имена category, definitionID, points — дополнительный шаблон и согласование всех 29 ID |
| 43 | `0x51DB30` | Три пары type/ID по байту, rewardType, speechID, rewardValue; подтверждено побайтовое извлечение |
| 65 | `0x51DC40` | statID и level выбирают ячейку; value записывается младшими 16 битами. В файле уровни 2..10 |
| 66 | `0x51DC90` | Младшие байты двух ID игроков выбирают симметричные ячейки союза; в обе записывается 1. Третье слово не читается |
| 77 | `0x51DD10 → 0x494A70` | creatureID выбирает определение; statID передаётся как u8; value меняет runtime-поле характеристики |
| 224 | `0x51DC70` | statID выбирает одну ячейку; value записывается младшими 16 битами. Третье слово **не читается**, хотя в файле равно 10 |

Тип 66 в исследованном образе — союз. Подпись ManaGenerationForJob в позднем `.bt` не соответствует этому обработчику. Тип 224 не образует таблицу по уровням аналогично типу 65.

Для типа 77 сопоставлены 29 веток setter с полями Creatures; три Stat ID (8, 11, 14) не имеют веток. Таблица — `CREATURE_STATS.md` и `creature-stat-runtime-map.json`. В частности, удалось связать неизвестные Stat ID 12, 20, 26, 29 с полями оценки существа, неудачного разоружения, движения во владении и масштаба. Названия последних происходят из шаблона; сохранён точный путь присваивания.

Значение type 77 не следует просто кодировать по масштабу одноимённого поля Creatures: setter работает после преобразования загрузчика. Например, в некоторых ветках он делит на TPS, в других обрезает до 8/16 бит; конвертер исходного каталога использует ещё и fixed-point преобразования. Универсального `/4096` для всех свойств нет.

## AutoSave.kwd

Парсер `0x56C4E0` принимает DF-блок штатной длины 0x6453 = 25683 байта и вызывает `0x56C100`. Этот конвертер читает **20 сообщений по 512 UTF-16 символов**, начиная с полезной части DF+0x138C. Шаг сообщения — 0x400 байт. Трактовка OpenKeeper «512 сообщений по 20 символов» имеет ту же длину, но неверные границы строк.

Параметр тиков по DF+0x1182 читается как u16. SpecialLevelNumber по +0x63C9 и следующий байт +0x63CA читаются раздельно; дополнительный шаблон называет второй UniqueCreatureTypeId. Базовые поля и расширение до 25683 описаны в `LEVEL_LAYOUT.md` / `level-layouts.json`.

После ссылок DE игра читает K DWORD и для каждого передаёт в callback младший байт как ID существа. Таким образом, «23 неизвестных слова» в данном AutoSave — структурно список creature ID. Старшие байты нельзя терять только потому, что текущий загрузчик их игнорирует. Дампы сохраняют `extra_words` и добавляют поясняющий `creature_list`.

## Что именно было «контрольной суммой»

Поля заголовка +0x08 и +0x34 — длины секции и данных; они пересчитываются по размеру записей. Это подтверждается писателями редактора и точной пересборкой файлов. Для записи файла не нужно придумывать CRC этих полей.

Внутри движка действительно есть отдельный аккумулятор: `0x5108C0` складывает знаковые байты промежуточных структур в поле объекта мира. Это runtime-механизм, а не описанное в книге поле длины и не дополнительная контрольная сумма в исследованных заголовках KWD.

## Граница результата

Форматы всех 15 запрошенных файлов разобраны до границ записей, смещений и правил безопасной сборки; семантика части полей остаётся неизвестной. Наличие имени в сторонней библиотеке или чтение DWORD в runtime не доказывает всех игровых эффектов. Позднее дополнение `PHASE2_FINDINGS_RU.md` раскрывает процесс-зависимые Data1/Data2 всех 39 процессов Shots и отдельно 9 режимов урона, реальные ширины 199 Variables, FootstepFrames, Width/Mass и WorkModifier. Для нерасшифрованных полей, неизвестных флагов и новых вариантов контейнера сохранять исходные данные и статус источника. Игровой прогон изменённых карт в эту проверку не входил.

## Уточнение25 октября: отдельные суммы определений

`CATALOGUE_CHECKSUMS_RU.md` и `catalogue-checksums.json` дополнительно фиксируют unsigned byte-sum девяти промежуточных структур. Они отличаются от signed-аккумулятора0x5108C0, уже описанного выше. Нормальные callbacks прибавляют DWORD определения к накопителю семейства. В исследованных KWD-заголовках дополнительных полей этих runtime-сумм не установлено; формулы N/D/S остаются правилами сериализации количества и размеров.

## Уточнение25 октября: signed-мана Terrain/Traps

`MANA_RUNTIME_RU.md` / `mana-semantics.json` подтверждают MOVSX для Terrain+0x19C (ManaGain),+0x19E (MaxManaGain) и Traps+0x202 (ManaUsage). MaxManaGain меняет player+0x10DE, native MaxManaGainPerSecond; отдельный предел запаса — player+0x10EA/MaxMana. Типы baseline оставлены для хранения raw-битов, игровое представление signed16. Обработчик смены типа Terrain455B90 корректирует только доход;4559D0 при смене владельца корректирует оба накопителя. Формулы, исключения и62 native-эмуляции закреплены отдельно; неизвестный Traps+0x204 не получил имени ManaUsage.

## Уточнение25 октября: Creatures Ability Creep Percentage

`CREATURE_CREEP_RU.md` подтверждает raw+0xEE4, runtime+0x760, StatId26 и native property0xF85C. При Creeping0x1000 поле пересчитывает метрику квадрата XY-расстояния; это не прямая скорость движения. Game converter/loader сохраняет32 бита, а Editor Pro callback сужает до8; writer обнуляет старшие24 бита. Необходимы полный raw DWORD для сохранности и отдельная явная native-compatibility projection. Новая схема — компонент35.


## Этап25: first-person Creatures

`CREATURE_FIRST_PERSON_RU.md` и `creature-first-person-semantics.json`: raw107B/runtime79B сначала unsigned>>12, затем>1; первым выбирается доступный ненулевой spell slot0..2. Producer40FDF0 создаёт ActionE с low32(base*factor) либо base, затем Action27; handler5171D0 меняет только SniperMode bit80000000. Значения камеры и300 не получают неподтверждённых физических единиц. RawDBC/DC0/D3D потребляются40E910 с native signed-синусной таблицей и разными делителями; input-waddle и action80-waddle различаются в8 раз до усечения.65 native-прогонов,613 границ,3036 пересборок в памяти. Полная камера/renderer не исполнялись; raw сохранение без нормализации.


## Этап26: KeeperSpells flags и боевой выбор

`KEEPER_AI_RU.md` / `keeper-ai-semantics.json`: gameFlags DWORD DC→154→54 сохраняет все биты; Editor Pro420270/420230 сужает представление до mask1E при записи. AI503060 требует8 или4 при отсутствии10. Защитный selectedId присваивается до random и оставляет последнего подходящего при всех провалах; атакующий присваивается после успеха. Cost читается unsigned и делится на2499; threshold=unsigned32(4-P),P три бита AI.48 native-прогонов и273 пересборки в памяти. Xc8 не расшифрован: native editor model178 — отдельный Q12 scalar, основной RechargeTime использует CC/model180.


## Этап27: Effects native save

`EFFECT_EDITOR_RU.md`: rawF4/F5 не назначается просмотренным writer4AA040 перед file-vcall38 с246 байтами. В31 native-прогоне проверены writer capture и отдельные flags/tail/converter ветви; четыре stack seeds сохранились в emittedF4. Game converter этот WORD не переносит; global unused не утверждается. Новый редактор сохраняет rawWORD вместо native stack-содержимого. Raw flagsB4→game168→runtime5F полныйDWORD; native callback+encoder даютmaskFFF7 с добавлением80 при отсутствииC0. Nonzero overlay12 WORD подтверждён тремя native fixtures.530 границ14430 пересборок в памяти, компонент38.


## Этап28: Map native cells

`MAP_CELLS_RU.md` / `map-cell-semantics.json`: byte2 не универсальный enumBridge — flags. Writer494470 для17/26/36 проверяет четыре линейных соседних байта:4→1, затем5→2; без4/5 сохраняет scratch flag предыдущей клетки, без bounds checks. Game callback51EC60 переносит flags02 вruntimeWORD+8 bit1000 послеterrain/room placement. Byte3 ignored в inspected parser/converter; full writer его не назначает, поэтому independent codec сохраняет raw.109 карт583365 клеток,22 native-прогона1287 границ96 пересборок в памяти; компонент39.


## Этап29: размещённые Trap/Door

`PLACED_STRUCTURES_RU.md`: Trap payload+C — selector 0/неноль для activation, а не присваиваемый shot count в placement. Door payload+10 — masks 01/02; blueprint в instance109 блокирует state3 в setter 4A6800. Game placement вычисляет центр X/Y в Q12 и Z0. Opaque DWORD+8 переносится в intermediate/checksum; global unused не утверждается. Native writer пишет этот DWORD0, independent codec сохраняет raw. 28 native-прогонов, 527 границ и 294 пересборки в памяти; компонент40.
