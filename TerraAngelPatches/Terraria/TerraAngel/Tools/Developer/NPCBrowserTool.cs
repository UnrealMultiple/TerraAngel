using Terraria.DataStructures;

namespace TerraAngel.Tools.Developer;

public class NPCBrowserTool : Tool
{
    public override string Name => GetString("NPC Browser");

    public override ToolTabs Tab => ToolTabs.NewTab;

    public string NPCSearch = "";

    public override void DrawUI(ImGuiIOPtr io)
    {
        ImGui.TextUnformatted(GetString("Search")); 
        ImGui.SameLine();

        ImGui.InputText("##NPCSearch", ref NPCSearch, 64);
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
}