using GymManagement.Application.Requests;
using GymManagement.Application.Services;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = Policies.OnlyAdmin)]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("Users")]
        public ActionResult GetAll()
        {
            var users = _adminService.GetAllUsers();
            return Ok(users);
        }

        [HttpGet("{UserId}")]
        public IActionResult GetUserById(Guid UserId)
        {
            var user = _adminService.GetUserById(UserId);
            if (user == null)
            {
                return NotFound();
            } return Ok(user);
        }

        [HttpGet("DeletedUsers/{UserId}")]
        public IActionResult GetUserDeleted(Guid UserId)
        {
            var user = _adminService.GetUserDeleted(UserId);
            if (user == null)
            {
                return NotFound();
            } return Ok(user);
        }

        [HttpPut("Update/{UserId}")]
        public IActionResult UpdateUser(Guid UserId, [FromBody] UserRequest userRequest)
        {
            var success = _adminService.UpdateUser(UserId, userRequest);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("Delete/{UserId}")]
        public IActionResult DeleteUser(Guid UserId)
        {
            var success = _adminService.DeleteUser(UserId);
            return success ? NoContent() : NotFound();
        }

        [HttpPost("Recover/{UserId}")]
        public IActionResult RecoverUser(Guid UserId)
        {
            var success = _adminService.RecoverUser(UserId);
            return success ? NoContent() : NotFound();
        }
    }
}