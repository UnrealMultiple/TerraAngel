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
                WorldItem item = Main.item[i];
                if (item.active && item.type > 0 && !item.beingGrabbed)
                {
                    Vector2 itemPos = item.position;
                    item.TurnToAir();
                    num++;
                    if (Main.netMode == 1)
                    {
                        Util.FalsePlayerPacket(itemPos);
                        NetMessage.SendData(21, -1, -1, null, i, 0f, 0f, 0f, 0, 0, 0);
                        NetMessage.SendData(13, -1, -1, null, Main.myPlayer, 0f, 0f, 0f, 0, 0, 0);
                    }
                }
            }
            ClientLoader.Console.WriteLine("Total items removed: " + num);
        }
    }
}
