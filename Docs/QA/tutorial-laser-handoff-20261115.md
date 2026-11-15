# QA handoff: tutorials and active laser emitters

Date: 2026-11-15. Shared Unity Editor was released after the final 14-case gate.

## Confirmed

- `final-full-routes-after-targeted-results.xml`: PlayMode **14/14** in one run. L01–L07 reached BedExit with both Controlled and Responsive movement profiles after the route-input corrections below.
- `tutorial-runtime-results.xml`: PlayMode 3/3. T01 used a physical wall contact and wall jump, landed on ExitLedge, reached BedExit, and advanced to T02. T02 plain jump and wall-jump skip attempts died on the visible pit spikes and retried; jump plus dash landed on FinalRun, reached BedExit, and advanced to main L01. The test also verified runtime Dream Tile Matrix, backdrop, and bed sprite.
- Runtime frames were rendered with the graphics device and visually opened: `tutorial-runtime-T01-16x9.png`, `tutorial-runtime-T01-4x3.png`, `tutorial-runtime-T02-16x9.png`, `tutorial-runtime-T02-4x3.png`. All four show themed tiles, background, bed art, and readable hints; T02 spikes are visible.
- `final-remaining-routes-jump-retry-results.xml`: historical diagnostic PlayMode 8/10. L01, L02, L03, and L04 reached their short-route targets at Odradek 1 and 100. L04 requires a jump over the active laser emitter after its button disables the beam.
- `final-remaining-routes-double-dash-staged-results.xml`: PlayMode 10/10. L07 crosses the active first emitter by jumping and dashing, lands on LaserRun to recharge, then jumps and dashes across the pit to DashLanding at Odradek 1 and 100. The saved events report stable landing margins 18.771 and 12.690 world units, with 6.680 and 5.860 seconds before the saw reactivates.
- `targeted-L07-controlled-class-results.xml`: PlayMode 12/12. This adds the same L07 short route with the Controlled movement profile at Odradek 1 and 100; the other tests use Responsive.
- `targeted-full-L07-controlled-landing-results.xml` and `targeted-full-L07-responsive-results.xml`: PlayMode 1/1 each. Both movement profiles physically cleared the second active emitter, landed on SecondLaserRun, crossed the final pit, and reached BedExit. No level geometry or gameplay change was required.
- `targeted-full-L04-controlled-results.xml` and `targeted-full-L04-responsive-results.xml`: PlayMode 1/1 each. Both reached BedExit after the recorded short prefix completed its jump and physically landed on LaserRun.
- `targeted-full-L05-controlled-early-results.xml`: PlayMode 1/1. A fresh jump from TimedC starts earlier than the old recorded input, clears the roof step near X=18, and reaches BedExit without a level or gameplay change.

## L07 diagnosis and resolution

Scene: `Assets/Scenes/Sleepwalking.unity`; prefab: `Assets/SleepwalkingLevels/Prefabs/L07_DreamGeometry.prefab`; test: `SleepwalkingRemainingRouteTests.L07WallButtonsDashSawReachDashLanding` at Odradek 1 and 100.

Expected: button A disables only the beam; the player avoids the still-lethal emitter, reaches the pit, uses dash, and lands on DashLanding.

The earlier plain-jump route died on the active emitter after pressing ButtonA. At the fatal frame, the event trace reported `buttonA=True`, `beamDisabled=True`, `baseOverlap=True`. The low LaserCeiling cut off that jump. The verified short route now uses a midair dash to pass the emitter and recharges on LaserRun. No geometry change was needed for this section.

`final-full-routes-results.xml` (14 tests, 9 passed) was run before the latest full-route input edits. L07 Responsive had died on the second active emitter; L07 Controlled had used a Responsive prefix. Both defects in the test route were corrected, then both targeted L07 runs and the final 14-case gate reached BedExit.

All five cases that failed in that old aggregate (L04 Controlled/Responsive, L05 Controlled, L07 Controlled/Responsive) passed separately after test-input corrections and again in the final 14-case gate. No gameplay or level geometry code changed during these targeted fixes.

## Remaining acceptance boundaries

1. Re-run `SleepwalkingLaserIntegrationTests` and the bypass tests against the final emitted beam/base behavior. Treat sampled bypass negatives as sampled evidence only. Earlier sequence results passed 8/8 before the latest contour and emitter changes.

The tests use simulated input in PlayMode. A rendered manual gameplay pass remains separate. The Windows build and Player smoke were reported successful by the coordinator; they are outside this PlayMode evidence.
