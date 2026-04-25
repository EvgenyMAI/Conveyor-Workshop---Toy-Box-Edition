using UnityEngine;

public class DiverterSwitch : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [Header("Lane selection")]
    [SerializeField] private KeyCode lane1Key = KeyCode.Alpha1;
    [SerializeField] private KeyCode lane2Key = KeyCode.Alpha2;
    [SerializeField] private KeyCode lane3Key = KeyCode.Alpha3;
    [SerializeField] private KeyCode previousLaneKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode nextLaneKey = KeyCode.RightArrow;
    [Range(0, 2)]
    [SerializeField] private int startLaneIndex = 1;
    [SerializeField] private GameObject[] laneBlockers = new GameObject[3];

    private int currentLane;
    public int CurrentLane => currentLane;

    private GameManager GM => gameManager != null ? gameManager : GameManager.Instance;

    private void Start()
    {
        currentLane = Mathf.Clamp(startLaneIndex, 0, 2);
        ApplyLaneState();
    }

    private void Update()
    {
        if (GM != null && !GM.IsGameRunning)
        {
            return;
        }

        if (InputCompat.IsLaneKeyDown(0, lane1Key))
        {
            SetLane(0);
            return;
        }

        if (InputCompat.IsLaneKeyDown(1, lane2Key))
        {
            SetLane(1);
            return;
        }

        if (InputCompat.IsLaneKeyDown(2, lane3Key))
        {
            SetLane(2);
            return;
        }

        if (InputCompat.IsArrowStepDown(true, previousLaneKey))
        {
            SetLane((currentLane + 2) % 3);
            return;
        }

        if (InputCompat.IsArrowStepDown(false, nextLaneKey))
        {
            SetLane((currentLane + 1) % 3);
        }
    }

    public void SetLane(int laneIndex)
    {
        currentLane = Mathf.Clamp(laneIndex, 0, 2);
        ApplyLaneState();
    }

    private void ApplyLaneState()
    {
        for (int i = 0; i < laneBlockers.Length; i++)
        {
            if (laneBlockers[i] != null)
            {
                // active blocker closes lane; selected lane is open.
                laneBlockers[i].SetActive(i != currentLane);
            }
        }
    }
}
