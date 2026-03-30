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

        PacketBuilder.FastSendPacket(MessageID.PlayerControls, p => p
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
    }

    /// <summary>
    /// Sends a PlayerControls (13) packet with hidden broadcast presence
    /// </summary>
    /// <param name="playerIndex">Player index</param>
    public static void SendPlayerControlsPacketWithHiddenPresenceMessage(int playerIndex)
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
        // extraFlags[5] = player.netCameraTarget.HasValue;
        extraFlags[5] = true; // hide message inside netCameraTarget
        extraFlags[6] = player.lastItemUseAttemptSuccess;

        PacketBuilder.FastSendPacket(MessageID.PlayerControls, p => p
            .Write((byte)playerIndex)
            .Write(controlFlags)
            .Write(movementFlags)
            .Write(miscFlags)
            .Write(extraFlags)
            .Write((byte)player.selectedItem)
            .WriteVector2(player.position)
            .If(movementFlags[2], b => b.WriteVector2(player.velocity))
            .If(movementFlags[7], b => b.Write((ushort)player.mount.Type))
            .If(miscFlags[6], b => b
                .WriteVector2(player.PotionOfReturnOriginalUsePosition!.Value)
                .WriteVector2(player.PotionOfReturnHomePosition!.Value))
            .If(extraFlags[5], b => b.WriteVector2(new Vector2(-114514, -1919810)))); // magic number
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

        BitsByte itemFlags = (byte)0;
        itemFlags[0] = favorited;
        itemFlags[1] = indicateBlockedSlot;

        PacketBuilder.FastSendPacket(MessageID.SyncEquipment, p => p
            .Write((byte)playerIndex)
            .Write((short)slot)
            .Write((short)stack)
            .Write((byte)prefix)
            .Write((short)itemId)
            .Write(itemFlags));
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

    public static void SendTileManipulationWithItem(int x, int y, int operation, int data, int itemId, int useSlot = 0, bool resetToNormal = true)
    {
        SendPlayerControl(new Vector2(x * 16f, y * 16f), useSlot);
        SendInventorySlot(useSlot, itemId);
        NetMessage.SendData(MessageID.TileManipulation, number: operation, number2: x, number3: y, number4: data);

        if (resetToNormal)
        {
            NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer, number2: useSlot);
            NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);
        }
    }

    public static void SendTileManipulation(int x, int y, int operation, int data, bool resetToNormal = true)
    {
        SendPlayerControl(new Vector2(x * 16f, y * 16f));
        NetMessage.SendData(MessageID.TileManipulation, number: operation, number2: x, number3: y, number4: data);

        if (resetToNormal)
        {
            NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);
        }
    }

    public static void SendPlaceTile(int x, int y, int tile, int useSlot = 0, bool resetToNormal = true)
    {
        int itemId = TileUtil.GetItemFromTile(tile);
        SendTileManipulationWithItem(x, y, TileManipulationID.PlaceTile, tile, itemId, useSlot, resetToNormal);
    }

    public static void SendPlaceWall(int x, int y, int wall, int useSlot = 0, bool resetToNormal = true)
    {
        int itemId = TileUtil.GetItemFromWall(wall);
        SendTileManipulationWithItem(x, y, TileManipulationID.PlaceWall, wall, itemId, useSlot, resetToNormal);
    }

    public static void SendSlopeTile(int x, int y, int slope, int useSlot = 0, bool resetToNormal = true)
    {
        SendTileManipulationWithItem(x, y, TileManipulationID.SlopeTile, slope, ItemID.IronHammer, useSlot, resetToNormal);
    }

    public static void SendKillTile(int x, int y, int useSlot = 0, bool resetToNormal = true)
    {
        SendTileManipulationWithItem(x, y, TileManipulationID.KillTile, 0, ItemID.IronPickaxe, useSlot, resetToNormal);
    }

    public static void SendKillWall(int x, int y, int useSlot = 0, bool resetToNormal = true)
    {
        SendTileManipulationWithItem(x, y, TileManipulationID.KillWall, 0, ItemID.IronHammer, useSlot, resetToNormal);
    }

    public static void SendLiquidUpdate(int x, int y, int liquidType, int amount, int useSlot = 0, bool resetToNormal = true)
    {
        var itemId = TileUtil.GetItemFromLiquid(liquidType);
        SendPlayerControl(new Vector2(x * 16f, y * 16f), useSlot);
        SendInventorySlot(useSlot, itemId);

        PacketBuilder.FastSendPacket(MessageID.LiquidUpdate, b => b
            .Write((short)x)
            .Write((short)y)
            .Write(amount)
            .Write(liquidType));

        if (resetToNormal)
        {
            NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer, number2: useSlot);
            NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);
        }
    }

    public static void SendPaintTile(int x, int y, int tileColor, int tileCoatId, int useSlot = 0, bool resetToNormal = true)
    {
        SendPlayerControl(new Vector2(x * 16f, y * 16f), useSlot);
        SendInventorySlot(useSlot, ItemID.PaintRoller);

        NetMessage.SendData(MessageID.PaintTile, number: x, number2: y, number3: tileColor, number4: tileCoatId);

        if (resetToNormal)
        {
            NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer, number2: useSlot);
            NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);
        }
    }

    public static void SendPaintWall(int x, int y, int wallColor, int wallCoatId, int useSlot = 0, bool resetToNormal = true)
    {
        SendPlayerControl(new Vector2(x * 16f, y * 16f), useSlot);
        SendInventorySlot(useSlot, ItemID.PaintRoller);

        NetMessage.SendData(MessageID.PaintWall, number: x, number2: y, number3: wallColor, number4: wallCoatId);

        if (resetToNormal)
        {
            NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer, number2: useSlot);
            NetMessage.SendData(MessageID.PlayerControls, number: Main.myPlayer);
        }
    }

    public static void SendPlaceWire(int x, int y, int wireType, int useSlot = 0, bool resetToNormal = true)
    {
        int operation = wireType switch
        {
            1 => TileManipulationID.PlaceWire,
            2 => TileManipulationID.PlaceWire2,
            3 => TileManipulationID.PlaceWire3,
            4 => TileManipulationID.PlaceWire4,
            _ => -1
        };
        if (operation == -1)
            return;

        SendTileManipulationWithItem(x, y, operation, 0, ItemID.WireKite, useSlot, resetToNormal);
    }

    public static void SendPlaceActuator(int x, int y, int useSlot = 0, bool resetToNormal = true)
    {
        SendTileManipulationWithItem(x, y, TileManipulationID.PlaceActuator, 0, ItemID.Actuator, useSlot, resetToNormal);
    }
}
