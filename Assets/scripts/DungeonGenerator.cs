using System.Collections.Generic;
using System.Diagnostics;
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
    public int Size = 12; // Default size of the dungeon (N x N)

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

    public DungeonDifficulty Difficulty = new();
    public static Dictionary<Vector3Int, DungeonRoom> rooms = new();
    public static List<OpenConnection> openConnections = new();

    private static DungeonRoom startRoom;
    private static DungeonRoom endRoom;
    private static GameObject player;
    private bool hasWon;
    private static List<Vector3Int> occupiedPositions = new();

    [SerializeField]
    private GameObject navMesh;

    [SerializeField]
    private List<GameObject> allRooms = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ApplyDifficulty(MenuControler.SelectedDifficulty);
    }

    public void ApplyDifficulty(string difficultyName)
    {
        if (string.IsNullOrEmpty(difficultyName))
        {
            difficultyName = PlayerPrefs.GetString("GameDifficulty", "Medium");
        }

        switch (difficultyName)
        {
            case "Easy":
                Difficulty.EnemyStrength = 5;
                Difficulty.PuzzleComplexity = 5;
                Difficulty.TrapFrequency = 5;
                Difficulty.Size = 12;
                Difficulty.RoomCount = 15;
                break;

            case "Hard":
                Difficulty.EnemyStrength = 45;
                Difficulty.PuzzleComplexity = 45;
                Difficulty.TrapFrequency = 45;
                Difficulty.Size = 12;
                Difficulty.RoomCount = 35;
                break;

            case "Medium":
            default:
                Difficulty.EnemyStrength = 25;
                Difficulty.PuzzleComplexity = 25;
                Difficulty.TrapFrequency = 25;
                Difficulty.Size = 12;
                Difficulty.RoomCount = 25;
                break;
        }
    }

    private DungeonRoom CreateRoom(
        GameObject prefab,
        int rotation,
        Vector3Int position,
        RoomConnections required
    )
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

        // Only set neighbor links if that connection direction was explicitly required
        // by the already-placed neighbors. This prevents accidental connections in
        // directions we didn't plan for, which would cause misaligned doors and traversability issues.
        if (
            rooms.TryGetValue(
                position + Vector3Int.forward * Difficulty.Size,
                out DungeonRoom northNeighbor
            )
        )
        {
            if (
                northNeighbor != null
                && (required & RoomConnections.North) != 0
                && CanConnectInDirection(room, northNeighbor, RoomConnections.North)
            )
            {
                room.North = northNeighbor;
                northNeighbor.South = room;
            }
        }

        if (
            rooms.TryGetValue(
                position + Vector3Int.back * Difficulty.Size,
                out DungeonRoom southNeighbor
            )
        )
        {
            if (
                southNeighbor != null
                && (required & RoomConnections.South) != 0
                && CanConnectInDirection(room, southNeighbor, RoomConnections.South)
            )
            {
                room.South = southNeighbor;
                southNeighbor.North = room;
            }
        }

        if (
            rooms.TryGetValue(
                position + Vector3Int.right * Difficulty.Size,
                out DungeonRoom eastNeighbor
            )
        )
        {
            if (
                eastNeighbor != null
                && (required & RoomConnections.East) != 0
                && CanConnectInDirection(room, eastNeighbor, RoomConnections.East)
            )
            {
                room.East = eastNeighbor;
                eastNeighbor.West = room;
            }
        }

        if (
            rooms.TryGetValue(
                position + Vector3Int.left * Difficulty.Size,
                out DungeonRoom westNeighbor
            )
        )
        {
            if (
                westNeighbor != null
                && (required & RoomConnections.West) != 0
                && CanConnectInDirection(room, westNeighbor, RoomConnections.West)
            )
            {
                room.West = westNeighbor;
                westNeighbor.East = room;
            }
        }

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

    private bool CanConnectInDirection(
        DungeonRoom room,
        DungeonRoom neighbor,
        RoomConnections direction
    )
    {
        // Check if this room can connect in the given direction
        RoomConnections roomConnections = RoomDefinition.RotateConnections(
            room.RoomDefinition.Connections,
            room.Rotation,
            room.RoomDefinition
        );

        if ((roomConnections & direction) == 0)
            return false;

        // Check if neighbor can connect in the opposite direction
        RoomConnections opposite = RoomDefinition.Opposite(direction);
        RoomConnections neighborConnections = RoomDefinition.RotateConnections(
            neighbor.RoomDefinition.Connections,
            neighbor.Rotation,
            neighbor.RoomDefinition
        );

        return (neighborConnections & opposite) != 0;
    }

    private struct RoomPlacement
    {
        public GameObject Prefab;
        public int Rotation;
        public RoomConnections Connections;
    }

    private struct GridDirection
    {
        public Vector3Int Offset;
        public RoomConnections Connection;

        public GridDirection(Vector3Int offset, RoomConnections connection)
        {
            Offset = offset;
            Connection = connection;
        }
    }

    private static readonly GridDirection[] GridDirections =
    {
        new GridDirection(Vector3Int.forward, RoomConnections.North),
        new GridDirection(Vector3Int.back, RoomConnections.South),
        new GridDirection(Vector3Int.right, RoomConnections.East),
        new GridDirection(Vector3Int.left, RoomConnections.West),
    };

    private List<RoomPlacement> GetRoomCandidates(
        RoomType requiredType,
        RoomConnections required,
        RoomConnections forbidden
    )
    {
        List<RoomPlacement> candidates = new List<RoomPlacement>();
        foreach (GameObject prefabObject in allRooms)
        {
            if (prefabObject == null)
                continue;

            DungeonRoom prefabRoom = prefabObject.GetComponent<DungeonRoom>();
            if (prefabRoom == null)
            {
                UnityEngine.Debug.LogError($"Room prefab {prefabObject.name} has no DungeonRoom.");
                continue;
            }

            RoomDefinition definition = prefabRoom.RoomDefinition;
            if (definition == null)
            {
                UnityEngine.Debug.LogError($"Room prefab {prefabObject.name} has no RoomDefinition.");
                continue;
            }

            if (requiredType != RoomType.None && (definition.Type & requiredType) != requiredType)
                continue;

            bool isStartRoom = (definition.Type & RoomType.Start) != 0;
            bool isEndRoom = (definition.Type & RoomType.End) != 0;
            if (isStartRoom && isEndRoom)
            {
                UnityEngine.Debug.LogError(
                    "Room " + prefabObject.name + " cannot be both Start and End type."
                );
                continue;
            }

            if (isStartRoom && requiredType != RoomType.Start)
                continue;
            if (isEndRoom && requiredType != RoomType.End)
                continue;

            int rotationCount = definition.CanRotate ? 4 : 1;
            for (int rotation = 0; rotation < rotationCount; rotation++)
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

                candidates.Add(
                    new RoomPlacement
                    {
                        Prefab = prefabObject,
                        Rotation = rotation,
                        Connections = rotated,
                    }
                );
            }
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);
        }
        return candidates;
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

        RoomConnections opposite = RoomDefinition.Opposite(direction);
        RoomConnections neighborConnections = RoomDefinition.RotateConnections(
            neighbor.RoomDefinition.Connections,
            neighbor.Rotation,
            neighbor.RoomDefinition
        );

        if ((neighborConnections & opposite) != 0)
            required |= direction;
        else
            forbidden |= direction;
    }

    private bool IsPositionOccupied(Vector3Int position)
    {
        return rooms.ContainsKey(position);
    }

    private List<Vector3Int> GenerateLayout()
    {
        // Build the connected topology, just track positions, not actual rooms yet.
        // Rooms will be populated later.
        List<Vector3Int> layoutPositions = new List<Vector3Int>();
        List<Vector3Int> frontier = new List<Vector3Int>();

        layoutPositions.Add(Vector3Int.zero);
        frontier.Add(Vector3Int.zero);

        while (layoutPositions.Count < Difficulty.RoomCount && frontier.Count > 0)
        {
            // Pick a random position from the frontier
            Vector3Int current = frontier[Random.Range(0, frontier.Count)];
            frontier.Remove(current);

            // Try to expand in random directions
            Vector3Int[] directions =
            {
                Vector3Int.forward * Difficulty.Size,
                Vector3Int.back * Difficulty.Size,
                Vector3Int.right * Difficulty.Size,
                Vector3Int.left * Difficulty.Size,
            };

            // Shuffle directions randomly
            for (int i = directions.Length - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (directions[i], directions[randomIndex]) = (directions[randomIndex], directions[i]);
            }

            foreach (Vector3Int direction in directions)
            {
                if (layoutPositions.Count >= Difficulty.RoomCount)
                    break;

                Vector3Int newPosition = current + direction;

                if (!layoutPositions.Contains(newPosition))
                {
                    layoutPositions.Add(newPosition);
                    frontier.Add(newPosition);
                }
            }
        }

        return layoutPositions;
    }

    // A spanning tree makes every room reachable without requiring passages between
    // every pair of adjacent rooms. Its leaves are suitable for start and end rooms.
    private bool PlanConnections(
        List<Vector3Int> layoutPositions,
        out Dictionary<Vector3Int, RoomConnections> required,
        out Vector3Int startPosition,
        out Vector3Int endPosition
    )
    {
        required = new Dictionary<Vector3Int, RoomConnections>();
        startPosition = Vector3Int.zero;
        endPosition = Vector3Int.zero;
        if (Difficulty.Size <= 0 || layoutPositions.Count < 2)
            return false;

        HashSet<Vector3Int> layout = new HashSet<Vector3Int>(layoutPositions);
        if (layout.Count != layoutPositions.Count)
            return false;

        Queue<Vector3Int> frontier = new Queue<Vector3Int>();
        frontier.Enqueue(layoutPositions[0]);
        required.Add(layoutPositions[0], RoomConnections.None);
        while (frontier.Count > 0)
        {
            Vector3Int position = frontier.Dequeue();
            foreach (GridDirection direction in GridDirections)
            {
                Vector3Int neighbor = position + direction.Offset * Difficulty.Size;
                if (!layout.Contains(neighbor) || required.ContainsKey(neighbor))
                    continue;

                required[position] |= direction.Connection;
                required.Add(neighbor, RoomDefinition.Opposite(direction.Connection));
                frontier.Enqueue(neighbor);
            }
        }

        if (required.Count != layout.Count)
            return false;

        float maxDistance = -1f;
        foreach (Vector3Int first in layoutPositions)
        {
            if (!HasSingleConnection(required[first]))
                continue;
            foreach (Vector3Int second in layoutPositions)
            {
                if (first == second || !HasSingleConnection(required[second]))
                    continue;
                float distance = (first - second).sqrMagnitude;
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    startPosition = first;
                    endPosition = second;
                }
            }
        }
        return maxDistance >= 0f;
    }

    private static bool HasSingleConnection(RoomConnections connections)
    {
        int mask = (int)connections;
        return mask != 0 && (mask & (mask - 1)) == 0;
    }

    private bool SolvePlacements(
        List<Vector3Int> positions,
        Dictionary<Vector3Int, List<RoomPlacement>> candidates,
        Dictionary<Vector3Int, RoomPlacement> placements,
        ref int attemptsRemaining
    )
    {
        if (placements.Count == positions.Count)
            return true;
        if (attemptsRemaining <= 0)
            return false;

        Vector3Int bestPosition = Vector3Int.zero;
        List<RoomPlacement> bestCandidates = null;
        foreach (Vector3Int position in positions)
        {
            if (placements.ContainsKey(position))
                continue;

            List<RoomPlacement> compatible = new List<RoomPlacement>();
            foreach (RoomPlacement candidate in candidates[position])
            {
                bool matches = true;
                foreach (GridDirection direction in GridDirections)
                {
                    Vector3Int neighbor = position + direction.Offset * Difficulty.Size;
                    if (!placements.TryGetValue(neighbor, out RoomPlacement neighborPlacement))
                        continue;

                    bool opensHere = (candidate.Connections & direction.Connection) != 0;
                    bool opensThere = (
                        neighborPlacement.Connections & RoomDefinition.Opposite(direction.Connection)
                    ) != 0;
                    if (opensHere != opensThere)
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                    compatible.Add(candidate);
            }

            if (compatible.Count == 0)
                return false;
            if (bestCandidates == null || compatible.Count < bestCandidates.Count)
            {
                bestPosition = position;
                bestCandidates = compatible;
            }
        }

        foreach (RoomPlacement candidate in bestCandidates)
        {
            if (attemptsRemaining-- <= 0)
                return false;
            placements.Add(bestPosition, candidate);
            if (SolvePlacements(positions, candidates, placements, ref attemptsRemaining))
                return true;
            placements.Remove(bestPosition);
        }
        return false;
    }

    private bool PopulateRooms(List<Vector3Int> layoutPositions)
    {
        if (!PlanConnections(layoutPositions, out var required, out var startPosition, out var endPosition))
        {
            UnityEngine.Debug.LogWarning("Cannot build a connected room layout with a start and end.");
            return false;
        }

        HashSet<Vector3Int> layout = new HashSet<Vector3Int>(layoutPositions);
        Dictionary<Vector3Int, List<RoomPlacement>> candidates = new Dictionary<Vector3Int, List<RoomPlacement>>();
        foreach (Vector3Int position in layoutPositions)
        {
            RoomType type = position == startPosition ? RoomType.Start
                : position == endPosition ? RoomType.End : RoomType.None;
            RoomConnections forbidden = RoomConnections.None;
            if (type != RoomType.None)
            {
                foreach (GridDirection direction in GridDirections)
                {
                    Vector3Int neighbor = position + direction.Offset * Difficulty.Size;
                    if (layout.Contains(neighbor) && (required[position] & direction.Connection) == 0)
                        forbidden |= direction.Connection;
                }
            }
            candidates.Add(position, GetRoomCandidates(type, required[position], forbidden));
        }

        Dictionary<Vector3Int, RoomPlacement> placements = new Dictionary<Vector3Int, RoomPlacement>();
        int attemptsRemaining = 20000;
        if (!SolvePlacements(layoutPositions, candidates, placements, ref attemptsRemaining))
        {
            UnityEngine.Debug.LogWarning("Could not find compatible rotations for the connected room layout.");
            return false;
        }

        foreach (Vector3Int position in layoutPositions)
        {
            RoomConnections existingConnections = RoomConnections.None;
            RoomConnections forbidden = RoomConnections.None;
            foreach (GridDirection direction in GridDirections)
            {
                CollectDirectionConstraints(
                    position + direction.Offset * Difficulty.Size,
                    direction.Connection,
                    ref existingConnections,
                    ref forbidden
                );
            }
            RoomPlacement placement = placements[position];
            DungeonRoom room = CreateRoom(placement.Prefab, placement.Rotation, position, existingConnections);
            rooms.Add(position, room);
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
        if (!player)
        {
            UnityEngine.Debug.LogError("Player appears to have been deleted");
        }
    }

    private bool TryGenerateRooms()
    {
        startRoom = null;
        endRoom = null;
        hasWon = false;
        ClearRooms();

        // Generate just a list of positions
        List<Vector3Int> layoutPositions = GenerateLayout();
        if (layoutPositions.Count == 0)
        {
            UnityEngine.Debug.LogError("Failed to generate layout positions.");
            return false;
        }

        if (!PopulateRooms(layoutPositions))
        {
            UnityEngine.Debug.LogWarning("Could not populate the dungeon with compatible rooms.");
            return false;
        }

        return true;
    }

    private void ClearRooms()
    {
        foreach (DungeonRoom room in rooms.Values)
        {
            if (room == null)
                continue;
            if (Application.isPlaying)
                Destroy(room.gameObject);
            else
                DestroyImmediate(room.gameObject);
        }
        rooms.Clear();
        openConnections.Clear();
        occupiedPositions.Clear();
        startRoom = null;
        endRoom = null;
    }

    public void GenerateDungeon()
    {
        GenerateDungeonInternal(spawnEnemiesAndLoot: true);
    }

    public void GenerateDungeonEditorMode()
    {
        GenerateDungeonInternal(spawnEnemiesAndLoot: false);
    }

    private void GenerateDungeonInternal(bool spawnEnemiesAndLoot)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // Generation is random and can fail, which used to leave the player with no floor
        // to stand on. Retry with a fresh layout until one works.
        const int maxAttempts = 50;
        bool generated = false;
        for (int attempt = 0; attempt < maxAttempts && !generated; attempt++)
        {
            generated = TryGenerateRooms();
            if (!generated)
                ClearRooms();
        }

        if (!generated)
        {
            UnityEngine.Debug.LogError($"Dungeon generation failed after {maxAttempts} attempts.");
            return;
        }

        if (spawnEnemiesAndLoot)
        {
            StartCoroutine(navMesh.GetComponent<GenerateNavMesh>().Bake());
            StartCoroutine(navMesh.GetComponent<GenerateNavMesh>().SpawnEnemies());
            StartCoroutine(GetComponent<LootGenerator>().GenerateLoot());
        }

        //MovePlayerToStart();
        stopwatch.Stop();
        UnityEngine.Debug.Log("Dungeon generation time: " + stopwatch.ElapsedMilliseconds + " ms");
    }

    public void ResetDungeon()
    {
        ClearRooms();
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
    }

    public static DungeonRoom GetRandomRoom()
    {
        if (rooms.Count == 0)
            return null;

        List<DungeonRoom> roomList = new List<DungeonRoom>(rooms.Values);
        return roomList[Random.Range(0, roomList.Count)];
    }
}
