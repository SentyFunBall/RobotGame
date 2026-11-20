using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using static SpawnUtility;

[Serializable]
public struct EnemySpawnInfo
{
    [SerializeField]
    public GameObject prefab;
    [SerializeField]
    public float weight;
}

public class GenerateNavMesh : MonoBehaviour
{
    [SerializeField]
    private List<EnemySpawnInfo> enemyInfos = new();
    private List<GameObject> spawnedEnemies = new();

    private static GenerateNavMesh instance;

    private void Awake()
    {
        instance = this;
    }

    public static void Reset()
    {
        foreach (GameObject enemy in instance.spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        instance.spawnedEnemies.Clear();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public IEnumerator Bake()
    {
        yield return null;
    }
    
    public IEnumerator SpawnEnemies()
    {
        yield return null;

        int numEnemies = DungeonGenerator.Instance.Difficulty.numberOfEnemies;

        for (int i = 0; i < numEnemies; i++)
        {
            DungeonRoom room = DungeonGenerator.GetRandomNonEndRoom();
            if (room == null)
                break;

            Vector3 roomCenter = room.GridPosition;
            if (
                TryGetRandomNavMeshPosition(roomCenter, 8f, out Vector3 spawnPosition)
                && !DungeonGenerator.IsInEndRoom(spawnPosition)
            )
            {
                // TODO: Will need to restrict enemies to certain levels/rooms eventually
                GameObject enemyPrefab = PickRandomEnemy();
                if (enemyPrefab == null)
                {
                    UnityEngine.Debug.LogWarning("failed to spawn in enemy, due to no enemy infos or failed pick");
                    continue;
                }

                GameObject enemy = Instantiate(
                    enemyPrefab,
                    spawnPosition,
                    Quaternion.identity
                );
                instance.spawnedEnemies.Add(enemy);
            }
        }
    }

    private GameObject PickRandomEnemy()
    {
        float max = 0;
        GameObject selected = null;
        foreach(EnemySpawnInfo enemySpawn in enemyInfos)
        {
            float weightedRand = UnityEngine.Random.Range(0f, 1f) * enemySpawn.weight;
            if (weightedRand > max)
            {
                max = weightedRand;
                selected = enemySpawn.prefab;
            }
        }

        return selected;
    }
}
