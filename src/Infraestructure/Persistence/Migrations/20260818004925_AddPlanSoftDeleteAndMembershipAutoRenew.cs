using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanSoftDeleteAndMembershipAutoRenew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue is true, not the bool default EF scaffolds: every membership that already
            // exists is renewing normally, and nothing has been deliberately stopped on it. Backfilling
            // false would tell the preapproval webhook that every one of those cancellations was the
            // gym's own doing, so a client cancelling their subscription would keep gym access.
            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "Memberships",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MembershipPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MembershipPlans");
        }
    }
}
