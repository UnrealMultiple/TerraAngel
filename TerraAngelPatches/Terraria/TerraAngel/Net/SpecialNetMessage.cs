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
        controlFlags[0] = player.controlUp;
        controlFlags[1] = player.controlDown;
        controlFlags[2] = player.controlLeft;
        controlFlags[3] = player.controlRight;
        controlFlags[4] = player.controlJump;
        controlFlags[5] = player.controlUseItem;
        controlFlags[6] = player.direction == 1;

        BitsByte movementFlags = (byte)0;
        movementFlags[0] = player.pulley;
        movementFlags[1] = player.pulley && player.pulleyDir == 2;
        movementFlags[2] = player.velocity != Vector2.Zero;
        movementFlags[3] = player.vortexStealthActive;
        movementFlags[4] = player.gravDir == 1f;
        movementFlags[5] = player.shieldRaised;
        movementFlags[6] = player.ghost;
        movementFlags[7] = player.mount.Active;

        BitsByte miscFlags = (byte)0;
        miscFlags[0] = player.tryKeepingHoveringUp;
        miscFlags[1] = player.IsVoidVaultEnabled;
        miscFlags[2] = player.sitting.isSitting;
        miscFlags[3] = player.downedDD2EventAnyDifficulty;
        miscFlags[4] = player.petting.isPetting;
        miscFlags[5] = player.petting.isPetSmall;
        miscFlags[6] = player.PotionOfReturnOriginalUsePosition.HasValue;
        miscFlags[7] = player.tryKeepingHoveringDown;

        BitsByte extraFlags = (byte)0;
        extraFlags[0] = player.sleeping.isSleeping;
        extraFlags[1] = player.autoReuseAllWeapons;
        extraFlags[2] = player.controlDownHold;
        extraFlags[3] = player.isOperatingAnotherEntity;
        extraFlags[4] = player.controlUseTile;
        extraFlags[5] = player.netCameraTarget.HasValue;
        extraFlags[6] = player.lastItemUseAttemptSuccess;

        using var builder = new PacketBuilder();
        builder.MakePacket(MessageID.PlayerControls, p => p
            .Write((byte)playerIndex)
            .Write(controlFlags)
            .Write(movementFlags)
            .Write(miscFlags)
            .Write(extraFlags)
            .Write((byte)selectedItem)
            .WriteVector2(position)
            .If(movementFlags[2], b => b.WriteVector2(Vector2.Zero)) // intentionally set to zero
            .If(movementFlags[7], b => b.Write((ushort)player.mount.Type))
            .If(miscFlags[6], b => b
                .WriteVector2(player.PotionOfReturnOriginalUsePosition!.Value)
                .WriteVector2(player.PotionOfReturnHomePosition!.Value))
            .If(extraFlags[5], b => b.WriteVector2(player.netCameraTarget!.Value)));
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
    public static void SendSyncEquipmentPacket(int playerIndex, int slot, int stack, int prefix, int netId, bool favorited = false, bool indicateBlockedSlot = false)
    {
        if (Main.netMode == 0)
            return;

        BitsByte itemFlags = (byte)0;
        itemFlags[0] = favorited;
        itemFlags[1] = indicateBlockedSlot;

        using var builder = new PacketBuilder();
        builder.MakePacket(MessageID.SyncEquipment, p => p
            .Write((byte)playerIndex)
            .Write((short)slot)
            .Write((short)stack)
            .Write((byte)prefix)
            .Write((short)netId)
            .Write(itemFlags));
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
            NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer, number2: Main.LocalPlayer.selectedItem);
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
            NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer, number2: Main.LocalPlayer.selectedItem);
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
