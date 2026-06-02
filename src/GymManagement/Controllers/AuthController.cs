using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("SignUp")]
        [AllowAnonymous]
        public IActionResult SignUp([FromBody] SignUpRequest request)
        {
            if (request.UserRole != Domain.Enums.UserRole.Client)
                return BadRequest("Solo se permite registrar clientes desde este endpoint.");

            var response = _authService.SignUp(request);
            if (response == null)
                return BadRequest("El email ya está en uso.");

            return Ok(response);
        }

        [HttpPost("SignIn")]
        [AllowAnonymous]
        public IActionResult SignIn([FromBody] SignInRequest request)
        {
            var response = _authService.SignIn(request);
            if (response == null)
                return Unauthorized("Credenciales inválidas.");

            return Ok(response);
        }

        [HttpPost("CreateTrainer")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult CreateTrainer([FromBody] SignUpRequest request)
        {
            if (request.UserRole != Domain.Enums.UserRole.Trainer)
                return BadRequest("Este endpoint solo crea trainers.");

            var response = _authService.SignUp(request);
            if (response == null)
                return BadRequest("El email ya está en uso.");

            return Ok(response);
        }

        [HttpPost("CreateAdmin")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult CreateAdmin([FromBody] SignUpRequest request)
        {
            if (request.UserRole != Domain.Enums.UserRole.Admin)
                return BadRequest("Este endpoint solo crea admins.");

            var response = _authService.SignUp(request);
            if (response == null)
                return BadRequest("El email ya está en uso.");

            return Ok(response);
        }
    }
}