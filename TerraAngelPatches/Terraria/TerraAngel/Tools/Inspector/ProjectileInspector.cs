﻿using System;
using System.Collections.Generic;
using TerraAngel.Inspector.Tools;
using Terraria.GameContent;

namespace TerraAngel.Tools.Inspector;

public class ProjectileInspector : InspectorTool
{
    private static readonly Dictionary<int, nint> BoundProjectileTextures = new Dictionary<int, nint>();

    public override string Name => GetString("Projectile Inspector");

    private int SelectedProjectileIndex = -1;

    private Projectile? SelectedProjectile => SelectedProjectileIndex > -1 ? Main.projectile[SelectedProjectileIndex] : null;

    private Projectile DefaultProjectiledCache = new Projectile();

    public override void DrawMenuBar(ImGuiIOPtr io)
    {
        DrawProjectileSelectMenu(out _);

        if (SelectedProjectile is null)
        {
            return;
        }

        if (ImGui.Button($"{Icon.Move}"))
        {
            Main.LocalPlayer.velocity = Vector2.Zero;
            Main.LocalPlayer.Teleport(SelectedProjectile.position, TeleportationStyleID.RodOfDiscord);

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
            ImGui.Text(GetString($"Teleport to \"{SelectedProjectile.Name.Truncate(30)}\""));
            ImGui.EndTooltip();
        }

        if (ImGui.Button($"{Icon.CircleSlash}"))
        {
            ClientLoader.Console.WriteError(GetString("Not implemented yet"));
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(GetString($"Kill \"{SelectedProjectile.Name.Truncate(30)}\""));
            ImGui.Text(GetString($"*Not implemented yet"));
            ImGui.EndTooltip();
        }
    }

    public override void DrawInspector(ImGuiIOPtr io)
    {
        if (SelectedProjectile is null)
        {
            return;
        }

        DefaultProjectiledCache.SetDefaults(SelectedProjectile.type);

        ImGui.Text(GetString($"Inspecting Projectile[{SelectedProjectileIndex}] \"{SelectedProjectile.Name.Truncate(60)}\"/{InternalRepresentation.GetProjectileIDName(SelectedProjectile.type)}/{SelectedProjectile.type}"));
        ImGui.Text(GetString($"Damage:      {SelectedProjectile.damage}"));
        ImGui.Text(GetString($"Hostile:     {SelectedProjectile.hostile}"));
        ImGui.Text(GetString($"Time Left:   {SelectedProjectile.timeLeft}/{DefaultProjectiledCache.timeLeft}"));
        ImGui.Text(GetString($"Position:    {SelectedProjectile.position}"));
        ImGui.Text(GetString($"Speed:       {SelectedProjectile.velocity.Length()}"));
        ImGui.Text(GetString($"Velocity:    {SelectedProjectile.velocity}"));
        ImGui.Text(GetString($"Velocity Dir: "));

        if (SelectedProjectile.velocity.Length() > 0f)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            Vector2 center = new Vector2(ImGui.GetItemRectMax().X, ImGui.GetItemRectMin().Y) + new Vector2(ImGui.GetItemRectSize().Y / 2f);
            Matrix3x2 rotationMatrix = Matrix3x2.CreateRotation(SelectedProjectile.velocity.SafeNormalize(Vector2.Zero).AngleTo(Vector2.Zero) + MathF.PI / 2f);

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

        ImGui.Text(GetString($"AI Style:    {SelectedProjectile.aiStyle}"));

        for (int i = 0; i < Projectile.maxAI; i++)
        {
            ImGui.Text(GetString($"AI[{i}]:     {SelectedProjectile.ai[i]}"));
        }

        if (Main.netMode == 1 && SelectedProjectile.active)
        {
            ImGui.Text(GetString($"Owned By:  {(SelectedProjectile.npcProj ? "None/Server" : SelectedProjectile.owner switch
            {
                >= 255 => GetString("None/Server"),
                          >= 0 => $"{Main.player[SelectedProjectile.owner].name}",
                _ => GetString("None/Server"),
            })}/{SelectedProjectile.owner}"));
        }

        if (SelectedProjectile.type > 0 && SelectedProjectile.type < TextureAssets.Projectile.Length && !Main.gameMenu)
        {
            Main.instance.LoadProjectile(SelectedProjectile.type);

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            Texture2D tex = TextureAssets.Projectile[SelectedProjectile.type].Value;

            if (!BoundProjectileTextures.TryGetValue(SelectedProjectile.type, out nint texId))
            {
                texId = ClientLoader.MainRenderer!.BindTexture(tex);
                BoundProjectileTextures.Add(SelectedProjectile.type, texId);
            }

            Rectangle frame = tex.Frame(1, Main.projFrames[SelectedProjectile.type], 0, SelectedProjectile.frame);

            float scale = 1f;
            if (frame.Width > 256 || frame.Height > 256)
                scale = 256f / Math.Max(frame.Width, frame.Height);

            Vector2 uv1 = frame.TopLeft() / tex.Size();
            Vector2 uv2 = frame.BottomRight() / tex.Size();

            Vector2 frameSize = frame.Size();
            Vector2 center = ImGui.GetCursorScreenPos() + new Vector2(Math.Max(frame.Width, frame.Height) * scale) / 2f;
            Matrix3x2 rotationMat = Matrix3x2.CreateRotation(SelectedProjectile.rotation);

            Vector2[] positions = new Vector2[4]
            {
                center + Vector2.Transform(new Vector2(-frameSize.X / 2f, -frameSize.Y / 2f), rotationMat) * scale, // top left
                center + Vector2.Transform(new Vector2(+frameSize.X / 2f, -frameSize.Y / 2f), rotationMat) * scale, // top right
                center + Vector2.Transform(new Vector2(+frameSize.X / 2f, +frameSize.Y / 2f), rotationMat) * scale, // bottom right
                center + Vector2.Transform(new Vector2(-frameSize.X / 2f, +frameSize.Y / 2f), rotationMat) * scale, // bottom left
            };

            if (SelectedProjectile.direction == -1)
            {
                uv1.X = 1.0f - uv1.X;
                uv2.X = 1.0f - uv2.X;
            }

            drawList.AddImageQuad(texId, positions[0], positions[1], positions[2], positions[3], new Vector2(uv1.X, uv1.Y), new Vector2(uv2.X, uv1.Y), new Vector2(uv2.X, uv2.Y), new Vector2(uv1.X, uv2.Y));
        }
    }

    private void DrawProjectileSelectMenu(out bool showTooltip)
    {
        showTooltip = true;

        if (ImGui.BeginMenu(GetString("Projectiles")))
        {
            for (int i = 0; i < 1000; i++)
            {
                bool anyActiveProjectiles = false;
                for (int j = i; j < Math.Min(i + 32, 1000); j++)
                {
                    if (Main.projectile[j].active)
                        anyActiveProjectiles = true;
                }
                
                ImGui.BeginDisabled(!anyActiveProjectiles);
                if (ImGui.BeginMenu(GetString($"Projectiles {i}-{Math.Min(i + 32, 1000)}")))
                {
                    showTooltip = false;
                    ImGui.BeginDisabled(false);
                    for (int j = i; j < Math.Min(i + 32, 1000); j++)
                    {
                        ImGui.PushID(j);
                        if (!Main.projectile[j].active) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] * new Vector4(1f, 1f, 1f, 0.4f));

                        if (ImGui.MenuItem(GetString($"Projectile \"{Main.projectile[j].Name.Truncate(30)}\"/{InternalRepresentation.GetProjectileIDName(Main.projectile[j].type)}/{Main.projectile[j].type}")))
                        {
                            SelectedProjectileIndex = j;
                        }

                        if (!Main.projectile[j].active) ImGui.PopStyleColor();
                        ImGui.PopID();
                    }
                    ImGui.EndDisabled();
                    ImGui.EndMenu();
                }
                ImGui.EndDisabled();
                i += 32;
            }
            ImGui.EndMenu();
        }
    }

    public override void UpdateInGameSelect()
    {
        for (int i = 0; i < 1000; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile.active)
            {
                Rectangle selectRect = new Rectangle((int)(projectile.position.X + projectile.width * 0.5 - 16.0), (int)(projectile.position.Y + projectile.height - 48f), 32, 48);

                if (InputSystem.RightMousePressed && selectRect.Contains(Util.ScreenToWorldWorld(InputSystem.MousePosition).ToPoint()))
                {
                    SelectedProjectileIndex = i;
                    InspectorWindow.OpenTab(this);
                    break;
                }
            }
        }
    }
}
