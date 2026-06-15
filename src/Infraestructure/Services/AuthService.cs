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

        public AuthResponse? SignUpClient(UserRequest request)
        {
            if (_context.Users.Any(u => u.Email == request.Email)) return null;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            Guid newId = Guid.NewGuid();

            var client = new Client
            {
                UserId = newId,
                Name = request.Name,
                Email = request.Email,
                Password = hashedPassword,
                UserRole = UserRole.Client
            };
            _context.Clients.Add(client);
            _context.SaveChanges();

            return BuildAuthResponse(newId, request.Email, UserRole.Client.ToString());
        }

        public AuthResponse? SignUpTrainer(TrainerRequest request)
        {
            if (_context.Users.Any(u => u.Email == request.Email)) return null;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            Guid newId = Guid.NewGuid();

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
            _context.SaveChanges();

            return BuildAuthResponse(newId, request.Email, UserRole.Trainer.ToString());
        }

        public AuthResponse? SignUpAdmin(UserRequest request)
        {
            if (_context.Users.Any(u => u.Email == request.Email)) return null;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            Guid newId = Guid.NewGuid();

            var admin = new Admin
            {
                UserId = newId,
                Name = request.Name,
                Email = request.Email,
                Password = hashedPassword,
                UserRole = UserRole.Admin
            };
            _context.Admins.Add(admin);
            _context.SaveChanges();

            return BuildAuthResponse(newId, request.Email, UserRole.Admin.ToString());
        }

        public AuthResponse? SignIn(SignInRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email && !u.IsUserDeleted);
            if (user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return BuildAuthResponse(user.UserId, user.Email, user.UserRole.ToString());
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