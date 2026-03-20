using System.Reflection;

namespace TerraAngel.Utility;

public class TileUtil
{
    public static int[] TileToItem = new int[0];
    public static int[] WallToItem = new int[0];

    public static Color[] PaintColors = new Color[32];

    public static Color[] colorLookup = new Color[0];
    public static ushort[] wallLookup = new ushort[0];
    public static ushort[] tileLookup = new ushort[0];

    public static Color[] TileColor = new Color[0];
    public static Color[] WallColor = new Color[0];

    public static int GetItemFromTile(int type)
    {
        return GetPlacementItem(TileToItem, type);
    }

    public static int GetItemFromWall(int type)
    {
        return GetPlacementItem(WallToItem, type);
    }

    private static int GetPlacementItem(int[] lookup, int type)
    {
        if (type < 0 || type >= lookup.Length)
        {
            return -1;
        }

        return lookup[type];
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
        TileToItem = CreateTileLookup();
        WallToItem = CreateWallLookup();
    }

    private static int[] CreateTileLookup()
    {
        int[] lookup = new int[TileID.Count];
        System.Array.Fill(lookup, -1);

        foreach (Item item in ContentSamples.ItemsByType.Values)
        {
            int tileType = item.createTile;
            if (tileType < 0 || tileType >= lookup.Length)
            {
                continue;
            }

            if (ShouldReplacePlacementItem(lookup[tileType], item))
            {
                lookup[tileType] = item.type;
            }
        }

        return lookup;
    }

    private static int[] CreateWallLookup()
    {
        int[] lookup = new int[WallID.Count];
        System.Array.Fill(lookup, -1);

        foreach (Item item in ContentSamples.ItemsByType.Values)
        {
            int wallType = item.createWall;
            if (wallType < 0 || wallType >= lookup.Length)
            {
                continue;
            }

            if (ShouldReplacePlacementItem(lookup[wallType], item))
            {
                lookup[wallType] = item.type;
            }
        }

        return lookup;
    }

    private static bool ShouldReplacePlacementItem(int currentItemId, Item candidate)
    {
        if (candidate.IsAir)
        {
            return false;
        }

        if (currentItemId == -1)
        {
            return true;
        }

        Item current = ContentSamples.ItemsByType[currentItemId];
        int candidatePriority = GetPlacementItemPriority(candidate);
        int currentPriority = GetPlacementItemPriority(current);
        if (candidatePriority != currentPriority)
        {
            return candidatePriority < currentPriority;
        }

        return candidate.type < current.type;
    }

    private static int GetPlacementItemPriority(Item item)
    {
        // the smaller the value, the higher the priority
        int priority = 0;

        if (!item.consumable)
        {
            priority += 1000;
        }

        if (item.maxStack <= 1)
        {
            priority += 250;
        }

        if (item.tileWand >= 0)
        {
            priority += 500;
        }

        if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
        {
            priority += 500;
        }

        if (item.damage > 0)
        {
            priority += 250;
        }

        if (item.accessory)
        {
            priority += 250;
        }

        if (item.ammo > 0 || item.useAmmo > 0)
        {
            priority += 250;
        }

        if (item.useStyle <= 0)
        {
            priority += 100;
        }

        if (item.placeStyle != 0)
        {
            priority += 10;
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
