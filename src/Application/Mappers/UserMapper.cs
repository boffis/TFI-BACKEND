using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;

namespace GymManagement.Application.Mappers
{
    public static class UserMapper
    {
        public static UserResponse ToUserResponse(this User user)
        {
            return new UserResponse
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Password = user.Password,
              

            };
        }

            public static User ToUser(this UserRequest userRequest)
            {
                return  new User
                {
                    UserId = Guid.NewGuid(),
                    Name = userRequest.Name,
                    Email = userRequest.Email,
                    Password = userRequest.Password,
                    UserRole = UserRole.Client,
                    IsUserDeleted = false,
                };
        }
    }
}
