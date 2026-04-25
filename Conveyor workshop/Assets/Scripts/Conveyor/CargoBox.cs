using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CargoBox : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CargoType cargoType = CargoType.Red;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private Renderer colorRenderer;
    [SerializeField] private Color redColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color blueColor = new Color(0.2f, 0.4f, 0.9f);
    [SerializeField] private Color greenColor = new Color(0.2f, 0.8f, 0.35f);

    public CargoType Type => cargoType;
    public int ScoreValue => scoreValue;
    private MaterialPropertyBlock propertyBlock;
    private Rigidbody rb;
    [SerializeField] private float forwardSpeed = 1.45f;
    [SerializeField] private float laneChangeDuration = 0.28f;
    [SerializeField] private float maxTiltAngle = 10f;
    [SerializeField] private float rollAngle = 7f;
    [SerializeField] private float turnYawAngle = 11f;
    [SerializeField] private float centerJitterAngle = 4f;
    [SerializeField] private float centerYawShake = 3f;
    private bool laneChanging;
    private float laneChangeElapsed;
    private float laneStartX;
    private float laneTargetX;
    private GameManager GM => gameManager != null ? gameManager : GameManager.Instance;

    private void OnValidate()
    {
        ApplyDebugColor();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Box should glide along conveyor instead of tumbling.
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }

        ApplyDebugColor();
    }

    private void FixedUpdate()
    {
        if (rb == null || GM == null || !GM.IsGameRunning)
        {
            return;
        }

        float dz = forwardSpeed * GM.ConveyorSpeedMultiplier * Time.fixedDeltaTime;
        Vector3 next = rb.position + new Vector3(0f, 0f, dz);

        if (laneChanging)
        {
            laneChangeElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(laneChangeElapsed / Mathf.Max(0.05f, laneChangeDuration));
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            float laneDelta = laneTargetX - laneStartX;
            next.x = Mathf.Lerp(laneStartX, laneTargetX, smooth);

            Quaternion targetRotation;
            if (Mathf.Abs(laneDelta) < 0.08f)
            {
                // Subtle "bump" animation even when staying on the middle lane.
                float wobble = Mathf.Sin(t * Mathf.PI * 2.4f) * (1f - t) * centerJitterAngle;
                float yawShake = Mathf.Sin(t * Mathf.PI) * centerYawShake;
                targetRotation = Quaternion.Euler(0f, yawShake, wobble);
            }
            else
            {
                float dir = Mathf.Sign(laneDelta);
                float bank = Mathf.Sin(t * Mathf.PI) * maxTiltAngle * -dir;
                float yaw = Mathf.Sin(t * Mathf.PI) * turnYawAngle * dir;
                float twist = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * rollAngle * dir;
                targetRotation = Quaternion.Euler(0f, yaw, bank + twist);
            }
            rb.MoveRotation(targetRotation);

            if (t >= 1f)
            {
                laneChanging = false;
                rb.MoveRotation(Quaternion.identity);
            }
        }

        rb.MovePosition(next);
    }

    public void StartLaneChange(float targetX)
    {
        if (rb == null)
        {
            return;
        }

        laneChanging = true;
        laneChangeElapsed = 0f;
        laneStartX = rb.position.x;
        laneTargetX = targetX;
    }

    public void SetCargoType(CargoType newType)
    {
        cargoType = newType;
        ApplyDebugColor();
    }

    private void ApplyDebugColor()
    {
        if (colorRenderer == null)
        {
            return;
        }

        Color color = redColor;
        switch (cargoType)
        {
            case CargoType.Blue:
                color = blueColor;
                break;
            case CargoType.Green:
                color = greenColor;
                break;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        colorRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color); // URP Lit
        propertyBlock.SetColor("_Color", color);     // Standard fallback
        colorRenderer.SetPropertyBlock(propertyBlock);
    }
}
