namespace TerraAngel.Tools.Developer;

public class TeleportTool: Tool
{
    public override string Name => GetString("Teleport Tool");

    public override ToolTabs Tab => ToolTabs.MainTools;
    
    public bool AllowTeleport = true;
    
    public bool LogTargetPosition = false;
    
    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(GetString("Allow Teleport"), ref AllowTeleport);
        ImGui.Checkbox(GetString("Log Teleport Position"), ref LogTargetPosition);
    }
}