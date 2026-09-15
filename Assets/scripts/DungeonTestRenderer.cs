using System.Collections.Generic;
using UnityEngine;

public class DungeonTestRenderer : MonoBehaviour
{
    private List<GameObject> shownCubes = new List<GameObject>();

    [SerializeField]
    private PhysicsMaterial m_physicMaterial;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void RenderDungeon()
    {
        // Clear previously shown cubes
        foreach (var cube in shownCubes)
        {
            DestroyImmediate(cube.GetComponent<Renderer>().material); // Need to clean up the material instance to avoid leaks in edit mode
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
            cube.transform.localScale = new Vector3(10, 1, 10);
            cube.GetComponent<Collider>().material = m_physicMaterial;

            if (room.RoomDefinition.Type.HasFlag(RoomType.Start))
            {
                cube.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.green);
            }
            else if (room.RoomDefinition.Type.HasFlag(RoomType.End))
            {
                cube.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.red);
            }

            shownCubes.Add(cube);
        }
    }

    void Start()
    {
        RenderDungeon();
    }
}
