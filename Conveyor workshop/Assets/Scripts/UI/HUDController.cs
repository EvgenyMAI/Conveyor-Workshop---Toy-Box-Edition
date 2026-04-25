using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDController : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text defectsText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text waveIndexText;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private TMP_Text gameStateText;
    [SerializeField] private TMP_Text cooldownHintText;
    [SerializeField] private Image resultOverlay;
    [SerializeField] private CanvasGroup resultCanvasGroup;
    [SerializeField] private float resultFadeSeconds = 0.38f;
    [SerializeField] private Image resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultHintText;
    [SerializeField] private Image introOverlay;
    [SerializeField] private TMP_Text introText;

    [Header("Bars")]
    [SerializeField] private Slider defectsSlider;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private RectTransform cooldownFillRect;
    [SerializeField] private float cooldownBarWidth = 200f;
    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;

    private int bestScore;
    private AudioSource uiAudioSource;
    private AudioClip[] streakTierClips;
    private int lastTier = -1;
    private Coroutine resultFadeRoutine;

    private void Start()
    {
        gameManager = gameManager != null ? gameManager : GameManager.Instance;
        if (gameManager == null)
        {
            return;
        }

        bestScore = PlayerPrefs.GetInt("BestScore", 0);

        gameManager.ScoreChanged += OnScoreChanged;
        gameManager.DefectChanged += OnDefectChanged;
        gameManager.WaveTimeChanged += OnWaveTimeChanged;
        gameManager.WaveIndexChanged += OnWaveIndexChanged;
        gameManager.StreakChanged += OnStreakChanged;
        gameManager.SlowMoCooldownChanged += OnSlowMoCooldownChanged;
        gameManager.GameStateChanged += OnGameStateChanged;
        EnsureAudio();

        OnScoreChanged(gameManager.Score);
        OnDefectChanged(gameManager.Defects);
        OnWaveTimeChanged(gameManager.WaveTimeLeft);
        OnWaveIndexChanged(gameManager.CurrentWaveIndex);
        OnStreakChanged(gameManager.CurrentStreak, gameManager.CurrentStreakTier);
        OnSlowMoCooldownChanged(0f);
        OnGameStateChanged(gameManager.IsGameRunning);

        if (introOverlay != null && introText != null)
        {
            StartCoroutine(ShowIntroCountdown());
        }
    }

    private void OnDestroy()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.ScoreChanged -= OnScoreChanged;
        gameManager.DefectChanged -= OnDefectChanged;
        gameManager.WaveTimeChanged -= OnWaveTimeChanged;
        gameManager.WaveIndexChanged -= OnWaveIndexChanged;
        gameManager.StreakChanged -= OnStreakChanged;
        gameManager.SlowMoCooldownChanged -= OnSlowMoCooldownChanged;
        gameManager.GameStateChanged -= OnGameStateChanged;
    }

    private void OnScoreChanged(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Счёт: {score}";
        }

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = $"Рекорд: {bestScore}";
        }
    }

    private void OnDefectChanged(int defects)
    {
        if (defectsText != null && gameManager != null)
        {
            defectsText.text = $"Брак: {defects}/{gameManager.MaxDefects}";
        }

        if (defectsSlider != null && gameManager != null)
        {
            defectsSlider.maxValue = gameManager.MaxDefects;
            defectsSlider.value = defects;
        }
    }

    private void OnWaveTimeChanged(float seconds)
    {
        if (timerText == null)
        {
            return;
        }

        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"Время: {mins:00}:{secs:00}";
    }

    private void OnWaveIndexChanged(int waveIndex)
    {
        if (waveIndexText != null)
        {
            waveIndexText.text = string.Empty;
        }
    }

    private void OnStreakChanged(int streak, int tier)
    {
        if (streakText != null)
        {
            int bonus = tier * 5;
            streakText.text = bonus > 0 ? $"Серия: {streak} (+{bonus})" : $"Серия: {streak}";
        }

        if (lastTier >= 0 && tier != lastTier && uiAudioSource != null && tier > 0 && tier <= streakTierClips.Length)
        {
            AudioClip clip = streakTierClips[tier - 1];
            if (clip != null)
            {
                uiAudioSource.PlayOneShot(clip, 0.28f);
            }
        }
        lastTier = tier;
    }

    private void OnSlowMoCooldownChanged(float normalized)
    {
        if (cooldownFill != null)
        {
            float clamped = Mathf.Clamp01(normalized);
            cooldownFill.fillAmount = 1f;
            cooldownFill.color = clamped >= 0.99f
                ? new Color(0.3f, 0.95f, 0.45f, 0.95f)
                : new Color(0.25f, 0.9f, 1f, 0.9f);

            if (cooldownFillRect != null)
            {
                Vector2 size = cooldownFillRect.sizeDelta;
                size.x = cooldownBarWidth * clamped;
                cooldownFillRect.sizeDelta = size;
            }
        }

        if (cooldownHintText != null)
        {
            if (gameManager != null && gameManager.IsSlowMoActive)
            {
                cooldownHintText.text = $"Замедление: АКТИВНО {gameManager.SlowMoTimeLeft:0.0}с";
            }
            else
            {
                cooldownHintText.text = normalized < 0.99f
                    ? $"Замедление: перезарядка {gameManager.SlowMoCooldownLeft:0.0}с"
                    : "Замедление: ГОТОВО [Space]";
            }
        }
    }

    private void OnGameStateChanged(bool isRunning)
    {
        if (gameStateText != null)
        {
            gameStateText.text = string.Empty;
        }

        if (resultFadeRoutine != null)
        {
            StopCoroutine(resultFadeRoutine);
            resultFadeRoutine = null;
        }

        if (isRunning)
        {
            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = 0f;
                resultCanvasGroup.interactable = false;
                resultCanvasGroup.blocksRaycasts = false;
            }

            if (resultOverlay != null)
            {
                resultOverlay.gameObject.SetActive(false);
            }

            if (resultPanel != null)
            {
                resultPanel.gameObject.SetActive(false);
            }

            return;
        }

        if (resultOverlay != null)
        {
            resultOverlay.gameObject.SetActive(true);
        }

        if (resultPanel != null)
        {
            resultPanel.gameObject.SetActive(true);
        }

        if (resultCanvasGroup != null)
        {
            resultCanvasGroup.alpha = 0f;
            resultCanvasGroup.interactable = true;
            resultCanvasGroup.blocksRaycasts = true;
            resultFadeRoutine = StartCoroutine(FadeResultScreenIn());
        }

        if (resultTitleText != null && gameManager != null)
        {
            resultTitleText.text = gameManager.LastRoundWon ? "СМЕНА ЗАВЕРШЕНА" : "СМЕНА ПРОВАЛЕНА";
            resultTitleText.color = gameManager.LastRoundWon
                ? new Color(0.45f, 1f, 0.55f)
                : new Color(1f, 0.45f, 0.45f);
        }

        if (resultHintText != null && gameManager != null)
        {
            resultHintText.text = $"{gameManager.EndMessage}\nИтоговый счёт: {gameManager.Score}";
        }
    }

    private IEnumerator FadeResultScreenIn()
    {
        if (resultCanvasGroup == null)
        {
            yield break;
        }

        float dur = Mathf.Max(0.02f, resultFadeSeconds);
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            resultCanvasGroup.alpha = Mathf.Clamp01(t / dur);
            yield return null;
        }

        resultCanvasGroup.alpha = 1f;
        resultFadeRoutine = null;
    }

    private IEnumerator ShowIntroCountdown()
    {
        introOverlay.gameObject.SetActive(true);
        introText.text = "3";
        yield return new WaitForSeconds(0.5f);
        introText.text = "2";
        yield return new WaitForSeconds(0.5f);
        introText.text = "1";
        yield return new WaitForSeconds(0.5f);
        introText.text = "СТАРТ!";
        yield return new WaitForSeconds(0.5f);
        introOverlay.gameObject.SetActive(false);
    }

    private void EnsureAudio()
    {
        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
        }
        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f;

        streakTierClips = new[]
        {
            ProceduralAudio.SineBlip("Tier1", 690f, 0.08f, 0.24f),
            ProceduralAudio.SineBlip("Tier2", 790f, 0.09f, 0.24f),
            ProceduralAudio.SineBlip("Tier3", 890f, 0.1f, 0.24f)
        };
    }
}
