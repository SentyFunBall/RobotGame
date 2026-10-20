using System.Collections.Generic;
using UnityEngine;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dungeonRooms = DungeonGenerator.rooms;

        // bing bang boom generate loot for each dungeon room
        foreach (var room in dungeonRooms.Values)
        {
            GenerateLootForRoom(room);
        }
        while (totalLootCount < minLootInDungeon)
        {
            // pick random rooms to add remaining loot
            var randomRoom = new List<DungeonRoom>(dungeonRooms.Values)[
                Random.Range(0, dungeonRooms.Count)
            ];
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

            newLoot.transform.position = new Vector3(
                room.GridPosition.x + randomOffset.x,
                1,
                room.GridPosition.z + randomOffset.z
            );
            newLoot.name = "Loot" + totalLootCount;
            loot[room].Add(newLoot);

            totalLootCount++;
        }
    }
}
