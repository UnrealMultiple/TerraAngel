using System;
using TerraAngel.ID;
using TerraAngel.Utility;

namespace TerraAngel.Net;

public class SpecialNetMessage
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

        Player player = Main.player[playerIndex];

        BitsByte controlFlags = (byte)0;
        controlFlags[5] = player.controlUseItem;
        controlFlags[6] = player.direction == 1;

        BitsByte movementFlags = (byte)0;
        movementFlags[2] = player.velocity != Vector2.Zero;
        movementFlags[4] = player.gravDir == 1f;
        movementFlags[5] = player.shieldRaised;

        BitsByte miscFlags = (byte)0;
        miscFlags[2] = player.sitting.isSitting;
        miscFlags[3] = player.downedDD2EventAnyDifficulty;
        miscFlags[4] = player.petting.isPetting;
        miscFlags[5] = player.petting.isPetSmall;
        miscFlags[6] = player.PotionOfReturnOriginalUsePosition.HasValue;

        BitsByte extraFlags = (byte)0;
        extraFlags[0] = player.sleeping.isSleeping;

        using var builder = new PacketBuilder();
        builder.MakePacket(MessageID.PlayerControls, p => p
            .Write((byte)playerIndex)
            .Write(controlFlags)
            .Write(movementFlags)
            .Write(miscFlags)
            .Write(extraFlags)
            .Write((byte)selectedItem)
            .WriteVector2(position)
            .If(movementFlags[2], b => b.WriteVector2(Vector2.Zero))
            .If(miscFlags[6], b => b
                .WriteVector2(player.PotionOfReturnOriginalUsePosition!.Value)
                .WriteVector2(player.PotionOfReturnHomePosition!.Value)));
        builder.Send();
    }

    /// <summary>
    /// Sends a SyncEquipment (5) packet with custom inventory data
    /// </summary>
    /// <param name="playerIndex">Player index</param>
    /// <param name="slot">Inventory slot</param>
    /// <param name="stack">Item stack</param>
    /// <param name="prefix">Item prefix</param>
    /// <param name="netId">Item net ID</param>
    public static void SendSyncEquipmentPacket(int playerIndex, int slot, int stack, int prefix, int netId)
    {
        if (Main.netMode == 0)
            return;

        using var builder = new PacketBuilder();
        builder.MakePacket(MessageID.SyncEquipment, p => p
            .Write((byte)playerIndex)
            .Write((short)slot)
            .Write((short)stack)
            .Write((byte)prefix)
            .Write((short)netId));
        builder.Send();
    }

    public static void SendPlayerControl(Vector2 position, int selectedIndex = -1)
    {
        int trueSelectedIndex = (selectedIndex == -1) ? Main.LocalPlayer.selectedItem : selectedIndex;
        SendPlayerControlsPacket(Main.myPlayer, position, trueSelectedIndex);
    }

    public static void SendInventorySlot(int slot, int itemId, int stack = 1, int prefix = 0)
    {
        SendSyncEquipmentPacket(Main.myPlayer, slot, stack, prefix, itemId);
    }

    public static void SendPlaceTile(int x, int y, int tile, int useSlot = 0, bool resetToNormal = true)
    {
        int itemId = TileUtil.TileToItem[tile];
        if (itemId == -1)
            itemId = 0;
        SendPlayerControl(new Vector2(x * 16f, y * 16f), 0);
        SendInventorySlot(useSlot, itemId);
        NetMessage.SendData(MessageID.TileManipulation, number: TileManipulationID.PlaceTile, number2: x, number3: y, number4: tile);

        if (resetToNormal)
        {
            NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);
            NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer, number2: Main.LocalPlayer.selectedItem, number3: Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem].prefix);
        }
    }

    public static void SendPlaceWall(int x, int y, int wall, int useSlot = 0, bool resetToNormal = true)
    {
        int itemId = TileUtil.WallToItem[wall];
        if (itemId == -1)
            itemId = 0;
        SendPlayerControl(new Vector2(x * 16f, y * 16f), 0);
        SendInventorySlot(useSlot, itemId);
        NetMessage.SendData(MessageID.TileManipulation, number: TileManipulationID.PlaceWall, number2: x, number3: y, number4: wall);

        if (resetToNormal)
        {
            NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);
            NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer, number2: Main.LocalPlayer.selectedItem, number3: Main.LocalPlayer.inventory[Main.LocalPlayer.selectedItem].prefix);
        }
    }

    public static void SendUpdateSignExploit(int signId, int numOfPackets)
    {
        if (Main.netMode == 0)
            return;

        using var builder = new PacketBuilder();
        for (int i = 0; i < numOfPackets; i++)
        {
            builder.MakePacket(MessageID.UpdateSign, p => p
                .Write((short)signId)
                .Write((short)Main.sign[signId].x)
                .Write((short)Main.sign[signId].y)
                .Write7BitEncodedInt(60000)
                .Write((byte)Random.Shared.Next(0, 256)));
        }

        builder.Send();
    }
}
