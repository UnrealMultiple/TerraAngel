﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Steamworks;
using Color = Microsoft.Xna.Framework.Color;
using ImGuiUtil = TerraAngel.Graphics.ImGuiUtil;
namespace TerraAngel.Tools.Developer;

public class ItemBrowserTool : Tool
{
    public static readonly string[] ItemGiveModeNames =
    [
        GetString("In mouse") ,
        GetString("Drop in world")
    ];

    public static readonly Vector2 ItemDrawSize = new Vector2(32, 32);

    public override string Name => GetString("Item Browser");

    public override ToolTabs Tab => base.Tab;

    public string ItemSearch = "";

    public ItemGiveMode GiveMode = ItemGiveMode.InMouse;

    public bool SyncWithServer = true;

    private QuickItemBrowserWindow QuickBrowserWindow;

    public ItemBrowserTool()
    {
        QuickBrowserWindow = new QuickItemBrowserWindow();
    }

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGuiStylePtr style = ImGui.GetStyle();

        ImGui.TextUnformatted(GetString("Search")); 
        ImGui.SameLine();

        ImGui.InputText("##ItemSearch", ref ItemSearch, 64);

        ImGui.TextUnformatted(GetString("Give Type")); 
        ImGui.SameLine();

        ImGui.PushItemWidth(MathF.Max(ImGui.GetContentRegionAvail().X / 3.4f, ImGui.CalcTextSize(ItemGiveModeNames[(int)GiveMode]).X + 30f));
        ImGui.Combo("##ItemGiveType", ref Unsafe.As<ItemGiveMode, int>(ref GiveMode), ItemGiveModeNames, ItemGiveModeNames.Length);

        ImGui.PopItemWidth();
        ImGui.SameLine();

        ImGui.Checkbox(GetString("Sync With Server"), ref SyncWithServer);
        bool searchEmpty = ItemSearch.Length == 0;

        if (ImGui.BeginChild("ItemBrowserScrolling"))
        {
            float windowMaxX = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f));
            for (int i = 1; i < ItemID.Count; i++)
            {
                if (searchEmpty || Lang.GetItemName(i).Value.ToLower().Contains(ItemSearch.ToLower()))
                {
                    if (ImGuiUtil.ItemButton(i, $"IBI{i}", true))
                    {
                        GiveItem(i);
                    }

                    float nextButtonX = ImGui.GetItemRectMax().X + style.ItemSpacing.X + ItemDrawSize.X;
                    if (i + 1 < ItemID.Count && nextButtonX < windowMaxX)
                    {
                        ImGui.SameLine();
                    }
                }
            }
            ImGui.PopStyleVar();
            ImGui.EndChild();
        }
    }

    public override void Update()
    {
        QuickBrowserWindow.Draw(ImGui.GetIO());
    }

    public void GiveItem(int type)
    {
        switch (GiveMode)
        {
            case ItemGiveMode.InMouse:
                ItemSpawner.SpawnItemInMouse(type, 9999, SyncWithServer);
                break;
            case ItemGiveMode.DropInWorld:
                ItemSpawner.SpawnItemInWorld(Main.LocalPlayer.Center, type, Vector2.Zero, 9999, SyncWithServer);
                break;
        }
    }

    public enum ItemGiveMode
    {
        InMouse,
        DropInWorld,
    }
}
