using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class DungeonGeneratorTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

    private Type generatorType;
    private Type roomType;
    private Type definitionType;
    private Type connectionsType;
    private Type roomKindType;
    private Component generator;
    private GameObject generatorObject;
    private IDictionary rooms;
    private readonly Dictionary<object, object> previousRooms = new Dictionary<object, object>();
    private readonly List<object> previousOpenConnections = new List<object>();
    private readonly List<object> previousOccupiedPositions = new List<object>();
    private object previousStart;
    private object previousEnd;
    private readonly List<Object> createdObjects = new List<Object>();
    private Random.State previousRandomState;

    [SetUp]
    public void SetUp()
    {
        previousRooms.Clear();
        previousOpenConnections.Clear();
        previousOccupiedPositions.Clear();
        createdObjects.Clear();
        generatorType = Type.GetType("DungeonGenerator, Assembly-CSharp", true);
        roomType = Type.GetType("DungeonRoom, Assembly-CSharp", true);
        definitionType = Type.GetType("RoomDefinition, Assembly-CSharp", true);
        connectionsType = Type.GetType("RoomConnections, Assembly-CSharp", true);
        roomKindType = Type.GetType("RoomType, Assembly-CSharp", true);
        rooms = (IDictionary)generatorType.GetField("rooms").GetValue(null);
        foreach (DictionaryEntry entry in rooms)
            previousRooms.Add(entry.Key, entry.Value);
        rooms.Clear();
        SaveAndClearList((IList)generatorType.GetField("openConnections").GetValue(null), previousOpenConnections);
        SaveAndClearList((IList)generatorType.GetField("occupiedPositions", PrivateStatic).GetValue(null), previousOccupiedPositions);
        previousStart = generatorType.GetField("startRoom", PrivateStatic).GetValue(null);
        previousEnd = generatorType.GetField("endRoom", PrivateStatic).GetValue(null);
        previousRandomState = Random.state;

        generatorObject = new GameObject("Dungeon test generator");
        generatorObject.SetActive(false);
        generator = generatorObject.AddComponent(generatorType);
        SetDifficulty("Size", 10);
    }

    [TearDown]
    public void TearDown()
    {
        generatorType.GetMethod("ResetDungeon").Invoke(generator, null);
        Object.DestroyImmediate(generatorObject);
        foreach (Object created in createdObjects)
            Object.DestroyImmediate(created);
        foreach (KeyValuePair<object, object> entry in previousRooms)
            rooms.Add(entry.Key, entry.Value);
        RestoreList((IList)generatorType.GetField("openConnections").GetValue(null), previousOpenConnections);
        RestoreList((IList)generatorType.GetField("occupiedPositions", PrivateStatic).GetValue(null), previousOccupiedPositions);
        generatorType.GetField("startRoom", PrivateStatic).SetValue(null, previousStart);
        generatorType.GetField("endRoom", PrivateStatic).SetValue(null, previousEnd);
        Random.state = previousRandomState;
    }

    private static void SaveAndClearList(IList source, List<object> saved)
    {
        foreach (object value in source)
            saved.Add(value);
        source.Clear();
    }

    private static void RestoreList(IList destination, List<object> saved)
    {
        foreach (object value in saved)
            destination.Add(value);
    }

    [Test]
    public void CornerAndEndRotateToConnectBothPassages()
    {
        SetPrefabs(
            MakeRoom("Start", 2, 1, true),
            MakeRoom("End", 2, 2, true),
            MakeRoom("Corner", 2 | 4, 0, true)
        );
        var layout = new List<Vector3Int>
        {
            Vector3Int.zero,
            Vector3Int.back * 10,
            Vector3Int.left * 10,
        };

        Assert.That(Populate(layout), Is.True);
        Assert.That(rooms.Count, Is.EqualTo(3));
        Assert.That(GetRotation(Vector3Int.zero), Is.EqualTo(1));
        Assert.That(GetRotation(Vector3Int.back * 10), Is.EqualTo(2));
        Assert.That(GetRotation(Vector3Int.left * 10), Is.EqualTo(3));
        AssertConnectedAndWalls();
    }

    [Test]
    public void NonRotatableRoomsAreNotPhysicallyRotated()
    {
        SetPrefabs(MakeRoom("Start", 2, 1, false), MakeRoom("End", 2, 2, true));
        LogAssert.Expect(LogType.Warning, "Could not find compatible rotations for the connected room layout.");
        Assert.That(Populate(new List<Vector3Int> { Vector3Int.zero, Vector3Int.right * 10 }), Is.False);
        Assert.That(rooms.Count, Is.Zero);
    }

    [Test]
    public void NonRotatableRoomCanBeUsedInItsAuthoredOrientation()
    {
        SetPrefabs(MakeRoom("Start", 2, 1, false), MakeRoom("End", 2, 2, true));
        Assert.That(Populate(new List<Vector3Int> { Vector3Int.zero, Vector3Int.back * 10 }), Is.True);
        Assert.That(GetRotation(Vector3Int.zero), Is.Zero);
        AssertConnectedAndWalls();
    }

    [Test]
    public void AdjacentStartAndEndDoNotCreateAnExtraPassage()
    {
        SetPrefabs(
            MakeRoom("Start", 2, 1, true),
            MakeRoom("End", 2, 2, true),
            MakeRoom("Corner", 2 | 4, 0, true)
        );
        var layout = new List<Vector3Int>
        {
            Vector3Int.zero,
            Vector3Int.right * 10,
            Vector3Int.forward * 10,
            (Vector3Int.right + Vector3Int.forward) * 10,
        };
        Assert.That(Populate(layout), Is.True);
        Assert.That(rooms.Count, Is.EqualTo(4));
        AssertConnectedAndWalls();
    }

    [Test]
    public void DisconnectedLayoutFailsWithoutInstantiatingRooms()
    {
        LogAssert.Expect(LogType.Warning, "Cannot build a connected room layout with a start and end.");
        Assert.That(Populate(new List<Vector3Int> { Vector3Int.zero, Vector3Int.right * 20 }), Is.False);
        Assert.That(rooms.Count, Is.Zero);
    }

    [Test]
    public void GeneratedDungeonsWithRealPrefabsAreConnected()
    {
        const string folder = "Assets/Level Prefabs/World 1/";
        string[] names =
        {
            "Room-Start", "Room-End", "Room-angle", "Room-Climb",
            "Room-Empty", "Room-Pillars", "Room-Pit", "Room-Slides",
        };
        var prefabs = new List<GameObject>();
        foreach (string name in names)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + name + ".prefab");
            Assert.That(prefab, Is.Not.Null, name);
            prefabs.Add(prefab);
        }
        SetPrefabs(prefabs.ToArray());

        foreach (int count in new[] { 15, 25, 35 })
        {
            SetDifficulty("RoomCount", count);
            for (int seed = 0; seed < 25; seed++)
            {
                Random.InitState(seed);
                bool generated = false;
                for (int attempt = 0; attempt < 50 && !generated; attempt++)
                    generated = (bool)generatorType.GetMethod("TryGenerateRooms", PrivateInstance).Invoke(generator, null);
                Assert.That(generated, Is.True, $"Failed with {count} rooms and seed {seed}");
                Assert.That(rooms.Count, Is.EqualTo(count));
                AssertConnectedAndWalls();
            }
        }
    }

    private void SetDifficulty(string field, int value)
    {
        object difficulty = generatorType.GetField("Difficulty").GetValue(generator);
        difficulty.GetType().GetField(field).SetValue(difficulty, value);
    }

    private GameObject MakeRoom(string name, int connections, int kind, bool canRotate)
    {
        var prefab = new GameObject(name);
        createdObjects.Add(prefab);
        Component room = prefab.AddComponent(roomType);
        ScriptableObject definition = ScriptableObject.CreateInstance(definitionType);
        createdObjects.Add(definition);
        definitionType.GetField("Connections").SetValue(definition, Enum.ToObject(connectionsType, connections));
        definitionType.GetField("Type").SetValue(definition, Enum.ToObject(roomKindType, kind));
        definitionType.GetField("CanRotate").SetValue(definition, canRotate);
        roomType.GetField("RoomDefinition").SetValue(room, definition);
        foreach (string direction in new[] { "N", "S", "E", "W" })
        {
            var wall = new GameObject("Wall " + direction);
            wall.transform.SetParent(prefab.transform);
            roomType.GetField("Door" + direction).SetValue(room, wall);
        }
        return prefab;
    }

    private void SetPrefabs(params GameObject[] prefabs)
    {
        generatorType.GetField("allRooms", PrivateInstance).SetValue(generator, new List<GameObject>(prefabs));
    }

    private bool Populate(List<Vector3Int> positions)
    {
        return (bool)generatorType.GetMethod("PopulateRooms", PrivateInstance).Invoke(generator, new object[] { positions });
    }

    private int GetRotation(Vector3Int position)
    {
        return (int)roomType.GetField("Rotation").GetValue(rooms[position]);
    }

    private void AssertConnectedAndWalls()
    {
        var start = (Component)generatorType.GetField("startRoom", PrivateStatic).GetValue(null);
        var end = (Component)generatorType.GetField("endRoom", PrivateStatic).GetValue(null);
        Assert.That(start, Is.Not.Null);
        Assert.That(end, Is.Not.Null);
        var visited = new HashSet<Component> { start };
        var queue = new Queue<Component>();
        queue.Enqueue(start);
        string[] directions = { "North", "East", "South", "West" };
        string[] opposites = { "South", "West", "North", "East" };
        string[] doors = { "N", "E", "S", "W" };
        Vector3Int[] offsets = { Vector3Int.forward, Vector3Int.right, Vector3Int.back, Vector3Int.left };

        while (queue.Count > 0)
        {
            Component room = queue.Dequeue();
            int rotation = (int)roomType.GetField("Rotation").GetValue(room);
            Assert.That(Quaternion.Angle(room.transform.localRotation, Quaternion.Euler(0, 90 * rotation, 0)), Is.LessThan(0.01f));
            int neighborCount = 0;
            for (int i = 0; i < directions.Length; i++)
            {
                var neighbor = (Component)roomType.GetField(directions[i]).GetValue(room);
                int localDirection = (i - rotation + 4) % 4;
                var wall = (GameObject)roomType.GetField("Door" + doors[localDirection]).GetValue(room);
                if (wall != null)
                    Assert.That(wall.activeSelf, Is.EqualTo(neighbor == null));
                if (neighbor == null)
                    continue;

                neighborCount++;
                Assert.That(roomType.GetField(opposites[i]).GetValue(neighbor), Is.SameAs(room));
                var roomPosition = (Vector3Int)roomType.GetField("GridPosition").GetValue(room);
                var neighborPosition = (Vector3Int)roomType.GetField("GridPosition").GetValue(neighbor);
                Assert.That(neighborPosition, Is.EqualTo(roomPosition + offsets[i] * 10));
                object direction = Enum.ToObject(connectionsType, new[] { 1, 4, 2, 8 }[i]);
                Assert.That((bool)roomType.GetMethod("CanConnect").Invoke(room, new[] { direction }), Is.True);
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
            if (room == start || room == end)
                Assert.That(neighborCount, Is.EqualTo(1));
        }
        Assert.That(visited.Count, Is.EqualTo(rooms.Count));
        Assert.That(visited.Contains(end), Is.True);
    }
}
