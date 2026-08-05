using GymManagement.Application.Responses;

namespace GymManagement.Application.Mappers
{
    public static class AuthMapper
    {
        public static AuthResponse ToAuthResponse(UserDetailedResponse detailedUser, string role, string token)
        {
            return new AuthResponse
            {
                Token = token,
                Role = detailedUser.Role,
                UserId = detailedUser.UserId,
                Email = detailedUser.Email,
                Name = detailedUser.Name,
                DateOfBirth = detailedUser.DateOfBirth,
                DNI = detailedUser.DNI,
                Gender = detailedUser.Gender,
                PhoneNumber = detailedUser.PhoneNumber,
                Specialization = detailedUser.Specialization,
                Payments = detailedUser.Payments,
                Memberships = detailedUser.Memberships,
                Inscriptions = detailedUser.Inscriptions,
                TaughtClasses = detailedUser.TaughtClasses
            };
        }
    }
}
