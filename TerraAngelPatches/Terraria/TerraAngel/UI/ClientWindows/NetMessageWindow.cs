using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using TrProtocol;
using MessageID = Terraria.ID.MessageID;

namespace TerraAngel.UI.ClientWindows;

public class NetMessageWindow : ClientWindow
{
    public static NetMessageWindow Instance { get; private set; } = new NetMessageWindow();

    public override bool IsCheat => false;
    public override bool IsToggleable => true;
    
    public override bool DefaultEnabled => false;

    public override string Title => GetString("Net Debugger");

    public override Keys ToggleKey => ClientConfig.Settings.ToggleNetDebugger;

    public override bool IsGlobalToggle => false;

    public List<NetPacketLog>[] SentNetPacketLogs = new List<NetPacketLog>[MessageID.Count];

    public List<NetPacketLog>[] ReceivedNetPacketLogs = new List<NetPacketLog>[MessageID.Count];

    public List<NetPacketLog>[] AllNetPacketLogs = new List<NetPacketLog>[MessageID.Count];

    public bool FancyLoggingEnabled = false;

    public string FancyLogsFilter = "";

    public string FancyLogsTracesFilter = "";

    public string FancyLogsDetailsFilter = "";

    public HashSet<int> MessagesTypesToLogTraces = new HashSet<int>();
    
    public HashSet<int> MessagesTypesToLogDetails = new HashSet<int>();

    public bool FancyLogsShowSent = true;

    public bool FancyLogsShowReceived = true;

    private bool[] TreeMessageTypesOpen = new bool[MessageID.Count];

    public List<RawNetPacketLog> RawNetPacketLogs = new List<RawNetPacketLog>();

    public bool RawLoggingEnabled = false;

    public bool RawLogsShowSent = true;

    public bool RawLogsShowReceived = true;

    public NetMessageWindow()
    {
        for (int i = 0; i < MessageID.Count; i++)
        {
            SentNetPacketLogs[i] = new List<NetPacketLog>();
            ReceivedNetPacketLogs[i] = new List<NetPacketLog>();
            AllNetPacketLogs[i] = new List<NetPacketLog>();
        }
    }

    public override void Draw(ImGuiIOPtr io)
    {
        ImGui.PushFont(ClientAssets.GetMonospaceFont(16f));
        bool open = IsEnabled;
        ImGui.Begin(GetString("Net Debugger"), ref open, ImGuiWindowFlags.MenuBar);
        IsEnabled = open;

        bool rawMessagesOepn = false;

        if (ImGui.BeginTabBar("NetDebuggerTabBar"))
        {
            if (ImGui.BeginTabItem(GetString("Net Message Logs")))
            {
                ImGui.Text(GetString("Search:"));
                ImGui.SameLine();

                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X / 2.8f);
                ImGui.InputText("##FancyLogsFilter", ref FancyLogsFilter, 512);
                ImGui.PopItemWidth();

                ImGui.Checkbox(GetString("Log Traffic"), ref FancyLoggingEnabled);
                ImGui.SameLine();
                ImGui.Checkbox($"{Icon.ArrowUp}", ref FancyLogsShowSent);
                ImGui.SameLine();
                ImGui.Checkbox($"{Icon.ArrowDown}", ref FancyLogsShowReceived);

                ImGui.Text(GetString("Packets with traces:")); ImGui.SameLine();
                unsafe
                {
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X / 2.8f); ImGui.InputText("##TraceFilter", ref FancyLogsTracesFilter, 512, ImGuiInputTextFlags.CallbackCharFilter, (x) =>
                    {
                        switch (x->EventFlag)
                        {
                            case ImGuiInputTextFlags.CallbackCharFilter:
                                if (char.IsNumber((char)x->EventChar) || x->EventChar == ',' || x->EventChar == ' ' || x->EventChar == '*') return 0;
                                return 1;
                        }
                        return 0;
                    }); ImGui.PopItemWidth();
                }
                MessagesTypesToLogTraces = FancyLogsTracesFilter.Contains('*')
                    ? new HashSet<int>(Enumerable.Range(0, MessageID.Count))
                    : new HashSet<int>(FancyLogsTracesFilter
                        .Split(',')
                        .Select(x =>
                        {
                            if (int.TryParse(x.Trim(), out int a)) { return a; } return -1;
                        })
                        .Where(x => x != -1));
                
                ImGui.Text(GetString("Packets with details:")); ImGui.SameLine();
                unsafe
                {
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X / 2.8f); ImGui.InputText("##DetailFilter", ref FancyLogsDetailsFilter, 512, ImGuiInputTextFlags.CallbackCharFilter, (x) =>
                    {
                        switch (x->EventFlag)
                        {
                            case ImGuiInputTextFlags.CallbackCharFilter:
                                if (char.IsNumber((char)x->EventChar) || x->EventChar == ',' || x->EventChar == ' ' || x->EventChar == '*') return 0;
                                return 1;
                        }
                        return 0;
                    }); ImGui.PopItemWidth();
                }
                MessagesTypesToLogDetails = FancyLogsDetailsFilter.Contains('*')
                    ? new HashSet<int>(Enumerable.Range(0, MessageID.Count))
                    : new HashSet<int>(FancyLogsDetailsFilter
                        .Split(',')
                        .Select(x =>
                        {
                            if (int.TryParse(x.Trim(), out int a)) { return a; } return -1;
                        })
                        .Where(x => x != -1));

                if (ImGui.BeginChild("##MessageLogScrolling"))
                {

                    string fancyLogsFilterLower = FancyLogsFilter.ToLower();

                    bool PassesFilter(int i, string displayName)
                    {
                        if (FancyLogsFilter.Length > 0)
                        {
                            if (int.TryParse(FancyLogsFilter, out int p))
                            {
                                if (i != p)
                                {
                                    return false;
                                }
                            }
                            else if (!displayName.ToLower().Contains(fancyLogsFilterLower))
                            {
                                return false;
                            }
                        }

                        return true;
                    }

                    StringBuilder builder = new StringBuilder();

                    for (int i = 0; i < MessageID.Count; i++)
                    {
                        string displayName = $"{InternalRepresentation.GetMessageIDName(i)}/{i}";

                        if (!PassesFilter(i, displayName))
                            continue;

                        List<NetPacketLog> packetLogProvider = AllNetPacketLogs[i];

                        if (FancyLogsShowSent && !FancyLogsShowReceived)
                        {
                            packetLogProvider = SentNetPacketLogs[i];
                        }
                        else if (FancyLogsShowReceived && !FancyLogsShowSent)
                        {
                            packetLogProvider = ReceivedNetPacketLogs[i];
                        }

                        string packetLogCountString = packetLogProvider.Count == 0 ? "" : packetLogProvider.Count.ToString();

                        if (ImGui.Selectable($"{(TreeMessageTypesOpen[i] ? Icon.TriangleDown : Icon.TriangleRight)} {displayName,-35} {packetLogCountString}###{displayName}"))
                        {
                            TreeMessageTypesOpen[i] = !TreeMessageTypesOpen[i];
                        }

                        if (TreeMessageTypesOpen[i])
                        {
                            ImGui.Indent(20f);

                            for (int j = 0; j < packetLogProvider.Count; j++)
                            {
                                ImGui.PushID(j);
                                if (packetLogProvider[j] is SentNetPacketLog sent)
                                {
                                    builder.Clear();

                                    builder.Append(GetString($"params: {sent.Number1},{sent.Number2},{sent.Number3},{sent.Number4},{sent.Number5},{sent.Number6},{sent.Number7}"));

                                    if (sent.Packet is not null)
                                    {
                                        builder.AppendLine();
                                        builder.Append($"   {sent.Packet}");
                                    }
                                    
                                    if (sent.StackTrace is not null)
                                    {
                                        builder.AppendLine();
                                        builder.Append(sent.StackTrace);
                                    }

                                    string builtString = builder.ToString();

                                    ImGui.PushStyleColor(ImGuiCol.Text, Color.LimeGreen.PackedValue);
                                    if (ImGui.Selectable($"{Icon.ArrowUp} {builtString}"))
                                    {
                                        ImGui.SetClipboardText(builtString);
                                    }
                                    ImGui.PopStyleColor();
                                }
                                else if (packetLogProvider[j] is ReceivedNetPacketLog received)
                                {
                                    builder.Clear();

                                    if (received.Packet is null)
                                    {
                                        builder.Append(GetString("Received packet"));
                                    }
                                    else
                                    {
                                        builder.Append(received.Packet);
                                    }
                                    
                                    string builtString = builder.ToString();

                                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Crimson.PackedValue);
                                    if (ImGui.Selectable($"{Icon.ArrowDown} {builtString}"))
                                    {
                                        ImGui.SetClipboardText(builtString);
                                    }
                                    ImGui.PopStyleColor();
                                }
                                ImGui.PopID();
                            }

                            ImGui.Unindent(20f);
                        }
                    }

                    ImGui.EndChild();
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(GetString("Raw Message Logs")))
            {
                rawMessagesOepn = true;

                StringBuilder builder = new StringBuilder();

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                ImGui.Checkbox(GetString("Log Traffic"), ref RawLoggingEnabled);
                ImGui.SameLine();
                ImGui.Checkbox($"{Icon.ArrowUp}", ref RawLogsShowSent);
                ImGui.SameLine();
                ImGui.Checkbox($"{Icon.ArrowDown}", ref RawLogsShowReceived);

                for (int i = 0; i < RawNetPacketLogs.Count; i++)
                {
                    ImGui.PushID(i);
                    RawNetPacketLog log = RawNetPacketLogs[i];

                    if (RawLogsShowSent && !RawLogsShowReceived && !log.Sent)
                        continue;

                    if (RawLogsShowReceived && !RawLogsShowSent && log.Sent)
                        continue;

                    builder.Clear();

                    builder.Append($"T: {log.Type,3} L: {log.Data.Length,5} B: {Convert.ToHexString(log.Data, 0, Math.Min(log.Data.Length, 250))}");
                    if (log.Data.Length >= 250)
                        builder.Append("..");

                    if (ImGui.Selectable($"{(log.Sent ? Icon.ArrowUp : Icon.ArrowDown)} {builder}"))
                    {
                        builder.Clear();

                        builder.Append($"T: {log.Type,3} L: {log.Data.Length,5} B: {Convert.ToHexString(log.Data, 0, log.Data.Length)}");

                        ImGui.SetClipboardText(builder.ToString());
                    }
                    ImGui.PopID();
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu(GetString($"{(rawMessagesOepn ? GetString("Raw ") : "")}Message Logs")))
            {
                if (ImGui.MenuItem(GetString("Clear")))
                {
                    if (rawMessagesOepn)
                    {
                        RawNetPacketLogs.Clear();
                    }
                    else
                    {
                        for (int i = 0; i < MessageID.Count; i++)
                        {
                            SentNetPacketLogs[i].Clear();
                            ReceivedNetPacketLogs[i].Clear();
                            AllNetPacketLogs[i].Clear();
                        }
                    }
                }

                if (rawMessagesOepn)
                {
                    if (ImGui.MenuItem(GetString("Copy All")))
                    {
                        StringBuilder builder = new StringBuilder();

                        for (int i = 0; i < RawNetPacketLogs.Count; i++)
                        {
                            RawNetPacketLog log = RawNetPacketLogs[i];

                            if (RawLogsShowSent && !RawLogsShowReceived && !log.Sent)
                                continue;

                            if (RawLogsShowReceived && !RawLogsShowSent && log.Sent)
                                continue;

                            builder.Append($"T: {log.Type,3} L: {log.Data.Length,5} B: {Convert.ToHexString(log.Data, 0, log.Data.Length)}");
                            builder.AppendLine();
                        }

                        ImGui.SetClipboardText(builder.ToString());
                    }
                }
                ImGui.EndMenu();
            }
            ImGui.EndMenuBar();
        }

        ImGui.End();
        ImGui.PopFont();
    }
}

public abstract class NetPacketLog
{
    public int Type;
    
    public Packet? Packet;
}

public class ReceivedNetPacketLog : NetPacketLog
{

}

public class SentNetPacketLog : NetPacketLog
{
    public int Number1;

    public float Number2;

    public float Number3;

    public float Number4;

    public int Number5;

    public int Number6;

    public int Number7;

    public string? StackTrace;
}

public class RawNetPacketLog : NetPacketLog
{
    public byte[] Data = Array.Empty<byte>();

    public bool Sent;
}