using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class DungeonRoom
{
    public RoomDefinition RoomDefinition;
    public Vector3Int GridPosition;

    public DungeonRoom North;
    public DungeonRoom South;
    public DungeonRoom East;
    public DungeonRoom West;
    public DungeonRoom Up;
    public DungeonRoom Down;

    public IEnumerable<DungeonRoom> GetNeighbors()
    {
        if (North != null)
            yield return North;
        if (South != null)
            yield return South;
        if (East != null)
            yield return East;
        if (West != null)
            yield return West;
        if (Up != null)
            yield return Up;
        if (Down != null)
            yield return Down;
    }
}

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

    [SerializeField]
    private DungeonDifficulty Difficulty = new DungeonDifficulty();
    public static Dictionary<Vector3Int, DungeonRoom> rooms = new();
    public static List<OpenConnection> openConnections = new List<OpenConnection>();

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

    private DungeonRoom CreateRoom(RoomDefinition prefab, Vector3Int position)
    {
        if (IsPositionOccupied(position))
            return null;

        DungeonRoom room = new DungeonRoom { GridPosition = position };

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
        if (
            rooms.TryGetValue(position + Vector3Int.up * Difficulty.Size, out room.Up)
            && room.Up != null
        )
            room.Up.Down = room;
        if (
            rooms.TryGetValue(position + Vector3Int.down * Difficulty.Size, out room.Down)
            && room.Down != null
        )
            room.Down.Up = room;

        room.RoomDefinition = prefab;

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

    private RoomDefinition PickCompatibleRoom(Vector3Int openConnection)
    {
        // Implement logic to pick a compatible room based on the open connection
        // For now, just create a room instance with all open connections.
        RoomDefinition roomDef = ScriptableObject.CreateInstance<RoomDefinition>();
        roomDef.Connections =
            RoomConnections.North
            | RoomConnections.South
            | RoomConnections.East
            | RoomConnections.West;
        // | RoomConnections.Up
        // | RoomConnections.Down;
        return roomDef;
    }

    private bool IsPositionOccupied(Vector3Int position)
    {
        return rooms.ContainsKey(position);
    }

    private void AddOpenConnections(DungeonRoom room)
    {
        if (
            room.North == null
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

    public void GenerateDungeon()
    {
        // Step 1: Expanding random walk to generate the layout, biasing expansion to open directions
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        openConnections.Clear();
        rooms.Clear();
        DungeonRoom startingRoom = CreateRoom(PickCompatibleRoom(Vector3Int.zero), Vector3Int.zero);
        rooms.Add(startingRoom.GridPosition, startingRoom);
        AddOpenConnections(rooms[startingRoom.GridPosition]);
        while (rooms.Count < Difficulty.RoomCount && openConnections.Count > 0)
        {
            Vector3Int openConnection = PickOpenConnection(openConnections);
            RoomDefinition prefab = PickCompatibleRoom(openConnection);

            if (prefab == null)
            {
                openConnections.RemoveAll(oc => oc.Position == openConnection);
                continue;
            }

            DungeonRoom newRoom = CreateRoom(prefab, openConnection);

            rooms.Add(newRoom.GridPosition, newRoom);
            openConnections.RemoveAll(oc => oc.Position == openConnection);
            AddOpenConnections(newRoom);
        }

        // Pick start and end as two tiles with furthest dungeon path distance
        // Using two-BFS to find dpd instead of 2D euclidean distance
        var (startRoom, endRoom, _) = BFS.FindFurthestPair(startingRoom);

        if (startRoom != null)
        {
            startRoom.RoomDefinition.Type |= RoomType.Start;
        }
        if (endRoom != null)
        {
            endRoom.RoomDefinition.Type |= RoomType.End;
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log("Dungeon generation time: " + stopwatch.ElapsedMilliseconds + " ms");
    }

    public void ResetDungeon()
    {
        openConnections.Clear();
        rooms.Clear();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        if (rooms.Count == 0)
            GenerateDungeon();
    }
}
