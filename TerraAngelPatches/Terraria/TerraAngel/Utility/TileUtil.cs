using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ObjectData;

namespace TerraAngel.Utility;

public class TileUtil
{
    private const int DefaultPlacementStyle = -1;

    public static Dictionary<(int Type, int Style), int> TileToItem = [];
    public static int[] WallToItem = [];

    public static Color[] PaintColors = new Color[32];

    public static Color[] colorLookup = [];
    public static ushort[] wallLookup = [];
    public static ushort[] tileLookup = [];

    public static Color[] TileColor = [];
    public static Color[] WallColor = [];

    public static int GetItemFromTile(int type)
    {
        return GetPlacementItem(TileToItem, type, DefaultPlacementStyle, allowDefaultFallback: false);
    }

    public static int GetItemFromTile(int type, int style)
    {
        return GetPlacementItem(TileToItem, type, style, allowDefaultFallback: true);
    }

    public static int GetItemFromTile(Tile tile)
    {
        if (!tile.active())
        {
            return ItemID.None;
        }

        return GetItemFromTile(tile.type, GetTilePlacementStyle(tile));
    }

    public static int GetItemFromWall(int type)
    {
        return GetPlacementItem(WallToItem, type);
    }

    public static int GetItemFromWall(Tile tile)
    {
        if (tile.wall <= 0)
        {
            return ItemID.None;
        }

        return GetItemFromWall(tile.wall);
    }

    public static int GetItemFromLiquid(int type)
    {
        return type switch
        {
            LiquidID.Water => ItemID.WaterBucket,
            LiquidID.Honey => ItemID.HoneyBucket,
            LiquidID.Lava => ItemID.LavaBucket,
            LiquidID.Shimmer => ItemID.BottomlessShimmerBucket,
            _ => ItemID.None
        };
    }

    private static int GetPlacementItem(Dictionary<(int Type, int Style), int> lookup, int type, int style, bool allowDefaultFallback)
    {
        if (type < 0)
        {
            return ItemID.None;
        }

        if (style == DefaultPlacementStyle)
        {
            return lookup.GetValueOrDefault((type, DefaultPlacementStyle), ItemID.None);
        }

        style = Math.Max(style, 0);
        if (lookup.TryGetValue((type, style), out int itemId))
        {
            return itemId;
        }

        return allowDefaultFallback
            ? lookup.GetValueOrDefault((type, DefaultPlacementStyle), ItemID.None)
            : ItemID.None;
    }

    private static int GetPlacementItem(int[] lookup, int type)
    {
        if (type < 0 || type >= lookup.Length)
        {
            return ItemID.None;
        }

        return lookup[type];
    }

    private static int GetTilePlacementStyle(Tile tile)
    {
        TileObjectData? tileData = TileObjectData.GetTileData(tile);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (tileData == null || tileData.CoordinateFullWidth <= 0 || tileData.CoordinateFullHeight <= 0)
        {
            return DefaultPlacementStyle;
        }

        int xStyle = tile.frameX / tileData.CoordinateFullWidth;
        int yStyle = tile.frameY / tileData.CoordinateFullHeight;
        int styleWrapLimit = tileData.StyleWrapLimit;
        if (styleWrapLimit == 0)
        {
            styleWrapLimit = 1;
        }

        int style = tileData.StyleHorizontal ? yStyle * styleWrapLimit + xStyle : xStyle * styleWrapLimit + yStyle;
        int styleMultiplier = tileData.StyleMultiplier;
        if (styleMultiplier <= 0)
        {
            styleMultiplier = 1;
        }

        style /= styleMultiplier;

        int styleLineSkip = tileData.StyleLineSkip;
        if (styleLineSkip > 1)
        {
            style = tileData.StyleHorizontal ? yStyle / styleLineSkip * styleWrapLimit + xStyle : xStyle / styleLineSkip * styleWrapLimit + yStyle;
        }

        return style;
    }

    public static Color GetWallColor(int type)
    {
        return WallColor[type];
    }
    public static Color GetWallColor(int type, int paint)
    {
        Color wallColor = WallColor[type];
        switch (paint)
        {
            case 29:
                {
                    Color color = PaintColors[paint];
                    float num = (float)(int)wallColor.R / 255f;
                    float num2 = (float)(int)wallColor.G / 255f;
                    float num3 = (float)(int)wallColor.B / 255f;
                    if (num2 > num)
                    {
                        float num4 = num;
                        num = num2;
                        num2 = num4;
                    }
                    if (num3 > num)
                    {
                        float num5 = num;
                        num = num3;
                        num3 = num5;
                    }
                    float num7 = num3 * 0.3f;
                    wallColor.R = (byte)((float)(int)color.R * num7);
                    wallColor.G = (byte)((float)(int)color.G * num7);
                    wallColor.B = (byte)((float)(int)color.B * num7);
                    break;
                }
            case 30:
                {
                    wallColor.R = (byte)((255 - wallColor.R) / 2);
                    wallColor.G = (byte)((255 - wallColor.G) / 2);
                    wallColor.B = (byte)((255 - wallColor.B) / 2);
                }
                break;
            case 31:
            case 0:
                break;
            default:
                {
                    Color color = PaintColors[paint];

                    float num = (float)(int)wallColor.R / 255f;
                    float num2 = (float)(int)wallColor.G / 255f;
                    float num3 = (float)(int)wallColor.B / 255f;
                    if (num2 > num)
                    {
                        float num4 = num;
                        num = num2;
                    }
                    if (num3 > num)
                    {
                        float num5 = num;
                        num = num3;
                    }
                    float num6 = num;
                    wallColor.R = (byte)((float)color.R * num6);
                    wallColor.G = (byte)((float)color.G * num6);
                    wallColor.B = (byte)((float)color.B * num6);
                    break;
                }
        }
        return wallColor;
    }

    public static Color GetTileColor(int type)
    {
        return TileColor[type];
    }
    public static Color GetTileColor(int type, int paint)
    {
        Color tileColor = TileColor[type];

        switch (paint)
        {
            case 29:
                {
                    Color color = PaintColors[paint];
                    float num = (float)(int)tileColor.R / 255f;
                    float num2 = (float)(int)tileColor.G / 255f;
                    float num3 = (float)(int)tileColor.B / 255f;
                    if (num2 > num)
                    {
                        float num4 = num;
                        num = num2;
                        num2 = num4;
                    }
                    if (num3 > num)
                    {
                        float num5 = num;
                        num = num3;
                        num3 = num5;
                    }
                    float num7 = num3 * 0.3f;
                    tileColor.R = (byte)((float)(int)color.R * num7);
                    tileColor.G = (byte)((float)(int)color.G * num7);
                    tileColor.B = (byte)((float)(int)color.B * num7);
                    break;
                }
            case 30:
                {
                    tileColor.R = (byte)(255 - tileColor.R);
                    tileColor.G = (byte)(255 - tileColor.G);
                    tileColor.B = (byte)(255 - tileColor.B);
                }
                break;
            case 31:
            case 0:
                break;
            default:
                {
                    Color color = PaintColors[paint];

                    float num = (float)(int)tileColor.R / 255f;
                    float num2 = (float)(int)tileColor.G / 255f;
                    float num3 = (float)(int)tileColor.B / 255f;
                    if (num2 > num)
                    {
                        float num4 = num;
                        num = num2;
                    }
                    if (num3 > num)
                    {
                        float num5 = num;
                        num = num3;
                    }
                    float num6 = num;
                    tileColor.R = (byte)((float)color.R * num6);
                    tileColor.G = (byte)((float)color.G * num6);
                    tileColor.B = (byte)((float)color.B * num6);
                    break;
                }
        }
        return tileColor;
    }

    private static void LoadPrivateArrays()
    {
        colorLookup = (Color[])((Color[])typeof(Terraria.Map.MapHelper).GetField("colorLookup", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!).Clone();
        wallLookup = (ushort[])((ushort[])typeof(Terraria.Map.MapHelper).GetField("wallLookup", BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!).Clone();
        tileLookup = (ushort[])((ushort[])typeof(Terraria.Map.MapHelper).GetField("tileLookup", BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!).Clone();
    }
    private static void LoadDefaultData()
    {
        TileColor = new Color[tileLookup.Length];
        WallColor = new Color[wallLookup.Length];

        for (int i = 0; i < TileColor.Length; i++)
        {
            TileColor[i] = colorLookup[tileLookup[i]];
        }

        for (int i = 0; i < WallColor.Length; i++)
        {
            WallColor[i] = colorLookup[wallLookup[i]];
        }

        for (int i = 0; i < PaintColors.Length; i++)
        {
            PaintColors[i] = WorldGen.paintColor(i);
        }
    }

    private static void LoadPlacementLookups()
    {
        TileToItem = CreatePlacementLookup(TileID.Count, item => item.createTile);
        WallToItem = CreatePlacementLookupArray(WallID.Count, item => item.createWall);
    }

    private static Dictionary<(int Type, int Style), int> CreatePlacementLookup(int placementCount, System.Func<Item, int> getPlacementType)
    {
        Dictionary<(int Type, int Style), int> lookup = new Dictionary<(int Type, int Style), int>();

        foreach (Item item in ContentSamples.ItemsByType.Values)
        {
            int placementType = getPlacementType(item);
            if (placementType < 0 || placementType >= placementCount)
            {
                continue;
            }

            SetPlacementItem(lookup, (placementType, DefaultPlacementStyle), item);
            SetPlacementItem(lookup, (placementType, Math.Max(item.placeStyle, 0)), item);
        }

        return lookup;
    }

    private static int[] CreatePlacementLookupArray(int placementCount, System.Func<Item, int> getPlacementType)
    {
        int[] lookup = new int[placementCount];
        Array.Fill(lookup, ItemID.None);

        foreach (Item item in ContentSamples.ItemsByType.Values)
        {
            int placementType = getPlacementType(item);
            if (placementType < 0 || placementType >= placementCount)
            {
                continue;
            }

            if (!ShouldReplacePlacementItem(lookup[placementType], item))
            {
                continue;
            }

            lookup[placementType] = item.type;
        }

        return lookup;
    }

    private static void SetPlacementItem(Dictionary<(int Type, int Style), int> lookup, (int Type, int Style) key, Item candidate)
    {
        int currentItemId = ItemID.None;
        if (lookup.TryGetValue(key, out int existingItemId))
        {
            currentItemId = existingItemId;
        }

        if (!ShouldReplacePlacementItem(currentItemId, candidate))
        {
            return;
        }

        lookup[key] = candidate.type;
    }

    private static bool ShouldReplacePlacementItem(int currentItemId, Item candidate)
    {
        if (candidate.IsAir)
        {
            return false;
        }

        if (currentItemId == ItemID.None)
        {
            return true;
        }

        Item current = ContentSamples.ItemsByType[currentItemId];
        int currentPriority = GetPlacementItemPriority(current);
        int candidatePriority = GetPlacementItemPriority(candidate);
        if (currentPriority != candidatePriority)
        {
            return currentPriority < candidatePriority;
        }

        return candidate.type < current.type;
    }

    private static int GetPlacementItemPriority(Item item)
    {
        // the larger the value, the higher the priority
        int priority = 0;

        if (!item.consumable)
        {
            priority -= 1000;
        }

        if (item.maxStack <= 1)
        {
            priority -= 250;
        }

        if (item.tileWand >= 0)
        {
            priority -= 500;
        }

        if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
        {
            priority -= 500;
        }

        if (item.damage > 0)
        {
            priority -= 250;
        }

        if (item.accessory)
        {
            priority -= 250;
        }

        if (item.ammo > 0 || item.useAmmo > 0)
        {
            priority -= 250;
        }

        if (item.useStyle <= 0)
        {
            priority -= 100;
        }

        if (item.placeStyle != 0)
        {
            priority -= 10;
        }

        return priority;
    }

    public static void Init()
    {
        LoadPrivateArrays();
        LoadDefaultData();
        LoadPlacementLookups();
    }
}
