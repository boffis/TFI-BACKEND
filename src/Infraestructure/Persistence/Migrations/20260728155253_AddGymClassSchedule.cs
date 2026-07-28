using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGymClassSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Trainers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiration",
                table: "Trainers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GymClassScheduleId",
                table: "GymClasses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiration",
                table: "Clients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Admins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiration",
                table: "Admins",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GymClassSchedules",
                columns: table => new
                {
                    GymClassScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClassDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    TimeOfDay = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsWeekly = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymClassSchedules", x => x.GymClassScheduleId);
                    table.ForeignKey(
                        name: "FK_GymClassSchedules_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymClasses_GymClassScheduleId",
                table: "GymClasses",
                column: "GymClassScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_GymClassSchedules_TrainerId",
                table: "GymClassSchedules",
                column: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GymClasses_GymClassSchedules_GymClassScheduleId",
                table: "GymClasses",
                column: "GymClassScheduleId",
                principalTable: "GymClassSchedules",
                principalColumn: "GymClassScheduleId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GymClasses_GymClassSchedules_GymClassScheduleId",
                table: "GymClasses");

            migrationBuilder.DropTable(
                name: "GymClassSchedules");

            migrationBuilder.DropIndex(
                name: "IX_GymClasses_GymClassScheduleId",
                table: "GymClasses");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiration",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "GymClassScheduleId",
                table: "GymClasses");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiration",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiration",
                table: "Admins");
        }
    }
}
