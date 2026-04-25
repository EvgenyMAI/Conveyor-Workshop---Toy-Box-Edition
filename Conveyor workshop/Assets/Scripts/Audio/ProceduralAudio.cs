using UnityEngine;

/// <summary>
/// Shared tiny procedural clips (no .wav assets). Kept in one place to avoid duplicating math in HUD vs game SFX.
/// </summary>
public static class ProceduralAudio
{
    public const int SampleRate = 44100;

    public static AudioClip SineBlip(string name, float frequency, float duration, float peakAmplitude = 0.28f)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Sin(Mathf.PI * t);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * (i / (float)SampleRate)) * env * peakAmplitude;
        }

        AudioClip clip = AudioClip.Create(name, n, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip TwoToneChime(string name, float freqA, float freqB, float duration, float peakAmplitude = 0.22f)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[n];
        int split = Mathf.Max(1, n / 2);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Sin(Mathf.PI * t);
            env *= env;
            float f = i < split ? freqA : freqB;
            samples[i] = Mathf.Sin(2f * Mathf.PI * f * (i / (float)SampleRate)) * env * peakAmplitude;
        }

        AudioClip clip = AudioClip.Create(name, n, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Audible on laptop speakers: low body + slight upper buzz, longer than a 75ms click.
    /// </summary>
    public static AudioClip WrongBuzzThud(string name = "SfxWrong")
    {
        const float duration = 0.14f;
        int n = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Sin(Mathf.PI * t);
            float decay = Mathf.Exp(-t * 8.5f);
            float body = Mathf.Sin(2f * Mathf.PI * 205f * (i / (float)SampleRate));
            float sub = Mathf.Sin(2f * Mathf.PI * 102f * (i / (float)SampleRate)) * 0.42f;
            float buzz = Mathf.Sin(2f * Mathf.PI * 520f * (i / (float)SampleRate)) * 0.14f;
            samples[i] = (body * 0.55f + sub + buzz) * decay * env * 0.62f;
        }

        AudioClip clip = AudioClip.Create(name, n, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>Short upward major arpeggio — celebratory but not loud.</summary>
    public static AudioClip WinEndStinger(string name = "SfxWinEnd")
    {
        return StackedNotes(name, new[] { 523f, 659f, 784f, 988f }, 0.36f, 0.17f);
    }

    /// <summary>Soft descending line — clear “loss” without being harsh.</summary>
    public static AudioClip LoseEndStinger(string name = "SfxLoseEnd")
    {
        return StackedNotes(name, new[] { 392f, 349f, 311f, 262f }, 0.42f, 0.13f);
    }

    private static AudioClip StackedNotes(string name, float[] freqs, float totalDuration, float peak)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(SampleRate * totalDuration));
        float[] samples = new float[n];
        int noteCount = freqs.Length;
        float noteLen = totalDuration / Mathf.Max(1, noteCount);
        for (int i = 0; i < n; i++)
        {
            float sec = i / (float)SampleRate;
            int noteIdx = Mathf.Clamp((int)(sec / noteLen), 0, noteCount - 1);
            float local = (sec - noteIdx * noteLen) / noteLen;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(local));
            float f = freqs[noteIdx];
            samples[i] = Mathf.Sin(2f * Mathf.PI * f * (i / (float)SampleRate)) * env * peak;
        }

        AudioClip clip = AudioClip.Create(name, n, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip SoftPing(string name, float frequency, float duration, float peakAmplitude = 0.14f)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Sin(Mathf.PI * t);
            env *= env * env;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * (i / (float)SampleRate)) * env * peakAmplitude;
        }

        AudioClip clip = AudioClip.Create(name, n, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
