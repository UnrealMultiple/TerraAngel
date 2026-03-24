namespace TerraAngel.Tools.Developer;

public class InfiniteReachTool : Tool
{
    public override string Name => GetString("Infinite reach");

    public override ToolTabs Tab => ToolTabs.MainTools;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultInfiniteReach))]
    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
        ImGui.SameLine();
        ImGuiUtil.HelpMarker(GetString("This tool doesn't help you to bypass TShock's range check. Placing tiles too far away might be blocked by TShock"));
    }
}
