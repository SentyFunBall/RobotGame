using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MinimapTile
{
    // World-space X/Z center and full width/depth, respectively.
    public Vector2 position;
    public Vector2 size;
    public bool isHighlighted;
    public List<Vector2> walls;
}

public struct MinimapEdge
{
    public Vector2 start;
    public Vector2 end;
    public bool isBorder;
}

public static class MinimapGeometry
{
    private const float Epsilon = 0.001f;

    // Split each edge at neighboring edges so partial shared edges remain thin.
    public static List<MinimapEdge> BuildEdges(IReadOnlyList<MinimapTile> tiles)
    {
        var result = new List<MinimapEdge>();
        for (int i = 0; i < tiles.Count; i++)
        {
            MinimapTile tile = tiles[i];
            if (tile == null || tile.size.x <= 0f || tile.size.y <= 0f)
            {
                Debug.LogError("Minimap tiles must have positive width and depth.");
                continue;
            }

            for (int side = 0; side < 4; side++)
                AddSide(tiles, i, side, result);
        }
        return result;
    }

    private static void AddSide(
        IReadOnlyList<MinimapTile> tiles,
        int index,
        int side,
        List<MinimapEdge> result
    )
    {
        MinimapTile tile = tiles[index];
        bool horizontal = side < 2;
        bool positive = side == 1 || side == 3;
        float axis = horizontal ? tile.position.y : tile.position.x;
        float halfAxis = (horizontal ? tile.size.y : tile.size.x) * 0.5f;
        float fixedCoordinate = axis + (positive ? halfAxis : -halfAxis);
        float center = horizontal ? tile.position.x : tile.position.y;
        float halfLength = (horizontal ? tile.size.x : tile.size.y) * 0.5f;
        float start = center - halfLength;
        float end = center + halfLength;
        var overlaps = new List<Vector2>();
        var cuts = new List<float> { start, end };

        for (int i = 0; i < tiles.Count; i++)
        {
            MinimapTile other = tiles[i];
            if (i == index || other == null || other.size.x <= 0f || other.size.y <= 0f)
                continue;

            float otherAxis = horizontal ? other.position.y : other.position.x;
            float otherHalfAxis = (horizontal ? other.size.y : other.size.x) * 0.5f;
            float opposite = otherAxis + (positive ? -otherHalfAxis : otherHalfAxis);
            if (Mathf.Abs(fixedCoordinate - opposite) > Epsilon)
                continue;

            float otherCenter = horizontal ? other.position.x : other.position.y;
            float otherHalfLength = (horizontal ? other.size.x : other.size.y) * 0.5f;
            float overlapStart = Mathf.Max(start, otherCenter - otherHalfLength);
            float overlapEnd = Mathf.Min(end, otherCenter + otherHalfLength);
            if (overlapEnd - overlapStart <= Epsilon)
                continue;
            overlaps.Add(new Vector2(overlapStart, overlapEnd));
            cuts.Add(overlapStart);
            cuts.Add(overlapEnd);
        }

        cuts.Sort();
        for (int i = 0; i < cuts.Count - 1; i++)
        {
            float from = cuts[i];
            float to = cuts[i + 1];
            if (to - from <= Epsilon)
                continue;
            float midpoint = (from + to) * 0.5f;
            bool shared = false;
            foreach (Vector2 overlap in overlaps)
            {
                if (midpoint >= overlap.x && midpoint <= overlap.y)
                {
                    shared = true;
                    break;
                }
            }

            // The neighboring tile draws shared edges from its positive side.
            if (shared && !positive)
                continue;
            result.Add(new MinimapEdge
            {
                start = horizontal ? new Vector2(from, fixedCoordinate) : new Vector2(fixedCoordinate, from),
                end = horizontal ? new Vector2(to, fixedCoordinate) : new Vector2(fixedCoordinate, to),
                isBorder = !shared,
            });
        }
    }
}
