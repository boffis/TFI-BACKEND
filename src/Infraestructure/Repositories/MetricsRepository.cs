using GymManagement.Application.Interfaces;
using GymManagement.Application.Responses;
using GymManagement.Domain.Enums;
using GymManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Repositories
{
    /// <inheritdoc cref="IMetricsRepository"/>
    public class MetricsRepository : IMetricsRepository
    {
        private readonly ApplicationDbContext _context;

        public MetricsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(decimal Total, int Count)> GetRevenueTotalAsync(
            IReadOnlyCollection<string> revenueStates)
        {
            var states = Normalise(revenueStates);

            var result = await _context.Payments
                .AsNoTracking()
                .Where(p => states.Contains(p.PaymentState.ToLower()))
                .GroupBy(p => 1)
                .Select(g => new { Total = g.Sum(p => (decimal?)p.Price) ?? 0m, Count = g.Count() })
                .FirstOrDefaultAsync();

            return (result?.Total ?? 0m, result?.Count ?? 0);
        }

        public async Task<List<PaymentPoint>> GetPaymentsSinceAsync(DateTime utcFrom)
            => await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate >= utcFrom)
                .Select(p => new PaymentPoint(p.PaymentDate, p.Price, p.PaymentState))
                .ToListAsync();

        public async Task<List<PaymentStateResponse>> GetPaymentStateBreakdownAsync()
            => await _context.Payments
                .AsNoTracking()
                .GroupBy(p => p.PaymentState)
                .Select(g => new PaymentStateResponse
                {
                    State = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(p => (decimal?)p.Price) ?? 0m
                })
                .OrderByDescending(s => s.Count)
                .ToListAsync();

        public async Task<MemberCounts> GetMemberCountsAsync(DateTime utcNow)
        {
            var totalClients = await _context.Clients
                .AsNoTracking()
                .CountAsync(c => !c.IsUserDeleted);

            // Counted per user, not per membership row: a client holding two non-cancelled
            // memberships is still one active member.
            var withActive = await _context.Memberships
                .AsNoTracking()
                .Where(m => !m.IsCancelled && m.ExpirationDate > utcNow)
                .Select(m => m.UserId)
                .Distinct()
                .CountAsync();

            var expired = await _context.Memberships
                .AsNoTracking()
                .Where(m => !m.IsCancelled && m.ExpirationDate <= utcNow)
                .Select(m => m.UserId)
                .Distinct()
                .CountAsync();

            var cancelled = await _context.Memberships
                .AsNoTracking()
                .Where(m => m.IsCancelled)
                .Select(m => m.UserId)
                .Distinct()
                .CountAsync();

            return new MemberCounts(totalClients, withActive, expired, cancelled);
        }

        public async Task<List<PlanMetricsResponse>> GetPlanBreakdownAsync(
            IReadOnlyCollection<string> revenueStates)
        {
            var states = Normalise(revenueStates);
            var utcNow = DateTime.UtcNow;

            return await _context.MembershipPlans
                .AsNoTracking()
                .Select(plan => new PlanMetricsResponse
                {
                    MembershipPlanId = plan.MembershipPlanId,
                    Type = plan.Type ?? string.Empty,
                    Price = plan.Price,
                    DurationInDays = plan.DurationInDays,
                    TotalMemberships = _context.Memberships
                        .Count(m => m.MembershipPlanId == plan.MembershipPlanId),
                    ActiveMemberships = _context.Memberships
                        .Count(m => m.MembershipPlanId == plan.MembershipPlanId
                                    && !m.IsCancelled
                                    && m.ExpirationDate > utcNow),
                    Revenue = _context.Payments
                        .Where(p => p.Membership.MembershipPlanId == plan.MembershipPlanId
                                    && states.Contains(p.PaymentState.ToLower()))
                        .Sum(p => (decimal?)p.Price) ?? 0m
                })
                .OrderByDescending(p => p.Revenue)
                .ToListAsync();
        }

        public async Task<ClassCounts> GetClassCountsAsync(DateTime windowStart, DateTime now)
        {
            var upcoming = await _context.GymClasses
                .AsNoTracking()
                .CountAsync(gc => !gc.IsClassDeleted && gc.Schedule > now);

            var held = await _context.GymClasses
                .AsNoTracking()
                .Where(gc => !gc.IsClassDeleted && gc.Schedule <= now && gc.Schedule >= windowStart)
                .GroupBy(gc => 1)
                .Select(g => new
                {
                    Sessions = g.Count(),
                    Capacity = g.Sum(gc => gc.MaxCapacity),
                    Inscriptions = g.Sum(gc => gc.Inscriptions.Count())
                })
                .FirstOrDefaultAsync();

            return new ClassCounts(
                upcoming,
                held?.Sessions ?? 0,
                held?.Inscriptions ?? 0,
                held?.Capacity ?? 0);
        }

        public async Task<List<PopularClassResponse>> GetPopularClassesAsync(
            DateTime windowStart, DateTime now, int take)
        {
            // Grouped by name rather than id: an admin wants "how is Spinning doing", not how one
            // Tuesday session did. Recurring sessions all share the schedule's class name.
            var rows = await _context.GymClasses
                .AsNoTracking()
                .Where(gc => !gc.IsClassDeleted && gc.Schedule <= now && gc.Schedule >= windowStart)
                .GroupBy(gc => gc.ClassName)
                .Select(g => new
                {
                    ClassName = g.Key,
                    Sessions = g.Count(),
                    Inscriptions = g.Sum(gc => gc.Inscriptions.Count()),
                    Capacity = g.Sum(gc => gc.MaxCapacity)
                })
                .OrderByDescending(g => g.Inscriptions)
                .Take(take)
                .ToListAsync();

            return [.. rows.Select(r => new PopularClassResponse
            {
                ClassName = r.ClassName,
                Sessions = r.Sessions,
                Inscriptions = r.Inscriptions,
                OccupancyRate = Percentage(r.Inscriptions, r.Capacity)
            })];
        }

        public async Task<AttendanceCounts> GetAttendanceCountsAsync(DateTime windowStart, DateTime now)
        {
            var rows = await _context.Inscriptions
                .AsNoTracking()
                .Where(i => !i.GymClass.IsClassDeleted
                            && i.GymClass.Schedule <= now
                            && i.GymClass.Schedule >= windowStart)
                .GroupBy(i => i.AttendanceStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int CountFor(AttendanceStatus status) =>
                rows.FirstOrDefault(r => r.Status == status)?.Count ?? 0;

            return new AttendanceCounts(
                CountFor(AttendanceStatus.Present),
                CountFor(AttendanceStatus.Absent),
                CountFor(AttendanceStatus.NotRecorded));
        }

        public async Task<List<TrainerMetricsResponse>> GetTrainerBreakdownAsync(
            DateTime windowStart, DateTime now)
            => await _context.Trainers
                .AsNoTracking()
                .Where(t => !t.IsUserDeleted)
                .Select(t => new TrainerMetricsResponse
                {
                    TrainerId = t.UserId,
                    Name = t.Name,
                    ClassesInWindow = _context.GymClasses
                        .Count(gc => gc.TrainerId == t.UserId && !gc.IsClassDeleted
                                     && gc.Schedule <= now && gc.Schedule >= windowStart),
                    UpcomingClasses = _context.GymClasses
                        .Count(gc => gc.TrainerId == t.UserId && !gc.IsClassDeleted
                                     && gc.Schedule > now),
                    InscriptionsInWindow = _context.Inscriptions
                        .Count(i => i.GymClass.TrainerId == t.UserId && !i.GymClass.IsClassDeleted
                                    && i.GymClass.Schedule <= now && i.GymClass.Schedule >= windowStart)
                })
                .OrderByDescending(t => t.InscriptionsInWindow)
                .ToListAsync();

        /// <summary>
        /// Payment states are written inconsistently upstream ("Pending" locally, "pending" and raw
        /// Mercado Pago values from the webhook), so matching is done in lower case on both sides.
        /// </summary>
        private static List<string> Normalise(IReadOnlyCollection<string> states)
            => [.. states.Select(s => s.ToLowerInvariant())];

        private static double Percentage(int part, int whole)
            => whole == 0 ? 0 : Math.Round(part * 100.0 / whole, 1);
    }
}
