﻿using System;
using System.IO;
using TerraAngel.Inspector.Tools;
using Terraria;
using Terraria.Net.Sockets;
using ImGuiUtil = TerraAngel.Graphics.ImGuiUtil;
using Terraria.ID;

namespace TerraAngel.Tools.Inspector;

public class ItemInspectorTool : InspectorTool
{
    public override string Name => GetString("Item Inspector");

    private int SelectedItemIndex = -1;

    private Item? SelectedItem => SelectedItemIndex > -1 ? Main.item[SelectedItemIndex] : null;

    private bool ShowTooltip = true;

    public override void DrawMenuBar(ImGuiIOPtr io)
    {
        DrawItemSelectMenu(out ShowTooltip);

        if (SelectedItem is null)
        {
            return;
        }

        if (ImGui.Button($"{Icon.Move}"))
        {
            Main.LocalPlayer.velocity = Vector2.Zero;
            Main.LocalPlayer.Teleport(SelectedItem.position, TeleportationStyleID.RodOfDiscord);

            NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);

            if (ClientConfig.Settings.TeleportSendRODPacket)
            {
                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null,
                    0,
                    Main.LocalPlayer.whoAmI,
                    Main.LocalPlayer.position.X,
                    Main.LocalPlayer.position.Y,
                    TeleportationStyleID.RodOfDiscord);
            }
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(GetString($"Teleport to \"{SelectedItem.Name.Truncate(30)}\""));
            ImGui.EndTooltip();
        }

        if (ImGui.Button($"{Icon.CircleSlash}"))
        {
            //Taken from ZaZaClient
            if (SelectedItem.active && SelectedItem.type > 0 && !SelectedItem.beingGrabbed)
            {
                SelectedItem.active = false;
                SelectedItem.stack = 0;
                if (Main.netMode == 1)
                {
                    Util.FalsePlayerPacket(new Vector2(SelectedItem.position.X, SelectedItem.position.Y));
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, SelectedItem.netID, 0f, 0f, 0f, 0, 0, 0);
                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Main.myPlayer, 0f, 0f, 0f, 0, 0, 0);
                }
            }
            ClientLoader.Console.WriteLine($"Item \"{SelectedItem.Name.Truncate(30)}\" removed");
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(GetString($"Kill \"{SelectedItem.Name.Truncate(30)}\""));
            ImGui.EndTooltip();
        }
    }

    public override void DrawInspector(ImGuiIOPtr io)
    {
        if (SelectedItem is null)
        {
            return;
        }

        ImGui.Text(GetString($"Inspecting Item[{SelectedItemIndex}] \"{SelectedItem.Name.Truncate(60)}\""));
        ImGui.Text(GetString($"Position:    {SelectedItem.position}"));
        ImGui.Text(GetString($"Speed:       {SelectedItem.velocity.Length()}"));
        ImGui.Text(GetString($"Velocity:    {SelectedItem.velocity}"));
        ImGui.Text(GetString($"Velocity Dir: "));

        if (SelectedItem.velocity.Length() > 0f)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            Vector2 center = new Vector2(ImGui.GetItemRectMax().X, ImGui.GetItemRectMin().Y) + new Vector2(ImGui.GetItemRectSize().Y / 2f);
            Matrix3x2 rotationMatrix = Matrix3x2.CreateRotation(SelectedItem.velocity.SafeNormalize(Vector2.Zero).AngleTo(Vector2.Zero) + MathF.PI / 2f);

            Vector2 head = center + Vector2.Transform(new Vector2(0f, ImGui.GetTextLineHeight() / 2f), rotationMatrix);
            Vector2 tail = center + Vector2.Transform(new Vector2(0f, -ImGui.GetTextLineHeight() / 2f), rotationMatrix);
            Vector2 tri1 = center + Vector2.Transform(new Vector2(0f, ImGui.GetTextLineHeight() / 2f), rotationMatrix);
            Vector2 tri2 = center + Vector2.Transform(new Vector2(ImGui.GetTextLineHeight() / 7f, ImGui.GetTextLineHeight() / 4f), rotationMatrix);
            Vector2 tri3 = center + Vector2.Transform(new Vector2(-ImGui.GetTextLineHeight() / 7f, ImGui.GetTextLineHeight() / 4f), rotationMatrix);

            drawList.AddLine(head, tail, Color.Red.PackedValue);
            drawList.AddTriangle(tri1, tri2, tri3, Color.Red.PackedValue);
            drawList.AddTriangle(tri1, tri2, tri3, Color.Red.PackedValue);
            drawList.AddTriangleFilled(tri1, tri2, tri3, Color.Red.PackedValue);
        }

        if (Main.netMode == 1 && SelectedItem.active)
        {
            ImGui.Text(GetString($"Owned By:  {SelectedItem.playerIndexTheItemIsReservedFor switch
            {
                >= 255 => GetString("None/Server"),
                >= 0 => $"{Main.player[SelectedItem.playerIndexTheItemIsReservedFor].name}",
                _ => GetString("None/Server"),
            }}/{SelectedItem.playerIndexTheItemIsReservedFor}"));
        }

        ImGuiUtil.ItemButton(SelectedItem, "InspectorItem", new Vector2(32f), ShowTooltip);
    }

    private void DrawItemSelectMenu(out bool showTooltip)
    {
        showTooltip = true;

        if (ImGui.BeginMenu(GetString("Items")))
        {
            for (int i = 0; i < 400; i++)
            {
                bool anyActiveItems = false;
                for (int j = i; j < Math.Min(i + 20, 400); j++)
                {
                    if (Main.item[j].active)
                        anyActiveItems = true;
                }

                ImGui.BeginDisabled(!anyActiveItems);
                if (ImGui.BeginMenu(GetString($"Items {i}-{Math.Min(i + 20, 400)}")))
                {
                    showTooltip = false;
                    ImGui.BeginDisabled(false);
                    for (int j = i; j < Math.Min(i + 20, 400); j++)
                    {
                        ImGui.PushID(j);
                        if (!Main.item[j].active) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] * new Vector4(1f, 1f, 1f, 0.4f));

                        if (ImGui.MenuItem(GetString($"Item \"{Main.item[j].Name.Truncate(60)}\"")))
                        {
                            SelectedItemIndex = j;
                        }

                        if (!Main.item[j].active) ImGui.PopStyleColor();
                        ImGui.PopID();
                    }
                    ImGui.EndDisabled();
                    ImGui.EndMenu();
                }
                ImGui.EndDisabled();
                i += 20;
            }
            ImGui.EndMenu();
        }
    }

    public override void UpdateInGameSelect()
    {
        for (int i = 0; i < 400; i++)
        {
            Item item = Main.item[i];
            if (item.active)
            {
                Microsoft.Xna.Framework.Rectangle drawHitbox = Item.GetDrawHitbox(item.type, null);
                Vector2 bottom = item.Bottom;
                Microsoft.Xna.Framework.Rectangle selectRect = new Microsoft.Xna.Framework.Rectangle((int)(bottom.X - (float)drawHitbox.Width * 0.5f), (int)(bottom.Y - (float)drawHitbox.Height), drawHitbox.Width, drawHitbox.Height);

                if (InputSystem.RightMousePressed && selectRect.Contains(Util.ScreenToWorldWorld(InputSystem.MousePosition).ToPoint()))
                {
                    SelectedItemIndex = i;
                    InspectorWindow.OpenTab(this);
                    break;
                }
            }
        }
    }
}
