using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TerraAngel.Physics;

namespace TerraAngel.Tools.Automation;

public class AutoAimTool : Tool
{
    public override string Name => GetString("Auto-Aim");
    public override ToolTabs Tab => ToolTabs.AutomationTools;
    public ref float MinAttackRange => ref ClientConfig.Settings.AutoAttackMinTargetRange;
    public ref bool MouseBound => ref ClientConfig.Settings.AutoAttackMouseBounding;
    public ref bool ShowMouseRange => ref ClientConfig.Settings.AutoAttackShowMouseRange;
    public ref bool RequireLineOfSight => ref ClientConfig.Settings.AutoAttackRequireLineOfSight;
    public bool AutoUse;
    public bool CanAutoUse => AutoUse && !Main.playerInventory;
    public ref bool VelocityPrediction => ref ClientConfig.Settings.AutoAttackVelocityPrediction;
    public ref float VelocityPrectionScaling => ref ClientConfig.Settings.AutoAttackVelocityPredictionScaling;
    public ref bool TargetHostileNPCs => ref ClientConfig.Settings.AutoAttackTargetHostileNPCs;
    public ref bool FavorBosses => ref ClientConfig.Settings.AutoAttackFavorBosses;
    public ref TargetPriority TargetPriorityMode => ref Unsafe.As<int, TargetPriority>(ref ClientConfig.Settings.AutoAttackTargetPriority);
    public ref bool TargetPlayers => ref ClientConfig.Settings.AutoAttackTargetPlayers;
    public ref bool OnlyPvPPlayers => ref ClientConfig.Settings.AutoAttackOnlyPvPPlayers;
    public ref string TargetPlayerName => ref ClientConfig.Settings.AutoAttackTargetPlayerName;

    public bool Enabled;

    private int targetPlayerIndex = 0;

    public enum TargetPriority
    {
        Closest,
        HighestMaxHP,
        LowestMaxHP,
        HighestHP,
        LowestHP
    }

    public string[] TargetPriorityStrings =
    [
        "Closest",
        "Highest Max HP",
        "Lowest Max HP",
        "Highest HP",
        "Lowest HP",
    ];


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
                    if (MouseBound)
                    {
                        Vector2 screenCenter = Util.WorldToScreenWorld(Main.MouseWorld);
                        drawList.AddCircleFilled(screenCenter, screenCenter.Distance(Util.WorldToScreenWorld(Main.MouseWorld + new Vector2(MinAttackRange, 0f))), Color.Red.WithAlpha(0.5f).PackedValue);
                    }
                    else
                    {
                        if (Main.mapFullscreen)
                        {
                            Vector2 screenCenter = Util.WorldToScreenFullscreenMap(Main.LocalPlayer.Center);
                            drawList.AddCircleFilled(screenCenter, screenCenter.Distance(Util.WorldToScreenFullscreenMap(Main.LocalPlayer.Center + new Vector2(MinAttackRange, 0f))), Color.Red.WithAlpha(0.5f).PackedValue);
                        }
                        else
                        {
                            Vector2 screenCenter = Util.WorldToScreenWorld(Main.LocalPlayer.Center);
                            drawList.AddCircleFilled(screenCenter, screenCenter.Distance(Util.WorldToScreenWorld(Main.LocalPlayer.Center + new Vector2(MinAttackRange, 0f))), Color.Red.WithAlpha(0.5f).PackedValue);
                        }
                    }
                }

                ImGui.Checkbox(GetString("Bound Range to Mouse"), ref MouseBound);

                if (MouseBound)
                {
                    ImGui.Indent();
                    ImGui.Checkbox(GetString("Show Mouse Range"), ref ShowMouseRange);
                    ImGui.Unindent();
                }

                ImGui.Checkbox(GetString("Require Line of Sight"), ref RequireLineOfSight);
                ImGui.Checkbox(GetString("Auto Use"), ref AutoUse);
                ImGui.Checkbox(GetString("Velocity Prediction"), ref VelocityPrediction);
                if (VelocityPrediction)
                {
                    ImGui.Indent();
                    ImGui.SliderFloat(GetString("Prediction Scaling"), ref VelocityPrectionScaling, 0.0f, 10.0f, "%.3f");
                    ImGui.Unindent();
                }

                ImGui.Separator();
                ImGui.Checkbox(GetString("Target Hostile NPCs"), ref TargetHostileNPCs);

                if (TargetHostileNPCs)
                {
                    ImGui.Indent();
                    ImGui.Checkbox(GetString("Favor Bosses"), ref FavorBosses);
                    ImGui.Combo("Target Priority", ref Unsafe.As<TargetPriority, int>(ref TargetPriorityMode), TargetPriorityStrings, TargetPriorityStrings.Length);
                    ImGui.Unindent();
                }

                ImGui.Checkbox(GetString("Target Players"), ref TargetPlayers);

                if (TargetPlayers)
                {
                    ImGui.Indent();
                    ImGui.Checkbox(GetString("Only PvP Players"), ref OnlyPvPPlayers);

                    ImGui.Separator();

                    var onlinePlayerNames = new List<string>();
                    var onlinePlayerIndices = new List<int>();

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

                    if (targetPlayerIndex >= onlinePlayerNames.Count)
                        targetPlayerIndex = 0;

                    if (!string.IsNullOrWhiteSpace(TargetPlayerName))
                    {
                        bool found = false;
                        for (int i = 1; i < onlinePlayerIndices.Count; i++)
                        {
                            if (onlinePlayerIndices[i] >= 0 && Main.player[onlinePlayerIndices[i]].name == TargetPlayerName)
                            {
                                targetPlayerIndex = i;
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            targetPlayerIndex = 0;
                    }
                    else
                    {
                        targetPlayerIndex = 0;
                    }

                    ImGui.Text($"{GetString("Target Player")}:");
                    if (ImGui.Combo("##TargetPlayerSelection", ref targetPlayerIndex, onlinePlayerNames.ToArray(), onlinePlayerNames.Count))
                    {
                        if (targetPlayerIndex == 0 || onlinePlayerIndices[targetPlayerIndex] < 0)
                        {
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
        public bool IsBoss;
        public int CurrentHealth;
        public int MaxHealth;
    }

    private bool TryFindBestTarget(
        Vector2 pointCenter,
        Func<int, TargetCandidate> getCandidate,
        int maxCount,
        out Vector2 bestTarget)
    {
        (bool NotBoss, float Score) bestScore = (true, float.MaxValue);
        float minDistSq = float.MaxValue;
        bestTarget = Vector2.Zero;
        bool foundTarget = false;
        Vector2 playerCenter = Main.LocalPlayer.RotatedRelativePoint(Main.LocalPlayer.MountedCenter, reverseRotation: true);

        for (int i = 0; i < maxCount; i++)
        {
            TargetCandidate candidate = getCandidate(i);
            if (!candidate.IsValid)
                continue;

            float distToPoint = pointCenter.Distance(candidate.Center);
            if (distToPoint > MinAttackRange)
                continue;

            float distToPlayer = playerCenter.Distance(candidate.Center);
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

            float distSq = targetPoint.DistanceSQ(pointCenter);
            float score = GetTargetScore(candidate, distSq);
            (bool NotBoss, float Score) current = (FavorBosses && !candidate.IsBoss, score);

            if (current.CompareTo(bestScore) < 0 || (current == bestScore && distSq < minDistSq))
            {
                bestTarget = targetPoint;
                bestScore = current;
                minDistSq = distSq;
                foundTarget = true;
            }
        }

        return foundTarget;
    }

    private float GetTargetScore(TargetCandidate candidate, float distanceSq)
    {
        return TargetPriorityMode switch
        {
            TargetPriority.Closest => distanceSq,

            TargetPriority.HighestHP => -candidate.CurrentHealth,

            TargetPriority.HighestMaxHP => -candidate.MaxHealth,

            TargetPriority.LowestHP => candidate.CurrentHealth,

            TargetPriority.LowestMaxHP => candidate.MaxHealth,

            _ => distanceSq
        };
    }

    public override void Update()
    {
        ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();
        if (Enabled && !Main.gameMenu)
        {
            Vector2 correctedPlayerCenter = !MouseBound ? Main.LocalPlayer.RotatedRelativePoint(Main.LocalPlayer.MountedCenter, reverseRotation: true) : Main.MouseWorld;
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

            if (!Main.mapFullscreen && MouseBound && ShowMouseRange)
            {
                Vector2 screenCenter = Util.WorldToScreenWorld(Main.MouseWorld);
                drawList.AddCircle(screenCenter, screenCenter.Distance(Util.WorldToScreenWorld(Main.MouseWorld + new Vector2(MinAttackRange, 0f))), Color.Red.WithAlpha(0.5f).PackedValue);
            }
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
            IsValid = true,
            IsBoss = npc.boss,
            CurrentHealth = npc.life,
            MaxHealth = npc.lifeMax
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
            IsValid = true,
            IsBoss = false,
            CurrentHealth = player.statLife,
            MaxHealth = player.statLifeMax
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