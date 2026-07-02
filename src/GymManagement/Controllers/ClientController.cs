using GymManagement.Application.Requests;
using GymManagement.Application.Services;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = Policies.OnlyClient)]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly ClientService _clientService;

        public ClientController(ClientService clientService)
        {
            _clientService = clientService;
        }
        [HttpGet("Clients")]
        public IActionResult GetClients()
        {
            var clients = _clientService.GetAllClients();
            return Ok(clients);
        }

        [HttpGet("{userId}")]
        public IActionResult GetClientById(Guid userId)
        {
            var client = _clientService.GetClientById(userId);
            if (client == null) return NotFound();
            return Ok(client);
        }

        [HttpGet("Deleted/{userId}")]
        public IActionResult GetDeletedClientById(Guid userId)
        {
            var client = _clientService.GetDeletedClientById(userId);
            if (client == null) return NotFound();
            return Ok(client);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateClient(Guid userId, [FromBody] UserRequest request)
        {
            var success = _clientService.UpdateClient(userId, request);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteClient(Guid userId)
        {
            var success = _clientService.DeleteClient(userId);
            return success ? NoContent() : NotFound();
        }

        [HttpPost("Recover/{userId}")]
        public IActionResult RecoverClient(Guid userId)
        {
            var success = _clientService.RecoverClient(userId);
            return success ? NoContent() : NotFound();
        }
    }
}