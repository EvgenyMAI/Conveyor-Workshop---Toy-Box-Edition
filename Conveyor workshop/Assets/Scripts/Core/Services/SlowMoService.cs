using UnityEngine;

public sealed class SlowMoService
{
    private readonly float duration;
    private readonly float cooldown;

    public bool IsActive { get; private set; }
    public float TimeLeft { get; private set; }
    public float CooldownLeft { get; private set; }

    public SlowMoService(float duration, float cooldown)
    {
        this.duration = Mathf.Max(0.01f, duration);
        this.cooldown = Mathf.Max(0.01f, cooldown);
    }

    public bool TryActivate()
    {
        if (IsActive || CooldownLeft > 0f)
        {
            return false;
        }

        IsActive = true;
        TimeLeft = duration;
        CooldownLeft = cooldown;
        return true;
    }

    public float Tick(float deltaSeconds)
    {
        float dt = Mathf.Max(0f, deltaSeconds);
        if (IsActive)
        {
            TimeLeft -= dt;
            float activeProgress = Mathf.Clamp01(TimeLeft / duration);
            if (TimeLeft <= 0f)
            {
                IsActive = false;
                TimeLeft = 0f;
            }

            return activeProgress;
        }

        if (CooldownLeft > 0f)
        {
            CooldownLeft -= dt;
            return 1f - Mathf.Clamp01(CooldownLeft / cooldown);
        }

        CooldownLeft = 0f;
        return 1f;
    }
}
