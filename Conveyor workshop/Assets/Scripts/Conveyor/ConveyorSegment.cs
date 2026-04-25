using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConveyorSegment : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform directionReference;
    [SerializeField] private float baseSpeed = 2f;
    [SerializeField] private float lateralDamping = 0.92f;

    private Vector3 MoveDirection => directionReference != null ? directionReference.forward : transform.forward;
    private GameManager GM => gameManager != null ? gameManager : GameManager.Instance;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (GM == null || !GM.IsGameRunning)
        {
            return;
        }

        // CargoBox owns movement in FixedUpdate; do not fight its physics here.
        if (other.GetComponentInParent<CargoBox>() != null)
        {
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null)
        {
            return;
        }

        float speed = baseSpeed * GM.ConveyorSpeedMultiplier;
        Vector3 conveyorVelocity = MoveDirection.normalized * speed;

        // Stable conveyor motion: direct velocity with soft lateral damping.
        Vector3 current = rb.linearVelocity;
        float keepY = 0f;
        rb.linearVelocity = new Vector3(
            Mathf.Lerp(current.x, conveyorVelocity.x, lateralDamping),
            keepY,
            conveyorVelocity.z
        );
    }
}
