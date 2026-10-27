using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SpawnUtility;

public class LootGenerator : MonoBehaviour
{
    private static Dictionary<Vector3Int, DungeonRoom> dungeonRooms = new();
    public float lootSpawnChance;
    public int maxLootInDungeon;
    public int minLootInDungeon;

    public static int totalLootCount;

    private Dictionary<DungeonRoom, List<GameObject>> loot = new();

    [SerializeField]
    private GameObject lootPrefab;

    public IEnumerator GenerateLoot()
    {
        yield return null;
        dungeonRooms = DungeonGenerator.rooms;

        // bing bang boom generate loot for each dungeon room
        foreach (var room in dungeonRooms.Values)
        {
            GenerateLootForRoom(room);
        }
        while (totalLootCount < minLootInDungeon)
        {
            // pick random rooms to add remaining loot
            var randomRoom = DungeonGenerator.GetRandomRoom();
            GenerateLootForRoom(randomRoom);
        }
    }

    private void GenerateLootForRoom(DungeonRoom room)
    {
        if (
            Random.value < lootSpawnChance
            && totalLootCount < maxLootInDungeon
            && !room.HasFeature(RoomFeatures.NoLoot)
        )
        {
            if (!loot.ContainsKey(room))
            {
                loot[room] = new List<GameObject>();
            }

            GameObject newLoot = Instantiate(lootPrefab);

            // Generate random offset for loot placement within the room
            Vector3 randomOffset = new Vector3(Random.Range(-4f, 4f), 0, Random.Range(-4f, 4f));
            if (
                !TryGetRandomNavMeshPosition(
                    room.GridPosition + randomOffset,
                    4f,
                    out Vector3 spawnPosition
                )
            )
            {
                return;
            }

            newLoot.transform.position = spawnPosition;
            newLoot.name = "Loot" + totalLootCount;
            loot[room].Add(newLoot);

            totalLootCount++;
        }
    }
}
