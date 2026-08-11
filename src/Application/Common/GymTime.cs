namespace GymManagement.Application.Common
{
    /// <summary>
    /// The gym's local wall-clock time.
    /// <para>
    /// This app deals with two different kinds of time and they must not be mixed:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <b>Wall-clock times</b> — <c>GymClass.Schedule</c> and <c>GymClassSchedule.TimeOfDay</c>.
    /// When an admin schedules a class for 18:00 they mean 18:00 at the gym, not 18:00 UTC.
    /// These are stored without a zone and must be compared against <see cref="Now"/>.
    /// </item>
    /// <item>
    /// <b>Instants</b> — membership expirations, payment dates, token expirations, JWT lifetimes.
    /// These are moments on the global timeline and stay in UTC (<c>DateTime.UtcNow</c>).
    /// </item>
    /// </list>
    /// <para>
    /// Comparing a wall-clock time against <c>DateTime.UtcNow</c> is wrong by the gym's UTC offset:
    /// in Argentina (UTC−3) an 18:00 class would be treated as already started at 15:00 local.
    /// </para>
    /// </summary>
    public static class GymTime
    {
        /// <summary>
        /// IANA id. .NET 6+ accepts IANA ids on Windows and Windows ids on Linux, so this works
        /// on either App Service flavour; the Windows id is kept as a fallback in case the host
        /// is missing ICU data.
        /// </summary>
        private const string TimeZoneId = "America/Argentina/Buenos_Aires";
        private const string WindowsFallbackTimeZoneId = "Argentina Standard Time";

        private static readonly TimeZoneInfo Zone = Resolve();

        /// <summary>Current wall-clock time at the gym.</summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

        /// <summary>Current date at the gym, with the time component zeroed.</summary>
        public static DateTime Today => Now.Date;

        private static TimeZoneInfo Resolve()
        {
            foreach (var id in new[] { TimeZoneId, WindowsFallbackTimeZoneId })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
                {
                    // Try the next candidate.
                }
            }

            // Failing loudly beats silently running on UTC — every class time would be off by
            // the gym's offset, which is easy to miss and hard to trace back.
            throw new InvalidOperationException(
                $"Could not resolve the gym time zone. Tried '{TimeZoneId}' and " +
                $"'{WindowsFallbackTimeZoneId}'. The host is missing time zone data.");
        }
    }
}
