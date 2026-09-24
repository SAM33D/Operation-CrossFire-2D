using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Round state, the round timer, phases, score and win/lose. Ticks every system in a fixed order.
/// </summary>
public class GameManager : MonoBehaviour
{
    private enum RoundState { WaitingToStart, Playing, Ended }

    public static GameManager Instance { get; private set; }

    [Header("Systems")]
    [SerializeField] private InputHandler input;
    [SerializeField] private ShipPilot pilot;
    [SerializeField] private ShipGunner gunner;
    [SerializeField] private ShipHealth health;
    [SerializeField] private Spawner spawner;
    [SerializeField] private Playfield playfield;
    [SerializeField] private UIManager ui;
    [Tooltip("Filled at startup so nothing is instantiated during play.")]
    [SerializeField] private ObjectPool[] poolsToPrewarm;

    [Header("Round")]
    [Tooltip("Seconds to survive to win.")]
    [SerializeField] private float roundDuration = 60f;
    [Tooltip("Seconds of countdown shown before each Quantum Flux.")]
    [SerializeField] private float fluxCountdownSeconds = 3f;

    [Header("Phases")]
    [Tooltip("A Quantum Flux happens at each phase's start time. Each row holds its full values.")]
    [SerializeField] private PhaseData[] phases =
    {
        new PhaseData("Patrol",    0f, 1.00f, 1.00f, 1.00f, false),
        new PhaseData("Alert",    20f, 1.25f, 1.00f, 1.00f, true),
        new PhaseData("Critical", 40f, 1.25f, 0.70f, 1.50f, true),
    };

    private RoundState state;
    private float elapsed;
    private int phaseIndex;
    private int score;
    private int pilotPlayer;

    public InputHandler Input => input;
    public Playfield Playfield => playfield;
    public Spawner Spawner => spawner;
    public bool IsPlaying => state == RoundState.Playing;
    public float RemainingTime => Mathf.Max(0f, roundDuration - elapsed);
    public int PilotPlayer => pilotPlayer;

    private bool HasNextPhase => phaseIndex + 1 < phases.Length;

    #region Initialisation
    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        playfield.Initialize();
        PrewarmPools();
        pilot.Initialize();
        gunner.Initialize(); // needs the ship's start position
        health.Initialize();
        spawner.Initialize();

        pilotPlayer = 1;
        ui.RefreshRoles(pilotPlayer);
        ui.SetAbilityFills(pilot.Boost, gunner.Shield);
        ui.SetHull(health.Hull);
        ui.ShowStartScreen();
        state = RoundState.WaitingToStart;
    }

    private void Update()
    {
        if (state != RoundState.Playing) return;
        float dt = Time.deltaTime;

        input.Tick();
        UpdateRoundTimer(dt);
        if (state != RoundState.Playing) return;

        pilot.Tick(dt);
        gunner.Tick(dt);
        health.Tick(dt);
        spawner.Tick(dt);
        ui.SetAbilityFills(pilot.Boost, gunner.Shield);
        ui.Tick();
    }
    #endregion

    #region Round Flow
    public void StartRound()
    {
        if (state != RoundState.WaitingToStart) return;

        Time.timeScale = 1f;
        elapsed = 0f;
        score = 0;
        phaseIndex = 0;

        spawner.ApplyPhase(phases[phaseIndex]);
        spawner.Begin();

        ui.SetPhase(phases[phaseIndex].phaseName);
        ui.SetScore(score);
        ui.HideStartScreen();

        state = RoundState.Playing;
        ui.Tick();
    }

    public void RestartRound()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion

    #region Round Timer & Quantum Flux
    private void UpdateRoundTimer(float dt)
    {
        elapsed += dt;

        if (elapsed >= roundDuration) { EndRound(true, "Corridor secured"); return; }
        if (!HasNextPhase) return;

        float timeToFlux = phases[phaseIndex + 1].startTime - elapsed;
        if (timeToFlux <= 0f) TriggerQuantumFlux();
        else if (timeToFlux <= fluxCountdownSeconds) ui.ShowFluxCountdown(Mathf.CeilToInt(timeToFlux));
    }

    // Steps follow the spec's order: cancel, swap, apply phase, update panels, banner
    private void TriggerQuantumFlux()
    {
        input.CancelAllInput();
        pilot.CancelActions();
        gunner.CancelActions();

        pilotPlayer = pilotPlayer == 1 ? 2 : 1;

        phaseIndex++;
        spawner.ApplyPhase(phases[phaseIndex]);

        ui.RefreshRoles(pilotPlayer);
        ui.SetPhase(phases[phaseIndex].phaseName);

        ui.ShowFluxBanner();
    }
    #endregion

    #region Score
    public void AddScore(int points)
    {
        if (!IsPlaying) return;

        score += points;
        ui.SetScore(score);
    }
    #endregion

    #region Win / Lose
    public void OnHullChanged(int hull)
    {
        ui.SetHull(hull);
        if (hull <= 0) EndRound(false, "Hull destroyed");
    }
    public void OnBreachReachedBottom()
    {
        EndRound(false, "A breach hazard got through");
    }
    private void EndRound(bool won, string reason)
    {
        if (state == RoundState.Ended) return;
        state = RoundState.Ended;

        spawner.Stop();
        input.CancelAllInput();
        Time.timeScale = 0f;
        ui.Tick();
        ui.ShowEndScreen(won, reason);

        if (Debug.isDebugBuild) LogPoolPeaks();
    }
    #endregion

    #region Pooling
    private void PrewarmPools()
    {
        for (int i = 0; i < poolsToPrewarm.Length; i++)
        {
            poolsToPrewarm[i].Prewarm();
        }
    }

    private void LogPoolPeaks()
    {
        for (int i = 0; i < poolsToPrewarm.Length; i++)
        {
            poolsToPrewarm[i].LogPeakUsage();
        }
    }
    #endregion
}
