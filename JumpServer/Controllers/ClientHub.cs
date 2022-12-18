using System;
using System.Threading.Tasks;
using JumpServer.Busi;
using JumpServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared;

namespace JumpServer.Controllers
{
   
    public class ClientHub : Hub<IClient>, IHub
    {
        private readonly ClientModel _model;
        private readonly ILogger<ClientHub> _logger;

        public ClientHub(ClientModel model, ILogger<ClientHub> logger)
        {
            _model = model;
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            _logger.Log(LogLevel.Information, $"Client connected with ID: {Context.ConnectionId}");
            _model.ClientConnected(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.Log(LogLevel.Information, $"Client disconnected with ID: {Context.ConnectionId}");
            _model.ClientDisconnected(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task ClientHello(ClientValueObject client)
        {
            _logger.Log(LogLevel.Information, $"ClientHello with ID: {client.ClientName} ({client.ClientId})");
            _model.UpdateClient(client with {ConnectionId = Context.ConnectionId});
        }

        public async Task TunnelConnected(ClientValueObject client)
        {
            foreach (var tunnel in client.Tunnels)
            {
                _logger.Log(LogLevel.Information, $"Tunnel Connected with ID: {client.ClientName} ({tunnel.TunnelId})");
                _model.TunnelConnected(client.ClientId, tunnel.TunnelId);
            }
        }

        public async Task TunnelDisconnected(ClientValueObject client)
        {
            foreach (var tunnel in client.Tunnels)
            {
                _logger.Log(LogLevel.Information, $"Tunnel Disconnected with ID: {client.ClientName} ({tunnel.TunnelId})");
                _model.TunnelDisconnected(client.ClientId, tunnel.TunnelId);
            } 
        }
    }
}