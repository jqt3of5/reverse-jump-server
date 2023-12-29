namespace Shared.Models;

public record JumpServerValueObject (string jumpServerId, string ipAddress, int sshPort, string keyFileName);