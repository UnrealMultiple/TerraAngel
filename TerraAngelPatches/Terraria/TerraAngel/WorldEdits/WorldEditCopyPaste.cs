extern alias TrProtocol;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using TerraAngel.ID;
using TerraAngel.Net;
using Terraria.Utilities;
using TrProtocol::TrProtocol.Models;

namespace TerraAngel.WorldEdits;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class WorldEditCopyPaste : WorldEdit
{
    public override bool RunEveryFrame => false;

    public TileSectionRenderer Renderer = new TileSectionRenderer();
    public TileSection? CopiedSection;
    private bool IsCopying = false;
    private Vector2 StartSelectTile;
    private int CurrentPlaceMode = 0;
    private bool DestroyTiles = false;

    public override void DrawPreviewInMap(ImGuiIOPtr io, ImDrawListPtr drawList)
    {
        Vector2 worldMouse = Util.ScreenToWorldFullscreenMap(InputSystem.MousePosition);
        Vector2 tileMouse = (worldMouse / 16f).Floor();

        if (InputSystem.IsKeyPressed(ClientConfig.Settings.WorldEditSelectKey))
        {
            IsCopying = true;

            StartSelectTile = tileMouse;
        }

        if (IsCopying)
        {
            var (vectl, vecbr) = ToInclusiveSelection(StartSelectTile, tileMouse);

            if (InputSystem.IsKeyDown(ClientConfig.Settings.WorldEditSelectKey))
            {
                drawList.DrawTileRect(vectl, vecbr, new Color(1f, 0f, 0f, 0.5f).PackedValue);
            }

            if (InputSystem.IsKeyUp(ClientConfig.Settings.WorldEditSelectKey))
            {
                IsCopying = false;
                Copy(vectl, vecbr);
            }
        }
        else
        {
            if (CopiedSection is not null)
            {
                Renderer.DrawPrimitiveMap(CopiedSection, tileMouse * 16f, Vector2.Zero, io.DisplaySize, DestroyTiles);

                drawList.AddRect(Util.WorldToScreenFullscreenMap(tileMouse * 16f), Util.WorldToScreenFullscreenMap((tileMouse + new Vector2(CopiedSection.Width, CopiedSection.Height)) * 16f), Color.LimeGreen.PackedValue, 0f, ImDrawFlags.None, 2f);
            }
        }
    }

    public override void DrawPreviewInWorld(ImGuiIOPtr io, ImDrawListPtr drawList)
    {
        Vector2 worldMouse = Util.ScreenToWorldWorld(InputSystem.MousePosition);
        Vector2 tileMouse = (worldMouse / 16f).Floor();

        if (InputSystem.IsKeyPressed(ClientConfig.Settings.WorldEditSelectKey))
        {
            IsCopying = true;

            StartSelectTile = tileMouse;
        }

        if (IsCopying)
        {
            var (vectl, vecbr) = ToInclusiveSelection(StartSelectTile, tileMouse);

            if (InputSystem.IsKeyDown(ClientConfig.Settings.WorldEditSelectKey))
            {
                drawList.DrawTileRect(vectl, vecbr, new Color(1f, 0f, 0f, 0.5f).PackedValue);
            }

            if (InputSystem.IsKeyUp(ClientConfig.Settings.WorldEditSelectKey))
            {
                IsCopying = false;
                Copy(vectl, vecbr);
            }
        }
        else
        {
            if (CopiedSection is not null)
            {
                Renderer.DrawDetailed(CopiedSection, Util.WorldToScreenWorld(tileMouse * 16f), Vector2.Zero, io.DisplaySize, DestroyTiles);

                drawList.AddRect(Util.WorldToScreenWorld(tileMouse * 16f), Util.WorldToScreenWorld((tileMouse + new Vector2(CopiedSection.Width, CopiedSection.Height)) * 16f), Color.LimeGreen.PackedValue, 0f, ImDrawFlags.None, 1f);
            }
        }
    }

    public override bool DrawUITab(ImGuiIOPtr io)
    {
        if (ImGui.BeginTabItem(GetString("Copy/Paste")))
        {
            ImGuiUtil.HelpMarkerTopRight(GetString($"Press {ClientConfig.Settings.WorldEditSelectKey:G} to copy\nPress middle mouse button to paste"));

            ImGui.Checkbox(GetString("Destroy Tiles"), ref DestroyTiles);
            ImGui.Text(GetString("Place Mode")); ImGui.SameLine(); ImGui.Combo("##PlaceMode", ref CurrentPlaceMode, PlaceModeNames, PlaceModeNames.Length);
            ImGui.EndTabItem();
            return true;
        }
        return false;
    }

    public override void Edit(Vector2 cursorTilePosition)
    {
        if (CopiedSection is null)
            return;
        switch ((PlaceMode)CurrentPlaceMode)
        {
            case PlaceMode.SendTileRect:
                EditSendTileRect(cursorTilePosition);
                break;
            case PlaceMode.TileManipulation:
                EditSendTileManipulation(cursorTilePosition);
                break;
        }
    }

    private void EditSendTileRect(Vector2 originTile)
    {
        if (CopiedSection is null)
            return;

        originTile = originTile.Floor();
        int ox = (int)originTile.X;
        int oy = (int)originTile.Y;
        Task.Run(() =>
        {
            CopyTilesForPass(ox, oy, true);
            CopyTilesForPass(ox, oy, false);

            // pass three, for framing and syncing
            for (int x = 0; x < CopiedSection.Width; x++)
            {
                for (int y = CopiedSection.Height - 1; y > -1; y--)
                {
                    if (!WorldGen.InWorld(ox + x, oy + y))
                        continue;

                    Tile? tile = Main.tile[ox + x, oy + y];
                    Tile? copiedTile = CopiedSection.Tiles?[x, y];

                    if (tile is null || copiedTile is null)
                        continue;

                    WorldGen.SquareTileFrame(ox + x, oy + y);
                    WorldGen.SquareWallFrame(ox + x, oy + y);

                    NetMessage.SendTileSquare(Main.myPlayer, ox + x, oy + y);
                }
            }
        });
    }


    private void EditSendTileManipulation(Vector2 originTile)
    {
        if (CopiedSection is null)
            return;

        originTile = originTile.Floor();
        int ox = (int)originTile.X;
        int oy = (int)originTile.Y;

        Task.Run(async () =>
        {
            if (CopiedSection is null)
                return;

            // TODO: custom delay
            var delayForThreshold = TimeSpan.FromSeconds(1) / 75;
            await using var pb = new PacketBuilder();

            // pass one
            for (int y = CopiedSection.Height - 1; y > -1; y--)
            {
                for (int x = 0; x < CopiedSection.Width; x++)
                {
                    var worldX = ox + x;
                    var worldY = oy + y;
                    if (!WorldGen.InWorld(worldX, worldY))
                        continue;

                    Tile tile = Main.tile[worldX, worldY];
                    Tile copiedTile = CopiedSection.Tiles[x, y];

                    if (tile == null || copiedTile == null)
                        continue;

                    bool isSolidCopiedTile = Main.tileSolid[copiedTile.type] &&
                                             copiedTile.type != TileID.GolfTee &&
                                             copiedTile.type != TileID.GolfHole &&
                                             copiedTile.type != TileID.GolfCupFlag;
                    if (!isSolidCopiedTile)
                        continue;

                    bool isCopiedTileEmpty = !(copiedTile.active() || copiedTile.wall > 0);
                    if (isCopiedTileEmpty && !DestroyTiles)
                        continue;

                    tile.CopyFrom(copiedTile);

                    // TODO: this is incomplete, many kill are not implemented
                    if (tile.active())
                    {
                        pb.WritePlayerPlaceTile(worldX, worldY, tile.type, resetToNormal: false);

                        if (tile.slope() > 0 || tile.halfBrick())
                        {
                            var slopeData = tile.halfBrick() ? 1 : tile.slope();
                            pb.WritePlayerSlopeTile(worldX, worldY, slopeData, resetToNormal: false);
                        }
                    }
                    else
                    {
                        pb.WritePlayerKillTile(worldX, worldY, resetToNormal: false);
                    }

                    if (tile.wall > 0)
                    {
                        pb.WritePlayerPlaceWall(worldX, worldY, tile.wall, resetToNormal: false);
                    }
                    else
                    {
                        pb.WritePlayerKillWall(worldX, worldY, resetToNormal: false);
                    }

                    if (tile.liquid > 0)
                    {
                        pb.WritePlayerUpdateLiquid(worldX, worldY, tile.liquidType(), tile.liquid, resetToNormal: false);
                    }

                    if (tile.color() > 0)
                    {
                        pb.WritePlayerPaintTile(worldX, worldY, tile.color(), 0, resetToNormal: false);
                    }

                    if (tile.wallColor() > 0)
                    {
                        pb.WritePlayerPaintWall(worldX, worldY, tile.wallColor(), 0, resetToNormal: false);
                    }

                    if (tile.wire())
                    {
                        pb.WritePlayerPlaceWire(worldX, worldY, 1, resetToNormal: false);
                    }

                    if (tile.wire2())
                    {
                        pb.WritePlayerPlaceWire(worldX, worldY, 2, resetToNormal: false);
                    }

                    if (tile.wire3())
                    {
                        pb.WritePlayerPlaceWire(worldX, worldY, 3, resetToNormal: false);
                    }

                    if (tile.wire4())
                    {
                        pb.WritePlayerPlaceWire(worldX, worldY, 4, resetToNormal: false);
                    }

                    if (tile.actuator())
                    {
                        pb.WritePlayerPlaceActuator(worldX, worldY, resetToNormal: false);
                    }

                    // reset to normal
                    pb.WriteSyncEquipmentPacketNormal(0);
                    pb.WritePlayerControlsPacketNormal();
                    pb.Send();
                    pb.Clear();

                    await Task.Delay(delayForThreshold);
                }
            }

            // TODO: pass two

            // pass three, for framing
            for (int y = CopiedSection.Height - 1; y > -1; y--)
            {
                for (int x = 0; x < CopiedSection.Width; x++)
                {
                    if (!WorldGen.InWorld(ox + x, oy + y))
                        continue;

                    Tile? tile = Main.tile[ox + x, oy + y];
                    Tile? copiedTile = CopiedSection.Tiles?[x, y];

                    if (tile is null || copiedTile is null)
                        continue;

                    WorldGen.SquareTileFrame(ox + x, oy + y);
                    WorldGen.SquareWallFrame(ox + x, oy + y);
                }
            }
        });
    }


    public void Copy(Vector2 startTile, Vector2 endTile)
    {
        float width = endTile.X - startTile.X;
        float height = endTile.Y - startTile.Y;

        //for (int x = ((int)startTile.X); x < ((int)endTile.X); x++)
        //{
        //    for (int y = ((int)startTile.Y); y < ((int)endTile.Y); y++)
        //    {
        //        WorldGen.SquareTileFrame(x, y);
        //        WorldGen.SquareWallFrame(x, y);
        //    }
        //}

        // if (width * height <= 0) return;

        CopiedSection = new TileSection(((int)startTile.X), ((int)startTile.Y), ((int)width), ((int)height));
    }

    private static (Vector2 vectl, Vector2 vecbr) ToInclusiveSelection(Vector2 vec1, Vector2 vec2)
    {
        var vectl = vec1;
        var vecbr = vec2;

        if (vecbr.X < vectl.X)
        {
            (vectl.X,  vecbr.X) = (vecbr.X, vectl.X);
        }

        if (vecbr.Y < vectl.Y)
        {
            (vectl.Y,  vecbr.Y) = (vecbr.Y, vectl.Y);
        }

        vecbr += Vector2.One;

        return (vectl, vecbr);
    }

    private void CopyTilesForPass(int ox, int oy, bool copySolidTiles)
    {
        if (CopiedSection is null)
            return;

        for (int x = 0; x < CopiedSection.Width; x++)
        {
            for (int y = CopiedSection.Height - 1; y > -1; y--)
            {
                if (!WorldGen.InWorld(ox + x, oy + y))
                    continue;

                Tile tile = Main.tile[ox + x, oy + y];
                Tile copiedTile = CopiedSection.Tiles[x, y];

                if (tile == null || copiedTile == null)
                    continue;

                bool isSolidCopiedTile = Main.tileSolid[copiedTile.type] &&
                                         copiedTile.type != TileID.GolfTee &&
                                         copiedTile.type != TileID.GolfHole &&
                                         copiedTile.type != TileID.GolfCupFlag;
                if (copySolidTiles != isSolidCopiedTile)
                    continue;

                bool isCopiedTileEmpty = !(copiedTile.active() || copiedTile.wall > 0);
                if (isCopiedTileEmpty && !DestroyTiles)
                    continue;

                tile.CopyFrom(copiedTile);
            }
        }
    }

    enum PlaceMode
    {
        SendTileRect,
        TileManipulation
    }

    private string[] PlaceModeNames =
    {
        GetString("Send tile rect"),
        GetString("Tile manipulation")
    };
}