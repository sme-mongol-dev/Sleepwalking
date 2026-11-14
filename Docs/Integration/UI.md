# UI integration contract

Owner: `Assets/SleepwalkingUI/`. The shared gameplay scene is owned by Game Mechanics. Add a root GameObject with `SleepwalkingUI` to that scene; it builds its Canvas when first enabled. Do not use the UI showcase scene as the gameplay scene.

## Room objects

The room prefab belongs to Visual Director. Its bed/hero, clock, bookshelf, and door have `Collider` plus `Sleepwalking.Visual.RoomHotspot` with `Action` (`Dream`, `Settings`, `Credits`, `Quit`) and `Label`. The UI reads this component directly; no extra target component is needed on the Visual prefab. Set `roomCamera` on the UI to the camera rendering the room (the main camera is the fallback). The UI raycasts colliders under the pointer, shows the English action label beside the pointer, and activates the action on click. Tab/Right Arrow cycles room targets, places the label near the focused object with a blue border, and Enter/Space activates the selected item. The lower instruction changes with hover or keyboard focus. Escape closes overlays. The UI does not create the production room geometry.

For an orthographic room camera, the UI widens its view only as needed to keep every active room hotspot inside the frame when the window becomes narrow. It keeps the camera's original orthographic size as the minimum and recalculates on aspect changes or `EnterRoom`. Call `EnterRoom` after the room prefab and its hotspots exist, including after every wake transition. The sleep transition can still animate the camera after the room selection.

## Game Mechanics API

```csharp
ui.DreamRequested += StartSleepingCutscene;
ui.RestartRequested += RestartCurrentDream;
ui.MainMenuRequested += ReturnToRoom;
ui.SetDreamsCompleted(completedDreams); // синхронизация подписи после загрузки сцены
ui.EnterRoom();      // комната видима, игра идёт
ui.EnterDream();     // скрыть меню после кат-сцены
ui.OpenPause();      // остановить время и открыть паузу
ui.CloseOverlay();   // закрыть экран или продолжить игру
bool blocked = ui.BlocksGameplayInput; // запрет ввода геймплея под меню
```

`DreamRequested` is a request, not an automatic scene load. Connect it to the sleep cutscene and level transition. `RestartRequested` and `MainMenuRequested` are raised only after their confirmations. Call `SetDreamsCompleted` with the actual completed count when entering the room, then `EnterRoom` after the wake transition. `EnterDream` should be called when the cutscene hands control to gameplay. The UI owns `Time.timeScale` while paused and restores its prior value on resume or scene teardown. `MusicVolume` and `SfxVolume` are normalized 0–1 values from saved settings.

## UI showcase

`Assets/Scenes/SleepwalkingUI.unity` is the standalone UI showcase. It contains a simple editable demonstration room using `SleepwalkingRoomTarget` and `PixelRoomView` so bed/clock/bookshelf/door interactions can be tried in the pixel output before the Visual Director prefab is connected. Its `SleepwalkingUIShowcase` handler enters the dream view when the bed is selected, allowing Escape, pause, settings, restart, and main-menu confirmation to be exercised. This room is not a replacement for the Visual Director prefab.

The active screens are room menu, pause, settings, credits, and confirmations. There are no Victory or Game Over screens; traps and beds are handled by gameplay.

## Fonts and sound

The bundled fonts in `Assets/SleepwalkingUI/Resources/SleepwalkingFonts/` come from the [Google Fonts Cormorant Garamond](https://github.com/google/fonts/tree/main/ofl/cormorantgaramond) and [Space Mono](https://github.com/google/fonts/tree/main/ofl/spacemono) family directories. Both OFL license texts are included beside the fonts. The UI falls back to Georgia and Consolas when these resources cannot load.

The UI plays `MenuHover`, `MenuConfirm`, and `MenuBack` through `SleepwalkingAudioDirector.Instance` when the audio director exists. The Game Mechanics scene creates the director and calls its room/dream state transitions. Settings propagate normalized music, ambience, and effects levels to that instance.
