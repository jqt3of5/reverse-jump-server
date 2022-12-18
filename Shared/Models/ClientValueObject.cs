namespace JumpServer.Models
{
    public record TunnelValueObject(string TunnelId, int ServerPort, int ClientPort, bool Established, string TunnelName);

    public record ClientValueObject(string ClientId, string? ConnectionId, string ClientName, bool Connected,
        TunnelValueObject[] Tunnels)
    {
    }
}