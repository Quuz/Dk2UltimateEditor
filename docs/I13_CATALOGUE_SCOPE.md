# Граница и закрытие I13: каталоги, подписи и изображения

Актуально на 23 октября 2026: **I13 закрыт по коду**.
Заключительный этап — [сверка 70 функций заполнения списков](UI_LIST_CALLER_AUDIT.md).
Ручная приёмка остаётся открытой. Перечень ограничен текущими 23 возможностями
и их исходными controls; неизвестной игровой семантике значения не придуманы.

| Раздел | Конечное подтверждение |
| --- | --- |
| Global definitions/targets | [184 строки и 116 targets](GLOBAL_DEFINITION_IMAGES.md), [used-by123](CREATURE_SPELL_USAGE.md), [20 enum tables / 26 contexts](GLOBAL_CATALOGUE_CAPTION_AUDIT.md). |
| Effect/variable names | [371 effects, 193 variables, 20 groups](EFFECT_VARIABLE_CAPTION_AUDIT.md): source ID/order и EN/RU. |
| Scenario choices | [38 image contexts](SCENARIO_CATALOGUE_IMAGES.md), [50 radio/enum contexts](SCENARIO_RADIO_CAPTION_AUDIT.md), [состав отрядов](SCENARIO_BAND_PREVIEWS.md), [обе страницы владельцев](SCENARIO_POINT_BAND_DETAILS.md). |
| Editable field labels | [27551 schema occurrences / 14 historical captions](FIELD_LABEL_RESOURCE_AUDIT.md), [AI](PLAYER_AI_FIELD_CAPTIONS.md), [global elements](GLOBAL_ELEMENT_FIELD_CAPTIONS.md), [creatures](CREATURE_FIELD_CAPTIONS.md), [raw/typed routes](DEFINITION_CAPTION_ROUTES.md). |
| Custom and map captions | [Placed things](PLACED_THING_CAPTIONS.md), [regions/effects](SPATIAL_CUSTOM_CAPTIONS.md), [map metadata](MAP_UI_CAPTIONS.md), [brush/player/generator](BRUSH_PLAYER_GATE_CAPTION_AUDIT.md), [service captions](SERVICE_CAPTION_AUDIT.md). |
| Palette/rules/rewards | [53 terrain rows / 12 tools](MAP_PALETTE_IMAGES.md), [139 rule rows / 23 fills](MAP_RULE_FILL_IMAGES.md), [AI rooms](AI_ROOM_PRIORITY_IMAGES.md), [reward/destination images](REWARD_DESTINATION_IMAGES.md). |
| Object/list displays | [Object chooser](OBJECT_IMAGES.md), [templates/properties](PROPERTY_IMAGES.md), [bands](HERO_BAND_DISPLAY.md), [patrol/tree/preview](EDITOR_LIST_IMAGES.md), [point owners](SCENARIO_POINT_OWNER_IMAGES.md), [direct list consumers](DIRECT_LIST_IMAGES.md). |
| Remaining image consumers | [Direction/anchor/spatial buttons](DIRECTIONAL_CONTROL_IMAGES.md), [status/gold/legend](MAP_INFO_IMAGES.md), [requests/cameras](REQUEST_CAMERA_IMAGES.md), [native chooser preview](FILE_CHOOSER_PREVIEW.md). |
| Document and reference details | [Live name/author/dimensions/path, 7 historical metadata layouts and 27 typed reference fields](DOCUMENT_REFERENCE_PRESENTATION.md); ambiguity/None/namespaces сохраняются. |
| Final source population callers | [315 sites / 70 functions / 31 families](UI_LIST_CALLER_AUDIT.md), отдельные matrix 81 allocation calls и 51 direct API calls. Каждая функция имеет текущий route, прежнее доказательство и границу вывода. |

Это закрытие подписей, каталогов и изображений, а не всех ветвей перечисленных
функций. Writers/validation, identities, сценарные операции, преобразования,
статистика и исторические форматы остаются в своих открытых I-пунктах.
Все 23 M-группы сохранены; совпадение ресурсов или CLI-проверка не подтверждает
настоящие окна, DPI, звук и игру. Датированные заявления «I13 открыт» в прежних
отчётах описывают соответствующий старый этап и не создают новый остаток.
