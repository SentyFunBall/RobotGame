using UnityEditor;
using UnityEngine;

#if EDITOR
[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DungeonGenerator generator = (DungeonGenerator)target;
        DungeonTestRenderer renderer = FindAnyObjectByType<DungeonTestRenderer>();

        if (GUILayout.Button("Generate Dungeon (No loot/enemies)"))
        {
            generator.GenerateDungeonEditorMode();
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
#endif