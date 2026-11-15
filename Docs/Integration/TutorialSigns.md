# World-space tutorial signs

Owner: `Assets/SleepwalkingUI/`. The signs are reusable prefab assets for the two tutorial levels, not Canvas UI. The level builder owns placement and tutorial geometry.

## Assets and meaning

| Prefab | Keys and direction | English caption | Level |
| --- | --- | --- | --- |
| `Assets/SleepwalkingUI/Prefabs/Tutorial/Tutorial_Run.prefab` | A, D, right arrow | A / D · MOVE | Tutorial1 |
| `Assets/SleepwalkingUI/Prefabs/Tutorial/Tutorial_Jump.prefab` | Space, up arrow | SPACE · JUMP | Tutorial1 |
| `Assets/SleepwalkingUI/Prefabs/Tutorial/Tutorial_Grab.prefab` | Space, right arrow | SPACE · CLING | Tutorial1 |
| `Assets/SleepwalkingUI/Prefabs/Tutorial/Tutorial_WallJump.prefab` | Space, diagonal up arrow | SPACE · WALL JUMP | Tutorial1 |
| `Assets/SleepwalkingUI/Prefabs/Tutorial/Tutorial_Dash.prefab` | Shift, right arrow | SHIFT · DASH | Tutorial2 only |

Each prefab contains `SleepwalkingTutorialSign`, sprite renderers and a 3D `TextMesh`; it has no Canvas, colliders, trigger, or gameplay behavior. The five source PNGs are copied to `Assets/SleepwalkingUI/Textures/Tutorial/` and imported as 32×32 sprites with 32 pixels per unit, Point filtering, no mipmaps and uncompressed texture data.

## Builder placement contract

Create local markers under `Readability/TutorialHints/{Run,Jump,Grab,WallJump,Dash}`. Tutorial1 must omit Dash. The tutorial level root uses `localScale=(32,32,1)`, so one local X/Y unit is one block. Instantiate the corresponding prefab as a child of its marker, then set local position to zero, rotation to identity and scale to one. The marker represents the **centre** of the sign. Run spans about 3.4×2 blocks; the others span about 2.6×2. The lower edge of the two-line caption is approximately `marker.y - 1.1` blocks. The current Builder markers keep the captions above the player's path.

The icon artwork sits at local Z≈−2, in front of the 2D gameplay solids viewed from negative Z. Do not resize the prefab after instantiation; adjust the marker's block coordinates. Keep signs outside the movement and hazard colliders. A static-camera 16:9/4:3 Player capture is still needed to verify actual readability and placement.

Build or regenerate only the UI-owned sign prefabs with `Sleepwalking.Editor.TutorialSignBuilder.BuildFromCommandLine`. [The import log](TutorialSignBuild.log) records a successful Unity batch run. Level prefabs are not modified by that method.

## Visual QA status

The initial captures revealed an oversized TextMesh caption that obscured the keys. They were taken before the final tutorial collider baseline and were rejected as UI art. The final builder uses `characterSize = 0.07` instead of `0.38` and adds a short key name above each action, because the tiny lettering within the supplied key images does not survive downsampling. The capture command is `Sleepwalking.Editor.TutorialSignCapture.RebuildAndCaptureFromCommandLine`.

After the tutorial collider verification, the UI sign prefabs were regenerated and the four same-named captures were replaced. The final [Unity capture log](TutorialSignFinalCapture-v3.log) records process exit 0, five rebuilt prefabs and four new 320×180/320×240 renders upscaled with nearest-neighbor sampling to 1280×720/1280×960. On the resulting [Tutorial1 16:9](../QA/tutorial01-signs-16x9.png), [Tutorial1 4:3](../QA/tutorial01-signs-4x3.png), [Tutorial2 16:9](../QA/tutorial02-signs-16x9.png), and [Tutorial2 4:3](../QA/tutorial02-signs-4x3.png) images, the A/D, Space, Shift and direction icons are visible. The two-line English key/action captions are readable on the dark background, signs do not overlap each other or leave the frame, and Shift appears only in Tutorial2. The capture code also verifies four sign components in Tutorial1, one Dash sign in Tutorial2, and no colliders on signs. These are static Editor camera renders of the tutorial prefabs; a built Player interaction and input check remain separate.
