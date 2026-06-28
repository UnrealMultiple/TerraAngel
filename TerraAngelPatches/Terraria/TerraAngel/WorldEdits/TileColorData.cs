using Color = Microsoft.Xna.Framework.Color;
using System.Collections.Generic;
using System.Linq;

namespace TerraAngel.WorldEdits;

public readonly struct TileColor(int type, int paint, int wallType, int wallPaint, Color color, double[] lab)
{
    public readonly int Type = type;
    public readonly int Paint = paint;
    public readonly int WallType = wallType;
    public readonly int WallPaint = wallPaint;
    public readonly Color Color = color;
    public readonly double[] Lab = lab;
}

public static class TileColorData
{
    private static TileColor[]? _colors;

    public static readonly HashSet<int> SkippedTiles =
    [
        TileID.Bubble,
        TileID.ShimmerBlock,
        TileID.WaterBlock,

        //会被特殊处理的方块
        TileID.RainbowBrick,
        TileID.KryptonMossBlock,
        TileID.RainbowMossBlock,
        TileID.ArgonMossBlock,
        TileID.LavaMossBlock,
        TileID.VioletMossBlock,
        TileID.XenonMossBlock,

        TileID.GolfHole,

        // 无色/透明方块
        TileID.EchoBlock,
        TileID.AntiPortalBlock,

        // 无法放置的特殊方块
        TileID.InactiveStoneBlock,
        TileID.BoulderBlock,
        TileID.DamagingSpikeBlock,
        TileID.RollingCactus,
        // TileID.RainbowBoulder,
        // TileID.Poulder,
        // TileID.LavaBoulder,
        // TileID.SpiderBoulder,
        // TileID.Ghoulder,
        // TileID.BoulderThatSpawnsPet,
        // TileID.BouncyBoulder,

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

        // 太丑的方块
        // TileID.Shimmerfall,
        // TileID.Lavafall,
        // TileID.Waterfall,
        // TileID.RainbowCloud,
        // TileID.RainbowMoss,
        // TileID.RainbowMossBlock,
        // TileID.RainbowMossBrick,
        TileID.MushroomGrass,

        // 远程放置需要特殊处理的方块
        // TODO: fix MossBrick
        // those commented out are handled in PacketBuilderExtensions.WritePlayerPlaceTile
        // TileID.GreenMoss,
        // TileID.BrownMoss,
        // TileID.RedMoss,
        // TileID.BlueMoss,
        // TileID.PurpleMoss,
        // TileID.LongMoss,
        // TileID.LavaMoss,
        // TileID.KryptonMoss,
        // TileID.XenonMoss,
        // TileID.ArgonMoss,
        // TileID.VioletMoss,
        // TileID.RainbowMoss,

        // TileID.Grass,
        // TileID.CorruptGrass,
        // TileID.JungleGrass,
        // TileID.MushroomGrass,
        // TileID.HallowedGrass,
        // TileID.CrimsonGrass,
        TileID.GolfGrass,
        TileID.GolfGrassHallowed,
        // TileID.AshGrass,
        // TileID.CorruptJungleGrass,
        // TileID.CrimsonJungleGrass,
    ];

    public static readonly HashSet<int> SkippedWalls =
    [
        // unsafe walls
        WallID.DirtUnsafe,
        WallID.EbonstoneUnsafe,
        WallID.BlueDungeonUnsafe,
        WallID.GreenDungeonUnsafe,
        WallID.PinkDungeonUnsafe,
        WallID.HellstoneBrickUnsafe,
        WallID.ObsidianBrickUnsafe,
        WallID.MudUnsafe,
        WallID.PearlstoneBrickUnsafe,
        WallID.SnowWallUnsafe,
        WallID.AmethystUnsafe,
        WallID.TopazUnsafe,
        WallID.SapphireUnsafe,
        WallID.EmeraldUnsafe,
        WallID.RubyUnsafe,
        WallID.DiamondUnsafe,
        WallID.CaveUnsafe,
        WallID.Cave2Unsafe,
        WallID.Cave3Unsafe,
        WallID.Cave4Unsafe,
        WallID.Cave5Unsafe,
        WallID.Cave6Unsafe,
        WallID.Cave7Unsafe,
        WallID.SpiderUnsafe,
        WallID.GrassUnsafe,
        WallID.JungleUnsafe,
        WallID.FlowerUnsafe,
        WallID.CorruptGrassUnsafe,
        WallID.HallowedGrassUnsafe,
        WallID.IceUnsafe,
        WallID.ObsidianBackUnsafe,
        WallID.MushroomUnsafe,
        WallID.CrimsonGrassUnsafe,
        WallID.CrimstoneUnsafe,
        WallID.HiveUnsafe,
        WallID.LihzahrdBrickUnsafe,
        WallID.BlueDungeonSlabUnsafe,
        WallID.BlueDungeonTileUnsafe,
        WallID.PinkDungeonSlabUnsafe,
        WallID.PinkDungeonTileUnsafe,
        WallID.GreenDungeonSlabUnsafe,
        WallID.GreenDungeonTileUnsafe,
        WallID.MarbleUnsafe,
        WallID.GraniteUnsafe,
        WallID.Cave8Unsafe,
        WallID.CorruptionUnsafe1,
        WallID.CorruptionUnsafe2,
        WallID.CorruptionUnsafe3,
        WallID.CorruptionUnsafe4,
        WallID.CrimsonUnsafe1,
        WallID.CrimsonUnsafe2,
        WallID.CrimsonUnsafe3,
        WallID.CrimsonUnsafe4,
        WallID.DirtUnsafe1,
        WallID.DirtUnsafe2,
        WallID.DirtUnsafe3,
        WallID.DirtUnsafe4,
        WallID.HallowUnsafe1,
        WallID.HallowUnsafe2,
        WallID.HallowUnsafe3,
        WallID.HallowUnsafe4,
        WallID.JungleUnsafe1,
        WallID.JungleUnsafe2,
        WallID.JungleUnsafe3,
        WallID.JungleUnsafe4,
        WallID.LavaUnsafe1,
        WallID.LavaUnsafe2,
        WallID.LavaUnsafe3,
        WallID.LavaUnsafe4,
        WallID.RocksUnsafe1,
        WallID.RocksUnsafe2,
        WallID.RocksUnsafe3,
        WallID.RocksUnsafe4,
        WallID.LivingWoodUnsafe,
        WallID.StoneUnsafe,
    ];

    public static TileColor[] Colors => _colors ??= GenerateTileColors();

    public static void InvalidateCache()
    {
        _colors = null;
    }

    private static TileColor[] GenerateTileColors()
    {
        var colors = new Dictionary<Color, TileColor>();

        for (int tileType = 0; tileType < TileID.Count; tileType++)
        {
            if (!Main.tileSolid[tileType])
                continue;

            if (Main.tileSolidTop[tileType])
                continue;

            // skip all falling blocks, e.g. sand
            if (TileID.Sets.Falling[tileType])
                continue;

            // skip all boulders
            if (TileID.Sets.Boulders[tileType])
                continue;

            if (SkippedTiles.Contains(tileType))
                continue;

            if (TileUtil.GetItemFromTile(tileType) == ItemID.None)
                continue;

            for (int paint = 31; paint >= 0; paint--)
            {
                // TODO: make illuminate paint more accurate
                if (paint == PaintID.IlluminantPaint)
                    continue;

                Color color = TileUtil.GetTileColor(tileType, paint);
                if (color.A != 255)
                    continue;
                if (colors.TryGetValue(color, out var exists) &&
                    (exists.Type == -1 || exists.Paint == 0) &&
                    (exists.WallType == -1 || exists.WallPaint == 0))
                    continue;
                double[] lab = ColorUtil.RgbToLab(color);
                colors[color] = new TileColor(tileType, paint, -1, 0, color, lab);
            }
        }

        for (int wallType = WallID.Stone; wallType < WallID.Count; wallType++)
        {
            if (SkippedWalls.Contains(wallType))
                continue;

            if (TileUtil.GetItemFromWall(wallType) == ItemID.None)
                continue;

            for (int paint = 31; paint >= 0; paint--)
            {
                // TODO: make illuminate paint more accurate
                if (paint == PaintID.IlluminantPaint)
                    continue;

                Color color = TileUtil.GetWallColor(wallType, paint);
                if (color.A != 255)
                    continue;
                if (colors.TryGetValue(color, out var exists) &&
                    (exists.Type == -1 || exists.Paint == 0) &&
                    (exists.WallType == -1 || exists.WallPaint == 0))
                    continue;
                double[] lab = ColorUtil.RgbToLab(color);
                colors[color] = new TileColor(-1, 0, wallType, paint, color, lab);
            }
        }

        return colors.Values.ToArray();
    }
}
