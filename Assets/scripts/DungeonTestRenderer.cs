using System.Collections.Generic;
using UnityEngine;

public class DungeonTestRenderer : MonoBehaviour
{
    private List<GameObject> shownCubes = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void RenderDungeon()
    {
        // Clear previously shown cubes
        foreach (var cube in shownCubes)
        {
            DestroyImmediate(cube);
        }
        shownCubes.Clear();

        var rooms = DungeonGenerator.rooms;
        foreach (var kvp in rooms)
        {
            var room = kvp.Value;
            // Spawn a primitive cube to represent the room
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = room.GridPosition;
            cube.transform.localScale = new Vector3(8, 1, 8);
            shownCubes.Add(cube);
        }
    }

    void Start()
    {
        RenderDungeon();
    }
}
