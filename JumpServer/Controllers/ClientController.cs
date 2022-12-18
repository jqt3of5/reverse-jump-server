using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JumpServer.Busi;
using JumpServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared;

namespace JumpServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly ILogger<ClientController> _logger;
        private readonly ClientModel _model;
        private readonly IHubContext<ClientHub, IClient> _hubContext;

        public ClientController(ILogger<ClientController> logger, ClientModel model, IHubContext<ClientHub, IClient> hubContext)
        {
            _logger = logger;
            _model = model;
            _hubContext = hubContext;
        }

        [HttpGet]
        public IEnumerable<ClientValueObject> Get()
        {
            return _model.Clients;
        }

        [HttpPost]
        [Route("{clientId}/tunnel")]
        public async Task<bool> AttachTunnel(string clientId, [FromBody] TunnelValueObject tunnel)
        {
            //Send a signalR message to client to establish ssh tunnel
            //If the port is already in use, decline

            if (_model.GetClient(clientId, out var client))
            {
                if (client.Tunnels.Any(t => t.ClientPort == tunnel.ClientPort) ||
                    client.Tunnels.Any(t => t.ServerPort == tunnel.ServerPort) ||
                    client.Tunnels.Any(t => t.TunnelName == tunnel.TunnelName))
                {
                    return false;
                }

                //Not established yet
                tunnel = tunnel with {Established = false};
                
                if (_model.AttachTunnel(clientId, tunnel))
                {
                    if (client.ConnectionId != null)
                    {
                        if (await _hubContext.Clients.Client(client.ConnectionId).StartTunnel(tunnel))
                        {
                            return true;
                        }
                    }
                }    
            }

            return false;
        }

        [HttpDelete]
        [Route("{clientId}/tunnel/{tunnelId}")]
        public async Task<bool> DetachTunnel(string clientId, string tunnelId)
        {
            //If the port isn't in use, fail
            //Send a signalR message to Stop the ssh tunnel
            //Remove the port from the list
            if (_model.GetClient(clientId, out var client))
            {
                if (_model.DetachTunnel(clientId, tunnelId))
                {
                    if (client.ConnectionId != null)
                    {
                        if (await _hubContext.Clients.Client(client.ConnectionId).StopTunnel(tunnelId))
                        {
                            return true;
                        }
                    }
                }    
            }

            return false;
        }
    }
}