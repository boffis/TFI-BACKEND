using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MembershipType",
                table: "Memberships");

            migrationBuilder.AddColumn<Guid>(
                name: "MembershipPlanId",
                table: "Memberships",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "MembershipPlans",
                columns: table => new
                {
                    MembershipPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DurationInDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPlans", x => x.MembershipPlanId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_MembershipPlanId",
                table: "Memberships",
                column: "MembershipPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_MembershipPlans_MembershipPlanId",
                table: "Memberships",
                column: "MembershipPlanId",
                principalTable: "MembershipPlans",
                principalColumn: "MembershipPlanId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_MembershipPlans_MembershipPlanId",
                table: "Memberships");

            migrationBuilder.DropTable(
                name: "MembershipPlans");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_MembershipPlanId",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "MembershipPlanId",
                table: "Memberships");

            migrationBuilder.AddColumn<int>(
                name: "MembershipType",
                table: "Memberships",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
