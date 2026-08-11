using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Presentation.Authorization;
using GymManagement.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _userService.GetAllAsync());
        }

        [HttpGet("Deleted")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult GetAllDeleted()
        {
            return Ok(_userService.GetAllDeleted());
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetDetailedByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpGet("Deleted/{id}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult GetDeletedById(Guid id)
        {
            var user = _userService.GetDeletedById(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("{id}")]
        [Authorize]
        public IActionResult Update(Guid id, [FromBody] UserRequest request)
        {
            var currentUserId = User.GetUserId();
            var currentUserRole = User.GetUserRole();

            if (currentUserId != id && currentUserRole != "Admin")
            {
                return Forbid();
            }

            var success = _userService.Update(id, request);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(Guid id)
        {
            var currentUserId = User.GetUserId();
            var currentUserRole = User.GetUserRole();

            if (currentUserId != id && currentUserRole != "Admin")
            {
                return Forbid();
            }

            var success = _userService.Delete(id);
            return success ? NoContent() : NotFound();
        }

        [HttpPost("Recover/{id}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult Recover(Guid id)
        {
            var success = _userService.Recover(id);
            return success ? NoContent() : NotFound();
        }

        [HttpPatch("{id}/Role")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<IActionResult> ChangeRole(Guid id, [FromQuery] string newRole, [FromQuery] string? specialization = null)
        {
            try
            {
                var success = await _userService.ChangeRoleAsync(id, newRole, specialization);
                return success ? NoContent() : NotFound();
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(501, ex.Message);
            }
        }

        [HttpGet("activetrainers")]
        [Authorize]
        public IActionResult GetActiveTrainers()
        {
            return Ok(_userService.GetActiveTrainers());
        }
    }
}
