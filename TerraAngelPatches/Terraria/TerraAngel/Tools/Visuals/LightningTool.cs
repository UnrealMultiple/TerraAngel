using Terraria.GameContent.Drawing;

namespace TerraAngel.Tools.Visuals;

public class LightningTool : Tool
{
    public override string Name => GetString("Lightning Tool");

    public override ToolTabs Tab => ToolTabs.VisualTools;

    private bool ShowLightning = false;
    private int RandomFrame = 200;
    private int LightningCount = 7;
    private int _FrameCounter = 0;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(GetString("Lightning Effect"), ref ShowLightning);
        if(ShowLightning && ImGui.CollapsingHeader(GetString("Lightning Settings")))
        {
            ImGui.SliderInt(GetString("Generate frame"), ref RandomFrame, 1, 300);
            ImGui.SliderInt(GetString("Number of lightning"), ref LightningCount, 1, 300);
        }
    }

    public override void Update()
    {
        if(ShowLightning)
        {
            _FrameCounter++;
            if(_FrameCounter % RandomFrame == 0)
            {
                for (var i = 0; i < LightningCount; i++)
                {
                    var randomColor = new Color(Main.rand.Next(0, 255), Main.rand.Next(0, 255), Main.rand.Next(0, 255));
                    var direction = Main.rand.Next(0, 2) == 0 ? -1 : 1;
                    var settings = new ParticleOrchestraSettings()
                    {
                        PositionInWorld = Main.LocalPlayer.position + new Vector2(Main.rand.Next(60 * 16) * direction, 0),
                        UniqueInfoPiece = (int)randomColor.PackedValue,
                        MovementVector = new Vector2(Main.rand.Next(0, 1145), 0f),
                        IndexOfPlayerWhoInvokedThis = (byte)Main.myPlayer
                    };
                    ParticleOrchestrator.BroadcastOrRequestParticleSpawn(ParticleOrchestraType.StormLightning, settings);
                }
            }
        }
    }
}
