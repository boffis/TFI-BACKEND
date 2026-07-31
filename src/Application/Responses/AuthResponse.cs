namespace GymManagement.Application.Responses
{
    public class AuthResponse : UserDetailedResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}
