using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

public class DungeonRoom : MonoBehaviour
{
    public Vector3Int GridPosition;

    public DungeonRoom North;
    public DungeonRoom South;
    public DungeonRoom East;
    public DungeonRoom West;

    public NavMeshLink LinkNorth;
    public NavMeshLink LinkSouth;
    public NavMeshLink LinkEast;
    public NavMeshLink LinkWest;

    public GameObject DoorN;
    public GameObject DoorS;
    public GameObject DoorE;
    public GameObject DoorW;

    public RoomDefinition RoomDefinition;

    // Number of 90 degree clockwise rotations applied to RoomDefinition.Connections.
    public int Rotation;

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
    }

    public bool HasFeature(RoomFeatures feature)
    {
        return (RoomDefinition.Features & feature) != 0;
    }

    public bool IsType(RoomType type)
    {
        return (RoomDefinition.Type & type) != 0;
    }

    public bool CanConnect(RoomConnections required)
    {
        RoomConnections effective = RoomDefinition.RotateConnections(
            RoomDefinition.Connections,
            Rotation,
            RoomDefinition
        );
        return (effective & required) != 0;
    }

    // Disables the door/wall object on any side that has a placed neighbor, opening a
    // passage between the rooms. Doors are authored in local (unrotated) room space, so
    // a world-space direction has to be rotated back to find the matching local door.
    public void UpdateWalls()
    {
        SetWallState(RoomConnections.North, North);
        SetWallState(RoomConnections.South, South);
        SetWallState(RoomConnections.East, East);
        SetWallState(RoomConnections.West, West);
    }

    private void SetWallState(RoomConnections worldDirection, DungeonRoom neighbor)
    {
        RoomConnections localDirection = RoomDefinition.RotateConnections(
            worldDirection,
            -Rotation,
            RoomDefinition
        );
        GameObject door = GetDoor(localDirection);
        if (door != null)
            door.SetActive(neighbor == null);
    }

    private GameObject GetDoor(RoomConnections localDirection)
    {
        return localDirection switch
        {
            RoomConnections.North => DoorN,
            RoomConnections.South => DoorS,
            RoomConnections.East => DoorE,
            RoomConnections.West => DoorW,
            _ => null,
        };
    }

    public bool HasNeighbor(DungeonRoom neighbor)
    {
        return North == neighbor || South == neighbor || East == neighbor || West == neighbor;
    }

    public bool IsConnected()
    {
        return North != null || South != null || East != null || West != null;
    }
}
