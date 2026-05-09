using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        public ActionResult GetAllUsers()
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
            }
            return Ok(user);
        }

        [HttpPost]

        public ActionResult<UserResponse> CreateUser([FromBody] UserRequest user)
        {
            var createdUser = _adminService.CreateUser(user);
            return CreatedAtAction(nameof(GetUserById), new { UserId = createdUser }, createdUser);
        }
       
    }
}
