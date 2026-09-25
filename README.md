# Operation Cross-Fire

A landscape 2D local co-op shooter for one phone. Two players share one ship: one is the **Pilot** (moves left/right, Boost) and the other is the **Gunner** (aims, fires, Shield). A round lasts 60 seconds. At 20s and 40s a **Quantum Flux** hits: whatever you're holding gets cancelled, the two players swap roles, and the game gets harder.

- **Win:** survive 60 seconds with at least 1 hull point.
- **Lose:** hull hits 0, or a breach hazard (the orange diamond) reaches the bottom of the screen.

![Gameplay on the phone](Docs/Evidence/Gameplay%20ScreenShots/Mid%20Game.jpeg)

Everything is built from Unity's basic shapes. No sourced art or UI kits.

> Git was started partway through the project, so the first commit contains everything built up to that point. Commits after that follow the work step by step.

---

## 1. Setup

- **Unity:** `6000.6.2f1` (Unity 6), Universal 2D template
- **Scene:** `Assets/_Project/Scenes/Main.unity` (the only scene)
- **Active Input Handling:** `Input Manager (Old)`. All gameplay input uses the legacy `Input` API, and the EventSystem uses the Standalone Input Module. I started on `Both`, but Unity warns that `Both` might not work on Android, and nothing here needs the new Input System.
- **Physics 2D Simulation Mode:** `Update`, so physics steps once per frame in sync with the game's own update order.

### Playing in the Editor

Press Play, then click **Start**. In the Editor, one person can drive both roles:

| Action | Key / mouse |
|---|---|
| Move left / right | `A` / `D` or arrow keys |
| Boost | `Space` |
| Aim | Move the mouse (reticle follows the cursor) |
| Fire | Hold left mouse button |
| Shield | Right mouse button |

Keyboard and mouse only work in the Editor and on desktop. On the phone, it's touch only.

### Playing on the phone

Hold the phone in landscape. Each player has a control panel in their bottom corner (Player 1 on the left, Player 2 on the right). The panel shows the controls for that player's current role:

- **Pilot panel (cyan):** ◀ ▶ to move, ▲ Boost
- **Gunner panel (red):** a large AIM pad (drag to move the reticle, like a laptop trackpad), FIRE (hold), SHIELD

When the flux hits, the panels swap layouts, so each player uses the other role's controls in their own corner.

### Building for Android

1. Android Build Support has to be installed in Unity Hub.
2. Build Profiles → Android. Player settings already in the project: IL2CPP, ARM64 only, minimum API 26, Landscape Left/Right only.
3. Build the APK and install it on the phone. You can also use Unity Remote 5 to test multi-touch from the Editor.

**Device tested on:** Samsung Galaxy A32, Android 13

**Gameplay video:** [Mobile Gameplay Screen Recording.mp4](Docs/Evidence/Gameplay%20Video/Mobile%20Gameplay%20Screen%20Recording.mp4) (recorded on the Galaxy A32)

**Screenshots:** [`Docs/Evidence/Gameplay ScreenShots/`](Docs/Evidence/Gameplay%20ScreenShots)

---

## 2. Project layout

```
Assets/_Project/
  Scenes/       Main.unity
  Scripts/
    Core/        GameManager, PhaseData, Playfield, BoundaryZone
    Input/       InputHandler, TouchControl
    Ship/        ShipPilot, ShipGunner, ShipHealth, TimedAbility
    Threats/     Hazard, Spawner
    Projectiles/ Projectile
    Pooling/     ObjectPool, PooledObject
    UI/          UIManager
  Prefabs/      Laser, EnemyProjectile, Enemy, Debris, BreachHazard
  Sprites/      Circle, Square, Triangle
```

---

## 3. Architecture

I kept this deliberately simple. Some rules I stuck to throughout:

- **`GameManager` is the hub, and the only singleton.** It owns the round state, the single round timer, the phase table, roles, score and win/lose. Everything else is reached through a serialized reference or through `GameManager.Instance`.
- **One update loop.** Systems don't have their own `Update()`. They expose `Tick(dt)`, and `GameManager.Update` calls them in a fixed order. The only exception is pooled objects (hazards shooting on their own timer), since each one only looks after itself.
- **`Awake` caches a script's own components. Nothing else.** Anything that depends on another system happens in `GameManager.Start`, in an order you can read top to bottom.
- **Direct method calls only.** No C# events, UnityEvents (apart from the two button OnClicks), interfaces or message buses. "Find All References" on any method shows every place it's used, and that matters more to me than decoupling in a project this size.

### How the pieces connect

```
                         GameManager
       (round state, timer, phases, roles, score, win/lose)
                              |
   +-----------+-----------+--+--------+-----------+-----------+
   |           |           |           |           |           |
InputHandler ShipPilot  ShipGunner  ShipHealth   Spawner    UIManager
controls ->  move,      aim, fire,  hull,        spawns     HUD, panels,
values       Boost      Shield      invuln.      threats    banner, screens
                           |                        |
                       Laser pool          Enemy / Debris / Breach /
                                           EnemyProjectile pools

Playfield: works out the screen bounds from the camera and places the boundary triggers.
```

### Scripts

| Script | What it does |
|---|---|
| `GameManager` | Round state, the round timer, phases and Quantum Flux, roles, score, win/lose, and the per-frame update order. |
| `PhaseData` | One row of the phase table: name, start time, multipliers, and whether breach hazards spawn. |
| `Playfield` | Works out the playfield bounds from the camera and positions the four boundary triggers, so it fits any aspect ratio. |
| `BoundaryZone` | Marks a boundary trigger. `IsBottomEdge` is how a breach hazard knows it got through. |
| `InputHandler` | Reads touches (and keyboard/mouse in the Editor) and turns them into control values: move direction, boost pressed, aim delta, fire held, shield pressed. It knows about controls, not players. |
| `TouchControl` | Sits on each on-screen button. Says which control it is and hit-tests a screen point. |
| `ShipPilot` | Horizontal movement, clamping to the screen, Boost. |
| `ShipGunner` | Reticle aiming, fire rate, lasers, Shield and the shield visual. |
| `ShipHealth` | Hull points, taking hits, the invulnerability window after a hit. |
| `TimedAbility` | A plain class for "lasts X seconds, then cools down for Y". Used by both Boost and Shield. |
| `Spawner` | Spawn timer, picks the threat type, applies the phase multipliers, fires enemy shots. |
| `Hazard` | One script for enemies, debris and breach hazards. The differences are just inspector values on each prefab. |
| `Projectile` | One script for player lasers and enemy shots. Flies straight and goes back to the pool at a boundary. |
| `PooledObject` | Base class for anything pooled. Knows its pool and guards against being returned twice. |
| `ObjectPool` | Creates one prefab type up front and hands instances out and back. |
| `UIManager` | All UI: timer, score, phase, hull icons, player panels, ability fills, flux countdown/banner, start and end screens. |

### Startup order (`GameManager.Start`)

1. `playfield.Initialize()`: bounds and boundaries first, since the ship and spawner need them
2. `PrewarmPools()`: every pooled object is created here, never during play
3. `pilot`, `gunner`, `health`, `spawner` `.Initialize()` (the gunner goes after the pilot because it places the reticle relative to the ship)
4. Player 1 starts as Pilot, UI gets its initial state, and the start screen shows

### Per-frame order (`GameManager.Update`)

```csharp
input.Tick();            // read what the players are pressing
UpdateRoundTimer(dt);    // the one timer: may trigger a flux or the win
if (state != RoundState.Playing) return;

pilot.Tick(dt);
gunner.Tick(dt);
health.Tick(dt);
spawner.Tick(dt);
ui.SetAbilityFills(pilot.Boost, gunner.Shield);
ui.Tick();
```

The flux runs inside `UpdateRoundTimer`, which runs *before* the ship scripts. So on the frame of a swap, the ship sees zero input.

---

## 4. Input and role switching

This was the bit I thought about most. **The input code has no idea who Player 1 or Player 2 is, or who's Pilot.** It only knows which on-screen control a finger started on.

- When a touch **begins**, `InputHandler` hit-tests it against the `TouchControl`s that are currently visible. If it lands on one, that finger ID **owns** that control until the finger lifts. A touch that starts on nothing is ignored for its whole life.
- While the finger is held, it keeps its control, **wherever it moves**. A finger that started on ◀ still means Left even if it slides across the middle of the screen. A finger that started on SHIELD can never fire or aim. This is what lets two people on one phone not interfere with each other.
- Only a finger that owns the **AIM** pad produces an aim delta. Aiming is relative: the reticle moves by the drag distance, scaled by `aimDragSensitivity` (2 by default, because at 1:1 a swipe across the pad only moves the reticle about 3 world units).
- Held controls are counted, not flagged, so two fingers on Fire and one lifting still leaves Fire held.

**Roles are just panel layouts.** Each player's panel has both a Pilot layout and a Gunner layout, and `UIManager.RefreshRoles` switches which one is active. Swapping roles means flipping one int (`pilotPlayer`) and refreshing the panels. Since hidden controls can't be hit-tested, fingers automatically end up driving whatever role their side of the screen shows.

**What happens to held input at a flux:** `CancelAllInput()` clears all finger ownership. Any finger still down is ignored until it's lifted and pressed again. In the Editor, keys/mouse buttons held through a flux are ignored until *everything* is released. Without this, whoever was holding Fire would suddenly be pressing a Pilot button.

In the Editor, the mouse only updates the reticle on frames where it actually moved, so touch-drag aiming through Unity Remote isn't overwritten every frame.

---

## 5. Quantum Flux and progression

There's **one timer** (`elapsed` in `GameManager`) and **one transition method** (`TriggerQuantumFlux`). No separate flux timers or coroutines.

The phase table lives in the GameManager inspector:

| Phase | Starts | Threat speed | Spawn interval | Enemy shot speed | Breach hazards |
|---|---|---|---|---|---|
| Patrol | 0s | ×1.00 | ×1.00 | ×1.00 | no |
| Alert | 20s | ×1.25 | ×1.00 | ×1.00 | yes |
| Critical | 40s | ×1.25 | ×0.70 | ×1.50 | yes |

Every frame, `UpdateRoundTimer` checks how long until the next phase's start time. In the last 3 seconds it shows `QUANTUM FLUX IN 3...`, and when the time is reached it calls `TriggerQuantumFlux`, which follows the spec's order:

1. Cancel all input, and end any active Boost or Shield (their cooldowns start from that moment)
2. Swap Pilot and Gunner
3. Apply the new phase's values to the spawner
4. Refresh the player panels, role labels and phase text
5. Show the `QUANTUM FLUX — ROLES REVERSED` banner for 1.5s

Adding a fourth phase is just one more row in the array. The flux happens automatically at its start time.

---

## 6. Pooling

Everything that gets spawned repeatedly is pooled: lasers, enemies, debris, breach hazards and enemy shots. Each type has its own `ObjectPool` and they're all prewarmed in `GameManager.Start`, before the Start button is even pressed.

The lifecycle:

1. **Prewarm:** the pool instantiates `prewarmCount` copies, inactive, and pushes them onto a `Stack`.
2. **Get:** `Get<T>()` pops one. If the pool is empty, it creates one more and logs a warning telling me to raise the prewarm count. The game doesn't break, it just shows up in the console.
3. **Launch:** `Launch(...)` is where the object resets itself: position, rotation, health, colour, fire timer. Then it activates and sets its velocity. Velocity is set *after* `SetActive(true)`, because the Rigidbody isn't simulated while inactive.
4. **ReturnToPool:** guarded by `IsActive`, so if two things try to return the same object in the same frame (e.g. a laser hitting two enemies at once), only the first one counts.
5. **Return:** deactivated and pushed back on the stack.

| Pool | Prewarm |
|---|---|
| Laser | 20 |
| Enemy | 15 |
| Debris | 10 |
| Breach hazard | 6 |
| Enemy projectile | 30 |

In development builds, each pool logs its **peak** number of objects in use at the end of a round. I use that to check the prewarm sizes are big enough for the Critical phase.

---

## 7. Collisions

Everything uses trigger colliders. Hazards and projectiles are **Dynamic** Rigidbody2Ds with gravity 0 (so trigger callbacks always fire) and move by velocity set once at launch. The ship is **Kinematic** and moves with `MovePosition`. Lasers use Continuous collision detection so they don't skip through things.

Layers: `Ship` (6), `PlayerLaser` (7), `Hazard` (8), `BreachHazard` (9), `EnemyProjectile` (10), `Boundary` (11).

Only these pairs collide. Everything else is off in the Physics 2D matrix:

| | Ship | PlayerLaser | Hazard | BreachHazard | EnemyProjectile | Boundary |
|---|---|---|---|---|---|---|
| **Ship** | | | ✔ | | ✔ | |
| **PlayerLaser** | | | ✔ | ✔ | | ✔ |
| **Hazard** | ✔ | ✔ | | | | ✔ |
| **BreachHazard** | | ✔ | | | | ✔ |
| **EnemyProjectile** | ✔ | | | | | ✔ |
| **Boundary** | | ✔ | ✔ | ✔ | ✔ | |

Because the matrix already filters what can touch what, the scripts don't need to check tags or layers. The **receiver handles the hit**:

- `Hazard` handles lasers (takes damage, returns the laser, adds score on death) and boundaries (returns itself; a breach hazard at the bottom edge ends the round).
- `ShipHealth` handles anything that reaches the ship. It always removes the threat, but only takes damage if the Shield is off and it isn't in its invulnerability window.
- `Projectile` only cares about boundaries.

The boundaries are four triggers placed by `Playfield`. The bottom one sits exactly on the screen's bottom edge, since it's also the breach line. The top/left/right ones are 2 units off-screen as cleanup walls.

---

## 8. Performance

Rules I followed in every script:

- No allocations in per-frame code: pools for everything spawned, `Input.GetTouch(i)` instead of `Input.touches` (which allocates a new array), the touch-ownership `Dictionary` created once with capacity 10.
- TMP text is set with `SetText("{0}", n)`, and the timer text is only rebuilt when the displayed second changes.
- UI objects are only `SetActive`'d when their state actually changes.
- No `Find*`, no `Camera.main` in hot paths, no LINQ, no coroutines. References are serialized or cached in `Awake`/`Initialize`.
- `Application.targetFrameRate = 60` (mobile defaults to 30).

### Profiling results

I couldn't profile on the phone itself. I don't do development at home, so I didn't have the setup to connect the phone to the Profiler, and with the time limit I couldn't get hold of it in time. Instead I profiled in the Unity Editor during the Critical phase (after 40s, with the pools already warmed up). Screenshots are in `Docs/Evidence/`.

- **Garbage:** the game's own code allocates **0 B** per frame. The whole `PlayerLoop` showed 131 B, and all of it came from Unity internals (`DeformationManager` and `NativeInputSystem`), not my scripts. (`profiler-gc-alloc-breakdown.png`, `profiler-memory.png`)
- **Frame time:** flat at about 16 ms (60 FPS, capped). The tall spikes in the graph are `EditorLoop`, the Editor itself. (`profiler-cpu-frame-time.png`)
- **Pools:** peak objects in use at once over a round, against the prewarm size. Nothing ran out, so nothing was instantiated during play:

| Pool | Peak | Prewarm |
|---|---|---|
| Laser | 6 | 20 |
| Enemy | 6 | 15 |
| Debris | 3 | 10 |
| Breach hazard | 3 | 6 |
| Enemy projectile | 5 | 30 |

I left the prewarm sizes as they are. A round where the Gunner fires non-stop uses more lasers, and a few spare inactive objects cost almost nothing.

On the phone itself (Galaxy A32) I played full rounds, and the Critical phase ran smoothly with no visible stutter.

---

## 9. AI usage

I used Claude Code (Anthropic's AI coding assistant) on this project.

**What I did:**
- Proposed the gameplay and design decisions to Ai. Made the gameplay and design decisions, including structure and architecture of the whole code, how aiming works on touch, touch/finger behavior during a flux, cooldown presentation, UI layout and colours, and gameplay tuning such as hazard speeds after testing on the device.
- Reviewed the proposed architecture and implementation approach after intensive discussion, and approved or changed them where necessary. The architectural rules used in the project were defined or agreed upon by me, including the use of a single singleton pattern, a single update loop, direct method calls, limited comments, and the code organization/region structure.
- Did all Unity Editor work myself, including scene setup, UI, prefabs, layers, collision matrix, project settings, and Android builds.
- Tested each functionality in the Unity Editor and on the phone before moving on, identified issues, and iterated on the implementation.
- Performed the profiling and device testing.

**What the AI did:**
- Assisted with implementation by suggesting approaches for technically difficult areas and explaining their trade-offs so I could choose the approach that fit the project.
- Helped write C# code based on the architecture and requirements I had established.
- Assisted with documentation and project README preparation.
- Helped inspect the project and identify potential setup or implementation issues.

**How I validated the work:** Every functionality was reviewed and tested by me in the Editor and on the target device. Nothing was considered complete until it produced the intended result.

---

## 10. Assumptions

Where the spec was silent, or the mockups disagreed with the text, this is what I went with:

| Topic | What I did |
|---|---|
| Mockups vs text | Where they differ (e.g. the banner wording, who is Pilot in the 40s mockup), the **written text wins**. The banner reads `QUANTUM FLUX — ROLES REVERSED`. |
| Aiming | Touch aiming is a relative drag, like a laptop trackpad. In the Editor the reticle follows the mouse. The reticle stays on screen and above the ship. |
| Breach hazard vs ship | The spec doesn't say the breach hazard damages the ship, so it passes through. It has to be shot down. |
| Cooldown timing | The cooldown starts when the effect ends. |
| Held input at a flux | Fingers held during a flux are ignored until lifted. In the Editor, held keys/buttons are ignored until everything is released. |
| Enemy firing | Enemies fire straight down on a fixed interval, with a random first delay so they don't all fire together. |
| Win timing | You win the moment the timer hits 60s, whatever is still on screen. |
| Cooldown indicators | Shown as fills on the Boost and Shield buttons. An active Shield is a circle around the ship. |

---

## 11. Incomplete work and next steps

- **Controls cover part of the playfield.** The two control panels sit in the bottom corners, so the ship and falling hazards can pass behind them. The fix I'd make next is to raise the playfield floor to the top of the panels (the ship, breach line and aim limit all come from `Playfield`, so it's one change there).
- **No on-device profiling.** Explained in the Performance section. With a cable I'd do a Development Build with Autoconnect Profiler and capture the Critical phase on the phone.
- **Not a full check against every line of the spec.** I built from the spec and checked it when questions came up, but I didn't do a final line-by-line pass through it.
