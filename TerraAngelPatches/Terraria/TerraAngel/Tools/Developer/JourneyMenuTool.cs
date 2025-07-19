
namespace TerraAngel.Tools.Developer;

public class JourneyMenuTool : Tool
{
    public override string Name => GetString("Journey Menu");

    public override ToolTabs Tab => ToolTabs.MainTools;

    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
    }
}
