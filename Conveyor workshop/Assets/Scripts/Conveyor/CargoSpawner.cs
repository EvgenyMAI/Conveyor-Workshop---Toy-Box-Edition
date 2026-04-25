using UnityEngine;

public class CargoSpawner : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CargoBox[] cargoPrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float baseSpawnInterval = 2f;
    [SerializeField] private float minSpawnInterval = 0.4f;
    [SerializeField] private Vector3 spawnArea = new Vector3(0.25f, 0f, 0.1f);
    [SerializeField] private int initialBurstCount = 1;
    [SerializeField] private Vector3 overlapCheckExtents = new Vector3(0.45f, 0.4f, 0.45f);
    [Header("Spawn safety")]
    [Tooltip("Минимальный боковой зазор между коробками у спавна.")]
    [SerializeField] private float minSpawnGapX = 1.1f;
    [Tooltip("Минимальный продольный зазор между коробками у спавна.")]
    [SerializeField] private float minSpawnGapZ = 1.45f;
    [SerializeField] private bool useRandomSpawnLane = true;
    [SerializeField] private float[] spawnLaneX = { -3.5f, 0f, 3.5f };
    [Tooltip("Смещение влево/вправо от центра полосы (коробка не по центру дорожки).")]
    [SerializeField] private float spawnLaneLateralMin = 0.2f;
    [SerializeField] private float spawnLaneLateralMax = 0.42f;

    private float timer;
    private GameManager GM => gameManager != null ? gameManager : GameManager.Instance;

    private void Start()
    {
        for (int i = 0; i < initialBurstCount; i++)
        {
            TrySpawnCargo();
        }
    }

    private void Update()
    {
        if (GM == null || !GM.IsGameRunning)
        {
            return;
        }

        timer += Time.deltaTime;
        float interval = CurrentSpawnInterval();
        if (timer >= interval)
        {
            timer = 0f;
            TrySpawnCargo();
        }
    }

    private float CurrentSpawnInterval()
    {
        float multiplier = GM != null ? GM.SpawnRateMultiplier : 1f;
        float dynamicInterval = baseSpawnInterval / Mathf.Max(0.01f, multiplier);
        return Mathf.Max(minSpawnInterval, dynamicInterval);
    }

    private void TrySpawnCargo()
    {
        if (cargoPrefabs == null || cargoPrefabs.Length == 0)
        {
            return;
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;
        float baseX = point.position.x;
        if (useRandomSpawnLane && spawnLaneX != null && spawnLaneX.Length > 0)
        {
            baseX = spawnLaneX[Random.Range(0, spawnLaneX.Length)];
            float sign = Random.value < 0.5f ? -1f : 1f;
            float mag = Random.Range(spawnLaneLateralMin, spawnLaneLateralMax);
            baseX += sign * mag;
        }

        Vector3 spawnPosition = new Vector3(
            baseX + Random.Range(-spawnArea.x, spawnArea.x),
            point.position.y + Random.Range(-spawnArea.y, spawnArea.y),
            point.position.z + Random.Range(-spawnArea.z, spawnArea.z));
        if (!IsSpawnAreaClear(spawnPosition))
        {
            return;
        }

        CargoBox prefab = cargoPrefabs[Random.Range(0, cargoPrefabs.Length)];
        Instantiate(prefab, spawnPosition, point.rotation);
    }

    private bool IsSpawnAreaClear(Vector3 spawnPosition)
    {
        Vector3 safetyExtents = new Vector3(
            Mathf.Max(overlapCheckExtents.x, minSpawnGapX * 0.5f),
            overlapCheckExtents.y,
            Mathf.Max(overlapCheckExtents.z, minSpawnGapZ * 0.5f));

        Collider[] overlaps = Physics.OverlapBox(
            spawnPosition,
            safetyExtents,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            CargoBox existing = overlaps[i].GetComponentInParent<CargoBox>();
            if (existing == null)
            {
                continue;
            }

            Vector3 delta = existing.transform.position - spawnPosition;
            if (Mathf.Abs(delta.x) <= minSpawnGapX && Mathf.Abs(delta.z) <= minSpawnGapZ)
            {
                return false;
            }
        }

        return true;
    }
}
