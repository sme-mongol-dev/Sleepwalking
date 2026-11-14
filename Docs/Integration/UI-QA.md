# UI verification — 14 November 2026

Scope: `Assets/SleepwalkingUI/`, `Assets/Scenes/SleepwalkingUI.unity`, and `Docs/Integration/UI.md`. The Game Mechanics assembly scene and Visual Director room prefab are separate deliverables.

## Independent UI lead review

The UI lead inspected the implementation against `Docs/SHIFT_PLAN.md`, `Docs/MVP.md`, and `Docs/UI/UI_BRIEF.md` and reported these issues. Each was changed before final verification:

| Finding | Resolution |
| --- | --- |
| Room raycast used window coordinates against PixelRoomView's smaller RenderTexture | Pointer coordinates now scale by `camera.pixelWidth/Screen.width` and `camera.pixelHeight/Screen.height`. |
| Serialized scene still had Victory and Game Over, no room targets or hover label | Rebuilt only `SleepwalkingUI.unity`; inspected YAML contains four `SleepwalkingRoomTarget` actions, `Room Label`, pause/settings/credits/confirmation, and no old outcome screens. |
| UI contract mentioned a prefab that did not exist | Contract now instructs Game Mechanics to add `SleepwalkingUI` to its own scene. |
| Standalone bed action had no listener | `SleepwalkingUIShowcase` handles dream, restart and room requests in the showcase scene. |
| Credits screen lacked content | Added game jam and verified sound-source credits from `Docs/Integration/Audio.md`. Team names have not been supplied. |
| `NEXT DREAM` was saved across launches | Label derives from session state; Game Mechanics can call `SetDreamsCompleted` after a scene load. |
| Resolution options stayed open after Back/Escape | Closing Settings now closes the options list. |
| Keyboard room focus did not play hover sound | Cycling to a new target plays `MenuHover`. |
| Resolution options reused one Image for border and fill, hiding the focus colour | Each option now has a separate inner Fill Image so hover and keyboard focus remain visible. |
| UI did not play menu sounds | It now uses `MenuHover`, `MenuConfirm`, and `MenuBack` from an existing audio director, with no hard requirement that one exist. |
| Showcase camera lacked pixel output | Builder adds `PixelRoomView`; showcase also adds it on enable if an older serialized scene lacks it. |
| `BlocksGameplayInput` could be read before `Awake` | Null checks protect overlay references. |
| Log files were accidentally placed under Assets | Removed the temporary log and its `.meta`. |

## Executed checks

- Final `dotnet build Assembly-CSharp-Editor.csproj -v:q` (which also builds `Assembly-CSharp.csproj`): passed with 0 errors; one CS0649 warning in Sound Design's `SleepwalkingAudioDirector.catalog` (Unity serialized field). A separate runtime assembly build also passed with 0 errors and 0 warnings before the final small changes.
- UI lead's serialized-scene inspection: four room targets with actions 0–3, assigned camera, showcase handler, Input System EventSystem, relevant screens and controls; no Victory, Game Over, or opaque Dream Background.
- Unity 6000.3.28f1 batch command `SleepwalkingSceneBuilder.Build` completed with return code 0. The [build log](UI-final-build-2.log) records `Sleepwalking UI showcase updated: Assets/Scenes/SleepwalkingUI.unity`. Final YAML has `Sleepwalking.Visual.PixelRoomView`, `Room Label`, `Credits Body`, four targets, and no old outcome screens.

## Runtime check

Tester launched `Builds/Windows/Sleepwalking.exe` for 12 seconds on 14 November 2026. The [Player log](../QA/player-smoke-elevated.log) shows Unity, D3D12 and Physics initialization without a gameplay exception; SHA-256 of the exe was `FBD073CA59C4918EDBC6704F5393CF59021173AF11714C00DB4212C50117492B`. This confirms startup only.

The UI lead's interactive Play check of hover/click, keyboard focus, navigation, pause, settings, and confirmations remains unperformed because the GUI control helper was occupied by another active request. QA later captured the 1280×720 and 1024×768 menu screenshots described below. Layout at 1920×1080 and 21:9 remains unverified.

### Confirmed visual defect from Player capture

QA captured the actual 1280×720 Player menu [before](../QA/player-screen-before-ui.png) and [after](../QA/player-screen-after-ui.png) the change. Before, `Full Circle of your dream` overlapped the clock and calendar and was barely readable. `BuildMenu` moved its anchored Y from `265` to `0`, placing it on the dark central wall below the calendar and above the bed while keeping the specified secondary text colour and 26 px reference size. `SleepwalkingSceneBuilder.Build` resaved the standalone scene; [the Unity log](UI-subtitle-build.log) ended with return code 0 and the serialized Subtitle RectTransform has `m_AnchoredPosition: {x: 0, y: 0}`. The independent UI lead compared the two full Player screenshots and confirmed the new subtitle is wholly readable on the uniform dark wall, without overlap with the clock, calendar, bed, or window. The greater visual distance from the logo remains a composition tradeoff. This is a visual check of one 1280×720 frame, not other ratios or interaction.

The UI lead also found by geometry that the original orthographic room camera would crop the door and bookshelf at 4:3. `SleepwalkingUI.FitRoomCamera` now projects the active hotspot collider bounds along the camera's right axis and increases orthographic size when the viewport is too narrow. This change has not yet been observed in a 4:3 Player capture.

The independent lead checked the new framing math against the Visual prefab: at 4:3 the rightmost hotspot requires about `orthographicSize = 5.93` instead of `4.15`, leaving a small horizontal margin. At 21:9 the base size remains. QA captured a [1024×768 Player frame](../QA/player-screen-after-ui-4x3.png). The UI lead independently accepted this frame: the bookshelf, clock, bed, and door are all visible, and the subtitle remains readable on the dark wall without overlap. The smaller room and larger empty top/bottom areas are visible composition tradeoffs. This screenshot does not establish hover or click reachability. If another integrator creates hotspots after `UI.EnterRoom`, they must call `EnterRoom` again so the camera recalculates.

On 14 November 2026 at 02:15 Europe/Kiev, the agreed `SleepwalkingGameplayBuilder.BuildWindows` rebuilt `Assets/Scenes/Sleepwalking.unity` and `Builds/Windows/Sleepwalking_Data/Managed/Assembly-CSharp.dll`. The [Windows rebuild log](UI-subtitle-windows-rebuild.log) contains `Windows build: Succeeded 0 errors` and batch return code 0. The rebuilt managed assembly SHA-256 is `22D3D4D53D081D0D08A7CB2EECCEC03713C17B07F2748FF8D1A70E773CF120CF`; its file timestamp is 02:15:30. The launcher exe retained its 01:58 timestamp, so the managed assembly and rebuild log identify this revision. QA captured the new 1280×720 and 1024×768 Player frames after the rebuild.

## UI lead verdict

The independent UI lead accepted the static source and scene audit after rechecking the listed fixes. They compared QA's full 1280×720 Player frames before and after the subtitle fix and accepted its readability and contrast, then accepted the 1024×768 frame's subtitle and four visible objects. Their attempt to open Unity through the Computer Use helper stalled and was interrupted; they did not perform an interactive Player test. Hover/click, Tab/Enter focus, Escape/Back/Cancel, pause timing, audible feedback, and visual layout at 1920×1080 and 21:9 remain open acceptance checks.

## Visual menu polish, 14 November 2026

The combined Visual+UI [Windows build](VisualMenuPolish-WindowsBuild.log) reported `Windows build: Succeeded 0 errors` and updated the managed assembly at 02:44:02 (SHA-256 `3B11C173D48C697DD42341DEC4C818F77029DD958F8E1AFBB47B86F69CDEC0E7`). The shell detached from the Unity GUI process, so its own exit code was not captured; the log records normal batchmode shutdown. QA captured [16:9](../QA/player-screen-polish-16x9.png) at 02:44:50 and [4:3](../QA/player-screen-polish-4x3.png) at 02:45:05. Both frames therefore show the previous subtitle coordinates `(-165, 285)`: the subtitle crosses the clock in 16:9. The independent UI lead confirmed this defect from the actual frames. `BuildMenu` was changed at 02:46:01 to place the subtitle at `(-100, 50)` on clear wall space below the calendar in both ratios; this source change compiles with `dotnet build Assembly-CSharp-Editor.csproj` and is newer than the Player assembly and both frames. The UI lead checked the new position geometrically against both frames and estimated at least 22 px clearance from the calendar and 38 px from the bed at 4:3, with larger gaps at 16:9. A further Player rebuild and frame comparison are required before accepting the new position. The hover label is absent from these still frames, so its placement has not been visually verified.

The [correction Windows build](VisualMenuPolish-Correction-WindowsBuild.log) returned process code 0 and reported `Windows build: Succeeded 0 errors`. The managed assembly was updated at 02:49:48 (SHA-256 `E58EC5557D09DF50AEAC7B9A0C51E42D6E1FB971B9E1B83C7E9672D0F39BA9A0`). QA captured the corrected [16:9](../QA/player-screen-correction-16x9.png) and [4:3](../QA/player-screen-correction-4x3.png) Player frames after that build. The subtitle is now fully readable on the dark wall in both ratios, with no overlap with the clock, calendar, or bed. The independent UI lead accepted the visual correction: logo, subtitle, and lower hint are readable; all four room objects remain visible. The logo's lower strokes sit close to the clock in 16:9 but the word remains legible. These still frames do not verify the hover label, click or keyboard interactions, 1920×1080, or ultrawide layouts.

## Adaptive logo spacing, pending Player capture

The later [room polish 16:9](../QA/player-screen-room-polish-main-16x9.png) and [4:3](../QA/player-screen-room-polish-main-4x3.png) frames show the full-size logo touching the window in 16:9 and crossing the upper room line in 4:3. Only the main-menu logo group was changed: local scale `0.81`, reference X `-100`, and reference Y interpolated from `465` at 4:3 to `422` at 16:9. The subtitle and room interactions remain in place. C# compilation passed. The independent UI lead judged the placement plausible from frame geometry, with a small estimated gap to the clock at 16:9; a new Player build and screenshots are needed for visual acceptance.
