using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LaneRouter : MonoBehaviour
{
    [SerializeField] private DiverterSwitch diverter;
    [SerializeField] private float[] laneXPositions = { -3.5f, 0f, 3.5f };

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        CargoBox cargo = other.GetComponentInParent<CargoBox>();
        if (cargo == null || diverter == null)
        {
            return;
        }

        Rigidbody rb = cargo.GetComponent<Rigidbody>();
        if (rb == null)
        {
            return;
        }

        int lane = Mathf.Clamp(diverter.CurrentLane, 0, laneXPositions.Length - 1);
        float targetX = laneXPositions[lane];
        cargo.StartLaneChange(targetX);
        rb.linearVelocity = Vector3.zero;
    }
}
