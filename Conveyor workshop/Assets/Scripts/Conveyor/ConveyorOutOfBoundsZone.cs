using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConveyorOutOfBoundsZone : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private bool countAsDefect = true;
    private GameManager GM => gameManager != null ? gameManager : GameManager.Instance;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        CargoBox cargo = other.GetComponentInParent<CargoBox>();
        if (cargo == null)
        {
            return;
        }

        if (countAsDefect && GM != null)
        {
            GM.RegisterLostCargo();
        }

        Destroy(cargo.gameObject);
    }
}
