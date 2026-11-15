# Ping Pong Mania: handoff (2026-11-15)

VR table tennis for Quest. Unity 6000.3.9f1, URP, Meta XR SDK (OpenXR), solo vs AI, LAN multiplayer later.
Repo BrusleyM/Ping-Pong. Default branch `main`, work branch `develop`. Unity MCP instance **Ping-Pong@4f493402**: always `set_active_instance` and check `Application.productName == "Ping Pong Mania"` (a second Unity project may be open).

## Rules for working
- Replies short, minimal changes. Never poll CI or builds. Fresh session per task.
- **Never stash, revert or discard the user's local changes.** Understand each one, then commit it or ask.
- **Tests:** run EditMode locally through Unity MCP `run_tests` before every push (about 10 s). Say plainly when not run. Run them with Main.unity closed or accept that the open scene is swapped.
- **PRs:** one concern each. PR into develop: title starts `[Develop]` (merge uploads to Meta ALPHA, bumps version). PR into main: no keyword (promotes ALPHA to BETA). Merge commit, never squash. The review session (`local_553ad29f-b633-458a-8b61-a4b190f5b73c`, "Review PR 67: World prefab") reviews every PR before the user merges. After a review do not push to that PR. The user merges; I never merge. No "Generated with Claude Code" line in PR bodies.
- **Code style:** no comments or XML summaries in code; names (types, methods, tests) tell the story; test names are full sentences. Single source for logic used twice. No scripts that position or compute static things at runtime: bake into prefabs. Events over polling. Tuning in ScriptableObjects.
- Never read or print secrets.

## State
- develop is at version 0.0.17 (code 219). PRs through 94 are merged. No PR of mine is open.
- **Scene:** `Assets/_Project/Scenes/Main.unity` is the only scene (thin: light + one `World.prefab`). Overrides live in the prefab that owns them; World keeps only wiring (cross-prefab references such as the Ball's `_table`, `_menuGesture`, `_physicsPaddle`, Tutorial refs). A nested prefab cannot hold a reference to an object outside itself, so "apply down" drops those.
- Layers: PingPong.Core (pure), Gameplay, Presentation, Platform.Meta, Platform.Input, Editor, Tests (147 EditMode tests).
- Hits are scripted by default; optional physics paddle contacts via the menu toggle.
- Hand-tracking menu: wrist glance (back of left wrist faces eyes).
- Tutorial: built by the Framework package session (PRs 91-93: sequence plus scene wiring, Replay tutorial button in the menu). Ghost hands and further steps are still tickets in Notion.
- Release pipeline in `.github/workflows/main.yml`; version only in ProjectSettings; BETA promotion gated on the latest release being in main.

## Known gaps and open decisions
- **CI Meta SDK mismatch:** `manifest.ci.json` pins 205, `manifest.json` pins 209. Waiting for the user's yes to align CI to 209 (small `[Develop]` PR touching `manifest.ci.json` and `packages-lock.ci.json`).
- Ball audio: `BallImpactAudio` paddle, net and other clips are unassigned (silent). Needs clips.
- Offered, awaiting a go: AI visible-miss fix; floor collider cleanup; hit-zone debug view; silence idle-phase contact in `ContactMatters`; Notion rows for the seven architecture findings.
- Remaining items of `docs/REVIEW-2026-11-14.md`: M4 (headset), M7, M8, M9, M10, L2-L13, M1.
- Needs a headset: 1/72 vs 1/90 fixed step, physics-mode lag, scoreboard text size (2.4 to 1.65 is intended), hand-tracking menu.

## Gotchas
- Unity MCP drops on every domain reload (30-90 s): wait for `~/.unity-mcp/unity-mcp-status-4f493402.json` (`reloading=false`, fresh heartbeat), then `set_active_instance` again. Entering Play mode reloads too.
- Saving a prefab through `PrefabUtility.SaveAsPrefabAsset` rewrites Meta `BuildingBlock` `instanceId`s: restore them from the git diff.
- LFS: `Assets/Models/**` is LFS. A checkout from before the `.gitattributes` fix shows `Ping_Pong_Paddle.fbx` as modified (filter artifact, same bytes); switching branches then needs the file reset.
- Meta Locomotion kit stays deactivated in World. The tracking origin is floor level.
- Disk fills up: free space before Unity builds.

## Notion
Tasks DB "Ping Pong VR - Tasks", data source `b5cbde60-aeb9-44ef-82c8-5d23bdcf4e2d`. Keep rows updated; mark "Rules: let serve" Done.

## Waiting on the user
Headset test of the latest ALPHA, the CI SDK 209 decision, ball audio clips, go/no-go on the offered items above.
