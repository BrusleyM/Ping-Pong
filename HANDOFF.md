# Ping Pong Mania: handoff (2026-11-13)

VR table tennis for Quest. Unity 6000.3.9f1, Meta XR SDK 78, OpenXR, solo vs AI now, Photon Fusion / LAN multiplayer later.
Repo BrusleyM/Ping-Pong (public). Default branch `main`, work branch `develop`. Unity MCP instance: **Ping-Pong@4f493402** (never touch "NEO AR Showroom@4beeff28"; set it before Unity actions).

## Rules for working
- Replies short, minimal changes. Don't poll CI. Fresh session per task. See memory: lean-working-habits, quality-bar, single-source-rule, pr-review-workflow.
- **Tests:** CI runs them; don't run locally. Check Unity console for compile errors, push, act only on CI failure.
- **PR flow:** every PR goes to the fork session "PR (fork)" (`local_ed529ebd-39d6-4aa1-8789-3e2c81fc4fd2`) for review (keep briefs 5-8 lines; skip for trivial changes). The user merges. I never merge.
- **Git:** stage only named files (`git add <paths>`), never `-A` on the whole tree. The working tree has the user's uncommitted Unity changes (URP settings, OVRBuildConfig, PlayerRig, GameScene, Meta Platform sample, `.utmp/`, `_Recovery`, ProjectSettings version lines). Leave them.
- Never read/print secrets or password.txt. The user enters API keys, App IDs and tokens.
- Disk was ~100% full: free space before Unity builds. Git worktrees fail on the LFS avatar files; use the GitHub contents API for small edits.

## State
- develop is at version **0.0.8 (code 210)** (the build uploaded to Meta; fixed by PR 66, was wrongly 0.0.7 (217)). PRs 55-65 are merged; PR 66 (scoreboards, banner, restart) and PR 67 (World prefab, stacked on 66) are open.
- **Scene:** `Assets/_Project/Scenes/test scene.unity` (Meta Camera Rig + interaction rig). It is the scene the build launches; `GameScene.unity` is kept but disabled and has stale overrides (decide: delete or re-run setup).
- Scene: `test scene.unity` is a thin scene (Directional Light + one `World` prefab instance). Everything lives in `Assets/_Project/Prefabs/World.prefab` (World > Environment, Match, Player, UI, Platform > leaf prefabs; one shared `Scoreboard.prefab` at both ends). Edit prefabs in Unity and apply overrides to the prefab that owns them; there is no scene setup tool any more (deleted in PR 67; git history has it).
- **Menu** (ModeMenuMeta): Meta canvas ray/poke templates; opens at 0.55 m. Start picks a level, starts solo vs AI, paddle goes to the right hand. Only grip (grab paddle) and the left menu button remain on the controllers.
- Paddle: controllers use the snapped toggle grip (`PaddleGrip`); bare hands keep Meta's free grab.
- Rules done: let serves, receiver-volley let. AI returns serves and holds the paddle face-on.

## Release pipeline (merged, not yet run for real)
- Version lives only in ProjectSettings (`bundleVersion`, `AndroidBundleVersionCode`).
- Editor menu **Build > Develop / Staging / Production / Check Build**. Develop bumps z; Staging bumps y, z=0; Production bumps x, y=z=0; every build bumps the code, above the highest released code (read from annotated `v*` tags, subject `<Kind> build X.Y.Z (CODE)`).
- CI: a push to **develop or main** whose commit message (or merged PR title) contains `[Develop]`, `[Staging]` or `[Production]` builds, commits the version back (`[skip ci]`), tags `vX.Y.Z`, then uploads to Meta **ALPHA**. Main without a keyword promotes ALPHA to BETA. PRs run a check build. Use merge commits, not squash.
- Secret `RELEASE_PUSH_TOKEN` is set. **First release is untested:** next `[Develop]` should give 0.0.9 (211); watch it once.
- A PR check failed once from the runner running out of disk (re-run passed). Optional fix, not applied: move the "Free disk space" step before "Restore Unity Library" in `.github/workflows/main.yml`.

## Notion
Tasks DB "Ping Pong VR - Tasks", data source `b5cbde60-aeb9-44ef-82c8-5d23bdcf4e2d` (https://app.notion.com/p/e5a83ef5d8004edf80449d589076edf8). Keep rows updated.

## Next tasks (by value)
1. **Test on the Quest** (user): menu ray/poke/hand poke at 0.55 m, paddle snaps to right hand on Start, serve and rally, paddle lag. Tune `_pitch` / `_bladeDistance` on `PaddleGrip`.
2. Run the first `[Develop]` release and watch it (see above).
3. Rules: AI follows the rules (no volleys over table), best-of 3/5/7 + change ends, edge-ball / net-touch.
4. LAN multiplayer (no internet): spike the transport first (Fusion needs Photon Cloud; NGO + Unity Transport works direct over Wi-Fi), then discovery/join, host-authoritative sync, offline table alignment, two-Quest test.
5. Housekeeping: GameScene decision; CI exit-139 crash after tests; CI speed; optimise scripts/models/textures for Quest; B5 list (binding display strings, localization, seedable AI RNG, `.inputactions`, IHaptics.Pulse(Hand), release paddle when controller untracked, late-latch lag).

## Waiting on the user
Photon Fusion/Voice SDK + App IDs (only if Fusion is chosen), Meta Platform App ID (Edit Settings), Quest testing.
