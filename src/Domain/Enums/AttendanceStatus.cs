namespace GymManagement.Domain.Enums
{
    /// <summary>
    /// Whether an enrolled client actually turned up to the class.
    /// Stored as an int, so the numeric values are part of the database contract —
    /// reorder them and existing rows change meaning.
    /// </summary>
    public enum AttendanceStatus
    {
        /// <summary>No one has marked this client yet. Every inscription starts here.</summary>
        NotRecorded = 0,

        Present = 1,

        Absent = 2
    }
}
