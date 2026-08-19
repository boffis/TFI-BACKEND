namespace GymManagement.Domain.Enums
{
    /// <summary>
    /// Whether an enrolled client turned up. Stored as an int, so these values are part of the
    /// database contract — reordering them changes what existing rows mean.
    /// </summary>
    public enum AttendanceStatus
    {
        /// <summary>Every inscription starts here.</summary>
        NotRecorded = 0,

        Present = 1,

        Absent = 2
    }
}
