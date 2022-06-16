﻿using System;
using System.IO;
using Ionic.Zlib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.Social;
using Terraria;

namespace TerraAngel.Net
{
    public class SpecialNetMessage
    {
        /// <summary>
        /// Sends custom data to the server
        /// </summary>
        /// <param name="number">13: Player index</param>
        /// <param name="number2">13: Player X</param>
        /// <param name="number3">13: Player Y</param>
        /// <param name="number4">13: Selected item</param>
        /// <param name="number5"></param>
        /// <param name="number6"></param>
        /// <param name="number7"></param>
        public static void SendData(int msgType, NetworkText text = null, int number = 0, float number2 = 0f, float number3 = 0f, float number4 = 0f, int number5 = 0, int number6 = 0, int number7 = 0)
        {
            if (Main.netMode == 0)
            {
                return;
            }

            int num = 256;
            if (text == null)
            {
                text = NetworkText.Empty;
            }

            lock (NetMessage.buffer[num])
            {
                BinaryWriter writer = NetMessage.buffer[num].writer;
                if (writer == null)
                {
                    NetMessage.buffer[num].ResetWriter();
                    writer = NetMessage.buffer[num].writer;
                }

                writer.BaseStream.Position = 0L;
                long position = writer.BaseStream.Position;
                writer.BaseStream.Position += 2L;
                writer.Write((byte)msgType);

                switch (msgType)
                {
                    case 13:
                        Player player6 = Main.player[number];
                        writer.Write((byte)number);
                        BitsByte bitsByte9 = (byte)0;
                        bitsByte9[0] = false;
                        bitsByte9[1] = false;
                        bitsByte9[2] = false;
                        bitsByte9[3] = false;
                        bitsByte9[4] = false;
                        bitsByte9[5] = player6.controlUseItem;
                        bitsByte9[6] = player6.direction == 1;
                        writer.Write(bitsByte9);
                        BitsByte bitsByte10 = (byte)0;
                        bitsByte10[0] = false;
                        bitsByte10[1] = false;
                        bitsByte10[2] = player6.velocity != Vector2.Zero;
                        bitsByte10[3] = false;
                        bitsByte10[4] = player6.gravDir == 1f;
                        bitsByte10[5] = player6.shieldRaised;
                        bitsByte10[6] = false;
                        writer.Write(bitsByte10);
                        BitsByte bitsByte11 = (byte)0;
                        bitsByte11[0] = false;
                        bitsByte11[1] = false;
                        bitsByte11[2] = player6.sitting.isSitting;
                        bitsByte11[3] = player6.downedDD2EventAnyDifficulty;
                        bitsByte11[4] = player6.isPettingAnimal;
                        bitsByte11[5] = player6.isTheAnimalBeingPetSmall;
                        bitsByte11[6] = player6.PotionOfReturnOriginalUsePosition.HasValue;
                        bitsByte11[7] = false;
                        writer.Write(bitsByte11);
                        BitsByte bitsByte12 = (byte)0;
                        bitsByte12[0] = player6.sleeping.isSleeping;
                        writer.Write(bitsByte12);
                        writer.Write((byte)number4);
                        writer.WriteVector2(new Vector2(number2, number3));
                        if (bitsByte10[2])
                        {
                            writer.WriteVector2(Vector2.Zero);
                        }

                        if (bitsByte11[6])
                        {
                            writer.WriteVector2(player6.PotionOfReturnOriginalUsePosition.Value);
                            writer.WriteVector2(player6.PotionOfReturnHomePosition.Value);
                        }
                        break;
                }

                int packetLength = (int)writer.BaseStream.Position;
                writer.BaseStream.Position = position;
                writer.Write((short)packetLength);
                writer.BaseStream.Position = packetLength;
                if (Main.netMode == 1)
                {
                    if (Netplay.Connection.Socket.IsConnected())
                    {
                        try
                        {
                            NetMessage.buffer[num].spamCount++;
                            Main.ActiveNetDiagnosticsUI.CountSentMessage(msgType, packetLength);
                            Netplay.Connection.Socket.AsyncSend(NetMessage.buffer[num].writeBuffer, 0, packetLength, Netplay.Connection.ClientWriteCallBack);
                        }
                        // Really ReShit/NoLogic? (It's a play on words you see.)
                        // Saying ReShit is a derogatory term that is used to make fun of and mock the company ReLogic, by calling them "shit", which has the same meaning as human fecal matter
                        // This holds iconic meaning because it says that the company is shit and lacks experience
                        // Saying NoLogic is similar in function, mainly to mock and make fun of ReLogic, the creators of this game and writers of the code.
                        // Although it holds the same sentiment as ReShit, it has different meanings and is meant to be used in different contexts
                        // ReShit is used mainly to make fun of bugs in the game
                        // NoLogic otherwise is used to make fun of decisions made by the staff and developers there that make no sense
                        // eg., how they wrote a fairly decent ui library that is used in many places in the game, such as character creation, world creation, creative ui, and most of the settings options
                        // but yet, they still haven't upgraded the code that draws the main menu when first opening the game to this new ui style, which shows a large lack of miss-management on part of the company
                        // it also shows that there have been no attempts to fix old and poorly written code from 10 years ago, even though they definetly have the resources and developers to do this
                        catch
                        {
                        }
                    }
                }
            }
        }
    }
}