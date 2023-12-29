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
        private readonly TunnelRepo _repo;
        private readonly ILogger<TunnelController> _logger;

        public TunnelController(TunnelRepo repo, ILogger<TunnelController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<TunnelValueObject> Get()
        {
            return _repo.GetAllTunnels();
        }
    }
}