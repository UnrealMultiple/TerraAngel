using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace TerraAngel.Utility;

public class PacketBuilder : IDisposable, IAsyncDisposable
{
    public PacketBuilder()
    {
        Buffer = ArrayPool<byte>.Shared.Rent(MessageBuffer.writeBufferMax);
        Ms = new MemoryStream(Buffer);
        Bw = new BinaryWriter(Ms);
        Ms.SetLength(0);
        Ms.Position = 0;
    }

    public byte[]? Buffer;
    public readonly MemoryStream Ms;
    public readonly BinaryWriter Bw;
    public long PacketHead = -1;
    
    // searching for BitsByte? Use BitsByte constructor instead

    public PacketBuilder If(bool condition, Action<PacketBuilder> action)
    {
        if (condition)
            action(this);
        return this;
    }
    
    public PacketBuilder If(Func<PacketBuilder, bool> condition, Action<PacketBuilder> action)
    {
        if (condition(this))
            action(this);
        return this;
    }
    
    public PacketBuilder WriteRGB(Color c)
    {
        Bw.Write(c.R);
        Bw.Write(c.G);
        Bw.Write(c.B);
        return this;
    }

    public PacketBuilder WriteVector2(Vector2 v)
    {
        Bw.Write(v.X);
        Bw.Write(v.Y);
        return this;
    }
    
    public PacketBuilder WritePackedVector2(Vector2 v)
    {
        Bw.Write(new HalfVector2(v.X, v.Y).PackedValue);
        return this;
    }

    public PacketBuilder Write(bool value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(byte value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(sbyte value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(byte[] buffer)
    {
        Bw.Write(buffer);
        return this;
    }

    public PacketBuilder Write(byte[] buffer, int index, int count)
    {
        Bw.Write(buffer, index, count);
        return this;
    }

    public PacketBuilder Write(char ch)
    {
        Bw.Write(ch);
        return this;
    }

    public PacketBuilder Write(char[] chars)
    {
        Bw.Write(chars);
        return this;
    }

    public PacketBuilder Write(char[] chars, int index, int count)
    {
        Bw.Write(chars, index, count);
        return this;
    }

    public PacketBuilder Write(double value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(decimal value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(short value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(ushort value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(int value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(uint value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(long value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(ulong value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(float value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(Half value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(string value)
    {
        Bw.Write(value);
        return this;
    }

    public PacketBuilder Write(ReadOnlySpan<byte> buffer)
    {
        Bw.Write(buffer);
        return this;
    }

    public PacketBuilder Write(ReadOnlySpan<char> chars)
    {
        Bw.Write(chars);
        return this;
    }

    public PacketBuilder Write7BitEncodedInt(int value)
    {
        Bw.Write7BitEncodedInt(value);
        return this;
    }

    public PacketBuilder Write7BitEncodedInt64(long value)
    {
        Bw.Write7BitEncodedInt64(value);
        return this;
    }

    public PacketBuilder MakePacket(byte msgType, Action<PacketBuilder> contentBuilder)
    {
        StartPacket(msgType);
        contentBuilder(this);
        EndPacket();
        return this;
    }

    public PacketBuilder StartPacket(byte msgType)
    {
        if (PacketHead >= 0)
            throw new InvalidOperationException("A packet is already being built. Call EndPacket() before starting a new one.");

        PacketHead = Ms.Position;
        Ms.Position += sizeof(ushort);
        Bw.Write(msgType);
        return this;
    }

    public PacketBuilder EndPacket()
    {
        if (PacketHead < 0)
            throw new InvalidOperationException("No packet is being built. Call StartPacket() before calling EndPacket().");

        var length = Ms.Position - PacketHead;

        var origPos = Ms.Position;
        Ms.Position = PacketHead;
        Bw.Write((ushort)length);
        Ms.Position = origPos;

        PacketHead = -1;

        Ms.Flush();
        return this;
    }

    public byte[] Build()
    {
        return Ms.ToArray();
    }

    public void Send()
    {
        if (Main.netMode != 1 || !Netplay.Connection.Socket.IsConnected())
            return;
        try
        {
            NetMessage.buffer[256].spamCount++;
            // Main.ActiveNetDiagnosticsUI.CountSentMessage(msgType, packetLength);
            Netplay.Connection.Socket.AsyncSend(Buffer!, 0, (int)Ms.Length, Netplay.Connection.ClientWriteCallBack);
        }
        catch (Exception ex)
        {
            ClientLoader.Console.WriteError($"[{nameof(PacketBuilder)}] {nameof(PacketBuilder)}.{nameof(Send)}() Error: {ex.Message}");
        }
    }

    public PacketBuilder Clear()
    {
        Ms.SetLength(0);
        Ms.Position = sizeof(ushort);
        PacketHead = 0;
        return this;
    }

    public void Dispose()
    {
        if (Buffer is not null)
            ArrayPool<byte>.Shared.Return(Buffer);
        Buffer = null;
        Ms.Dispose();
        Bw.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Buffer is not null)
            ArrayPool<byte>.Shared.Return(Buffer);
        Buffer = null;
        await Ms.DisposeAsync();
        await Bw.DisposeAsync();
    }

    public static void FastSendPacket(byte msgType, Action<PacketBuilder> contentBuilder)
    {
        using var builder = new PacketBuilder();
        builder.StartPacket(msgType);
        contentBuilder(builder);
        builder.EndPacket();
        builder.Send();
    }

    public static byte[] FastBuildPacket(byte msgType, Action<PacketBuilder> contentBuilder)
    {
        using var builder = new PacketBuilder();
        builder.StartPacket(msgType);
        contentBuilder(builder);
        builder.EndPacket();
        return builder.Build();
    }
}