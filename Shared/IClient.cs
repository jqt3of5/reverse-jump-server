using System.Threading.Tasks;
using JumpServer.Models;

namespace Shared
{
    public interface IClient
    {
        Task<bool> StartTunnel(TunnelValueObject tunnel);
        Task<bool> StopTunnel(string tunnelId);
    }
}