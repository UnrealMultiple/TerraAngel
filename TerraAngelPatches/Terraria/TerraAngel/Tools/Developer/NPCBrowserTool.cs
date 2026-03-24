using Terraria.DataStructures;

namespace TerraAngel.Tools.Developer;

public class NPCBrowserTool : Tool
{
    public override string Name => GetString("NPC Browser");

    public override ToolTabs Tab => ToolTabs.NewTab;

    private static readonly int[] BossID = [4, 13, 50, 126, 125, 134, 127, 128, 131, 129, 130, 222, 245, 266, 370, 657, 668, 398];

    public string NPCSearch = "";

    public override void DrawUI(ImGuiIOPtr io)
    {
        if (ImGui.Button(GetString("Spawn All Boss")))
            SpawnAllBoss();
        ImGui.TextUnformatted(GetString("Search")); 
        ImGui.SameLine();

        ImGui.InputText("##NPCSearch", ref NPCSearch, 64);
        ImGui.SameLine();
        ImGuiUtil.HelpMarker(GetString("Search npc name or id"));

        bool searchEmpty = NPCSearch.Length == 0;
        
        if (ImGui.BeginChild("NPCBrowserScrolling"))
        {
            for (int i = NPCID.NegativeIDCount + 1; i < NPCID.Count; i++)
            {
                if (i == NPCID.None)
                    continue;
                
                if (
                    searchEmpty ||
                    Lang.GetNPCName(i).Value.ToLower().Contains(NPCSearch.ToLower()) ||
                    i.ToString().StartsWith(NPCSearch))
                {
                    ImGui.PushID($"NBI{i}");
                    if (ImGui.Selectable($"{Lang.GetNPCName(i).Value} ({i})"))
                    {
                        SpawnNPC(i);
                    }

                    if (ImGui.BeginItemTooltip())
                    {
                        ImGuiUtil.DrawNPCDelayLoad(i);
                        ImGui.EndTooltip();
                    }
                    ImGui.PopID();
                }
            }

            ImGui.EndChild();
        }
    }

    public override void Update()
    {
        
    }
    
    public static void SpawnNPC(int npcID)
    {
        if (Main.netMode == 0)
        {
            var coords = Main.LocalPlayer.Center.ToPoint();
            NPC.NewNPC(new EntitySource_SpawnNPC(), coords.X, coords.Y, npcID);
        }
    }

    public static void SpawnAllBoss()
    {
        foreach (var id in BossID)
        {
            SpawnBoss(id);
        }
    }

    public static void SpawnBoss(int npcID)
    {
        switch (Main.netMode)
        {
            case 0:
                NPC.SpawnOnPlayer(Main.LocalPlayer.whoAmI, npcID);
                break;
            case 1:
                PacketBuilder.FastSendPacket(MessageID.SpawnBossUseLicenseStartEvent, b =>
                {
                    b.Write((short)Main.myPlayer);
                    b.Write((short)npcID);
                });
                break;
        }
    }
}