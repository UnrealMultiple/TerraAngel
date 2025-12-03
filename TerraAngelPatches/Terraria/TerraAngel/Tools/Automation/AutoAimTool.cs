using TerraAngel.Physics;

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

    public override void Update()
    {
        ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();
        if (Enabled && !Main.gameMenu)
        {
            Vector2 correctedPlayerCenter = Main.LocalPlayer.RotatedRelativePoint(Main.LocalPlayer.MountedCenter, reverseRotation: true);
            float minDist = float.MaxValue;

            // 瞄准 NPC
            if (TargetHostileNPCs)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];

                    if (npc.active)
                    {
                        if (npc.friendly)
                            continue;

                        if (npc.immortal || npc.dontTakeDamage)
                            continue;

                        float distToPlayer = correctedPlayerCenter.Distance(npc.Center);

                        if (distToPlayer > MinAttackRange)
                            continue;

                        RaycastData raycast = Raycast.Cast(correctedPlayerCenter, (npc.Center - correctedPlayerCenter).Normalized(), distToPlayer + 1f);
                        if (RequireLineOfSight)
                        {
                            if (raycast.Hit)
                            {
                                continue;
                            }
                        }

                        Vector2 targetPoint = npc.Center;
                        if (VelocityPrediction)
                        {
                            float sp = CalcPlayerShootSpeed();
                            if (sp > 0 && (npc.velocity.X != 0 || npc.velocity.Y != 0))
                            {
                                float ttt = (raycast.Distance / sp) * VelocityPrectionScaling;
                                RaycastData tttCorrection = Raycast.Cast(npc.Center, (npc.velocity * ttt).Normalized(), (npc.velocity * ttt).Length() + 0.1f);
                                targetPoint = tttCorrection.End;
                            }
                        }

                        float d = targetPoint.DistanceSQ(correctedPlayerCenter);
                        if (d < minDist)
                        {
                            TargetPoint = targetPoint;
                            LockedOnToTarget = true;
                            minDist = d;
                        }
                    }
                }
            }

            // 瞄准玩家
            if (TargetPlayers)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];

                    if (!player.active || player.dead || i == Main.myPlayer)
                        continue;

                    // 如果开启了仅锁定PVP玩家，则跳过非PVP玩家
                    if (OnlyPvPPlayers && !player.hostile)
                        continue;

                    // 如果指定了玩家名称，则仅锁定该玩家
                    if (!string.IsNullOrWhiteSpace(TargetPlayerName) && player.name != TargetPlayerName)
                        continue;
                   
                    if (player.team == Main.LocalPlayer.team && player.team != 0)
                        continue;

                    float distToPlayer = correctedPlayerCenter.Distance(player.Center);

                    if (distToPlayer > MinAttackRange)
                        continue;

                    RaycastData raycast = Raycast.Cast(correctedPlayerCenter, (player.Center - correctedPlayerCenter).Normalized(), distToPlayer + 1f);
                    if (RequireLineOfSight)
                    {
                        if (raycast.Hit)
                        {
                            continue;
                        }
                    }

                    Vector2 targetPoint = player.Center;
                    if (VelocityPrediction)
                    {
                        float sp = CalcPlayerShootSpeed();
                        if (sp > 0 && (player.velocity.X != 0 || player.velocity.Y != 0))
                        {
                            float ttt = (raycast.Distance / sp) * VelocityPrectionScaling;
                            RaycastData tttCorrection = Raycast.Cast(player.Center, (player.velocity * ttt).Normalized(), (player.velocity * ttt).Length() + 0.1f);
                            targetPoint = tttCorrection.End;
                        }
                    }

                    float d = targetPoint.DistanceSQ(correctedPlayerCenter);
                    if (d < minDist)
                    {
                        TargetPoint = targetPoint;
                        LockedOnToTarget = true;
                        minDist = d;
                    }
                }
            }

            if (!Main.mapFullscreen && LockedOnToTarget)
                drawList.AddCircleFilled(Util.WorldToScreenWorld(TargetPoint), 5f, Color.Red.PackedValue);
        }
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
