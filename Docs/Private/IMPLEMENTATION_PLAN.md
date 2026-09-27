# Operation Cross-Fire: Implementation Plan

> **For Claude Code.** This document is the single source of truth for how this project is built. Read it fully before writing any code. The original exercise spec from SquadLoom is at `Docs/Private/Exercise_Spec.pdf` (git-ignored, confidential). When this plan and the spec disagree, **stop and raise it**, do not silently pick one.

---

## 0. Working Agreement (read first)

### 0.1 Who does what

| Task | Owner |
|---|---|
| Writing and editing C# scripts, `.gitignore`, README | Claude Code |
| Creating scenes, prefabs, GameObjects, layers, collision matrix, Project Settings, inspector wiring, device builds | **Sam (the developer), in the Unity Editor**, following step-by-step instructions that Claude Code writes for each milestone |

Claude Code must **not** hand-edit `.unity`, `.prefab`, or `ProjectSettings/*.asset` YAML files. Instead, every milestone ends with an **Editor Setup Steps** checklist that Sam follows.

### 0.2 Cross-check protocol (mandatory)

**Before any code is written (once, at the start):**
1. Read `Docs/Private/Exercise_Spec.pdf` and this plan.
2. Produce a **Spec Coverage Check**: go through every requirement in the spec (layout/rules, roles/controls, touch rules, abilities, Quantum Flux, objects/scoring, HUD, collisions, pooling, performance, acceptance criteria, submission checklist) and state where in this plan it is covered.
3. List any requirement the plan misses or contradicts, and any decision in §3 that you believe conflicts with the spec.
4. Wait for Sam to confirm or adjust. Update §3 if anything changes.

**Before every milestone**, post this and wait for Sam's "go":

```
### Intended Output: Milestone N
What I will build:        <plain-language summary of behaviour Sam will see>
Spec lines satisfied:     <quote the relevant spec lines>
Decisions used (§3):      <IDs, e.g. D3, D7>
Mismatches / questions:   <anything unclear; "none" if none>
Files:                    <created / modified>
Editor steps for Sam:     <short preview>
```

**After every milestone**, post:
- **Walkthrough:** for each new/changed script, 3–6 sentences explaining what it does, who calls it, and what it calls. 
- **Editor Setup Steps:** numbered, exact (component names, inspector values, layers).
- **Test checklist:** what Sam should see when pressing Play.
- **Suggested commit message.**

### 0.3 Scope rules

- Build **only** what the spec requires and this plan describes. No extra features, systems, abstractions, managers, interfaces, events, or "future-proofing".
- If there are two ways to do something, pick the simpler one that fully meets the spec.
- If something seems to need a new script or pattern not listed in §5, **ask first**.
- Out of scope (spec): networking, accounts, multiple levels, upgrades, alternate ammo, shield polarity, adaptive difficulty, detailed art/audio/VFX, production menus, tutorials, save data.

---

## 1. Project Summary

A landscape 2D local co-op shooter on one mobile device. Two players share one ship:
- **Pilot:** move left/right, Boost.
- **Gunner:** aim reticle, fire lasers, Shield.

Threats (enemies, debris, breach hazards, enemy projectiles) descend from the top. The round lasts 60 seconds. At 20s and 40s a **Quantum Flux** event cancels input, swaps roles, raises difficulty, and shows a banner. Win: survive 60s with ≥1 hull. Lose: hull reaches 0 or a breach hazard reaches the bottom.

Time budget: **6–8 hours total.**

---

## 2. Guiding Principles

1. **Clean + modular + optimized + simple.** Readability beats cleverness. A junior developer should follow any script top to bottom.
2. **One responsibility per script**, but no script exists just to have more scripts. If two things naturally belong together, keep them together.
3. **GameManager is the hub.** It owns the round state, the single round timer, phases, roles, score, and win/lose. It holds references to every system and runs the per-frame update order. Systems do their own job and report back to the GameManager.
4. **Direct method calls, no events.** Systems talk through plain method calls (e.g. `GameManager.Instance.AddScore(10)`). No C# events, UnityEvents (except UI button `OnClick` in the inspector), message buses, or interfaces. Reason: "Find All References" shows every connection, so the flow is traceable.
5. **One singleton only:** `GameManager.Instance`. Every other system is reached through the GameManager or through a serialized reference.
6. **One clock:** GameManager's `Update()` ticks the systems in a fixed order. Systems expose `Tick(float deltaTime)` instead of their own `Update()`. Exception: pooled objects (hazards, projectiles) manage their own small per-object logic.
7. **Initialization order is explicit:** `Awake()` only caches a script's *own* components. All cross-system setup happens in `GameManager.Start()` in a fixed, visible order.
8. **Inspector: least options, most control.** Expose values a designer would realistically tune (all spec values: durations, cooldowns, multipliers, phase table) plus key references. Use `[Header]` and `[Tooltip]`. Do not serialize internal state or rarely-tuned constants.
9. **Regions for real groupings.** Fields go at the top grouped with `[Header]`. Methods are grouped into `#region`s by functionality (e.g. `#region Round Flow`, `#region Quantum Flux`). Scripts under ~50 lines need no regions.
10. **Practical performance.** Pools, cached references, no per-frame allocations, no scene searches. No micro-optimizations that hurt readability.
11. **Minimal comments** *(changed by Sam, 2026-09-24)*. Each class gets a one-line `/// <summary>` saying what
    it's for, as a plain sentence. Inside the code, comment only where the code alone would mislead (e.g. a guard
    or an ordering requirement), in one short line. No "Called by / Talks to" blocks, no step-by-step narration,
    no XML docs on every property. The README carries the architecture explanation. Keep `[Tooltip]`s short.

---

## 3. Spec Decisions & Assumptions (confirm before coding)

The written spec is the source of truth. Where it is silent or the mockups disagree, these defaults apply. Each must be listed under "Assumptions" in the README.

| ID | Topic | Decision |
|---|---|---|
| D1 | Mockups vs text | Mockups differ from the text (20s banner says "PROJECTILES x2"; 40s mockup shows P1 as Gunner; banner says "ROLES SWAPPED"). **Written text wins:** banner reads `QUANTUM FLUX — ROLES REVERSED`; Critical phase is P1 Pilot / P2 Gunner. |
| D2 | Phase modifiers | Each phase stores its **full** set of multipliers (no stacking logic). Critical keeps Alert's enemy speed ×1.25 and breach hazards, and adds spawn interval ×0.70 and projectile speed ×1.50. "Advances the difficulty" is read as cumulative. |
| D3 | What "enemy speed" covers | Applies to all descending threats (enemies, debris, breach hazards). Enemy projectiles use their own multiplier. |
| D4 | When speed changes apply | New multipliers apply to objects **spawned after** the transition; objects already on screen keep their speed. |
| D5 | Aiming | Touch: **relative drag**, like a laptop trackpad (reticle moves by the drag distance). Editor: reticle follows the mouse. Reticle is clamped to the playfield, above the ship. |
| D6 | Ship collisions | Any enemy/debris/enemy projectile that touches the ship is removed (returned to pool), whether or not damage is applied (shielded or invulnerable). **No score** for these; score only comes from laser kills. |
| D7 | Breach hazard vs ship | Not listed in the spec's damage rules, so breach hazards **pass through** the ship (excluded in the collision matrix). They must be shot down. |
| D8 | Ability cooldown timing | Cooldown starts **when the effect ends** (Boost reusable 1s + 4s after pressing). |
| D9 | Flux and abilities | Flux ends active Boost/Shield immediately; their cooldowns start from that moment. Cooldowns belong to the ship's abilities, not to a person, so they carry over across the swap. |
| D10 | Held input after flux | Touches held during a flux are ignored until lifted and pressed again. Editor keys/mouse buttons held during a flux are ignored until all are released. |
| D11 | Enemy firing | Enemies fire straight down at a fixed interval (with a random initial delay so they don't fire in sync). |
| D12 | Unspecified base values | Chosen defaults (tunable in inspector): see §7 per system. |
| D13 | Round start / restart | A minimal Start button begins the round (pools are prewarmed before it). Restart reloads the scene. Not a production menu. |
| D14 | End of round | On win/lose: spawning stops, gameplay input stops, `Time.timeScale = 0` freezes the scene, result panel shows. |
| D15 | Win timing | Win triggers the instant the timer reaches 60s, regardless of threats on screen. |
| D16 | Cooldown indicators | Radial/linear fill on the Boost and Shield buttons in the players' control panels (visible in editor too). Active Shield shown as a circle around the ship. |
| D17 | Bomb *(added by Sam after submission, 2026-09-27; not in the spec)* | A Gunner ability, on both players' Gunner layouts. Instant: kills every active enemy, debris and breach hazard (each gives its normal score) and returns every enemy projectile to its pool. Cooldown 10s, starting on use. A flux doesn't affect it (nothing to cancel). |

---

## 4. Technical Setup

### 4.1 Unity & project settings (Sam in Editor)
- **Unity 6 LTS**, template: **Universal 2D**.
- **Player Settings → Resolution and Presentation:** Default Orientation = Auto Rotation, allow **Landscape Left and Landscape Right only**.
- **Player Settings → Other Settings → Active Input Handling = `Input Manager (Old)`** *(changed from `Both` on
  2026-09-25: Unity 6 warns at Android build time that `Both` may not work on Android)*. All gameplay input is
  legacy `Input.*`, and the EventSystem uses the legacy Standalone module, so nothing needs the new backend.
- **Project Settings → Physics 2D → Simulation Mode = `Update`.** Physics steps once per frame, in sync with our single GameManager clock. This removes Update/FixedUpdate mismatch entirely.
- `Application.targetFrameRate = 60` is set in code (mobile defaults to 30).
- Android: IL2CPP + ARM64 for the final build (Mono is fine for quick iteration). Enable **Development Build** + **Autoconnect Profiler** for profiling builds only.
- EventSystem in scene uses **Standalone Input Module** (legacy), only for the Start/Restart buttons.

### 4.2 Layers
`Ship`, `PlayerLaser`, `Hazard`, `BreachHazard`, `EnemyProjectile`, `Boundary`

### 4.3 Physics 2D collision matrix
Enable **only** these pairs; everything else off (including self-pairs and `Default`):

| | Ship | PlayerLaser | Hazard | BreachHazard | EnemyProjectile | Boundary |
|---|---|---|---|---|---|---|
| **Ship** | | | ✔ | | ✔ | |
| **PlayerLaser** | | | ✔ | ✔ | | ✔ |
| **Hazard** | ✔ | ✔ | | | | ✔ |
| **BreachHazard** | | ✔ | | | | ✔ |
| **EnemyProjectile** | ✔ | | | | | ✔ |
| **Boundary** | | ✔ | ✔ | ✔ | ✔ | |

### 4.4 Physics body setup
- **Ship:** `Rigidbody2D` **Kinematic**, trigger collider, moved with `MovePosition`.
- **Hazards and projectiles:** `Rigidbody2D` **Dynamic**, Gravity Scale 0, Freeze Rotation Z, trigger collider, moved by setting `linearVelocity` once at launch. (Dynamic bodies guarantee trigger callbacks against every other body type.) Projectiles use Collision Detection = Continuous.
- **Boundaries:** `BoxCollider2D` trigger, no Rigidbody.
- Unity 6 API: use `rb.linearVelocity` (not the deprecated `rb.velocity`).

### 4.5 Folder structure
```
Assets/_Project/
  Scenes/        Main.unity
  Scripts/
    Core/        GameManager.cs, PhaseData.cs, Playfield.cs, BoundaryZone.cs
    Input/       InputHandler.cs, TouchControl.cs
    Ship/        ShipPilot.cs, ShipGunner.cs, ShipHealth.cs, TimedAbility.cs
    Threats/     Hazard.cs, Spawner.cs
    Projectiles/ Projectile.cs
    Pooling/     ObjectPool.cs, PooledObject.cs
    UI/          UIManager.cs
  Prefabs/       Laser, EnemyProjectile, Enemy, Debris, BreachHazard
  Sprites/       (Unity-made shapes, see §4.6)
Docs/Private/    (git-ignored: spec PDF)
```

### 4.6 Art
Use Unity's built-in shapes (**Create → 2D → Sprites → Square / Circle / Triangle / Diamond**) tinted to match the reference sheet: cyan triangle ship, red square enemy, grey circle debris, orange diamond breach hazard, thin cyan rectangle laser, thin red rectangle enemy projectile, blue circle shield. Self-made sprites avoid any redistribution issue.

### 4.7 Git
- Standard Unity `.gitignore` + `Docs/Private/`.
- Commit at least once per milestone with descriptive messages (the spec grades Git history).

---

## 5. Architecture Overview

### 5.1 System map

```
                           ┌──────────────────────────┐
                           │       GameManager        │
                           │ round state, ONE timer,  │
                           │ phases, roles, score,    │
                           │ win/lose, update order   │
                           └────────────┬─────────────┘
        ┌──────────────┬───────────────┼───────────────┬──────────────┬─────────────┐
        ▼              ▼               ▼               ▼              ▼             ▼
  InputHandler     ShipPilot       ShipGunner      ShipHealth      Spawner      UIManager
  touch + kb/mouse move + Boost    aim, fire,      hull, invuln,   spawns       HUD, panels,
  → control state  (TimedAbility)  Shield          damage          threats,     banners,
        │              ▲           (TimedAbility)      ▲           enemy shots  start/end
        └── read by ───┴───────────────┘               │               │
                                   │                   │               │
                                   ▼                   │               ▼
                               ObjectPool ◄── Projectile / Hazard ◄── ObjectPool
                                   (Laser)       (collisions)      (Enemy, Debris,
                                                                   Breach, EnemyProj)
  Playfield: screen bounds + positions BoundaryZones (used by Ship, Gunner, Spawner)
```

### 5.2 Scripts and responsibilities

| Script | Type | Responsibility |
|---|---|---|
| `GameManager` | MonoBehaviour (singleton) | Round state machine, single round timer, phase table & Quantum Flux, role assignment, score, win/lose, per-frame update order, access point to all systems. |
| `PhaseData` | `[Serializable]` class | One row of the phase table (name, start time, multipliers, breach on/off). |
| `Playfield` | MonoBehaviour | Computes playfield bounds from the camera; positions the boundary triggers to fit any screen aspect. |
| `BoundaryZone` | MonoBehaviour (tiny) | Marks a boundary trigger; `isBottomEdge` tells breach hazards they reached the bottom. |
| `InputHandler` | MonoBehaviour | Reads touches and editor keyboard/mouse; tracks touch ownership by finger ID; outputs simple control state (move direction, boost pressed, aim delta, fire held, shield pressed, bomb pressed). Knows **controls**, not players. |
| `TouchControl` | MonoBehaviour (tiny) | Placed on each on-screen control image; says which control it is (`Left`, `Right`, `Boost`, `Aim`, `Fire`, `Shield`, `Bomb`) and hit-tests a screen point. |
| `ShipPilot` | MonoBehaviour | Everything the Pilot does: horizontal movement, clamping, Boost. |
| `ShipGunner` | MonoBehaviour | Everything the Gunner does: reticle aiming, fire cadence, lasers, Shield + shield visual, Bomb. |
| `ShipHealth` | MonoBehaviour | Hull points, invulnerability window, receiving hits, notifying GameManager when destroyed. |
| `TimedAbility` | `[Serializable]` plain class | Reusable duration + cooldown timer used by Boost, Shield and Bomb (duration 0 = instant). |
| `Spawner` | MonoBehaviour | Spawn timer, picks threat type, applies phase multipliers, fires enemy projectiles on request, clears all threats for the Bomb. |
| `Hazard` | PooledObject | One script for Enemy / Debris / Breach (configured per prefab): health, score, speed, optional firing, collision with lasers and boundaries. |
| `Projectile` | PooledObject | One script for player lasers and enemy projectiles: moves in a direction, returns to pool at a boundary. |
| `PooledObject` | abstract MonoBehaviour | Base for pooled things: knows its pool, `IsActive` guard, `ReturnToPool()`. |
| `ObjectPool` | MonoBehaviour | Prewarms and reuses one prefab type; safe, logged expansion; keeps a list of every object it created (for the Bomb). |
| `UIManager` | MonoBehaviour | All UI: timer, hull, score, phase, role panels + labels, cooldown fills, flux countdown/banner, start/end panels. |

**Key idea to explain in interview:** *Input doesn't know about players or roles. It only knows which on-screen control a finger started on. Roles decide which controls are visible on each side of the screen. So swapping roles is just swapping which panel layout is visible on each side.*

### 5.3 Communication rules

| From → To | How |
|---|---|
| GameManager → systems | Serialized references; calls `Tick()`, `ApplyPhase()`, `CancelAllInput()`, etc. |
| Systems → GameManager | `GameManager.Instance.AddScore()`, `.OnHullDestroyed()`, `.OnBreachReachedBottom()`, `.IsPlaying` |
| Ship scripts → InputHandler | Read public properties via `GameManager.Instance.Input` |
| Hazard → Spawner | `GameManager.Instance.Spawner.FireEnemyProjectile(pos)` |
| ShipHealth → ShipGunner | Cached reference (same GameObject) to check `IsShieldActive` |
| UI buttons → GameManager | Inspector `OnClick` → `StartRound()` / `RestartRound()` |

---

## 6. Game Flow

### 6.1 Startup
1. **Awake (every script):** cache own components only. `GameManager.Awake` sets `Instance` and `Application.targetFrameRate = 60`.
2. **GameManager.Start** (fixed order):
   1. `playfield.Initialize()` (compute bounds, place boundaries)
   2. `PrewarmPools()` (all pools instantiate their objects now, never during play)
   3. `pilot.Initialize()`, `gunner.Initialize()`, `health.Initialize()`, `spawner.Initialize()`
   4. `ui.ShowStartScreen()`; state = `WaitingToStart`
3. **Start button → `StartRound()`:** `Time.timeScale = 1`, elapsed = 0, score = 0, phase index = 0, Player 1 = Pilot, apply phase 0, refresh UI, state = `Playing`.

### 6.2 Per-frame order (`GameManager.Update`)

```csharp
private void Update()
{
    if (state != RoundState.Playing) return;
    float dt = Time.deltaTime;

    input.Tick();              // 1. Read what the players are pressing this frame
    UpdateRoundTimer(dt);      // 2. Advance the ONE timer; may trigger Quantum Flux or Win
    if (state != RoundState.Playing) return;

    pilot.Tick(dt);            // 3. Pilot acts on input (move, boost)
    gunner.Tick(dt);           // 4. Gunner acts on input (aim, fire, shield)
    health.Tick(dt);           // 5. Invulnerability countdown
    spawner.Tick(dt);          // 6. Spawn threats
    ui.Tick();                 // 7. Refresh timer/cooldown displays (only when values change)
}
```
Because Quantum Flux runs in step 2 and cancels input, the ship sees zero input on the transition frame.

### 6.3 Round timer & Quantum Flux (one timer, one transition)

```csharp
private void UpdateRoundTimer(float dt)
{
    elapsed += dt;

    if (elapsed >= roundDuration) { EndRound(won: true, "Corridor secured"); return; }
    if (!HasNextPhase) return;

    float timeToFlux = phases[phaseIndex + 1].startTime - elapsed;
    if (timeToFlux <= 0f)                        TriggerQuantumFlux();
    else if (timeToFlux <= fluxCountdownSeconds) ui.ShowFluxCountdown(Mathf.CeilToInt(timeToFlux));
}

private void TriggerQuantumFlux()
{
    // 1. Cancel active movement, firing, Boost, Shield and touches
    input.CancelAllInput();
    pilot.CancelActions();
    gunner.CancelActions();
    // 2. Swap Pilot and Gunner
    pilotPlayer = pilotPlayer == 1 ? 2 : 1;
    // 3. Apply new phase values
    phaseIndex++;
    spawner.ApplyPhase(phases[phaseIndex]);
    // 4. Update control panels and role labels
    ui.RefreshRoles(pilotPlayer);
    ui.SetPhase(phases[phaseIndex].phaseName);
    // 5. Banner
    ui.ShowFluxBanner();
}
```
The code order matches the spec's numbered list exactly. Adding a 4th phase later = adding one row to the `phases` array; the flux happens automatically.

### 6.4 End of round
`EndRound(bool won, string reason)` runs once (guarded by state): state = `Ended`, `spawner.Stop()`, `input.CancelAllInput()`, `Time.timeScale = 0`, `ui.ShowEndScreen(won, reason)`. Restart button → `SceneManager.LoadScene(active scene)`; `StartRound` resets `timeScale` to 1.

Triggers: timer ≥ 60 (win), `ShipHealth` → `OnHullDestroyed()` (lose), breach `Hazard` → `OnBreachReachedBottom()` (lose).

---

## 7. System Specifications

Fields below are the **only** serialized fields unless Sam approves more. Defaults marked *(D12)* are tunable guesses.

### 7.1 GameManager
**Fields**
- `[Header("Systems")]` `InputHandler input`, `ShipPilot pilot`, `ShipGunner gunner`, `ShipHealth health`, `Spawner spawner`, `UIManager ui`, `Playfield playfield`, `ObjectPool[] poolsToPrewarm`
- `[Header("Round")]` `float roundDuration = 60`, `float fluxCountdownSeconds = 3`
- `[Header("Phases")]` `PhaseData[] phases` pre-filled via field initializer:

| phaseName | startTime | enemySpeedMultiplier | spawnIntervalMultiplier | enemyProjectileSpeedMultiplier | spawnBreachHazards |
|---|---|---|---|---|---|
| Patrol | 0 | 1.00 | 1.00 | 1.00 | false |
| Alert | 20 | 1.25 | 1.00 | 1.00 | true |
| Critical | 40 | 1.25 | 0.70 | 1.50 | true |

**Public API:** `static Instance`; properties `Input`, `Spawner`, `Playfield`, `IsPlaying`, `RemainingTime`, `PilotPlayer`; methods `StartRound()`, `RestartRound()`, `AddScore(int)`, `OnHullDestroyed()`, `OnBreachReachedBottom()`.

**Private state:** `enum RoundState { WaitingToStart, Playing, Ended }`, `state`, `elapsed`, `phaseIndex`, `pilotPlayer` (1 or 2), `score`.

**Regions:** `Unity Callbacks`, `Round Flow`, `Round Timer & Quantum Flux`, `Score`, `Win / Lose`.

### 7.2 PhaseData
`[System.Serializable]` class with the six fields above plus a constructor for the defaults. No logic.

### 7.3 Playfield
**Fields:** `Camera gameCamera`, `BoxCollider2D bottomEdge, topCleanup, leftCleanup, rightCleanup`, `float cleanupMargin = 2`.
**Initialize():** compute `MinX/MaxX/MinY/MaxY` from the orthographic camera (size 5) and screen aspect; place the bottom edge so its top sits at the screen bottom; place the top cleanup above the spawn line; left/right cleanups just outside the screen; size each to span the playfield.
**Public:** `MinX, MaxX, MinY, MaxY`, `SpawnY` (just above the top edge), `Vector2 ClampToPlayfield(Vector2 point)`.
The bottom edge doubles as cleanup for every descending object.

### 7.4 BoundaryZone
`[SerializeField] bool isBottomEdge;` public getter. Nothing else.

### 7.5 InputHandler
**Fields:** `TouchControl[] touchControls` (all 12: both layouts on both sides), `float aimDragSensitivity = 1`, `Camera gameCamera`.

**Outputs (read by ship scripts each frame):**
`float MoveDirection` (−1, 0, 1) · `bool BoostPressed` (this frame) · `bool FireHeld` · `bool ShieldPressed` (this frame) · `bool BombPressed` (this frame) · `Vector2 AimDragDelta` (world units) · `bool HasMouseAim` + `Vector2 MouseAimPoint` (editor only).

**Internal state:**
- `Dictionary<int, ControlType> touchOwners` (created once with capacity 10; add/remove after that does not allocate).
- `int[] holdCounts` indexed by `ControlType` (how many fingers are holding each control).
- `bool devInputLocked` for D10.
- `bool useDevControls = Application.isEditor || !Application.isMobilePlatform`.
- In `Awake`: `Input.simulateMouseWithTouches = false` (so touches don't also fire as mouse clicks on device); cache `unitsPerPixel = 2 * orthoSize / Screen.height`.

**Tick():**
1. Reset per-frame values (`BoostPressed`, `ShieldPressed`, `AimDragDelta`).
2. **Touches:** loop `for i < Input.touchCount` using `Input.GetTouch(i)` (never `Input.touches`, which allocates).
   - `Began`: hit-test active `TouchControl`s (skip ones not `activeInHierarchy`). If hit: store `touchOwners[fingerId] = control`, `holdCounts[control]++`; if Boost → `BoostPressed = true`; if Shield → `ShieldPressed = true`; if Bomb → `BombPressed = true`. If nothing hit: ignore the touch forever.
   - `Moved/Stationary`: if owner is `Aim`, add `deltaPosition * unitsPerPixel * aimDragSensitivity` to `AimDragDelta`. Other owners: nothing (ownership never changes, so crossing the centre line does nothing).
   - `Ended/Canceled`: if owned, `holdCounts[control]--` and remove from dictionary.
3. **Dev controls** (if `useDevControls`): A/D/arrows, Space (`GetKeyDown`), mouse position → world, left button held, right button down, `B` (`GetKeyDown`) for Bomb. If `devInputLocked`, ignore all but aim until no relevant key/button is held, then unlock.
4. Combine: `MoveDirection = (rightHeld ? 1 : 0) - (leftHeld ? 1 : 0)` (holding both = 0, per spec); `FireHeld = holdCounts[Fire] > 0 || mouseFire`.

**CancelAllInput():** clear `touchOwners`, zero `holdCounts`, reset outputs, `devInputLocked = true`. Fingers still down are no longer in the dictionary, so they are ignored until lifted and pressed again (their `Ended` is harmlessly ignored).

Why Shield can't aim or fire: a touch is owned by exactly one control, and only `Aim` touches produce aim, only `Fire` touches produce fire.

**Regions:** `Public Output`, `Touch Input`, `Editor Keyboard & Mouse`, `Cancel`.

### 7.6 TouchControl
`public enum ControlType { Left, Right, Boost, Aim, Fire, Shield, Bomb }` (same file).
Fields: `ControlType controlType`. Caches its `RectTransform`. `bool Contains(Vector2 screenPoint)` → `RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null)` (Screen Space Overlay canvas). Control images have **Raycast Target off** (we hit-test ourselves; no EventSystem involvement).

### 7.7 TimedAbility
`[System.Serializable]` plain C# class, used as a field in ShipPilot (Boost) and ShipGunner (Shield, Bomb).
A duration of 0 makes it instant: `TryActivate` starts the cooldown straight away instead of the active timer (used by the Bomb).
```csharp
[SerializeField] private float duration;
[SerializeField] private float cooldown;
private float activeTimer, cooldownTimer;

public bool IsActive => activeTimer > 0f;
public bool IsReady  => !IsActive && cooldownTimer <= 0f;
public float ReadyProgress01 => ...; // 1 = ready; used for the UI fill

public bool TryActivate() { if (!IsReady) return false; activeTimer = duration; return true; }
public void Tick(float dt) { /* count down active; when it ends, start cooldown (D8); else count down cooldown */ }
public void Cancel()       { if (IsActive) { activeTimer = 0f; cooldownTimer = cooldown; } } // D9
```

### 7.8 ShipPilot
**Fields:** `float moveSpeed = 8` *(D12)*, `float boostSpeedMultiplier = 1.75`, `TimedAbility boost` (duration 1, cooldown 4).
**Cached:** `Rigidbody2D`, half-width from collider bounds, `targetX`, fixed ship Y (from Playfield, near bottom centre).
**Initialize():** place ship at bottom centre; `targetX = 0`.
**Tick(dt):**
1. `boost.Tick(dt)`; if `input.BoostPressed` → `boost.TryActivate()`.
2. `speed = moveSpeed * (boost.IsActive ? boostSpeedMultiplier : 1)`.
3. `targetX = Mathf.Clamp(targetX + input.MoveDirection * speed * dt, MinX + halfWidth, MaxX - halfWidth)`.
4. `rb.MovePosition(new Vector2(targetX, shipY))`.
Track `targetX` ourselves instead of reading `rb.position`, so no movement is lost between physics steps.
**CancelActions():** `boost.Cancel()`. **Public:** `Boost` (for UI).
Boost gives no invulnerability (spec): ShipPilot never touches ShipHealth.

### 7.9 ShipGunner
**Fields:** `Transform reticle`, `Transform muzzle`, `ObjectPool laserPool`, `float laserSpeed = 14` *(D12)*, `float fireCooldown = 0.25`, `TimedAbility shield` (duration 1.5, cooldown 5), `GameObject shieldVisual`, `TimedAbility bomb` (duration 0, cooldown 10).
**Initialize():** reticle a few units above the ship; shield visual off.
**Tick(dt):**
1. Shield: `shield.Tick(dt)`; if `input.ShieldPressed` → `TryActivate()`. Toggle `shieldVisual` only when `IsActive` changes.
1b. Bomb: `bomb.Tick(dt)`; if `input.BombPressed && bomb.TryActivate()` → `spawner.ClearAllThreats()` (Spawner cached from GameManager in `Initialize`).
2. Aim: if `input.HasMouseAim` → reticle = `MouseAimPoint`; add `input.AimDragDelta`; clamp to playfield and keep above the ship.
3. Fire: `fireTimer -= dt`; if `input.FireHeld && fireTimer <= 0` → `FireLaser()`, `fireTimer = fireCooldown`. The timer is independent of clicks, so rapid clicking cannot bypass it.
**FireLaser():** direction = (reticle − muzzle).normalized (fallback `Vector2.up`); `laserPool.Get<Projectile>().Launch(muzzle.position, direction, laserSpeed)`.
**CancelActions():** `shield.Cancel()`, hide shield visual.
**Public:** `IsShieldActive`, `Shield`, `Bomb` (for UI).
**Regions:** `Aiming`, `Firing`, `Shield`, `Bomb`.

### 7.10 ShipHealth
**Fields:** `int maxHull = 3`, `float invulnerabilityDuration = 1` *(D12)*.
**Cached:** `ShipGunner gunner` (same GameObject), `SpriteRenderer`, original colour.
**State:** `hull`, `invulnerableTimer`.
**Tick(dt):** count down invulnerability; ship sprite at half alpha while invulnerable (restore when done).
**OnTriggerEnter2D(Collider2D other):** ignore if `!GameManager.Instance.IsPlaying`.
- `Hazard` (via `TryGetComponent`) and `hazard.IsActive` → `hazard.ReturnToPool()` (D6), then `TryTakeDamage()`.
- `Projectile` and `IsActive` → `ReturnToPool()`, then `TryTakeDamage()`.
**TryTakeDamage():** return if `gunner.IsShieldActive` or `invulnerableTimer > 0`. Else `hull--`, `ui.SetHull(hull)`, start invulnerability, if `hull <= 0` → `GameManager.Instance.OnHullDestroyed()`.
The collision matrix guarantees only non-breach hazards and enemy projectiles reach this callback.

### 7.11 Spawner
**Fields:** `ObjectPool enemyPool, debrisPool, breachPool, enemyProjectilePool`, `float baseSpawnInterval = 0.9` *(D12)*, `[Range(0,1)] float debrisChance = 0.3`, `[Range(0,1)] float breachChance = 0.15`, `float enemyProjectileBaseSpeed = 5` *(D12)*, `float spawnEdgePadding = 0.5`.
**State:** `PhaseData currentPhase`, `spawnTimer`, `isSpawning`.
**Methods:** `Initialize()`, `ApplyPhase(PhaseData)`, `Begin()`, `Stop()`, `Tick(dt)`, `FireEnemyProjectile(Vector2 position)`, `ClearAllThreats()`.
**Tick:** if spawning, count down; on zero spawn one hazard and reset to `baseSpawnInterval * currentPhase.spawnIntervalMultiplier`.
**Choosing a type:** one `Random.value` roll: breach (only if `spawnBreachHazards`) → debris → otherwise enemy.
**Spawning:** random X within playfield (padded), Y = `SpawnY`; `pool.Get<Hazard>().Launch(position, currentPhase.enemySpeedMultiplier)`.
**FireEnemyProjectile:** `enemyProjectilePool.Get<Projectile>().Launch(position, Vector2.down, enemyProjectileBaseSpeed * currentPhase.enemyProjectileSpeedMultiplier)`.
**ClearAllThreats (Bomb):** calls `Kill()` on every object in the enemy, debris and breach pools, then `enemyProjectilePool.ReturnAllActive()`.
**Regions:** `Phase`, `Spawning`, `Bomb`, `Enemy Projectiles`.

### 7.12 Hazard (extends PooledObject)
**Fields (set per prefab):** `int maxHealth`, `int scoreValue`, `float baseSpeed`, `bool isBreachHazard`, `bool canShoot`, `float fireInterval`.

| Prefab | Sprite | Layer | Health | Score | Base speed *(D12)* | Shoots |
|---|---|---|---|---|---|---|
| Enemy | red square | Hazard | 1 | 10 | 2.5 | yes, every 1.8s |
| Debris | grey circle | Hazard | 2 | 15 | 2.0 | no |
| BreachHazard | orange diamond | BreachHazard | 3 | 25 | 1.5 | no |

**Cached:** `Rigidbody2D`, `SpriteRenderer`, original colour.
**Launch(Vector2 position, float speedMultiplier)** (full reset, per spec): position, rotation = identity, `health = maxHealth`, `fireTimer = Random.Range(0.5f, fireInterval)`, colour restored, `MarkActive()`, `SetActive(true)`, `linearVelocity = Vector2.down * baseSpeed * speedMultiplier`, `angularVelocity = 0`.
**Update:** if `canShoot` and playing: count down; on zero → `GameManager.Instance.Spawner.FireEnemyProjectile(transform.position)`, reset timer.
**OnTriggerEnter2D:**
- `Projectile` (a laser, guaranteed by matrix) and `laser.IsActive` → `laser.ReturnToPool()`, `TakeDamage(1)`. The `IsActive` check stops one laser damaging two hazards in the same frame.
- `BoundaryZone`: if `isBreachHazard && zone.IsBottomEdge && IsActive` → `GameManager.Instance.OnBreachReachedBottom()`. In all cases `ReturnToPool()`.
**TakeDamage(int):** ignore if `!IsActive`; `health--`; darken colour to show damage; if `health <= 0` → `Kill()`.
**Kill():** ignore if `!IsActive`; `GameManager.Instance.AddScore(scoreValue)`, `ReturnToPool()`. Also called by the Bomb.

### 7.13 Projectile (extends PooledObject)
Used by both Laser (layer PlayerLaser, cyan) and EnemyProjectile (layer EnemyProjectile, red) prefabs.
**Cached:** `Rigidbody2D`.
**Launch(Vector2 position, Vector2 direction, float speed):** position, rotation so `transform.up == direction`, `MarkActive()`, `SetActive(true)`, `linearVelocity = direction * speed`.
**OnTriggerEnter2D:** if `BoundaryZone` → `ReturnToPool()`. (Hits on hazards/ship are handled by the receiver: rule is *"the thing that gets hurt handles the collision"*.)

### 7.14 PooledObject (abstract)
```csharp
public ObjectPool OwnerPool { get; set; }   // set once by the pool
public bool IsActive { get; private set; }  // true while in play

protected void MarkActive() => IsActive = true;

public void ReturnToPool()
{
    if (!IsActive) return;        // guards against double-return in the same frame
    IsActive = false;
    OwnerPool.Return(this);
}
```

### 7.15 ObjectPool
**Fields:** `PooledObject prefab`, `int prewarmCount`.
**State:** `Stack<PooledObject> available` (created with capacity), `List<PooledObject> allObjects` (every object created, added in `CreateObject`), `activeCount`, `peakActiveCount` (for pool-sizing evidence).
**Prewarm():** instantiate `prewarmCount` objects as children, set `OwnerPool`, `SetActive(false)`, push.
**Get<T>() where T : PooledObject:** pop (if empty: instantiate one and `Debug.LogWarning` "pool expanded", documented safe expansion); update counts; return `(T)obj`. The object is still inactive; the caller's `Launch()` resets and activates it.
**Return(obj):** `SetActive(false)`, push, `activeCount--`.
**AllObjects / ReturnAllActive():** read-only view of `allObjects`, and a loop that calls `ReturnToPool()` on each (the `IsActive` guard skips inactive ones). Used by the Bomb; no allocation.
At round end, log each pool's `peakActiveCount` (editor/dev builds) to justify prewarm sizes.

**Starting prewarm sizes (sized for Critical, verify with peak logs):**

| Pool | Prewarm | Reasoning |
|---|---|---|
| Laser | 20 | 4 shots/s × ~1.5s flight, plus margin |
| EnemyProjectile | 30 | several enemies firing at ×1.5 speed |
| Enemy | 15 | spawn every ~0.63s × ~3–4s on screen |
| Debris | 10 | ~30% of spawns |
| BreachHazard | 6 | ~15% of spawns, slow |

### 7.16 UIManager
**Fields:**
- `[Header("HUD")]` `TMP_Text timerText, scoreText, phaseText`, `Image[] hullIcons`
- `[Header("Player Panels")]` `PlayerPanelUI player1Panel, player2Panel` where `[Serializable] class PlayerPanelUI { GameObject pilotControls; GameObject gunnerControls; TMP_Text roleLabel; Image boostCooldownFill; Image shieldCooldownFill; Image bombCooldownFill; }`
- `[Header("Banner")]` `GameObject bannerRoot`, `TMP_Text bannerText`, `float bannerDuration = 1.5`
- `[Header("Screens")]` `GameObject startPanel, endPanel`, `TMP_Text endResultText`

**Methods:** `ShowStartScreen()`, `HideStartScreen()`, `RefreshRoles(int pilotPlayer)` (activate pilot/gunner layouts per side, set labels like `PLAYER 1 — PILOT`), `SetScore(int)`, `SetHull(int)`, `SetPhase(string)`, `ShowFluxCountdown(int secondsLeft)`, `ShowFluxBanner()`, `ShowEndScreen(bool won, string reason)`, `Tick()`.
**Tick():** update timer text only when the whole-second value changes; set cooldown `fillAmount` from `ReadyProgress01` (no allocation); hide the banner when its timer ends.
**No per-frame allocations:** use TMP `SetText("...{0}...", number)` overloads (non-allocating) and only when the value changes. Role labels change only at flux (negligible).
**Layout:** Screen Space Overlay canvas, Canvas Scaler = Scale With Screen Size (1920×1080, match 0.5). Player 1 panel bottom-left, Player 2 panel bottom-right, HUD top-centre, banner centre. Panels slightly transparent so threats behind them stay visible.
**Regions:** `HUD`, `Role Panels`, `Quantum Flux Banner`, `Start / End Screens`.

---

## 8. Performance Rules (checklist for every script)

- No `Instantiate`/`Destroy` after pool prewarm (only logged emergency expansion).
- No `Find*`, `FindObjectOfType`, `GameObject.Find`, or `Camera.main` in `Update`/`Tick`/physics callbacks. Cache in `Awake`/`Initialize`.
- `TryGetComponent` in trigger callbacks is acceptable (no GC in player builds).
- No LINQ, no string concatenation/formatting per frame, no `Input.touches`, no coroutines (`WaitForSeconds` allocates; use timers in `Tick`), no lambdas/closures in hot paths.
- UI updates only when a value changes.
- Control images: Raycast Target off.
- Profile on device during Critical phase (§10).

---

## 9. Milestones

Each milestone: Intended Output → Sam's go → code → Walkthrough + Editor Steps + Test checklist → Sam tests → commit.

### M0 · Project setup (~30 min)
- **Claude Code:** folder structure, `.gitignore` (Unity + `Docs/Private/`), empty README skeleton.
- **Sam:** create project (§4.1), layers (§4.2), matrix (§4.3), sprites (§4.6), `Main` scene with orthographic camera (size 5, dark background), put spec PDF in `Docs/Private/`, `git init`, push to GitHub.
- **Commit:** `Initial Unity 6 project setup: layers, collision matrix, landscape, physics in Update`

### M1 · Core loop (~1 h)
- **Scripts:** `GameManager`, `PhaseData`, `Playfield`, `BoundaryZone`, `UIManager` (HUD, start/end screens, phase text; banners stubbed).
- `TriggerQuantumFlux()` at this stage only advances phase + updates phase text (full sequence in M4).
- **Test:** Start button → timer counts down from 60 → phase text changes at 20s and 40s → win screen at 60s → Restart works. Boundaries fit the Game view at 16:9 and 20:9.
- **Commit:** `Add GameManager round timer, phase table, playfield bounds and basic HUD`

### M2 · Pooling + Ship + editor controls (~1.5 h)
- **Scripts:** `PooledObject`, `ObjectPool`, `Projectile`, `TimedAbility`, `InputHandler` (editor keyboard/mouse only), `ShipPilot`, `ShipGunner`.
- **Test:** A/D/arrows move the ship, clamped to screen; Space boosts (visibly faster for 1s, then cooldown); mouse moves reticle; holding left click fires at 4 shots/s even while spam-clicking; right click shows shield circle for 1.5s then 5s cooldown; lasers return to pool at boundaries (Hierarchy shows no new objects).
- **Commits:** `Add object pool and pooled projectiles`, `Add ship pilot movement and boost`, `Add gunner aiming, firing cadence and shield`

### M3 · Threats, damage, win/lose (~1.5 h)
- **Scripts:** `Hazard`, `Spawner`, `ShipHealth`; hook score and hull into UI.
- **Test:** enemies and debris spawn in Patrol; breach hazards only from 20s; enemies fire downward; lasers destroy enemies (1 hit), debris (2), breach (3) with correct scores; unshielded hits remove 1 hull with invulnerability blink; shield blocks damage; hull 0 → loss; breach reaching bottom → loss; breach passes through ship; after end, nothing spawns and input does nothing; no pool expansion warnings.
- **Commits:** `Add pooled hazards and spawner with phase multipliers`, `Add ship hull, invulnerability, scoring and loss conditions`

### M4 · Quantum Flux + role panels (~1 h)
- **Work:** full `TriggerQuantumFlux()` sequence (§6.3), countdown `QUANTUM FLUX IN 3… 2… 1…`, banner `QUANTUM FLUX — ROLES REVERSED`, `RefreshRoles`, cooldown fills, both players' panels built in the Canvas (pilot layout: Left/Right/Boost; gunner layout: Aim area/Fire/Shield) with `TouchControl` components.
- **Test:** countdown at 17s and 37s; at 20s/40s held keys stop until released, active boost/shield end, panels swap sides with correct labels, difficulty visibly increases; Critical spawns faster with faster enemy shots.
- **Commit:** `Add Quantum Flux transition: input cancel, role swap, phase advance, banner`

### M5 · Touch controls + device build (~1.25 h)
- **Work:** touch path in `InputHandler` (§7.5).
- **Test on device:** two people play simultaneously; holding Left+Right stops; dragging in aim area moves reticle; dragging a finger from Left across the centre keeps it as Left; shield touch doesn't aim/fire; fingers held through a flux do nothing until re-pressed; win and loss both reachable. Enable Android Developer Options → **Show taps** for the recording.
- **Commit:** `Add touch input with per-finger ownership and mobile build settings`

### M6 · Performance pass + profiler (~30 min)
- **Work:** review every script against §8; fix anything found; adjust pool sizes from peak logs.
- **Profiler (Sam):** Development Build + Autoconnect Profiler → Build And Run on device → reach Critical (>40s) → record ~10s → CPU Usage, Hierarchy view, sort by **GC Alloc** → screenshot showing gameplay scripts at 0 B, plus a timeline screenshot showing stable frame time. Save to `Docs/Evidence/`.
- **Commit:** `Performance pass: remove per-frame allocations, tune pool sizes, add profiler evidence`

### M7 · README + evidence + cleanup (~45 min)
- **Claude Code:** README (§11), remove debug leftovers, final comment pass.
- **Sam:** device video + screenshots into `Docs/Evidence/` (or linked), final commit, verify repo contains no credentials, personal data, or the spec PDF.
- **Commit:** `Add README with architecture, decisions, AI usage and evidence`

If time runs out: finish the current milestone to a working state, then document the rest under "Incomplete work" rather than going over the time box.

---

## 10. Device & Profiling Notes
- Test device: one Android phone (record model, Android version, and Unity version in README).
- Physical-device video must show: simultaneous controls, Boost, Shield, both Quantum Flux transitions, increased difficulty, and a win or loss.
- Profiler evidence must be from the Critical phase after pool warm-up.
- Zero allocation from Unity internals isn't required; gameplay scripts must show none recurring.

---

## 11. README Structure (final deliverable)
1. **Overview:** what the game is, one screenshot.
2. **Setup:** Unity version, scene path, how to play in editor (controls table), how to build to device, device used.
3. **Architecture:** system map (§5.1), script table (§5.2), communication rules, per-frame order.
4. **Input & role switching:** control-based input, finger-ID ownership, why crossing the centre doesn't transfer, why roles are just panel layouts.
5. **Quantum Flux & progression:** one timer, one transition method, phase table in inspector.
6. **Pooling:** lifecycle (Prewarm → Get → Launch/reset → ReturnToPool guard → Return), sizes and peak evidence, expansion policy.
7. **Collisions:** layers, matrix, "receiver handles the hit" rule.
8. **Performance:** rules followed, profiler screenshots, target frame rate.
9. **AI usage:** honest account: what was AI-generated, how it was reviewed, tested, and understood line by line.
10. **Assumptions:** the §3 decision table.
11. **Incomplete work / next steps.**

---

## 12. Quick Reference: "Where does it live?"

| Question | Answer |
|---|---|
| Where is the round timer? | `GameManager.UpdateRoundTimer` (the only timer) |
| What starts a Quantum Flux? | `UpdateRoundTimer` when elapsed passes the next `PhaseData.startTime` → `TriggerQuantumFlux` |
| Where are difficulty values? | `GameManager` inspector → `phases` array |
| How are roles swapped? | `pilotPlayer` flip in `TriggerQuantumFlux` → `UIManager.RefreshRoles` shows the other layout on each side |
| Who decides which finger controls what? | `InputHandler` touch `Began` hit-test against visible `TouchControl`s |
| Where is movement / Boost? | `ShipPilot.Tick` |
| Where is aim / fire / Shield / Bomb? | `ShipGunner.Tick` (Bomb → `Spawner.ClearAllThreats`) |
| Where is damage decided? | `ShipHealth.OnTriggerEnter2D` → `TryTakeDamage` |
| Where is score added? | `Hazard.TakeDamage` → `GameManager.AddScore` |
| What causes a loss? | `ShipHealth` → `OnHullDestroyed`; breach `Hazard` → `OnBreachReachedBottom` |
| Where are threats created? | `Spawner.Tick` → `ObjectPool.Get` → `Hazard.Launch` |
| Where is an object reset for reuse? | Its `Launch()` method |

### Likely live-change requests and where to make them
| Change | Where |
|---|---|
| Different Boost/Shield duration or cooldown | `ShipPilot` / `ShipGunner` inspector (`TimedAbility`) |
| Add a 4th phase at 50s | Add a row to `GameManager.phases` (flux happens automatically) |
| Hull 5 instead of 3 | `ShipHealth.maxHull` + two more hull icons |
| Shield-blocked hits give score | `ShipHealth.OnTriggerEnter2D` (call `AddScore` before returning the hazard) |
| Enemies aim at the ship | `Spawner.FireEnemyProjectile` direction |
| Breach hazards damage the ship | Collision matrix (Ship ↔ BreachHazard) + handle in `ShipHealth` |
| New hazard type | New prefab with `Hazard`, new `ObjectPool`, add to `Spawner` roll + `poolsToPrewarm` |
| Absolute touch aiming | `ShipGunner` aim step + `InputHandler` Aim output |
| Longer invulnerability | `ShipHealth.invulnerabilityDuration` |
| Bomb cooldown | `ShipGunner` inspector → Bomb (`TimedAbility`) |
