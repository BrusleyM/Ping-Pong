# Game Feel

Extra game feel for Ping Pong Mania, kept separate from the main project in `Assets/Game Feel`.
Nothing in `Assets/_Project` is changed: these scripts only listen to the project's own events.

## How it connects to the project

| Game Feel | Listens to |
|---|---|
| Hit effects, streak, squash, table markers | `PingPongBall.Impact` (paddle, table, net, other) |
| Points, lets, game won, match started | `MatchController.PointWon / Let / GameWon`, `GameModeController.MatchStarted` (via `MatchFeed`) |
| Landing ring, ball machine aim | `BallFlightModel.Simulate / SolveLaunchClearingNet` |
| High scores | `JsonFileStore` (same as the profile and settings) |
| Stadium hides in passthrough | `IPassthrough.IsOn` |

Ball sounds and controller vibration are the project's own (`BallImpactAudio`, `PaddleHaptics`).

## Scoring pieces

| Component | Owns | Added when |
|---|---|---|
| `MatchFeed` | The match score and game start, point, let and win events | always |
| `RallyFeed` (logic in `RallyTracker`) | Paddle hits, combo, landings, net touches, practice streak | always |
| `HitPoints` | The extra hit-point score | `hitPoints` is on |
| `CrowdCheer` | Crowd jumps on start, point and win | `crowdReactions` is on |
| `DemoMatch` | Demo points and games | the scene has no match |

With `hitPoints` off nothing counts points and high scores keep only the longest rally.

## Scenes

Both are made from the **Tools > Game Feel** menu:

- **Create Game Feel Game Scene** copies `Assets/_Project/Scenes/Game.unity` to `Scenes/Game_GameFeel.unity`
  and adds the Game Feel object and the cube stadium. Press Play for a solo match with everything on.
- **Create Test Stations Scene** builds `Scenes/GameFeel_TestStations.unity`: eight stations in a line, each
  showing one feature on its own. No headset needed.

## Test scene controls

| Key | Does |
|---|---|
| 1-8, Left/Right arrows | Move the camera to a station |
| E | Run the station you are standing at |
| Right mouse + move | Look around |
| WASD, Space/Q, Shift | Move, up/down, faster |
| F1 | Overview of the whole line |

Stations: 1 STREAK, 2 HIT, 3 HIT POINTS, 4 CROWD, 5 POINT, 6 WIN, 7 TARGETS, 8 PRACTICE.

## Features (toggles on the Game Feel object)

- Ball: sparks, hit impact flash and shockwave, squash, paddle flash, streak coloured by the last hitter,
  landing ring, table landing markers.
- Start: GET READY, 3, 2, 1, GO! on the stadium screen, then the score. The first serve is held until GO
  (`MatchController.ServeHeld`), for you and for the AI.
- Match: crowd jumps on the start, every point and the win; lights and stadium screen flash in the winner's colour;
  score and announcements (SMASH, RALLY X5, NET, LET) on the stadium screen; confetti on a win.
- Hit points: points for every hit, a rally combo and a landing bonus, shown as labels above the ball
  (white Fredoka text on a plate in the player's colour).
  They never decide the game; the project's rules do.
- High scores: your best hit-point games and longest rally.
- Targets and the ball machine (practice): components ready; not in the project's menu yet.
- Crowd sounds: paused for now (`crowdSounds` is off).

## Removed from the old version

Our own score keeper and rules, the menu console, the ball sound override, our controller vibration,
auto serve and spin: the project already does these, or they need its owner's decision.
