using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("SignUp")]
        public IActionResult SignUp([FromBody] UserRequest request)
        {
            var response = _authService.SignUpClient(request);
            if (response == null)
                return BadRequest("El email ya está en uso.");

            return Ok(response);
        }

        [HttpPost("SignIn")]
        public IActionResult SignIn([FromBody] SignInRequest request)
        {
            var response = _authService.SignIn(request);
            if (response == null)
                return Unauthorized("Credenciales inválidas.");

            return Ok(response);
        }

        [HttpPost("CreateTrainer")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult CreateTrainer([FromBody] TrainerRequest request)
        {
            var response = _authService.SignUpTrainer(request);
            if (response == null)
                return BadRequest("El email ya está en uso.");

            return Ok(response);
        }

        [HttpPost("CreateAdmin")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult CreateAdmin([FromBody] UserRequest request)
        {
            var response = _authService.SignUpAdmin(request);
            if (response == null)
                return BadRequest("El email ya está en uso.");

            return Ok(response);
        }
    }
}