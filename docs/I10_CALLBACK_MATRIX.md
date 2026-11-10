# I10: конечная матрица параметров и обработчиков сценария

25 октября 2026. Кодовая сверка [I10 завершена](I10_CODE_CLOSURE.md).
Матрица охватывает все103 типа:69 Parameters,26 RepeatOnly,8 NoPage.
Source rows ссылаются на реальные объявления функций, не на вызовы внутри if.
Для групп с общим case-body полный текст и SharedWith сохранены в evidence.
Строка не означает новый runtime PASS: успешные старые проверки не повторялись.

| Тип | PageID / маршрут | Init / Check / Store, строки source | Семейство реализации |
| --- | --- | --- | --- |
| `TRIGGER_VARIABLE` | 7101 / Parameters | 837 / 1083 / 1404 | Alias |
| `TRIGGER_TIMER` | 7102 / Parameters | 860 / 1107 / 1427 | Alias |
| `TRIGGER_DOORDESTROYED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_OBJECTCLAIMED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_APPEAR` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_DEATH` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_SLAPPED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_SEE` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_JAILED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_TORTURED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_CONVERTED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_TURNCOAT` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_HUNGRY` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_AFRAID` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_STEAL` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_GOAWAY` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_STUNNED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_TRANSITIONENDS` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_NOTHUNGRY` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_FIRED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_TAKE` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_CREATURES` | 7104 / Parameters | 882 / 1133 / 1450 | Ordinary |
| `TRIGGER_HAPPYCREATURES` | 7105 / Parameters | 887 / 1140 / 1464 | Ordinary |
| `TRIGGER_ANGRYCREATURES` | 7135 / Parameters | 892 / 1147 / 1478 | Ordinary |
| `TRIGGER_LOSTCREATURES` | 7106 / Parameters | 897 / 1154 / 1492 | Ordinary |
| `TRIGGER_KILLEDCREATURES` | 7131 / Parameters | 902 / 1161 / 1506 | Ordinary |
| `TRIGGER_ROOMS` | 7108 / Parameters | 917 / 1182 / 1543 | Ordinary |
| `TRIGGER_ROOMSSIZE` | 7136 / Parameters | 927 / 1196 / 1571 | Ordinary |
| `TRIGGER_ROOMSCONTENT` | 7138 / Parameters | 932 / 1203 / 1585 | Ordinary |
| `TRIGGER_ROOMSTYPES` | 7109 / Parameters | 922 / 1189 / 1557 | Ordinary |
| `TRIGGER_DOORS` | 7110 / Parameters | 937 / 1210 / 1599 | Ordinary |
| `TRIGGER_TRAPS` | 7111 / Parameters | 942 / 1229 / 1616 | Ordinary |
| `TRIGGER_DESTROYED` | 7137 / Parameters | 947 / 1248 / 1633 | Ordinary |
| `TRIGGER_SPELLS` | 7112 / Parameters | 953 / 1254 / 1647 | Ordinary |
| `TRIGGER_GOLDAMOUNT` | 7114 / Parameters | 962 / 1265 / 1664 | Ordinary |
| `TRIGGER_GOLDMINED` | 7113 / Parameters | 958 / 1258 / 1653 | Ordinary |
| `TRIGGER_MANAAMOUNT` | 7130 / Parameters | 966 / 1272 / 1675 | Ordinary |
| `TRIGGER_TIMEELAPSED` | 7103 / Parameters | 878 / 1120 / 1439 | Ordinary |
| `TRIGGER_HURTED` | 7129 / Parameters | 1068 / 1371 / 1849 | Ordinary |
| `TRIGGER_CREAENTERAREA` | 7121 / Parameters | 993 / 1305 / 1730 | Ordinary |
| `TRIGGER_CLAIMAREA` | 7122 / Parameters | 999 / 1312 / 1747 | Ordinary |
| `TRIGGER_CLAIMENTIREAREA` | 7123 / Parameters | 1013 / 1319 / 1761 | Ordinary |
| `TRIGGER_AREACONTENT` | 7124 / Parameters | 1018 / 1323 / 1768 | Ordinary |
| `TRIGGER_BANDCREATED` | 7132 / Parameters | 1048 / 1342 / 1804 | Ordinary |
| `TRIGGER_JAILEDINBAND` | 7133 / Parameters | 1058 / 1355 / 1823 | Ordinary |
| `TRIGGER_DEATHSINBAND` | 7128 / Parameters | 1053 / 1347 / 1810 | Ordinary |
| `TRIGGER_GOLDDAY` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_HEARTDESTROYED` | 7119 / Parameters | 983 / 1297 / 1716 | Ordinary |
| `TRIGGER_CREATURESLEVEL` | 7107 / Parameters | 907 / 1168 / 1520 | Ordinary |
| `TRIGGER_CREATUREEXP` | 7134 / Parameters | 912 / 1176 / 1532 | Ordinary |
| `TRIGGER_CLICKELEMENT` | 7117 / Parameters | 974 / 1286 / 1697 | Ordinary |
| `TRIGGER_TAKECREATURE` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_FREECREATURE` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_FIRECREATURE` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_HURTCREATURE` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_SELECTAREA` | 7125 / Parameters | 1031 / 1331 / 1783 | Ordinary |
| `TRIGGER_STUNNEDINBAND` | 7127 / Parameters | 1063 / 1363 / 1836 | Ordinary |
| `TRIGGER_SELECTALLAREA` | 7126 / Parameters | 1043 / 1338 / 1797 | Ordinary |
| `TRIGGER_ENTERAREAINPOSS` | 7120 / Parameters | 988 / 1301 / 1723 | Ordinary |
| `TRIGGER_SLAPCOUNT` | 7116 / Parameters | 970 / 1279 / 1686 | Ordinary |
| `TRIGGER_GROUPINPOSSESS` | 7118 / Parameters | 979 / 1290 / 1705 | Ordinary |
| `TRIGGER_BREACHED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `TRIGGER_ENNEMYBREACHED` | -1 / RepeatOnly | — / — / — | RepeatOnly |
| `ACTION_ADDCREATURE` | 7203 / Parameters | 1006 / 1224 / 1632 | Spawn |
| `ACTION_SHOWCRYSTAL` | -1 / NoPage (неактивные Init/Check) | 1014 / 1235 / — | NoPage |
| `ACTION_ADDELEMENT` | 7204 / Parameters | 1018 / 1239 / 1642 | Ordinary |
| `ACTION_SETVARIABLE` | 7201 / Parameters | 1024 / 1245 / 1653 | Alias |
| `ACTION_SETTIMER` | 7202 / Parameters | 1043 / 1297 / 1675 | Alias |
| `ACTION_SHOWELEMENT` | 7210 / Parameters | 1060 / 1303 / 1680 | Ordinary |
| `ACTION_VICTORY` | -1 / NoPage | — / — / — | NoPage |
| `ACTION_DEFEAT` | -1 / NoPage | — / — / — | NoPage |
| `ACTION_RELEASEBAND` | 7207 / Parameters | 1065 / 1310 / 1691 | Ordinary |
| `ACTION_CHANGEPARAMS` | 7219 / Parameters | 1071 / 1332 / 1706 | Ordinary |
| `ACTION_SHOWAREA` | 7205 / Parameters | 1077 / 1370 / 1724 | Ordinary |
| `ACTION_ENABLEFOG` | 7206 / Parameters | 1082 / 1377 / 1731 | Ordinary |
| `ACTION_ALLIES` | 7222 / Parameters | 1087 / 1384 / 1738 | Ordinary |
| `ACTION_HASCRYSTAL` | -1 / NoPage | — / — / — | NoPage |
| `ACTION_POSSESSCREATURE` | -1 / NoPage | — / — / — | NoPage |
| `ACTION_CHANGESQUARE` | 7209 / Parameters | 1093 / 1389 / 1748 | Spatial |
| `ACTION_DISPLAYTIMER` | 7217 / Parameters | 1100 / 1400 / 1756 | Ordinary |
| `ACTION_SAY` | 7211 / Parameters | 1119 / 1414 / 1762 | Speech |
| `ACTION_MOVECAMERA` | 7213 / Parameters | 1125 / 1421 / 1768 | Ordinary |
| `ACTION_ROTATECAMERA` | 7214 / Parameters | 1130 / 1427 / 1773 | Ordinary |
| `ACTION_GENERATE` | 7223 / Parameters | 1167 / 1487 / 1825 | Ordinary |
| `ACTION_MAKEHUNGRY` | -1 / NoPage | — / — / — | NoPage |
| `ACTION_SHOWHEALTH` | 7231 / Parameters | 1204 / 1531 / 1875 | Ordinary |
| `ACTION_CAMERAPATH` | 7224 / Parameters | 1185 / 1507 / 1850 | Camera |
| `ACTION_DESTROYGATE` | 7208 / Parameters | 1135 / 1437 / 1780 | Gate |
| `ACTION_RUN` | 7220 / Parameters | 1140 / 1443 / 1785 | Ordinary |
| `ACTION_MUSICLEVEL` | 7225 / Parameters | 1194 / 1521 / 1864 | Ordinary |
| `ACTION_REMOVE` | -1 / NoPage | — / — / — | NoPage |
| `ACTION_ATTACK` | 7226 / Parameters | 1200 / 1526 / 1869 | Ordinary |
| `ACTION_PORTALS` | 7227 / Parameters | 1173 / 1492 / 1832 | Ordinary |
| `ACTION_CHANGEVIEW` | 7212 / Parameters | 1144 / 1448 / 1791 | Ordinary |
| `ACTION_PRIMARYTARGET` | 7218 / Parameters | 1148 / 1453 / 1797 | Ordinary |
| `ACTION_SHOW` | -1 / NoPage | — / — / — | NoPage |
| `ACTION_MOODS` | 7228 / Parameters | 1177 / 1497 / 1838 | Ordinary |
| `ACTION_SYSTEMMESSAGES` | 7229 / Parameters | 1190 / 1516 / 1858 | Ordinary |
| `ACTION_SHOWNEXTROOM` | 7215 / Parameters | 1152 / 1468 / 1805 | Ordinary |
| `ACTION_TOGGLEEFFECT` | 7232 / Parameters | 1208 / 1537 / 1880 | Ordinary |
| `ACTION_CHANGEOWNER` | 7221 / Parameters | 1157 / 1472 / 1812 | Spatial |
| `ACTION_SLAPSREMAINING` | 7216 / Parameters | 1163 / 1481 / 1820 | Ordinary |
| `ACTION_TIMERSPEECH` | 7230 / Parameters | 1181 / 1502 / 1844 | Ordinary |

У условий source — `Source/Sources/Triggers/Triggers_Triggers.c`; у действий —
`Source/Sources/Triggers/Triggers_Actions.c`. NoPage не получает придуманных
полей; RepeatOnly допускает только исходный Repeat byte, остальные imported
operands не меняются. Parameters сопоставлены с текущими control/alias/compound
providers. [Общий scope](I10_CODE_CLOSURE.md).

## Конечные stateful callback families

| ID | Контекст / обработчик | Итог |
| --- | --- | --- |
| C01 | Type selection/defaults, same-type retention, scope restrictions | Trigger/Action DlgList, DlgInit, DlgSetDefault; existing event identity/link retention, no-page routing and category remembered state. [Доказательства](SCENARIO_EXISTING_EVENT_WIZARD_SOURCE_SCOPE.md) |
| C02 | Active-page initialization and collection prerequisites | 69 active Init/Check/Store rows; twelve explicit collection prerequisites and physical IDs, distinct from inactive SHOWCRYSTAL. [Доказательства](SCENARIO_PREREQUISITES.md) |
| C03 | Variable literal/alias and arithmetic mode switching | Source206/207 and200..205/212/214 change active operand, stable slot and name. Private alias+event commit corrects early source global writes; empty inline name does not erase. [Доказательства](SCENARIO_ALIAS_PARENT_WINDOWS.md) |
| C04 | Terrain-dependent owner and named choice states | AreaContent and ChangeSquare owner disable/fixed1 or2; no-selection and unknown-import policies explicit; current source-backed choices and images are covered by I13 list caller audit. [Доказательства](SCENARIO_DEPENDENT_INPUTS.md) |
| C05 | Duration input and Repeat notifications | 700/701/702 duration clamps and total; Repeat Once1/Count0..254/Infinite255. Existing numeric projections retained without rerun. [Доказательства](SCENARIO_CONTROL_PAGES_SOURCE_SCOPE.md) |
| C06 | Band membership hints and Any sentinel | Three source LVN_ITEMCHANGED counts and scoped band previews. Zero band ID is a valid identity, distinct from Any; displayed members do not become selectable band IDs. [Доказательства](SCENARIO_BAND_PREVIEWS.md) |
| C07 | Area/point/spatial rectangle callbacks | 311 normalized area top-left,312 point picker,315 rectangle/314 picker; existing-edit rectangle blocked, creation expansion explicit; x/y offsets remain on the private parent. [Доказательства](SCENARIO_SPATIAL_PAGES.md) |
| C08 | Spawn child flags and identity | 313 child flags private and source-active, inactive/unknown bits retained; creation/editing parents and late child authority remain guarded. [Доказательства](SCENARIO_SPAWN_PARENT_WINDOWS.md) |
| C09 | Category/ID paired controls | 49 AddElement and54 ShowElement options, plus ClickElement paired operands atomically updated; choice row carries encoded identity independent of translated text. [Доказательства](SCENARIO_NAMED_ACTION_WINDOWS.md) |
| C10 | ReleaseBand dependency | Source500/501 unchecked/disabled state composes to the existing three values0/1/2; modern combo has equivalent Store semantics without hidden reset. [Доказательства](SCENARIO_REMAINING_PARAMETER_PAGES.md) |
| C11 | Camera child ShowCell inversion | 315 library picker,52 paths, ID and ceiling choice committed privately; source200=true/201=false inversion retained. [Доказательства](SCENARIO_CAMERA_PARENT_WINDOWS.md) |
| C12 | Speech selection, text and audio child lifetime | 401 selection/double-click uses explicit one-based speech ID and current bank; suppress-text flag correct; corresponding text/sound lifetime stays separate from map mutation. Actual playback remains manual. [Доказательства](SCENARIO_SPEECH_PAGE.md) |
| C13 | Native DestroyGate pair/physical identity | Source project ID versus native pair(+1 coordinates), hearts excluded; unknown/ambiguous import displayed literally, no invented ID in padding. Physical target key transaction and stale checks retained. [Доказательства](NATIVE_GATE_BINDINGS.md) |
| C14 | Back/Next/Finish and Repeat page | Source Back ignores failed Store result; current Back attempts private Apply then returns to Type. Next/Finish check success; existing-edit retains identity, creation expands only when explicit. Parent Cancel is atomic, fixing early source alias writes. [Доказательства](SCENARIO_PARAMETER_SAVE.md) |
| C15 | Descriptions, linked context, stale windows and translation | Accepted103 schema/127 descriptions and138 context/numeric projections are dated evidence. Current refresh preserves explicit row/physical identity and pending inputs; owner/revision/resource changes revoke child authority. GUI rendering/input remains M18/M23. [Доказательства](SCENARIO_DESCRIPTIONS.md) |

Имена, SHA256 текущих providers и первичных файлов; полные19 function bodies;
все case lines/group bodies и ссылки на датированные evidence находятся в
`artifacts/Evidence/ScenarioCallbackMatrix-20261025/i10-closure-proof.json`.
Это конечная кодовая матрица, не физическое исполнение всех callbacks.
