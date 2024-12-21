using ImGuiUtil = TerraAngel.Graphics.ImGuiUtil;

namespace TerraAngel.Tools.Visuals;

public class ESPTool : Tool
{
    public override string Name => GetString("ESP Boxes");

    public override ToolTabs Tab => ToolTabs.ESPTools;

    public ref Color LocalPlayerColor => ref ClientConfig.Settings.LocalBoxPlayerColor;
    public ref Color OtherPlayerColor => ref ClientConfig.Settings.OtherBoxPlayerColor;
    public ref Color OtherTerraAngelUserColor => ref ClientConfig.Settings.OtherTerraAngelUserColor;
    public ref Color NPCColor => ref ClientConfig.Settings.NPCBoxColor;
    public ref Color NPCNetOffsetColor => ref ClientConfig.Settings.NPCNetOffsetBoxColor;
    public ref Color ProjectileColor => ref ClientConfig.Settings.ProjectileBoxColor;
    public ref Color ItemColor => ref ClientConfig.Settings.ItemBoxColor;
    public ref Color TracerColor => ref ClientConfig.Settings.TracerColor;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultDrawAnyESP))]
    public bool DrawAnyESP = true;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultMapESP))]
    public bool MapESP = true;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultPlayerESPBoxes))]
    public bool PlayerBoxes = false;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultPlayerESPTracers))]
    public bool PlayerTracers = false;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultNPCBoxes))]
    public bool NPCBoxes = false;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultProjectileBoxes))]
    public bool ProjectileBoxes = false;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultItemBoxes))]
    public bool ItemBoxes = false;

    [DefaultConfigValue(nameof(ClientConfig.Config.DefaultTileSections))]
    public bool ShowTileSections = false;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(GetString("Draw Any ESP"), ref DrawAnyESP);
        if (DrawAnyESP)
        {
            ImGui.Checkbox(GetString("Draw ESP on map"), ref MapESP);
            ImGui.Checkbox(GetString("Player hitboxes"), ref PlayerBoxes);
            ImGui.Checkbox(GetString("NPC hitboxes"), ref NPCBoxes);
            ImGui.Checkbox(GetString("Projectile hitboxes"), ref ProjectileBoxes);
            ImGui.Checkbox(GetString("Item hitboxes"), ref ItemBoxes);
            ImGui.Checkbox(GetString("Player tracers"), ref PlayerTracers);
            ImGui.Checkbox(GetString("Tile Sections"), ref ShowTileSections);
            if (ImGui.CollapsingHeader(GetString("ESP Colors")))
            {
                ImGui.Indent();
                ImGuiUtil.ColorEdit4(GetString("Local player box color"), ref LocalPlayerColor);
                ImGuiUtil.ColorEdit4(GetString("Other player box color"), ref OtherPlayerColor);
                ImGuiUtil.ColorEdit4(GetString("Other TerraAngel user box color"), ref OtherTerraAngelUserColor);
                ImGuiUtil.ColorEdit4(GetString("Player Tracer color"), ref TracerColor);
                ImGuiUtil.ColorEdit4(GetString("NPC box color"), ref NPCColor);
                ImGuiUtil.ColorEdit4(GetString("NPC net box color"), ref NPCNetOffsetColor);
                ImGuiUtil.ColorEdit4(GetString("Projectile box color"), ref ProjectileColor);
                ImGuiUtil.ColorEdit4(GetString("Item box color"), ref ItemColor);
                ImGui.Unindent();
            }
        }
    }

    public override void Update()
    {
        if (InputSystem.IsKeyPressed(ClientConfig.Settings.ToggleDrawAnyESP))
        {
            DrawAnyESP = !DrawAnyESP;
        }
    }
}
