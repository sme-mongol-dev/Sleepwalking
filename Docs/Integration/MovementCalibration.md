# Movement calibration

The three movement profiles are assets in `Assets/SleepwalkingGameplay/Profiles/`. `SleepwalkingGame.MovementProfile` selects the active profile; `MovementProfiles` exposes all three candidates. The controller applies the chosen asset when it creates the Dreamer.

| Profile | Run | Jump impulse | Gravity scale | Dash speed × seconds | Air acceleration |
| --- | ---: | ---: | ---: | ---: | ---: |
| `01_Controlled` | 7.6 | 11.8 | 3.3 | 17.5 × 0.17 | 30 |
| `02_Responsive` | 8.5 | 12.0 | 3.2 | 18.0 × 0.18 | 38 |
| `03_Aggressive` | 9.4 | 12.15 | 3.35 | 20.0 × 0.19 | 48 |

## PlayMode method

`MovementTrajectoryTests` loads the authored scene, chooses each profile, starts each authored level, and drives the real `SleepwalkingPlayer` through simulated inputs processed by its normal `Update` and `FixedUpdate`. A landing requires contact with the named target `Collider2D` for three frames, the player's feet near its current top, and the player centre inside the design safe interval. The log records jump, wall jump, and dash positions, apex, farthest X, landing X, death, and elapsed time. The full L07 sequence starts at the authored entry, presses both buttons through contact, crosses the laser, jumps from the wall, dashes across the pit, and checks the landing.

`Docs/QA/movement-trajectory-results.xml` passed on 2026-11-14 (one test, zero failures); `Docs/QA/movement-trajectory.log` contains the `SW_MOVE` rows. The PlayMode runner passing means the measurement fixture completed, not that every route passed.

## Results on prefabs dated 12:40, before L05/L06 revision

| Route | Controlled | Responsive | Aggressive |
| --- | --- | --- | --- |
| L01 two gaps | both land | both land | both land |
| L02 gap, spike hop, exit gap | all land | all land | all land |
| L03 gap, saw hop, exit gap | all land | all land | all land |
| L04 button/laser run | reaches far floor | reaches far floor | reaches far floor |
| L05 A, B, C, exit (isolated starts) | all land | all land | all land |
| L05 Start → Exit ordinary-jump bypass | blocked | **lands X 8.97** | **lands X 9.44** |
| L06 Start → horizontal mover | lands X 6.11 | lands X 5.98 | lands X 6.02 |
| L06 Start → LiftLanding ordinary-jump bypass | **lands X 8.52** | **lands X 8.56** | **lands X 8.56** |
| L07 ordinary wall jump | wall jump succeeds in full run | lands X 4.42 | lands X 4.51 |
| L07 pit ordinary jump | falls short | falls short | falls short |
| L07 pit jump + dash, isolated | lands X 19.74 | lands X 20.29 | overshoots |
| L07 full entry → wall → buttons → pit landing | lands X 19.64 | lands X 20.33 | overshoots X 21.52 |

The L07 full-run results above confirm button contact and hazard deactivation. The recorded run did not yet measure timer reserve at the far side or contact with the bed. The fixture now measures both, pending the next PlayMode execution. The L04 row only confirms travel to the far floor; button timer reserve and bed contact remain unmeasured. Isolated L05 steps reset time and position for each jump, so they do not prove a complete timed sequence. L06 moving-platform landing is confirmed, while rider carry, dismount, vertical boarding, and the exit remain separate checks; `Docs/QA/mechanism-physics-rerun-results.xml` verifies platform rider contact and relative motion but does not prove end-to-end traversal.

## Results on revised prefabs dated 13:19

`Docs/QA/movement-trajectory-calibrated-results.xml` passed as a measurement fixture (one test, zero failures, 141.89 s). Its `SW_MOVE` rows in `Docs/QA/movement-trajectory-calibrated.log` show:

| Route | Controlled | Responsive | Aggressive |
| --- | --- | --- | --- |
| L01 two gaps; L02 three steps; L03 three steps | all land | all land | all land |
| L04 far floor | reached | reached | reached |
| L05 A, B, C, exit with revised 1.3-height steps | **none land** | **none land** | **none land** |
| L05 Start → Exit ordinary-jump bypass | blocked by height | blocked by height | blocked by height |
| L06 Start → horizontal mover at top 0.4 | lands X 6.29 | lands X 6.38 | lands X 6.16 |
| L06 Start → LiftLanding; LiftLanding → HighExit ordinary-jump bypasses | blocked | blocked | blocked |
| L07 full route: wall, both buttons, pit dash, bed | lands X 19.64; laser/saw reserve 7.44/6.96 s | lands X 20.33; reserve 7.48/6.96 s | overshoots X 21.52 |

The L05 first step reaches X 3.31–3.34 and feet Y 0.85–0.99, short of pad top Y 1.3. Later isolated starts similarly stop against the next pad. The current launch locations are close to each edge; an earlier launch must be tested before classifying the geometry as impassable. These isolated starts do not establish a continuous timed traversal. The L06 mover contact is physical, but the long ride, dismount, vertical lift, and exit have not been traversed end-to-end. Early dash bypasses on L05/L06 and L04 timer/bed still need dedicated checks.

At this intermediate point `02_Responsive` was still the provisional scene selection. Later results below supersede that status.

## Retest after one-way collider correction

Level Design corrected the one-way effectors and rebuilt the prefabs at 13:25. `Docs/QA/movement-l05-l06-results.xml` passed as a measurement fixture (one test, zero failures); its `SW_MOVE` rows are in `Docs/QA/movement-l05-l06.log`. Earlier L05 failures above belong to the pre-correction prefabs.

| Check on revised prefabs | Controlled | Responsive | Aggressive |
| --- | --- | --- | --- |
| L05 A → B → C → Exit, each as an isolated jump | four land | four land | A/B/C land; exit overshoots to X 12.05 |
| L05 direct plain or dash to B/Exit | all blocked | all blocked | all blocked |
| L06 Start → moving platform | lands | lands | lands |
| L06 Start → LiftLanding plain/dash; LiftLanding → HighExit plain/dash | all blocked | all blocked | all blocked |
| L06 full automated route | reaches mover, misses LiftLanding | reaches mover, misses LiftLanding | reaches mover, misses LiftLanding |

The L06 full route dismounted at X 13.13/13.26/13.50, then stopped at X 14.32/14.27/14.29 before the safe LiftLanding window X 14.925–15.475. This is an autopilot target error: the test released horizontal input at X 14.25 while airborne. The target was raised to X 14.85 in the test source; a rerun is needed. The vertical mover and HighExit remain unverified by this route. The four L05 isolated landings do not yet prove that a single attempt can meet all timed windows.

The independent Tester ran `Docs/QA/traversal-sequence-calibrated-results.xml` on `02_Responsive`: both end-to-end tests failed. Its L05 CSV confirms stable A and B landings, then C deactivated while the player was still airborne. A took 0.94 s, B 0.78 s, and C had only 0.40 s remaining at takeoff. The current `ActiveSeconds = 2.8`, `PhaseSeconds = 0.6` schedule does not meet the required 0.15 s landing reserve with this measured input sequence. Its L06 test launched from about X 2.25 and missed the horizontal mover, which moved ahead; the successful isolated calibration launched near X 2.9. This is a trajectory difference, so the independent L06 failure does not by itself prove that the authored route is impossible.

## Final calibration on the 13:42 prefabs and controller

Level Design set L05 TimedC phase to 0 and rebuilt the prefabs. Game Mechanics excluded one-way effectors from the wall probe, limited dash to one use until grounded, and required real standing contact on the support under BedExit. The later PlayMode runs below use those changes. Earlier rows in this document are retained as evidence of why the calibration changed, not as current verdicts.

`Docs/QA/movement-continuous-lift-results.xml` passed as a fixture (one test, zero failures). Its contact traces in `movement-continuous-lift.log` show:

| Continuous route | Controlled | Responsive |
| --- | --- | --- |
| L05 Start → A → B → C → ExitRun → bed | Passed; C reserve 0.97 s; exit X 9.50 | Passed; C reserve 0.85 s; exit X 9.49 |
| L06 Start → horizontal mover → LiftLanding → vertical mover → HighExit → bed | Passed; mover contact 207 frames; exit X 17.79 | Passed; mover contact 191 frames; exit X 17.68 |

`Docs/QA/movement-core-final-results.xml` passed as a fixture (one test, zero failures). The `movement-core-final.log` traces confirm both L01 gaps, L07's true wall jump, both timed buttons, the laser, jump plus dash landing, and the bed. Plain L07 wall/pit attempts fail as intended. Ordinary and dash attempts to skip L05/L06 fail for all three profiles under the measured inputs; the longer dash bypass scripts also issue a second dash press while airborne. These are sampled input schedules, not an exhaustive proof against every possible player input.

| L07 full route | Controlled | Responsive | Aggressive |
| --- | ---: | ---: | ---: |
| DashLanding X | 19.56 | 20.01 | 20.36 |
| Safe landing margin | 0.64 | 0.66 | 0.32 |
| Laser/saw reserve at crossing/landing | 7.44/6.96 s | 7.48/6.96 s | 7.54/7.04 s |

The isolated L07 pit jump plus dash is more sensitive to timing: landing margin is 0.74 for Controlled, 0.52 for Responsive, and only 0.02 for Aggressive. The L05 exit landing margin is 0.79 for Controlled and 0.30 for Responsive. Controlled completed the two continuous timed/moving routes with slightly more L05 C reserve and L06 exit inset under the calibrated input schedule.

Independent Tester reran its own scripts after the controller changes in `Docs/QA/traversal-sequence-final-results.xml`: Responsive completed L05 with 0.228 s C reserve; Controlled reached B but its slower, later trajectory started the C jump with 0.814 s remaining and missed the pad. Both L06 scripts first landed on the horizontal mover near X 12.6, close to its far turn, so the script's subsequent requirement to ride another 6 units could not be met. The calibrated Game Mechanics route boards near X 6 and reaches both movers and the bed. This difference limits the independent L06 verdict; it does not invalidate the contact measurements from either run.

After the independent fixture adjusted its mover boarding and exit-contact checks, `Docs/QA/traversal-sequence-exit-contact-results.xml` finished **4 Passed, 0 Failed**: Controlled and Responsive each completed the full L05 and L06 sequences. The final independent negative bypass audit also passed (`Docs/QA/traversal-bypass-final-results.xml`, one test, zero failures). The Game Mechanics rerun with four physics frames of supported BedExit contact passed (`Docs/QA/movement-continuous-exit-stability-results.xml`). That exit rule requires the player's feet to touch a solid support beneath the bed with a 0.15-unit landing inset; merely passing through the trigger in flight no longer wakes the player.

**`02_Responsive` is selected in `Assets/Scenes/Sleepwalking.unity`, the scene builder, and its profile asset as the default.** Both viable profiles pass the continuous routes; the independent L05 run landed on TimedC with 0.375 s remaining for Responsive versus 0.195 s for Controlled, while Responsive retained 0.52 world units of inset in the isolated L07 dash landing and 0.66 in the full L07 route. `01_Controlled` and `03_Aggressive` remain selectable for comparison. Interactive keyboard feel and visual timing on a physical display remain unverified.
