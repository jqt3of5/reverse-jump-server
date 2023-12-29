
using System.Diagnostics;
using System.Text.Json.Serialization;
using Renci.SshNet;

namespace JumpServer.Models
{
    public record TunnelValueObject(string? TunnelId, string JumpServerIp, uint JumpServerPort, uint RemotePort,
        uint LocalPort, string Host, bool Established, string TunnelName)
    {
    }
}