using UnityEngine;
using System.Collections;

public class ReceiverFeedback : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private ParticleSystem successEffect;
    [SerializeField] private ParticleSystem failEffect;
    [SerializeField] private Renderer pulseRenderer;
    [SerializeField] private Color failFlashColor = new Color(1f, 0.88f, 0.35f);
    [SerializeField] private float failFlashEmissionMul = 3.2f;
    [SerializeField] private float failFlashDuration = 0.16f;
    private Coroutine flashRoutine;
    private MaterialPropertyBlock mpb;
    private Color cachedEmission;

    private void Awake()
    {
        if (pulseRenderer != null && pulseRenderer.sharedMaterial != null)
        {
            Material m = pulseRenderer.sharedMaterial;
            cachedEmission = m.HasProperty(EmissionColorId) ? m.GetColor(EmissionColorId) : Color.black;
        }
    }

    public void Play(bool success)
    {
        if (success)
        {
            if (successEffect != null)
            {
                successEffect.Play();
            }
            return;
        }

        if (failEffect != null)
        {
            failEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            failEffect.Play();
        }

        if (pulseRenderer != null)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }
            flashRoutine = StartCoroutine(PulseFailEmission());
        }
    }

    private IEnumerator PulseFailEmission()
    {
        if (mpb == null)
        {
            mpb = new MaterialPropertyBlock();
        }

        pulseRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(EmissionColorId, failFlashColor * failFlashEmissionMul);
        pulseRenderer.SetPropertyBlock(mpb);
        yield return new WaitForSecondsRealtime(failFlashDuration);
        pulseRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(EmissionColorId, cachedEmission);
        pulseRenderer.SetPropertyBlock(mpb);
    }
}
