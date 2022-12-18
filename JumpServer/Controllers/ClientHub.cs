using System;
using System.Linq;
using System.Threading.Tasks;
using JumpServer.Busi;
using JumpServer.Models;
using Microsoft.AspNetCore.SignalR;

namespace JumpClient.Controllers
{
    public interface IClient
    {
        Task<bool> StartTunnel(TunnelValueObject tunnel);
        Task<bool> StopTunnel(TunnelValueObject tunnel);
    }
    public class ClientHub : Hub<IClient>
    {
        private readonly ClientModel _model;

        public ClientHub(ClientModel model)
        {
            _model = model;
        }

        public override Task OnConnectedAsync()
        {
            _model.ClientConnected(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _model.ClientDisconnected(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task ClientHello(ClientValueObject client)
        {
                 
        }
        
        public async Task StartTunnel(string clientId, TunnelValueObject tunnel)
        {
            await Clients.Client(clientId).StartTunnel(tunnel);
        }

        public async Task StopTunnel(string clientId, TunnelValueObject tunnel)
        {
            await Clients.Client(clientId).StopTunnel(tunnel);
        }
    }
}