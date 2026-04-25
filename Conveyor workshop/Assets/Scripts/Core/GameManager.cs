using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<int> ScoreChanged;
    public event Action<int> DefectChanged;
    public event Action<float> WaveTimeChanged;
    public event Action<int> WaveIndexChanged;
    public event Action<float> SlowMoCooldownChanged;
    public event Action<bool> GameStateChanged;
    public event Action<int, int> StreakChanged;

    [Header("Wave")]
    [SerializeField] private float waveDurationSeconds = 300f;
    [SerializeField] private int maxDefects = 10;
    [SerializeField] private int waveCount = 10;
    [SerializeField] private float waveStepSpeedBonus = 0.3f;
    [SerializeField] private float waveStepSpawnBonus = 0.45f;

    [Header("Difficulty Ramp")]
    [SerializeField] private float baseConveyorSpeedMultiplier = 1f;
    [SerializeField] private float conveyorSpeedRampPerMinute = 0.12f;
    [SerializeField] private float baseSpawnRateMultiplier = 1f;
    [SerializeField] private float spawnRateRampPerMinute = 0.15f;
    [Tooltip("Последние N секунд: ослабление добавки к скорости/спавну (до финиша).")]
    [SerializeField] private float finalStretchSeconds = 90f;
    [SerializeField] [Range(0.5f, 1f)] private float finalStretchDifficultyFactor = 0.88f;
    [Tooltip("Последние M секунд — ещё сильнее облегчение.")]
    [SerializeField] private float finalSprintSeconds = 30f;
    [SerializeField] [Range(0.5f, 1f)] private float finalSprintDifficultyFactor = 0.74f;

    [Header("Scoring")]
    [SerializeField] private int correctCargoScore = 10;
    [SerializeField] private int wrongCargoPenalty = 5;
    [SerializeField] private int wrongCargoDefectCost = 1;

    [Header("Conveyor Slowdown")]
    [SerializeField] private KeyCode slowMoKey = KeyCode.Space;
    [SerializeField] private float slowMoDuration = 3.5f;
    [SerializeField] private float slowMoCooldown = 8f;
    [Range(0.1f, 1f)]
    [SerializeField] private float slowMoConveyorMultiplier = 0.55f;

    private bool isGameRunning = true;
    private string endMessage = string.Empty;
    private bool lastRoundWon;
    private int lastWaveIndexForSfx = 1;
    private ScoreService scoreService;
    private WaveTimerService waveTimerService;
    private SlowMoService slowMoService;
    private DifficultyService difficultyService;

    public bool IsGameRunning => isGameRunning;
    public int Score => scoreService != null ? scoreService.Score : 0;
    public int Defects => scoreService != null ? scoreService.Defects : 0;
    public int MaxDefects => maxDefects;
    public float WaveTimeLeft => waveTimerService != null ? waveTimerService.TimeLeft : 0f;
    public int CurrentWaveIndex => waveTimerService != null ? waveTimerService.CurrentWaveIndex : 1;
    public int TotalWaves => waveCount;
    public string EndMessage => endMessage;
    public bool LastRoundWon => lastRoundWon;
    public int CurrentStreak => scoreService != null ? scoreService.Streak : 0;
    public int CurrentStreakTier => scoreService != null ? scoreService.StreakTier : 0;

    public float ConveyorSpeedMultiplier
    {
        get
        {
            if (difficultyService == null || waveTimerService == null || slowMoService == null)
            {
                return 1f;
            }

            return difficultyService.ConveyorMultiplier(
                waveTimerService.ElapsedMinutes,
                waveTimerService.TimeLeft,
                waveTimerService.CurrentWaveIndex,
                waveTimerService.WaveCount,
                slowMoService.IsActive,
                slowMoConveyorMultiplier);
        }
    }

    public float SpawnRateMultiplier
    {
        get
        {
            if (difficultyService == null || waveTimerService == null)
            {
                return 1f;
            }

            return difficultyService.SpawnMultiplier(
                waveTimerService.ElapsedMinutes,
                waveTimerService.TimeLeft,
                waveTimerService.CurrentWaveIndex,
                waveTimerService.WaveCount);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        scoreService = new ScoreService(maxDefects, correctCargoScore, wrongCargoPenalty, wrongCargoDefectCost);
        waveTimerService = new WaveTimerService(waveDurationSeconds, waveCount);
        slowMoService = new SlowMoService(slowMoDuration, slowMoCooldown);
        difficultyService = new DifficultyService(
            baseConveyorSpeedMultiplier,
            conveyorSpeedRampPerMinute,
            baseSpawnRateMultiplier,
            spawnRateRampPerMinute,
            finalStretchSeconds,
            finalStretchDifficultyFactor,
            finalSprintSeconds,
            finalSprintDifficultyFactor,
            waveStepSpeedBonus,
            waveStepSpawnBonus);
    }

    private void Start()
    {
        ScoreChanged?.Invoke(Score);
        DefectChanged?.Invoke(Defects);
        WaveTimeChanged?.Invoke(WaveTimeLeft);
        WaveIndexChanged?.Invoke(CurrentWaveIndex);
        SlowMoCooldownChanged?.Invoke(1f);
        GameStateChanged?.Invoke(isGameRunning);
        StreakChanged?.Invoke(CurrentStreak, CurrentStreakTier);
        lastWaveIndexForSfx = CurrentWaveIndex;
    }

    private void Update()
    {
        if (!isGameRunning)
        {
            if (InputCompat.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        HandleSlowMoInput();
        UpdateSlowMoTimers();
        UpdateWaveTimer();
    }

    public void HandleCargoDelivered(bool isCorrect, int cargoScore = 0)
    {
        if (!isGameRunning)
        {
            return;
        }

        if (isCorrect)
        {
            ConveyorGameSfx.Instance?.PlayDelivery(true);
            int basePoints = cargoScore > 0 ? cargoScore : correctCargoScore;
            scoreService.ApplyDelivery(true, basePoints);
            ScoreChanged?.Invoke(Score);
            StreakChanged?.Invoke(scoreService.Streak, scoreService.StreakTier);
            return;
        }

        ConveyorGameSfx.Instance?.PlayDelivery(false);
        bool defectsReached = scoreService.ApplyDelivery(false, cargoScore);
        ScoreChanged?.Invoke(Score);
        DefectChanged?.Invoke(scoreService.Defects);
        StreakChanged?.Invoke(scoreService.Streak, scoreService.StreakTier);

        if (defectsReached)
        {
            EndGame(false);
        }
    }

    public void RegisterLostCargo()
    {
        HandleCargoDelivered(false, 0);
    }

    private void UpdateWaveTimer()
    {
        waveTimerService.Tick(Time.deltaTime);
        WaveTimeChanged?.Invoke(waveTimerService.TimeLeft);
        int waveNow = CurrentWaveIndex;
        WaveIndexChanged?.Invoke(waveNow);

        if (isGameRunning && waveNow > lastWaveIndexForSfx)
        {
            ConveyorGameSfx.Instance?.PlayWaveAdvance();
            lastWaveIndexForSfx = waveNow;
        }

        if (waveTimerService.IsFinished)
        {
            EndGame(true);
        }
    }

    private void HandleSlowMoInput()
    {
        if (InputCompat.GetKeyDown(slowMoKey) && slowMoService.TryActivate())
        {
            SlowMoCooldownChanged?.Invoke(1f);
        }
    }

    private void UpdateSlowMoTimers()
    {
        float normalized = slowMoService.Tick(Time.deltaTime);
        SlowMoCooldownChanged?.Invoke(normalized);
    }

    public bool IsSlowMoActive => slowMoService != null && slowMoService.IsActive;
    public float SlowMoTimeLeft => slowMoService != null ? Mathf.Max(0f, slowMoService.TimeLeft) : 0f;
    public float SlowMoCooldownLeft => slowMoService != null ? Mathf.Max(0f, slowMoService.CooldownLeft) : 0f;

    private void EndGame(bool isWin)
    {
        isGameRunning = false;
        lastRoundWon = isWin;
        endMessage = isWin ? "Смена завершена! Нажмите [R], чтобы начать заново" : "Лимит брака достигнут. Нажмите [R], чтобы повторить";
        ConveyorGameSfx.Instance?.PlayGameEnd(isWin);
        GameStateChanged?.Invoke(false);
    }

}
