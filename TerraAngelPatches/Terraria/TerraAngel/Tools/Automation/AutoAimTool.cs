using TerraAngel.Physics;
using System.Collections.Generic;
using System;

namespace TerraAngel.Tools.Automation;

public class AutoAimTool : Tool
{
    public override string Name => GetString("Auto-Aim");
    public override ToolTabs Tab => ToolTabs.AutomationTools;

    public ref bool FavorBosses => ref ClientConfig.Settings.AutoAttackFavorBosses;
    public ref bool TargetHostileNPCs => ref ClientConfig.Settings.AutoAttackTargetHostileNPCs;
    public ref bool TargetPlayers => ref ClientConfig.Settings.AutoAttackTargetPlayers;
    public ref bool RequireLineOfSight => ref ClientConfig.Settings.AutoAttackRequireLineOfSight;
    public ref bool VelocityPrediction => ref ClientConfig.Settings.AutoAttackVelocityPrediction;
    public ref bool OnlyPvPPlayers => ref ClientConfig.Settings.AutoAttackOnlyPvPPlayers;

    public ref float MinAttackRange => ref ClientConfig.Settings.AutoAttackMinTargetRange;
    public ref float VelocityPrectionScaling => ref ClientConfig.Settings.AutoAttackVelocityPredictionScaling;

    public ref string TargetPlayerName => ref ClientConfig.Settings.AutoAttackTargetPlayerName;

    public bool AutoUse;
    public bool CanAutoUse => AutoUse && !Main.playerInventory;

    public bool Enabled;

    private int targetPlayerIndex = 0;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);

        ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();

        if (Enabled)
        {
            if (ImGui.CollapsingHeader(GetString("Auto-Attack Settings")))
            {
                ImGui.Indent();
                ImGui.SliderFloat(GetString("Minimum Target Range"), ref MinAttackRange, 1f, 2000f);
                if (ImGui.IsItemFocused())
                {
                    if (Main.mapFullscreen)
                    {
                        drawList.AddCircleFilled(Util.WorldToScreenFullscreenMap(Main.LocalPlayer.Center), Util.WorldToScreenFullscreenMap(Main.LocalPlayer.Center).Distance(Util.WorldToScreenFullscreenMap(Main.LocalPlayer.Center + new Vector2(MinAttackRange, 0f))), Color.Red.WithAlpha(0.5f).PackedValue);
                    }
                    else
                    {
                        drawList.AddCircleFilled(Util.WorldToScreenWorld(Main.LocalPlayer.Center), MinAttackRange, Color.Red.WithAlpha(0.5f).PackedValue);
                    }
                }

                ImGui.Checkbox(GetString("Require Line of Sight"), ref RequireLineOfSight);
                ImGui.Checkbox(GetString("Auto Use"), ref AutoUse);
                ImGui.Checkbox(GetString("Velocity Prediction"), ref VelocityPrediction);
                if (VelocityPrediction)
                {
                    ImGui.SliderFloat(GetString("Prediction Scaling"), ref VelocityPrectionScaling, 0.0f, 10.0f, "%.3f");
                }

                ImGui.Separator();
                ImGui.Checkbox(GetString("Target Hostile NPCs"), ref TargetHostileNPCs);
                ImGui.Checkbox(GetString("Target Players"), ref TargetPlayers);

                if (TargetPlayers)
                {
                    ImGui.Indent();
                    ImGui.Checkbox(GetString("Only PvP Players"), ref OnlyPvPPlayers);
                    
                    ImGui.Separator();
                    
                    // 获取在线玩家列表
                    var onlinePlayerNames = new List<string>();
                    var onlinePlayerIndices = new List<int>();

                    // 添加"不选中任何玩家"选项
                    onlinePlayerNames.Add(GetString("Auto-target nearest player"));
                    onlinePlayerIndices.Add(-1);

                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active && i != Main.myPlayer)
                        {
                            string pvpStatus = Main.player[i].hostile ? "PvP" : "Non-PvP";
                            onlinePlayerNames.Add($"[{i}] {Main.player[i].name} ({pvpStatus})");
                            onlinePlayerIndices.Add(i);
                        }
                    }

                    // 确保索引有效
                    if (targetPlayerIndex >= onlinePlayerNames.Count)
                        targetPlayerIndex = 0;

                    // 同步当前目标玩家名称与下拉框索引
                    if (!string.IsNullOrWhiteSpace(TargetPlayerName))
                    {
                        bool found = false;
                        for (int i = 1; i < onlinePlayerIndices.Count; i++) // 从1开始，跳过"不选中"选项
                        {
                            if (onlinePlayerIndices[i] >= 0 && Main.player[onlinePlayerIndices[i]].name == TargetPlayerName)
                            {
                                targetPlayerIndex = i;
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            targetPlayerIndex = 0; // 如果找不到玩家，重置为"不选中"
                    }
                    else
                    {
                        targetPlayerIndex = 0; // 没有指定玩家名称时，选择"不选中"
                    }

                    ImGui.Text($"{GetString("Target Player")}:");
                    if (ImGui.Combo("##TargetPlayerSelection", ref targetPlayerIndex, onlinePlayerNames.ToArray(), onlinePlayerNames.Count))
                    {
                        // 选择改变时更新目标玩家名称
                        if (targetPlayerIndex == 0 || onlinePlayerIndices[targetPlayerIndex] < 0)
                        {
                            // 选择了"不选中任何玩家"
                            TargetPlayerName = "";
                        }
                        else
                        {
                            TargetPlayerName = Main.player[onlinePlayerIndices[targetPlayerIndex]].name;
                        }
                    }
                    
                    ImGui.Unindent();
                }

                ImGui.Unindent();
            }
        }
    }

    public Vector2 TargetPoint = Vector2.Zero;
    public bool wantToShoot = false;
    public bool LockedOnToTarget = false;

    private struct TargetCandidate
    {
        public Vector2 Center;
        public Vector2 Velocity;
        public bool IsValid;
    }

    private bool TryFindBestTarget(
        Vector2 playerCenter,
        Func<int, TargetCandidate> getCandidate,
        int maxCount,
        out Vector2 bestTarget)
    {
        float minDistSq = float.MaxValue;
        bestTarget = Vector2.Zero;
        bool foundTarget = false;

        for (int i = 0; i < maxCount; i++)
        {
            TargetCandidate candidate = getCandidate(i);
            if (!candidate.IsValid)
                continue;

            float distToPlayer = playerCenter.Distance(candidate.Center);
            if (distToPlayer > MinAttackRange)
                continue;

            RaycastData raycast = Raycast.Cast(playerCenter, (candidate.Center - playerCenter).Normalized(), distToPlayer + 1f);
            if (RequireLineOfSight && raycast.Hit)
                continue;

            Vector2 targetPoint = candidate.Center;
            if (VelocityPrediction)
            {
                float shootSpeed = CalcPlayerShootSpeed();
                if (shootSpeed > 0 && (candidate.Velocity.X != 0 || candidate.Velocity.Y != 0))
                {
                    float timeToTarget = (raycast.Distance / shootSpeed) * VelocityPrectionScaling;
                    RaycastData predictionRaycast = Raycast.Cast(
                        candidate.Center,
                        (candidate.Velocity * timeToTarget).Normalized(),
                        (candidate.Velocity * timeToTarget).Length() + 0.1f);
                    targetPoint = predictionRaycast.End;
                }
            }

            float distSq = targetPoint.DistanceSQ(playerCenter);
            if (distSq < minDistSq)
            {
                bestTarget = targetPoint;
                minDistSq = distSq;
                foundTarget = true;
            }
        }

        return foundTarget;
    }

    public override void Update()
    {
        ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();
        if (Enabled && !Main.gameMenu)
        {
            Vector2 correctedPlayerCenter = Main.LocalPlayer.RotatedRelativePoint(Main.LocalPlayer.MountedCenter, reverseRotation: true);
            LockedOnToTarget = false;

            // Target NPCs
            if (TargetHostileNPCs)
            {
                if (TryFindBestTarget(correctedPlayerCenter, GetNPCCandidate, Main.maxNPCs, out Vector2 npcTarget))
                {
                    TargetPoint = npcTarget;
                    LockedOnToTarget = true;
                }
            }

            // Target Players
            if (TargetPlayers)
            {
                if (TryFindBestTarget(correctedPlayerCenter, GetPlayerCandidate, Main.maxPlayers, out Vector2 playerTarget))
                {
                    float currentBestDistSq = LockedOnToTarget ? TargetPoint.DistanceSQ(correctedPlayerCenter) : float.MaxValue;
                    float newDistSq = playerTarget.DistanceSQ(correctedPlayerCenter);
                    
                    if (newDistSq < currentBestDistSq)
                    {
                        TargetPoint = playerTarget;
                        LockedOnToTarget = true;
                    }
                }
            }

            if (!Main.mapFullscreen && LockedOnToTarget)
                drawList.AddCircleFilled(Util.WorldToScreenWorld(TargetPoint), 5f, Color.Red.PackedValue);
        }
    }

    private TargetCandidate GetNPCCandidate(int index)
    {
        NPC npc = Main.npc[index];
        
        if (!npc.active || npc.friendly || npc.immortal || npc.dontTakeDamage)
            return new TargetCandidate { IsValid = false };

        return new TargetCandidate
        {
            Center = npc.Center,
            Velocity = npc.velocity,
            IsValid = true
        };
    }

    private TargetCandidate GetPlayerCandidate(int index)
    {
        Player player = Main.player[index];

        if (!player.active || player.dead || index == Main.myPlayer)
            return new TargetCandidate { IsValid = false };

        if (OnlyPvPPlayers && !player.hostile)
            return new TargetCandidate { IsValid = false };

        if (!string.IsNullOrWhiteSpace(TargetPlayerName) && player.name != TargetPlayerName)
            return new TargetCandidate { IsValid = false };

        if (player.team == Main.LocalPlayer.team && player.team != 0)
            return new TargetCandidate { IsValid = false };

        return new TargetCandidate
        {
            Center = player.Center,
            Velocity = player.velocity,
            IsValid = true
        };
    }

    public float CalcPlayerShootSpeed()
    {
        int projectileType = Main.LocalPlayer.HeldItem.shoot;
        float shootSpeed = Main.LocalPlayer.HeldItem.shootSpeed;

        if (Main.LocalPlayer.HeldItem.useAmmo > 0)
        {
            bool cs = true;
            int dm = 0;
            float kb = 0.0f;
            Main.LocalPlayer.PickAmmo(Main.LocalPlayer.HeldItem, ref projectileType, ref shootSpeed, ref cs, ref dm, ref kb, out _, true);
        }

        return shootSpeed;
    }
}
