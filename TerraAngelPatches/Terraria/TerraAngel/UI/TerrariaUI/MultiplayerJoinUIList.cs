﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace TerraAngel.UI.TerrariaUI;

public class MultiplayerJoinUIList : UIState, IHaveBackButtonCommand
{
    private readonly UIElement RootElement;

    private readonly UIPanel BackgroundPanel;

    private readonly UIList ServerListContainer;

    private readonly UIAutoScaleTextTextPanel<string> BackButton;

    private readonly UIAutoScaleTextTextPanel<string> AddServerButton;

    private readonly UIAutoScaleTextTextPanel<string> JoinServerButton;

    private readonly MultiplayerAddServerUI AddServerUI = new MultiplayerAddServerUI();

    public bool NeedsUpdate = false;

    public MultiplayerJoinUIList()
    {
        RootElement = new UIElement
        {
            Width = { Percent = 0.8f },
            MaxWidth = { Pixels = 600, Percent = 0f, },
            Top = { Pixels = 110 },
            Height = { Pixels = -110, Percent = 1f },
            HAlign = 0.5f
        };

        BackgroundPanel = new UIPanel
        {
            Width = { Percent = 1f },
            Height = { Pixels = -210, Percent = 1f },
            BackgroundColor = UIUtil.BGColor * 0.98f,
            Top = { Pixels = 95 }
        };

        RootElement.Append(BackgroundPanel);

        ServerListContainer = new UIList
        {
            Width = { Pixels = -25, Percent = 1f },
            Height = { Percent = 1f },
            Top = { Pixels = 8 },
            ListPadding = 5f,
            ManualSortMethod = (x) => { }
        };

        BackgroundPanel.Append(ServerListContainer);

        UIScrollbar settingsScrollbar = new UIScrollbar
        {
            Height = { Pixels = -12f, Percent = 1f },
            Top = { Pixels = 12 },
            HAlign = 1f,
            BarColor = UIUtil.ScrollbarColor,
        }.WithView(100f, 1000f);

        BackgroundPanel.Append(settingsScrollbar);

        ServerListContainer.SetScrollbar(settingsScrollbar);

        BackButton = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("UI.Back"))
        {
            Width = new StyleDimension(-5f, 1.0f/3),
            Height = { Pixels = 40 },
            Top = { Pixels = -65 },
            BackgroundColor = UIUtil.ButtonColor * 0.98f,
            VAlign = 1f,
        }.WithFadedMouseOver();
        BackButton.SetSnapPoint("Back", 0);

        BackButton.OnLeftClick += (x, y) =>
        {
            HandleBackButtonUsage();
        };

        RootElement.Append(BackButton);

        AddServerButton = new UIAutoScaleTextTextPanel<string>(GetString("Add New Server"))
        {
            Width = new StyleDimension(-5f, 1.0f/3),
            Height = { Pixels = 40 },
            Top = { Pixels = -65 },
            BackgroundColor = UIUtil.ButtonColor * 0.98f,
            VAlign = 1f,
            HAlign = 0.5f,
        }.WithFadedMouseOver();
        AddServerButton.SetSnapPoint("Add Server", 0);

        AddServerButton.OnLeftClick += (x, y) =>
        {
            SoundEngine.PlaySound(10);
            Main.MenuUI.SetState(AddServerUI);
        };

        RootElement.Append(AddServerButton);

        JoinServerButton = new UIAutoScaleTextTextPanel<string>(GetString("Join Server"))
        {
            Width = new StyleDimension(-5f, 1.0f/3),
            Height = { Pixels = 40 },
            Top = { Pixels = -65 },
            BackgroundColor = UIUtil.ButtonColor * 0.98f,
            VAlign = 1f,
            HAlign = 1.0f,
        }.WithFadedMouseOver();
        JoinServerButton.SetSnapPoint("Join Server", 0);

        JoinServerButton.OnLeftClick += (x, y) =>
        {
            SoundEngine.PlaySound(10);
            Main.menuMode = 13;
            Main.MenuUI.SetState(null!);
            Main.clrInput();
        };

        RootElement.Append(JoinServerButton);
    }

    public override void OnInitialize()
    {
        NeedsUpdate = true;

        Append(RootElement);
    }

    public override void OnActivate()
    {
        base.OnActivate();
        UILinkPointNavigator.ChangePoint(3003);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        SetupGamepadPoints(spriteBatch);

        if (InputSystem.IsKeyDown(Keys.Escape))
            HandleBackButtonUsage();
    }

    public override void Update(GameTime gameTime)
    {
        if (NeedsUpdate)
        {
            ServerListContainer.Clear();

            ClientConfig.Settings.MultiplayerServers.Sort((x, y) => y.LastInteractedTime.CompareTo(x.LastInteractedTime));

            var index = 0;
            foreach (MultiplayerServerInfo server in ClientConfig.Settings.MultiplayerServers)
            {
                ServerListContainer.Add(new ServerCardUI(server, index++)
                {
                    Width = { Percent = 1.0f, },
                    Height = { Pixels = 80f }
                });
            }

            ServerListContainer.Recalculate();

            NeedsUpdate = false;
        }

        base.Update(gameTime);
    }

    public void HandleBackButtonUsage()
    {
        Main.OpenCharacterSelectUI();
        SoundEngine.PlaySound(SoundID.MenuClose);
    }

    private void SetupGamepadPoints(SpriteBatch spriteBatch)
    {
        var offset = 3000; // hardcoded
        var snapPoints = GetSnapPoints();

        UIUtil.BuildHorizontalSnapPoints(
            offset,
            snapPoints,
            ["Back", "Add Server", "Join Server"]);
        var offset2 = offset + 3;

        List<string> cardPointNames = ["Card Join Server", "Card Delete Server"];
        var height = ClientConfig.Settings.MultiplayerServers.Count;
        var stride = cardPointNames.Count;
        UIUtil.BuildVerticalListSnapPoints(offset2, snapPoints, cardPointNames, height);

        UIUtil.SetHorizontalSnapPoints(offset2 + (height - 1) * stride, stride, x =>
        {
            x.Down = offset + 1;
        });

        UIUtil.SetHorizontalSnapPoints(offset, 3, x =>
        {
            x.Up = offset2 + (height - 1) * stride;
        });

        if (UILinkPointNavigator.CurrentPoint >= offset2 + ClientConfig.Settings.MultiplayerServers.Count * stride)
        {
            UILinkPointNavigator.ChangePoint(3000);
        }
    }

    private class ServerCardUI : UIElement
    {
        private UIPanel BackgroundPanel;

        private UIText ServerNameText;

        private UIText ServerIPPortText;

        private MultiplayerServerInfo ServerInfo;

        private UIImageButton DeleteButton;

        public ServerCardUI(MultiplayerServerInfo serverInfo, int index)
        {
            ServerInfo = serverInfo;

            BackgroundPanel = new UIPanel
            {
                Width = { Percent = 1.0f },
                Height = { Percent = 1.0f },
                BorderColor = Color.Black,
                BackgroundColor = UIUtil.BGColor2
            }.WithFadedMouseOver(origColor: UIUtil.BGColor2, hoverdColor: UIUtil.ButtonHoveredColor);
            BackgroundPanel.SetSnapPoint("Card Join Server", index);

            ServerNameText = new UIText(ServerInfo.Name, 0.6f, true)
            {
                HAlign = 0.0f,
                VAlign = 0.0f,
            };

            BackgroundPanel.Append(ServerNameText);

            ServerIPPortText = new UIText($"{ServerInfo.IP}:{ServerInfo.Port}")
            {
                HAlign = 0.0f,
                VAlign = 1.0f,
            };

            BackgroundPanel.Append(ServerIPPortText);

            DeleteButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"))
            {
                HAlign = 1.0f,
                VAlign = 1.0f,
                Top = { Pixels = 5 }
            };
            DeleteButton.SetSnapPoint("Card Delete Server", index);

            DeleteButton.OnLeftClick += (x, y) =>
            {
                ClientConfig.Settings.MultiplayerServers.Remove(ServerInfo);

                ClientLoader.MultiplayerJoinUI!.NeedsUpdate = true;
            };

            BackgroundPanel.Append(DeleteButton);

            BackgroundPanel.OnLeftDoubleClick += (x, y) =>
            {
                ServerInfo.LastInteractedTime = DateTime.UtcNow;

                ClientLoader.MultiplayerJoinUI!.NeedsUpdate = true;

                Main.autoPass = false;
                Netplay.ListenPort = serverInfo.Port;
                Main.getIP = serverInfo.IP;
                Netplay.SetRemoteIPAsync(serverInfo.IP, Main.StartClientGameplay);
                Main.menuMode = 14;
                Main.statusText = Language.GetTextValue("Net.ConnectingTo", serverInfo.IP);
            };

            Append(BackgroundPanel);
        }
    }
}

public class MultiplayerServerInfo
{
    public string Name = "";

    public string IP = "";

    public ushort Port = 0;

    public DateTime LastInteractedTime = DateTime.UtcNow;
}