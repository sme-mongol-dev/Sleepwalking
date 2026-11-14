# Audio integration contract

Owner: `Assets/SleepwalkingAudio/`. The shared scene belongs to Game Mechanics. The deliverable is the `Resources/SleepwalkingAudioDirector.prefab` prefab and its referenced `SleepwalkingAudioCatalog.asset`. No edits to the shared scene are needed.

## Quick start

```csharp
using Sleepwalking.Audio;

SleepwalkingAudioDirector audio = SleepwalkingAudioDirector.CreateOrGet();
audio.EnterRoom();                        // 3D room/main menu: music on, dream ambience off
audio.Play(SoundCue.MenuHover);           // random variant, avoids immediately repeating one
audio.Play(SoundCue.MenuConfirm, 1);      // exact zero-based variant for morning comparison
audio.EnterDream(DreamTheme.DeepWater);   // music off, underwater ambience on
audio.Play(SoundCue.Jump);
audio.Play(SoundCue.TrapHit, 2);
audio.SetDreamTheme(DreamTheme.Heaven);   // change the active dream's ambience
audio.EnterRoom();                        // wake in room, music back on
```

Call `CreateOrGet()` once in the room scene or at startup. It loads the prefab from Resources, creates one persistent instance, and returns the existing instance after scene changes. `EnterRoom()` should be called when the room is ready; `EnterDream(theme)` when the sleep cutscene gives control to a level. The menu music source is stopped and its clip cleared in `EnterDream`, `StopAll`, and any dream style change. The ambient source is stopped and cleared in `EnterRoom`. The prefab does not autoplay. Audio is 2D, so it does not require an `AudioListener` reference (the scene still needs one listener for playback).

Gameplay cues: `Jump`, `Dash`, `WallGrab`, `Landing`, `TrapHit`, `Collision`, `Button`. UI cues: `MenuHover`, `MenuConfirm`, `MenuBack`. `GetVariantCount(cue)` gives the choice count; `Play(cue, index)` returns `false` for an invalid index. Each cue's clip list and gain can be changed in `SleepwalkingAudioCatalog.asset` without renaming or editing audio files. The four dream ambience entries follow the `DreamTheme` enum order (`Meat`, `PostApocalypse`, `DeepWater`, `Heaven`). The unused `sfx100v2_loop_water_02.ogg` is a ready alternate for DeepWater; assign it in the catalog inspector to compare.

For the current UI settings, use `audio.SetVolumes(ui.MusicVolume, ui.SfxVolume * 0.32f, ui.SfxVolume)` on initialization and each slider change. The arguments are normalized music, ambience, and effects volumes. The prefab defaults are music `0.32`, ambience `0.22`, effects `0.70`; per-cue gains range `0.22–0.58`, so a single effect source peaks at `0.406` before the clip's own signal level. Eight effects can overlap. A final listening pass in the Windows build is still required to judge perceived balance on actual output hardware.

## Included sources and rights

All sources below are explicitly marked **Creative Commons CC0** on their individual pages. CC0 permits copying, modification, and use in a commercial game and Windows build without attribution. Author/source credit remains courteous. This is a license check, not a claim that “free” or “no copyright” labels grant rights. The legal text is [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/). Source and direct file links were checked on 14 November 2026.

| Use / files in project | Author and source page | Direct original download | License |
| --- | --- | --- | --- |
| Meat: `meat_horror.ogg` | techiew, [Ambient horror](https://opengameart.org/content/ambient-horror) | [OGG](https://opengameart.org/sites/default/files/ambient_horror_0.ogg) | CC0 |
| Postapocalypse: `postapoc_generator.wav` | bart, [Steam boiler sound loop](https://opengameart.org/content/steam-boiler-sound-loop) | [WAV](https://opengameart.org/sites/default/files/generator_loop.wav) | CC0 |
| DeepWater: `sfx100v2_loop_water_01.ogg`, alternate `_02` | rubberduck, [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) | [ZIP](https://opengameart.org/sites/default/files/sfx_100_v2.zip) | CC0 |
| Heaven: `heaven_wind.ogg` | SketchMan3, [wind whoosh loop](https://opengameart.org/content/wind-whoosh-loop) | [OGG](https://opengameart.org/sites/default/files/wind%20woosh%20loop.ogg) | CC0 |
| Room menu music: `menu_horror.ogg` | Cleyton Kauffman, [Ambient Horror Track 01](https://opengameart.org/content/ambient-horror-track-01) | [ZIP with OGG and WAV](https://opengameart.org/sites/default/files/ambient_horror_track_01_0.zip) | CC0 |
| Player, collision, button, menu variants: `sfx100v2_*` in `Actions/` and `Menu/` | rubberduck, [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) | [ZIP](https://opengameart.org/sites/default/files/sfx_100_v2.zip) | CC0 |

The OpenGameArt menu track page includes a suggested author credit while its license field says CC0; the suggestion is honored here and is not an extra legal requirement. The soundscape loops are routed through `DreamAmbience` and contain no menu music. Attribution links may be copied into the in-game Credits page if desired.

## Rebuild and checks

The included editor menu action `Sleepwalking > Audio > Rebuild catalog and prefab` recreates serialized references after moving clips. `Sleepwalking > Audio > Validate catalog` checks every theme and at least two working clips per cue. `SleepwalkingAudioBuilder.Rebuild` is also callable with Unity `-batchmode -executeMethod` for a repeatable import. The builder writes only under `Assets/SleepwalkingAudio/`.
