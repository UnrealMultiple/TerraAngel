namespace TerraAngel.Tools.Automation;

public class ButcherTool : Tool
{
    public override string Name => GetString("Butcher");

    public override ToolTabs Tab => ToolTabs.NewTab;

    public int ButcherDamage = 1000;

    public bool AutoButcherHostiles = false;

    public bool BypassTShock = false;

    public override void DrawUI(ImGuiIOPtr io)
    {
        if (ImGui.Button(GetString("Butcher All Hostile NPCs")))
        {
            Butcher.ButcherAllHostileNPCs(ButcherDamage, bypassTShock: BypassTShock);
        }
        ImGui.Checkbox(GetString("Auto-Butcher Hostiles"), ref AutoButcherHostiles);
        if (ImGui.Button(GetString("Butcher All Friendly NPCs")))
        {
            Butcher.ButcherAllFriendlyNPCs(ButcherDamage, bypassTShock: BypassTShock);
        }
        if (ImGui.Button(GetString("Butcher All Players")))
        {
            Butcher.ButcherAllPlayers(ButcherDamage);
        }
        ImGui.SliderInt(GetString("Butcher Damage"), ref ButcherDamage, 1, (int)short.MaxValue);
        ImGui.Checkbox(GetString("Bypass TShock"), ref BypassTShock);
    }

    public override void Update()
    {
        if (AutoButcherHostiles)
        {
            Butcher.ButcherAllHostileNPCs(ButcherDamage, bypassTShock: BypassTShock);
        }
    }
}
