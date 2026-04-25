using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ReceiverZone : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private List<CargoType> acceptedTypes = new List<CargoType> { CargoType.Red };
    [SerializeField] private ReceiverFeedback feedback;
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

        bool isCorrect = acceptedTypes.Contains(cargo.Type);
        if (GM != null)
        {
            GM.HandleCargoDelivered(isCorrect, cargo.ScoreValue);
        }

        if (feedback != null)
        {
            feedback.Play(isCorrect);
        }

        Destroy(cargo.gameObject);
    }
}
