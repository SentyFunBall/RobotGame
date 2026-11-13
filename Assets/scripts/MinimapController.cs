using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MinimapController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField, Min(0.01f)] private float mapScale = 4f;
    [SerializeField, Min(0.1f)] private float borderWidth = 3f;
    [SerializeField, Min(0.1f)] private float tileLineWidth = 1f;
    [SerializeField, Min(0.1f)] private float enemyDotSize = 5f;
    [SerializeField, Min(0.1f)] private float playerDotSize = 6f;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color borderColor = Color.white;
    [SerializeField] private Color tileLineColor = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private Color roomHighlightColor = new Color(1f, 0.72f, 0.1f, 0.45f);
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color playerColor = Color.white;

    private readonly List<MinimapEdge> edges = new List<MinimapEdge>();
    private readonly List<Rect> highlightedRooms = new List<Rect>();
    private EnemyAI[] enemies = new EnemyAI[0];
    private RobotBossAI[] bosses = new RobotBossAI[0];
    private float nextSearch;
    private int layoutVersion = -1;
    private bool useGeneratedDungeon = true;
    private bool warnedAboutPlayer;
    private GameObject viewport;
    private MinimapGraphic graphic;

    private void OnEnable()
    {
        if (viewport == null)
            CreateGraphics();
        viewport.SetActive(true);
        nextSearch = 0f;
        layoutVersion = -1;
    }

    private void OnDisable()
    {
        if (viewport != null)
            viewport.SetActive(false);
    }

    private void OnDestroy()
    {
        if (viewport != null)
            Destroy(viewport);
    }

    private void CreateGraphics()
    {
        viewport = new GameObject("Minimap viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        RectTransform viewportRect = (RectTransform)viewport.transform;
        viewportRect.SetParent(transform, false);
        Stretch(viewportRect);
        Image background = viewport.GetComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        GameObject drawing = new GameObject("Minimap drawing", typeof(RectTransform), typeof(CanvasRenderer), typeof(MinimapGraphic));
        RectTransform drawingRect = (RectTransform)drawing.transform;
        drawingRect.SetParent(viewportRect, false);
        Stretch(drawingRect);
        graphic = drawing.GetComponent<MinimapGraphic>();
        graphic.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void LateUpdate()
    {
        if (useGeneratedDungeon && layoutVersion != DungeonGenerator.LayoutVersion)
            RebuildGeneratedTiles();

        if (Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.5f;
            if (player == null)
            {
                GameObject playerObject = GameObject.FindWithTag("Player");
                if (playerObject != null)
                    player = playerObject.transform;
                else if (!warnedAboutPlayer)
                {
                    Debug.LogWarning("MinimapController could not find a player with the Player tag.", this);
                    warnedAboutPlayer = true;
                }
            }
            enemies = FindObjectsByType<EnemyAI>();
            bosses = FindObjectsByType<RobotBossAI>();
        }

        graphic.SetData(this);
    }

    private void RebuildGeneratedTiles()
    {
        layoutVersion = DungeonGenerator.LayoutVersion;
        var tiles = new List<MinimapTile>();
        if (DungeonGenerator.Instance != null)
        {
            float size = DungeonGenerator.Instance.Difficulty.Size;
            foreach (DungeonRoom room in DungeonGenerator.rooms.Values)
            {
                if (room == null)
                    continue;
                tiles.Add(new MinimapTile
                {
                    position = new Vector2(room.GridPosition.x, room.GridPosition.z),
                    size = new Vector2(size, size),
                    isHighlighted = room == DungeonGenerator.StartRoom || room == DungeonGenerator.EndRoom,
                });
            }
        }
        SetEdges(tiles);
    }

    // Overrides the generated layout with world-space rectangular footprints.
    public void SetTiles(IReadOnlyList<MinimapTile> tiles)
    {
        if (tiles == null)
        {
            Debug.LogError("Cannot set minimap tiles to null.", this);
            return;
        }
        useGeneratedDungeon = false;
        SetEdges(tiles);
    }

    public void UseGeneratedDungeon()
    {
        useGeneratedDungeon = true;
        layoutVersion = -1;
    }

    private void SetEdges(IReadOnlyList<MinimapTile> tiles)
    {
        edges.Clear();
        edges.AddRange(MinimapGeometry.BuildEdges(tiles));
        highlightedRooms.Clear();
        foreach (MinimapTile tile in tiles)
        {
            if (tile != null && tile.isHighlighted && tile.size.x > 0f && tile.size.y > 0f)
                highlightedRooms.Add(new Rect(tile.position - tile.size * 0.5f, tile.size));
        }
    }

    internal void Populate(VertexHelper mesh, Rect rect)
    {
        if (player == null)
            return;

        Vector3 playerPosition = player.position;
        Vector2 center = rect.center;
        float scale = Mathf.Max(0.01f, mapScale);
        Vector2 Map(Vector2 world) =>
            WorldToMap(world, new Vector2(playerPosition.x, playerPosition.z), center, scale);

        foreach (Rect room in highlightedRooms)
            AddRoomFill(mesh, Map(room.min), Map(room.max), roomHighlightColor);

        foreach (MinimapEdge edge in edges)
            AddLine(mesh, Map(edge.start), Map(edge.end),
                edge.isBorder ? borderWidth : tileLineWidth,
                edge.isBorder ? borderColor : tileLineColor);

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy != null && enemy.isActiveAndEnabled && enemy.IsAlive)
                AddDot(mesh, Map(new Vector2(enemy.transform.position.x, enemy.transform.position.z)), enemyDotSize, enemyColor);
        }
        foreach (RobotBossAI boss in bosses)
        {
            if (boss != null && boss.isActiveAndEnabled && boss.Health > 0f)
                AddDot(mesh, Map(new Vector2(boss.transform.position.x, boss.transform.position.z)), enemyDotSize, enemyColor);
        }
        AddDot(mesh, center, playerDotSize, playerColor);
    }

    private static Vector2 WorldToMap(Vector2 world, Vector2 playerPosition, Vector2 center, float scale)
    {
        return center - (world - playerPosition) * scale;
    }

    private static void AddRoomFill(VertexHelper mesh, Vector2 first, Vector2 second, Color color)
    {
        Vector2 min = Vector2.Min(first, second);
        Vector2 max = Vector2.Max(first, second);
        int index = mesh.currentVertCount;
        mesh.AddVert(new Vector2(min.x, min.y), color, Vector2.zero);
        mesh.AddVert(new Vector2(min.x, max.y), color, Vector2.zero);
        mesh.AddVert(new Vector2(max.x, max.y), color, Vector2.zero);
        mesh.AddVert(new Vector2(max.x, min.y), color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }

    private static void AddLine(VertexHelper mesh, Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 delta = end - start;
        if (delta.sqrMagnitude < 0.000001f)
            return;
        Vector2 normal = new Vector2(-delta.y, delta.x).normalized * (Mathf.Max(0.1f, width) * 0.5f);
        int index = mesh.currentVertCount;
        mesh.AddVert(start - normal, color, Vector2.zero);
        mesh.AddVert(start + normal, color, Vector2.zero);
        mesh.AddVert(end + normal, color, Vector2.zero);
        mesh.AddVert(end - normal, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2);
        mesh.AddTriangle(index, index + 2, index + 3);
    }

    private static void AddDot(VertexHelper mesh, Vector2 center, float size, Color color)
    {
        const int sides = 12;
        int index = mesh.currentVertCount;
        mesh.AddVert(center, color, Vector2.zero);
        float radius = Mathf.Max(0.1f, size) * 0.5f;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
        }
        for (int i = 0; i < sides; i++)
            mesh.AddTriangle(index, index + 1 + i, index + 1 + (i + 1) % sides);
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public class MinimapGraphic : MaskableGraphic
{
    private MinimapController controller;

    internal void SetData(MinimapController source)
    {
        controller = source;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (controller != null)
            controller.Populate(mesh, rectTransform.rect);
    }
}
