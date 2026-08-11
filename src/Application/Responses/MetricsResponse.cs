namespace GymManagement.Application.Responses
{
    /// <summary>
    /// Everything the admin metrics dashboard needs, in one response.
    /// <para>
    /// This is a read model: the shapes below are what the dashboard renders, and the repository
    /// projects straight into them. Adding a parallel set of near-identical "query result" types
    /// would double the code without protecting anything — nothing writes through this path.
    /// </para>
    /// </summary>
    public class MetricsResponse
    {
        public RevenueMetricsResponse Revenue { get; set; } = new();
        public MemberMetricsResponse Members { get; set; } = new();
        public List<PlanMetricsResponse> Plans { get; set; } = [];
        public ClassMetricsResponse Classes { get; set; } = new();
        public AttendanceMetricsResponse Attendance { get; set; } = new();
        public List<TrainerMetricsResponse> Trainers { get; set; } = [];

        /// <summary>How many days the "recent" windows (classes, attendance, trainers) cover.</summary>
        public int RecentWindowDays { get; set; }

        /// <summary>
        /// Which <c>Payment.PaymentState</c> values were counted as money received. Surfaced so the
        /// dashboard can state its own assumption instead of presenting a total with no provenance.
        /// </summary>
        public List<string> RevenueStates { get; set; } = [];
    }

    public class RevenueMetricsResponse
    {
        public decimal Total { get; set; }
        public decimal ThisMonth { get; set; }
        public int PaidPaymentCount { get; set; }

        /// <summary>Oldest month first, so the dashboard can render it as a left-to-right timeline.</summary>
        public List<MonthlyRevenueResponse> ByMonth { get; set; } = [];

        /// <summary>
        /// Every payment state present in the database with its count and value. This is what makes
        /// the revenue figure auditable — an unexpected state showing up here explains a gap.
        /// </summary>
        public List<PaymentStateResponse> ByState { get; set; } = [];
    }

    public class MonthlyRevenueResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int PaymentCount { get; set; }
    }

    public class PaymentStateResponse
    {
        public string State { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public bool CountsAsRevenue { get; set; }
    }

    public class MemberMetricsResponse
    {
        public int TotalClients { get; set; }
        public int WithActiveMembership { get; set; }
        public int Expired { get; set; }
        public int Cancelled { get; set; }
    }

    public class PlanMetricsResponse
    {
        public Guid MembershipPlanId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int ActiveMemberships { get; set; }
        public int TotalMemberships { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ClassMetricsResponse
    {
        public int Upcoming { get; set; }
        public int HeldInWindow { get; set; }
        public int InscriptionsInWindow { get; set; }
        public int CapacityInWindow { get; set; }

        /// <summary>Inscriptions as a percentage of capacity across classes held in the window.</summary>
        public double OccupancyRate { get; set; }

        public List<PopularClassResponse> MostPopular { get; set; } = [];
    }

    public class PopularClassResponse
    {
        public string ClassName { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public int Inscriptions { get; set; }
        public double OccupancyRate { get; set; }
    }

    public class AttendanceMetricsResponse
    {
        public int Present { get; set; }
        public int Absent { get; set; }
        public int NotRecorded { get; set; }

        /// <summary>Present as a percentage of marked enrolments. Unmarked rows are excluded.</summary>
        public double AttendanceRate { get; set; }

        /// <summary>Enrolments actually marked, as a percentage of all enrolments in the window.</summary>
        public double RecordedRate { get; set; }
    }

    public class TrainerMetricsResponse
    {
        public Guid TrainerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ClassesInWindow { get; set; }
        public int UpcomingClasses { get; set; }
        public int InscriptionsInWindow { get; set; }
    }
}
