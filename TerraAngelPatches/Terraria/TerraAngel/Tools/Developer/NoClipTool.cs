using Microsoft.Xna.Framework.Input;

namespace TerraAngel.Tools.Developer;

public class NoClipTool : Tool
{
    public override string Name => GetString("No-Clip");

    public override ToolTabs Tab => ToolTabs.MainTools;

    public float NoClipSpeed = 150f;
    public float ShiftNoClipSpeed = 300f;
    public float CTRLNoClipSpeed = 75f;
    public int NoClipPlayerSyncTime = 1;
    public float CurrentSpeed = 300f;

    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
        if (Enabled)
        {
            if (ImGui.CollapsingHeader(GetString("No-Clip Settings")))
            {
                ImGui.Indent();
                if (ImGui.BeginTable("NoClipInfoTable", 2))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(GetString("Speed"));
                    ImGui.TableNextColumn();
                    ImGui.SliderFloat("##NoClipSpeed", ref NoClipSpeed, 1f, 500f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(GetString("Shift Speed"));
                    ImGui.TableNextColumn();
                    ImGui.SliderFloat("##NoClipShiftSpeed", ref ShiftNoClipSpeed, 1f, 500f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(GetString("CTRL Speed"));
                    ImGui.TableNextColumn();
                    ImGui.SliderFloat("##NoClipCTRLSpeed", ref CTRLNoClipSpeed, 1f, 500f);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(GetString("Frames between sync"));
                    ImGui.TableNextColumn();
                    ImGui.SliderInt("##NoClipSyncTime", ref NoClipPlayerSyncTime, 1, 60);

                    ImGui.EndTable();
                }
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
                    if (InputSystem.IsKeyDown(Keys.LeftShift) || InputSystem.IsKeyDown(Keys.RightShift))
                        CurrentSpeed = ShiftNoClipSpeed;
                    else if (InputSystem.IsKeyDown(Keys.LeftControl) || InputSystem.IsKeyDown(Keys.RightControl))
                        CurrentSpeed = CTRLNoClipSpeed;
                    else
                        CurrentSpeed = NoClipSpeed;

                    if (InputSystem.IsKeyDown(Keys.W))
                    {
                        self.position.Y -= CurrentSpeed * 16f * Time.DrawDeltaTime;
                    }
                    if (InputSystem.IsKeyDown(Keys.S))
                    {
                        self.position.Y += CurrentSpeed * 16f * Time.DrawDeltaTime;
                    }
                    if (InputSystem.IsKeyDown(Keys.A))
                    {
                        self.position.X -= CurrentSpeed * 16f * Time.DrawDeltaTime;
                    }
                    if (InputSystem.IsKeyDown(Keys.D))
                    {
                        self.position.X += CurrentSpeed * 16f * Time.DrawDeltaTime;
                    }
                }
            }
        }
    }
}
