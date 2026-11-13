using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MinimapControllerTests
{
    [Test]
    public void WorldCoordinatesAreRotatedAroundPlayer()
    {
        Type controller = Type.GetType("MinimapController, Assembly-CSharp", true);
        MethodInfo map = controller.GetMethod("WorldToMap", BindingFlags.NonPublic | BindingFlags.Static);
        Vector2 player = new Vector2(10f, 20f);
        Vector2 center = new Vector2(30f, 40f);

        Vector2 Convert(Vector2 world) => (Vector2)map.Invoke(null, new object[] { world, player, center, 2f });

        Assert.That(Convert(player), Is.EqualTo(center));
        Assert.That(Convert(player + Vector2.up), Is.EqualTo(new Vector2(30f, 38f)));
        Assert.That(Convert(player + Vector2.right), Is.EqualTo(new Vector2(28f, 40f)));
        Assert.That(Convert(player + new Vector2(-3f, -4f)), Is.EqualTo(new Vector2(36f, 48f)));
    }

    [Test]
    public void HighlightedRectanglesAreDrawnBeforeBordersAndReplacedWithLayout()
    {
        Type controllerType = Type.GetType("MinimapController, Assembly-CSharp", true);
        Type tileType = Type.GetType("MinimapTile, Assembly-CSharp", true);
        Type graphicType = Type.GetType("MinimapGraphic, Assembly-CSharp", true);
        Type meshType = graphicType.BaseType.Assembly.GetType("UnityEngine.UI.VertexHelper", true);
        GameObject minimap = new GameObject("Test minimap", typeof(RectTransform));
        GameObject player = new GameObject("Test player");
        object mesh = Activator.CreateInstance(meshType);
        try
        {
            Component controller = minimap.AddComponent(controllerType);
            controllerType.GetField("player", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(controller, player.transform);
            object Tile(Vector2 position, Vector2 size, bool highlighted)
            {
                object tile = Activator.CreateInstance(tileType);
                tileType.GetField("position").SetValue(tile, position);
                tileType.GetField("size").SetValue(tile, size);
                tileType.GetField("isHighlighted").SetValue(tile, highlighted);
                return tile;
            }

            IList tiles = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tileType));
            tiles.Add(Tile(new Vector2(10f, 20f), new Vector2(4f, 6f), true));
            tiles.Add(Tile(new Vector2(-10f, -20f), new Vector2(8f, 2f), true));
            tiles.Add(Tile(new Vector2(50f, 50f), new Vector2(2f, 2f), false));
            MethodInfo setTiles = controllerType.GetMethod("SetTiles");
            MethodInfo populate = controllerType.GetMethod("Populate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo vertex = meshType.GetMethod("PopulateUIVertex");
            setTiles.Invoke(controller, new object[] { tiles });
            populate.Invoke(controller, new object[] { mesh, new Rect(-100, -100, 200, 200) });

            Assert.That((int)meshType.GetProperty("currentVertCount").GetValue(mesh), Is.EqualTo(69));
            object[] firstVertex = { new UIVertex(), 0 };
            vertex.Invoke(mesh, firstVertex);
            UIVertex first = (UIVertex)firstVertex[0];
            Assert.That(first.position, Is.EqualTo(new Vector3(-48f, -92f, 0f)));
            Assert.That(first.color, Is.EqualTo((Color32)new Color(1f, 0.72f, 0.1f, 0.45f)));
            object[] borderVertex = { new UIVertex(), 8 };
            vertex.Invoke(mesh, borderVertex);
            Assert.That(((UIVertex)borderVertex[0]).color, Is.EqualTo((Color32)Color.white));

            tiles.Clear();
            tiles.Add(Tile(Vector2.zero, new Vector2(4f, 6f), false));
            setTiles.Invoke(controller, new object[] { tiles });
            meshType.GetMethod("Clear").Invoke(mesh, null);
            populate.Invoke(controller, new object[] { mesh, new Rect(-100, -100, 200, 200) });
            Assert.That((int)meshType.GetProperty("currentVertCount").GetValue(mesh), Is.EqualTo(29));
        }
        finally
        {
            ((IDisposable)mesh).Dispose();
            Transform viewport = minimap.transform.Find("Minimap viewport");
            if (viewport != null)
                Object.DestroyImmediate(viewport.gameObject);
            Object.DestroyImmediate(minimap);
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void CreatedGraphicsHaveCanvasRenderers()
    {
        GameObject canvas = new GameObject("Test canvas", typeof(RectTransform), typeof(Canvas));
        try
        {
            GameObject minimap = new GameObject("Test minimap", typeof(RectTransform));
            minimap.transform.SetParent(canvas.transform, false);
            minimap.AddComponent(Type.GetType("MinimapController, Assembly-CSharp", true));

            Transform viewport = minimap.transform.Find("Minimap viewport");
            Assert.That(viewport, Is.Not.Null);
            Assert.That(viewport.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Transform drawing = viewport.Find("Minimap drawing");
            Assert.That(drawing, Is.Not.Null);
            Assert.That(drawing.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That(drawing.GetComponent(Type.GetType("MinimapGraphic, Assembly-CSharp", true)), Is.Not.Null);

            minimap.SetActive(false);
            minimap.SetActive(true);
            Assert.That(minimap.transform.childCount, Is.EqualTo(1));
            Assert.That(drawing.gameObject.activeInHierarchy, Is.True);
        }
        finally
        {
            Transform viewport = canvas.transform.Find("Test minimap/Minimap viewport");
            if (viewport != null)
                Object.DestroyImmediate(viewport.gameObject);
            Object.DestroyImmediate(canvas);
        }
    }

    [Test]
    public void AddingGraphicDirectlyAlsoAddsCanvasRenderer()
    {
        GameObject drawing = new GameObject("Test drawing", typeof(RectTransform));
        try
        {
            drawing.AddComponent(Type.GetType("MinimapGraphic, Assembly-CSharp", true));
            Assert.That(drawing.GetComponent<CanvasRenderer>(), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(drawing);
        }
    }
}
