using GymManagement.Domain.Enums;

namespace GymManagement.Application.Requests
{
    public class SignUpRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public UserRole UserRole { get; set; } 

        public string Specialization { get; set; } = string.Empty;
    }
}