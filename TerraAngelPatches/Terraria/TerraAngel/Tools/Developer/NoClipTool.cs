namespace TerraAngel.Tools.Developer;

public class NoClipTool : Tool
{
    public override string Name => GetString("Noclip");

    public override ToolTabs Tab => ToolTabs.MainTools;

    public float NoClipSpeed = 150f;
    public int NoClipPlayerSyncTime = 1;

    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
        if (Enabled)
        {
            if (ImGui.CollapsingHeader(GetString("Noclip Settings")))
            {
                ImGui.Indent();
                ImGui.TextUnformatted(GetString("Speed")); ImGui.SameLine();
                ImGui.SliderFloat("##Speed", ref NoClipSpeed, 1f, 500f);

                ImGui.TextUnformatted(GetString("Frames between sync")); ImGui.SameLine();
                ImGui.SliderInt("##SyncTime", ref NoClipPlayerSyncTime, 1, 60);
                ImGui.Unindent();
            }
        }
    }

    public override void Update()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        if (!Main.mapFullscreen)
        {
            Player self = Main.LocalPlayer;

            if (InputSystem.IsKeyPressed(ClientConfig.Settings.ToggleNoclip))
            {
                Enabled = !Enabled;
            }

            if (!io.WantCaptureKeyboard && !io.WantTextInput && !Main.drawingPlayerChat)
            {
                if (Enabled)
                {
                    self.oldPosition = self.position;
                    if (ImGui.IsKeyDown(ImGuiKey.W))
                    {
                        self.position.Y -= NoClipSpeed * 16f * Time.DrawDeltaTime;
                    }
                    if (ImGui.IsKeyDown(ImGuiKey.S))
                    {
                        self.position.Y += NoClipSpeed * 16f * Time.DrawDeltaTime;
                    }
                    if (ImGui.IsKeyDown(ImGuiKey.A))
                    {
                        self.position.X -= NoClipSpeed * 16f * Time.DrawDeltaTime;
                    }
                    if (ImGui.IsKeyDown(ImGuiKey.D))
                    {
                        self.position.X += NoClipSpeed * 16f * Time.DrawDeltaTime;
                    }
                }
            }
        }
    }
}
