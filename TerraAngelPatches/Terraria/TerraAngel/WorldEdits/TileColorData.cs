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

    public double[] GetLab() => [LabL, LabA, LabB];
}

public static class TileColorData
{
    private static TileColor[]? _colors;

    private static readonly HashSet<int> SkippedTiles =
    [
        TileID.Bubble,
        TileID.ShimmerBlock,
        TileID.WaterBlock,

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

        // 会因底部方块不完整而被破坏的方块
        TileID.Teleporter,
        TileID.MetalBars,
    ];

    public static TileColor[] Colors => _colors ??= GenerateTileColors();

    public static void InvalidateCache()
    {
        _colors = null;
    }

    private static TileColor[] GenerateTileColors()
    {
        var colors = new List<TileColor>();

        for (int tileType = 0; tileType < TileID.Count; tileType++)
        {
            if (!Main.tileSolid[tileType])
                continue;

            // skip all falling blocks, e.g. sand
            if (TileID.Sets.Falling[tileType])
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
