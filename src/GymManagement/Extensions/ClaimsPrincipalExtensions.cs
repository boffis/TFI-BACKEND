using System;
using System.Security.Claims;
using GymManagement.Application.Exceptions;

namespace GymManagement.Presentation.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? 
                throw new UnauthorizedException("Usuario no autenticado");
            return Guid.Parse(claim.Value);
        }

        public static string GetUserRole(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.Role) ?? 
                throw new UnauthorizedException("El usuario no tiene un rol asignado");
            return claim.Value;
        }
    }
}
