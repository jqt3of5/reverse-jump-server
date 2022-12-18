using System.Threading.Tasks;
using JumpServer.Models;

namespace Shared
{
    public interface IHub
    {
        Task ClientHello(ClientValueObject client);
        Task TunnelConnected(ClientValueObject client);
        Task TunnelDisconnected(ClientValueObject client);
    }
}