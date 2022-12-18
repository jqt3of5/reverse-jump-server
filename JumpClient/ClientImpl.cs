using System.Threading.Tasks;
using JumpServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Shared;

namespace JumpClient
{
    public class ClientImpl : IHub
    {
        private readonly HubConnection _connection;

        public ClientImpl(HubConnection connection)
        {
            _connection = connection;
        }

        public async Task ClientHello(ClientValueObject client)
        {
            await _connection.SendAsync(nameof(ClientHello), client);
        }

        public async Task TunnelConnected(ClientValueObject client)
        {
            await _connection.SendAsync(nameof(TunnelConnected), client);
        }

        public async Task TunnelDisconnected(ClientValueObject client)
        {
            await _connection.SendAsync(nameof(TunnelDisconnected), client);
        }
    }
}