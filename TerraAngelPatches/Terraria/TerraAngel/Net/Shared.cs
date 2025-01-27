using TrProtocol;

namespace TerraAngel.Net;

public static class Shared
{
    public static readonly PacketSerializer C2SPacketSerializer = new(false, $"Terraria{Main.curRelease}");
    
    public static readonly PacketSerializer S2CPacketSerializer = new(true, $"Terraria{Main.curRelease}");
}