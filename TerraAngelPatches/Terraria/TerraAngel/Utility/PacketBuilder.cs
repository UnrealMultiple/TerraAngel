using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace TerraAngel.Utility;

public class PacketBuilder : IDisposable, IAsyncDisposable
{
    public static PacketBuilder NewBuilder() => new();

    public PacketBuilder(bool doSanityCheck = true)
    {
        Buffer = ArrayPool<byte>.Shared.Rent(MessageBuffer.writeBufferMax);
        Ms = new MemoryStream(Buffer);
        Bw = new BinaryWriter(Ms);
        Ms.SetLength(0);
        Ms.Position = sizeof(ushort);
        
        _packetEndSanityCheck = !doSanityCheck;
    }

    private bool _packetEndSanityCheck;

    public byte[]? Buffer;
    public readonly MemoryStream Ms;
    public readonly BinaryWriter Bw;
    public long PacketHead;
    
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
    
    public PacketBuilder Do(Action<PacketBuilder> action)
    {
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

    public PacketBuilder EndPacket()
    {
        var length = Ms.Position - PacketHead;

        var origPos = Ms.Position;
        Ms.Position = PacketHead;
        Bw.Write((ushort)length);
        Ms.Position = origPos;

        PacketHead = Ms.Position;
        Ms.Position += sizeof(ushort);
        
        Ms.Flush();

        _packetEndSanityCheck = true;
        return this;
    }

    public byte[] Build(bool isDispose = true)
    {
        if (!_packetEndSanityCheck)
            ClientLoader.Console.WriteError($"[{nameof(PacketBuilder)}] {nameof(PacketBuilder)}.{nameof(Build)}() Warning: EndPacket had never been called, probably a bug");
        
        var ret = Ms.ToArray();
        if (isDispose)
            Dispose();
        return ret;
    }

    public void Send(bool isDispose = true)
    {
        if (Main.netMode != 1 || !Netplay.Connection.Socket.IsConnected())
            return;
        if (!_packetEndSanityCheck)
            ClientLoader.Console.WriteError($"[{nameof(PacketBuilder)}] {nameof(PacketBuilder)}.{nameof(Send)}() Warning: EndPacket had never been called, probably a bug");
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
        if (isDispose)
            Dispose();
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
}