namespace Shared.Models;

public record JumpServerValueObject (string JumpServerId, string IpAddress, uint SshPort, string KeyFileName, string UserName, string HostKey);