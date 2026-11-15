# Menu decorations and Odradek

The authoritative JSON schema, file locations, validation rules, and HTML editor contract are in `Docs/Integration/Visual.md` under **Odradek menu decoration JSON**. The complete default table is `Assets/StreamingAssets/SleepwalkingMenuDecor.json` (36 stable rows).

The active room is `Assets/SleepwalkingVisual/Prefabs/SleepwalkingMenuRoom.prefab`. Its `RoomEnvironment.Decorations` rows keep the Unity object, texture, placement, and sorting references. JSON overrides each row's inclusive `min`/`max` and `asset` by stable `id`; no C# edit is needed. `poster` and `bed` select exactly one texture at every value from 1 to 100. By default, `calendar` appears from 25 onward; the JSON can move its first appearance to any value. Five `cracks` rows accumulate at 35/50/65/80/95. The bed remains the same interactive GameObject when its visual frame changes.

Original supplied PNGs are preserved in `Assets/SleepwalkingVisual/Textures/Source/MenuDecor/`. Point-filtered palette copies, including eight sliced 256×256 bed frames, are in `Assets/SleepwalkingVisual/Textures/MenuDecor/`. `Tools/Visual/import_menu_decor.ps1` reproduces them.

Each of the 12 posters has its own alpha mask in `Assets/SleepwalkingVisual/Textures/Posters/Masks/` and a ready-to-use image in `Posters/Masked/`. `Tools/Visual/apply_poster_masks.ps1` reapplies the masks. Every processed poster is 40×80 or 80×160; poster 09 was normalized to 80×160 with nearest-neighbor scaling.

`RoomOdradekDecorationTests` checks Odradek 1/50/75/100, the four menu hotspots, variant textures, accumulation, and JSON overrides. It writes 1280×720 room captures into `Docs/QA/VisualLogs/MenuOdradek1.png`, `MenuOdradek50.png`, `MenuOdradek75.png`, and `MenuOdradek100.png` when run with a graphics device.
