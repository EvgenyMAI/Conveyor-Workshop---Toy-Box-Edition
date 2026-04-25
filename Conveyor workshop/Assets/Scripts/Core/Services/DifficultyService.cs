using UnityEngine;

public sealed class DifficultyService
{
    private readonly float baseConveyorSpeedMultiplier;
    private readonly float conveyorSpeedRampPerMinute;
    private readonly float baseSpawnRateMultiplier;
    private readonly float spawnRateRampPerMinute;
    private readonly float finalStretchSeconds;
    private readonly float finalStretchDifficultyFactor;
    private readonly float finalSprintSeconds;
    private readonly float finalSprintDifficultyFactor;
    private readonly float waveStepSpeedBonus;
    private readonly float waveStepSpawnBonus;

    public DifficultyService(
        float baseConveyorSpeedMultiplier,
        float conveyorSpeedRampPerMinute,
        float baseSpawnRateMultiplier,
        float spawnRateRampPerMinute,
        float finalStretchSeconds,
        float finalStretchDifficultyFactor,
        float finalSprintSeconds,
        float finalSprintDifficultyFactor,
        float waveStepSpeedBonus,
        float waveStepSpawnBonus)
    {
        this.baseConveyorSpeedMultiplier = baseConveyorSpeedMultiplier;
        this.conveyorSpeedRampPerMinute = conveyorSpeedRampPerMinute;
        this.baseSpawnRateMultiplier = baseSpawnRateMultiplier;
        this.spawnRateRampPerMinute = spawnRateRampPerMinute;
        this.finalStretchSeconds = finalStretchSeconds;
        this.finalStretchDifficultyFactor = finalStretchDifficultyFactor;
        this.finalSprintSeconds = finalSprintSeconds;
        this.finalSprintDifficultyFactor = finalSprintDifficultyFactor;
        this.waveStepSpeedBonus = waveStepSpeedBonus;
        this.waveStepSpawnBonus = waveStepSpawnBonus;
    }

    public float ConveyorMultiplier(float elapsedMinutes, float timeLeft, int currentWaveIndex, int waveCount, bool slowMoActive, float slowMoConveyorMultiplier)
    {
        float tame = FinalStretchTamer(timeLeft);
        float ramp = baseConveyorSpeedMultiplier + conveyorSpeedRampPerMinute * elapsedMinutes * tame;
        ramp += WaveDifficultyCurve(currentWaveIndex, waveCount) * waveStepSpeedBonus * (waveCount - 1) * tame;
        return slowMoActive ? ramp * slowMoConveyorMultiplier : ramp;
    }

    public float SpawnMultiplier(float elapsedMinutes, float timeLeft, int currentWaveIndex, int waveCount)
    {
        float tame = FinalStretchTamer(timeLeft);
        return baseSpawnRateMultiplier
            + spawnRateRampPerMinute * elapsedMinutes * tame
            + WaveDifficultyCurve(currentWaveIndex, waveCount) * waveStepSpawnBonus * (waveCount - 1) * tame;
    }

    private float WaveDifficultyCurve(int currentWaveIndex, int waveCount)
    {
        if (waveCount <= 1)
        {
            return 0f;
        }

        float t = (currentWaveIndex - 1f) / (waveCount - 1f);
        return Mathf.Pow(t, 1.38f);
    }

    private float FinalStretchTamer(float timeLeft)
    {
        if (finalStretchSeconds <= 0.1f || timeLeft >= finalStretchSeconds)
        {
            return 1f;
        }

        float mid = finalStretchDifficultyFactor;
        if (timeLeft <= finalSprintSeconds && finalSprintSeconds > 0.05f)
        {
            return Mathf.Lerp(finalSprintDifficultyFactor, mid, timeLeft / finalSprintSeconds);
        }

        float span = finalStretchSeconds - finalSprintSeconds;
        if (span <= 0.01f)
        {
            return Mathf.Lerp(mid, 1f, timeLeft / finalStretchSeconds);
        }

        return Mathf.Lerp(mid, 1f, (timeLeft - finalSprintSeconds) / span);
    }
}
