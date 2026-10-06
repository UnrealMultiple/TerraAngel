namespace TerraAngel.Tools.Developer;

public class RangeLimitTool : Tool
{
    public override string Name => GetString("Remove some range limits");

    public override ToolTabs Tab => ToolTabs.MainTools;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultRemoveViewRangeLimits))]
    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
        ImGui.SameLine();
        ImGuiUtil.HelpMarker(GetString("Removes the distance caps on the Rod of Discord, the Zenith target range and lock-on targets, and allows larger windows."));
    }

    public override void Update()
    {
        Main.ApplyViewRangeLimits(Enabled);
    }
}
