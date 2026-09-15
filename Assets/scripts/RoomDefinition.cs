using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[Flags]
public enum RoomType
{
    None = 0,
    Start = 1 << 0,
    End = 1 << 1,
}

[Flags]
public enum RoomConnections
{
    None = 0,
    North = 1 << 0,
    South = 1 << 1,
    East = 1 << 2,
    West = 1 << 3,
    Up = 1 << 4,
    Down = 1 << 5,
}

[CreateAssetMenu]
public class RoomDefinition : ScriptableObject
{
    public GameObject Prefab;
    public RoomConnections Connections;
    public RoomType Type = RoomType.None;

    public bool CanConnect(RoomDefinition room, RoomConnections required)
    {
        return (room.Connections & required) != 0;
    }
}
