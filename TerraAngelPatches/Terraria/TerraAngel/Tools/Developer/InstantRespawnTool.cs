namespace TerraAngel.Tools.Developer;

public class InstantRespawnTool : Tool
{
    public override string Name => GetString("Instant Respawn");

    public override ToolTabs Tab => ToolTabs.MainTools;

    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
    }
}