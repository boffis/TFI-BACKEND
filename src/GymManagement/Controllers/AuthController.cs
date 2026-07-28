using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
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
        public async Task<IActionResult> SignUp([FromBody] UserRequest request)
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            bool result = await _authService.SignUpAsync(request, baseUrl);
            
            if (!result)
                return BadRequest("El email ya se encuentra registrado.");

            return Ok(new { message = "Registro exitoso. Se ha enviado un correo de confirmación a tu casilla de email. Por favor confírmalo antes de iniciar sesión." });
        }

        [HttpGet("ConfirmEmail")]
        public IActionResult ConfirmEmail([FromQuery] string email, [FromQuery] string token)
        {
            bool success = _authService.ConfirmEmail(email, token);
            if (!success)
            {
                return BadRequest(new { message = "El enlace de confirmación es incorrecto o han transcurrido más de 48 horas. Si tu token expiró, la cuenta fue eliminada y deberás registrarte nuevamente." });
            }

            return Ok(new { message = "Tu cuenta ha sido verificada correctamente. Ya puedes iniciar sesión en Gym Management." });
        }

        [HttpPost("SignIn")]
        public IActionResult SignIn([FromBody] SignInRequest request)
        {
            var response = _authService.SignIn(request);
            if (response == null)
                return Unauthorized("Credenciales inválidas.");

            return Ok(response);
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request);
            return Ok(new { message = "Si la cuenta existe y está confirmada, se ha enviado un correo electrónico con instrucciones para restablecer tu contraseña." });
        }
        
        [HttpPost("ResetPassword")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest request)
        {
            bool success = _authService.ResetPassword(request);
            if (!success)
            {
                return BadRequest(new { message = "El token de restablecimiento es inválido o ha expirado." });
            }

            return Ok(new { message = "Tu contraseña ha sido restablecida exitosamente. Ya puedes iniciar sesión con tu nueva contraseña." });
        }
    }
}
