# Architecture / performance / robustness review

Scope: `Assets/_Project/Scripts` (about 4.6k lines, 7 asmdefs) and `.github/workflows`. Static read only. Nothing was run, so the Quest-specific points are reasoned from the code and settings, not profiled.

## Summary

The pure layer is in good shape. `MatchRulesEngine`, `MatchSnapshot`, `BallFlightModel` and the Side/Pacing/Tuning structs are deterministic and engine-light, with a real unit-test suite. The scene adapters are thin, and `Seam<T>` plus `OnValidate` wiring checks are used consistently.

The weak points are these:

1. **No game-over or point feedback.** `ScoreboardPresenter` was deleted, so the board shows scores only. At game over the player gets no result and no hint how to restart. The only way back in is a left-controller menu button, and nothing says so.
2. **Release queue can drop a release.** A keyword push can be replaced by a later plain push to `develop`/`main`, and the run is lost without a trace. `main` pushes promote ALPHA to BETA without checking what ALPHA contains.
3. **Layering is leaky.** `PingPong.Gameplay` references `Unity.InputSystem` and hosts `InputSystemPlayerCommands`. Core is not engine-free, which its docs imply.
4. **Update loops.** Seven per-frame loops exist, and 3 to 4 of them can be removed or made event-driven. `Maximum Allowed Timestep` is 0.33 s, which risks a physics spiral on Quest hitches.
5. **Test and multiplayer gaps.** Tests cover only the pure layer. Nothing covers the scene adapters (`MatchController`, AI, `PaddleHitter`), which is where the state-machine holes are. The multiplayer seams exist (snapshot, `IsAuthority`, `DetectHits`) but are untested and incomplete. Online is entirely absent.

---

## Critical

None found that corrupt the rules engine. Items 1 and 2 under High are the nearest.

## High

### H1. Game over and point feedback are invisible; restart is hard to discover
- `Presentation/MatchScoreboard.cs:47-61`, `Presentation/MatchText.cs`, `Presentation/ModeMenuPresenter.cs:69-79`.
- `MatchScoreboard` only writes names, scores and a SERVE marker. The result, point reason, "Your serve", let, and "Menu button to play again" strings in `MatchText` are no longer used by any runtime code (`Detail`, `Status`, `Point`, `Game`, `Describe`, `Let`, `Name`, `OpponentName`, `Score`, `PlayAgain`, `LocalName`). Only tests reference them.
- Nothing subscribes to `MatchController.GameWon`/`PointWon`/`Let`. At `GameOver` the ball just falls, and the menu never opens.
- Restart depends on `InputSystemPlayerCommands._menu`, which is left `menuButton` or Escape only (`InputSystemPlayerCommands.cs:15`). With only the right controller held, or in hand tracking, there is no way to open the menu and the session is stuck at GameOver.
- **Fix:**
  - Re-add a thin presenter driven by `Changed`/`PointWon`/`GameWon` that uses `MatchText`, or delete the dead strings and tests.
  - At `GameOver`, have `ModeMenuPresenter` auto-open on `GameWon`.
  - Bind a second menu source: right-hand B/system button, a "restart" pinch or poke on the scoreboard, or a world-space Restart button on the table.
  - Add a safety net: a long-press or timeout path that restarts when idle at GameOver.

### H2. Release workflow can silently drop a requested release
- `.github/workflows/main.yml:25-30`.
- All pushes to `develop`/`main` share group `release` with `cancel-in-progress: false`, but GitHub keeps only one pending run. A `[Production]` push waiting behind a running build is cancelled when any later push arrives, including a plain one that plans to `None`.
- The comment at lines 25-27 accepts this for "a third quick push", but the loser can be the only keyword run.
- **Fix:**
  - Give keyword releases their own concurrency group, for example derive `group` from the commit message, or split plan and release into two workflows.
  - Or make the plan job re-run any dropped keyword commit by scanning the commits since the last `v*` tag, instead of only `head_commit`.

### H3. `main` push promotes ALPHA to BETA without checking what ALPHA is
- `main.yml:292-324`.
- Any non-keyword push to `main` runs `set-release-channel-build ALPHA -> BETA`. It never checks that the ALPHA build corresponds to `main`'s tip, or that it is newer than BETA.
- The tag is `v<bundleVersion>-beta.<run>`, taken from `main`'s version file, which may not match the uploaded build.
- A hotfix or revert merged to `main` promotes a build that does not contain it.
- **Fix:**
  - Read the ALPHA build's version code through `ovr-platform-util list-builds` and compare it with the `v*` tag reachable from `HEAD`. Refuse to promote otherwise.
  - Or make promotion explicit with a `workflow_dispatch` or `[Promote]` keyword.
- Related: the `appSecret` is passed on the command line at line 309. It is masked, but pass it through env and check the CLI's input options instead.

### H4. Release tag may point at an unbuilt commit
- `main.yml:244-270`.
- The release job re-checks out `github.ref_name` (the branch tip at that moment, not `github.sha`), pulls with rebase, commits the version, and tags `HEAD`.
- If someone pushes between the build and the release job, the tag and the version commit land on code that was never built.
- **Fix:** check out `github.sha`, apply the two-line version edit, push with `--force-with-lease` only if the branch tip is still `sha`, and otherwise fail loudly. Alternatively tag `github.sha` for the build and keep the version commit separate.

### H5. Physics spiral risk: `Maximum Allowed Timestep` 0.333 s
- `ProjectSettings/TimeManager.asset:12`. Fixed timestep is 1/90 (verified).
- A 100 ms hitch on Quest (loading, GC, thermal) runs about 9 physics steps in one frame and makes the next frame slower, which is a spiral.
- The ball is CCD continuous and every step runs a `PaddleHitter` swept test.
- **Fix:** set `Maximum Allowed Timestep` to about 0.03 to 0.05 (3 to 4 fixed steps). Consider `Fixed Timestep` 1/72 or 1/80 if the headset runs at 72 Hz, to avoid a 90:72 beat that gives alternating 1 and 2 steps per frame.

### H6. Gameplay asmdef depends on Unity Input System
- `Gameplay/PingPong.Gameplay.asmdef` references `Unity.InputSystem`; `Gameplay/Input/InputSystemPlayerCommands.cs` lives in Gameplay.
- Gameplay is meant to be platform-free (seams `IPlayerCommands`, `IHaptics`, `IPassthrough`), but a concrete input adapter sits in it. It also uses `XRController`, which is XR-specific.
- **Fix:** move it to a new `PingPong.Platform.InputSystem` asmdef next to `Platform.Meta`, and remove the reference from Gameplay. The composition root (scene setup) then wires `Seam<IPlayerCommands>` to it.

## Medium

### M1. State-machine holes in MatchController
- `Gameplay/Match/MatchController.cs:57-68, 126-140, 155-167`.
- **FreePlay is never serviced.** `Update` handles only `Rally` and `WaitingForServe`. In `FreePlay` (before the first menu pick, or after `StopMatch`) a ball that falls or leaves play is never reset. `StopMatch` is also never called anywhere, so it is dead API.
- **Stuck at PointOver.** `OnDisable` cancels the coroutine (`:67`). If the controller is disabled and re-enabled during the point pause, phase stays `PointOver` forever and no serve is scheduled. Only a manual restart recovers. The same happens if the GameObject is deactivated for a scene change. Re-arm in `OnEnable`: if `IsAuthority && Phase == PointOver`, call `ScheduleNextServe`.
- **Timeout and time scale.** The rally timeout uses `Time.time - LastHitTime` and the pause uses `WaitForSeconds`. Both follow `Time.timeScale`, so any pause that scales time or a debug time scale can stall or skip them. Use an unscaled or injected clock, consistent with the "no clock in the engine" design.
- **Fix:** replace the coroutine with a plain timer field (`_serveAt`) checked in a single `Update`, and make that `Update` handle all phases through one state-driven method. This also removes the `Coroutine` handle, the per-point `WaitForSeconds` allocation, and the cancel bookkeeping.

### M2. `Strike` launches the ball before the rules judge the hit
- `MatchController.cs:101-108`.
- `_ball.Launch` and `PaddleHit` fire before `Engine.RegisterHit`. If the engine rejects the hit (for example `WaitingForServe` and `player != Server`, or `GameOver`), the ball has already been launched and `LastHitTime`/`HitCount` advanced.
- `PaddleHitter` guards with `CanHit`, and the AI checks `CanHit` in the strike path, but `Strike` is a public method and the comment calls it "the one way a player strikes the ball" (including the future network path).
- **Fix:** have `Strike` check `Engine.CanHit(player)` itself and return a bool. Launch and raise events only on success, so the invariant lives in one place.

### M3. Hand-tracking vs controller switching is one-way and fragile
- `Gameplay/Players/PaddleGrip.cs:59-66, 75-89`, `InputSystemPlayerCommands.cs:24-33`.
- On hand tracking, `PaddleGrip` releases to a **dynamic** Rigidbody with the last hand velocity. Nothing re-grabs when the controller returns, nothing respawns the paddle if it falls on the floor or leaves the room, and `OnMatchStarted` skips the grab when `ControllerActive` is false. The player can end up with no paddle and no recovery.
- `ControllerActive` returns `true` when no `XRController` device exists (keyboard), and relies on `isTracked.isPressed`. On OpenXR the `isTracked` control can read `false` for a frame or two on regain or loss. There is no debounce, so a one-frame false drops the paddle.
- Meta's "free grab" for hand tracking (class comment) is not in this codebase. The paddle behaviour with hands is whatever the scene prefab does.
- **Fix:**
  - Debounce `ControllerActive` (about 0.2 s).
  - When the controller returns and the paddle is not held, re-grab or snap it to the hand. Respawn the paddle at the rest position if it falls below the floor.
  - Release should set the body kinematic again after a short throw, or reset it, rather than leaving a free dynamic body.
  - Add `OnDisable` release.

### M4. Paddle posed in `LateUpdate` from tracked anchors: possible one-frame latency
- `PaddleGrip.cs:13, 69-76`.
- `[DefaultExecutionOrder(-100)]` places the paddle before `PaddleHitter` samples, but the controller anchors are updated by the OVR rig in its own `Update`/before-render callbacks. If the rig updates after `LateUpdate`, the paddle lags the hand by a frame, which is visible in VR and also feeds a stale velocity into `FrameVelocity`.
- Verify in the profiler/frame debugger. If confirmed, pose the paddle in an `Application.onBeforeRender` handler and sample `PaddleHitter` after that.
- Also: `_handVelocity` is computed per rendered frame from raw deltas with no smoothing, so a single long frame gives a spike on release. Use a short ring buffer or exponential filter.

### M5. Per-frame polling that should be event-driven
- See "Update/coroutine reductions" below. The main item is `GameModeController.Update` and `PaddleGrip.Update` polling `WasPressedThisFrame` through an interface cast every frame (`Seam<T>.Value` does `as T` plus a Unity-null check each call).

### M6. TMP scoreboard is rewritten on every engine event
- `Presentation/MatchScoreboard.cs:33-61`.
- `Refresh` runs on every `MatchController.Changed`, which the engine raises on every hit, net touch and serve setup, and also on every `GameModeController.Changed`. Each call sets up to 4 `TMP_Text.text` values (names are constants, scores allocate via `int.ToString()`), and `SetActive` twice.
- Rebuilding TMP mesh on identical text wastes CPU on a mobile GPU/CPU budget and creates garbage during a rally (hits are the hot path).
- **Fix:** cache last score and server and exit early when unchanged. Only touch name labels when mode or local side changes. Use `SetText` with a cached number-string table for 0 to 30. Subscribe to a narrower event (point/phase), not `Changed`.

### M7. Ball CCD mode and paddle colliders
- `Gameplay/Ball/PingPongBall.cs:62-63`.
- `ContinuousDynamic` is the most expensive CCD mode, and mobile guidance is `ContinuousSpeculative` for small fast bodies. Ball vs table/net/static walls is the only case needing CCD, because paddle hits are done by `PaddleHitter`'s swept test (paddle-ball collisions are ignored, line 76).
- Ball speed is capped, so verify with a profiler that `ContinuousSpeculative` does not miss the thin net at `MaxSpeed`. Net thickness and `defaultContactOffset` 0.01 matter here.
- The paddle colliders (`PaddleHitter.Start`, line 76 ignores only the ball) still collide with the table, floor and each other. The paddle's colliders can be reduced to trigger-free kinematic shapes or moved to a layer that collides with nothing but the room, so a player's own paddle doesn't push at the table.

### M8. Seven pipeline assets and quality levels ship to Quest
- `ProjectSettings/QualitySettings.asset`, `Assets/Settings/*_PipelineAsset.asset`.
- Android uses level 2 (Medium). All 7 URP assets and 7 renderer assets (Very Low to Ultra) are in the build, unused.
- Additional lights are `per-pixel` (2) on Low/Very Low but `per-vertex` (1) on Medium. That is the wrong way round for a mobile tier.
- MSAA 4 is on at every level. That is normally right for Quest, but `m_RenderScale` is 1 everywhere, so there is no dynamic resolution or foveation lever.
- **Fix:** keep one or two quality levels for Quest, remove the rest from the build, set additional lights to per-vertex or off, and expose render scale and foveation (the Meta plugin) in a settings SO.

### M9. Online/Fusion/NGO readiness: seams exist but are incomplete
- `MatchController.IsAuthority`, `ApplySnapshot`, `PaddleHitter.DetectHits`, `MatchSnapshot` (blittable, `IEquatable`) are good groundwork.
- Gaps:
  - `MatchSnapshot` has no tick or sequence number, so `Restore` cannot order or reject stale snapshots (`MatchRulesEngine.cs:172-183`). If two points arrive in one snapshot only the last fires, which is documented but loses audio/feedback.
  - `MatchSnapshot.Equals` is a manual field list that must be kept in sync. `Winner` is covered, but nothing enforces future fields (use a unit test that reflects over fields).
  - The ball position and velocity are not part of the snapshot or any state-authority interface. Fusion needs the ball on the state authority with `[Networked]` position/velocity, and clients in kinematic follow mode. `PingPongBall.Hold/Launch` mix presentation and authority concerns.
  - On clients nothing places the held serve ball (`PlaceServeBall` runs only on the authority), so a client sees a stale ball.
  - `MatchRules` are not in the snapshot (documented), so a mismatch between peers desyncs silently. Add a rules hash to the join handshake.
  - `GameMode.Online` and `OnlineAvailable => false` are placeholders. `Restart()` silently does nothing for `Online` while `ModeMenuPresenter.Pick` still closes the menu (`GameModeController.cs:57-60`).
- **Fix:** add a `MatchSnapshot.Tick`, an `INetworkMatch`/`IBallAuthority` seam, and route `Strike` through an authority-aware command.

### M10. No scene-adapter tests (where the bugs are)
- Tests: engine (27), flight model (10), text (11), grip (2), model (1), release version (4). Nothing for `MatchController`, `AIOpponent`, `PaddleHitter.TryHit`, `TableFrame`, `GameModeController`, or `ModeMenuPresenter`.
- `PaddleHitter.TryHit` (swept test, relative velocity, assist) is the most important feel code and has no test. It could be made a pure static function in Core over `(prevPose, pose, prevBall, ball, paddleVel)` and tested directly.
- **Fix:** add PlayMode tests for the restart paths (restart in each phase, disable/enable during `PointOver`, `GameOver` then restart, AI as server) and EditMode tests for the extracted hit function.

## Low

- **L1. Core is not engine-free** (`Core/PingPong.Core.asmdef`: `noEngineReferences: false`). `TuningData.cs` and `BallFlightModel.cs` use `Vector3`/`Tooltip`. The engine class comments say "no engine dependencies", which holds only for `MatchRulesEngine`. Split `PingPong.Rules` (pure, `noEngineReferences: true`, easy to reuse on a server) from `PingPong.Core.Physics`.
- **L2. Magic numbers.** `ModeMenuPresenter.cs:115` (`1e-4f`), `PaddleHitter.cs` (`1e-5f`, `ContactClearance`), `AIOpponent.cs:35-36` and `1.5f` at `:96`, `PingPongTable`'s net height default, `MenuText`/`MatchText` literals mixed with constants. Move design values into the SO profiles; keep only solver constants in code.
- **L3. Duplicated text and wording.** `MatchText.BoardName` exists beside the unused `Name`/`OpponentName`. `PingPongSceneSetup.cs:392-393` sets board names at editor time and `MatchScoreboard` sets them at runtime, so two writers exist. Keep one (runtime).
- **L4. `MenuText.Hint`** is used only by the editor builder, and it tells the user "Menu button opens and closes this menu" while the code only maps the left menu button.
- **L5. `Seam<T>.Value`** does `_component as T` every call. Cache in `OnEnable` where used per frame (`GameModeController.Update`, `PaddleGrip.Update`, `PaddleHaptics`).
- **L6. `ModeMenuPresenter.Label`** (`:140-145`) calls `GetComponentInChildren<TMP_Text>(true)` on every refresh for every button. Cache the labels once in `Awake`.
- **L7. `AIOpponent.Level` getter** returns `_level` (-1) until `Awake` has run, and `Difficulty` indexes with it. `ModeMenuPresenter.Refresh` is safe only because it runs after Awake. Initialise in a field initialiser or guard in the getter.
- **L8. `AIOpponent.Serve`** (`:176-198`) simulates up to 12 candidate serves in a single frame, and `Plan` simulates up to 240 steps. Both are cheap in isolation, but both are single-frame spikes on Quest. Spread `Serve` over frames or precompute.
- **L9. `PaddleHitter.Start`** (`:72-77`) does `GetComponent` and `GetComponentsInChildren` once, fine, but misses colliders added later and ignores the AI paddle. Use a layer matrix instead of `Physics.IgnoreCollision` per collider.
- **L10. Dual input stacks.** Input uses the Input System (`XRController`) while haptics and passthrough use OVR (`OVRInput`, `OVRPassthroughLayer`), and the project has OpenXR Package Settings modified in the working tree. Pick one runtime (OpenXR plus Meta feature, or OVRPlugin) per build to avoid double hand/controller state.
- **L11. `PingPongBall.Hold`** sets both `Body.position` and `transform.position`. With `Interpolate` the one-frame smear can show on teleport; set `Body.interpolation = None` while held and restore on `Launch`.
- **L12. `MetaPassthrough.Set`** toggles the skybox and every renderer/light under the hide roots on one frame (`:51-66`). Fine on a menu action, but `RenderSettings.skybox = null` plus lights off changes ambient lighting. Test for a pop on toggle.
- **L13. Workflows.**
  - Third-party actions are pinned to tags, not SHAs (`jlumbroso/free-disk-space`, `RageAgainstThePixel/*`, `game-ci/*`). Pin to SHAs.
  - The top-level `permissions: contents: write` applies to the Plan job, which needs none.
  - `Packages/manifest.ci.json` and `packages-lock.ci.json` are a second copy of the package manifest, so they can drift. Generate the CI variant from the real one by script.
  - Only EditMode tests run in CI. PlayMode tests are absent.
  - The cleanup workflow deletes artifacts by name, which is fine, but runs for forks that never had one.

---

## Update/coroutine reductions

| Where | Today | Change | Saves |
|---|---|---|---|
| `GameModeController.Update` (`:72-77`) | Polls `MenuPressed` every frame | Subscribe to `InputAction.performed` through `IPlayerCommands.MenuPerformed` event, delete `Update` | 1 Update plus an interface cast per frame |
| `PaddleGrip.Update` (`:59-66`) | Polls `GrabPressed` x2 and `ControllerActive` | Events for grab; poll `ControllerActive` only while `HeldBy != None` (or subscribe to device change) | 3 interface calls per frame |
| `OvrControllerHaptics.Update` (`:29-33`) | Runs forever to stop a pulse | Set `enabled = false` when idle; enable in `Pulse` (or use a coroutine only while pulsing) | 1 Update, always |
| `MatchController.Update` (`:126-140`) | Runs for every phase, always | Enable only in `Rally` and `WaitingForServe`; or check in `FixedUpdate` at physics rate (the ball is physics-driven) | Whole Update during pauses and game over |
| `MatchController.NextServeAfterPause` coroutine | `StartCoroutine` + new `WaitForSeconds` per point, timeScale-bound | Plain timer field in the one `Update` (see M1) | Coroutine object and handle |
| `AIOpponent.Update` (`:90-114`) | `MoveTowards`, `rotation`, phase switch every frame, even idle at rest | Disable the component, or early out when `_target == position && _swing == 0` and phase is not `WaitingForServe`/`Rally` | Per-frame transform writes |
| `PaddleHitter.LateUpdate` (`:81-89`) | Samples the blade pose every frame | Needed for velocity; keep, but run only while `_match.CanHit(_player)` or phase is `Rally`/`WaitingForServe` | Small |
| `PaddleHitter.FixedUpdate` (`:91-103`) and `AIOpponent.FixedUpdate` (`:116-124`) | Run at 90 Hz always | Skip when `Phase` is `GameOver`/`PointOver`/`FreePlay`; AI: enable only while `_strikeTime >= 0` | Up to 90 calls/s while idle |
| `MatchScoreboard.Refresh` on every `Changed` | 4 TMP writes and 2 SetActive per hit | Narrow events plus cached last-value diff (M6) | TMP rebuilds and string garbage per hit |
| `ModeMenuPresenter.Refresh` | `GetComponentInChildren` per button per refresh | Cache labels in `Awake` | Lookups per open |
| `ModeMenuPresenter.Start` coroutine (`:81-87`) | One-frame wait then `Open` | Fine (runs once). Could be `Invoke`-free by opening in `OnEnable` after head tracking is valid, but not worth changing | none |

---

## Prioritized action list (top 10)

1. **Game over and restart UX (H1).** Auto-open the menu on `GameWon`, restore point and result text from `MatchText` (or delete the dead strings), and add a second menu input that works on the right controller and with hands.
2. **Fix the release queue (H2).** Keyword releases must never be dropped by a later plain push.
3. **Gate BETA promotion (H3, H4).** Verify ALPHA matches `main`'s tip and make the tag point at the built commit.
4. **Lower `Maximum Allowed Timestep` to about 0.03 to 0.05 (H5).** Evaluate 1/72 or 1/80 fixed step for a 72 Hz headset.
5. **Make `Strike` the single guarded entry point (M2).** `CanHit` check inside, launch and events only on success.
6. **Match state robustness (M1).** Re-arm the serve timer in `OnEnable`, service `FreePlay`, replace the coroutine with one timer, and add PlayMode tests for restart in every phase.
7. **Paddle and tracking robustness (M3, M4).** Debounce `ControllerActive`, re-grab on controller return, respawn a dropped paddle, check anchor-update ordering and move pose to `onBeforeRender` if needed.
8. **Move input out of Gameplay (H6) and split pure Rules from Core (L1).** This makes the asmdef graph match the stated layering and prepares a headless server build.
9. **Remove idle Update loops (table above) and narrow the scoreboard refresh (M6).** Convert polling to input events and disable idle components.
10. **Prepare for online (M9) and widen tests (M10).** Add a snapshot tick, a ball-authority seam, a rules hash and a testable pure hit function. Trim the quality levels and pipeline assets shipped to Quest (M8).
