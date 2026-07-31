using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Mappers
{
    public static class ClientMapper
    {
        public static ClientResponse ToClientResponse(this Client client, bool isMembershipActive)
        {
            return new ClientResponse
            {
                UserId = client.UserId,
                Name = client.Name,
                Email = client.Email,
                DateOfBirth = client.DateOfBirth,
                DNI = client.DNI,
                Gender = client.Gender,
                PhoneNumber = client.PhoneNumber,
                Role = client.GetType().Name,
                IsMembershipActive = isMembershipActive
            };
        }
    }
}
