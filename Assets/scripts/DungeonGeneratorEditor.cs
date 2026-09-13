using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DungeonGenerator generator = (DungeonGenerator)target;
        DungeonTestRenderer renderer = FindAnyObjectByType<DungeonTestRenderer>();
        if (GUILayout.Button("Generate Dungeon"))
        {
            generator.GenerateDungeon();
            if (renderer != null)
            {
                renderer.RenderDungeon();
            }
        }

        if (GUILayout.Button("Reset Dungeon"))
        {
            generator.ResetDungeon();
            if (renderer != null)
            {
                renderer.RenderDungeon();
            }
        }
    }
}
