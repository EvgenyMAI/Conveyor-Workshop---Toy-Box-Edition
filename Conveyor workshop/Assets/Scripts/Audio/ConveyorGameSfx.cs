using UnityEngine;

/// <summary>
/// Procedural SFX for deliveries, waves, and round end. Single non-spatial AudioSource.
/// </summary>
[DefaultExecutionOrder(-200)]
public class ConveyorGameSfx : MonoBehaviour
{
    public static ConveyorGameSfx Instance { get; private set; }

    [Header("Levels")]
    [SerializeField] [Range(0.05f, 0.55f)] private float masterVolume = 0.34f;
    [SerializeField] [Range(0.25f, 1f)] private float correctVolumeMul = 0.48f;
    [SerializeField] [Range(0.25f, 1f)] private float waveVolumeMul = 0.38f;
    [SerializeField] [Range(0.5f, 1.5f)] private float wrongVolumeMul = 1.22f;
    [SerializeField] [Range(0.5f, 1.2f)] private float winEndVolumeMul = 0.88f;
    [SerializeField] [Range(0.5f, 1.2f)] private float loseEndVolumeMul = 0.82f;

    private AudioSource audioSource;
    private AudioClip correctClip;
    private AudioClip wrongClip;
    private AudioClip waveClip;
    private AudioClip winEndClip;
    private AudioClip loseEndClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.volume = 1f;

        BuildClips();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BuildClips()
    {
        // Softer, slightly lower pitches — less fatiguing when heard often.
        correctClip = ProceduralAudio.TwoToneChime("SfxCorrect", 560f, 760f, 0.1f, 0.12f);
        wrongClip = ProceduralAudio.WrongBuzzThud();
        waveClip = ProceduralAudio.SoftPing("SfxWave", 400f, 0.05f, 0.1f);
        winEndClip = ProceduralAudio.WinEndStinger();
        loseEndClip = ProceduralAudio.LoseEndStinger();
    }

    /// <summary>Correct vs wrong delivery at a receiver or lost cargo.</summary>
    public void PlayDelivery(bool success)
    {
        if (success)
        {
            PlayCorrectDelivery();
        }
        else
        {
            PlayWrongDelivery();
        }
    }

    public void PlayCorrectDelivery()
    {
        PlayClip(correctClip, correctVolumeMul);
    }

    public void PlayWrongDelivery()
    {
        PlayClip(wrongClip, wrongVolumeMul);
    }

    public void PlayWaveAdvance()
    {
        PlayClip(waveClip, waveVolumeMul);
    }

    public void PlayGameEnd(bool won)
    {
        PlayClip(won ? winEndClip : loseEndClip, won ? winEndVolumeMul : loseEndVolumeMul);
    }

    private void PlayClip(AudioClip clip, float volumeMul)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, masterVolume * volumeMul);
    }
}
