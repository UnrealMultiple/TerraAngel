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

    public TileSectionRenderer Renderer = new();
    public TileSectionPaster Paster = new();
    public TileSection? CopiedSection;
    private bool IsCopying = false;
    private Vector2 StartSelectTile;
    private int CurrentPlaceMode = 0;
    private bool DestroyTiles = false;

    public void PreUpdate()
    {
        Renderer.PreUpdate();
    }

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
                Renderer.DrawPrimitiveMap(CopiedSection, tileMouse * 16f, Vector2.Zero, io.DisplaySize, DestroyTiles, enableCaching: true);

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
        var originTile = cursorTilePosition.Floor();

        switch ((PlaceMode)CurrentPlaceMode)
        {
            case PlaceMode.SendTileRect:
                Paster.PasteBySendTileRectInNewTask(CopiedSection, originTile, DestroyTiles);
                break;
            case PlaceMode.TileManipulation:
                Paster.PasteByTileManipulationInNewTask(CopiedSection, originTile, DestroyTiles, 75);
                break;
        }
    }

    public void Copy(Vector2 startTile, Vector2 endTile)
    {
        startTile.X = Math.Clamp(startTile.X, 0, Main.maxTilesX - 1);
        startTile.Y = Math.Clamp(startTile.Y, 0, Main.maxTilesY - 1);
        endTile.X = Math.Clamp(endTile.X, 0, Main.maxTilesX - 1);
        endTile.Y = Math.Clamp(endTile.Y, 0, Main.maxTilesY - 1);

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

        CopiedSection = new TileSection((int)startTile.X, (int)startTile.Y, (int)width, (int)height);
        Renderer.InvalidateDrawPrimitiveMapCache();
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