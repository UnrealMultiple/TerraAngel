﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using TerraAngel.Inspector.Tools;
using TerraAngel.Tools.Inspector;
using TerraAngel.UI.TerrariaUI;
using TerraAngel.WorldEdits;
using Terraria.GameContent;
using TerraAngel.UI.ClientWindows.Console;

namespace TerraAngel.Graphics;

public class ClientRenderer : ImGuiRenderer
{
    public List<ClientWindow> ClientWindows = new List<ClientWindow>();

    public bool ShowMetricsWindow = false;

    public bool GlobalToggle = true;

    public List<WorldEdit> WorldEdits = new List<WorldEdit>() { new WorldEditBrush(), new WorldEditCopyPaste() };

    public int CurrentWorldEditIndex = -1;

    public WorldEdit? CurrentWorldEdit
    {
        get
        {
            if (CurrentWorldEditIndex == -1)
            {
                return null;
            }
            else
            {
                return WorldEdits[CurrentWorldEditIndex];
            }
        }
    }

    private bool OpenUpdatePopup = false;

    public ClientRenderer(Game game)
        : base(game)
    {
        Init();
    }

    public void Init()
    {
        TileUtil.Init();
        RebuildFontAtlas();
        AddWindow(ClientLoader.ChatWindow = new ChatWindow());
        ClientLoader.ConsoleWindow = (ConsoleWindow)AddWindow(new ConsoleWindow());

        Main.instance.Exiting += (args, o) =>
        {
            ClientConfig.WriteToFile();
            ClientLoader.WindowManager?.WriteToFile();
        };

        unsafe
        {
            TextInputEXT.TextInput += (c) =>
            {
                ImGuiIOPtr io = ImGui.GetIO();
                if (io.NativePtr != null)
                {
                    io.AddInputCharacter(c);
                }
            };

            if (!OperatingSystem.IsWindows())
            {
                var pio = ImGui.GetPlatformIO();
                pio.Platform_GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(new GetClipboardTextFn(static _ =>
                {
                    return new StringBuilder(SDL2.SDL.SDL_GetClipboardText());
                }));
                pio.Platform_SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(new SetClipboardTextFn(static (_, text) =>
                {
                    SDL2.SDL.SDL_SetClipboardText(text.ToString());
                }));
            }
        }

        Task.Run(async () =>
        {
            bool updateAvailable = await UpdateChecker.IsUpdateAvailableAsync();
            if (updateAvailable)
            {
                OpenUpdatePopup = true;
            }
        });
    }

    protected override void SetupInput()
    {
        base.SetupInput();

        ImGuiStylePtr style = ImGui.GetStyle();
        RangeAccessor<Vector4> colors = style.Colors;

        colors[(int)ImGuiCol.Text] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.10f, 0.10f, 0.10f, 1.00f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.19f, 0.19f, 0.19f, 0.92f);
        colors[(int)ImGuiCol.Border] = new Vector4(0.19f, 0.19f, 0.19f, 0.29f);
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.24f);
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.05f, 0.05f, 0.05f, 0.54f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.19f, 0.19f, 0.19f, 0.54f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.20f, 0.22f, 0.23f, 1.00f);
        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.06f, 0.06f, 0.06f, 1.00f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.05f, 0.05f, 0.05f, 0.54f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.34f, 0.34f, 0.34f, 0.54f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.40f, 0.40f, 0.40f, 0.54f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.56f, 0.56f, 0.56f, 0.54f);
        colors[(int)ImGuiCol.CheckMark] = new Vector4(0.33f, 0.67f, 0.86f, 1.00f);
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.34f, 0.34f, 0.34f, 0.54f);
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.56f, 0.56f, 0.56f, 0.54f);
        colors[(int)ImGuiCol.Button] = new Vector4(0.05f, 0.05f, 0.05f, 0.54f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.19f, 0.19f, 0.19f, 0.54f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.20f, 0.22f, 0.23f, 1.00f);
        colors[(int)ImGuiCol.Header] = new Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.00f, 0.00f, 0.00f, 0.36f);
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.20f, 0.22f, 0.23f, 0.33f);
        colors[(int)ImGuiCol.Separator] = new Vector4(0.28f, 0.28f, 0.28f, 0.29f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.44f, 0.44f, 0.44f, 0.29f);
        colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.40f, 0.44f, 0.47f, 1.00f);
        colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.28f, 0.28f, 0.28f, 0.29f);
        colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.44f, 0.44f, 0.44f, 0.29f);
        colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.40f, 0.44f, 0.47f, 1.00f);
        colors[(int)ImGuiCol.Tab] = new Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.TabHovered] = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.TabSelected] = new Vector4(0.20f, 0.20f, 0.20f, 0.36f);
        colors[(int)ImGuiCol.TabDimmed] = new Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.TabDimmedSelected] = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.33f, 0.67f, 0.86f, 1.00f);
        colors[(int)ImGuiCol.DockingEmptyBg] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotLines] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogram] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.28f, 0.28f, 0.28f, 0.29f);
        colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);
        colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.20f, 0.22f, 0.23f, 1.00f);
        colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.33f, 0.67f, 0.86f, 1.00f);
        colors[(int)ImGuiCol.NavCursor] = new Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 0.00f, 0.00f, 0.70f);
        colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(1.00f, 0.00f, 0.00f, 0.20f);
        colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0f, 0.00f, 0.00f, 0f);

        ClientConfig.AfterReadLater();
    }

    public void SetupWindows()
    {
        AddWindow(new MainWindow());
        AddWindow(new DrawWindow());
        AddWindow(new StatsWindow());
        AddWindow(NetMessageWindow.Instance);
        AddWindow(InspectorTool.InspectorWindow);
        AddWindow(new StyleEditorWindow());
        AddWindow(new TimingMetricsWindow());
        AddWindow(new ImeWindow());
    }

    public void PreUpdate()
    {
        ToolManager.GetTool<NPCInspectorTool>().PreDraw();
    }

    public void Update()
    {
        Lighting.AbleToProcessPerFrameLights = true;

        Time.UpdateUpdate();

        ClientConfig.Update();
    }

    public void Render()
    {
        UIInputText.UUpdate();

        MetricsTimer renderTimer = TimeMetrics.GetMetricsTimer("Client Draw");
        renderTimer.Start();
        PreDraw();
        PreRender();
        Draw();
        PostDraw();
        PostRender();
        renderTimer.Stop();
    }

    public void PreDraw()
    {
        Time.UpdateDraw();

        InputSystem.EndUpdateInput();

        InputSystem.UpdateInput();
    }

    public void PostDraw()
    {
        if (Main.netMode == 1 && Netplay.Connection.State <= 3)
        {
            Main.tile.LoadedTileSections = new bool[Main.tile.Width / Main.sectionWidth, Main.tile.Height / Main.sectionHeight];
        }

        if (!ClientConfig.Settings.UseDiscordRPC && ClientLoader.DiscordClient is not null)
        {
            ClientLoader.DiscordClient.Dispose();
            ClientLoader.DiscordClient = null;
        }
        if (ClientConfig.Settings.UseDiscordRPC && ClientLoader.DiscordClient is null)
        {
            ClientLoader.InitDiscord();
        }

        for (int i = 0; i < Plugin.PluginLoader.LoadedPlugins.Count; i++)
        {
            Plugin.Plugin plugin = Plugin.PluginLoader.LoadedPlugins[i];

            if (plugin is not null && plugin.IsInited)
            {
                plugin.Update();
            }
        }

        if (ClientConfig.Settings.ClearChatThroughWorldChanges && Main.gameMenu)
        {
            ClientLoader.ChatWindow?.ChatItems.Clear();
        }

        for (int i = 0; i < 10 && ImGuiUtil.ItemIdsToLoad.Count > 0; i++)
        {
            int id = ImGuiUtil.ItemIdsToLoad.Dequeue();
            Main.instance.LoadItem(id);
            ImGuiUtil.ItemImages[id] = BindTexture(TextureAssets.Item[id].Value);
        }

    }

    public void Draw()
    {
        if (Main.hideUI && !Main.gameMenu)
            return;

        ImGuiIOPtr io = ImGui.GetIO();

        if (InputSystem.IsKeyPressed(ClientConfig.Settings.ToggleUIVisibility) && !InputSystem.Ctrl)
        {
            GlobalToggle = !GlobalToggle;
        }

        for (int i = 0; i < ClientWindows.Count; i++)
        {
            ClientWindow window = ClientWindows[i];

            if (window.IsToggleable && InputSystem.IsKeyPressed(window.ToggleKey))
            {
                window.IsEnabled = !window.IsEnabled;

                if (window.IsEnabled)
                {
                    window.OnShow();
                }
                else
                {
                    window.OnHide();
                }
            }

            if ((!window.IsGlobalToggle || GlobalToggle) && window.IsEnabled)
            {
                window.Draw(io);
            }

            window.Update();
        }

        if (ShowMetricsWindow)
        {
            ImGui.ShowMetricsWindow(ref ShowMetricsWindow);
        }

        if (OpenUpdatePopup)
        {
            ImGui.OpenPopup("Update Available");
            OpenUpdatePopup = false;
        }

        bool dontClosePopup = true;

        ImGui.PushFont(ClientAssets.GetMonospaceFont(16f));
        if (ImGui.BeginPopupModal(GetString("Update Available"), ref dontClosePopup))
        {
            ImGui.Text(GetString("There is an update for TerraAngel available on GitHub"));
            ImGui.Text($"v{ClientLoader.TerraAngelVersion} {Icon.ArrowRight} v{UpdateChecker.NextUpdateVersion}");

            if (!dontClosePopup)
                ImGui.CloseCurrentPopup();

            ImGui.EndPopup();
        }
        ImGui.PopFont();
    }

    public ClientWindow AddWindow(ClientWindow window)
    {
        ClientWindows.Add(window);
        window.IsEnabled = window.DefaultEnabled;
        return window;
    }

    public void RemoveWindow(ClientWindow window)
    {
        ClientWindows.Remove(window);
    }
}
