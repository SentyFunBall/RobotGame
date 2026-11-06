using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using static SpawnUtility;

public class GenerateNavMesh : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> enemyPrefabs = new();

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
                Instantiate(
                    enemyPrefabs[Random.Range(0, enemyPrefabs.Count)],
                    spawnPosition,
                    Quaternion.identity
                );
            }
        }
    }
}
