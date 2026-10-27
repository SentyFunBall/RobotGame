using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using static SpawnUtility;

public class GenerateNavMesh : MonoBehaviour
{
    [SerializeField]
    private NavMeshSurface navMeshSurface;

    [SerializeField]
    private List<GameObject> enemyPrefabs = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public IEnumerator Bake()
    {
        yield return null;
        navMeshSurface.BuildNavMesh();
    }

    public IEnumerator SpawnEnemies()
    {
        yield return null;

        int numEnemies = DungeonGenerator.Instance.Difficulty.numberOfEnemies;

        for (int i = 0; i < numEnemies; i++)
        {
            Vector3 roomCenter = DungeonGenerator.GetRandomRoom()?.GridPosition ?? Vector3.zero;
            if (TryGetRandomNavMeshPosition(roomCenter, 8f, out Vector3 spawnPosition))
            {
                // TODO: Will need to restrict enemies to certain levels/rooms eventually
                Instantiate(
                    enemyPrefabs[Random.Range(0, enemyPrefabs.Count)],
                    spawnPosition,
                    Quaternion.identity
                );
            }
        }
    }
}
