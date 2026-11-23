# Figma Make revision prompt: in-game HUD (scoreboard and point banner)

Paste this into the existing Figma Make project ("Review VR Ping Pong Brief"). It adds screens; it does not change the existing menu screens.

---

Revise this project: ADD the in-game HUD screens below. Keep every existing screen, token, font and component exactly as they are. Reuse the same party look (bold rounded panels, chunky shadows, confetti dots) so the HUD feels like the same game as the menus.

## Where these are seen
VR headset (Meta Quest), mixed reality or a neon room behind them. They are world-space panels the player glances at while playing table tennis, from 2 to 3 metres away, mid-rally and often at an angle. So: very large type, very high contrast, almost no small text, nothing the player has to read for more than a second. Backgrounds around the panels are transparent (design them over a dark photo and over a bright photo to prove they read on both).

## Design tokens (unchanged)
Primary #FF3F88, secondary #18BDEB, accent #FFD43B, success #49DDA7, danger #F24F5E, surface #2D195E, surface raised #47267C, ink #23113F. Titles: Fredoka Bold. Body and numbers: Nunito ExtraBold. Team colours: the same red and blue used for the Red / Blue chips on the profile screen.

## Screen 1: Scoreboard (frame 1200 x 400)
Hangs above the net, readable from both ends of the table. Two score plates side by side, one per player.
- Left plate = the local player, right plate = the opponent. Each plate: team colour, a short name on top (PLAYER, AI, or OPPONENT; in solo play the opponent reads "AI" with the level name, for example AI Easy), and one huge score number (0 to 11 and beyond, two digits must fit without shrinking the type).
- A SERVE marker on the plate of whoever is serving: a small ball icon plus the word SERVE. Only one plate shows it at a time.
- A slim divider or net motif between the plates. No other chrome.
Variants to show (as component variants): 0 : 0 and local serving; 7 : 9 and opponent serving; 11 : 10 (two-digit score on one plate); score just changed (a "punch" state where the new number is 1.45 times bigger with a glow ring, it settles back over 0.35 s).

## Screen 2: Point banner (frame 1600 x 300)
A wide strip that pops in front of the player for 1.4 s after every point, then pops out. One line of big text plus a small second line for the reason.
- Variants, each with its own colour and icon:
  1. Point won: success green. Text "You score". Reason line examples: "not returned", "out", "double bounce".
  2. Point lost: danger red or pink. Text "AI scores" (or "Opponent scores"). Same reason lines.
  3. Let: secondary cyan, net-tap icon. Text "Let - serve again". No reason line.
  4. Game won: accent gold, crown or trophy icon, confetti. Text "You win!". This one stays on screen (no auto dismiss).
  5. Game lost: calm, surface purple with an accent border, no confetti. Text "AI wins!" (opponent's name and level can be longer, for example "AI (Chill Rally) wins!"; design for up to 28 characters).
- Reason lines the design must hold (lower case, one line): serve must bounce on your side first; serve bounced twice on the server's side; fault; bounced on own side; double bounce; double hit; volley over the table; long; not returned; out. The longest is about 40 characters, so the reason line is smaller than the main line but never below 48 px at this frame size.

## Motion notes (match the code, so developers can copy them)
- Banner: pop in 0.28 s (scale from 0.6 with a slight overshoot), hold 1.4 s, pop out 0.2 s. Game-won banner stays.
- Score punch: 1.45 times bigger, back to 1 over 0.35 s.
- No full-screen flashes, no rapid flicker, nothing that moves faster than the pop timings above (VR comfort).

## Deliverables
- Frames: HUD / Scoreboard and HUD / Point banner, each with the variants above, laid out with auto layout so text can change length.
- Components for: score plate, serve marker, banner strip, banner icon.
- A sprite export list with names in the same style as before (for example ui_score_plate_red, ui_score_plate_blue, ui_serve_ball, ui_banner_strip, ui_banner_icon_win, ui_banner_icon_lose, ui_banner_icon_let, ui_banner_icon_crown), 9-slice friendly panels, and the text styles used (font, size, weight, colour) listed next to each element so they can be copied exactly into Unity.
- Show each frame once over a dark background and once over a bright background.

Do not add: a pause menu, a game-over screen (both exist), a minimap, a timer, or any extra counters.
