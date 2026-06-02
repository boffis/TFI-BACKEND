using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;
using GymManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
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

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public AuthResponse? SignUp(SignUpRequest request)
        {
            bool usedEmail = _context.Clients.Any(c => c.Email == request.Email)
                   || _context.Trainers.Any(t => t.Email == request.Email)
                   || _context.Admins.Any(a => a.Email == request.Email);

            if (usedEmail) return null;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            Guid newId = Guid.NewGuid();
            string role;

            switch (request.UserRole)
            {
                case UserRole.Admin:
                    var admin = new Admin
                    {
                        UserId = newId,
                        Name = request.Name,
                        Email = request.Email,
                        Password = hashedPassword,
                        UserRole = UserRole.Admin
                    };
                    _context.Admins.Add(admin);
                    role = "Admin";
                    break;

                case UserRole.Client:
                    var client = new Client
                    {
                        UserId = newId,
                        Name = request.Name,
                        Email = request.Email,
                        Password = hashedPassword,
                        UserRole = UserRole.Client
                    };
                    _context.Clients.Add(client);
                    role = "Client";
                    break;

                case UserRole.Trainer:
                    var trainer = new Trainer
                    {
                        UserId = newId,
                        Name = request.Name,
                        Email = request.Email,
                        Password = hashedPassword,
                        UserRole = UserRole.Trainer,
                        Specialization = request.Specialization
                    };
                    _context.Trainers.Add(trainer);
                    role = "Trainer";
                    break;

                default:
                    return null;
            }
            _context.SaveChanges();

            return new AuthResponse
            {
                Token = GenerateToken(newId, request.Email, request.UserRole.ToString()),
                Role = role,
                UserId = newId,
                Email = request.Email
            };
        }
        public AuthResponse? SignIn(SignInRequest request)
        {
            var client = _context.Clients.FirstOrDefault(c => c.Email == request.Email);
            if (client != null && BCrypt.Net.BCrypt.Verify(request.Password, client.Password))
            {
                return BuildAuthResponse(client.UserId, client.Email, client.UserRole.ToString());
            }

            var trainer = _context.Trainers.FirstOrDefault(t => t.Email == request.Email);
            if (trainer != null && BCrypt.Net.BCrypt.Verify(request.Password, trainer.Password))
            {
                return BuildAuthResponse(trainer.UserId, trainer.Email, trainer.UserRole.ToString());
            }

            var admin = _context.Admins.FirstOrDefault(a => a.Email == request.Email);
            if (admin != null && BCrypt.Net.BCrypt.Verify(request.Password, admin.Password))
            {
                return BuildAuthResponse(admin.UserId, admin.Email, admin.UserRole.ToString());
            }

            return null;
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