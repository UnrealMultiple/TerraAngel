using Terraria;

namespace TerraAngel.Tools.Map;

public class MapTeleportTool : Tool
{
    public override string Name => GetString("Map Teleport");

    public override ToolTabs Tab => ToolTabs.MainTools;

    [DefaultConfigValue(nameof(ClientConfig.Config.RightClickOnMapToTeleport))]
    public bool Enabled;

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.Checkbox(Name, ref Enabled);
    }

    public override void Update()
    {
        if (Main.mapFullscreen && Enabled && !ImGui.GetIO().WantCaptureMouse)
        {
            if (InputSystem.RightMousePressed || (InputSystem.Ctrl && InputSystem.RightMouseDown))
            {
                Main.LocalPlayer.velocity = Vector2.Zero;
                if (InputSystem.Ctrl)
                {
                    Main.LocalPlayer.Center = Util.ScreenToWorldFullscreenMap(InputSystem.MousePosition);
                }
                else
                {
                    if (Util.IsMouseHoveringRect(Vector2.Zero, ImGui.GetIO().DisplaySize))
                    {
						Main.mapFullscreen = false;
                        Main.LocalPlayer.Teleport(Util.ScreenToWorldFullscreenMap(InputSystem.MousePosition) - new Vector2(Main.LocalPlayer.width / 2f, Main.LocalPlayer.height), TeleportationStyleID.RodOfDiscord);
                        if (ClientConfig.Settings.TeleportSendRODPacket)
                        {
                            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null,
                                0,
                                Main.LocalPlayer.whoAmI,
                                Main.LocalPlayer.position.X,
                                Main.LocalPlayer.position.Y,
                                TeleportationStyleID.RodOfDiscord);
                        }
                    }
                }

                NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);
            }
        }
    }
}
