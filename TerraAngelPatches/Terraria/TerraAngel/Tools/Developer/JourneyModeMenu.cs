namespace TerraAngel.Tools.Developer;

public class JourneyModeMenu : Tool
{
    public override string Name => GetString("Journey Mode Menu");

    public override ToolTabs Tab => ToolTabs.MainTools;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultJourneyModeMenu))]
    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
    }
}
