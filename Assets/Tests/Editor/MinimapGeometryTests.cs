using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MinimapGeometryTests
{
    private Type tileType;
    private Type geometryType;

    [SetUp]
    public void SetUp()
    {
        tileType = Type.GetType("MinimapTile, Assembly-CSharp", true);
        geometryType = Type.GetType("MinimapGeometry, Assembly-CSharp", true);
    }

    private object Tile(float x, float z, float width, float depth)
    {
        object tile = Activator.CreateInstance(tileType);
        tileType.GetField("position").SetValue(tile, new Vector2(x, z));
        tileType.GetField("size").SetValue(tile, new Vector2(width, depth));
        return tile;
    }

    private IList Edges(params object[] tiles)
    {
        IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tileType));
        foreach (object tile in tiles)
            list.Add(tile);
        return (IList)geometryType.GetMethod("BuildEdges").Invoke(null, new object[] { list });
    }

    private static float Length(IList edges, bool border)
    {
        float length = 0f;
        foreach (object edge in edges)
        {
            Type type = edge.GetType();
            if ((bool)type.GetField("isBorder").GetValue(edge) != border)
                continue;
            Vector2 start = (Vector2)type.GetField("start").GetValue(edge);
            Vector2 end = (Vector2)type.GetField("end").GetValue(edge);
            length += Vector2.Distance(start, end);
        }
        return length;
    }

    [Test]
    public void SingleRectangularTileHasOnlyAnOutline()
    {
        IList edges = Edges(Tile(0, 0, 12, 6));
        Assert.That(edges.Count, Is.EqualTo(4));
        Assert.That(Length(edges, true), Is.EqualTo(36f).Within(0.001f));
        Assert.That(Length(edges, false), Is.Zero);
    }

    [Test]
    public void EqualNeighborsDrawTheirSharedEdgeOnce()
    {
        IList edges = Edges(Tile(0, 0, 12, 6), Tile(12, 0, 12, 6));
        Assert.That(Length(edges, true), Is.EqualTo(60f).Within(0.001f));
        Assert.That(Length(edges, false), Is.EqualTo(6f).Within(0.001f));
    }

    [Test]
    public void PartialSharedEdgeSplitsOutlineAndDrawsBorderOnce()
    {
        IList edges = Edges(Tile(0, 0, 12, 10), Tile(9, 2, 6, 4));
        Assert.That(Length(edges, true), Is.EqualTo(56f).Within(0.001f));
        Assert.That(Length(edges, false), Is.EqualTo(4f).Within(0.001f));
    }

    [Test]
    public void MultipleNeighborsCanSharePartsOfOneEdge()
    {
        IList edges = Edges(
            Tile(0, 0, 12, 12),
            Tile(8, -4, 4, 4),
            Tile(8, 4, 4, 4)
        );
        Assert.That(Length(edges, true), Is.EqualTo(64f).Within(0.001f));
        Assert.That(Length(edges, false), Is.EqualTo(8f).Within(0.001f));
    }

    [Test]
    public void CornerContactDoesNotCreateSharedBorder()
    {
        IList edges = Edges(Tile(0, 0, 10, 10), Tile(10, 10, 10, 10));
        Assert.That(Length(edges, true), Is.EqualTo(80f).Within(0.001f));
        Assert.That(Length(edges, false), Is.Zero);
    }
}
