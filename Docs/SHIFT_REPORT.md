# Итог ночной смены — 14 ноября 2026

## Готовые артефакты

- Общая Unity-сцена: `Assets/Scenes/Sleepwalking.unity`. Windows Player: `Builds/Windows/Sleepwalking.exe`.
- Редактируемая 3D-комната с объектами меню и пиксельной камерой: `Assets/SleepwalkingVisual/Prefabs/SleepwalkingRoom.prefab`; отдельный предпросмотр: `Assets/SleepwalkingVisual/Preview/RoomPreview.unity`.
- Альтернатива меню с иллюстрированным героем: `Assets/SleepwalkingVisual/Prefabs/SleepwalkingRoom_IllustratedHero.prefab`, отдельный Player `Builds/VisualPreview/SleepwalkingIllustrated.exe`. Исходный и обработанный Tessera ассеты находятся в `Assets/SleepwalkingVisual/Textures/Source/` и `Processed/`.
- Семь префабов неизменной геометрии: `Assets/SleepwalkingLevels/Prefabs/L01_DreamGeometry.prefab` … `L07_DreamGeometry.prefab`.
- Четыре сменяемые стилистики сна: `Assets/SleepwalkingVisual/Prefabs/Themes/`.
- UI, геймплей, аудио и контракты подключения: `Assets/SleepwalkingUI/`, `Assets/SleepwalkingGameplay/`, `Assets/SleepwalkingAudio/`, `Docs/Integration/`.

## Что проверено

| Проверка | Результат | Доказательство |
| --- | --- | --- |
| Основной и альтернативный Windows Player | Unity `Succeeded`, 0 ошибок в обоих билдах после финального polish меню | `Docs/QA/VisualLogs/RoomLogoIntegratedMainBuild.log`, `Docs/QA/VisualLogs/RoomLogoIntegratedIllustratedBuild.log` |
| Структура семи уровней и триггеров | Gate прошёл в Unity | `Docs/QA/shift-report.md` |
| Запуск Windows Player | Комната-меню видна в обеих сборках | `Docs/QA/player-screen-room-logo-final-main-16x9.png`, `Docs/QA/player-screen-room-logo-final-illustrated-16x9.png` |
| Разрешения 1280×720 и 1024×768 | Подзаголовок читается, кровать/часы/полка/дверь видны; логотип не пересекает верхнюю линию в 4:3; оба варианта героя сняты отдельно | `Docs/QA/player-screen-room-logo-final-main-16x9.png`, `Docs/QA/player-screen-room-logo-final-main-4x3.png`, `Docs/QA/player-screen-room-logo-final-illustrated-16x9.png`, `Docs/QA/player-screen-room-logo-final-illustrated-4x3.png` |
| Восемь переходов сон → комната | PlayMode-тест прошёл; восьмой вход использует первую геометрию | `Docs/QA/playmode-cycle-ui-results.xml` |
| Связь UI и пауза | Активация hotspot кровати через UI запускает первый сон; Pause останавливает время, CloseOverlay восстанавливает | `Docs/QA/playmode-cycle-ui-results.xml` |
| Смерть и следы | API-смерть создаёт след; Restart сохраняет, выход очищает | `Assets/SleepwalkingTests/PlayMode/SleepwalkingCycleTests.cs`, `Docs/QA/playmode-cycle-ui-results.xml` |
| Временная/движущаяся платформа и ширина лазера | PlayMode: 3 пройдено, 0 ошибок, 1 пропущен (L07 не содержит движущейся платформы) | `Docs/QA/mechanism-physics-rerun-results.xml`, `Docs/QA/MechanicsLead.md` |
| Игровые кадры | Опоры L01 лучше отделяются от фона; дальний L07 закрыт темой по всем краям | `Docs/QA/dream-L01-final-main.png`, `Docs/QA/dream-L07-far-final-main.png`, `Docs/QA/visual-polish-scorecard.md` |

Подробные независимые проверки направлений: `Docs/QA/shift-report.md`, `Docs/QA/Visual.md`, `Docs/QA/MechanicsLead.md` и отчёты в `Docs/Integration/`.

Художественная граница: меню с иллюстрированным героем выразительнее базового варианта, оба работают как отдельные prefab/Player. После дополнительного прохода дверь с ручкой видна отдельно от окна, а логотип в 4:3 не пересекает верхнюю линию; Art lead не нашёл визуальных регрессий P0/P1 на статичных кадрах. Кадры уровней остаются визуально прототипными, персонаж мелок на 320×240. Фактурный пилот по `TL_Meat_Dream.png` отложен как отдельный `MeatTexturePilot.prefab`: Art lead счёл выигрыш недостаточным; основная тема не заменена.

## Что осталось проверить в интерактивной игре

- Наведение и нажатие на предметы комнаты, клавиатурная навигация UI, пауза и настройки.
- Управление персонажем, зацепы, рывок, столкновения с каждым типом ловушки, срабатывание кнопки, движение платформ, видимая толщина лазера и звук.
- Прохождение каждого уровня собственными действиями игрока и визуальные переходы сна/пробуждения.

Computer Use запрос потока механик истёк без подтверждения (`Computer Use app approval timed out`), поэтому GUI-прогон не состоялся. Независимый 12-секундный smoke Player в `Docs/QA/MechanicsLead.md` подтвердил запуск без записанных исключений, но не управление. Альтернативный захват собственного окна Player дал кадры; синтетический ввод Windows до Unity не дошёл. Незавершённые интерактивные сценарии не считаются пройденными. Подробности и воспроизведение — в `Docs/QA/shift-report.md`.

Задачи с точным названием `Документация`, однозначно привязанной к проекту `T:\Sleepwalking`, не найдено; сообщение в задачи других проектов не отправлялось.

## Дополнение: тайловые уровни, 14 ноября

По уточнению владельца четыре присланных файла `TL_Meat_Dream.png`, `TL_PostApoc_Dream.png`, `TL_DeepWater_Dream.png`, `TL_Heaven_Dream.png` используются как рабочие тайлсеты. Их копии в `Assets/SleepwalkingVisual/Textures/Tilesets/` совпадают с исходниками по SHA-256. У каждого сна два визуальных слоя: отдельный фон без коллайдера и основной игровой слой из тайлов. Все семь префабов получили маркеры тайловых поверхностей; структура и прежние 2D-коллайдеры прошли проверки 7/7. Подробный контракт — `Docs/Integration/Levels.md` и `Docs/Integration/Visual.md`.

Первый вариант L01 был отклонён: тайлы выглядели узкой наклейкой на однотонных блоках. После переделки они заполняют видимую толщину платформ; фон отделён, временный игровой силуэт читается в 16:9 и 4:3. Кадры: `Docs/QA/player-screen-l01-tiles-dreamer-16x9.png`, `Docs/QA/player-screen-l01-tiles-dreamer-4x3.png`. Отдельная витрина показывает четыре темы на той же геометрии: `Docs/QA/player-screen-l01-{meat,postapoc,deepwater,heaven}-showcase-16x9.png`. Это принудительный выбор стиля для визуальной проверки, а не назначение первого цикла.

Кадры L02–L07 зафиксированы в `Docs/QA/Visual.md`. В L04 устранён видимый зазор под кроватью без изменения её триггера; L05 проверен с включёнными и скрытыми временными площадками. QA-ввод в отдельном preview сдвинул героя по X с 1.000 до 1.865 и сохранил временный спрайт на актёре. Эти проверки не подтверждают полное прохождение, прыжок, рывок или все столкновения.

Сборки на этапе исправления L04: основной `Builds/Windows/Sleepwalking.exe` (`Docs/QA/VisualLogs/DreamTilesL04FinalWindowsBuild.log`) и вариант с иллюстрированным героем `Builds/VisualPreview/SleepwalkingIllustrated.exe` (`Docs/QA/VisualLogs/IllustratedTilesL04WindowsBuild.log`). Обе собраны Unity с 0 ошибками. В альтернативном Player отдельно проверены меню и переход по кровати в L01; кадры `Docs/QA/player-screen-illustrated-tiles-final-menu-16x9.png` и `Docs/QA/player-screen-illustrated-tiles-final-l01-16x9.png`. Более новые сборки после калибровки указаны ниже.

Допущение по цветам: текущий вариант использует исходные цвета TL PNG по последнему уточнению владельца. Ранее была задана общая ограниченная палитра; решение о ремапе этих тайлсетов остаётся открытым. Исходники сохранены для такой обработки.

## Дополнение: калибровка движения и расстояний

Для настройки прыжков подготовлены три редактируемых профиля в `Assets/SleepwalkingGameplay/Profiles/`: `01_Controlled` (бег 7.6, импульс прыжка 11.8, гравитация 3.3, dash 17.5 × 0.17 с), `02_Responsive` (8.5, 12.0, 3.2, 18 × 0.18 с) и `03_Aggressive` (9.4, 12.15, 3.35, 20 × 0.19 с). Профиль по умолчанию — `02_Responsive`; его выбор записан в сцене и генераторе. Геометрия L05/L06 исправлена после измерений: временные ступени L05 требуют последовательного подъёма, а L06 — поездки на горизонтальной и вертикальной платформах. Подробные размеры, замеры и причина выбора — `Docs/Integration/MovementCalibration.md`.

Независимый QA прошёл **22/22 предусмотренных перехода** пофазными PlayMode-сценариями с настоящим `SleepwalkingPlayer` и `Rigidbody2D`: L05/L06 на Controlled и Responsive (`Docs/QA/traversal-sequence-exit-contact-results.xml`, 4/4), L01–L04/L07 на Responsive (`Docs/QA/traversal-remaining-results.xml`, 5/5). Посадки требовали трёх последовательных физических контактов, безопасной зоны и запаса от края не менее 0.15 world; маршруты начинались у авторского старта и заканчивались контактом с кроватью. Три дополнительные попытки dash-обхода L05/L06 закончились смертью до целевых опор (`Docs/QA/traversal-dash-bypass-results.xml`, 1/1); это результат для заданных расписаний ввода, а не доказательство отсутствия любого обхода. Поэтапные контакты и ограничения описаны в `Docs/QA/traversal-calibration-results.md`.

В L06 добавлена видимая стрелка ожидания у стартовой опоры: когда движущаяся платформа за кадром, направление остаётся понятным. Знак не имеет коллайдера; после правки структура и коллайдеры прошли 7/7, у L06 осталось 9 коллайдеров. Кадры Player: `Docs/QA/player-screen-final-l06-chevron-near-16x9.png` и `Docs/QA/player-screen-final-l06-chevron-far-16x9.png`. После этой графической правки основной и альтернативный Windows Player пересобраны без ошибок: `Docs/QA/VisualLogs/ResponsiveChevronMainWindowsBuild.log` и `ResponsiveChevronIllustratedWindowsBuild.log`. Игровые managed DLL обоих Player совпадают побайтно.

Эти результаты подтверждают физическую достижимость задуманного маршрута при записанном вводе. Ручное ощущение управления с клавиатуры, полный непрерывный сеанс семи снов и визуальная приёмка во время реальной игры пока не проверены.
