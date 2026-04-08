using Color = Microsoft.Xna.Framework.Color;
using System.Collections.Generic;

namespace TerraAngel.WorldEdits;

public readonly struct TileColor(int type, int paint, Color color, double labL, double labA, double labB)
{
    public readonly int Type = type;
    public readonly int Paint = paint;
    public readonly Color Color = color;
    public readonly double LabL = labL;
    public readonly double LabA = labA;
    public readonly double LabB = labB;

    public double[] GetLab() => new[] { LabL, LabA, LabB };
}

public static class TileColorData
{
    private static TileColor[]? _colors;

    private static readonly HashSet<int> SkippedTiles = new()
    {
        // 液体方块
        TileID.Bubble,
        TileID.ShimmerBlock,
        TileID.WaterBlock,

        // 重力方块 (会下落)
        TileID.SandFallBlock,
        TileID.SnowFallBlock,

        // 无色/透明方块
        TileID.EchoBlock,
        TileID.AntiPortalBlock,

        // 无法放置的特殊方块
        TileID.InactiveStoneBlock,
        TileID.BoulderBlock,
        TileID.DamagingSpikeBlock,

        // 腐化/猩红之地的祭坛和暗影珠/猩红之心
        TileID.DemonAltar,
        TileID.ShadowOrbs,

        // 生命水晶和生命果
        TileID.Heart,
        TileID.LifeFruit,

        // 门和闸门
        TileID.ClosedDoor,
        TileID.OpenDoor,
        TileID.TrapdoorOpen,
        TileID.TrapdoorClosed,
        TileID.TallGateClosed,
        TileID.TallGateOpen,
    };

    public static TileColor[] Colors
    {
        get
        {
            _colors ??= GenerateTileColors();
            return _colors;
        }
    }

    public static void InvalidateCache()
    {
        _colors = null;
    }

    private static TileColor[] GenerateTileColors()
    {
        var colors = new List<TileColor>();

        for (int tileType = 0; tileType < TileUtil.TileColor.Length; tileType++)
        {
            if (tileType < Main.tileSolid.Length && !Main.tileSolid[tileType])
                continue;

            if (SkippedTiles.Contains(tileType))
                continue;

            for (int paint = 0; paint < 32; paint++)
            {
                Color color = TileUtil.GetTileColor(tileType, paint);
                double[] lab = ColorUtil.RgbToLab(color);
                colors.Add(new TileColor(tileType, paint, color, lab[0], lab[1], lab[2]));
            }
        }

        return colors.ToArray();
    }
}
