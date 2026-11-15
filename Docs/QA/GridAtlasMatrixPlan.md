# Grid atlas matrix capture plan

Status: captured after the L03/L07 saw adjustment and L05 timer change on 2026-11-14. `VisualLogs/GridMatrixCaptureFinal.log` reports 56 frames and a clean batch exit.

## Capture set

- 7 levels × 4 themes (`Meat`, `PostApocalypse`, `DeepWater`, `Heaven`) × 2 aspect ratios (`16x9` at 1280×720, `4x3` at 1024×768) = 56 full-resolution frames.
- Files: `Docs/QA/VisualLogs/GridMatrix/L01-Meat-16x9.png` through `L07-Heaven-4x3.png`.
- One contact sheet for each aspect ratio, arranged as levels by rows and themes by columns. Inspect every full-resolution frame after scanning the sheets.
- Focused captures: L04 connected Platform/CaveShell edge; L05 timed platforms on/off/restored; L06 moving platforms at initial and far positions; L07 wall, laser, saw, dash landing and bed. Capture both ratios for any focused view that reveals an issue.

## Framing and checks

For each level, derive the frame from `SleepwalkingLevelDefinition.PlayerStart`, `BedExit`, non-shell `SleepwalkingTileSurface` colliders, and visible hazard bounds. Include the travel extent of moving platforms. Exclude the outer CaveShell masses from framing calculations. Fit the result to each target aspect with a margin, then verify start and exit remain inside the camera viewport.

Check the static `Dream Tile Matrix` for duplicate internal edges and missing cells; compare the four atlas materials on the same level. Check point-filtered scale, legibility of spikes, saw, laser, buttons and exit, and the visual separation of dynamic islands. Verify L05 timed visibility and L06 movement with dedicated state frames. Record any defect with level, theme, aspect ratio and frame path.

These Editor renders establish composition and tile appearance. PlayMode interaction, complete traversal, timing and Windows Player presentation require their separate checks.

## Result

- All 56 frames are in `VisualLogs/GridMatrix/`; `GridMatrixContact-16x9.png` and `GridMatrixContact-4x3.png` show every level and theme. The matrix was regenerated after the saw axis moved to the supporting block top.
- Full-size L03/L07 frames for all four themes show the upper saw half above the block with the lower half covered by the tile mesh. L04 has a continuous Platform/CaveShell surface; L05/L06 dynamic islands remain distinct from static geometry.
- `VisualLogs/GridBackdropActualResults.xml` passed 1/1 and updated `supplied-backdrop-meat.png` and `supplied-backdrop-war.png` from the integrated PlayMode scene. `VisualLogs/GridActualSceneResults.xml` passed 1/1 and updated `grid-static-L01-start.png` through `grid-static-L07-start.png`; actual L04 Meat confirms the backdrop scale fix.
- `VisualLogs/GridFinalWindowsBuild.log` and `VisualLogs/GridFinalIllustratedBuild.log` each report `Succeeded 0 errors`. `player-printwindow-grid-final-main-16x9.png` and `player-printwindow-grid-final-illustrated-16x9.png` are rendered Windows Player menu captures. Computer Use could not navigate Player levels because its Windows sandbox failed before initialization, so the level visual evidence remains Editor/PlayMode capture rather than Player GUI navigation.

## Platform underlay removal

- After the platform `_SafeEdge` objects were removed, `VisualLogs/GridMatrixCaptureNoBacking.log` captured 56/56 new frames. The two contact sheets in `VisualLogs/GridMatrix/` were regenerated. Comparing the old `VisualLogs/GridL04-Meat.png` with the new `VisualLogs/GridMatrix/L04-Meat-16x9.png` shows that the thick pale strip below the tiles is gone; the fine atlas edge remains.
- `VisualLogs/GridNoBackingActualResults.xml` passed 1/1 and refreshed actual PlayMode `grid-static-L04-start.png` and `grid-static-L07-start.png`. `VisualLogs/GridNoBackingFourThemesResults.xml` passed 1/1 and refreshed `environment-traps-{meat,postapoc,darkwater,heaven}.png` from L07. The vertical WallGrip cue remains as a separate functional marker.
- `VisualLogs/GridNoBackingWindowsBuild.log` and `VisualLogs/GridNoBackingIllustratedBuild.log` each report `Succeeded 0 errors`. Current Player menu captures are `player-printwindow-grid-no-backing-main.png` and `player-printwindow-grid-no-backing-illustrated.png`. Their runtime logs contain no Error/Exception entries. Player level navigation is still outside this window smoke check.
