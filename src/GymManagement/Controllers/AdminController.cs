using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Controllers
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
    }
}
