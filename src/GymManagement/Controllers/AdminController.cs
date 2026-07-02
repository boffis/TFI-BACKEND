using GymManagement.Application.Requests;
using GymManagement.Application.Services;
using GymManagement.Domain.Entities;
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
        public ActionResult<List<Admin>> GetAllAdmins()
        {
            var admins = _adminService.GetAllAdmins();
            return Ok(admins);
        }

        [HttpGet("{UserId}")]
        public IActionResult GetAdminById(Guid UserId)
        {
            var admin = _adminService.GetAdminById(UserId);
            if (admin == null) return NotFound();
            return Ok(admin);
        }

        [HttpGet("Deleted/{UserId}")]
        public IActionResult GetDeletedAdminById(Guid UserId)
        {
            var admin = _adminService.GetDeletedAdminById(UserId);
            if (admin == null) return NotFound();
            return Ok(admin);
        }

        [HttpPut("Update/{UserId}")]
        public IActionResult UpdateAdmin(Guid UserId, [FromBody] UserRequest request)
        {
            var success = _adminService.UpdateAdmin(UserId, request);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("Delete/{UserId}")]
        public IActionResult DeleteAdmin(Guid UserId)
        {
            var success = _adminService.DeleteAdmin(UserId);
            return success ? NoContent() : NotFound();
        }

        [HttpPost("Recover/{UserId}")]
        public IActionResult RecoverAdmin(Guid UserId)
        {
            var success = _adminService.RecoverAdmin(UserId);
            return success ? NoContent() : NotFound();
        }
    }
}