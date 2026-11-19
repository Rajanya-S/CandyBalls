# Candy Balls — a match-3 game for Unity 2D

> **🎮 Play it right now in your browser:** **https://rajanya-s.github.io/CandyBalls/**
> No download, no Unity needed — the same game, rebuilt in pure HTML/CSS/JavaScript and served by
> GitHub Pages from `index.html` + `style.css` + `game.js`.

A complete, playable Candy Crush–style game with colourful glossy balls. Swap two neighbouring
balls to line up **3 or more of the same colour**. They pop, the balls above fall down, new ones
drop in, and chains of matches multiply your score.

Everything is generated in code: **there are no image, audio, font or prefab files to import.**
The balls, the board, the particles and even the sound effects are created at runtime.

```
 Assets
 ├── Scenes
 │   └── Game.unity         the playable scene: camera, board, HUD, all wired up
 ├── Scripts
 │   ├── Core
 │   │   ├── GameManager.cs  camera, layout, score, moves, win/lose
 │   │   ├── Board.cs        grid, matching, swapping, gravity, cascades, hints
 │   │   ├── Ball.cs         one ball + its animations
 │   │   ├── BallPalette.cs  the ball colours
 │   │   ├── BoardInput.cs   swipe and tap-to-swap input
 │   │   ├── GameBootstrap.cs fallback auto-start (only used in a scene with no GameManager)
 │   │   └── Easing.cs       tween curves
 │   ├── Visuals
 │   │   ├── SpriteFactory.cs draws balls, panels, gradients, rings, dots
 │   │   └── Effects.cs      particle bursts when balls pop
 │   ├── UI
 │   │   ├── Hud.cs          score, moves, target, banners
 │   │   ├── FloatingText.cs "COMBO x3" popups
 │   │   └── UIFont.cs       font lookup that works on any Unity version
 │   ├── Input
 │   │   └── PointerInput.cs mouse/touch wrapper (old + new Input System)
 │   └── Audio
 │       └── Sfx.cs          synthesised blips and jingles
 └── Editor
     ├── CandyBallsSceneBuilder.cs rebuilds Game.unity (menu: Candy Balls ▸ Rebuild Game Scene)
     └── SceneSmokeTest.cs       headless play-mode sanity check
```

## Play in the browser (GitHub Pages)

**https://rajanya-s.github.io/CandyBalls/**

The `index.html` / `style.css` / `game.js` trio at the root of this repo is a complete, standalone
match-3 game with the exact same rules as the Unity version (8×8 board, 6 colours, 30 moves,
2000-point target, 20 points per ball × cascade multiplier). It uses a canvas for the glossy balls,
Web Audio for the sounds, and `localStorage` for the best score — no libraries, no assets, no build
step. Open `index.html` locally or just use the Pages link.

| Action | Browser | Unity |
| --- | --- | --- |
| Swap two balls | tap/click both, or drag one onto its neighbour | swipe or tap-to-swap |
| Restart | `R` or ↻ button | `R` |
| Mute | `M` or 🔊 button | `M` |
| Best score | saved in `localStorage` | saved via `PlayerPrefs` |

## Getting it running (the Unity version)

**Option A — open this folder directly (easiest)**

1. Open Unity Hub → **Add** → **Add project from disk** → pick this folder.
2. Open it with any Unity **2021.3 or newer** (Unity 6 works too). If Hub asks about the editor
   version, let it use one you have installed.
3. Open **`Assets/Scenes/Game.unity`** and press **Play**.

The scene already contains everything: a `Main Camera`, a `Candy Balls` object holding the
`GameManager`, the `Board` (with its `Balls`, `Tiles` and `Panel` children) and the `HUD` canvas
with its text elements. Every reference is assigned in the inspector, so you can watch the score
update, drag the board around, or tweak colours while the game runs.

**Option B — drop it into an existing project**

1. Create a new project from the **2D** template.
2. Copy the `Assets/Scripts` folder into your project's `Assets` folder.
3. Press **Play** in *any* scene — `GameBootstrap` builds the whole game for you.

**Rebuilding the scene.** If you change the object layout and want to start over, use
**Candy Balls ▸ Rebuild Game Scene** in the menu bar. It regenerates `Assets/Scenes/Game.unity`
from scratch and adds it to the build settings.

> The game still boots itself from code if you press Play in an empty scene, so nothing is
> fragile — the scene is there for convenience and inspection, not because the code depends on it.

## Controls

| Action | Input |
| --- | --- |
| Swap two balls | **Drag** a ball onto a neighbour, or **tap** one ball then tap the neighbour |
| Restart the level | **R**, or tap the text at the bottom of the screen |
| Mute the sound | **M** |
| Get a hint | Wait ~6 seconds — a valid move starts pulsing |

## How the game works

**Matching.** `Board.FindMatches()` scans every row and column for runs of three or more balls of
the same colour, returning each ball once even when a horizontal and a vertical run cross.

**A swap is only legal if it creates a match.** The board swaps the two balls, checks for matches,
and if there are none it swaps them straight back and plays a "nope" sound.

**Cascades.** After a successful swap the board loops: find matches → pop them → let balls fall →
spawn new ones at the top → check again. Each loop is a *cascade*, and the multiplier grows with it,
so a lucky chain can be worth far more than the original match. The loop ends when the board is
stable.

**Never stuck.** If a swap would leave no possible move, the board automatically reshuffles the
colours (you'll see the balls pulse) so the player always has something to do. The idle hint timer
uses the same move-finder.

**Winning.** You have 30 moves to reach 2000 points. Hit the target and you win immediately;
run out of moves and the level ends.

## Making it your own

Select the `Candy Balls` object in the hierarchy while the game is playing — `GameManager` exposes
all the important knobs in the inspector:

| Field | Default | What it does |
| --- | --- | --- |
| `columns` / `rows` | 8 / 8 | Board size |
| `colors` | 6 | How many ball colours are in play. **Fewer = easier** (more matches), more = harder |
| `cellSize` | 1 | Distance between ball centres |
| `ballSize` | 0.86 | Ball diameter relative to the cell |
| `startingMoves` | 30 | Moves per level |
| `targetScore` | 2000 | Score needed to win |
| `backgroundBottom` / `backgroundTop` | dark purple | Background gradient |
| `panelBottom` / `panelTop` | purple | Board panel colours |
| `textColor` / `accentColor` | near-white / gold | HUD colours |

Want different ball colours? Edit the `Colors` array in `Assets/Scripts/Core/BallPalette.cs` — add
up to 8 entries, then raise `colors` on the GameManager.

Want faster or slower animations? The timings are named constants at the top of `Board.cs`
(`SwapDuration`, `FallDuration`, `ClearDelay`) and in `Ball.cs`.

Other useful constants:

- `Board.PointsPerBall` (20) — base score per cleared ball.
- `BoardInput.HintDelay` (6 seconds) — how long before a hint appears.
- `ParticleBurst.Lifetime` — how long pop particles live.

## Notes

- **Input System.** The game works with either the old Input Manager or the new Input System
  package; `PointerInput.cs` detects which one your project uses. Nothing to configure.
- **Fonts.** Unity renamed its built-in font in 2022.2, so `UIFont.cs` asks for the right name for
  your Unity version first (`Arial.ttf` on 2021.3, `LegacyRuntime.ttf` on 2022.2+), then falls back
  to the other and finally to an OS font. If your HUD text is invisible, check the Console for a
  `[CandyBalls]` warning.
- **Unity UI module.** The HUD uses `com.unity.ugui`, which ships with every Unity install and is
  already listed in `Packages/manifest.json`.
- **Disabling auto-start.** Set `GameBootstrap.AutoStart = false` before the first scene loads, or
  simply place your own `GameManager` in the scene — the bootstrap detects it and steps aside. The
  supplied `Game.unity` scene already does this.
- **Automated check.** **Candy Balls ▸ Run Scene Smoke Test** plays the scene headlessly, performs a
  real swap and reports `[SMOKE] PASS` in the Console. It is what I used to verify the project
  compiles and runs.

## Ideas if you want to keep going

- Special balls: match 4 to make a striped ball, match 5 for a colour bomb.
- Level progression with a target score that increases each round.
- Swap-in animations and a proper combo streak bar.
- Save the best score with `PlayerPrefs`.
- Add a `ScriptableObject` for level definitions instead of inspector fields.
