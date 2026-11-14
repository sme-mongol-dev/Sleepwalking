# Seven dream geometries

Use `Sleepwalking/Levels/Build Seven Prefabs` once after importing this folder. Place any `Prefabs/L##_DreamGeometry.prefab` under the shared scene's level root at `(0,0,0)`. The scene builder should use the root `SleepwalkingLevelDefinition` and connect behaviors through `SleepwalkingLevelFeature`; see `Docs/Integration/Levels.md`. Every block, hazard, button, and bed is an independent prefab child. The generator is source for the initial layout; edit the generated prefabs directly while tuning playability.

The first cycle style is an editable enum on each prefab. All seven currently carry the `Meat` placeholder because the player's exact distribution is pending. The geometry is style neutral; use the selected style only for material, ambience, and dressing overrides at runtime. Keep the same prefab for later cycles.

## Tile foreground and background

Every solid platform and wall has a `SleepwalkingTileSurface` marker. Its `Solid` property returns the existing `BoxCollider2D` that fixes the covered rectangle. Visual Director uses the four original `Docs/UI/References/TL_*_Dream.png` atlases as the foreground tiles; these are the actual tile sources, not visual references alone. The background is a separate collider-free theme layer and may use temporary generated art. Switching dream style changes the tile atlas and background without changing the seven layouts or their physics.

The collider-free `Readability` group keeps a thin pale top edge (0.04 world units, about one internal pixel at 320×240) and a pale wall-grip stripe in front of the tiles. The earlier solid dark faces were removed so they cannot cover the atlas. Static platforms can have a tile-only visual skirt below the collider to show top, side, and lower corners while leaving the gaps open. Hazards are orange, buttons cyan, timed platforms have three cyan tabs, and moving platforms have a cyan edge with pale direction marks. The bed has a dark silhouette and pale frame. `FallReset` is an invisible trigger below the route. `SleepwalkingLevelVisualState` hides a disabled hazard or timed platform and child tile renderers/cues when its 2D collider is disabled.

L04 seats its visible bed parts 0.2 world units lower to meet its level floor. The original `BedExit`, `Frame`, and `Pillow` trigger objects stay at their authored positions; the 2D collider baseline remains unchanged.

Gameplay `Collider2D` components remain on the authored objects. Matching 3D colliders live on their `Collision3D` children because Unity cannot put both physics collider families on one GameObject. The before/after 2D collider signatures are recorded by `Tools/check-colliders.cjs` and `Tools/collider-baseline.json`.

The 14 PNGs in `Previews/` are earlier 320×240 schematic views of serialized prefab geometry, before tile-atlas integration. They cover each lesson and the L01/L07 approach in all four styles, but are no longer evidence of the final foreground art. Inspect fresh Unity Player frames after the tile renderer is connected.

## Routes

All positions are in XY Unity units. The player starts at X=1. Solid tops denote feet height. The bed is on the last block. These are intended routes for the current 0.55×1.0 player collider; measured landing windows and acceptance limits are in `Docs/Integration/Levels.md`.

| Level | Route and lesson | Critical clearance |
| --- | --- | --- |
| L01 | Ground 0–3.8 → jump to 5.2–8.4 → jump to exit 9.5–12. | Gaps 1.4 and 1.1; rise 0.2. |
| L02 | Ground 0–3.5 → jump over low pit spikes to 4.7–8.2 → hop over floor spikes → exit 9.3–12. | Gaps 1.2 and 1.1; 0.9 spike strip. |
| L03 | Ground 0–3.6 → jump to 4.8–8.4 → jump over saw at X=6.45 → exit 9.5–12. | Gaps 1.2 and 1.1; saw top 0.765. |
| L04 | Step on button X=3.1, which disables laser X=6 for 7 seconds → cross at ground level → bed X=10.5. | Button to laser 2.9; beam reaches overhead block; crossing requires button. |
| L05 | Climb timed platforms centered at X=4, 5.65, 7.3, with tops 1.3/2.6/3.9 → exit at X8.5–12, top5.2. | Each intended rise 1.3; skipping a pad requires 2.6, above the three candidate plain-jump apexes. One-way tops prevent side contact, and the player wall probe excludes them. Timed phases are 0/0.3/0; each pad is active for 2.8 of 4 s. |
| L06 | Start top−0.9 → jump to horizontal mover at X4.4, top0.4 → ride to its far span X12.4–14.0 → jump to LiftLanding X14.5–15.9, top2.35 → ride vertical mover at X16.15 from top2.5 to4.0 → jump to HighExit X17–18.5, top5.05. | Horizontal mover travels +8.8 over 8 s; far gap0.5 with rise1.95. Direct LiftLanding→HighExit rise2.70 exceeds a plain jump. Both movers, LiftLanding and HighExit use one-way tops. Static dots and a right arrow on Start mark the wait direction when the mover leaves the camera. |
| L07 | Jump to wall at X=3.7, grab/slide and wall-jump onto ledge 3.7–5.3 at 2.45 → press A at 4.7 to shut laser at 7.4 → press B at 9 to shut saw at 19.0 → jump and dash the pit to exit at 18.5–21.1. | Ledge rise 2.45, above the current controller's plain jump apex; button timers 8 seconds; pit gap 8.8, above its plain jump reach even with coyote time and collider edge overlap. |

Spikes and saws sit beside a safe route or are disabled by a button. Laser bases are independently lethal. Laser beams preview at 0.2 world units, or 3.6 rendered pixels at 320×180 with orthographic size 5; the `BeamPixelWidth` feature setting requests 3.5 pixels at the final render size. The moving and timed platform logic is supplied by Game Mechanics. `Docs/QA/movement-continuous-lift-results.xml` and `traversal-sequence-exit-contact-results.xml` record the L05/L06 runtime routes; rendered views of the raised layouts still need review.

Each prefab includes an invisible `FallReset` trigger strip at Y=-3.2 below the full route. Connect it to the same retry animation and camera reset as trap contact; a missed jump must never leave the player falling indefinitely.
