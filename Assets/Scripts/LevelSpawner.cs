using UnityEngine;
using System.Collections.Generic;

public class LevelSpawner : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] GameObject startingLevelPrefab;
    [SerializeField] GameObject[] levelPrefabs;
    [SerializeField] float spawnAheadDistance = 90f;
    [SerializeField] float despawnBehindDistance = 45f;
    [SerializeField] float levelGap = 0f;
    [SerializeField] float fallbackSectionLength = 32f;

    readonly Queue<SpawnedSection> spawnedSections = new Queue<SpawnedSection>();
    float nextSpawnStartZ;

    public int SectionsPassed { get; private set; }

    void Start()
    {
        FindPlayer();
        SpawnStartingLayout();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        SpawnUntilAhead();
        RemoveSectionsBehindPlayer();
    }

    void SpawnStartingLayout()
    {
        spawnedSections.Clear();
        SectionsPassed = 0;
        nextSpawnStartZ = 0f;

        GameObject firstPrefab = startingLevelPrefab != null ? startingLevelPrefab : PickRandomLevelPrefab();
        if (firstPrefab == null)
        {
            Debug.LogWarning("LevelSpawner needs at least one level prefab assigned.");
            return;
        }

        SpawnSection(firstPrefab, false);
        SpawnUntilAhead();
    }

    void SpawnUntilAhead()
    {
        float targetZ = player.position.z + spawnAheadDistance;
        int safety = 0;

        while (nextSpawnStartZ < targetZ && safety < 20)
        {
            GameObject prefab = PickRandomLevelPrefab();
            if (prefab == null || !SpawnSection(prefab, true))
                break;

            safety++;
        }
    }

    bool SpawnSection(GameObject prefab, bool alignStartToNextSpawn)
    {
        GameObject section = Instantiate(prefab, new Vector3(0f, 0f, nextSpawnStartZ), Quaternion.identity, transform);

        if (!TryGetSectionBounds(section, out Bounds bounds))
        {
            bounds = new Bounds(section.transform.position + Vector3.forward * (fallbackSectionLength * 0.5f), new Vector3(10f, 1f, fallbackSectionLength));
        }

        if (alignStartToNextSpawn)
        {
            float zOffset = nextSpawnStartZ - bounds.min.z;
            section.transform.position += Vector3.forward * zOffset;
            TryGetSectionBounds(section, out bounds);
        }

        float sectionLength = Mathf.Max(bounds.size.z, fallbackSectionLength);
        float sectionEndZ = Mathf.Max(bounds.max.z, bounds.min.z + sectionLength);

        spawnedSections.Enqueue(new SpawnedSection(section, bounds.min.z, sectionEndZ));
        nextSpawnStartZ = sectionEndZ + levelGap;

        return true;
    }

    GameObject PickRandomLevelPrefab()
    {
        if (levelPrefabs == null || levelPrefabs.Length == 0)
            return startingLevelPrefab;

        int startIndex = Random.Range(0, levelPrefabs.Length);
        for (int i = 0; i < levelPrefabs.Length; i++)
        {
            GameObject prefab = levelPrefabs[(startIndex + i) % levelPrefabs.Length];
            if (prefab != null)
                return prefab;
        }

        return startingLevelPrefab;
    }

    void RemoveSectionsBehindPlayer()
    {
        float despawnZ = player.position.z - despawnBehindDistance;

        while (spawnedSections.Count > 0 && spawnedSections.Peek().EndZ < despawnZ)
        {
            SpawnedSection oldSection = spawnedSections.Dequeue();
            if (oldSection.Instance != null)
                Destroy(oldSection.Instance);

            SectionsPassed++;
        }
    }

    bool TryGetSectionBounds(GameObject section, out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = new Bounds(section.transform.position, Vector3.zero);

        Renderer[] renderers = section.GetComponentsInChildren<Renderer>();
        foreach (Renderer sectionRenderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = sectionRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sectionRenderer.bounds);
            }
        }

        Collider[] colliders = section.GetComponentsInChildren<Collider>();
        foreach (Collider sectionCollider in colliders)
        {
            if (!hasBounds)
            {
                bounds = sectionCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sectionCollider.bounds);
            }
        }

        return hasBounds;
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    struct SpawnedSection
    {
        public readonly GameObject Instance;
        public readonly float StartZ;
        public readonly float EndZ;

        public SpawnedSection(GameObject instance, float startZ, float endZ)
        {
            Instance = instance;
            StartZ = startZ;
            EndZ = endZ;
        }
    }
}
