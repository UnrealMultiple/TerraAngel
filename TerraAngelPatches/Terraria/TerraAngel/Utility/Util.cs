using System;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.Net.Sockets;
using static Terraria.WorldBuilding.Modifiers;

namespace TerraAngel.Utility;

public class Util
{
    private static readonly string[] ByteSizeNames = { "b", "k", "m", "g", "t", "p" };

    public static Vector2 ScreenSize => ClientLoader.WindowManager?.Size ?? Vector2.One;

    public static bool IsRectOnScreen(Vector2 min, Vector2 max, Vector2 displaySize)
    {
        return (min.X > 0 || max.X > 0) && (min.X < displaySize.X || max.X < displaySize.X) && (min.Y > 0 || max.Y > 0) && (min.Y < displaySize.Y || max.X < displaySize.Y);
    }

    public static bool IsMouseHoveringRect(Vector2 min, Vector2 max)
    {
        Vector2 mousePos = InputSystem.MousePosition;
        return mousePos.X >= min.X &&
                mousePos.Y >= min.Y &&
                mousePos.X <= max.X &&
                mousePos.Y <= max.Y;
    }

    public static string PrettyPrintBytes(long bytes, string format = "{0:F2}{1}")
    {
        float len = bytes;
        int order = 0;
        while ((len >= 1024 || len >= 100f) && order < ByteSizeNames.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return string.Format(format, len, ByteSizeNames[order]);
    }


    public static Vector2 WorldToScreenDynamic(Vector2 worldPoint)
    {
        if (Main.mapFullscreen) return WorldToScreenFullscreenMap(worldPoint);
        else return WorldToScreenWorld(worldPoint);
    }

    public static Vector2 WorldToScreenDynamicPixelPerfect(Vector2 worldPoint)
    {
        if (Main.mapFullscreen) return WorldToScreenFullscreenMap(worldPoint);
        else return WorldToScreenWorldPixelPerfect(worldPoint);
    }

    public static Vector2 ScreenToWorldDynamic(Vector2 screenPoint)
    {
        if (Main.mapFullscreen) return ScreenToWorldFullscreenMap(screenPoint);
        else return ScreenToWorldWorld(screenPoint);
    }

    public static Vector2 ScreenToWorldFullscreenMap(Vector2 screenPoint)
    {
        screenPoint -= ScreenSize / 2f;
        screenPoint /= Main.mapFullscreenScale;
        screenPoint *= 16f;
        screenPoint = Main.mapFullscreenPos * 16f + screenPoint;
        return screenPoint;
    }


    public static Vector2 WorldToScreenFullscreenMap(Vector2 worldPoint)
    {
        worldPoint *= Main.mapFullscreenScale;
        worldPoint /= 16f;
        worldPoint -= Main.mapFullscreenPos * Main.mapFullscreenScale;
        worldPoint += ScreenSize / 2f;
        return worldPoint;
    }

    public static Vector2 WorldToScreenWorld(Vector2 worldPosition)
    {
        return Vector2.Transform(worldPosition - Main.screenPosition, Main.GameViewMatrix.ZoomMatrix);
    }
    public static Vector2 WorldToScreenWorldPixelPerfect(Vector2 worldPosition)
    {
        return Vector2.Transform((worldPosition - Main.screenPosition).Floor(), Main.GameViewMatrix.ZoomMatrix).Floor();
    }
    public static Vector2 ScreenToWorldWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, Main.GameViewMatrix.InverseZoomMatrix) + Main.screenPosition;
    }

    public static object? GetDefault(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    public static float Lerp(float x0, float x1, float t)
    {
        return MathHelper.Lerp(x0, x1, t);
    }

    public static void Bresenham(int x, int y, int x2, int y2, Action<int, int> predicate)
    {
        int w = x2 - x;
        int h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
        int longest = Math.Abs(w);
        int shortest = Math.Abs(h);
        if (!(longest > shortest))
        {
            longest = Math.Abs(h);
            shortest = Math.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        for (int i = 0; i <= longest; i++)
        {
            predicate(x, y);
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
    }

    public static CultureInfo CurrentCulture
    {
        get
        {
            var cultureInfo = Language.ActiveCulture?.CultureInfo;
            if (cultureInfo is null)
                return new CultureInfo("en-US");
            return cultureInfo.Name == "zh-Hans" ? new CultureInfo("zh-CN") : cultureInfo;
        }
    }

    //Taken From ZaZaClient
    public static void FalsePlayerPacket(Vector2 playerPosition)
    {
        // Don't process in single player mode
        if (Main.netMode == 0)
            return;

        int bufferIndex = 256;
        int packetType = 13;
        int playerIndex = Main.LocalPlayer.whoAmI;
        Player player = Main.player[playerIndex];

        MessageBuffer messageBuffer = NetMessage.buffer[bufferIndex];
        lock (messageBuffer)
        {
            // Setup writer
            BinaryWriter writer = messageBuffer.writer;
            if (writer == null)
            {
                messageBuffer.ResetWriter();
                writer = messageBuffer.writer;
            }

            writer.BaseStream.Position = 0L;
            long packetLengthPosition = writer.BaseStream.Position;
            writer.BaseStream.Position += 2L; // Skip length for now, will write later

            // Write packet type and player index
            writer.Write((byte)packetType);
            writer.Write((byte)playerIndex);

            // Player controls bitfield 1
            BitsByte controls1 = 0;
            controls1[0] = player.controlUp;
            controls1[1] = player.controlDown;
            controls1[2] = player.controlLeft;
            controls1[3] = player.controlRight;
            controls1[4] = player.controlJump;
            controls1[5] = player.controlUseItem;
            controls1[6] = player.direction == 1;
            writer.Write(controls1);

            // Player state bitfield 2
            BitsByte controls2 = 0;
            controls2[0] = player.pulley;
            controls2[1] = player.pulley && player.pulleyDir == 2;
            controls2[2] = player.velocity != Vector2.Zero;
            controls2[3] = player.vortexStealthActive;
            controls2[4] = player.gravDir == 1f;
            controls2[5] = player.shieldRaised;
            controls2[6] = player.ghost;
            writer.Write(controls2);

            // Player state bitfield 3
            BitsByte controls3 = 0;
            controls3[0] = player.tryKeepingHoveringUp;
            controls3[1] = player.IsVoidVaultEnabled;
            controls3[2] = player.sitting.isSitting;
            controls3[3] = player.downedDD2EventAnyDifficulty;
            controls3[4] = player.isPettingAnimal;
            controls3[5] = player.isTheAnimalBeingPetSmall;
            controls3[6] = player.PotionOfReturnOriginalUsePosition != null;
            controls3[7] = player.tryKeepingHoveringDown;
            writer.Write(controls3);

            // Player state bitfield 4
            BitsByte controls4 = 0;
            controls4[0] = player.sleeping.isSleeping;
            controls4[1] = player.autoReuseAllWeapons;
            controls4[2] = player.controlDownHold;
            controls4[3] = player.isOperatingAnotherEntity;
            writer.Write(controls4);

            // Write selected item and position
            writer.Write((byte)player.selectedItem);
            writer.WriteVector2(playerPosition);

            // Write conditional data
            if (controls2[2]) // Has velocity
            {
                writer.WriteVector2(player.velocity);
            }

            if (controls3[6]) // Has potion of return data
            {
                writer.WriteVector2(player.PotionOfReturnOriginalUsePosition.Value);
                writer.WriteVector2(player.PotionOfReturnHomePosition.Value);
            }

            // Check packet size
            int packetLength = (int)writer.BaseStream.Position;
            if (packetLength > 65535)
            {
                throw new Exception($"Maximum packet length exceeded. id: {packetType} length: {packetLength}");
            }

            // Go back and write packet length
            writer.BaseStream.Position = packetLengthPosition;
            writer.Write((ushort)packetLength);
            writer.BaseStream.Position = packetLength;

            // Send the packet - Client mode
            if (Main.netMode == 1)
            {
                if (Netplay.Connection.Socket.IsConnected())
                {
                    try
                    {
                        messageBuffer.spamCount++;
                        Main.ActiveNetDiagnosticsUI.CountSentMessage(packetType, packetLength);
                        Netplay.Connection.Socket.AsyncSend(
                            messageBuffer.writeBuffer,
                            0,
                            packetLength,
                            new SocketSendCallback(Netplay.Connection.ClientWriteCallBack),
                            null
                        );
                    }
                    catch
                    {
                        // Silently handle connection issues
                    }
                }
            }
            // Server mode
            else if (packetType == 13)
            {
                // Send to all connected clients
                for (int i = 0; i < 256; i++)
                {
                    if (NetMessage.buffer[i].broadcast && Netplay.Clients[i].IsConnected())
                    {
                        try
                        {
                            NetMessage.buffer[i].spamCount++;
                            Main.ActiveNetDiagnosticsUI.CountSentMessage(packetType, packetLength);
                            Netplay.Clients[i].Socket.AsyncSend(
                                NetMessage.buffer[bufferIndex].writeBuffer,
                                0,
                                packetLength,
                                new SocketSendCallback(Netplay.Clients[i].ServerWriteCallBack),
                                null
                            );
                        }
                        catch
                        {
                            // Silently handle connection issues
                        }
                    }
                }

                // Update player network skip counter
                Main.player[playerIndex].netSkip++;
                if (Main.player[playerIndex].netSkip > 2)
                {
                    Main.player[playerIndex].netSkip = 0;
                }
            }

            if (Main.verboseNetplay)
            {
                for (int j = 0; j < packetLength; j++)
                {
                }
                for (int k = 0; k < packetLength; k++)
                {
                    byte b = messageBuffer.writeBuffer[k];
                }
            }

            // Cleanup
            messageBuffer.writeLocked = false;

            // Handle termination if needed
            if (packetType == 2 && Main.netMode == 2)
            {
                Netplay.Clients[bufferIndex].PendingTermination = true;
                Netplay.Clients[bufferIndex].PendingTerminationApproved = true;
            }
        }
    }
}