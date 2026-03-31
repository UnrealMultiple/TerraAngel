using System;
using TerraAngel.ID;
using TerraAngel.Utility;

namespace TerraAngel.Net;

public static class SpecialNetMessage
{
    /// <summary>
    /// Sends a PlayerControls (13) packet with custom position and selected item
    /// </summary>
    /// <param name="playerIndex">Player index</param>
    /// <param name="position">Custom position to send</param>
    /// <param name="selectedItem">Selected item index</param>
    public static void SendPlayerControlsPacket(int playerIndex, Vector2 position, int selectedItem)
    {
        if (Main.netMode == 0)
            return;

        using var builder = new PacketBuilder();
        builder.WritePlayerControlsPacket(playerIndex, position, selectedItem);
        builder.Send();
    }

    public static void SendPlayerControl(Vector2 position, int selectedItem = -1)
    {
        if (Main.netMode == 0)
            return;

        using var builder = new PacketBuilder();
        builder.WritePlayerControlsPacket(position, selectedItem);
        builder.Send();
    }

    /// <summary>
    /// Sends a PlayerControls (13) packet with hidden broadcast presence
    /// </summary>
    /// <param name="playerIndex">Player index</param>
    public static void SendPlayerControlsPacketWithHiddenPresenceMessage(int playerIndex)
    {
        if (Main.netMode == 0)
            return;

        using var builder = new PacketBuilder();
        builder.WritePlayerControlsPacketWithHiddenPresenceMessage(playerIndex);
        builder.Send();
    }

    /// <summary>
    /// Sends a SyncEquipment (5) packet with custom inventory data
    /// </summary>
    /// <param name="playerIndex">Player index</param>
    /// <param name="slot">Inventory slot</param>
    /// <param name="stack">Item stack</param>
    /// <param name="prefix">Item prefix</param>
    /// <param name="itemId">Item id</param>
    public static void SendSyncEquipmentPacket(int playerIndex, int slot, int stack, int prefix, int itemId, bool favorited = false, bool indicateBlockedSlot = false)
    {
        if (Main.netMode == 0)
            return;

        using var builder = new PacketBuilder();
        builder.WriteSyncEquipmentPacket(playerIndex, slot, stack, prefix, itemId, favorited, indicateBlockedSlot);
        builder.Send();
    }

    public static void SendInventorySlot(int slot, int itemId, int stack = 1, int prefix = 0)
    {
        using var builder = new PacketBuilder();
        builder.WriteInventorySlot(slot, stack, prefix, itemId);
        builder.Send();
    }
}
