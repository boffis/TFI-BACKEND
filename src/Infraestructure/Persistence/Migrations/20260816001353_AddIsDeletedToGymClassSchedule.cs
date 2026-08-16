using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToGymClassSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GymClassSchedules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Previously, "delete" was implemented by setting IsActive = false, with no distinct
            // deleted flag. Any row already flagged that way was a soft-deleted schedule, not a
            // genuinely paused one, so carry that meaning forward onto the new column.
            migrationBuilder.Sql("UPDATE [GymClassSchedules] SET [IsDeleted] = 1 WHERE [IsActive] = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GymClassSchedules");
        }
    }
}
