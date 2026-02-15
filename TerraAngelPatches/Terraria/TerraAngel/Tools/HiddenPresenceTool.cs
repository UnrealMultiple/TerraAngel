using System;
using Microsoft.Xna.Framework.Input;
using TerraAngel.Config;
using TerraAngel.UI;
using ImGuiNET;
using TerraAngel.UI.ClientWindows;


namespace TerraAngel.Tools
{
    public class HiddenPresenceTool : Tool
    {
        // 这是一个不在菜单栏显示的工具，只用来跑后台逻辑
        public override string Name => "Hidden Presence Manager";
        public override ToolTabs Tab => ToolTabs.None; // 不显示在任何标签页

        public override void Update()
        {
            // 监听你在 Config 里设置的那个快捷键
            if (InputSystem.IsKeyPressed(ClientConfig.Settings.ToggleBroadcastPresenceKey))
            {
                // 切换状态
                ClientConfig.Settings.BroadcastPresence = !ClientConfig.Settings.BroadcastPresence;

                // 定义文本
                string statusText = ClientConfig.Settings.BroadcastPresence ? GetString("Enabled") : GetString("Disabled");
                string Message = GetString($"BroadcastPresence is ")+ statusText;

                // 在控制台打印一条消息，让你知道切换成功了（只有你看得到）
                ClientLoader.Console.WriteLine(Message);
                
                // 可选：在聊天栏回显一下，方便确认
                ClientLoader.Chat.WriteLine(Message);
            
            }
        }
        
        // 不需要绘制 UI，留空即可
        public override void DrawUI(ImGuiIOPtr io) { }
    }
}