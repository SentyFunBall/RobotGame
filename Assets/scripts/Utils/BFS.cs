using System.Collections.Generic;
using UnityEngine;

public static class BFS
{
    /// Calculates the shortest path distance (number of room connections) between two dungeon rooms using BFS.
    /// Returns -1 if either room is null or no path exists.
    public static int GetDistance(DungeonRoom start, DungeonRoom target)
    {
        if (start == null || target == null)
            return -1;

        if (start == target)
            return 0;

        Queue<(DungeonRoom room, int distance)> queue = new();
        HashSet<DungeonRoom> visited = new();

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, distance) = queue.Dequeue();

            if (current == target)
                return distance;

            foreach (var neighbor in current.GetNeighbors())
            {
                if (neighbor != null && visited.Add(neighbor))
                {
                    if (neighbor == target)
                        return distance + 1;

                    queue.Enqueue((neighbor, distance + 1));
                }
            }
        }

        return -1;
    }

    /// Alias for GetDistance to calculate dungeon path distance between two nodes.
    public static int CalculateDungeonPathDistance(DungeonRoom start, DungeonRoom target)
    {
        return GetDistance(start, target);
    }

    /// Finds the shortest path between start and target rooms using BFS.
    /// Returns a list of rooms from start to target, or null if unreachable.
    public static List<DungeonRoom> GetPath(DungeonRoom start, DungeonRoom target)
    {
        if (start == null || target == null)
            return null;

        if (start == target)
            return new List<DungeonRoom> { start };

        Queue<DungeonRoom> queue = new();
        Dictionary<DungeonRoom, DungeonRoom> cameFrom = new();
        HashSet<DungeonRoom> visited = new();

        queue.Enqueue(start);
        visited.Add(start);

        bool found = false;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == target)
            {
                found = true;
                break;
            }

            foreach (var neighbor in current.GetNeighbors())
            {
                if (neighbor != null && visited.Add(neighbor))
                {
                    cameFrom[neighbor] = current;
                    if (neighbor == target)
                    {
                        found = true;
                        break;
                    }
                    queue.Enqueue(neighbor);
                }
            }

            if (found)
                break;
        }

        if (!found)
            return null;

        List<DungeonRoom> path = new();
        DungeonRoom curr = target;
        while (curr != null)
        {
            path.Add(curr);
            cameFrom.TryGetValue(curr, out curr);
        }

        path.Reverse();
        return path;
    }

    /// Calculates the shortest distance from the start room to all reachable rooms in the dungeon.
    /// Returns a dictionary mapping each reachable room to its distance from the start room.
    public static Dictionary<DungeonRoom, int> GetAllDistances(DungeonRoom start)
    {
        Dictionary<DungeonRoom, int> distances = new();
        if (start == null)
            return distances;

        Queue<(DungeonRoom room, int distance)> queue = new();
        queue.Enqueue((start, 0));
        distances[start] = 0;

        while (queue.Count > 0)
        {
            var (current, distance) = queue.Dequeue();

            foreach (var neighbor in current.GetNeighbors())
            {
                if (neighbor != null && !distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = distance + 1;
                    queue.Enqueue((neighbor, distance + 1));
                }
            }
        }

        return distances;
    }

    /// <summary>
    /// Finds the furthest reachable room from the given start room along with its path distance.
    /// </summary>
    public static (DungeonRoom room, int distance) GetFurthestRoom(DungeonRoom start)
    {
        if (start == null)
            return (null, -1);

        var distances = GetAllDistances(start);
        DungeonRoom furthest = start;
        int maxDist = 0;

        foreach (var kvp in distances)
        {
            if (kvp.Value > maxDist)
            {
                maxDist = kvp.Value;
                furthest = kvp.Key;
            }
        }

        return (furthest, maxDist);
    }

    /// Performs a two-BFS algorithm to find the two nodes with the maximum dungeon path distance from each other.
    /// Pass 1: Traverses from the root/initial room to find the furthest leaf/endpoint room A.
    /// Pass 2: Traverses from room A to find the furthest room B.
    public static (DungeonRoom start, DungeonRoom end, int distance) FindFurthestPair(
        DungeonRoom root
    )
    {
        if (root == null)
            return (null, null, -1);

        // BFS 1: Find an extreme node furthest from root
        var (roomA, _) = GetFurthestRoom(root);
        if (roomA == null)
            return (null, null, -1);

        // BFS 2: Find the furthest node from roomA
        var (roomB, maxDistance) = GetFurthestRoom(roomA);

        return (roomA, roomB, maxDistance);
    }
}
