namespace GymManagement.Application.Responses
{
    public class UserDetailedResponse : UserResponse
    {
        public List<PaymentResponse> Payments { get; set; } = new();
        public List<MembershipResponse> Memberships { get; set; } = new();
        public List<InscriptionResponse>? Inscriptions { get; set; }
        public List<GymClassResponse>? TaughtClasses { get; set; }
    }
}
