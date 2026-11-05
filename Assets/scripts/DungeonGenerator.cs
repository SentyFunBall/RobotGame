using System.Collections.Generic;
using System.Diagnostics;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class DungeonDifficulty
{
    [Range(0, 100)]
    public int EnemyStrength;

    [Range(0, 100)]
    public int PuzzleComplexity;

    [Range(0, 100)]
    public int TrapFrequency;

    [Range(0, 100)]
    public int Size = 10; // Default size of the dungeon (N x N)

    [Range(0, 250)]
    public int RoomCount = 50; // Default number of rooms in the dungeon

    [Range(0, 100)]
    public int numberOfEnemies;
}

public struct OpenConnection
{
    public Vector3Int Position;
    public DungeonRoom SourceRoom;
}

// This class is responsible for generating the dungeon layout and managing its components.
public class DungeonGenerator : MonoBehaviour
{
    public static DungeonGenerator Instance { get; private set; }

    public DungeonDifficulty Difficulty = new DungeonDifficulty();
    public static Dictionary<Vector3Int, DungeonRoom> rooms = new();
    public static List<OpenConnection> openConnections = new List<OpenConnection>();

    private static DungeonRoom startRoom;
    private static DungeonRoom endRoom;
    private static GameObject player;
    private bool hasWon;

    [SerializeField]
    private GameObject navMesh;

    [SerializeField]
    private List<GameObject> allRooms = new List<GameObject>();

    [Tooltip(
        "Weight that determines how much the dungeon spreads out. Higher values result in more spread."
    )]
    [SerializeField]
    [Range(0.1f, 15.0f)]
    private float SpreadWeight = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private DungeonRoom CreateRoom(GameObject prefab, int rotation, Vector3Int position)
    {
        if (IsPositionOccupied(position))
            return null;

        GameObject instance = Instantiate(
            prefab,
            new Vector3(position.x, 0, position.z),
            Quaternion.Euler(0, 90f * rotation, 0),
            transform
        );
        DungeonRoom room = instance.GetComponent<DungeonRoom>();
        room.GridPosition = position;
        room.Rotation = rotation;

        // fill in neighbors
        if (
            rooms.TryGetValue(position + Vector3Int.forward * Difficulty.Size, out room.North)
            && room.North != null
        )
            room.North.South = room;
        if (
            rooms.TryGetValue(position + Vector3Int.back * Difficulty.Size, out room.South)
            && room.South != null
        )
            room.South.North = room;
        if (
            rooms.TryGetValue(position + Vector3Int.right * Difficulty.Size, out room.East)
            && room.East != null
        )
            room.East.West = room;
        if (
            rooms.TryGetValue(position + Vector3Int.left * Difficulty.Size, out room.West)
            && room.West != null
        )
            room.West.East = room;

        room.UpdateWalls();
        if (room.North != null)
            room.North.UpdateWalls();
        if (room.South != null)
            room.South.UpdateWalls();
        if (room.East != null)
            room.East.UpdateWalls();
        if (room.West != null)
            room.West.UpdateWalls();

        return room;
    }

    private Vector3Int PickOpenConnection(List<OpenConnection> openConnections)
    {
        float totalWeight = 0f;
        foreach (OpenConnection connection in openConnections)
        {
            DungeonRoom room;
            rooms.TryGetValue(connection.Position, out room);
            totalWeight += Weight(connection.SourceRoom);
        }

        float roll = Random.Range(0f, totalWeight);

        foreach (OpenConnection connection in openConnections)
        {
            DungeonRoom room;
            rooms.TryGetValue(connection.Position, out room);
            roll -= Weight(connection.SourceRoom);
            if (roll <= 0f)
                return connection.Position;
        }

        // Fallback in case of rounding errors
        return openConnections[^1].Position;
    }

    private struct RoomPlacement
    {
        public GameObject Prefab;
        public int Rotation;
    }

    // Picks a room definition (and rotation) that satisfies whatever connections are
    // required by already-placed neighbors around the given position.
    private RoomPlacement? PickCompatibleRoom(
        Vector3Int position,
        RoomType requiredType = RoomType.None
    )
    {
        RoomConnections required = RoomConnections.None;
        RoomConnections forbidden = RoomConnections.None;

        CollectDirectionConstraints(
            position + Vector3Int.forward * Difficulty.Size,
            RoomConnections.North,
            ref required,
            ref forbidden
        );
        CollectDirectionConstraints(
            position + Vector3Int.back * Difficulty.Size,
            RoomConnections.South,
            ref required,
            ref forbidden
        );
        CollectDirectionConstraints(
            position + Vector3Int.right * Difficulty.Size,
            RoomConnections.East,
            ref required,
            ref forbidden
        );
        CollectDirectionConstraints(
            position + Vector3Int.left * Difficulty.Size,
            RoomConnections.West,
            ref required,
            ref forbidden
        );

        List<RoomPlacement> candidates = new List<RoomPlacement>();
        foreach (GameObject prefabObject in allRooms)
        {
            if (prefabObject == null)
                continue;

            RoomDefinition definition = prefabObject.GetComponent<DungeonRoom>().RoomDefinition;
            if (definition == null)
                continue;
            if (requiredType != RoomType.None && (definition.Type & requiredType) != requiredType)
                continue;

            for (int rotation = 0; rotation < 4; rotation++)
            {
                RoomConnections rotated = RoomDefinition.RotateConnections(
                    definition.Connections,
                    rotation,
                    definition
                );
                if ((rotated & required) != required)
                    continue;
                if ((rotated & forbidden) != 0)
                    continue;

                candidates.Add(new RoomPlacement { Prefab = prefabObject, Rotation = rotation });
            }
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    // Determines whether the given direction from the room being placed is required
    // (a neighbor already exists and can connect back) or forbidden (a neighbor exists
    // but has no matching connection).
    private void CollectDirectionConstraints(
        Vector3Int neighborPosition,
        RoomConnections direction,
        ref RoomConnections required,
        ref RoomConnections forbidden
    )
    {
        if (!rooms.TryGetValue(neighborPosition, out DungeonRoom neighbor) || neighbor == null)
            return;

        if (neighbor.CanConnect(RoomDefinition.Opposite(direction)))
            required |= direction;
        else
            forbidden |= direction;
    }

    private bool IsPositionOccupied(Vector3Int position)
    {
        return rooms.ContainsKey(position);
    }

    private void AddOpenConnections(DungeonRoom room)
    {
        // Only frontier in directions where the room actually has a door - an empty
        // neighboring tile isn't a valid expansion point if there's no connection to it.
        if (
            room.North == null
            && room.CanConnect(RoomConnections.North)
            && !IsPositionOccupied(room.GridPosition + Vector3Int.forward * Difficulty.Size)
        )
            openConnections.Add(
                new OpenConnection
                {
                    Position = room.GridPosition + Vector3Int.forward * Difficulty.Size,
                    SourceRoom = room,
                }
            );
        if (
            room.South == null
            && room.CanConnect(RoomConnections.South)
            && !IsPositionOccupied(room.GridPosition + Vector3Int.back * Difficulty.Size)
        )
            openConnections.Add(
                new OpenConnection
                {
                    Position = room.GridPosition + Vector3Int.back * Difficulty.Size,
                    SourceRoom = room,
                }
            );
        if (
            room.East == null
            && room.CanConnect(RoomConnections.East)
            && !IsPositionOccupied(room.GridPosition + Vector3Int.right * Difficulty.Size)
        )
            openConnections.Add(
                new OpenConnection
                {
                    Position = room.GridPosition + Vector3Int.right * Difficulty.Size,
                    SourceRoom = room,
                }
            );
        if (
            room.West == null
            && room.CanConnect(RoomConnections.West)
            && !IsPositionOccupied(room.GridPosition + Vector3Int.left * Difficulty.Size)
        )
            openConnections.Add(
                new OpenConnection
                {
                    Position = room.GridPosition + Vector3Int.left * Difficulty.Size,
                    SourceRoom = room,
                }
            );
        /*if (room.Up == null)
            openConnections.Add(room.GridPosition + Vector3Int.up * Difficulty.Size);
        if (room.Down == null)
            openConnections.Add(room.GridPosition + Vector3Int.down * Difficulty.Size);*/
    }

    private int GetNeighborCount(DungeonRoom room)
    {
        int count = 0;
        if (IsPositionOccupied(room.GridPosition + Vector3Int.forward * Difficulty.Size))
            count++;
        if (IsPositionOccupied(room.GridPosition + Vector3Int.back * Difficulty.Size))
            count++;
        if (IsPositionOccupied(room.GridPosition + Vector3Int.right * Difficulty.Size))
            count++;
        if (IsPositionOccupied(room.GridPosition + Vector3Int.left * Difficulty.Size))
            count++;
        /*if (IsPositionOccupied(room.GridPosition + Vector3Int.up * Difficulty.Size))
            count++;
        if (IsPositionOccupied(room.GridPosition + Vector3Int.down * Difficulty.Size))
            count++;*/
        return count;
    }

    private float Weight(DungeonRoom room)
    {
        int neighbors = GetNeighborCount(room);
        float baseWeight = Mathf.Max(0.01f, 4f - neighbors);
        return Mathf.Pow(baseWeight, SpreadWeight);
    }

    private DungeonRoom GenerateLayout()
    {
        // Build the connected topology without requiring any special room type yet.
        openConnections.Clear();
        rooms.Clear();
        RoomPlacement? startPlacement = PickCompatibleRoom(Vector3Int.zero);
        if (startPlacement == null)
        {
            UnityEngine.Debug.LogError("No compatible room found for the layout start.");
            return null;
        }

        DungeonRoom startingRoom = CreateRoom(
            startPlacement.Value.Prefab,
            startPlacement.Value.Rotation,
            Vector3Int.zero
        );
        rooms.Add(startingRoom.GridPosition, startingRoom);
        AddOpenConnections(rooms[startingRoom.GridPosition]);

        while (rooms.Count < Difficulty.RoomCount && openConnections.Count > 0)
        {
            Vector3Int openConnection = PickOpenConnection(openConnections);
            RoomPlacement? placement = PickCompatibleRoom(openConnection);

            if (placement == null)
            {
                openConnections.RemoveAll(oc => oc.Position == openConnection);
                continue;
            }

            DungeonRoom newRoom = CreateRoom(
                placement.Value.Prefab,
                placement.Value.Rotation,
                openConnection
            );

            rooms.Add(newRoom.GridPosition, newRoom);
            openConnections.RemoveAll(oc => oc.Position == openConnection);
            AddOpenConnections(newRoom);
        }

        return startingRoom;
    }

    private bool PopulateRooms(Vector3Int startPosition, Vector3Int endPosition)
    {
        Dictionary<Vector3Int, RoomPlacement> placements = new();
        List<Vector3Int> positions = new(rooms.Keys);

        foreach (Vector3Int position in positions)
        {
            RoomType requiredType = RoomType.None;
            if (position == startPosition)
                requiredType = RoomType.Start;
            else if (position == endPosition)
                requiredType = RoomType.End;

            RoomPlacement? placement = PickCompatibleRoom(position, requiredType);
            if (placement == null)
            {
                UnityEngine.Debug.LogError(
                    $"No compatible room found at {position} for type {requiredType}."
                );
                return false;
            }

            placements.Add(position, placement.Value);
        }

        foreach (DungeonRoom room in rooms.Values)
            Destroy(room.gameObject);

        rooms.Clear();
        openConnections.Clear();

        foreach (KeyValuePair<Vector3Int, RoomPlacement> placement in placements)
        {
            DungeonRoom room = CreateRoom(
                placement.Value.Prefab,
                placement.Value.Rotation,
                placement.Key
            );
            rooms.Add(placement.Key, room);
        }

        startRoom = rooms[startPosition];
        endRoom = rooms[endPosition];
        startRoom.gameObject.name += " (Start)";
        endRoom.gameObject.name += " (End)";
        return true;
    }

    private void MovePlayerToStart()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (player != null && startRoom != null)
        {
            player.transform.position = new Vector3(
                startRoom.GridPosition.x,
                1,
                startRoom.GridPosition.z
            );
        }
    }

    public void GenerateDungeon()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        startRoom = null;
        endRoom = null;
        hasWon = false;

        DungeonRoom layoutRoot = GenerateLayout();
        if (layoutRoot == null)
            return;

        // Choose endpoints from the completed topology before assigning room types.
        var (layoutStart, layoutEnd, _) = BFS.FindFurthestPair(layoutRoot);
        if (layoutStart == null || layoutEnd == null)
        {
            UnityEngine.Debug.LogError("Could not find furthest rooms for the dungeon.");
            return;
        }

        if (!PopulateRooms(layoutStart.GridPosition, layoutEnd.GridPosition))
        {
            UnityEngine.Debug.LogError("Could not populate the dungeon with compatible rooms.");
            return;
        }

        StartCoroutine(navMesh.GetComponent<GenerateNavMesh>().Bake());
        StartCoroutine(navMesh.GetComponent<GenerateNavMesh>().SpawnEnemies());
        StartCoroutine(GetComponent<LootGenerator>().GenerateLoot());

        MovePlayerToStart();
        stopwatch.Stop();
        UnityEngine.Debug.Log("Dungeon generation time: " + stopwatch.ElapsedMilliseconds + " ms");
    }

    public void ResetDungeon()
    {
        openConnections.Clear();
        rooms.Clear();
        startRoom = null;
        endRoom = null;
    }

    void OnEnable()
    {
        if (rooms.Count == 0)
            GenerateDungeon();
    }

    void Start()
    {
        MovePlayerToStart();
    }

    void Update()
    {
        if (hasWon || endRoom == null)
            return;

        if (player == null)
            player = GameObject.FindWithTag("Player");
        if (player == null)
            return;

        // Rooms are centered on their grid position and spaced Size units apart
        float halfSize = Difficulty.Size * 0.5f;
        Vector3 offset = player.transform.position - endRoom.transform.position;
        if (Mathf.Abs(offset.x) > halfSize || Mathf.Abs(offset.z) > halfSize)
            return;

        hasWon = true;
        GameMenusController menus = FindAnyObjectByType<GameMenusController>();
        if (menus != null)
            menus.ShowWinMenu();
    }

    public static DungeonRoom GetRandomRoom()
    {
        if (rooms.Count == 0)
            return null;

        List<DungeonRoom> roomList = new List<DungeonRoom>(rooms.Values);
        return roomList[Random.Range(0, roomList.Count)];
    }
}
