using System.Collections.Generic;

namespace Shared.Models;

public class ConfiguredTunnels
{
    public List<ConfiguredTunnel> Tunnels { get; set;  }
}

public class ConfiguredTunnel
{
    public string JumpServerAddress { get; set; }
    public uint JumpServerPort { get; set; }
    public string JumpServerUser{ get; set; }
    public uint RemotePort { get; set; }
    public uint LocalPort { get; set;  } 
    public string Host { get; set; }
    public string KeyFile { get; set; }
    public string? TunnelName { get; set; }
}

