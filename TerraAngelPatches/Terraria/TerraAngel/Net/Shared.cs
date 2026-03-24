extern alias TrProtocol;
using TrProtocol::TrProtocol;

namespace TerraAngel.Net;

public static class Shared
{
    // serialize s->c, deserialize c->s
    public static readonly PacketSerializer ServerPacketSerializer = new(false);
    
    // serialize c->s, deserialize s->c
    public static readonly PacketSerializer ClientPacketSerializer = new(true);
}