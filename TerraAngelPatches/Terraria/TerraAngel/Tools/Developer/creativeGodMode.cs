namespace TerraAngel.Tools.Developer;

public class creativeGodMode : Tool
{
    public override string Name => GetString("GodMode");

    public override ToolTabs Tab => ToolTabs.MainTools;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultcreativeGodMode))]
    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
    }
}
