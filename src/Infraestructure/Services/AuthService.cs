using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;
using GymManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using GymManagement.Application.Exceptions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GymManagement.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<bool> SignUpAsync(UserRequest request, string baseUrl)
        {
            User? existingUser = GetUserByEmail(request.Email);

            if (existingUser != null)
            {
                if (existingUser.IsEmailConfirmed)
                {
                    return false; // Email strictly in use
                }

                // If user exists but is not confirmed, check if expired (48hs)
                if (existingUser.EmailConfirmationTokenExpiration.HasValue &&
                    existingUser.EmailConfirmationTokenExpiration.Value < DateTime.UtcNow)
                {
                    RemoveUser(existingUser);
                    _context.SaveChanges();
                }
                else
                {
                    // Unconfirmed user exists and token is still valid. Delete existing draft to re-issue
                    RemoveUser(existingUser);
                    _context.SaveChanges();
                }
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            Guid newId = Guid.NewGuid();
            string token = Guid.NewGuid().ToString("N");

            var client = new Client
            {
                UserId = newId,
                Name = request.Name,
                Email = request.Email,
                Password = hashedPassword,
                DateOfBirth = request.DateOfBirth,
                DNI = request.DNI,
                Gender = request.Gender,
                PhoneNumber = request.PhoneNumber,
                IsEmailConfirmed = false,
                EmailConfirmationToken = token,
                EmailConfirmationTokenExpiration = DateTime.UtcNow.AddHours(48)
            };

            _context.Clients.Add(client);
            _context.SaveChanges();

            string clientAppUrl = _configuration["EmailSettings:ClientAppUrl"] ?? "http://localhost:5173";
            string confirmUrl = $"{clientAppUrl}/confirm-email?email={Uri.EscapeDataString(request.Email)}&token={token}";
            string htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #2b2b2b;'>¡Bienvenido a High Level Performance!</h2>
                <p>Hola <strong>{request.Name}</strong>, gracias por registrarte.</p>
                <p>Por favor confirma tu dirección de correo electrónico haciendo clic en el siguiente botón:</p>
                <p style='margin: 30px 0; text-align: center;'>
                    <a href='{confirmUrl}' style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Confirmar mi Email</a>
                </p>
                <p style='color: #666; font-size: 13px;'>Este enlace expirará en 48 horas. Si no confirmas tu cuenta en ese plazo, la cuenta será eliminada y deberás registrarte nuevamente.</p>
            </div>";

            await _emailService.SendEmailAsync(request.Email, "Confirmación de correo electrónico - Gym Management", htmlBody);

            return true;
        }

        public AuthResponse? SignIn(SignInRequest request)
        {
            User? user = GetUserByEmail(request.Email);
            string? role = null;

            if (user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                if (user is Client) role = "Client";
                else if (user is Trainer) role = "Trainer";
                else if (user is Admin) role = "Admin";
            }

            if (user == null || role == null)
            {
                throw new UnauthorizedException("Credenciales incorrectas.");
            }

            if (!user.IsEmailConfirmed)
            {
                if (user.EmailConfirmationTokenExpiration.HasValue &&
                    user.EmailConfirmationTokenExpiration.Value < DateTime.UtcNow)
                {
                    RemoveUser(user);
                    _context.SaveChanges();
                    throw new UnauthorizedException("El plazo de 48 horas para confirmar tu email ha expirado y tu cuenta fue eliminada. Por favor regístrate nuevamente.");
                }

                throw new UnauthorizedException("Debes confirmar tu correo electrónico antes de iniciar sesión. Revisa tu casilla de correo.");
            }

            return BuildAuthResponse(user.UserId, user.Email, role);
        }

        public bool ConfirmEmail(string email, string token)
        {
            User? user = GetUserByEmail(email);

            if (user == null || user.EmailConfirmationToken != token)
            {
                return false;
            }

            if (user.EmailConfirmationTokenExpiration.HasValue &&
                user.EmailConfirmationTokenExpiration.Value < DateTime.UtcNow)
            {
                RemoveUser(user);
                _context.SaveChanges();
                return false;
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiration = null;
            _context.SaveChanges();

            return true;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            User? user = GetUserByEmail(request.Email);

            if (user == null || !user.IsEmailConfirmed || user.IsUserDeleted)
            {
                // Return true to prevent email enumeration attack
                return true;
            }

            string token = Guid.NewGuid().ToString("N");
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(2);
            _context.SaveChanges();

            string clientAppUrl = _configuration["EmailSettings:ClientAppUrl"] ?? "http://localhost:5173";
            string resetUrl = $"{clientAppUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={token}";

            string htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                <h2 style='color: #2b2b2b;'>Restablecimiento de contraseña</h2>
                <p>Hola <strong>{user.Name}</strong>,</p>
                <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta en Gym Management.</p>
                <p style='margin: 30px 0; text-align: center;'>
                    <a href='{resetUrl}' style='background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Restablecer mi Contraseña</a>
                </p>
                <p style='color: #666; font-size: 13px;'>Este enlace expirará en 2 horas. Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
            </div>";

            await _emailService.SendEmailAsync(user.Email, "Restablecer contraseña - Gym Management", htmlBody);
            return true;
        }

        public bool ResetPassword(ResetPasswordRequest request)
        {
            User? user = GetUserByEmail(request.Email);

            if (user == null || user.IsUserDeleted || !user.IsEmailConfirmed || user.PasswordResetToken != request.Token)
            {
                return false;
            }

            if (!user.PasswordResetTokenExpiration.HasValue || user.PasswordResetTokenExpiration.Value < DateTime.UtcNow)
            {
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiration = null;
                _context.SaveChanges();
                return false;
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiration = null;
            _context.SaveChanges();

            return true;
        }

        private User? GetUserByEmail(string email)
        {
            Client? client = _context.Clients.FirstOrDefault(c => c.Email == email && !c.IsUserDeleted);
            if (client != null) return client;

            Trainer? trainer = _context.Trainers.FirstOrDefault(t => t.Email == email && !t.IsUserDeleted);
            if (trainer != null) return trainer;

            Admin? admin = _context.Admins.FirstOrDefault(a => a.Email == email && !a.IsUserDeleted);
            if (admin != null) return admin;

            return null;
        }

        private void RemoveUser(User user)
        {
            if (user is Client client) _context.Clients.Remove(client);
            else if (user is Trainer trainer) _context.Trainers.Remove(trainer);
            else if (user is Admin admin) _context.Admins.Remove(admin);
        }

        private AuthResponse BuildAuthResponse(Guid userId, string email, string role)
        {
            return new AuthResponse
            {
                Token = GenerateToken(userId, email, role),
                Role = role,
                UserId = userId,
                Email = email
            };
        }

        private string GenerateToken(Guid userId, string email, string role)
        {
            string key = _configuration["Jwt:Key"]!;
            string issuer = _configuration["Jwt:Issuer"]!;
            string audience = _configuration["Jwt:Audience"]!;
            int expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"]!);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
