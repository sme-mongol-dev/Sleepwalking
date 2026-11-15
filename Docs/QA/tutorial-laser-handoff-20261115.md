# QA handoff: tutorials and active laser emitters

Date: 2026-11-15. Shared Unity Editor was released after the targeted L07 check.

## Confirmed

- `tutorial-runtime-results.xml`: PlayMode 3/3. T01 used a physical wall contact and wall jump, landed on ExitLedge, reached BedExit, and advanced to T02. T02 plain jump and wall-jump skip attempts died on the visible pit spikes and retried; jump plus dash landed on FinalRun, reached BedExit, and advanced to main L01. The test also verified runtime Dream Tile Matrix, backdrop, and bed sprite.
- Runtime frames were rendered with the graphics device and visually opened: `tutorial-runtime-T01-16x9.png`, `tutorial-runtime-T01-4x3.png`, `tutorial-runtime-T02-16x9.png`, `tutorial-runtime-T02-4x3.png`. All four show themed tiles, background, bed art, and readable hints; T02 spikes are visible.
- `final-remaining-routes-jump-retry-results.xml`: PlayMode 8/10. L01, L02, L03, and L04 reached their short-route targets at Odradek 1 and 100. L04 requires a jump over the active laser emitter after its button disables the beam.
- `final-remaining-routes-double-dash-staged-results.xml`: PlayMode 10/10. L07 crosses the active first emitter by jumping and dashing, lands on LaserRun to recharge, then jumps and dashes across the pit to DashLanding at Odradek 1 and 100. The saved events report stable landing margins 18.771 and 12.690 world units, with 6.680 and 5.860 seconds before the saw reactivates.
- `targeted-L07-controlled-class-results.xml`: PlayMode 12/12. This adds the same L07 short route with the Controlled movement profile at Odradek 1 and 100; the other tests use Responsive.

## L07 diagnosis and remaining full route

Scene: `Assets/Scenes/Sleepwalking.unity`; prefab: `Assets/SleepwalkingLevels/Prefabs/L07_DreamGeometry.prefab`; test: `SleepwalkingRemainingRouteTests.L07WallButtonsDashSawReachDashLanding` at Odradek 1 and 100.

Expected: button A disables only the beam; the player avoids the still-lethal emitter, reaches the pit, uses dash, and lands on DashLanding.

The earlier plain-jump route died on the active emitter after pressing ButtonA. At the fatal frame, the event trace reported `buttonA=True`, `beamDisabled=True`, `baseOverlap=True`. The low LaserCeiling cut off that jump. The verified short route now uses a midair dash to pass the emitter and recharges on LaserRun. No geometry change was needed for this section.

`final-full-routes-results.xml` (14 tests, 9 passed) was run before the latest full-route input edits. L07 Responsive reached the second laser and died on its active emitter; L07 Controlled used a Responsive prefix and died before DashLanding. The full-route test now selects a separately recorded Controlled prefix and jumps over the second emitter, but these edits have compiled only; **no new full-route PlayMode result exists**. Reaching the L07 bed remains unverified.

## Checks after the Windows build

1. Run the full L07 route to BedExit in Controlled and Responsive after the Git push. The new Controlled prefix and second-emitter jump need physical verification.
2. Resolve the remaining full-route test failures before treating the seven beds as accepted. In the 9/14 run, L04's short prefix ended airborne, and L05 Controlled replay hit the roof step near X=17.85 and then died near X=18.57. Those sections require new physical route input; neither failure proves an impossible level. Earlier sequence results passed 8/8 before the latest contour and emitter changes.
3. Re-run `SleepwalkingLaserIntegrationTests` and the bypass tests against the final emitted beam/base behavior. Treat sampled bypass negatives as sampled evidence only.

The tests use simulated input in PlayMode. A rendered manual gameplay pass and Windows build acceptance remain separate.
