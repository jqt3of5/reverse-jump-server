namespace JumpServer.Models
{
    public record TunnelValueObject(int ServerPort, int ClientPort, string TunnelName);
    public record ClientValueObject(string ClientId, string ConnectionId, string ClientName, bool connected, TunnelValueObject [] Tunnels);
}