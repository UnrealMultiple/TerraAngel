namespace TerraAngel.Tools.Developer;

public class InfiniteFlightTool : Tool
{
    public override string Name => GetString("Infinite Flight");

    public override ToolTabs Tab => ToolTabs.MainTools;

    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
    }
}