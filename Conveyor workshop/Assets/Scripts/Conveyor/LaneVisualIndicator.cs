using UnityEngine;

public class LaneVisualIndicator : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private DiverterSwitch diverter;
    [SerializeField] private Transform[] laneRoots = new Transform[3];
    [SerializeField] private Renderer[] laneDiscs = new Renderer[3];
    [SerializeField] private Renderer[] laneHalos = new Renderer[3];
    [SerializeField] private Color inactiveColor = new Color(0.82f, 0.74f, 0.62f);
    [SerializeField] private Color activeDiscColor = new Color(1f, 0.78f, 0.22f);
    [SerializeField] private Color inactiveHaloColor = new Color(0.7f, 0.62f, 0.5f);
    [SerializeField] private Color activeHaloColor = new Color(1f, 0.92f, 0.45f);
    [SerializeField] private float inactiveEmission = 0.04f;
    [SerializeField] private float activeDiscEmission = 0.38f;
    [SerializeField] private float activeHaloEmission = 0.62f;
    [SerializeField] private float pulseSpeed = 6.8f;
    [SerializeField] private float pulseScale = 0.07f;
    [SerializeField] private float emissionPulse = 0.14f;

    private MaterialPropertyBlock block;

    private void Update()
    {
        if (diverter == null || laneDiscs == null)
        {
            return;
        }

        int activeLane = diverter.CurrentLane;
        float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed);

        for (int i = 0; i < laneDiscs.Length; i++)
        {
            bool on = i == activeLane;
            float scaleMul = on ? 1f + pulse * pulseScale : 1f;
            if (laneRoots != null && i < laneRoots.Length && laneRoots[i] != null)
            {
                laneRoots[i].localScale = new Vector3(scaleMul, 1f, scaleMul);
            }

            float discEm = on ? activeDiscEmission + pulse * emissionPulse : inactiveEmission;
            float haloEm = on ? activeHaloEmission + pulse * emissionPulse * 1.2f : inactiveEmission * 0.35f;

            ApplyToRenderer(laneDiscs[i], on ? activeDiscColor : inactiveColor, discEm);
            if (laneHalos != null && i < laneHalos.Length && laneHalos[i] != null)
            {
                ApplyToRenderer(laneHalos[i], on ? activeHaloColor : inactiveHaloColor, haloEm);
            }
        }
    }

    private void ApplyToRenderer(Renderer r, Color c, float emissionMul)
    {
        if (r == null)
        {
            return;
        }

        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }

        r.GetPropertyBlock(block);
        block.SetColor("_BaseColor", c);
        block.SetColor("_Color", c);
        block.SetColor(EmissionColorId, c * emissionMul);
        r.SetPropertyBlock(block);
    }
}
