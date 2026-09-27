# Operation Cross-Fire: Project Context

Unity 6 (`6000.6.2f1`, Universal 2D) landscape local co-op shooter for one phone: Pilot + Gunner share a ship,
a Quantum Flux swaps roles at 20s and 40s, and the round lasts 60s. Built as a junior Unity developer exercise for
SquadLoom. Sam will be asked to explain the code in an interview.

> **Status (2026-09-25): SUBMITTED.** The public repo https://github.com/SAM33D/Operation-CrossFire-2D is final
> (`cd112e4`). What's left is **bringing everything together**: explaining how the whole game works (the
> architecture, input ownership, role switching, the pooling lifecycle and every line of code) to prepare for the
> interview, plus practising small live changes.

## Read first
- `PHASE_PLAN.md`: **current status, the numbered open items, and the per-phase progress log.** Start here.
- `IMPLEMENTATION_PLAN.md`: how everything is built (architecture, script specs, decisions D1–D16). The working
  source of truth.
- The original spec (10 pages, confidential, never committed) is at `C:\Users\Sameed\Downloads\Junior Unity Dev
  Exercise_ _Operation Cross-Fire_ Protecting the Orbital Corridor_.pdf` (`Docs/Private/` no longer exists).
  **Reference only:** open it when Sam asks what the spec says. A full line-by-line check passed on 2026-09-25. The
  `Read` tool can read it without page ranges; `pdftoppm` isn't installed, so the `pages` parameter fails.

## How we work
- **Sam does all Editor work** (scenes, prefabs, GameObjects, inspector wiring, Project Settings, builds) by
  following numbered steps. Claude Code writes C#, `.gitignore`, README and the plan docs, and **never hand-edits
  `.unity`, `.prefab` or `ProjectSettings/*.asset`**. Reading them to verify Sam's setup is fine.
- **Bigger phases are split into testable sub-steps** (Phase 2 was 2a/2b/2c), each with its own Intended Output,
  test and log entry.
- **After Sam reports a step working:** audit the scene read-only (grep `Main.unity` / prefabs for references,
  layers, values) and report any mistakes, then update `PHASE_PLAN.md`.
- **Per phase:** post the Intended Output (IMPLEMENTATION_PLAN §0.2 template) → wait for Sam's go → write code →
  post Walkthrough + Editor Setup Steps + Test Checklist + commit message → Sam tests → update `PHASE_PLAN.md`
  (status + progress log entry with the decisions made).
- A short system-by-system guide (what / how / why) already exists as `C:\Users\Sameed\Desktop\Operation CrossFire - Codebase Guide.pdf`, and
  Phase 6.5 should build on it rather than repeat it. Sam wants concise explanations, not line-by-line ones.
- Build only what the plan describes. Ask before adding any script, pattern or system not listed in §5.

- **Sam makes design calls; ask before changing behaviour Sam has opinions on.** Say plainly when something is
  our own choice rather than the spec's (e.g. the cooldown fill style). When Sam's idea conflicts with the spec or
  mockup, show both with the spec quote and let Sam pick.
- **Walkthroughs and commit messages are for different readers.** Commit messages and the README are read by the
  evaluator: describe the game, never "Phase N" or anything from the private planning docs.

## Git
- **Started 2026-09-25, partway through (honest late start; backdating/reconstruction declined).** Repo `main`, pushed
  to `github.com/SAM33D/Operation-CrossFire-2D` (**public** since submission). **Sam commits and pushes via GitHub Desktop.** After each
  working step, give Sam a Summary + description. Claude may run read-only git commands (`status`, `log`,
  `ls-tree`) to verify, but doesn't commit or push.
- **Never commit** `CLAUDE.md`, `IMPLEMENTATION_PLAN.md`, `PHASE_PLAN.md` or `.claude/`: excluded via
  `.git/info/exclude` (local-only), so `.gitignore` doesn't reveal them. `Docs/Private/` is in `.gitignore`.
- The README must say git started partway, and its AI-usage section must be truthful (spec requirement).
- **README status:** complete and committed (sections 1–11; evidence in `Docs/Evidence/`). The known issues listed in §11: the control panels hide the ship/hazards (planned fix: raise
  `Playfield.MinY` to the panel top) and there was no on-device profiling. Write any README text in Sam's voice: plain,
  first person, not polished or AI-sounding.

## Code conventions (from IMPLEMENTATION_PLAN §2)
- `GameManager.Instance` is the only singleton. Systems expose `Tick(float dt)`, and GameManager's `Update` calls
  them in a fixed order. Pooled objects are the only exception.
- `Awake` caches a script's own components only; all cross-system setup goes in `GameManager.Start`.
- Direct method calls only: no C# events, UnityEvents (except button OnClick), interfaces or message buses.
- **Minimal comments (Sam's rule):** a one-line `/// <summary>` per class, written as a plain sentence. Otherwise
  comment only non-obvious guards or ordering, in one short line. Never "Owns / Called by / Talks to" blocks. Sam
  finds heavy commenting looks AI-written, and the README carries the explanations. Put the *why* in chat
  walkthroughs instead.
- `[Header]` on serialized fields; short `[Tooltip]`s only where the name isn't self-explanatory. `#region`s by
  functionality for scripts over ~50 lines. **Sam's region style:** no blank line after `#region` or before
  `#endregion`, and GameManager's Awake/Start/Update region is called `Initialisation` (British spelling).
- No per-frame allocations, no `Find*` / `Camera.main` in hot paths, no LINQ, no coroutines. TMP `SetText("{0}", n)`.
- Scripts live under `Assets/_Project/Scripts/<Core|Input|Ship|Threats|Projectiles|Pooling|UI>/`.

## Environment notes
- Windows. **Python isn't installed**: `python` opens the Microsoft Store stub and hangs, so use PowerShell.
  To make a PDF: write HTML to the scratchpad and print it with headless Edge
  (`msedge.exe --headless --no-pdf-header-footer --print-to-pdf="<out>" file:///<html>`). Desktop is
  `C:\Users\Sameed\Desktop`.
- **Active Input Handling = `Input Manager (Old)`** (switched from `Both` on 2026-09-25, because Unity warns that
  `Both` may not work on Android). All gameplay input is legacy `Input.*`; the EventSystem uses the Standalone
  Input Module. Don't write new-Input-System code.
- VS Code shows false "type not found" errors for new scripts until Unity regenerates the `.csproj`. Check
  Unity's Console or `%LOCALAPPDATA%\Unity\Editor\Editor.log` for real compile errors.
- Project Settings changes don't always reach disk right away. If a `ProjectSettings/*.asset` value looks
  stale, ask Sam to use **File → Save Project** before assuming the change wasn't made.
- Layer indices: Ship 6, PlayerLaser 7, Hazard 8, BreachHazard 9, EnemyProjectile 10, Boundary 11.
- **Scene audits:** `Main.unity` is large (~280 KB). Use awk to resolve fileIDs to Hierarchy paths (GameObject
  names + m_Father chains). Watch for trailing spaces in Sam's object names (`'PilotControls '`). The
  TouchControl `controlType` enum is Left 0, Right 1, Boost 2, Aim 3, Fire 4, Shield 5, Bomb 6.
- **UI built by Sam:** `Canvas/Player{1,2}Panel` (frame Image + PanelBackground + RoleLabel + PilotControls (HLG) +
  GunnerControls (HLG: AimControl flex 1.6 + GunnerButtons VLG)). Each control is a border Image + Background
  inset 4. GunnerButtons also holds BombControl (added 2026-09-27, cooldown fill only). Boost/Shield have `ActiveFill` + `CooldownFill` children (switched off in the scene; the code toggles
  them). `Hull HUD` container, `FluxBanner`.
