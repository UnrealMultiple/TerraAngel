using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraAngel.Tools.Developer;

public class WipeGroundItems : Tool
{
    public override string Name => GetString("Wipe Ground Items");

    public override ToolTabs Tab => ToolTabs.MainTools;


    public override void DrawUI(ImGuiIOPtr io)
    {
        if (ImGui.Button(Name))
        {
            //Taken from ZaZaClient
            int num = 0;
            for (int i = 0; i < 400; i++)
            {
                Item item = Main.item[i];
                if (item.active && item.type > 0 && !item.beingGrabbed)
                {
                    item.active = false;
                    item.type = 0;
                    item.stack = 0;
                    num++;
                    if (Main.netMode == 1)
                    {
                        Util.FalsePlayerPacket(new Vector2(item.position.X, item.position.Y));
                        NetMessage.SendData(21, -1, -1, null, i, 0f, 0f, 0f, 0, 0, 0);
                        NetMessage.SendData(13, -1, -1, null, Main.myPlayer, 0f, 0f, 0f, 0, 0, 0);
                    }
                }
            }
            ClientLoader.Console.WriteLine("Total items removed: " + num);
        }
    }
}
