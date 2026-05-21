using GymManagement.Application.Requests;
using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;

namespace GymManagement.Application.Responses
{
    public class UserResponse
    {
        public required Guid UserId { get; set; }

        public required string Name { get; set; }

        public required string Email { get; set; }

        public required string Password { get; set; }
    }
}
