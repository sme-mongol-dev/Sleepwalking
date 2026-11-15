# Menu decorations and Odradek

The active menu room is `Assets/SleepwalkingVisual/Prefabs/SleepwalkingMenuRoom.prefab`. Its root `RoomEnvironment` has an editable `Decorations` list. Each row has a `MinOdradek` from 1 to 100, a room object or prefab reference, local placement, scale, and renderer sorting order. `RoomEnvironment` updates visibility when `SleepwalkingGame.Odradek` changes and when the room becomes active again. The four `RoomHotspot` objects (bed, books, clock, door) remain active at every value.

Default thresholds: window 1, nightstand 1, calendar 25, poster 1, both clock arrows 1, slippers 35, eye 55, eyeball 75. Change these values on the prefab; no code edit is needed. To add a new decoration, add a row, assign a prefab in `Prefab`, leave `RoomObject` empty, set `MinOdradek` and local transform. An empty `Parent` places it under the room root. `Build Textured Menu` preserves edited threshold, placement, and sorting values for named rows, as well as custom prefab rows, when it regenerates the room.

The five supplied PNGs are kept unmodified in `Assets/SleepwalkingVisual/Textures/Source/MenuDecor/`. Palette-mapped copies used by the room are in `Assets/SleepwalkingVisual/Textures/MenuDecor/`. `Tools/Visual/import_menu_decor.ps1` reproduces the copies. They retain source alpha and use point filtering without mipmaps.

`RoomOdradekDecorationTests` is the PlayMode check for the low/high thresholds, four hotspots, and 1280×720 room captures. It writes `Docs/QA/VisualLogs/MenuOdradek1.png` and `MenuOdradek100.png` when run.
