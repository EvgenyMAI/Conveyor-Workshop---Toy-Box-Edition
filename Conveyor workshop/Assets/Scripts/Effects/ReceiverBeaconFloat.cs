using UnityEngine;

public class ReceiverBeaconFloat : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 2.4f;
    [SerializeField] private float rotateSpeed = 55f;
    [SerializeField] private bool reactToGameSpeed = true;
    private Vector3 startLocalPos;
    private GameManager GM => gameManager != null ? gameManager : GameManager.Instance;

    private void Awake()
    {
        startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        float speedMul = 1f;
        if (reactToGameSpeed && GM != null)
        {
            speedMul = Mathf.Clamp(GM.ConveyorSpeedMultiplier, 0.7f, 2.6f);
        }

        float y = Mathf.Sin(Time.time * bobSpeed * speedMul) * bobAmplitude;
        transform.localPosition = startLocalPos + new Vector3(0f, y, 0f);
        transform.Rotate(Vector3.up, rotateSpeed * speedMul * Time.deltaTime, Space.Self);
    }
}
