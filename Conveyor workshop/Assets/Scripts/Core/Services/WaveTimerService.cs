using UnityEngine;

public sealed class WaveTimerService
{
    private readonly float waveDurationSeconds;
    private readonly int waveCount;

    public float TimeLeft { get; private set; }
    public int WaveCount => waveCount;
    public bool IsFinished => TimeLeft <= 0f;

    public int CurrentWaveIndex
    {
        get
        {
            if (waveCount <= 1)
            {
                return 1;
            }

            float elapsed = waveDurationSeconds - TimeLeft;
            float waveLength = waveDurationSeconds / waveCount;
            int idx = Mathf.FloorToInt(elapsed / Mathf.Max(0.001f, waveLength)) + 1;
            return Mathf.Clamp(idx, 1, waveCount);
        }
    }

    public float ElapsedMinutes
    {
        get
        {
            float elapsedSeconds = waveDurationSeconds - TimeLeft;
            return Mathf.Max(0f, elapsedSeconds / 60f);
        }
    }

    public WaveTimerService(float waveDurationSeconds, int waveCount)
    {
        this.waveDurationSeconds = Mathf.Max(1f, waveDurationSeconds);
        this.waveCount = Mathf.Max(1, waveCount);
        TimeLeft = this.waveDurationSeconds;
    }

    public void Tick(float deltaSeconds)
    {
        TimeLeft -= Mathf.Max(0f, deltaSeconds);
        TimeLeft = Mathf.Max(0f, TimeLeft);
    }
}
