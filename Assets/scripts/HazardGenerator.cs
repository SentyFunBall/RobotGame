using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SpawnUtility;

public class HazardGenerator : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> hazardPrefabs = new();

    [SerializeField]
    [Min(1)]
    private int maxHazardsPerRoom = 2;

    [SerializeField]
    private float spawnOffsetRadius = 4f;

    [SerializeField]
    private float minHazardSpacing = 2.5f;

    [SerializeField]
    [Tooltip("Multiplier applied to Difficulty.TrapFrequency in rooms marked as RoomType.Trap.")]
    private float trapRoomChanceMultiplier = 2f;

    [SerializeField]
    private float yOffset = 0f;

    private readonly List<GameObject> spawnedHazards = new();

    public void Reset()
    {
        foreach (GameObject hazard in spawnedHazards)
        {
            if (hazard != null)
                Destroy(hazard);
        }
        spawnedHazards.Clear();
    }

    public IEnumerator GenerateHazards()
    {
        yield return null;

        if (DungeonGenerator.Instance == null || hazardPrefabs == null || hazardPrefabs.Count == 0)
            yield break;

        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (GameObject prefab in hazardPrefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count == 0)
            yield break;

        float baseChance = Mathf.Clamp01(DungeonGenerator.Instance.Difficulty.TrapFrequency / 100f);

        foreach (DungeonRoom room in DungeonGenerator.rooms.Values)
        {
            if (
                room == null
                || room == DungeonGenerator.StartRoom
                || room == DungeonGenerator.EndRoom
            )
                continue;

            float roomChance = room.IsType(RoomType.Trap)
                ? Mathf.Clamp01(baseChance * trapRoomChanceMultiplier)
                : baseChance;

            for (int i = 0; i < maxHazardsPerRoom; i++)
            {
                if (Random.value >= roomChance)
                    continue;

                Vector3 randomOffset = new Vector3(
                    Random.Range(-spawnOffsetRadius, spawnOffsetRadius),
                    0f,
                    Random.Range(-spawnOffsetRadius, spawnOffsetRadius)
                );

                if (
                    !TryGetRandomNavMeshPosition(
                        room.GridPosition + randomOffset,
                        spawnOffsetRadius,
                        out Vector3 spawnPosition
                    )
                )
                    continue;

                if (DungeonGenerator.IsInEndRoom(spawnPosition) || IsTooCloseToExistingHazard(spawnPosition))
                    continue;

                GameObject prefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
                GameObject hazard = Instantiate(
                    prefab,
                    spawnPosition + Vector3.up * yOffset,
                    Quaternion.identity
                );
                hazard.name = "Hazard" + spawnedHazards.Count;
                spawnedHazards.Add(hazard);
            }
        }
    }

    private bool IsTooCloseToExistingHazard(Vector3 candidate)
    {
        float minSqrDist = minHazardSpacing * minHazardSpacing;
        foreach (GameObject existing in spawnedHazards)
        {
            if (existing == null)
                continue;
            if ((existing.transform.position - candidate).sqrMagnitude < minSqrDist)
                return true;
        }
        return false;
    }
}