using System;

namespace GymManagement.Application.Responses
{
    public class ClientSummaryResponse
    {
        public Guid ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
