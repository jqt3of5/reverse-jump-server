using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JumpServer.Busi;
using JumpServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.Models;

namespace JumpServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JumpServerController : ControllerBase
    {
        private readonly ILogger<TunnelController> _logger;

        public JumpServerController(ILogger<TunnelController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<JumpServerValueObject> Get()
        {
        }
    }
}