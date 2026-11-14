# Game Mechanics integration contract

Owner: `Assets/SleepwalkingGameplay/` and `Assets/Scenes/Sleepwalking.unity`. The scene contains `SleepwalkingGame` and is the Windows build entry point.

## Game loop

`SleepwalkingGame` begins in the room and subscribes to `SleepwalkingUI.DreamRequested`, `RestartRequested`, and `MainMenuRequested`. `StartDream()` plays a short sleep transition and instantiates the next level. `CompleteDream()` clears runtime paint, increments the level counter, plays a wake transition, and returns to the room. Level index is `completedDreams % 7`; cycle is `completedDreams / 7`. The same seven prefab assets are reused on every cycle. Styles on cycle zero come from `SleepwalkingLevelDefinition.FirstCycleStyle`; later styles are randomized independently. Progress is session-only.

## Level assets

The scene builder loads `Assets/SleepwalkingLevels/Prefabs/L01_DreamGeometry.prefab` through `L07_DreamGeometry.prefab`, assigning them in order. It uses the marker and feature contract in [Levels.md](Levels.md). If prefabs are temporarily unavailable, runtime fallback geometry makes the scene playable. Prefabs are not modified at runtime; only instances and their mechanism state are changed. `SleepwalkingLevelFeature.Kind` values drive spikes, saws, laser bases/beams, timed platforms, moving platforms, and buttons. A button's `LinkedFeatureId` disables every matching hazard for `ActiveSeconds`.

All gameplay is 2D XY. The player uses only `Rigidbody2D`, with a collision width of 0.55 and height of 1.0. Solid objects use `BoxCollider2D`; lethal surfaces use 2D trigger colliders. The prefab's 0.04-unit laser beam is expanded on its runtime instance, including its mesh and 2D trigger, to `3.5 × (2 × camera orthographicSize) / render texture height` world units. At size 5 and 180 output pixels, this is about 0.194 units; at 240 pixels, about 0.146 units. Both are 3.5 rendered pixels. The width updates if the output resolution changes. The authored prefab remains untouched and receives the same runtime width on every cycle.

`TimedPlatform` uses its authored `ActiveSeconds` as the solid part of `CycleSeconds`; the value is clamped into the cycle, and `PhaseSeconds` offsets the timed state after a retry. `MovingPlatform` uses a kinematic `Rigidbody2D.MovePosition` in `FixedUpdate` and a cosine path from the authored transform to `MoveOffset` and back. A standing player receives the platform's movement velocity without losing horizontal input. The platform's zero-friction physics material prevents double horizontal carry; a side contact does not attach the rider. Reset returns the platform to the authored transform and clears the rider's inherited speed. Current authored moving features have `PhaseSeconds = 0`; moving paths intentionally start at the authored transform regardless of phase. The L05 timed state and L06 horizontal/vertical rider tests passed in `Docs/QA/mechanism-physics-rerun-results.xml`. L07 contains no authored moving platform, so that optional branch was skipped. Keyboard-controlled traversal remains unverified.

## Controls and events

Keyboard: A/D or arrows move, Space/W/Up jump or wall jump, Shift dashes, Escape pauses. The controller includes coyote time and jump buffering. A lethal collision calls `SleepwalkingGame.KillPlayer(Vector2 contact)`, leaves a session-only paint mark, plays a short impact state, and returns the player and mechanisms to the current start with a camera glide. Paint persists across retries on that level; leaving it clears all marks. Touching the bed exit calls `CompleteDream()`.

The builder assigns `Assets/SleepwalkingVisual/Prefabs/SleepwalkingRoom.prefab`, its `RoomHotspot` objects, and the four prefabs from `Assets/SleepwalkingVisual/Prefabs/Themes/`. `RoomEnvironment.SetWakeCount` applies each room change. The room's `PixelRoomView` camera stays active through dreams and renders both room and levels at point-filtered low resolution. If the room prefab is unavailable, a simple editable room is generated. `SleepwalkingAudioDirector.CreateOrGet()` supplies menu music, dream ambience, and action cues; it requires its Resources prefab.

## Build

The editor menu `Sleepwalking/Gameplay/Build Integration Scene` regenerates only the shared scene and build scene list. `Sleepwalking/Gameplay/Build Windows` creates `Builds/Windows/Sleepwalking.exe` and a build log. Run the scene builder after prefabs become available to serialize their references.
