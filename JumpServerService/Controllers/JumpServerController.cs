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
        private readonly JumpServerRepo _repo;

        public JumpServerController(ILogger<TunnelController> logger, JumpServerRepo repo)
        {
            _logger = logger;
            _repo = repo;
        }

        [HttpGet]
        public IEnumerable<JumpServerValueObject> Get()
        {
            return _repo.GetAllJumpServers();
        }
    }
}