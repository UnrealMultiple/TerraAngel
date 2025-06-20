using System;

namespace TerraAngel.Tools.Developer;

public class ExtendedPickupTool : Tool
{
    public override string Name => GetString("Extended pickup");

    public override ToolTabs Tab => ToolTabs.MainTools;

    public float PullSpeed = 25f;
    public int Acceleration = 5;
    public int Range = 600;

    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
        if (Enabled)
        {
            if (ImGui.CollapsingHeader(GetString("Extended pickup Settings")))
            {
                ImGui.Indent();
                ImGui.TextUnformatted(GetString("Speed")); ImGui.SameLine();
                ImGui.SliderFloat("##PickupSpeed", ref PullSpeed, 1f, 500f);

                ImGui.TextUnformatted(GetString("Acceleration")); ImGui.SameLine();
                ImGui.SliderInt("##PickupAcceleration", ref Acceleration, 1, 100);

                ImGui.TextUnformatted(GetString("Range")); ImGui.SameLine();
                ImGui.SliderInt("##PickupRange", ref Range, 1, 2000);
                ImGui.Unindent();
            }
        }
    }

    public override void Update()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        if (Enabled)
        {
            Player localPlayer = Main.LocalPlayer;
            for (int i = 0; i < 400; i++)
            {
                Item item = Main.item[i];
                Player.ItemSpaceStatus itemSpaceStatus = localPlayer.ItemSpace(item);
                if (item.active && item.shimmerTime == 0f && localPlayer.CanAcceptItemIntoInventory(item) && localPlayer.CanPullItem(item, itemSpaceStatus) && new Rectangle((int)localPlayer.position.X - Range, (int)localPlayer.position.Y - Range, localPlayer.width + Range * 2, localPlayer.height + Range * 2).Intersects(item.Hitbox))
                {
                    item.beingGrabbed = true;
                    Vector2 vector = new Vector2(item.position.X + item.width / 2, item.position.Y + item.height / 2);
                    float distPlayerXtoItemX = localPlayer.position.X - vector.X;
                    float distPlayerYtoItemY = localPlayer.position.Y - vector.Y;
                    float distSqrt = (float)Math.Sqrt(distPlayerXtoItemX * distPlayerXtoItemX + distPlayerYtoItemY * distPlayerYtoItemY);
                    if (distSqrt == 0f)
                        distSqrt = 1f;
                    distPlayerXtoItemX *= PullSpeed / distSqrt;
                    distPlayerYtoItemY *= PullSpeed / distSqrt;
                    item.velocity.X = (item.velocity.X * (Acceleration - 1) + distPlayerXtoItemX) / Acceleration;
                    item.velocity.Y = (item.velocity.Y * (Acceleration - 1) + distPlayerYtoItemY) / Acceleration;
                }
            }
        }
    }
}
