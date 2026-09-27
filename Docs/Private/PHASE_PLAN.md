# Operation Cross-Fire: Phase Plan

> Companion to `IMPLEMENTATION_PLAN.md`. That document is the source of truth for *how* things are built;
> this one is the running order, the progress log and the pick-up point. Working rules live in `CLAUDE.md`.

---

## Current status

*(Updated 2026-09-25.)* **SUBMITTED. All phases done except 6.5 (interview prep).** The final commits are
`c529c41` (cleanup + evidence) and `cd112e4` (README), pushed to the **public** repo
https://github.com/SAM33D/Operation-CrossFire-2D. The working tree is clean. Sam emails SquadLoom the repo link.

**What's in the submission:**
- Game: meets every gameplay, technical and acceptance item in the spec (final line-by-line check, 2026-09-25).
- `Docs/Evidence/`: 3 Editor profiler screenshots, 5 phone screenshots (`Gameplay ScreenShots/`) and the phone video
  (`Gameplay Video/Mobile Gameplay Screen Recording.mp4`, 6 MB).
- README: complete (sections 1–11). Top screenshot = `Mid Game.jpeg`. Device: Samsung Galaxy A32, Android 13.

**Decisions and notes from the final session (2026-09-25):**
- **UI polish by Sam:** `StartPanel/GameName` title, bordered Start/Restart buttons, Player1 AimControl made cyan,
  StartPanel/EndPanel moved last in Canvas. Raycast Target turned off on everything except the Start/Restart
  buttons, their Backgrounds and the two overlay panels.
- **Phone test passed.** The hazard `baseSpeed` values were lowered (Enemy 2.5→2, Debris 2→1.6, Breach 1.5→1.2) and so
  was Spawner `enemyProjectileBaseSpeed` (5→4).
- **Known issue, not fixed:** the corner control panels hide the ship and hazards. The planned fix (option A,
  listed in README §11) is to raise `Playfield.MinY` to the top of the panels using a `controlsArea` RectTransform.
- **No on-device profiling:** Sam doesn't develop at home (no kit, no time to get it). The Editor profile of the
  Critical phase shows 0 B from the game's scripts; PlayerLoop is 131 B, all Unity internals (the spec allows
  that). Pool peaks / prewarm: Laser 6/20, Enemy 6/15, Debris 3/10, Breach 3/6, EnemyProjectile 5/30. Sizes left
  unchanged.
- **README:** Sam rewrote some sections in their own words. The Assumptions table was trimmed to 8 rows at
  Sam's request.
- `WinScreen.png` shows "Patrol" at 0s (probably taken in the Editor with a shortened round) and was committed
  unchanged.
- The spec PDF has moved out of the project to `C:\Users\Sameed\Downloads\Junior Unity Dev Exercise_ _Operation
  Cross-Fire_ Protecting the Orbital Corridor_.pdf`. `Docs/Private/` no longer exists.
- Codebase guide PDF: `C:\Users\Sameed\Desktop\Operation CrossFire - Codebase Guide.pdf`.

**Remaining (optional / post-submission):**
1. **Phase 6.5: interview prep.** The spec says: "Be ready to explain the architecture, input ownership, role
   switching, pooling lifecycle, and every line of submitted code—including AI-generated code—and make a small
   live change." Build on the codebase guide PDF, and practise live changes (e.g. the panel layout fix above).
2. Replace `WinScreen.png` if Sam gets a real end-screen shot.

### Deferred items

| Item | Plan says | Current state | When |
|---|---|---|---|
| ~~Physics 2D Simulation Mode~~ | `Update` (§4.1) | **Resolved 2026-09-24:** explained at the start of Phase 2, and Sam set it to `Update`. | Done |
| ~~Git~~ | `git init` in Phase 0, commit every phase (history is graded) | **Started 2026-09-25 (honest late start).** Claude ran `git init -b main`. Sam's first commit `3170d2e` ("Core game: …", 193 files, description says git started partway) is pushed to `github.com/SAM33D/Operation-CrossFire-2D` (made **public** for submission). The planning docs are excluded via `.git/info/exclude`. README committed last in `cd112e4`. | Sam commits via GitHub Desktop after each working step. Claude gives Summary + description, written for someone who has only seen the game (never "Phase N"). |
| Spec Coverage Check (§0.2) | Mandatory before code | **Waived by Sam.** The PDF (now in `Downloads`, see Current status) is for reference only. A full line-by-line check was finally done on 2026-09-25 and passed. It was read once, in 2c, to answer Sam's aiming question. Nothing in it contradicted the plan, but it was not a formal line-by-line check. | Only open it when Sam points to something. List the waiver under README → Assumptions. |

---

## Progress log

### Phase 0 · Project setup ✅ (2026-09-23)
- Universal 2D project, Unity `6000.6.2f1`, folder `Operation CrossFire 2D`.
- Active Input Handling = `Both`; landscape left/right only.
- Six layers at indices 6–11: `Ship`, `PlayerLaser`, `Hazard`, `BreachHazard`, `EnemyProjectile`, `Boundary`.
- Physics 2D collision matrix verified against §4.3 (decoded from `Physics2DSettings.asset`). The 3D matrix was
  touched by mistake and is back at Unity's default; the game doesn't use 3D physics.
- Sprites in `Assets/_Project/Sprites`: Circle, Square, Triangle, IsometricDiamond. Unity has no plain diamond
  and the isometric one is squashed, so **the breach hazard will use Square rotated 45°** (Phase 3).
- Scene moved to `Assets/_Project/Scenes/Main.unity` (in build list); camera orthographic size 5, dark background.
- TMP Essential Resources imported.
- Claude Code: removed the template's `Assets/Welcome/` and `Assets/Scripts/`, created the §4.5 folders plus
  `Docs/Private/` and `Docs/Evidence/`, wrote `.gitignore` (Unity + `Docs/Private/`) and the README skeleton.
- *Commit (later):* `Initial Unity 6 project setup: layers, collision matrix, landscape, physics in Update`

### Phase 1 · Core loop skeleton ✅ (2026-09-23)
**Scripts:** `Core/GameManager.cs`, `Core/PhaseData.cs`, `Core/Playfield.cs`, `Core/BoundaryZone.cs`, `UI/UIManager.cs`.

**Scene built by Sam:**
- `GameManager` (GameManager script, phases table = Patrol 0 / Alert 20 / Critical 40)
- `Playfield` (Playfield script) with children `BottomEdge`, `TopCleanup`, `LeftCleanup`, `RightCleanup`, each with
  layer Boundary, BoxCollider2D trigger and BoundaryZone (only BottomEdge has Is Bottom Edge ticked)
- `Canvas` (Screen Space Overlay, 1920×1080, match 0.5) with UIManager, `TimerText`, `ScoreText`, `PhaseText`,
  `StartPanel/StartButton` → `StartRound`, `EndPanel/EndResultText` + `RestartButton` → `RestartRound`
- `EventSystem` with Unity's default module (see decisions)
- Physics 2D gizmos: Always Show Colliders on

**Decisions made during the phase:**
- GameManager only references systems that exist so far (`playfield`, `ui`). Each later phase adds its own
  references and `Tick()` calls. `fluxCountdownSeconds` arrives with the countdown in Phase 4.
- `TriggerQuantumFlux()` only advances `phaseIndex` and the phase text. The full §6.3 sequence is Phase 4.
- The HUD is timer, score and phase for now. Hull icons come in Phase 3, role panels in Phase 4.
- Timer shows remaining whole seconds (`Mathf.CeilToInt`), and the text is rebuilt only when the number changes.
  `EndRound` calls `ui.Tick()` once, so a win shows `0`.
- End text: `MISSION COMPLETE` / `MISSION FAILED` plus the reason (`Corridor secured`, `Hull destroyed`,
  `A breach hazard got through`).
- **EventSystem:** kept Unity's default Input System UI module instead of §4.1's Standalone Input Module.
  It only drives the two buttons and works with Input Handling = Both.
- **Boundary layout** (Playfield constants): walls are 1 unit thick. Spawn line is 1 unit above the screen top.
  The top, left and right cleanup walls are `cleanupMargin` = 2 units outside the screen, so the top wall is above
  the spawn line. The bottom wall's top edge sits exactly on the screen bottom because it is also the breach line.
- **Known trade-off to revisit in Phase 3:** hazards are returned to the pool as soon as they touch the bottom
  wall, while still mostly visible (they pop out). Possible fix if Sam wants it: lower the bottom wall and do the
  breach check by position instead.

- *Commit (later):* `Add GameManager round timer, phase table, playfield bounds and basic HUD`

### Phase 2 · Pooling, ship & editor controls ✅ (split into 2a / 2b / 2c)
Physics 2D Simulation Mode set to `Update` at the start of the phase (explained to Sam, and verified in the settings file).

**2a · Pooling ✅ (2026-09-24)**
- **Scripts:** `Pooling/PooledObject.cs`, `Pooling/ObjectPool.cs`, `Projectiles/Projectile.cs`. GameManager gained
  `poolsToPrewarm`, `PrewarmPools()` (in `Start` right after the Playfield) and `LogPoolPeaks()` (in `EndRound`,
  `Debug.isDebugBuild` only).
- **Scene / prefabs:** `Prefabs/Laser` is a Square scaled 0.1×0.4, cyan `00E5FF`, layer PlayerLaser, Rigidbody2D
  Dynamic / gravity 0 / Continuous / freeze Z rotation, BoxCollider2D trigger, Projectile. The `LaserPool` object
  (ObjectPool, prewarm 20) is in GameManager's Pools To Prewarm.
- **Decisions:** `Projectile.Launch` builds its rotation from a Z angle (`Atan2`) instead of `FromToRotation`, which
  flips unpredictably for straight-down shots. Velocity is set after `SetActive(true)`.
- **Tested:** 20 inactive clones at startup, no new objects during play, peak log at round end. Flight and
  cleanup at the walls get tested in 2c.
- *Commit (later):* `Add object pool and pooled projectiles`

**2b · Input & Pilot ✅ (2026-09-24)**
- **Scripts:** `Input/InputHandler.cs` (keyboard only: `MoveDirection`, `BoostPressed`), `Ship/TimedAbility.cs`
  (including `Cancel()` for Phase 4), `Ship/ShipPilot.cs`. GameManager gained `input` and `pilot` references, an
  `Input` property, `pilot.Initialize()` after `PrewarmPools()`, and the Update order input → timer → pilot → UI.
- **Scene:** `Ship` is a Triangle, cyan, scale 1.5×1.2, layer Ship, Rigidbody2D Kinematic, PolygonCollider2D trigger,
  ShipPilot (8 / 1.5 / 1.75, Boost 1 s / 4 s). `InputHandler` is an empty object with InputHandler. Sam added
  separator objects (`-----Managers-----`, `-----Pools-----`, `-----Canvas-----`) as Hierarchy labels.
- **Decisions:** added `heightAboveBottom = 1.5` to ShipPilot, the only field outside §7.8, because the plan says
  "near the bottom" without a value. Input cancelling and D10 wait for Phase 4.
- **Scene audit (read-only, 2026-09-24):** all references and OnClicks are wired correctly. Found Round Duration
  left at 5 from testing, and asked Sam to set it back to 60. Cosmetic: LaserPool transform not reset,
  `'LeftCleanup '` has a trailing space, the object is named `PlayField`. The IsometricDiamond sprite was deleted by Sam.
- *Commit (later):* `Add ship pilot movement and boost`

**2c · Gunner ✅ (2026-09-24)**
- **Spec check:** Sam asked why a phone shooter needs a reticle, so the spec PDF was read (at Sam's request). It
  requires aiming in six places: the Gunner "controls aim", mouse aiming in the Editor, "dragging in the aiming area
  moves the reticle", the weapon "aims toward the reticle", the laser "travels from ship toward reticle", and the
  acceptance criteria. The mockups show straight-up shots only because the reticle happens to be above the ship.
  Kept as planned. Sam will play-test the feel.
- **Scripts:** `Ship/ShipGunner.cs` (regions Aiming / Firing / Shield). InputHandler gained a `gameCamera` field and
  the outputs `FireHeld`, `ShieldPressed`, `AimDragDelta` (always zero until Phase 5), `HasMouseAim` and
  `MouseAimPoint`. GameManager gained `gunner`, with `gunner.Initialize()` after the pilot's, and the Update order
  input → timer → pilot → gunner → UI.
- **Scene:** Ship children `Muzzle` (local 0, 0.5) and `ShieldVisual` (Circle, scale 1.35×1.7 to cancel the ship's
  1.5×1.2 scale, cyan at alpha 0.3, order -1, no collider). Top-level `Reticle` (Circle 0.3, white, order 10, no
  collider). ShipGunner is wired with 14 / 0.25 and Shield 1.5 s / 5 s. InputHandler has the camera.
- **Decisions:** the reticle is a separate top-level object, so it keeps its aim while the ship moves. Constants
  (not in the inspector): the reticle starts 3 units above the muzzle and has a minimum of 0.5 above it. The fire
  timer counts down whatever the input and uses `fireTimer += fireCooldown`, so the cadence is exactly 4/s and
  spam-clicking can't beat it. `ShipPilot`/`ShipGunner.CancelActions()` wait for Phase 4.
- **Scene audit:** all correct. **Round Duration is still 5.** Sam must set it back to 60 before Phase 3 testing.
- *Commit (later):* `Add gunner aiming, firing cadence and shield`

**Phase 2 complete (2026-09-24).**

**Comment cleanup (2026-09-24, Sam's request):** all 11 scripts rewritten to minimal comments: a one-line class
summary plus a few one-line notes on non-obvious guards (double-return guard, velocity after `SetActive`, fire timer
running regardless of input, timeScale surviving a reload, gunner initialized after the pilot, corner overlap).
Tooltips trimmed. No logic or serialized field names changed. Plan §2.11 and CLAUDE.md were updated to the new rule.

### Phase 3 · Threats, damage, win & lose ✅ (split into 3a / 3b)

**3a · Hazards & Spawner ✅ (2026-09-24)**
- **Scripts:** `Threats/Hazard.cs`, `Threats/Spawner.cs`. GameManager gained `spawner`, a `Spawner` property,
  `spawner.Initialize()`, `ApplyPhase` + `Begin` in StartRound, `ApplyPhase` in `TriggerQuantumFlux` (the flux's
  "apply new phase values" step lands early), `spawner.Tick` after the gunner, and `spawner.Stop()` in EndRound.
- **Prefabs:** Enemy (Square 0.7, `FF4040`, Hazard, Box trigger, 1/10/2.5, shoots every 1.8 s), Debris (Circle 0.9,
  `9AA0A8`, Hazard, Circle trigger, 2/15/2.0), BreachHazard (Square rotated 45°, scale 0.6, `FF8C1A`, 3/25/1.5,
  breach), EnemyProjectile (a copy of Laser, `FF4040`, EnemyProjectile layer). All are Dynamic, gravity 0, freeze Z.
- **Pools:** EnemyPool 15, DebrisPool 10, BreachPool 6, EnemyProjectilePool 30, all in Pools To Prewarm.
- **Decisions:**
  - Hazards reset to the **prefab's own rotation** (cached in Awake) instead of identity, so the diamond stays a
    diamond. That still satisfies "reset rotation".
  - Spawn chances are fractions of all spawns: debris is checked first, so Patrol is exactly 30% debris. The plan's
    breach-first order would give 45% debris in Patrol.
  - Damage feedback darkens the colour: `Lerp(black, original, 0.4 + 0.6 × health/max)`.
- **Scene audit:** all wiring correct. **Bug found: the BreachHazard prefab is on layer Hazard (8), not
  BreachHazard (9).** It would damage the ship in 3b, breaking D7. Sam asked to fix.
- *Commit (later):* `Add pooled hazards and spawner with phase multipliers`

**3b · Ship health, hull HUD & loss conditions ✅ (2026-09-24)**
- **Scripts:** `Ship/ShipHealth.cs` (on Ship; `[RequireComponent(ShipGunner)]`). UIManager gained `hullIcons` +
  `SetHull()`. GameManager gained `health`, with `health.Initialize()` after the gunner, `ui.SetHull(health.Hull)`
  in Start, `health.Tick` between gunner and spawner, and **`OnHullChanged(int)` replacing `OnHullDestroyed()`**.
- **Decisions:**
  - ShipHealth reports to GameManager instead of calling the UI directly: GameManager updates the HUD and decides
    the loss.
  - The trigger check uses the base class `PooledObject`, so one check covers Hazard and Projectile. The matrix
    guarantees only enemies, debris and enemy shots arrive.
  - A hit fades the ship to alpha 0.5 during invulnerability.
- **Scene (Sam's own layout):** a `Hull HUD` container under Canvas (stretched full) holding `HullLabel` and
  `HullIcon1–3` (Images 35×45, cyan, anchor and pivot top-left, y -160). Explained anchor, pivot and position to Sam.
  Audit: all wired; HullLabel still has Raycast Target on (harmless).
- *Commit (later):* `Add ship hull, invulnerability, scoring and loss conditions`

**Phase 3 complete (2026-09-24)**: a full round can be won or lost.

### Phase 4 · Quantum Flux & role panels (built and wired; awaiting Sam's test)
- **4a code:** full `TriggerQuantumFlux()` in the spec's order. Countdown from `UpdateRoundTimer` (one timer).
  `InputHandler.CancelAllInput()` + `devInputLocked` (D10). `ShipPilot/ShipGunner.CancelActions()` (D9). UIManager
  banner (`ShowFluxCountdown`, `ShowFluxBanner`, auto-hide 1.5 s) and `RefreshRoles`. `EndRound` cancels input.
  Dropped the idea of cancelling input in StartRound: Button OnClick fires on release, so it isn't needed.
- **4b code:** `Input/TouchControl.cs` (the `ControlType` enum + a `Contains` hit-test; not used until Phase 5).
  UIManager `PlayerPanel` = frame, roleLabel, pilotControls, gunnerControls, boost/shield cooldown fills.
  `RefreshRoles` toggles the layouts and colours the frame and label; `SetCooldowns` is called every frame from
  GameManager. Initial `pilotPlayer = 1` + `RefreshRoles` moved into `Start`.
- **Styling (Sam's request, 2026-09-24):** colour follows the **role**, not the player: Pilot is cyan, Gunner is
  red, as in the spec mockup. Sam confirmed. Pilot controls are cyan-bordered on dark cyan with white text and
  white triangles (◀ ▶ ▲). Gunner controls are red-bordered on dark red; Fire is a stronger red. The big AIM pad is
  on the left, with FIRE/SHIELD stacked on the right (Sam's first note had this reversed, and it was corrected).
  Borders are made from nested Images (a border-colour Image with a darker child inset 4 px). Labels are centred.
- **Scene audit (2026-09-25):** both panels are built; all 12 UIManager panel references are correct; P1 is
  anchored bottom-left (40, 40) and P2 bottom-right (-40, 40), 620×300; the fills are Filled/Vertical/Bottom
  with a sprite; the layout groups are right. **Bugs:** FireControl's TouchControl type is Aim and ShieldControl's
  is Boost, in both panels, so Phase 5 touch would break. Raycast Target is still ticked on most panel graphics
  (performance only). The leftover Layout Element on FireControl is harmless.

### Phase 5 · Touch controls & device build (in progress; started before Sam's Phase 4 test, at Sam's request)
- **Touch code (2026-09-25):** InputHandler gained `touchControls[]` (all 12), `aimDragSensitivity` (**default 2**,
  not the plan's 1: with a ~350 px pad, 1:1 dragging covers only ~3 world units per swipe), the
  `touchOwners` Dictionary(10) keyed by fingerId, `holdCounts[]`, and `unitsPerPixel`. On Began, the touch is
  hit-tested against active TouchControls and owns that control until Ended/Canceled; unowned touches are ignored.
  Only Aim touches produce `AimDragDelta`. Touch and dev inputs are combined into MoveDirection/FireHeld.
  `CancelAllInput` clears owners and counts.
- **Mouse aim change:** `HasMouseAim` is now true only on frames where the mouse moved, so drag aiming
  (Unity Remote in the Editor) isn't overridden every frame. The reticle stays put when the mouse is still.
- **Ability fills redesigned (Sam, 2026-09-25):** the spec only says "show simple cooldown indicators" (no
  direction or style). New behaviour: hidden when ready; a **light ActiveFill** drains over the duration, then a
  **dark CooldownFill** drains over the cooldown (Filled/Vertical/Origin Bottom, so it shrinks top to bottom).
  `TimedAbility.ReadyProgress01` was replaced by `ActiveRemaining01` + `CooldownRemaining01`.
  `UIManager.SetAbilityFills(boost, shield)` replaces `SetCooldowns`, and is also called once in Start so the
  fills are empty before the round. PlayerPanel gained `boostActiveFill` / `shieldActiveFill`.
  Sam's call: the fill **GameObjects** start switched off in the scene; `SetFill` switches each on only while it
  is draining and off when empty (SetActive only on change). Sam's child order puts CooldownFill above the label
  (dims the whole button during cooldown), deliberately.
- **Scene audit (2026-09-25):** all 12 TouchControls are in InputHandler's list, sensitivity 2. All 8 ability-fill
  references are correct (Filled/Vertical/Bottom; Active white α80, Cooldown black α150). P1 Fire/Shield Types are
  still wrong.
- **Input backend switched to Old only (2026-09-25):** the first APK attempt warned that "Both" may not work on
  Android, and Sam cancelled the build. Gameplay only uses legacy `Input.*`; the only new-Input-System user was
  the EventSystem's default UI module (the Phase 1 decision). Sam set Active Input Handling = Input Manager (Old)
  and replaced the module with **Standalone Input Module**, which is what §4.1 first specified. No code change.
  **Audit:** `activeInputHandler: 0`, EventSystem = EventSystem + Standalone module (legacy axes exist), no
  leftover Input System references in the scene. The build attempt made Unity-written changes: URP shader
  prefilter flags, `Assets/Resources/PerformanceTest*.json` (from the performance-testing package, which the
  test framework pulls in), and `InputSystem_Actions` added to Preloaded Assets (harmless now).
- **Still to do:** ~~Sam wires the 12 TouchControls~~ (done); test via Unity Remote or a device; Android build settings;
  device video.

### Bomb ability ✅ (2026-09-27, added after submission)
- **Sam's idea:** a Bomb button that instantly clears every enemy on screen, with a cooldown like Boost and Shield.
  Not in the spec (README Assumptions says so).
- **Decisions (Sam):** it's a **Gunner ability** on both players' Gunner layouts (Sam first placed it only on
  Player 2's). It kills every enemy, debris and breach hazard, each giving its normal score, and clears enemy shots.
- **Scripts:** `ControlType.Bomb`; InputHandler `BombPressed` (touch + `B` key in the Editor); `TimedAbility`
  treats duration 0 as instant (the cooldown starts on use); ShipGunner `bomb` (0 s / 10 s) → `Spawner.ClearAllThreats()`;
  `Hazard.Kill()` (score + return, shared with laser kills); ObjectPool keeps `allObjects` with `AllObjects` +
  `ReturnAllActive()`; UIManager `bombCooldownFill` per panel; `SetAbilityFills(boost, shield, bomb)`.
- **Scene (Sam):** BombControl in both panels' GunnerButtons with a CooldownFill, added to InputHandler's
  Touch Controls and UIManager's Bomb Cooldown Fill slots. Sam tested it working.
- README and IMPLEMENTATION_PLAN (D17) updated.

---

## Phases

### Phase 0 · Project setup & spec cross-check (~40 min) ✅
Fresh Universal 2D project with layers, collision matrix, landscape lock, Physics 2D in `Update`, the basic
shape sprites, folder structure, `.gitignore` and `git init`. Spec Coverage Check per §0.2.
*Commit:* `Initial Unity 6 project setup: layers, collision matrix, landscape, physics in Update`

### Phase 1 · Core loop skeleton (~1 h) ✅
`GameManager` (round state, the single timer, phase table), `PhaseData`, `Playfield` (camera-derived bounds
that position the boundary triggers), `BoundaryZone`, `UIManager` (timer, score, hull, phase text, start/end
panels). Quantum Flux at this stage only advances the phase text — the full sequence lands in Phase 4.
*Playable result:* Start → 60s countdown → phase text changes at 20s and 40s → win screen → Restart works.
*Commit:* `Add GameManager round timer, phase table, playfield bounds and basic HUD`

### Phase 2 · Pooling, ship & editor controls (~1.5 h) ✅
`PooledObject`, `ObjectPool`, `Projectile`, `TimedAbility`, `InputHandler` (keyboard/mouse path only),
`ShipPilot` (move, clamp, Boost), `ShipGunner` (reticle aim, fire cadence, Shield).
*Playable result:* you fly and shoot in the Editor, and the Hierarchy shows zero new objects during play.
*Commits:* `Add object pool and pooled projectiles` · `Add ship pilot movement and boost` · `Add gunner aiming, firing cadence and shield`

### Phase 3 · Threats, damage, win & lose (~1.5 h) ✅
`Hazard` (one script, configured per prefab for Enemy / Debris / Breach), `Spawner` with phase multipliers and
enemy fire, `ShipHealth` with the invulnerability window. Score and hull get wired into the HUD.
*Playable result:* a real round — kills score, unshielded hits cost hull, and both loss conditions fire.
*Commits:* `Add pooled hazards and spawner with phase multipliers` · `Add ship hull, invulnerability, scoring and loss conditions`

### Phase 4 · Quantum Flux & role panels (~1 h)
The full flux sequence from §6.3 — cancel input, swap roles, apply the new phase, refresh panels, banner —
plus the 3·2·1 countdown, both on-screen control layouts built in the Canvas with `TouchControl` components,
and the Boost/Shield cooldown fills.
*Playable result:* the headline feature. Roles visibly swap sides at 20s and 40s and difficulty ramps.
*Commit:* `Add Quantum Flux transition: input cancel, role swap, phase advance, banner`

### Phase 5 · Touch controls & device build (~1.25 h)
The touch path in `InputHandler`: per-finger ownership by finger ID, so a finger that began on Left stays Left
even when dragged across the centre, and a Shield touch can never aim or fire. Android build settings.
*Result:* two people playing simultaneously on one phone; the recording for submission.
*Commit:* `Add touch input with per-finger ownership and mobile build settings`

### Phase 6 · Performance pass & profiler evidence (~30 min)
Every script swept against the §8 rules (no per-frame allocations, no scene searches, no LINQ or coroutines),
pool prewarm sizes tuned from the logged peak counts, then Profiler capture during the Critical phase.
*Result:* screenshots showing gameplay scripts at 0 B GC Alloc and a stable frame time, saved to `Docs/Evidence/`.
*Commit:* `Performance pass: remove per-frame allocations, tune pool sizes, add profiler evidence`

### Phase 6.5 · Full game walkthrough for Sam (added by Sam)
Sam skims the per-step walkthroughs and wants to understand the whole game once it all works. Before the README,
walk through the finished project end to end: startup order, the per-frame flow, input ownership and role
switching, Quantum Flux, the pooling lifecycle, collisions and the matrix, and the likely interview questions and
live changes (IMPLEMENTATION_PLAN §12). Consider producing it as a document Sam can keep.

### Phase 7 · README, evidence & cleanup (~45 min)
The README per §11 — architecture, why input is control-based rather than player-based, pooling lifecycle,
collision rules, the §3 assumptions table, and an honest AI-usage section — plus debug cleanup and the
device video. Final check that the repo carries no credentials and not the spec PDF.
*Commit:* `Add README with architecture, decisions, AI usage and evidence`

### Phase 8 · Git & GitHub (mostly done early, 2026-09-25)
The repo is created and published (private). Remaining: make sure the final README and evidence are committed,
confirm `Docs/Private/` and the planning docs are absent from the repo, and decide on visibility for submission.
Rebuilding or backdating earlier history was explicitly declined.

---

## Open questions

None at the moment.
- *Resolved:* spec PDF. It's in `Docs/Private/`; the full coverage check was waived (see Deferred items).
- *Resolved:* art. Basic shapes only (circle, square, rectangle, triangle, diamond). No sourced sprites or UI kits.
