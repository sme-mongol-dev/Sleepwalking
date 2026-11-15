# Menu decorations and Odradek

The authoritative JSON schema, file locations, validation rules, and HTML editor contract are in `Docs/Integration/Visual.md` under **Odradek menu decoration JSON**. The complete default table is `Assets/StreamingAssets/SleepwalkingMenuDecor.json` (36 stable rows).

The active room is `Assets/SleepwalkingVisual/Prefabs/SleepwalkingMenuRoom.prefab`. Its `RoomEnvironment.Decorations` rows keep the Unity object, texture, placement, and sorting references. JSON overrides each row's inclusive `min`/`max` and `asset` by stable `id`; no C# edit is needed. `poster` and `bed` select exactly one texture at every value from 1 to 100. By default, `calendar` appears from 25 onward; the JSON can move its first appearance to any value. Only cracks 4 and 5 are used: each has three mutually exclusive growth stages at 0.3, 0.6, and 1 of the saved object scale. Their current Odradek ranges are placeholders for the level designer. Eye and pupil share one `eye` row; the pupil follows the menu cursor. The bed remains the same interactive GameObject when its visual frame changes.

Original supplied PNGs are preserved in `Assets/SleepwalkingVisual/Textures/Source/MenuDecor/`. Crack 4 and 5 materials use these original dark PNGs. Point-filtered palette copies, including eight sliced 256×256 bed frames, are in `Assets/SleepwalkingVisual/Textures/MenuDecor/`.

The bed uses `RoomBedAwakeAnimation` while the room is active. Before the first dream, the sleeping and breathing sequence loops. After entering a dream, the looking sequence plays forward then backward, omitting its last frame where the eyes close. The `bed` Odradek rows remain valid as static fallback textures in the room catalog, but the animation controls the visible bed texture while the menu is running. This does not replace the bed hotspot or its white hover highlight.

Each of the 12 posters has its own alpha mask in `Assets/SleepwalkingVisual/Textures/Posters/Masks/` and a ready-to-use image in `Posters/Masked/`. `Tools/Visual/apply_poster_masks.ps1` reapplies the masks. Every processed poster is 40×80 or 80×160; poster 09 was normalized to 80×160 with nearest-neighbor scaling.

`RoomOdradekDecorationTests` checks Odradek 1/50/75/100, the four menu hotspots, variant textures, all six crack growth thresholds, cursor tracking, and JSON overrides. It writes 1280×720 room captures into `Docs/QA/VisualLogs/MenuOdradek1.png`, `MenuOdradek50.png`, `MenuOdradek75.png`, and `MenuOdradek100.png` when run with a graphics device.
