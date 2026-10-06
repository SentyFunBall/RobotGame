using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VersionControl;
using UnityEngine;

[Flags]
public enum RoomType
{
    None = 0,
    Start = 1 << 0,
    End = 1 << 1,
    Boss = 1 << 2,
    Treasure = 1 << 3,
    Trap = 1 << 4,
    Puzzle = 1 << 5,
}

[Flags]
public enum RoomFeatures
{
    None = 0,
    IsIndoor = 1 << 2,
    HasSecret = 1 << 3,
    NoLoot = 1 << 4,
}

[Flags]
public enum RoomConnections
{
    None = 0,
    North = 1 << 0,
    South = 1 << 1,
    East = 1 << 2,
    West = 1 << 3,
}

[CreateAssetMenu(menuName = "Room Definition")]
public class RoomDefinition : ScriptableObject
{
    public RoomConnections Connections;
    public RoomType Type = RoomType.None;
    public RoomFeatures Features = RoomFeatures.None;

    public bool CanConnect(RoomDefinition room, RoomConnections required)
    {
        return (room.Connections & required) != 0;
    }

    private static readonly RoomConnections[] ClockwiseOrder =
    {
        RoomConnections.North,
        RoomConnections.East,
        RoomConnections.South,
        RoomConnections.West,
    };

    // Rotates a connection mask clockwise by the given number of 90 degree steps.
    public static RoomConnections RotateConnections(RoomConnections connections, int steps)
    {
        steps = ((steps % 4) + 4) % 4;
        if (steps == 0)
            return connections;

        RoomConnections result = RoomConnections.None;
        for (int i = 0; i < ClockwiseOrder.Length; i++)
        {
            if ((connections & ClockwiseOrder[i]) != 0)
                result |= ClockwiseOrder[(i + steps) % ClockwiseOrder.Length];
        }
        return result;
    }

    public static RoomConnections Opposite(RoomConnections direction)
    {
        return direction switch
        {
            RoomConnections.North => RoomConnections.South,
            RoomConnections.South => RoomConnections.North,
            RoomConnections.East => RoomConnections.West,
            RoomConnections.West => RoomConnections.East,
            _ => RoomConnections.None,
        };
    }
}
