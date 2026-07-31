namespace GymManagement.Application.Responses
{
    public class GetAllUsersResponse
    {
        public required List<ClientResponse> ClientList { get; set; }
        public required List<TrainerResponse> TrainerList { get; set; }
        public required List<UserResponse> AdminList { get; set; }
    }
}
