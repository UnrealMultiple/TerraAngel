extern alias TrProtocol;
using TrProtocol::TrProtocol;

namespace TerraAngel.Net;

extern alias TrProtocol;

public static class Shared
{
    // serialize s->c, deserialize c->s
    public static readonly PacketSerializer ServerPacketSerializer = new(false, (string)$"Terraria{Main.curRelease}");
    
    // serialize c->s, deserialize s->c
    public static readonly PacketSerializer ClientPacketSerializer = new(true, (string)$"Terraria{Main.curRelease}");
}