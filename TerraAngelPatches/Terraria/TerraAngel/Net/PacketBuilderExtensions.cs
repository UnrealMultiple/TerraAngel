using System;
using TerraAngel.ID;

namespace TerraAngel.Net;

public static class PacketBuilderExtensions
{
    public static PacketBuilder WritePlayerControlsPacket(this PacketBuilder pb, int playerIndex, Vector2 position, int selectedItem = -1, bool zeroVelocity = true)
    {
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
        // ReSharper disable once CompareOfFloatsByEqualityOperator
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

        pb.MakePacket(MessageID.PlayerControls, p => p
            .Write((byte)playerIndex)
            .Write(controlFlags)
            .Write(movementFlags)
            .Write(miscFlags)
            .Write(extraFlags)
            .Write((byte)(selectedItem == -1 ? player.selectedItem : selectedItem))
            .WriteVector2(position)
            .If(movementFlags[2], b => b.WriteVector2(zeroVelocity
                ? Vector2.Zero // intentionally set to zero
                : player.velocity))
            .If(movementFlags[7], b => b.Write((ushort)player.mount.Type))
            .If(miscFlags[6], b => b
                .WriteVector2(player.PotionOfReturnOriginalUsePosition!.Value)
                .WriteVector2(player.PotionOfReturnHomePosition!.Value))
            .If(extraFlags[5], b => b.WriteVector2(player.netCameraTarget!.Value)));

        return pb;
    }

    public static PacketBuilder WritePlayerControlsPacket(this PacketBuilder pb, Vector2 position, int selectedItem = -1)
    {
        int trueSelectedIndex = (selectedItem == -1) ? Main.LocalPlayer.selectedItem : selectedItem;
        pb.WritePlayerControlsPacket(Main.myPlayer, position, trueSelectedIndex);
        return pb;
    }

    public static PacketBuilder WritePlayerControlsPacketNormal(this PacketBuilder pb)
    {
        pb.WritePlayerControlsPacket(Main.myPlayer, Main.LocalPlayer.position, Main.LocalPlayer.selectedItem, false);
        return pb;
    }

    public static PacketBuilder WritePlayerControlsPacketWithHiddenPresenceMessage(this PacketBuilder pb, int playerIndex)
    {
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
        // ReSharper disable once CompareOfFloatsByEqualityOperator
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

        pb.MakePacket(MessageID.PlayerControls, p => p
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
        return pb;
    }

    public static PacketBuilder WriteSyncEquipmentPacket(this PacketBuilder pb, int playerIndex, int slot, int stack, int prefix, int itemId, bool favorited = false, bool indicateBlockedSlot = false)
    {
        BitsByte itemFlags = (byte)0;
        itemFlags[0] = favorited;
        itemFlags[1] = indicateBlockedSlot;

        pb.MakePacket(MessageID.SyncEquipment, p => p
            .Write((byte)playerIndex)
            .Write((short)slot)
            .Write((short)stack)
            .Write((byte)prefix)
            .Write((short)itemId)
            .Write(itemFlags));
        return pb;
    }

    public static PacketBuilder WriteSyncEquipmentPacketNormal(this PacketBuilder pb, int slot, bool indicateBlockedSlot = false)
    {
        Item item = new PlayerItemSlotID.SlotReference(Main.player[Main.myPlayer], slot).Item;
        if (item.Name == "" || item.stack == 0 || item.type == 0)
        {
            item.SetDefaults(0);
        }

        var stack = Math.Max(item.stack, 0);
        var prefix = item.prefix;
        var itemId = item.type;
        var favorited = item.favorited;

        pb.WriteSyncEquipmentPacket(Main.myPlayer, slot, stack, prefix, itemId, favorited, indicateBlockedSlot);
        return pb;
    }

    public static PacketBuilder WriteInventorySlot(this PacketBuilder pb, int slot, int itemId, int stack = 1, int prefix = 0)
    {
        pb.WriteSyncEquipmentPacket(Main.myPlayer, slot, stack, prefix, itemId);
        return pb;
    }

    public static PacketBuilder WriteTileManipulationPacket(this PacketBuilder pb, int operation, int x, int y, int data = 0, int data2 = 0)
    {
        pb.MakePacket(MessageID.TileManipulation, b => b
            .Write((byte)operation)
            .Write((short)x)
            .Write((short)y)
            .Write((short)data)
            .Write((byte)data2));
        return pb;
    }

    public static PacketBuilder WriteLiquidUpdatePacket(this PacketBuilder pb, int x, int y, int liquidType, int amount)
    {
        pb.MakePacket(MessageID.LiquidUpdate, b => b
            .Write((short)x)
            .Write((short)y)
            .Write(amount)
            .Write(liquidType));
        return pb;
    }

    public static PacketBuilder WritePaintTilePacket(this PacketBuilder pb, int x, int y, int tileColor, int tileCoatId)
    {
        pb.MakePacket(MessageID.PaintTile, b => b
            .Write((short)x)
            .Write((short)y)
            .Write((byte)tileColor)
            .Write((byte)tileCoatId));
        return pb;
    }

    public static PacketBuilder WritePaintWallPacket(this PacketBuilder pb, int x, int y, int wallColor, int wallCoatId)
    {
        pb.MakePacket(MessageID.PaintWall, b => b
            .Write((short)x)
            .Write((short)y)
            .Write((byte)wallColor)
            .Write((byte)wallCoatId));
        return pb;
    }

    public static PacketBuilder WritePositionedOperationWithItem(this PacketBuilder pb, int tileX, int tileY, int itemId, Action<PacketBuilder> operation, int useSlot = 0, bool resetToNormal = true)
    {
        var position = new Vector2(tileX, tileY) * 16f;
        pb.WritePlayerControlsPacket(position, useSlot);
        pb.WriteInventorySlot(useSlot, itemId);

        operation(pb);

        if (resetToNormal)
        {
            pb.WriteSyncEquipmentPacketNormal(useSlot);
            pb.WritePlayerControlsPacketNormal();
        }

        return pb;
    }

    public static PacketBuilder WritePositionedOperation(this PacketBuilder pb, int tileX, int tileY, Action<PacketBuilder> operation, bool resetToNormal = true)
    {
        var position = new Vector2(tileX, tileY) * 16f;
        pb.WritePlayerControlsPacket(position);

        operation(pb);

        if (resetToNormal)
        {
            pb.WritePlayerControlsPacketNormal();
        }

        return pb;
    }

    #region World Editing

    public static PacketBuilder WritePlayerPlaceTile(this PacketBuilder pb, int x, int y, int tile, int useSlot = 0, bool resetToNormal = true)
    {
        int itemId = TileUtil.GetItemFromTile(tile);
        // if (itemId == ItemID.None)
        //     return pb;

        pb.WritePositionedOperationWithItem(
            x, y,
            itemId,
            b => b.WriteTileManipulationPacket(TileManipulationID.PlaceTile, x, y, tile),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerKillTile(this PacketBuilder pb, int x, int y, int useSlot = 0, bool resetToNormal = true)
    {
        pb.WritePositionedOperationWithItem(
            x, y,
            ItemID.IronPickaxe,
            b => b.WriteTileManipulationPacket(TileManipulationID.KillTile, x, y),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerPaintTile(this PacketBuilder pb, int x, int y, int tileColor, int tileCoatId, int useSlot = 0, bool resetToNormal = true)
    {
        pb.WritePositionedOperationWithItem(
            x, y,
            ItemID.PaintRoller,
            b => b.WritePaintTilePacket(x, y, tileColor, tileCoatId),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerSlopeTile(this PacketBuilder pb, int x, int y, int slope, int useSlot = 0, bool resetToNormal = true)
    {
        pb.WritePositionedOperationWithItem(
            x, y,
            ItemID.IronHammer,
            b => b.WriteTileManipulationPacket(TileManipulationID.SlopeTile, x, y, slope),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerPlaceWall(this PacketBuilder pb, int x, int y, int wall, int useSlot = 0, bool resetToNormal = true)
    {
        int itemId = TileUtil.GetItemFromWall(wall);
        // if (itemId == ItemID.None)
        //     return pb;

        pb.WritePositionedOperationWithItem(
            x, y,
            itemId,
            b => b.WriteTileManipulationPacket(TileManipulationID.PlaceWall, x, y, wall),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerKillWall(this PacketBuilder pb, int x, int y, int useSlot = 0, bool resetToNormal = true)
    {
        pb.WritePositionedOperationWithItem(
            x, y,
            ItemID.IronHammer,
            b => b.WriteTileManipulationPacket(TileManipulationID.KillWall, x, y),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerPaintWall(this PacketBuilder pb, int x, int y, int wallColor, int wallCoatId, int useSlot = 0, bool resetToNormal = true)
    {
        pb.WritePositionedOperationWithItem(
            x, y,
            ItemID.PaintRoller,
            b => b.WritePaintWallPacket(x, y, wallColor, wallCoatId),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerUpdateLiquid(this PacketBuilder pb, int x, int y, int liquidType, int amount, int useSlot = 0, bool resetToNormal = true)
    {
        int itemId = TileUtil.GetItemFromLiquid(liquidType);
        pb.WritePositionedOperationWithItem(
            x, y,
            itemId,
            b => b.WriteLiquidUpdatePacket(x, y, liquidType, amount),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerPlaceWire(this PacketBuilder pb, int x, int y, int wireType, int useSlot = 0, bool resetToNormal = true)
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
            return pb;

        pb.WritePositionedOperationWithItem(
            x, y,
            ItemID.WireKite,
            b => b.WriteTileManipulationPacket(operation, x, y),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerKillWire(this PacketBuilder pb, int x, int y, int wireType, int useSlot = 0, bool resetToNormal = true)
    {
        int operation = wireType switch
        {
            1 => TileManipulationID.KillWire,
            2 => TileManipulationID.KillWire2,
            3 => TileManipulationID.KillWire3,
            4 => TileManipulationID.KillWire4,
            _ => -1
        };
        if (operation == -1)
            return pb;

        pb.WritePositionedOperationWithItem(
            x, y,
            ItemID.WireKite,
            b => b.WriteTileManipulationPacket(operation, x, y),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerPlaceActuator(this PacketBuilder pb, int x, int y, int useSlot = 0, bool resetToNormal = true)
    {
        pb.WritePositionedOperationWithItem(
            x, y,
            ItemID.Actuator,
            b => b.WriteTileManipulationPacket(TileManipulationID.PlaceActuator, x, y),
            useSlot, resetToNormal);
        return pb;
    }

    public static PacketBuilder WritePlayerKillActuator(this PacketBuilder pb, int x, int y, int useSlot = 0, bool resetToNormal = true)
    {
        pb.WritePositionedOperationWithItem(
            x, y,
            ItemID.WireKite,
            b => b.WriteTileManipulationPacket(TileManipulationID.KillActuator, x, y),
            useSlot, resetToNormal);
        return pb;
    }

    #endregion
}