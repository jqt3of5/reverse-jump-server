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
    public class TunnelController : ControllerBase
    {
        private readonly ILogger<TunnelController> _logger;

        public TunnelController(ILogger<TunnelController> logger) 
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<ClientValueObject> Get()
        {
            
        }

        [HttpPost]
        public async Task<bool> AttachTunnel([FromBody] TunnelValueObject tunnel)
        {
           
        }

        [HttpDelete]
        [Route("{tunnelId}")]
        public async Task<bool> DetachTunnel(string tunnelId)
        {
           
        }
    }
}