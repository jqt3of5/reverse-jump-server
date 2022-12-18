using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JumpServer.Busi;
using JumpServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JumpClient.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClientController : ControllerBase
    {
        private readonly ILogger<ClientController> _logger;
        private readonly ClientModel _model;

        public ClientController(ILogger<ClientController> logger, ClientModel model)
        {
            _logger = logger;
            _model = model;
        }

        [HttpGet]
        public IEnumerable<ClientValueObject> Get()
        {
            return _model.Clients;
        }

        [HttpPost]
        [Route("{clientId}/tunnel")]
        public bool AttachTunnel(string clientId, [FromBody] TunnelValueObject tunnel)
        {
            //Send a signalR message to client to establish ssh tunnel
            //If the port is already in use, decline

            return _model.AttachTunnel(clientId, tunnel);
        }

        [HttpDelete]
        [Route("{clientId}/tunnel/{serverPort}")]
        public bool DetachTunnel(string clientId, int serverPort)
        {
            //If the port isn't in use, fail
            //Send a signalR message to Stop the ssh tunnel
            //Remove the port from the list

            return _model.DetachTunnel(clientId, serverPort);
        }
    }
}