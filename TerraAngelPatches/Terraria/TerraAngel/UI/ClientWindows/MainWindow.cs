using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerraAngel.WorldEdits;

namespace TerraAngel.UI.ClientWindows;

public class MainWindow : ClientWindow
{
    public override bool DefaultEnabled => true;

    public override bool IsToggleable => false;

    public override string Title => GetString("Main Window");

    public override bool IsGlobalToggle => true;

    public override void Draw(ImGuiIOPtr io)
    {
        ImGui.PushFont(ClientAssets.GetMonospaceFont(16f));

        Vector2 windowSize = io.DisplaySize / new Vector2(3f, 2f);

        ImGui.SetNextWindowPos(new Vector2(0, io.DisplaySize.Y - windowSize.Y), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.FirstUseEver);

        ImGui.Begin(GetString("Main window"));

        if (!Main.gameMenu && Main.CanUpdateGameplay)
        {
            DrawInGameWorld(io);
        }
        else
        {
            DrawInMenu(io);
        }


        ImGui.End();

        ImGui.PopFont();
    }

    public void DrawInGameWorld(ImGuiIOPtr io)
    {
        if (ImGui.BeginTabBar("##MainTabBar"))
        {
            if (ImGui.BeginTabItem(GetString("Tools")))
            {
                if (ImGui.BeginTabBar("ToolBar"))
                {
                    if (ImGui.BeginTabItem(GetString("Main Tools")))
                    {
                        foreach (Tool cringe in ToolManager.GetToolsOfTab(ToolTabs.MainTools))
                        {
                            cringe.DrawUI(io);
                        }
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(GetString("Items")))
                    {
                        if (ImGui.BeginTabBar("ItemBar"))
                        {
                            if (ImGui.BeginTabItem(GetString("Item Browser")))
                            {
                                ToolManager.GetTool<ItemBrowserTool>().DrawUI(io);
                                ImGui.EndTabItem();
                            }
                            if (ImGui.BeginTabItem(ToolManager.GetTool<ItemEditorTool>().Name))
                            {
                                ToolManager.GetTool<ItemEditorTool>().DrawUI(io);
                                ImGui.EndTabItem();
                            }
                            ImGui.EndTabBar();
                        }
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(GetString("Automation")))
                    {
                        foreach (Tool cringe in ToolManager.GetToolsOfTab(ToolTabs.AutomationTools))
                        {
                            cringe.DrawUI(io);
                        }
                        ImGui.EndTabItem();
                    }

                    foreach (Tool cringe in ToolManager.GetToolsOfTab(ToolTabs.NewTab))
                    {
                        if (ImGui.BeginTabItem(cringe.Name))
                        {
                            cringe.DrawUI(io);
                            ImGui.EndTabItem();
                        }
                    }

                    ImGui.EndTabBar();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(GetString("Visuals")))
            {
                if (ImGui.BeginTabBar("VisualBar"))
                {
                    if (ImGui.BeginTabItem(GetString("Utility")))
                    {
                        foreach (Tool cringe in ToolManager.GetToolsOfTab(ToolTabs.VisualTools))
                        {
                            cringe.DrawUI(io);
                        }

                        if (ImGui.Button(GetString("Reveal Map")))
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    Stopwatch watch = Stopwatch.StartNew();

                                    if (Main.netMode == 1)
                                    {
                                        for (var x = 0; x < Main.maxSectionsX; x++)
                                        {
                                            for (var y = 0; y < Main.maxSectionsY; y++)
                                            {
                                                NetMessage.SendData(MessageID.RequestSection, -1, -1, null, x, y);
                                            }
                                        }

                                        while (watch.Elapsed.TotalSeconds <= 120)
                                        {
                                            var leftSections = Main.tile.LoadedTileSections.Cast<bool>().Count(x=> !x);

                                            if (leftSections == 0)
                                            {
                                                break;
                                            }
                                            ClientLoader.Console.WriteLine(GetString($"Map left {leftSections} sections"));
                                            await Task.Delay(500);
                                        }
                                    }
                                    
                                    int xlen = Main.Map.MaxWidth;
                                    int ylen = Main.Map.MaxHeight;
                                    for (int x = 0; x < xlen; x++)
                                    {
                                        for (int y = 0; y < ylen; y++)
                                        {
                                            if (Main.netMode == 0 || Main.tile.IsTileInLoadedSection(x, y))
                                            {
                                                Main.Map.Update(x, y, 255);
                                            }
                                        }
                                    }
                                    watch.Stop();
                                    ClientLoader.Console.WriteLine(GetString($"Map took {watch.Elapsed.TotalSeconds:F3}s"));
                                    Main.refreshMap = true;
                                }
                                catch (Exception e)
                                {
                                    ClientLoader.Console.WriteError($"{e}");
                                }
                            });
                        }
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem(GetString("ESP")))
                    {
                        foreach (Tool cringe in ToolManager.GetToolsOfTab(ToolTabs.ESPTools))
                        {
                            cringe.DrawUI(io);
                        }

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem(GetString("Lighting")))
                    {
                        foreach (Tool cringe in ToolManager.GetToolsOfTab(ToolTabs.LightingTools))
                        {
                            cringe.DrawUI(io);
                        }
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(GetString("World Edit")))
            {
                if (ImGui.BeginTabBar("WorldEditBar"))
                {
                    for (int i = 0; i < ClientLoader.MainRenderer!.WorldEdits.Count; i++)
                    {
                        WorldEdit worldEdit = ClientLoader.MainRenderer.WorldEdits[i];
                        if (worldEdit.DrawUITab(io))
                        {
                            ClientLoader.MainRenderer.CurrentWorldEditIndex = i;
                        }
                    }
                    ImGui.EndTabBar();
                }
                ImGui.EndTabItem();
            }
            else
            {
                ClientLoader.MainRenderer!.CurrentWorldEditIndex = -1;
            }

            if (ImGui.BeginTabItem(GetString("Misc")))
            {
                foreach (Tool cringe in ToolManager.GetToolsOfTab(ToolTabs.MiscTools))
                {
                    cringe.DrawUI(io);
                }

                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private int framesToShowUUIDFor = 0;
    private string customUUIDInput = "";
    private int invalidUUIDFrames = 0;
    private int customUUIDAppliedFrames = 0;

    public void DrawInMenu(ImGuiIOPtr io)
    {
        if (ImGui.BeginTabBar("##MainTabBar"))
        {
            if (ImGui.BeginTabItem(GetString("Tools")))
            {
                if (ImGui.Button(GetString($"{Icon.Refresh} Client UUID")))
                {
                    Main.clientUUID = Guid.NewGuid().ToString();
                    Main.SaveSettings();
                }
                ImGui.SameLine();
                if (ImGui.Button(GetString("Click to reveal")))
                {
                    framesToShowUUIDFor = 600;
                }
                ImGui.SameLine();
                if (ImGui.Button(GetString("Click to copy")))
                {
                    ImGui.SetClipboardText(Main.clientUUID);
                }

                if (framesToShowUUIDFor > 0)
                {
                    framesToShowUUIDFor--;
                    ImGui.Text(Main.clientUUID);
                }

                ImGui.Separator();

                ImGui.Text(GetString("Set Custom UUID:"));
                ImGui.InputText("##CustomUUID", ref customUUIDInput, 256);

                if (ImGui.Button(GetString("Apply Custom UUID")))
                {
                    if (!string.IsNullOrWhiteSpace(customUUIDInput))
                    {
                        Main.clientUUID = customUUIDInput;
                        Main.SaveSettings();
                        customUUIDInput = "";
                        customUUIDAppliedFrames = 60;
                    }
                    else
                    {
                        invalidUUIDFrames = 60;
                    }
                }
                if (customUUIDAppliedFrames > 0)
                {
                    customUUIDAppliedFrames--;
                    ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), GetString("Custom UUID applied successfully!"));
                }

                if (invalidUUIDFrames > 0)
                {
                    invalidUUIDFrames--;
                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), GetString("UUID cannot be empty!"));
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    public override void Update()
    {
        base.Update();
    }
}
